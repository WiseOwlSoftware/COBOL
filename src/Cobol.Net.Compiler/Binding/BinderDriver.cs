// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Binding.Passes;
using CobolNet.Common;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// THE Binder phase (rearch PHASE-06 Step 2): binds a whole compilation group to an immutable
/// <see cref="BoundCompilation"/>, so the driver's Phase 2 is literally <c>Bind → VersionConformancePass → Emit</c>
/// and the emitter consumes the bound result instead of orchestrating binding. This class owns the formerly-hidden
/// second pass pipeline (the binder half of the fused <c>CallEmitRunUnit</c>): TurnState → unit/class collection →
/// OO data/body binding → two-phase per-unit binding (ALL data before ANY procedure — the M2-UDF-1
/// forward-reference enabler) → the middle-end data-model passes (compiler-temp re-sync → UsageCollection →
/// image marking → OO harmonize → StorageForm) → the EC gate → file-connector registry-key qualification.
/// <para>The OO orchestration bodies physically remain on <c>CSharpEmitter</c> partials until PHASE 09 and are
/// reached through the <see cref="IOoBindHost"/> seam — they only mutate binder state, never emit.</para>
/// </summary>
internal sealed class BinderDriver
{
    /// <summary>Bind the WHOLE compilation group in <paramref name="tree"/> under the targeted EDITION
    /// (<paramref name="edition"/> — bind-time rejection diagnostics accumulate there; the driver fails the
    /// compile when any exist, BEFORE emit). <paramref name="turnEvents"/> are the frontend's <c>&gt;&gt;TURN</c>
    /// directive events (ISO §7.3.25) — they build the group's compile-time TurnState (EC deep-dive D10);
    /// null/empty means the GR1 default, EC-ALL CHECKING OFF.</summary>
    public BoundCompilation Bind(Core.CompilationUnitContext tree, EditionContext edition,
        IReadOnlyList<Frontend.Preprocessor.TurnEvent>? turnEvents, IOoBindHost oo)
    {
        // The group's compile-time TurnState (ISO §7.3.25; deep-dive D10) — built BEFORE binding so every unit's
        // statement binder folds the same source-ordered directive events (GR6: checking spans the compilation
        // group). Name/edition validation happens here (SR2 + the 2023-only families).
        var turn = TurnState.Build(turnEvents, edition);

        var (units, classes, table) = CollectUnits(tree, edition);
        var session = new BindSession { Turn = turn, OoClasses = table, Edition = edition };
        oo.BeginBind(session);
        foreach (var iface in table.Interfaces) oo.BindInterfaceData(iface);   // prototype formals (§10.6.2 SR4)
        foreach (var cls in classes) oo.BindClassData(cls);   // ALL signatures before ANY body (D1 pass-1)
        table.ValidateOverrideSignatures(edition);            // §9.3.8.2 — after all formals resolve (slice 3a)
        table.ValidateImplements(edition);                    // §9.3.11 via §9.3.8.2.3 (D-I1 — the binder is the authority)
        foreach (var cls in classes) oo.BindClassBody(cls);
        // TWO-PHASE program-unit binding (M2-UDF-1 key enabler): EVERY unit's DATA division binds before ANY
        // unit's procedure body binds, so a function-identifier reference resolves the callee's RETURNING /
        // USING signatures even when the FUNCTION-ID unit FOLLOWS the caller in the compilation group
        // (§8.4.3.2.4 GR1 — the caller-side temporary takes the callee's RETURNING description; the same
        // forward-reference discipline OoClassTable D1 gives typed object references).
        foreach (var unit in units) BindUnitData(unit, session);
        var userFunctions = BuildUserFunctionTable(units, edition);
        foreach (var unit in units) BindUnitProcedure(unit, userFunctions, session);
        // Compiler-temp description re-sync: StoreAsImage is still mutable while procedure bodies bind
        // (a ref-mod store / figurative MOVE in the MODEL's own unit flips it after a temp cloned it — the
        // M2-UDF-1 review's unit-order desync; both sides of the activation boundary must agree on the
        // carrier form). Runs after ALL procedure binds, before the image-marking pass reads the flags.
        foreach (var d in units.Select(u => u.Data)
                     .Concat(classes.SelectMany(c => new[] { c.Data, c.FactoryData })))
            foreach (var (temp, model) in d.CompilerTempClones)
                temp.StoreAsImage = model.StoreAsImage;
        // PHASE-05 Step 5 (DESIGN §2.5 step 9): collect WholeGroupReferenced from the BOUND tree (its whole-group
        // operands) + the program/OO boundary formals — the CORRECT set. Replaces ReferenceResolver's over-inclusive
        // mid-resolve mutation (deleted): the resolver added ANY resolved group (CORR operands, SEARCH tables,
        // qualifier groups, IX keys) that is never a whole-image operand. Runs BEFORE MarkStoreAsImage, which flips
        // each whole-referenced group's numeric-DISPLAY leaves to image storage (§14.9 MOVE GR4). (DEVLOG 752/753.)
        foreach (var cls in classes)
        {
            UsageCollectionPass.Collect(cls.Data, [cls.Bound], OoFormalGroups(cls.Symbol.Methods));
            UsageCollectionPass.Collect(cls.FactoryData, [cls.FactoryBound], OoFormalGroups(cls.Symbol.FactoryMethods));
        }
        foreach (var unit in units) UsageCollectionPass.Collect(unit.Data, [unit.Bound]);

        static IEnumerable<DataItem> OoFormalGroups(IEnumerable<OoMethodSymbol> methods) =>
            methods.SelectMany(m => m.Formals.Select(f => f.Item).Concat(m.Returning is { } r ? [r] : Array.Empty<DataItem>()));

        foreach (var cls in classes) { MarkStoreAsImage(cls.Data); MarkStoreAsImage(cls.FactoryData); }
        foreach (var unit in units) MarkStoreAsImage(unit.Data);
        oo.HarmonizeOverrideCrossings();   // C# override signatures must agree on the crossing form (review find)

        // PHASE-05 Step 2 (D0, prove-then-delete): compute the canonical StorageForm for every item ONCE, HERE —
        // the FINAL post-procedure-bind, post-temp-resync, post-MarkStoreAsImage, post-OO-harmonize state where every
        // StoreAsImage flag is settled. Nothing reads Storage yet; it runs in PARALLEL with the legacy flag and the
        // corpus equivalence assert (StorageFormPass.Verify) proves them equal before any deletion.
        foreach (var cls in classes) { StorageFormPass.Compute(cls.Data); StorageFormPass.Compute(cls.FactoryData); }
        foreach (var unit in units) StorageFormPass.Compute(unit.Data);

        // The group EC gate: ANY use of the EC model (an enabling TURN, a RAISE/RESUME/F3/RAISING, an
        // EXCEPTION-* function) turns the machinery on; otherwise the generated source is byte-identical to a
        // pre-EC build (the zero-scaffolding invariant, SSOT §18.16).
        bool ecActive = turn.AnyEnabled || units.Any(u => u.Bound.Ec is { Any: true })
            || classes.Any(c => c.Bound.Ec is { Any: true } || c.FactoryBound.Ec is { Any: true });

        // Per-program file-connector namespace (moved from the emit half — a BIND-phase model fact, so no CodeGen
        // write into the binding model remains; P6 exit criterion #2): the runtime file registry is
        // run-unit-global, but a file connector is INTERNAL to its program (ISO §8.6.3): two programs declaring
        // the same file-name (the IC-suite PRINT-FILE pattern, e.g. IC101A's two units) must not clobber each
        // other's connectors. Name resolution is done (bound nodes hold FileModel references), so qualifying the
        // runtime key is purely a rename. An EXTERNAL FD instead keys by its run-unit EXTERNALIZED name
        // (ISO §13.18.22.4 GR4a: ONE external file connector per run unit, shared by every describer — two units'
        // FileModels with the same external name converge on ONE registry key, hence one connector; GR5: the name
        // is the FD name). Each FileModel lives in exactly ONE unit's Files list (a fix-E GLOBAL merge shares
        // references through FilesByName only), so no model is renamed twice.
        foreach (var unit in units)
            foreach (var file in unit.Data.Files)
                file.CobolName = file is { IsExternal: true, ExternalName: { } ext }
                    ? "::EXT::" + ext
                    : unit.Path + "::" + file.CobolName;
        // The OO analogue (M2-OO-1i): an OBJECT/FACTORY file connector is scoped to its class, not a program unit,
        // so the program loop above never sees it. A factory file (singleton) keys by class; an instance file keys
        // per object (a minted key held in a __fkey field — see OoQualifyClassFiles); an EXTERNAL class file keys
        // by its run-unit external name, exactly like a program's.
        foreach (var cls in classes) oo.QualifyClassFiles(cls);

        // Declaratives emit the __IoCheck/__RunUse machinery, which reads CobolFile even when the unit declares
        // NO files (IC401M: mode-scoped USE procedures in a file-less flagging program) — the IO using must
        // cover both. A class-only file program (M2-OO-1i — an OBJECT/FACTORY file with no program-unit file)
        // needs it too, or the generated <c>CobolFile.Register</c>/OPEN in the class body has no CobolNet.Runtime.IO
        // import (CS0103).
        bool anyFiles = units.Any(u => u.Data.Files.Count > 0)
            || units.Any(u => u.Bound.Declaratives is { Count: > 0 })
            || classes.Any(c => c.Data.Files.Count > 0 || c.FactoryData.Files.Count > 0);

        return new BoundCompilation(tree, units, classes, table, oo.InterfaceData, turn, ecActive, anyFiles);
    }

    /// <summary>Flatten the compilation group into the ordered unit lists — top-level program units in source
    /// order, each followed by its contained programs (containers precede containees; load-bearing for GLOBAL
    /// inheritance), plus the group's CLASS-ID units (the Phase-3 OO spine). The pass-1 class symbol table
    /// (deep-dive D1) is built HERE — before ANY unit binds — so a driver's typed object references and INVOKEs
    /// resolve classes defined later in the file. A contained <c>nestedProgram</c> parse context is re-shaped
    /// into a synthetic <c>programUnit</c> context (identical child shape) so the per-unit binders consume one
    /// context type.</summary>
    private static (List<BoundUnit> Programs, List<OoClassUnit> Classes, OoClassTable Table) CollectUnits(
        Core.CompilationUnitContext tree, EditionContext edition)
    {
        var all = new List<BoundUnit>();
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
        var table = OoClassTable.Build(classDefs, edition, ifaceDefs);
        var classes = table.Classes.Select(sym => new OoClassUnit { Symbol = sym }).ToList();
        return (all, classes, table);

        void Collect(Core.ProgramUnitContext ctx, BoundUnit? parent)
        {
            var unit = MakeUnit(ctx, parent, all.Count, usedClassNames, edition);
            all.Add(unit);
            parent?.Children.Add(unit);
            foreach (var nested in ctx.nestedProgram())
                Collect(Reparent(nested), unit);
        }
    }

    /// <summary>Build one <see cref="BoundUnit"/> from a program unit's IDENTIFICATION DIVISION: the program name
    /// (PROGRAM-ID / FUNCTION-ID; the <c>AS literal</c> externalized name wins, ISO §11.10.4 GR1) and the
    /// COMMON / INITIAL / RECURSIVE attributes with their per-edition + placement gates (§11.10.3 SR4–6).</summary>
    private static BoundUnit MakeUnit(
        Core.ProgramUnitContext ctx, BoundUnit? parent, int index, HashSet<string> usedClassNames, EditionContext edition)
    {
        var idBody = ctx.identificationDivision()?.identificationBody();
        var pid = idBody?.programIdParagraph();
        var fid = idBody?.functionIdParagraph();
        string name = pid?.programName()?.GetText()
            ?? fid?.programName()?.GetText()
            ?? $"PROGRAM{index}";
        bool isFunction = pid is null && fid is not null;
        // §11.5 Format 2 — a signature-only prototype unit (M2-UDF-3). The COBOL-2002 introduction gate is now
        // VersionConformancePass.Run (14g.5, bound-arm over group.Units — BoundUnit.IsPrototype is drop-proof).
        bool isPrototype = fid?.PROTOTYPE() is not null;
        bool initial = false, common = false, recursive = false;
        foreach (var attr in pid?.programIdAttributes()?.programIdAttribute() ?? [])
        {
            var cpa = attr.commonProgramAttribute();
            if (cpa?.INITIAL_() is not null) initial = true;
            else if (cpa?.COMMON() is not null) common = true;
            else if (cpa?.RECURSIVE() is not null) recursive = true;
            else if (attr.literalAttribute()?.STRINGLIT() is { } asLit
                     && CobolLiteral.Decode(asLit.GetText()) is { Length: > 0 } asName)
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
        return new BoundUnit
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
    private static Core.ProgramUnitContext Reparent(Core.NestedProgramContext nested)
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
    /// (<see cref="BindUnitProcedure"/>) — the forward-reference enabler for user-function signatures.</summary>
    private static void BindUnitData(BoundUnit unit, BindSession session)
    {
        var edition = session.Edition;
        var data = new DataBinder(edition) { OoClasses = session.OoClasses };
        data.CallSeedUids(session.TakeUidBand());

        // Pre-seed inherited GLOBAL-table index names BEFORE Bind: the child's own INDEXED BY registrations then
        // allocate from a later ordinal and can never collide with a bridged container index field. The seeded
        // fields are SUPPRESSED from this unit's field emission — a global index-name is SHARED storage
        // (ISO §13.18.27 GR2), reached through the ref-bridge, never re-declared locally.
        for (var anc = unit.Parent; anc is not null; anc = anc.Parent)
            foreach (var g in anc.Data.CallGlobalRoots)
                foreach (string idxName in IndexNamesUnder(g))
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
                RegisterSubtree(data, g);
                foreach (var (condName, conds) in anc.Data.Conditions)
                    foreach (var cond in conds)
                        if (IsUnder(cond.Parent, g))
                        {
                            if (!data.Conditions.TryGetValue(condName, out var list)) data.Conditions[condName] = list = [];
                            list.Add(cond);
                        }
                if (g.Class is { Tier: RedefinesTier.StringCanonical } cls)
                    unit.Bridges.Add(new CallBridge(cls.BackingCsName, outer + cls.BackingCsName, "backing", null));
                else
                    unit.Bridges.Add(new CallBridge(g.CsName, outer + g.CsName, "field", g));
                foreach (string idxName in IndexNamesUnder(g))
                    if (anc.Data.IndexFields.TryGetValue(idxName, out string? field))
                        unit.Bridges.Add(new CallBridge(field, outer + field, "index", null));
            }
        }

        unit.Refs = new ReferenceResolver(data);
    }

    /// <summary>The PROCEDURE half of unit binding (phase 2): every unit's DATA is already bound
    /// (<see cref="BindUnitData"/>) and the group's user-function signature table is built, so a
    /// <c>FUNCTION user-name(args)</c> reference resolves its callee's RETURNING/USING descriptions
    /// regardless of unit order in the source (§8.4.3.2.4 GR1).</summary>
    private static void BindUnitProcedure(BoundUnit unit,
        IReadOnlyDictionary<string, UserFunctionSignature> userFunctions, BindSession session)
    {
        var data = unit.Data;
        var binder = new StatementBinder(data, unit.Refs)
        {
            OoClasses = session.OoClasses,
            UserFunctions = userFunctions,
            // §8.4.6.6 — inside a function definition its OWN name is a referable function-prototype-name
            // (self-recursion without a repository entry; §12.3.8 GR11 makes a present self-entry a no-op).
            UdfSelfName = unit.IsFunction ? unit.Name : null,
            // §15.65.3 argument rule 1 — MODULE-NAME NESTED requires a contained program.
            InNestedProgram = unit.Parent is not null,
        };
        binder.ConfigureEc(session.Turn, unit.Name);   // the EC bind context (TURN fold + §15.30 location element)
        unit.Bound = binder.Bind(unit.Ctx);
        // The boundary-copied GROUP formals + RETURNING item are registered whole-group-referenced (so MarkStoreAsImage
        // flips their numeric-DISPLAY leaves to image storage, and the formal's FromImage/AsImage round trip
        // type-checks — ISO §14.2.3 GR8 / §14.9 MOVE GR4) by the post-bind UsageCollectionPass, from data.LinkageFormals
        // + data.LinkageReturning. The pre-flip early-resolve of every formal existed ONLY for that side effect (which
        // ReferenceResolver no longer performs) — deleted, PHASE-05 Step 5.
    }

    /// <summary>Build the compilation group's user-function signature table (name → bound RETURNING item +
    /// USING formals), between the DATA and PROCEDURE bind phases: FUNCTION-ID units only (ISO §9.4 — the
    /// binder's function namespace never sees PROGRAM-ID units; §8.4.6.6 scope of function-prototype-names).
    /// The §14.2 procedure-division-header rule "The RETURNING phrase shall be specified in a function
    /// definition" (:23666) is checked HERE, once per unit — even an uncalled function without RETURNING is
    /// ill-formed.</summary>
    private static Dictionary<string, UserFunctionSignature> BuildUserFunctionTable(
        List<BoundUnit> units, EditionContext edition)
    {
        // Partition the group's FUNCTION-ID units by name into DEFINITIONS (a real body) and PROTOTYPES
        // (signature-only, §11.5 Format 2). A prototype precedes all other units (§10.6.2 SR1), so a naive
        // first-wins TryAdd would false-report the FOLLOWING same-name definition as a duplicate (1508) — the
        // partition prevents that. Every function unit must carry a RETURNING (§14.2 :23666) — checked once here.
        var defs = new Dictionary<string, BoundUnit>(StringComparer.OrdinalIgnoreCase);
        var protos = new Dictionary<string, BoundUnit>(StringComparer.OrdinalIgnoreCase);
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

    /// <summary>
    /// Whole-group analysis (ISO/IEC 1989:2023 §14.9 MOVE GR4 / COBOLNET_DESIGN §14.4): for every group used as a
    /// whole operand, flag each numeric USAGE-DISPLAY descendant leaf to store its character image
    /// (<see cref="DataItem.StoreAsImage"/>). A whole-group move fills the group "without consideration for the
    /// individual elementary items", so such a leaf may receive non-numeric characters (e.g. spaces) that a native
    /// <c>long</c> cannot represent. A COMP/COMP-3/COMP-5/float leaf is left native (its group is then a genuine
    /// mixed-usage byte-island — Tier-C, deferred); numeric-edited / alphanumeric leaves are already string-stored.
    /// (Relocated from the emitter, P6 Step 2 — a Bind-phase decision; Step 3 folds it into StorageFormPass.)
    /// </summary>
    private static void MarkStoreAsImage(DataBinder data)
    {
        foreach (var group in data.WholeGroupReferenced)
            MarkNumericDisplayLeaves(group);

        static void MarkNumericDisplayLeaves(DataItem item)
        {
            foreach (var child in item.Children)
            {
                // A fixed-OCCURS subordinate is part of the whole-group image too (ISO §14.9 — every OCCURS position):
                // a numeric-DISPLAY OCCURS leaf becomes string-stored (its array is string[]), so the §14.4 AsImage/
                // FromImage facility distributes it with no special case, and its subscripted accesses go through the
                // same StoreAsImage numeric pipeline (CobolNum.ParseDisplay/FormatDisplay).
                if (child.IsGroup) MarkNumericDisplayLeaves(child);
                else if (child.Pic is { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display })
                    child.StoreAsImage = true;
            }
        }
    }

    private static void RegisterSubtree(DataBinder data, DataItem item)
    {
        if (item.CobolName is { } name)
        {
            if (!data.ByName.TryGetValue(name, out var list)) data.ByName[name] = list = [];
            list.Add(item);
        }
        foreach (var child in item.Children) RegisterSubtree(data, child);
        foreach (var ren in item.Renames66) RegisterSubtree(data, ren);
    }

    private static IEnumerable<string> IndexNamesUnder(DataItem root)
    {
        foreach (string n in root.IndexNames) yield return n;
        foreach (var child in root.Children)
            foreach (string n in IndexNamesUnder(child)) yield return n;
    }

    private static bool IsUnder(DataItem item, DataItem ancestor)
    {
        for (DataItem? n = item; n is not null; n = n.Parent)
            if (ReferenceEquals(n, ancestor)) return true;
        return false;
    }
}
