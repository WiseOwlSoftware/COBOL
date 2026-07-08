// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolNet.Binding;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime;
using CobolNet.Frontend.Generated;

namespace CobolNet.CodeGen;

using Core = CobolParserCore;
using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>
/// The inter-program half of the Roslyn backend (COBOLNET_INTERPROGRAM_DESIGN D1–D5; ISO §14.9.4 / §14.9.5 /
/// §14.2 / §8.4.6.3): the MULTI-UNIT run-unit emission (every top-level program unit and every contained program
/// compiles — one instantiable C# class per program, nested programs as nested classes, ONE <c>.g.cs</c> / ONE
/// assembly, the first unit as entry — design D3, SSOT §18 #8), the program-class plumbing (the
/// <see cref="ICobolProgram"/> ABI, LINKAGE carrier mapping, GLOBAL bridges, EXTERNAL backings), and the
/// CALL / CANCEL / EXIT PROGRAM / GOBACK statement emitters.
/// </summary>
public sealed partial class CSharpEmitter
{
    // ── The per-compilation unit model ──────────────────────────────────────────────────────────────────────

    /// <summary>One program unit of the compilation group: its identity, containment, PROGRAM-ID attributes
    /// (ISO §11.10 / §8.6.6), bound model, and the GLOBAL bridges its class must emit (§13.18.27 GR2).</summary>
    private sealed class CallUnit
    {
        public required string Name;
        public required string ClassName;
        public required Core.ProgramUnitContext Ctx;
        public CallUnit? Parent;
        public List<CallUnit> Children = [];
        public bool Initial, Common, Recursive;
        /// <summary>True for a FUNCTION-ID unit (ISO §9.4 — a user-defined function; program-shaped except it
        /// RETURNs a value and always possesses the recursive attribute).</summary>
        public bool IsFunction;
        /// <summary>True for a FUNCTION-ID … IS PROTOTYPE unit (ISO §11.5 Format 2 / §10.6.2 SR4 — a
        /// signature-only unit: LINKAGE-only data + a header-only procedure division). It contributes its
        /// signature to the user-function table (M2-UDF-3) but emits NO body and does NOT register in the run
        /// unit — the separately-compiled definition (in-group per GR11a, else a sibling assembly) is the
        /// activation target. Always implies <see cref="IsFunction"/>.</summary>
        public bool IsPrototype;
        public DataBinder Data = null!;
        public ReferenceResolver Refs = null!;
        public BoundProgram Bound = null!;
        public List<CallBridge> Bridges = [];

        /// <summary>The run-unit-unique containment path id (registry key; §8.4.6.3 scoping).</summary>
        public string Path => Parent is null ? Name : Parent.Path + "/" + Name;

        /// <summary>The C# nested-type reference from the top-level scope (factory construction).</summary>
        public string ClassRef => Parent is null ? ClassName : Parent.ClassRef + "." + ClassName;
    }

    /// <summary>One inherited-GLOBAL bridge a nested class emits: a <c>ref</c>-returning property aliasing the
    /// containing instance's field (ISO §13.18.27 GR2 — the name is visible in every contained program; the
    /// STORAGE stays the container's). <paramref name="Kind"/>: "field" (a global root's typed field), "backing"
    /// (a Tier-B class's string backing), or "index" (an INDEXED BY <c>long</c> field of a global table).</summary>
    private sealed record CallBridge(string Field, string Path, string Kind, DataItem? Item);

    private string _callSelfPath = "";
    private Place? _callReturningPlace;
    private int _callCounter;
    private int _callUidBand;

    /// <summary>For each GLOBAL file INHERITED from a container (ISO §13.18.30), the place of the OWNER's FILE
    /// STATUS item reached through the <c>__outer</c> instance chain. §12.4.5.8.4 GR1 NOTE 1: "In the case where
    /// a file-name is global and data-name-1 is not, data-name-1 is updated by references to file-name in
    /// contained programs even though data-name-1 is a local name" — the contained program's after-verb status
    /// store must write the OWNER's storage although the NAME is not visible to it. Rebuilt per emitted unit
    /// (nearest container first); consumed by <c>EmitStoreFileStatus</c>.</summary>
    private readonly Dictionary<FileModel, Place> _callInheritedStatusPlace = [];

    // ── Run-unit emission (replaces the single-unit Emit body; design D3) ───────────────────────────────────

    /// <summary>
    /// Emit the WHOLE compilation group: bind every program unit (containers first, so GLOBAL items inherit —
    /// the legacy <c>CollectProgramContexts</c> shape re-derived from §8.4.6 scope rules), run the whole-group
    /// image analysis over ALL units (shared <see cref="DataItem"/>s — a contained program's whole-group use of
    /// an inherited GLOBAL item must flip the owner's leaf storage), then render one class per program plus the
    /// run-unit entry wrapper (<c>Program.Main</c>: registry registration + main activation + the §14.6.11
    /// implicit CLOSE at run-unit termination).
    /// </summary>
    internal string CallEmitRunUnit(Core.CompilationUnitContext tree, EditionContext edition,
        IReadOnlyList<CobolNet.Frontend.Preprocessor.TurnEvent>? turnEvents = null)
    {
        // The group's compile-time TurnState (ISO §7.3.25; deep-dive D10) — built BEFORE binding so every unit's
        // statement binder folds the same source-ordered directive events (GR6: checking spans the compilation
        // group). Name/edition validation happens here (SR2 + the 2023-only families).
        _turnState = TurnState.Build(turnEvents, edition);

        var (units, classes) = CallCollectUnits(tree, edition);
        _callUidBand = 0;
        foreach (var iface in _ooClasses.Interfaces) OoBindInterfaceData(iface, edition);   // prototype formals (§10.6.2 SR4)
        foreach (var cls in classes) OoBindClassData(cls, edition);   // ALL signatures before ANY body (D1 pass-1)
        _ooClasses.ValidateOverrideSignatures(edition);               // §9.3.8.2 — after all formals resolve (slice 3a)
        _ooClasses.ValidateImplements(edition);                       // §9.3.11 via §9.3.8.2.3 (D-I1 — the binder is the authority)
        foreach (var cls in classes) OoBindClassBody(cls);
        // TWO-PHASE program-unit binding (M2-UDF-1 key enabler): EVERY unit's DATA division binds before ANY
        // unit's procedure body binds, so a function-identifier reference resolves the callee's RETURNING /
        // USING signatures even when the FUNCTION-ID unit FOLLOWS the caller in the compilation group
        // (§8.4.3.2.4 GR1 — the caller-side temporary takes the callee's RETURNING description; the same
        // forward-reference discipline OoClassTable D1 gives typed object references).
        foreach (var unit in units) CallBindUnitData(unit, edition);
        var userFunctions = CallBuildUserFunctionTable(units, edition);
        foreach (var unit in units) CallBindUnitProcedure(unit, userFunctions);
        // Compiler-temp description re-sync: StoreAsImage is still mutable while procedure bodies bind
        // (a ref-mod store / figurative MOVE in the MODEL's own unit flips it after a temp cloned it — the
        // M2-UDF-1 review's unit-order desync; both sides of the activation boundary must agree on the
        // carrier form). Runs after ALL procedure binds, before the image-marking pass reads the flags.
        foreach (var d in units.Select(u => u.Data)
                     .Concat(classes.SelectMany(c => new[] { c.Data, c.FactoryData })))
            foreach (var (temp, model) in d.CompilerTempClones)
                temp.StoreAsImage = model.StoreAsImage;
        foreach (var cls in classes) { MarkStoreAsImage(cls.Data); MarkStoreAsImage(cls.FactoryData); }
        foreach (var unit in units) MarkStoreAsImage(unit.Data);
        OoHarmonizeOverrideCrossings();   // C# override signatures must agree on the crossing form (review find)

        // The group EC gate: ANY use of the EC model (an enabling TURN, a RAISE/RESUME/F3/RAISING, an
        // EXCEPTION-* function) turns the machinery on; otherwise the generated source is byte-identical to a
        // pre-EC build (the zero-scaffolding invariant, SSOT §18.16).
        _ecActive = _turnState.AnyEnabled || units.Any(u => u.Bound.Ec is { Any: true })
            || classes.Any(c => c.Bound.Ec is { Any: true } || c.FactoryBound.Ec is { Any: true });

        // Per-program file-connector namespace: the runtime file registry is run-unit-global, but a file
        // connector is INTERNAL to its program (ISO §8.6.3): two programs declaring the same file-name (the
        // IC-suite PRINT-FILE pattern, e.g. IC101A's two units) must not clobber each other's connectors. Name
        // resolution is done (bound nodes hold FileModel references), so qualifying the runtime key is purely an
        // emit-side rename. An EXTERNAL FD instead keys by its run-unit EXTERNALIZED name (ISO §13.18.22.4 GR4a:
        // ONE external file connector per run unit, shared by every describer — two units' FileModels with the
        // same external name converge on ONE registry key, hence one connector; GR5: the name is the FD name).
        // Each FileModel lives in exactly ONE unit's Files list (a fix-E GLOBAL merge shares references through
        // FilesByName only), so no model is renamed twice.
        foreach (var unit in units)
            foreach (var file in unit.Data.Files)
                file.CobolName = file is { IsExternal: true, ExternalName: { } ext }
                    ? "::EXT::" + ext
                    : unit.Path + "::" + file.CobolName;
        // The OO analogue (M2-OO-1i): an OBJECT/FACTORY file connector is scoped to its class, not a program unit,
        // so the program loop above never sees it. A factory file (singleton) keys by class; an instance file keys
        // per object (a minted key held in a __fkey field — see OoQualifyClassFiles); an EXTERNAL class file keys
        // by its run-unit external name, exactly like a program's.
        foreach (var cls in classes) OoQualifyClassFiles(cls);

        // Declaratives emit the __IoCheck/__RunUse machinery, which reads CobolFile even when the unit declares
        // NO files (IC401M: mode-scoped USE procedures in a file-less flagging program) — the IO using must
        // cover both. A class-only file program (M2-OO-1i — an OBJECT/FACTORY file with no program-unit file)
        // needs it too, or the generated <c>CobolFile.Register</c>/OPEN in the class body has no CobolNet.Runtime.IO
        // import (CS0103).
        bool anyFiles = units.Any(u => u.Data.Files.Count > 0)
            || units.Any(u => u.Bound.Declaratives is { Count: > 0 })
            || classes.Any(c => c.Data.Files.Count > 0 || c.FactoryData.Files.Count > 0);

        var w = new CodeWriter();
        w.Line("// <auto-generated>");
        w.Line("//   Generated by COBOL.NET — do not edit. A COBOL program compiled to typed-native C#.");
        w.Line("// </auto-generated>");
        w.Line("#nullable enable");
        w.Line("#pragma warning disable CS0164   // unreferenced label — SEARCH/NEXT-SENTENCE emit per-boundary labels; not every one is jumped to");
        w.Line("using System;                    // Int128 — the wide arithmetic carrier (numeric design D1)");
        w.Line("using CobolNet.Runtime;          // CobolNum / CobolString substrates + the inter-program ABI (ManagedPointer / ICobolProgram / ProgramRegistry)");
        if (anyFiles)
            w.Line("using CobolNet.Runtime.IO;       // CobolFile — the sequential file-I/O facade (§8)");
        if (_ecActive || classes.Count > 0)
            // The EC model, OR any class (D10): every class's generated __CobolInvoke switch raises
            // CobolFatalException (EC-OO-UNIVERSAL, GR7c). A class-less EC-free program keeps the
            // zero-scaffolding invariant byte-exact (SSOT §18.16 — the test greps the namespace).
            w.Line("using CobolNet.Runtime.Exceptions; // CobolFatalException — the EC signal type (ISO §14.6.13) + the D10 GR7c raises");
        w.Line();

        // Interfaces first (readability only — Roslyn needs no ordering), then classes (source order), then
        // the program classes and the run-unit entry wrapper. A class-only/interface-only compilation unit is
        // legal (§10.6) — its module emits the types and an empty Main.
        foreach (var iface in _ooClasses.Interfaces)
            OoEmitInterfaceUnit(iface, w);
        foreach (var cls in classes)
            OoEmitClassUnit(cls, w);
        if (units.Count == 0)
        {
            using (w.Block("internal static class Program"))
            using (w.Block("private static void Main()")) { }
            return w.ToString();
        }

        foreach (var unit in units)
            if (unit.Parent is null && !unit.IsPrototype)   // a prototype has no body (§10.6.2 SR4f) — no class
                CallEmitProgramClass(unit, w);
        CallEmitEntryWrapper(units, w, anyFiles);
        return w.ToString();
    }

    /// <summary>Flatten the compilation group into the ordered unit lists — top-level program units in source
    /// order, each followed by its contained programs (containers precede containees; load-bearing for GLOBAL
    /// inheritance), plus the group's CLASS-ID units (the Phase-3 OO spine). The pass-1 class symbol table
    /// (deep-dive D1) is built HERE — before ANY unit binds — so a driver's typed object references and INVOKEs
    /// resolve classes defined later in the file. A contained <c>nestedProgram</c> parse context is re-shaped
    /// into a synthetic <c>programUnit</c> context (identical child shape) so the per-unit binders consume one
    /// context type.</summary>
    private (List<CallUnit> Programs, List<OoClassUnit> Classes) CallCollectUnits(
        Core.CompilationUnitContext tree, EditionContext edition)
    {
        var all = new List<CallUnit>();
        var usedClassNames = new HashSet<string>(StringComparer.Ordinal);
        var classDefs = new List<Core.ClassDefinitionContext>();
        var ifaceDefs = new List<Core.InterfaceDefinitionContext>();

        foreach (var group in tree.compilationGroup())
        {
            classDefs.AddRange(group.classDefinition());
            ifaceDefs.AddRange(group.interfaceDefinition());   // §11.6 — collected, NEVER silently dropped (the W2 rule)
            foreach (var pu in group.programUnit())
                Collect(pu, null);
        }
        _ooClasses = OoClassTable.Build(classDefs, edition, ifaceDefs);
        var classes = _ooClasses.Classes.Select(sym => new OoClassUnit { Symbol = sym }).ToList();
        return (all, classes);

        void Collect(Core.ProgramUnitContext ctx, CallUnit? parent)
        {
            var unit = CallMakeUnit(ctx, parent, all.Count, usedClassNames, edition);
            all.Add(unit);
            parent?.Children.Add(unit);
            foreach (var nested in ctx.nestedProgram())
                Collect(CallReparent(nested), unit);
        }
    }

    /// <summary>Build one <see cref="CallUnit"/> from a program unit's IDENTIFICATION DIVISION: the program name
    /// (PROGRAM-ID / FUNCTION-ID; the <c>AS literal</c> externalized name wins, ISO §11.10.4 GR1) and the
    /// COMMON / INITIAL / RECURSIVE attributes with their per-edition + placement gates (§11.10.3 SR4–6).</summary>
    private static CallUnit CallMakeUnit(
        Core.ProgramUnitContext ctx, CallUnit? parent, int index, HashSet<string> usedClassNames, EditionContext edition)
    {
        var idBody = ctx.identificationDivision()?.identificationBody();
        var pid = idBody?.programIdParagraph();
        var fid = idBody?.functionIdParagraph();
        string name = pid?.programName()?.GetText()
            ?? fid?.programName()?.GetText()
            ?? $"PROGRAM{index}";
        bool isFunction = pid is null && fid is not null;
        bool isPrototype = fid?.PROTOTYPE() is not null;   // §11.5 Format 2 — a signature-only prototype unit (M2-UDF-3)
        bool initial = false, common = false, recursive = false;
        foreach (var attr in pid?.programIdAttributes()?.programIdAttribute() ?? [])
        {
            var cpa = attr.commonProgramAttribute();
            if (cpa?.INITIAL_() is not null) initial = true;
            else if (cpa?.COMMON() is not null) common = true;
            else if (cpa?.RECURSIVE() is not null) recursive = true;
            else if (attr.literalAttribute()?.STRINGLIT() is { } asLit
                     && DecodeCobolString(asLit.GetText()) is { Length: > 0 } asName)
                name = asName;   // PROGRAM-ID name AS "literal" — the externalized name (ISO §11.10.4 GR1)
        }

        if (recursive && edition.DialectLevel < 2002)
            edition.Error("COBOLNET0885",
                "PROGRAM-ID … RECURSIVE was introduced by ISO/IEC 1989:2002 (§11.10) — requires --std 2002 or "
                + $"later (targeting COBOL-{edition.DialectLevel})");
        if (initial && recursive)
            edition.Error("COBOLNET0886",
                $"program '{name}': INITIAL and RECURSIVE are mutually exclusive (ISO §11.10.3 SR5–6)");
        if (common && parent is null)
            edition.Error("COBOLNET0887",
                $"program '{name}': COMMON may be specified only in a CONTAINED program (ISO §11.10.3 SR4)");

        // §9.4 (:12529): "a user defined function always possesses the recursive attribute and may call
        // itself" — implicit, never the explicit PROGRAM-ID attribute, so it rides AFTER the 0885/0886 gates.
        if (isFunction) recursive = true;

        string baseName = "_PRG_" + DataItem.Sanitize(name).ToUpperInvariant();
        string className = baseName;
        for (int n = 2; !usedClassNames.Add(className); n++) className = $"{baseName}_{n}";
        return new CallUnit
        {
            Name = name, ClassName = className, Ctx = ctx,
            Parent = parent, Initial = initial, Common = common, Recursive = recursive,
            IsFunction = isFunction, IsPrototype = isPrototype,
        };
    }

    /// <summary>Re-shape a <c>nestedProgram</c> context into a synthetic <c>programUnit</c> context by adopting
    /// its children (the two rules have the identical child sequence — the generated <c>dataDivision()</c> /
    /// <c>procedureDivision()</c> accessors scan DIRECT children only, so each unit binds exactly its own
    /// subtree, never a containee's — the IC235A nested-scoping lesson).</summary>
    private static Core.ProgramUnitContext CallReparent(Core.NestedProgramContext nested)
    {
        var unit = new Core.ProgramUnitContext(null!, -1);
        for (int i = 0; i < nested.ChildCount; i++)
            switch (nested.GetChild(i))
            {
                case ParserRuleContext rc: unit.AddChild(rc); break;
                case ITerminalNode t: unit.AddChild(t); break;
            }
        return unit;
    }

    /// <summary>The DATA half of unit binding (phase 1 of the two-phase bind): the unit's DATA DIVISION on a
    /// per-unit <see cref="DataBinder"/> with a disjoint uid band (so nested-class struct/profile names never
    /// shadow a container's), then inject the containers' GLOBAL names (ISO §13.18.27 GR1–2 — nearest container
    /// first, a local name shadows) and record the <c>ref</c>-bridges the nested class needs to reach the
    /// container's storage. Every unit passes through here BEFORE any unit's procedure binds
    /// (<see cref="CallBindUnitProcedure"/>) — the forward-reference enabler for user-function signatures.</summary>
    private void CallBindUnitData(CallUnit unit, EditionContext edition)
    {
        var data = new DataBinder(edition) { OoClasses = _ooClasses };
        data.CallSeedUids(_callUidBand);
        _callUidBand += 100_000;

        // Pre-seed inherited GLOBAL-table index names BEFORE Bind: the child's own INDEXED BY registrations then
        // allocate from a later ordinal and can never collide with a bridged container index field. The seeded
        // fields are SUPPRESSED from this unit's field emission — a global index-name is SHARED storage
        // (ISO §13.18.27 GR2), reached through the ref-bridge, never re-declared locally.
        for (var anc = unit.Parent; anc is not null; anc = anc.Parent)
            foreach (var g in anc.Data.CallGlobalRoots)
                foreach (string idxName in CallIndexNamesUnder(g))
                    if (anc.Data.IndexFields.TryGetValue(idxName, out string? field) && data.IndexFields.TryAdd(idxName, field))
                        data.CallSuppressedRootFields.Add(field);

        data.Bind(unit.Ctx);
        unit.Data = data;

        // GLOBAL FD inheritance (ISO §13.18.30: the file-name of a GLOBAL FD is a GLOBAL name, visible in every
        // directly/indirectly contained program; §13.18.27 GR1–2 — nearest container first, a local declaration
        // shadows, which TryAdd realizes since local files are already present). Merge into FilesByName ONLY —
        // never Files: the child must not re-register, re-qualify, or CANCEL-close the owner's connector; its
        // bound verbs hold the SHARED FileModel reference, so the owner's one-time PROG::FILE qualification
        // automatically keys the child's verbs to the owner's connector. (EXTERNAL is NOT global — §13.18.22
        // NOTE 1: an EXTERNAL non-GLOBAL FD's name is not visible in contained programs.) The record-name half
        // of §13.18.30 rides the standard GLOBAL-root bridges (DataBinder.CallBindExternalAndGlobal adds a
        // GLOBAL FD's records to CallGlobalRoots).
        for (var anc = unit.Parent; anc is not null; anc = anc.Parent)
            foreach (var f in anc.Data.Files)
                if (f.IsGlobal)
                    data.FilesByName.TryAdd(f.CobolName, f);

        // Configuration-section inheritance (ISO §12.3.4 GR1: "the entries explicitly or implicitly
        // specified in the configuration section of a source unit that contains other source units apply to
        // each directly or indirectly contained source unit"; §12.3.3 SR1 — a contained program cannot have
        // its own): the containers' REPOSITORY user-function specifiers apply here, so a contained program's
        // FUNCTION reference resolves (the M2-UDF-1 review finding).
        for (var anc = unit.Parent; anc is not null; anc = anc.Parent)
        {
            data.UserFunctionNames.UnionWith(anc.Data.UserFunctionNames);
            data.RepositoryIntrinsics.UnionWith(anc.Data.RepositoryIntrinsics);   // §12.3.4 GR1 — the intrinsic keyword-omission specifiers inherit too (M2-UDF-4)
            if (anc.Data.RepositoryAllIntrinsic) data.RepositoryAllIntrinsic = true;
        }

        int depth = 0;
        for (var anc = unit.Parent; anc is not null; anc = anc.Parent)
        {
            depth++;
            string outer = string.Concat(Enumerable.Repeat("__outer.", depth));
            foreach (var g in anc.Data.CallGlobalRoots)
            {
                if (g.CobolName is null) continue;
                if (data.ByName.ContainsKey(g.CobolName)) continue;   // local (or nearer-container) name shadows (§13.18.27 GR2)
                CallRegisterSubtree(data, g);
                foreach (var (condName, conds) in anc.Data.Conditions)
                    foreach (var cond in conds)
                        if (CallIsUnder(cond.Parent, g))
                        {
                            if (!data.Conditions.TryGetValue(condName, out var list)) data.Conditions[condName] = list = [];
                            list.Add(cond);
                        }
                if (g.Class is { Tier: RedefinesTier.StringCanonical } cls)
                    unit.Bridges.Add(new CallBridge(cls.BackingCsName, outer + cls.BackingCsName, "backing", null));
                else
                    unit.Bridges.Add(new CallBridge(g.CsName, outer + g.CsName, "field", g));
                foreach (string idxName in CallIndexNamesUnder(g))
                    if (anc.Data.IndexFields.TryGetValue(idxName, out string? field))
                        unit.Bridges.Add(new CallBridge(field, outer + field, "index", null));
            }
        }

        unit.Refs = new ReferenceResolver(data);
    }

    /// <summary>The PROCEDURE half of unit binding (phase 2): every unit's DATA is already bound
    /// (<see cref="CallBindUnitData"/>) and the group's user-function signature table is built, so a
    /// <c>FUNCTION user-name(args)</c> reference resolves its callee's RETURNING/USING descriptions
    /// regardless of unit order in the source (§8.4.3.2.4 GR1).</summary>
    private void CallBindUnitProcedure(CallUnit unit, IReadOnlyDictionary<string, UserFunctionSignature> userFunctions)
    {
        var data = unit.Data;
        var binder = new StatementBinder(data, unit.Refs)
        {
            OoClasses = _ooClasses,
            UserFunctions = userFunctions,
            // §8.4.6.6 — inside a function definition its OWN name is a referable function-prototype-name
            // (self-recursion without a repository entry; §12.3.8 GR11 makes a present self-entry a no-op).
            UdfSelfName = unit.IsFunction ? unit.Name : null,
            // §15.65.3 argument rule 1 — MODULE-NAME NESTED requires a contained program.
            InNestedProgram = unit.Parent is not null,
        };
        binder.ConfigureEc(_turnState, unit.Name);   // the EC bind context (TURN fold + §15.30 location element)
        unit.Bound = binder.Bind(unit.Ctx);

        // Resolve every boundary-copied formal (and the RETURNING item) ONCE during the bind phase: resolving a
        // GROUP registers it as whole-group-referenced, so the later MarkStoreAsImage pass flips its
        // numeric-DISPLAY leaves to image storage BEFORE any field emission — the formal's FromImage/AsImage
        // round trip then type-checks (ISO §14.9 MOVE GR4; COBOLNET_DESIGN §14.4).
        foreach (var f in data.LinkageFormals)
            if (!f.CarrierResident)
                unit.Refs.ResolveItem(f.Item);
        if (data.LinkageReturning is { } returning)
            unit.Refs.ResolveItem(returning);
    }

    /// <summary>Build the compilation group's user-function signature table (name → bound RETURNING item +
    /// USING formals), between the DATA and PROCEDURE bind phases: FUNCTION-ID units only (ISO §9.4 — the
    /// binder's function namespace never sees PROGRAM-ID units; §8.4.6.6 scope of function-prototype-names).
    /// The §14.2 procedure-division-header rule "The RETURNING phrase shall be specified in a function
    /// definition" (:23666) is checked HERE, once per unit — even an uncalled function without RETURNING is
    /// ill-formed.</summary>
    private static Dictionary<string, UserFunctionSignature> CallBuildUserFunctionTable(
        List<CallUnit> units, EditionContext edition)
    {
        // Partition the group's FUNCTION-ID units by name into DEFINITIONS (a real body) and PROTOTYPES
        // (signature-only, §11.5 Format 2). A prototype precedes all other units (§10.6.2 SR1), so a naive
        // first-wins TryAdd would false-report the FOLLOWING same-name definition as a duplicate (1508) — the
        // partition prevents that. Every function unit must carry a RETURNING (§14.2 :23666) — checked once here.
        var defs = new Dictionary<string, CallUnit>(StringComparer.OrdinalIgnoreCase);
        var protos = new Dictionary<string, CallUnit>(StringComparer.OrdinalIgnoreCase);
        foreach (var u in units)
        {
            if (!u.IsFunction) continue;
            if (u.Data.LinkageReturning is null)
                edition.Error("COBOLNET1507",
                    $"FUNCTION-ID '{u.Name}': the RETURNING phrase shall be specified in a function {(u.IsPrototype ? "prototype" : "definition")} "
                    + "(ISO §14.2, procedure division header) — the function cannot deliver a result without it");
            if (!(u.IsPrototype ? protos : defs).TryAdd(u.Name, u))
                edition.Error("COBOLNET1508",
                    $"duplicate FUNCTION-ID '{u.Name}' in the compilation group — two function {(u.IsPrototype ? "prototypes" : "definitions")} with "
                    + "one name cannot both register in the run unit's activation namespace (ISO §8.4.6.6)");
        }

        // §12.3.8 GR11(a) — an in-group DEFINITION is authoritative over a same-name PROTOTYPE (:14871); a lone
        // prototype supplies the signature for a separately-compiled target (:14875 / §8.4.3.2.4 GR6b :6997).
        var table = new Dictionary<string, UserFunctionSignature>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, u) in defs)
            table[name] = new UserFunctionSignature(name, u.Data.LinkageReturning, u.Data.LinkageFormals);
        foreach (var (name, p) in protos)
        {
            if (defs.TryGetValue(name, out var def))
            {
                // §10.6.2 SR3 — an in-group prototype+definition pair shall have the SAME signature. Light check
                // (argument count; full §8.13 external-repository conformance is staged residue).
                if (p.Data.LinkageFormals.Count != def.Data.LinkageFormals.Count)
                    edition.Error("COBOLNET1513",
                        $"FUNCTION '{name}': the IS PROTOTYPE signature declares {p.Data.LinkageFormals.Count} "
                        + $"argument(s) but the in-group definition declares {def.Data.LinkageFormals.Count} — a "
                        + "function prototype and a same-name definition shall have the same signature (ISO §10.6.2 SR3)");
                continue;   // the definition's signature is authoritative (GR11a)
            }
            table[name] = new UserFunctionSignature(name, p.Data.LinkageReturning, p.Data.LinkageFormals);
        }
        return table;
    }

    private static void CallRegisterSubtree(DataBinder data, DataItem item)
    {
        if (item.CobolName is { } name)
        {
            if (!data.ByName.TryGetValue(name, out var list)) data.ByName[name] = list = [];
            list.Add(item);
        }
        foreach (var child in item.Children) CallRegisterSubtree(data, child);
        foreach (var ren in item.Renames66) CallRegisterSubtree(data, ren);
    }

    private static IEnumerable<string> CallIndexNamesUnder(DataItem root)
    {
        foreach (string n in root.IndexNames) yield return n;
        foreach (var child in root.Children)
            foreach (string n in CallIndexNamesUnder(child)) yield return n;
    }

    private static bool CallIsUnder(DataItem item, DataItem ancestor)
    {
        for (DataItem? n = item; n is not null; n = n.Parent)
            if (ReferenceEquals(n, ancestor)) return true;
        return false;
    }

    // ── Program-class emission (design D3/D4) ───────────────────────────────────────────────────────────────

    /// <summary>Emit one program's instantiable class (design D3 — a static class cannot recurse or hold the
    /// per-activation copies INITIAL/RECURSIVE need; the registry's cached singleton realizes last-used state,
    /// §14.6.2.3.3), its <see cref="ICobolProgram"/> ABI surface, and its contained programs as nested classes.</summary>
    private void CallEmitProgramClass(CallUnit unit, CodeWriter w)
    {
        var data = unit.Data;
        _refs = unit.Refs;
        _ctx = new EmissionContext(w, data);
        _num = new NumericRenderer(_ctx);
        _cond = new ConditionRenderer(_num, _ctx);
        _callSelfPath = unit.Path;
        _callReturningPlace = data.LinkageReturning is { } ret ? _refs.ResolveItem(ret) : null;
        _ecUnitHasF3 = unit.Bound.Declaratives?.Any(d => d.EcEntries is not null) ?? false;   // → __EcDispatch exists
        _ecUnitHasF4 = unit.Bound.Declaratives?.Any(d => d.EoClassCsName is not null) ?? false;   // → __EcObjDispatch exists (EC-OO F4)
        // A containing program with USE … GLOBAL declaratives makes this unit's I-O hooks walk outward on a
        // no-local-match (ISO §14.9.49.4 GR4b) — consumed by EmitDispatcher/EmitUseMachinery.
        _callOuterGlobalUse = CallChainHasGlobalUse(unit.Parent);

        // Inherited GLOBAL files' FILE STATUS routing (§12.4.5.8.4 GR1 NOTE 1 — see the field doc): resolve each
        // ancestor's status item with the ANCESTOR's resolver, then re-anchor the place behind the __outer chain.
        _callInheritedStatusPlace.Clear();
        int statusDepth = 0;
        for (var anc = unit.Parent; anc is not null; anc = anc.Parent)
        {
            statusDepth++;
            string outerPrefix = string.Concat(Enumerable.Repeat("__outer.", statusDepth));
            foreach (var f in anc.Data.Files)
                if (f.IsGlobal && f.FileStatusItem is { } si && !_callInheritedStatusPlace.ContainsKey(f)
                    && anc.Refs.ResolveItem(si) is { } sp && CallPrefixPlace(sp, outerPrefix) is { } pp)
                    _callInheritedStatusPlace[f] = pp;
        }

        // Per-formal carrier shape, resolved once: a carrier-resident formal aliases per access; a group /
        // redefined formal round-trips its character image at the activation boundary (deep-dive hard problem —
        // the whole-struct round trip, realized at the call boundary).
        var formals = data.LinkageFormals
            .Select(f =>
            {
                Place? place = f.CarrierResident ? null : _refs.ResolveItem(f.Item);
                bool isNum = f.CarrierResident
                    ? f.Item.Pic is { Category: PicCategory.Numeric, IsFloat: false } && !f.Item.StoreAsImage
                    : place is not null && !CallPlaceIsString(place);
                return (Formal: f, Place: place, IsNum: isNum);
            })
            .ToList();

        using (w.Block($"internal sealed class {unit.ClassName} : ICobolProgram"))
        {
            if (unit.Parent is { } parent)
            {
                w.Line($"private readonly {parent.ClassName} __outer;   // the containing program's instance (GLOBAL storage lives there, ISO §13.18.27)");
                using (w.Block($"public {unit.ClassName}({parent.ClassName} __o)")) w.Line("__outer = __o;");
            }
            w.Line("private bool __asCalled;   // true during a CALL activation — EXIT PROGRAM is CONTINUE otherwise (ISO §14.9.14 GR2)");
            if (data.Files.Count > 0)
                w.Line("private bool __filesRegistered;   // connectors register once per INSTANCE — a canceled/INITIAL program gets fresh connectors (ISO §14.6.2.3.2)");
            if (data.Collating is { } collate)
                w.Line($"private static readonly ushort[] __COLLATE = {{ {string.Join(", ", collate.Positions)} }};");

            foreach (var b in unit.Bridges)
            {
                string type = b.Kind switch
                {
                    "index" => "long",
                    "backing" => "string",
                    _ => b.Item!.Occurs is not null ? b.Item.ElementType + "[]" : b.Item.ElementType,
                };
                w.Line($"private ref {type} {b.Field} => ref {b.Path};   // GLOBAL item of a containing program (ISO §13.18.27 GR2 — container storage, contained visibility)");
            }
            EmitExternalBackings(data, w);
            foreach (var (backing, cellField, canonical, cellWidth) in data.PtrAddressableBackings)
            {
                // The seed is the SAME VALUE-honoring image expression the Tier-B stored backing uses.
                string seed = $"CobolString.Store({new FieldEmitter(_ctx).ImageInitOf(canonical)}, {cellWidth})";
                w.Line($"private readonly StorageCell {cellField} = new StorageCell {{ Ref = {seed} }};   // ADDRESS-OF-taken record — cell storage (ISO §8.4.3.11; Phase-4b inc 2)");
                w.Line($"private ref string {backing} => ref {cellField}.Ref;");
            }
            foreach (var (backing, addrField, width) in data.PtrBasedBridges)
            {
                w.Line($"private ManagedPointer {addrField} = ManagedPointer.Null;   // implicit data-address pointer (ISO §13.18.5 GR2 — initially NULL)");
                w.Line($"private ref string {backing} => ref CobolPtr.Deref({addrField}, {width}).Ref;   // BASED deref bridge (GR3/GR4 loud)");
            }

            new FieldEmitter(_ctx).Emit();

            foreach (var (f, _, isNum) in formals)
            {
                string init = isNum ? "ManagedPointer<long>.Cell(0L)"
                    : $"ManagedPointer<string>.Cell(new string(' ', {Math.Max(1, f.Item.ImageWidth)}))";
                w.Line($"private ManagedPointer<{(isNum ? "long" : "string")}> {f.CarrierField} = {init};   "
                    + $"// LINKAGE formal #{f.Position + 1} — the caller-storage carrier (ISO §13.7.1; design D1)");
            }
            w.Line();

            CallEmitCallMethod(unit, formals, w);
            w.Line("void ICobolProgram.Activate() => __Activate();");
            using (w.Block("public void CloseFiles()"))   // CANCEL §14.9.5 GR9 / run-unit close §14.6.11
                foreach (var file in data.Files)
                    if (!file.IsExternal)   // CANCEL closes INTERNAL connectors only (§14.9.5 GR9); an EXTERNAL connector persists (GR8 / §13.18.22.4 GR4a)
                        w.Line($"CobolFile.Close({FileKeyExpr(file)});");
            if (unit.Children.Count > 0 && CallChainHasGlobalUse(unit))
                CallEmitRunGlobalUse(unit, w);
            w.Line();

            if (unit.Bound.Paragraphs.Count > 0)
                EmitDispatcher(unit.Bound, w);
            else
                using (w.Block("public void __Activate()")) { }

            foreach (var child in unit.Children)
                CallEmitProgramClass(child, w);
        }
    }

    /// <summary>Re-anchor a CONTAINER-resolved place behind the contained class's <c>__outer</c> instance chain
    /// (the §12.4.5.8.4 GR1 NOTE 1 status routing). A FILE STATUS item is never subscripted (§12.4.5.8 SR1 — no
    /// OCCURS), so its member path / Tier-B backing prefix textually. An unexpected place shape returns null —
    /// the caller then falls back to the loud-guard path, never a silent wrong-storage store (§1.4).</summary>
    private static Place? CallPrefixPlace(Place p, string prefix) => p switch
    {
        MemberPlace m => new MemberPlace(prefix + m.Path, m.MemberItem),
        RedefViewPlace r => new RedefViewPlace(prefix + r.Backing, r.OffsetExpr, r.Width, r.ViewItem),
        _ => null,
    };

    /// <summary>True when <paramref name="u"/> or any of its containers declares a <c>USE … GLOBAL</c>
    /// declarative (ISO §14.9.49.4 GR4b — the containment chain a contained program's I-O check walks outward).</summary>
    private static bool CallChainHasGlobalUse(CallUnit? u)
    {
        for (; u is not null; u = u.Parent)
            if (u.Bound.Declaratives is { } ds && ds.Any(d => d.Global)) return true;
        return false;
    }

    /// <summary>Emit the cross-program GLOBAL USE dispatch member (ISO §14.9.49.4 GR4b): a contained program's
    /// <c>__IoCheck</c> fallthrough (no local match, GR4a) calls the container instance's
    /// <c>__RunGlobalUse</c>, which examines THIS program's <c>USE … GLOBAL</c> declaratives — file-name scope
    /// before open-mode scope (GR5) — and on a match runs the handler in THIS instance (the declaring program's
    /// data, §8.4.6.2); otherwise the walk continues to the next container ("repeated with the next higher
    /// directly containing source element", GR4b) or stops false at the outermost. Emitted only on classes a
    /// contained program can actually reach (children exist + the chain has GLOBAL declaratives), so a
    /// declarative-free compilation group's generated source is unchanged.</summary>
    private void CallEmitRunGlobalUse(CallUnit unit, CodeWriter w)
    {
        var decls = unit.Bound.Declaratives ?? [];
        using (w.Block("public bool __RunGlobalUse(string __f)"))
        {
            if (decls.Any(d => d.Global && d.Files.Count > 0))
                using (w.Block("switch (__f)"))   // GLOBAL file-name scope first (GR5)
                {
                    for (int i = 0; i < decls.Count; i++)
                        if (decls[i].Global)
                            foreach (var f in decls[i].Files)
                                w.Line($"case {FileKeyExpr(f)}: __RunUse({i}, {decls[i].StartPc}, {decls[i].HandlerEndPc}); return true;");
                }
            if (decls.Any(d => d.Global && d.ModeIndex is not null))
                using (w.Block("switch (CobolFile.OpenModeOf(__f))"))   // GLOBAL open-mode scope (GR3b/GR6b–e)
                {
                    for (int i = 0; i < decls.Count; i++)
                        if (decls[i].Global && decls[i].ModeIndex is { } m)
                            w.Line($"case {m}: __RunUse({i}, {decls[i].StartPc}, {decls[i].HandlerEndPc}); return true;");
                }
            w.Line(unit.Parent is { } p && CallChainHasGlobalUse(p)
                ? "return __outer.__RunGlobalUse(__f);   // continue outward (§14.9.49.4 GR4b)"
                : "return false;   // outermost source element reached — no qualifying GLOBAL declarative (GR4b)");
        }
        w.Line();
    }

    /// <summary>Emit the opaque-ABI <c>Call</c> body: positional formal mapping (ISO §14.2.3 GR2), the
    /// activation, boundary copy-out for image formals, and RETURNING delivery (GR7).</summary>
    private void CallEmitCallMethod(
        CallUnit unit, List<(LinkageFormal Formal, Place? Place, bool IsNum)> formals, CodeWriter w)
    {
        using (w.Block("public void Call(CobolArg[] __args, ManagedPointer? __ret)"))
        {
            foreach (var (f, place, isNum) in formals)
            {
                if (f.CarrierResident)
                {
                    // Per-access aliasing of the caller's storage (§14.2.3 GR8): every reference to the formal
                    // reads/writes through this carrier (its CsName IS `__lnkpN.Value`).
                    w.Line(isNum
                        ? $"{f.CarrierField} = CobolArgAdapt.Num(__args, {f.Position}, {f.Item.ProfileName}, {f.Item.Pic!.Scale});"
                        : $"{f.CarrierField} = CobolArgAdapt.Text(__args, {f.Position}, {Math.Max(1, f.Item.Pic!.Length)});");
                    continue;
                }
                // Boundary round-trip formal (group / redefined): adopt the carrier, copy the caller's image in.
                w.Line(isNum
                    ? $"{f.CarrierField} = CobolArgAdapt.Num(__args, {f.Position}, {f.Item.ProfileName}, {f.Item.Pic!.Scale});"
                    : $"{f.CarrierField} = CobolArgAdapt.Text(__args, {f.Position}, {Math.Max(1, f.Item.ImageWidth)});");
                using (w.Block($"if (CobolArgAdapt.Present(__args, {f.Position}))"))
                {
                    if (place is null)
                        w.Line(LoudStmt($"LINKAGE formal '{f.Item.CobolName}' is not resolvable to storage"));
                    else if (!isNum)
                        w.Line(CallStringWrite(place, $"{f.CarrierField}.Value"));
                    else
                        w.Line(place.Write($"{f.CarrierField}.Value"));
                }
            }
            w.Line("__asCalled = true;");
            w.Line("try { __Activate(); } finally { __asCalled = false; }");
            foreach (var (f, place, isNum) in formals)
            {
                if (f.CarrierResident || place is null) continue;
                // Copy the (possibly mutated) formal back to the caller's storage — the BY REFERENCE result
                // becomes visible at activation end (§14.2.3 GR8/GR9; a BY CONTENT cell absorbs it invisibly).
                using (w.Block($"if (CobolArgAdapt.Present(__args, {f.Position}))"))
                    w.Line(isNum
                        ? $"{f.CarrierField}.Value = {place.Read()};"
                        : $"{f.CarrierField}.Value = {CallStringRead(place)};");
            }
            if (_callReturningPlace is { } ret)
                w.Line(CallPlaceIsString(ret)
                    ? $"CobolArgAdapt.StoreReturn(__ret, {CallStringRead(ret)});"
                    : $"CobolArgAdapt.StoreReturn(__ret, {ret.Read()});");
        }
    }

    /// <summary>Emit the module registrar + the run-unit entry wrapper. <c>__CobolModule</c> is the ONE public,
    /// well-known discovery surface of a compiled module (deep-dive D2; the generated program classes are
    /// internal): its <c>Register()</c> registers every program unit (containers before containees), serving
    /// both the own-run-unit <c>Main</c> AND a CALLing run unit's sibling-assembly probe
    /// (<c>ProgramRegistry.ResolveVisible</c> rule-4 fallthrough — the implementor-defined §14.9.4.4 GR3b
    /// locate step; §14.6.1: a run unit contains one or more runtime modules). <c>Main</c> runs the first
    /// program as main and performs the §14.6.11 implicit CLOSE at run-unit termination; STOP RUN unwinds to
    /// here (§14.9.43); a main-program GOBACK already returned normally through its activation entry.</summary>
    private void CallEmitEntryWrapper(IReadOnlyList<CallUnit> units, CodeWriter w, bool anyFiles)
    {
        using (w.Block("public static class __CobolModule"))
        using (w.Block("public static void Register()"))
            foreach (var u in units)
            {
                if (u.IsPrototype) continue;   // a prototype registers no runtime module — the separately-compiled definition does (§10.6.3 GR1)
                string parentPath = u.Parent is { } p ? CsLiteral(p.Path) : "null";
                string factory = u.Parent is { } pp
                    ? $"static __o => new {u.ClassRef}(({pp.ClassRef})__o!)"
                    : $"static __o => new {u.ClassRef}()";
                w.Line($"ProgramRegistry.Register({CsLiteral(u.Path)}, {CsLiteral(u.Name)}, {parentPath}, "
                    + $"{CallBool(u.Initial)}, {CallBool(u.Common)}, {CallBool(u.Recursive)}, {factory});");
            }
        w.Line();
        // The run-unit main is the first top-level PROGRAM unit (§8.3.1). A prototype precedes every other unit
        // (§10.6.2 SR1), so units[0] may be a prototype; a function/prototype-only module (a callable library —
        // the cross-assembly UDF-3 target) has no main and only exposes Register() for the sibling probe.
        var mainUnit = units.FirstOrDefault(u => u.Parent is null && !u.IsFunction);
        using (w.Block("internal static class Program"))
        using (w.Block("private static void Main()"))
        {
            w.Line("ProgramRegistry.Reset();");
            if (anyFiles) w.Line("CobolFile.Init();");
            w.Line("__CobolModule.Register();");
            if (mainUnit is not null)
            {
                w.Line($"try {{ ProgramRegistry.RunMain({CsLiteral(mainUnit.Path)}); }}");
                w.Line("catch (StopRun) { }");
                if (_ecActive)
                    // The fatal-EC default (ISO §14.6.13.1.3 #7 → §14.6.12 abnormal run-unit termination; the settled
                    // SSOT §18.16 implementor choice): diagnostic on stderr + NONZERO exit. The finally's CloseAll is
                    // the §14.6.11 attempt-normal-termination step.
                    w.Line("catch (CobolFatalException __fx) { Console.Error.WriteLine(\"abnormal run-unit termination: \" + __fx.Message); Environment.ExitCode = 1; }");
                if (anyFiles)
                    w.Line("finally { CobolFile.CloseAll(); }   // run-unit termination implicit CLOSE (ISO §14.6.11)");
            }
        }
    }

    private static string CallBool(bool b) => b ? "true" : "false";

    // ── Statement emitters: CALL / CANCEL / GOBACK ──────────────────────────────────────────────────────────

    /// <summary>Emit one CALL (ISO §14.9.4.4). With no exception phrase, a CALL failure (not found / recursive
    /// re-entry) propagates and terminates the run unit loudly (the 85 abnormal-termination surface; the
    /// EC-PROGRAM model is the §11 subsystem). With a phrase, the failure runs the ON imperative and control
    /// falls to the end of the CALL (GR3h); NOT ON runs only on a successful return (GR3i).</summary>
    private bool CallEmitCall(BoundCallProgram c)
    {
        var w = _ctx.Writer;
        string nameExpr = c.LiteralName is { } literal
            ? CsLiteral(literal)
            : $"({OperandText.AsString(c.DynamicName!)}).Trim()";   // GR3b — the identifier's value at CALL time (GR3a: read once)
        string args = c.Args.Count == 0
            ? "System.Array.Empty<CobolArg>()"
            : $"new CobolArg[] {{ {string.Join(", ", c.Args.Select(CallArgText))} }}";
        string ret = c.Returning is { } rp ? CallRefCarrier(rp) : "null";
        // An EC-active group's CALL site consumes a callee-staged RAISING propagation itself (the pickup below
        // runs the §14.9.49 F3 selection and honors RESUME); the registry's boundary default stands down.
        string invocation = $"ProgramRegistry.CallProgram({nameExpr}, {CsLiteral(_callSelfPath)}, {args}, {ret}"
            + $"{(_ecActive ? ", siteHandlesPropagation: true" : "")}"
            + $"{(c.IsFunction ? ", notFoundEc: \"EC-FUNCTION-NOT-FOUND\"" : "")});";   // §8.4.3.2.4 GR6b — a UDF locate miss is EC-FUNCTION-NOT-FOUND

        var ecProg = EcEnabledProgramNames();
        bool hasPhrase = c.OnException is not null || c.NotOnException is not null;
        if (!hasPhrase && ecProg.Count == 0)
        {
            w.Line(invocation);
            CallEmitPropagationPickup();
            return false;
        }
        int id = _callCounter++;
        if (hasPhrase) w.Line($"bool __callErr{id} = false;");
        using (w.Block("try"))
            w.Line(invocation);
        if (ecProg.Count > 0)
            CallEmitProgramEcCatch(ecProg, hasPhrase, hasPhrase ? $"__callErr{id}" : null);
        if (hasPhrase)
            w.Line($"catch (CobolCallException) {{ __callErr{id} = true; }}   // CALL exception condition → the ON phrase (ISO §14.9.4.4 GR3h)");
        if (c.OnException is { } on)
        {
            using (w.Block($"if (__callErr{id})")) EmitStatementList(on);
            if (c.NotOnException is { } notAlso)
                using (w.Block("else")) EmitStatementList(notAlso);
        }
        else if (c.NotOnException is { } not)
            using (w.Block($"if (!__callErr{id})")) EmitStatementList(not);   // GR3i — only on a non-exception return
        CallEmitPropagationPickup();
        return false;
    }

    /// <summary>The enabled EC-PROGRAM-* names of the current statement (empty when none / no wrapper).</summary>
    private List<string> EcEnabledProgramNames() =>
        _ecInfo?.Enabled.Where(p => p.Ec.StartsWith("EC-PROGRAM-", StringComparison.Ordinal)).Select(p => p.Ec).ToList()
        ?? [];

    /// <summary>Emit the name-filtered <c>catch (CobolCallException)</c> arm of a CALL/CANCEL under enabled
    /// EC-PROGRAM checking (§9.1.13-style bridge for the inter-program family: the runtime latched the Table 13
    /// level-3 name in <see cref="CobolCallException.EcName"/>): set the last exception status (§14.6.13.1.1),
    /// then either flag the statement's own ON EXCEPTION phrase (it wins — §14.6.13.1.3 #1 / §14.9.4.4 GR3h) or
    /// run the §14.9.49 F3 selection with the fatal default (every EC-PROGRAM-* is fatal, Table 13). A
    /// CobolCallException whose name is NOT enabled falls through to the next catch arm / propagates — the
    /// checking-off behavior unchanged.</summary>
    private void CallEmitProgramEcCatch(List<string> ecProg, bool hasPhrase, string? phraseFlag)
    {
        var w = _ctx.Writer;
        int id = _ecCounter++;
        string nameTest = string.Join(" || ", ecProg.Select(n => $"__ce{id}.EcName == {CsLiteral(n)}"));
        var (stmt, loc) = EcStmtLoc(_ecInfo!);
        using (w.Block($"catch (CobolCallException __ce{id}) when ({nameTest})"))
        {
            w.Line($"ExceptionState.Set(__ce{id}.EcName, true, {stmt}, {loc});   // §14.6.13.1.1 — all EC-PROGRAM-* are fatal (Table 13)");
            if (hasPhrase)
                w.Line($"{phraseFlag} = true;   // the statement's ON EXCEPTION phrase handles it (§14.6.13.1.3 #1; §14.9.4.4 GR3h)");
            else
            {
                w.Line($"int __r{id} = {EcDispatchExpr($"__ce{id}.EcName", "\"\"")};");
                w.Line($"if (__r{id} >= 0) {{ __pc = __r{id}; break; }}   // RESUME AT procedure-name (§14.9.33.4 GR3)");
                w.Line($"if (__r{id} != -2) throw new CobolFatalException(__ce{id}.EcName, __ce{id}.Message);   // §14.6.13.1.3 #5/#7");
            }
        }
    }

    /// <summary>Emit the activator-side pickup of a callee-staged <c>GOBACK / EXIT PROGRAM … RAISING</c>
    /// exception condition (ISO §14.9.18 GR — raised "as if a RAISE statement" at the end of the activating
    /// statement; §14.6.13.1.3 #6): run the §14.9.49 F3 selection over the DYNAMIC name, honor RESUME, and apply
    /// the fatal default. Emitted only when the group uses the EC model (<c>_ecActive</c>) — the propagated name
    /// is dynamic (RAISING LAST EXCEPTION), so the gate is the group's EC participation, not a per-name TURN
    /// fold (the documented refinement, recorded in the deep-dive; an EC-free caller gets the registry's
    /// boundary default instead).</summary>
    private void CallEmitPropagationPickup()
    {
        if (!_ecActive) return;
        var w = _ctx.Writer;
        int id = _ecCounter++;
        using (w.Block($"if (ExceptionState.TakePropagatedObject(out var __po{id}))   // §14.6.13.1.5 — an exception OBJECT propagated"))
        {
            w.Line($"ExceptionState.SetObject(__po{id});   // GR1b2 — the current exception object HERE (the activator)");
            w.Line($"int __or{id} = {EcObjDispatchExpr($"__po{id}")};   // rule 2 — USE AFTER EXCEPTION OBJECT (GR14)");
            w.Line($"if (__or{id} >= 0) {{ __pc = __or{id}; break; }}   // RESUME AT procedure-name");
            using (w.Block($"if (__or{id} == -3)   // rule 3 PROPAGATE ON: directive not implemented (residue); rule 4 —"))
            {
                w.Line("ExceptionState.Set(\"EC-OO-EXCEPTION\", true);   // as if EXCEPTION EC-OO-EXCEPTION (:24608)");
                w.Line($"int __oq{id} = {EcDispatchExpr("\"EC-OO-EXCEPTION\"", "\"\"")};   // the name enters the F3 tiers");
                w.Line($"if (__oq{id} >= 0) {{ __pc = __oq{id}; break; }}");
                w.Line($"if (__oq{id} != -2) throw new CobolFatalException(\"EC-OO-EXCEPTION\", "
                    + "\"an exception object was not handled (ISO 14.6.13.1.5; Table 13 - fatal)\");");
            }
            w.Line("// -1/-2: declarative completed / RESUME NEXT — normal continuation (:24604)");
        }
        using (w.Block($"if (ExceptionState.TakePropagated(out var __pn{id}, out var __pf{id}))   // §14.9.18 GR — raised at the end of the CALL"))
        {
            w.Line($"int __pr{id} = {EcDispatchExpr($"__pn{id}", "\"\"")};");
            w.Line($"if (__pr{id} >= 0) {{ __pc = __pr{id}; break; }}   // RESUME AT procedure-name (§14.9.33.4 GR3)");
            w.Line($"if (__pr{id} != -2 && __pf{id}) throw new CobolFatalException(__pn{id}, "
                + "\"exception condition propagated by GOBACK/EXIT PROGRAM RAISING and not resumed "
                + "(ISO 14.9.18; 14.6.13.1.3 #6/#7)\");");
        }
    }

    /// <summary>The C# <c>CobolArg</c> expression for one bound CALL argument (caller side; design D1/D2).
    /// BY REFERENCE builds an accessor carrier over the caller's storage (§14.2.3 GR8); BY CONTENT/BY VALUE
    /// snapshot the value into a cell AT CALL INITIATION — which also realizes the §14.9.4.4 GR3a once-only
    /// evaluation for those modes. (A BY REFERENCE accessor over a SUBSCRIPTED operand re-evaluates the
    /// subscript inside the closure — the GR3a capture-into-locals refinement is a known follow-up.)</summary>
    private string CallArgText(BoundCallArg a)
    {
        if (a.Place is { } p)
        {
            string digits = (p.Pic?.Digits ?? 0).ToString();
            string scale = (p.Pic?.Scale ?? 0).ToString();
            if (p.Item.IsGroup && !p.Item.IsCharacterImage && p is not RedefViewPlace)
                return $"new CobolArg(CobolPassMode.{a.Mode}, ManagedPointer<string>.Cell("
                    + LoudValue("string", $"CALL USING mixed-usage group '{p.Item.CobolName}' with a COMP/binary leaf (Tier-C byte island, deferred)")
                    + "), 0, 0)";
            if (a.Mode == CobolPassMode.Reference)
                return $"new CobolArg(CobolPassMode.Reference, {CallRefCarrier(p)}, {digits}, {scale})";
            // BY CONTENT — "a record … allocated by the activating element" (§14.2.3 GR9): a value snapshot.
            return CallPlaceIsString(p)
                ? $"new CobolArg(CobolPassMode.Content, ManagedPointer<string>.Cell({CallStringRead(p)}), {digits}, {scale})"
                : $"new CobolArg(CobolPassMode.Content, ManagedPointer<long>.Cell({p.Read()}), {digits}, {scale})";
        }
        switch (a.Value)
        {
            case BoundStringLiteral s:
                return $"new CobolArg(CobolPassMode.Content, ManagedPointer<string>.Cell({CsLiteral(s.Value)}), 0, 0)";
            case BoundNumericLiteral n:
            {
                var lit = UnscaledLit(n.Text);
                int digits = n.Text.Count(char.IsAsciiDigit);
                if (digits > 18)
                    return $"new CobolArg(CobolPassMode.Content, ManagedPointer<string>.Cell("
                        + LoudValue("string", $"CALL USING wide numeric literal '{n.Text}' (19+ digits — the Int128 carrier tier)") + "), 0, 0)";
                return $"new CobolArg(CobolPassMode.Content, ManagedPointer<long>.Cell({lit.Expr}), {digits}, {lit.Scale})";
            }
            case BoundComputedOperand expr:
            {
                NumX x = _num.Render(expr.Expr);   // BY VALUE — a converted value copy (§14.2.3 GR10)
                return $"new CobolArg(CobolPassMode.Value, ManagedPointer<long>.Cell((long)({x.Expr})), 18, {x.Scale})";
            }
            case BoundAllLiteral all:
                return $"new CobolArg(CobolPassMode.Content, ManagedPointer<string>.Cell({CsLiteral(all.Literal)}), 0, 0)";
            case BoundFigurative fig:
                return $"new CobolArg(CobolPassMode.Content, ManagedPointer<string>.Cell(new string({_ctx.FigFill(fig.Kind)}, 1)), 0, 0)";
            default:
                return $"new CobolArg(CobolPassMode.Content, ManagedPointer<string>.Cell("
                    + LoudValue("string", "CALL USING argument form") + "), 0, 0)";
        }
    }

    /// <summary>An accessor carrier over a caller place — the BY REFERENCE / RETURNING aliasing form (design D1:
    /// <c>OverField</c> over the native field; a whole group crosses as its character image, distributed back
    /// through <c>FromImage</c> — the deep-dive group round-trip).</summary>
    private string CallRefCarrier(Place p) => CallPlaceIsString(p)
        ? $"ManagedPointer<string>.OverField(() => {CallStringRead(p)}, __v => {{ {CallStringWrite(p, "__v")} }})"
        : $"ManagedPointer<long>.OverField(() => {p.Read()}, __v => {{ {p.Write("__v")} }})";

    /// <summary>True when a place's storage crosses the CALL boundary as a character image (string carrier):
    /// groups, Tier-B windows, zoned-image leaves, alphanumeric / numeric-edited items. A native fixed-point
    /// leaf crosses as its <c>long</c> (fully typed — the common conforming case).</summary>
    private static bool CallPlaceIsString(Place p) =>
        p is RedefViewPlace || p.Item.IsGroup || p.Item.StoreAsImage
        || p.Item.Pic?.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited
            or PicCategory.National or PicCategory.Boolean   // string-stored (D-N1/D-B1): both ABI sides are C# strings, char-correct
        || p.Item.Pic is { IsFloat: true } || p.Item.Pic is { Digits: > 18 };

    /// <summary>The string image a place contributes ACROSS THE CALL BOUNDARY. An occurs-depending group reads
    /// its FULL maximum-allocation image here, never the ODO window: BY REFERENCE "operates as if the [formal]
    /// occupies the same storage area as the argument" (ISO §14.2.3 GR8 — the STORAGE is the maximum allocation)
    /// and a BY CONTENT copy is of the whole record (GR9); the current-extent window of §13.18.38 GR8 is a
    /// SENDING-OPERAND rule for MOVE/compare/INSPECT, not a storage-aliasing rule (IC207A: CALL … USING TABLE-01
    /// with DN3=3 must still carry all 15 character positions in, and carry the callee's full table back out).
    /// Every call site of this helper (the BY REFERENCE carrier, BY CONTENT snapshot, callee copy-out, and
    /// RETURNING delivery) is such a boundary.</summary>
    private static string CallStringRead(Place p) => p is OdoGroupPlace odo
        ? $"{odo.Read()}.AsImage()"
        : OperandText.AsString(new BoundFieldOperand(p));

    private static string CallStringWrite(Place p, string value) =>
        // The boundary WRITE half of the §14.2.3 GR8/GR9 full-allocation rule above: a group (including an
        // occurs-depending group — OdoGroupPlace.Write delegates to the full-width struct) distributes the whole
        // image through FromImage, never the GR8a current-extent splice.
        p.Item.IsGroup && p is not RedefViewPlace && p.Item.IsCharacterImage
            ? $"{p.Read()}.FromImage({value});"
            : p.Write(value);

    /// <summary>Emit CANCEL (ISO §14.9.5): one registry call per target, left to right (GR2). Under enabled
    /// EC-PROGRAM checking (>>TURN, §7.3.25) each target's <see cref="CobolCallException"/> runs the
    /// §14.6.13.1.3 sequence (status, F3 selection, fatal default) instead of crashing raw.</summary>
    private void CallEmitCancel(BoundCancel c)
    {
        var w = _ctx.Writer;
        var ecProg = EcEnabledProgramNames();
        foreach (var (literal, dynamic) in c.Targets)
        {
            string nameExpr = literal is { } l ? CsLiteral(l) : $"({OperandText.AsString(dynamic!)}).Trim()";
            string call = $"ProgramRegistry.Cancel({nameExpr}, {CsLiteral(_callSelfPath)});";
            if (ecProg.Count == 0)
            {
                w.Line(call);
                continue;
            }
            using (w.Block("try"))
                w.Line(call);
            CallEmitProgramEcCatch(ecProg, hasPhrase: false, phraseFlag: null);
        }
    }

    /// <summary>Emit GOBACK (ISO §14.9.18): move the RETURNING source into the header RETURNING item (GR2 — the
    /// activation result), stage a RAISING exception condition for the activator (the EC model — picked up at
    /// the activating CALL site or by the registry's boundary default), then raise <see cref="ProgramReturn"/> —
    /// caught at THIS program's activation entry, returning control to the activator (called program) or ending
    /// the run unit (main program, GR3).</summary>
    private bool CallEmitGoback(BoundGoback g)
    {
        var w = _ctx.Writer;
        if (g.ReturningSource is { } src)
        {
            if (_callReturningPlace is { } ret)
                EmitMove(new BoundMove(new BoundFieldOperand(src), [ret]));
            else
                w.Line(LoudStmt("GOBACK RETURNING without a PROCEDURE DIVISION RETURNING item (ISO §14.9.18 SR)"));
        }
        if (g.Raising is { } r) CallEmitRaisingStage(r, "GOBACK");
        w.Line("throw new ProgramReturn();   // return to the activator; in a main program ≡ STOP (ISO §14.9.18 GR2/GR3)");
        return true;
    }

    /// <summary>Emit EXIT PROGRAM [RAISING …] (ISO §14.9.14 Format 2): GR2 — in a program NOT under the control
    /// of a calling runtime element the statement is CONTINUE and "no exception condition is raised even if the
    /// RAISING phrase is specified", so BOTH the staging and the return are <c>__asCalled</c>-gated; GR3 — in a
    /// called program it returns per the GOBACK rules, staging the RAISING condition for the activator.</summary>
    private void CallEmitExitProgram(BoundExitProgram ep)
    {
        var w = _ctx.Writer;
        if (ep.Raising is null)
        {
            w.Line("if (__asCalled) throw new ProgramReturn();   // ISO §14.9.14 GR2: CONTINUE in a non-called program; GR3: return in a called one");
            return;
        }
        using (w.Block("if (__asCalled)   // GR2 — a non-called program raises nothing, even with RAISING"))
        {
            CallEmitRaisingStage(ep.Raising, "EXIT PROGRAM");
            w.Line("throw new ProgramReturn();   // return to the activator (ISO §14.9.14 GR3)");
        }
    }

    /// <summary>Stage a <c>RAISING</c> phrase's exception condition for re-raise in the ACTIVATOR
    /// (ISO §14.9.18 GR / §14.6.13.1.3 #6 — consumed by the activating CALL site's pickup, or by
    /// <c>ProgramRegistry</c>'s boundary default when the caller is EC-free). The TURN decision was baked in at
    /// bind time (§14.6.13.1.1: a condition is raised only when checking for it is enabled): disabled + nonfatal
    /// stages nothing (§14.6.13.1.4 first sentence); disabled + fatal is the §14.6.13.1.3 #8 implementor
    /// choice — this implementation terminates loudly (mirrors <see cref="EcEmitRaise"/>).</summary>
    private void CallEmitRaisingStage(BoundRaising r, string verb)
    {
        var w = _ctx.Writer;
        if (r.ObjectSource is { } os)
        {
            // The exception-OBJECT leg (§14.9.18.4 GR1b; the EC-OO wave): no Enabled/Fatal logic — objects
            // are not TURN-gated (§7.3.25 takes names only); the activator's §14.6.13.1.5 rules decide.
            w.Line($"ExceptionState.SetPropagatingObject({os.Read()});   // {verb} RAISING identifier-1 — staged for the activator");
            return;
        }
        if (r.IsLast)
        {
            w.Line("ExceptionState.SetPropagatingLast();   // RAISING LAST EXCEPTION (§14.9.18.2 — nothing staged when the status is clear)");
            return;
        }
        if (!r.Enabled)
        {
            if (!r.Fatal)
            {
                w.Line($"// {verb} RAISING {r.EcName}: checking not enabled — nonfatal, not raised (ISO §14.6.13.1.4)");
                return;
            }
            w.Line($"throw new CobolFatalException({CsLiteral(r.EcName!)}, \"raised by {verb} RAISING with checking "
                + "not enabled (ISO 14.6.13.1.3 #8 - implementor-defined; this implementation terminates)\");");
            return;
        }
        w.Line($"ExceptionState.SetPropagating({CsLiteral(r.EcName!)}, {(r.Fatal ? "true" : "false")});   // staged for the activator (§14.9.18 GR)");
    }
}
