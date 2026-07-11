// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.CodeGen.Emit;
using CobolNet.Frontend.Generated;

namespace CobolNet.CodeGen;

using Core = CobolParserCore;
using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>
/// The OO half of the Roslyn backend (docs/COBOLNET_OO_DESIGN.md — the Phase-3 spine): one real C# class per
/// CLASS-ID (<c>public class Foo : CobolObject</c>, deep-dive D1/D2), OBJECT-paragraph WORKING-STORAGE as
/// INSTANCE fields (D3), each METHOD-ID as a real virtual C# method (D7) running its contiguous exit-bounded
/// range of the class's ONE PC-dispatch space (the emit-into-a-type parameterization — the BoundUnit machinery
/// IS the template: the same per-unit emitter-state switch, FieldEmitter, and __Dispatch body render a class
/// exactly as they render a program class), and INVOKE as direct typed C# calls (D5) — the registry is
/// bypassed entirely (typed calls need no name resolution ABI).
/// </summary>
public sealed partial class CSharpEmitter
{
    // OoClassUnit relocated to Binding/Model/OoClassUnit.cs (P6 Step 2 — the binder owns the bound model).

    /// <summary>The group's pass-1 class symbol table (deep-dive D1) — built by <c>BinderDriver.CollectUnits</c> BEFORE
    /// any unit binds, so every DataBinder (typed object references) and StatementBinder (INVOKE) resolves
    /// classes defined anywhere in the file. Never null after collection (empty table when no classes).</summary>
    private OoClassTable _ooClasses = null!;

    /// <summary>The per-interface DATA forests (prototype LINKAGE formals — bound so ValidateImplements has
    /// resolved descriptions, and so the interface emission can render the formals' numeric profiles and
    /// group struct types as INTERFACE statics, which CONTENT conversions through interface-typed receivers
    /// qualify as <c>{IFACE}._P_n</c>; C# 8+ interfaces carry static members natively).</summary>
    private readonly Dictionary<OoInterfaceSymbol, DataBinder> _ooIfaceData = [];

    /// <summary>Bind one INTERFACE's prototype formals (§10.6.2 SR4 — LINKAGE-only data divisions; the
    /// prototypes reuse the whole OoBindMethodData machinery with no bodies).</summary>
    private void OoBindInterfaceData(OoInterfaceSymbol iface, EditionContext edition)
    {
        var data = new DataBinder(edition) { OoClasses = _ooClasses, OoIsClassUnit = true };
        data.CallSeedUids(_bindSession!.TakeUidBand());
        var synthetic = new Core.ProgramUnitContext(null!, -1);
        if (iface.Ctx.environmentDivision() is { } env) synthetic.AddChild(env);
        data.BindDeclarations(synthetic);
        foreach (var proto in iface.Prototypes)
            data.OoBindMethodData(proto);
        data.BindResolve(synthetic);
        _ooIfaceData[iface] = data;
    }

    /// <summary>Phase A of class binding — the DATA + SIGNATURES: the OBJECT paragraph's data division binds
    /// through the STANDARD DataBinder over a synthetic program-unit context (the <c>CallReparent</c>
    /// discipline — direct-children accessors see exactly the class's own divisions) producing INSTANCE
    /// fields; each METHOD's LINKAGE/LOCAL-STORAGE/WS sections and PD USING/RETURNING formals bind between
    /// the declaration and resolve halves (slice 2 — <c>DataBinder.OoBindMethodData</c>). Runs for EVERY class
    /// before ANY body binds, so a method of class A INVOKEing class B sees B's full signature regardless of
    /// source order (the pass-1 discipline, deep-dive D1).</summary>
    private void OoBindClassData(OoClassUnit cls, EditionContext edition)
    {
        var data = new DataBinder(edition) { OoClasses = _ooClasses, OoIsClassUnit = true };
        data.CallSeedUids(_bindSession!.TakeUidBand());
        var synthetic = OoReparentClassData(cls.Symbol.Ctx);
        data.BindDeclarations(synthetic);
        foreach (var m in cls.Symbol.Methods.ToList())   // snapshot — property synthesis appends accessors
            data.OoBindMethodData(m);
        data.OoBindPropertyClauses(cls.Symbol,
            cls.Symbol.Ctx.objectParagraph()?.dataDivision()?.workingStorageSection(), factory: false);
        data.BindResolve(synthetic);
        OoGateClassGlobal(data, cls.Name, "OBJECT", edition);
        cls.Data = data;
        cls.Refs = new ReferenceResolver(data);

        // The FACTORY half (§11.4; brief D11/D13): its OWN forest + uid band — factory data names are
        // invisible to instance methods and vice versa (separate source elements, §10.6), realized exactly
        // like method scoping: a second binder, never a merged namespace. SR 10 (INVOKE-argument ban on
        // factory WS) works free: the factory binder's WS roots are not method-scoped → OoIsObjectData.
        var fdata = new DataBinder(edition) { OoClasses = _ooClasses, OoIsClassUnit = true };
        fdata.CallSeedUids(_bindSession!.TakeUidBand());
        var fsynthetic = OoReparentFactoryData(cls.Symbol.Ctx);
        fdata.BindDeclarations(fsynthetic);
        foreach (var m in cls.Symbol.FactoryMethods.ToList())
            fdata.OoBindMethodData(m);
        fdata.OoBindPropertyClauses(cls.Symbol,
            cls.Symbol.Ctx.factoryParagraph()?.dataDivision()?.workingStorageSection(), factory: true);
        fdata.BindResolve(fsynthetic);
        OoGateClassGlobal(fdata, cls.Name, "FACTORY", edition);
        cls.FactoryData = fdata;
        cls.FactoryRefs = new ReferenceResolver(fdata);
    }

    /// <summary>Phase B — the method BODIES bind into the class's one pc space (per-method paragraph AND data
    /// scopes — §11.7; <c>StatementBinder.BindClassBody</c>).</summary>
    private void OoBindClassBody(OoClassUnit cls)
    {
        var binder = new StatementBinder(cls.Data, cls.Refs)
        {
            OoClasses = _ooClasses,
            OoCurrentClass = cls.Symbol,   // the SELF/SUPER resolution root (§8.4.3.8; slice 3b)
        };
        binder.ConfigureEc(_turnState, cls.Name);   // methods fold the same source-ordered >>TURN state (§7.3.25 GR6)
        cls.Bound = binder.BindMethodRoster(cls.Symbol, cls.Symbol.Methods);

        // The FACTORY roster binds through a SEPARATE binder over the factory forest, with the factory
        // SELF/SUPER context (§14.9.23.3 SR4f/h; §16.2.1 SELF|SUPER "NEW" — OoInFactory).
        var fbinder = new StatementBinder(cls.FactoryData, cls.FactoryRefs)
        {
            OoClasses = _ooClasses,
            OoCurrentClass = cls.Symbol,
            OoInFactory = true,
        };
        fbinder.ConfigureEc(_turnState, cls.Name);
        cls.FactoryBound = fbinder.BindMethodRoster(cls.Symbol, cls.Symbol.FactoryMethods);
    }

    /// <summary>Re-shape a class definition's data surface into a synthetic <c>programUnit</c> context for the
    /// per-unit DataBinder (the CallReparent pattern — accessors scan DIRECT children only): the OBJECT
    /// paragraph's environment/data divisions, then the class-level environment division (the singular
    /// <c>environmentDivision()</c> accessor returns the FIRST child, so the nearer OBJECT scope wins).</summary>
    private static Core.ProgramUnitContext OoReparentClassData(Core.ClassDefinitionContext ctx)
    {
        var unit = new Core.ProgramUnitContext(null!, -1);
        var obj = ctx.objectParagraph();
        if (obj?.environmentDivision() is { } envObj) unit.AddChild(envObj);
        if (ctx.environmentDivision() is { } envCls) unit.AddChild(envCls);
        if (obj?.dataDivision() is { } dd) unit.AddChild(dd);
        return unit;
    }

    /// <summary>Re-shape the FACTORY paragraph's data surface into a synthetic <c>programUnit</c> (the
    /// CallReparent discipline): factory env → class env → factory data division.</summary>
    private static Core.ProgramUnitContext OoReparentFactoryData(Core.ClassDefinitionContext ctx)
    {
        var unit = new Core.ProgramUnitContext(null!, -1);
        var fac = ctx.factoryParagraph();
        if (fac?.environmentDivision() is { } envFac) unit.AddChild(envFac);
        if (ctx.environmentDivision() is { } envCls) unit.AddChild(envCls);
        if (fac?.dataDivision() is { } dd) unit.AddChild(dd);
        return unit;
    }

    /// <summary>Enforce ISO §13.18.27.3 SR4 for an OBJECT/FACTORY definition: the GLOBAL clause shall not be
    /// specified in a factory, instance, or method definition — on an FD (SR1 file-description entry) OR a level-01
    /// data-description entry (SR1 file/WS/local-storage/linkage). GLOBAL is a nested-PROGRAM containment mechanism
    /// (a class contains no programs); program↔class file sharing is EXTERNAL only (§9.1.5). Both → COBOLNET1520.</summary>
    private static void OoGateClassGlobal(DataBinder data, string clsName, string half, EditionContext edition)
    {
        foreach (var f in data.Files)
            if (f.IsGlobal)
                edition.Error("COBOLNET1520", $"class '{clsName}': {half} file '{f.CobolName}' specifies the GLOBAL "
                    + "clause — GLOBAL shall not be specified in a factory, instance, or method definition (ISO §13.18.27.3 SR4)");
        // A GLOBAL level-01 DATA item (§13.18.27 SR1) — CallBindExternalAndGlobal collected it into CallGlobalRoots
        // (meaningless in a class, which contains no programs). An FD-record GLOBAL is already covered by the file loop.
        foreach (var g in data.CallGlobalRoots)
            if (!data.Files.Any(f => f.Records.Contains(g)))
                edition.Error("COBOLNET1520", $"class '{clsName}': {half} data item '{g.CobolName}' specifies the "
                    + "GLOBAL clause — GLOBAL shall not be specified in a factory, instance, or method definition (ISO §13.18.27.3 SR4)");
    }

    // OoQualifyClassFiles relocated to BinderDriver.QualifyClassFiles (P6 Step 5 — a Bind-phase FileModel
    // mutation; no CodeGen write into the binding model remains).

    /// <summary>Emit the object/factory file-connector members (M2-OO-1i): each host file the class declares
    /// registers in an emitted parameterless constructor — a FACTORY file registers once in the class singleton's
    /// ctor (inc 3); an OBJECT file registers once per object at construction (§9.1.4, inc 4) and also gets a
    /// per-object minted-key field. Zero host files ⇒ NO ctor emitted (byte-identical to a file-less class). The
    /// registration reuses <see cref="EmitFileRegistration"/> over <c>_ctx.Data</c> (set to this half's forest in
    /// <see cref="OoEmitTypeHalf"/>), each file addressed through <c>FileKeyExpr</c>.</summary>
    /// <summary>Emit the EXTERNAL record-area backings for a data forest (ISO §13.18.22.4 GR4b / §8.6.7): each
    /// <c>FD … IS EXTERNAL</c> record 01 re-bases onto a run-unit <c>ExternalStore</c> cell keyed by the FD name, so
    /// every describer (a program AND an object/factory) sees ONE shared record area. Shared by the program emit path
    /// (<see cref="CallEmitProgramClass"/>) and the OO type-half (M2-OO-1i inc 5) — a class EXTERNAL FD needs the same
    /// backing property, and <c>CallBindExternalAndGlobal</c> already populates <c>CallExternalBackings</c> on the
    /// class binder (it runs in <c>BindResolve</c>).</summary>
    private void EmitExternalBackings(DataBinder data, CodeWriter w)
    {
        foreach (var ext in data.CallExternalBackings)
            w.Line($"private ref string {ext.BackingCsName} => ref ExternalStore.Cell({CsLiteral(ext.ExternalName)}, "
                + $"{CsLiteral(ext.InitImage)}).Ref;   // EXTERNAL — ONE storage copy per run unit (ISO §8.6.7); survives CANCEL (§14.9.5 GR8)");
    }

    private void OoEmitFileMembers(string csName, DataBinder data, BoundProgram bound, CodeWriter w)
    {
        var host = data.Files.Where(f => !f.IsSortMerge).ToList();   // an SD is the in-memory sort store, never a host connector
        if (host.Count == 0) return;
        w.Line();
        // A per-object minted-key field for each instance file (§9.1.4 — one connector per object): initialized once
        // per object (field initializers run before the ctor body), so the ctor's Register/track see it live. A
        // factory / EXTERNAL file has a static literal key (InstanceKeyField null) and emits no field.
        foreach (var f in host.Where(f => f.InstanceKeyField is not null))
            w.Line($"private readonly string {f.InstanceKeyField} = CobolFile.MintInstanceKey({CsLiteral(f.CobolName)});");
        using (w.Block($"public {csName}()"))
        {
            EmitFileRegistration(w);   // each file registers under FileKeyExpr(f): a factory literal, or this.__fkey_X
            // A REPORT SECTION in this object/factory (Report Writer is a complete subsystem — the class emit path
            // just has to CALL it, the same class-emit-gap shape as inc 3/5): the engines construct AFTER their FDs
            // register (COBOLNET_REPORT_WRITER_DESIGN §4). Early-returns when Reports.Count == 0.
            RwEmitReportConstruction(bound, w);
            foreach (var f in host.Where(f => f.InstanceKeyField is not null))
                w.Line($"__TrackInstanceFile({FileKeyExpr(f)});");   // closed + dropped when the object is deleted (§9.1.4)
        }
        w.Line();
    }

    /// <summary>
    /// Emit one COBOL class as a real C# class (deep-dive D1/D2/D3/D7): instance fields from OBJECT data
    /// (VALUE clauses become field initializers — the generated public parameterless ctor IS the predefined
    /// NEW factory, D4: C# runs base-then-derived initialization exactly like COBOL's inherited-then-own
    /// order), one <c>public virtual</c> method per METHOD-ID whose body runs its exit-bounded pc range, and
    /// ONE <c>__Dispatch</c> over the class's whole method-paragraph space (the same dispatcher body a program
    /// class gets — the emit-into-a-type reuse). Runs on the SAME per-unit emitter-state switch as
    /// <see cref="CallEmitProgramClass"/>.
    /// </summary>
    private void OoEmitClassUnit(OoClassUnit cls, CodeWriter w)
    {
        // The INSTANCE class (D1/D2 + slice 3a: `: BASE` when the class INHERITS — single inheritance v1,
        // SSOT §18.18 — else the CobolObject runtime root; Roslyn needs no declaration ordering). The DIRECT
        // IMPLEMENTS list joins the base list (§11.8 — the closure arrives transitively at the C# level);
        // covariant-return conformances render as EXPLICIT interface implementations (D-I1's adapter cure:
        // C# forbids covariant interface implementations that §9.3.8.2.3 5a/5c2 permit).
        string instBase = string.Join(", ", new[] { cls.Symbol.Base?.CsName ?? "CobolObject" }
            .Concat(cls.Symbol.Implements.Select(i => i.CsName)));
        var instExtras = _ooClasses.AdapterPairs
            .Where(a => !a.Factory && ReferenceEquals(a.Impl.Owner, cls.Symbol))
            .Select(a =>
            {
                var (protoRet, protoSig) = OoSignatureOf(a.Proto);
                string args = string.Join(", ", a.Proto.Formals.Select(f => $"ref {f.ParamName}"));
                return $"{protoRet} {a.Iface.CsName}.{a.Proto.CsName}({protoSig}) => this.{a.Impl.CsName}({args});   // covariant-return adapter (§9.3.8.2.3 5c2)";
            })
            .ToList();
        OoEmitTypeHalf(cls.Name, cls.CsName, instBase,
            cls.Data, cls.Refs, cls.Bound, cls.Symbol.Methods, w,
            headerExtras: instExtras.Count > 0 ? instExtras : null,
            sealedType: cls.Symbol.IsFinal);

        // The FACTORY class (brief D11 — a REAL sibling singleton, NEVER statics: §8.6.4 per-class copies of
        // inherited factory data; SELF-in-factory polymorphism SR4f + GR2; §9.3.6 chain resolution). Every
        // CLASS-ID emits one — a class with no FACTORY paragraph still needs its own factory object and a
        // chain node for inherited factory methods.
        string facBase = string.Join(", ", new[] { cls.Symbol.Base?.FactoryCsName ?? "CobolObject" }
            .Concat(cls.Symbol.FactoryImplements.Select(i => i.CsName)));
        var extras = new List<string>
        {
            // The singleton (§9.3.14.2 "created before it is first referenced" — .NET static-readonly type
            // initialization satisfies it exactly). A derived factory needs `new` to shadow the base's.
            $"public {(cls.Symbol.Base is not null ? "new " : "")}static readonly {cls.Symbol.FactoryCsName} __Instance = new();",
            // The predefined New as a COVARIANT virtual (§16.2.1 GR1 ACTIVE-CLASS creation — an inherited
            // factory MAKE reached via INVOKE DOG "…" creates a DOG through the runtime override). A FINAL
            // class's factory is SEALED: its root __New emits NON-virtual (a virtual member in a sealed type
            // is Roslyn CS0549 on emitted code — the same trap the method-modifier table guards).
            cls.Symbol.Base is not null
                ? $"public override {cls.CsName} __New() => new {cls.CsName}();"
                : $"public {(cls.Symbol.IsFinal ? "" : "virtual ")}{cls.CsName} __New() => new {cls.CsName}();",
        };
        OoEmitTypeHalf(cls.Name, cls.Symbol.FactoryCsName, facBase,
            cls.FactoryData, cls.FactoryRefs, cls.FactoryBound, cls.Symbol.FactoryMethods, w, extras,
            sealedType: cls.Symbol.IsFinal);
    }

    /// <summary>The emit-into-a-type parameterization, realized (deep-dive Summary): ONE routine renders
    /// fields + methods + dispatch into a named type — called for the instance class and the factory class
    /// of every CLASS-ID (identical machinery; only the type identity, base, data forest, roster, and header
    /// extras differ).</summary>
    private void OoEmitTypeHalf(string cobolName, string csName, string baseCsName,
        DataBinder data, ReferenceResolver refs, BoundProgram bound, IReadOnlyList<OoMethodSymbol> roster,
        CodeWriter w, IReadOnlyList<string>? headerExtras, bool sealedType = false)
    {
        _refs = refs;
        _ctx = new EmitContext(w, data);
        _num = new NumericRenderer(_ctx);
        _cond = new ConditionRenderer(_num, _ctx);
        _callSelfPath = cobolName;       // a CALL from a method names the class as its calling path (§8.4.6.3)
        _callReturningPlace = null;      // methods deliver results via slice-2 RETURNING, never the program ABI
        _ecUnitHasF3 = false;            // declaratives inside methods are staged loud (no __EcDispatch here)
        _useDecls = false;               // a class owns no USE declaratives — clear any bleed from a prior unit (M2-OO-1i review)
        _callOuterGlobalUse = false;
        _callInheritedStatusPlace.Clear();

        using (w.Block($"public {(sealedType ? "sealed " : "")}class {csName} : {baseCsName}"))
        {
            foreach (string line in headerExtras ?? [])
                w.Line(line);
            var fields = new FieldEmitter(_ctx);
            fields.Emit();   // WS → INSTANCE fields (D3/D11); method WS → statics; VALUE inits = field initializers (D4)
            EmitExternalBackings(data, w);       // M2-OO-1i inc 5: a class EXTERNAL FD record → the shared run-unit cell
            RwEmitReportMembers(w);              // M2-OO-1i review: a class REPORT SECTION's engine fields + compose methods (Report Writer is complete)
            OoEmitFileMembers(csName, data, bound, w);   // M2-OO-1i: object/factory file connectors + report construction register in an emitted ctor
            // A method file verb under >>TURN EC-I-O … CHECKING emits an __IoCheckEc call (§9.1.13.1 fatal-status
            // default); the class type must declare it. A class has no USE declaratives (Declaratives == null), so
            // EcEmitIoCheckEc reduces to the status→EC bridge — no __RunUse/__EcDispatch needed (M2-OO-1i review).
            if (bound.Ec is { HasIoChecked: true }) EcEmitIoCheckEc(bound, w);
            if (bound.Paragraphs.Count > 0)
                w.Line($"private const int __N = {bound.Paragraphs.Count};   // paragraph count (all methods — one pc space)");
            w.Line();
            foreach (var m in roster)
                OoEmitMethod(bound, m, fields, w);
            OoEmitCobolInvoke(cobolName, roster, w);   // D10: the universal-dispatch switch (BOTH halves —
                                                       // a universal reference can hold a factory object)
        }
        w.Line();
    }

    /// <summary>Emit the class's <c>__CobolInvoke</c> override (D10/D-U2/D-U4): a switch over the methods
    /// this type DECLARES that are NOT overrides (an override needs no case — the BASE class's case calls
    /// <c>this.M(…)</c> and C# virtual dispatch delivers the override; 0829 guarantees identical
    /// descriptors), <c>default:</c> chains <c>base.__CobolInvoke</c> — the chain IS §9.3.6 resolution
    /// order, and the CobolObject root raises EC-OO-METHOD (GR7b). Each case enforces §14.9.23.4 GR7c at
    /// runtime — arity, per-argument conformance-descriptor equality (D-U3: the SAME rule as the
    /// compile-time strict check), RETURNING presence BOTH directions — raising EC-OO-UNIVERSAL (Table 13,
    /// fatal; unconditionally — the EC-OO-NULL/METHOD precedent: proceeding with a nonconforming crossing
    /// in a typed-native model is never an option). Box forms are CANONICAL BY DESCRIPTOR (D-U6a — never
    /// by either side's StoreAsImage, which MarkStoreAsImage flips per unit): S:* → string; N:Display:* →
    /// the display IMAGE string (bridged by the FormatDisplay/StoreDisplay overload pair); other N:* →
    /// the native value; O:* → the CobolObject reference. A type declaring zero non-override methods
    /// emits no override.</summary>
    private void OoEmitCobolInvoke(string cobolName, IReadOnlyList<OoMethodSymbol> roster, CodeWriter w)
    {
        var cases = roster.Where(m => m.OverrideOf is null).ToList();
        if (cases.Count == 0) return;
        w.Line();
        using (w.Block("public override void __CobolInvoke(string __name, CobolInvokeArg[] __a, CobolInvokeArg? __ret)"))
        using (w.Block("switch (__name)"))
        {
            foreach (var m in cases)
            {
                using (w.Block($"case {Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(m.Name.ToUpperInvariant(), quote: true)}:"))
                {
                    w.Line($"if (__a.Length != {m.Formals.Count}) throw new CobolFatalException(\"EC-OO-UNIVERSAL\", "
                        + $"$\"INVOKE '{cobolName}' '{m.Name}': {{__a.Length}} argument(s) for {m.Formals.Count} formal(s) "
                        + "(ISO §14.9.23.4 GR7c/§14.8.2 — runtime conformance through a universal receiver)\");");
                    for (int i = 0; i < m.Formals.Count; i++)
                    {
                        var f = m.Formals[i];
                        string want = OoClassTable.ConformanceDescriptor(f.Item);
                        string wantLit = Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(want, quote: true);
                        w.Line($"if (__a[{i}].Descriptor != {wantLit}) throw new CobolFatalException(\"EC-OO-UNIVERSAL\", "
                            + $"$\"INVOKE '{cobolName}' '{m.Name}': argument {i + 1} does not conform to the formal "
                            + $"(caller {{__a[{i}].Descriptor}}, formal {want.Replace('"', '\'')}) (ISO §14.9.23.4 GR7c/§14.8.2)\");");
                        w.Line($"var __p{i} = {OoUnivUnbox(f.Item, $"__a[{i}].Value")};");
                    }
                    if (m.Returning is null)
                        w.Line("if (__ret is not null) throw new CobolFatalException(\"EC-OO-UNIVERSAL\", "
                            + $"\"INVOKE '{cobolName}' '{m.Name}': RETURNING specified but the method declares none "
                            + "(ISO §14.8.3/GR7c)\");");
                    else
                    {
                        string rl = Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(
                            OoClassTable.ConformanceDescriptor(m.Returning), quote: true);
                        w.Line($"if (__ret is null || __ret.Descriptor != {rl}) throw new CobolFatalException(\"EC-OO-UNIVERSAL\", "
                            + $"\"INVOKE '{cobolName}' '{m.Name}': the RETURNING item is absent or does not conform "
                            + "(ISO §14.8.3/GR7c)\");");
                    }
                    string argList = string.Join(", ", Enumerable.Range(0, m.Formals.Count).Select(i => $"ref __p{i}"));
                    w.Line(m.Returning is null
                        ? $"this.{m.CsName}({argList});"
                        : $"var __rv = this.{m.CsName}({argList});");
                    for (int i = 0; i < m.Formals.Count; i++)
                        w.Line($"__a[{i}].Value = {OoUnivRebox(m.Formals[i].Item, $"__p{i}")};   // SR6 BY REFERENCE write-back");
                    if (m.Returning is not null)
                        w.Line($"__ret!.Value = {OoUnivRebox(m.Returning, "__rv")};");
                    w.Line("return;");
                }
            }
            w.Line("default: base.__CobolInvoke(__name, __a, __ret); return;");
        }
    }

    /// <summary>D-U6a: true when the item's canonical UNIVERSAL box form is the display IMAGE string while
    /// its local crossing form is native — the FormatDisplay/StoreDisplay bridge applies both directions.</summary>
    private static bool OoUnivImageBridged(DataItem item) =>
        !OoStringCarried(item)
        && item.Pic is { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display };

    /// <summary>The callee-side unbox: box value → a local in the FORMAL's own crossing form.</summary>
    private static string OoUnivUnbox(DataItem item, string box) =>
        OoStringCarried(item) ? $"(string){box}!"
        : OoUnivImageBridged(item) ? $"CobolNum.StoreDisplay((string){box}!, {item.ProfileName}, ({item.ElementType})0)"
        : item.Pic is { Category: PicCategory.ObjectReference } p ? $"({p.ClrType}){box}"
        : $"({item.ElementType}){box}!";

    /// <summary>The callee-side re-box: a local in the formal's crossing form → the canonical box form.</summary>
    private static string OoUnivRebox(DataItem item, string local) =>
        OoUnivImageBridged(item) ? $"CobolNum.FormatDisplay({local}, {item.ProfileName})" : $"(object?){local}";

    /// <summary>Caller-side universal dispatch (D-U6): box every argument per ITS OWN descriptor's canonical
    /// form, dispatch through the GR5 null guard with the bind-normalized literal or the runtime-normalized
    /// identifier-2 value, then copy out every argument (SR6 — all BY REFERENCE) and deliver RETURNING (GR8)
    /// through the receiver's own storage form. No direct-<c>ref</c> fast path BY DESIGN — the box IS the
    /// crossing (the abstract dispatch signature cannot take refs without per-signature generics).</summary>
    private void OoEmitUniversalInvoke(BoundInvokeUniversal u)
    {
        var w = _ctx.Writer;
        int id = _storeTmpCounter++;
        string boxes = string.Join(", ", u.Args.Select(a =>
            $"new CobolInvokeArg({Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(a.Descriptor, quote: true)}, {OoUnivCallerRead(a.Source)})"));
        w.Line($"var __ua{id} = new CobolInvokeArg[] {{ {boxes} }};");
        w.Line(u.Returning is not null
            ? $"var __ur{id} = new CobolInvokeArg({Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(u.ReturningDescriptor!, quote: true)});"
            : $"CobolInvokeArg? __ur{id} = null;");
        string selector = u.MethodLiteral is { } lit
            ? Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(lit, quote: true)
            : $"CobolObject.NormalizeMethodName({u.MethodSource!.Read()})";
        w.Line($"CobolObject.RequireNonNull({u.Receiver.Read()}).__CobolInvoke({selector}, __ua{id}, __ur{id});");
        for (int i = 0; i < u.Args.Count; i++)
            w.Line(OoUnivCallerWrite(u.Args[i].Source, $"__ua{id}[{i}].Value") + "   // BY REFERENCE copy-out (SR6)");
        if (u.Returning is { } ret)
            w.Line(OoUnivCallerWrite(ret, $"__ur{id}!.Value") + "   // RETURNING delivery (§14.9.23.4 GR8)");
        OoEmitInvokePickup();   // §14.6.13.1.5 — the universal path propagates identically (D-EO6)
    }

    /// <summary>SET Format 5 (D-U7; §14.9.39 GR9/GR10): copy the ONE sender reference into each target in
    /// order. The cast renders total: conformance was bind-checked (0867), and C# reference conversions
    /// cover the widening directions (typed→universal, subclass→base, null, this).</summary>
    private void OoEmitSetObjectRef(BoundSetObjectRef s)
    {
        var w = _ctx.Writer;
        if (s.FromExceptionObject)
        {
            // §8.4.3.6 — the register is implicitly UNIVERSAL: a universal target copies the reference;
            // a TYPED target takes the runtime narrow check (§9.3.8.2 :12291 — conformance through a
            // universal source is a RUNTIME question; failure = EC-OO-UNIVERSAL, Table 13).
            foreach (var tp in s.Targets)
            {
                if (tp.Item.Pic!.ObjectClassName is null)
                {
                    w.Line(tp.Write("ExceptionState.ExceptionObject") + "   // SET universal TO EXCEPTION-OBJECT (§8.4.3.6)");
                    continue;
                }
                string clr = tp.Item.Pic!.ClrType.TrimEnd('?');
                int id = _storeTmpCounter++;
                w.Line($"var __xo{id} = ExceptionState.ExceptionObject;");
                w.Line($"if (__xo{id} is not null && __xo{id} is not {clr}) throw new CobolFatalException(\"EC-OO-UNIVERSAL\", "
                    + $"\"SET {tp.Item.CobolName} TO EXCEPTION-OBJECT: the current exception object is not a "
                    + $"{tp.Item.Pic!.ObjectClassName} (ISO 9.3.8.2 runtime conformance; Table 13)\");");
                w.Line(tp.Write($"({clr}?)__xo{id}") + "   // SET typed TO EXCEPTION-OBJECT (runtime-narrowed)");
            }
            return;
        }
        string src = s.SourceIsNull ? "null"
            : s.SourceIsSelf ? "this"
            : s.SourceFactoryCs is { } fac ? $"{fac}.__Instance"
            : s.Source!.Read();
        foreach (var tp in s.Targets)
            w.Line(tp.Write($"({tp.Item.Pic!.ClrType})({src})") + "   // SET F5 (ISO §14.9.39 GR9 — reference copy)");
    }

    private static string OoUnivCallerRead(Place p) =>
        p is RefModPlace ? p.Read()
        : OoUnivImageBridged(p.Item) ? new NumericImagePlace(p).Read()
        : p.Read();

    private static string OoUnivCallerWrite(Place p, string box) =>
        p is RefModPlace ? p.Write($"(string){box}!")
        : OoStringCarried(p.Item) ? p.Write($"(string){box}!")
        : OoUnivImageBridged(p.Item) ? new NumericImagePlace(p).Write($"(string){box}!")
        : p.Item.Pic is { Category: PicCategory.ObjectReference } pic ? p.Write($"({pic.ClrType}){box}")
        : p.Write($"({p.Item.ElementType}){box}!");

    /// <summary>
    /// Emit one METHOD-ID as a real typed C# method (slice 2 — deep-dive D3/D6/D7/D8): BY REFERENCE formals as
    /// <c>ref</c> parameters copied into CAPTURABLE locals (a local function cannot capture a by-ref parameter),
    /// LINKAGE/LOCAL-STORAGE roots as locals (LOCAL-STORAGE re-initializes each activation, §14.5.3), the
    /// method's paragraph slice as a LOCAL-FUNCTION dispatcher (<c>__MDispatch</c> — it captures the locals by
    /// reference, so PERFORM recursion and the implicitly-RECURSIVE method rule, §12032/:12032, are structural),
    /// the ref copy-out, and the RETURNING local as the C# return value (§14.9.23.4 GR8). D7: <c>virtual</c> by
    /// default. The exit-bounded slice is the trap-#4 guard; a group formal crosses as its character image
    /// (the CALL-boundary discipline — a caller's group struct TYPE differs from the method's).
    /// </summary>
    private void OoEmitMethod(BoundProgram bound, OoMethodSymbol m, FieldEmitter fields, CodeWriter w)
    {
        var (retType, sig) = OoSignatureOf(m);
        if (m.PropertySubject is { } subject)
        {
            // A PROPERTY-clause-synthesized accessor (D-P1): a DIRECT field body — identical descriptions
            // make the spec's implicit MOVE a straight copy (§13.18.42 GR1/GR2 :21214-21229).
            string pmod = m.OverrideOf is not null
                ? (m.IsFinal && !m.Owner.IsFinal ? "sealed override" : "override")
                : (m.IsFinal || m.Owner.IsFinal) ? "" : "virtual";
            string pmods = pmod.Length == 0 ? "" : pmod + " ";
            if (m.Accessor == 'G')
                w.Line($"public {pmods}{retType} {m.CsName}() => {subject.CsName};   // PROPERTY {m.PropertyName} GET (§13.18.42 GR1)");
            else
                w.Line($"public {pmods}void {m.CsName}(ref {(OoStringCarried(subject) ? "string" : subject.ElementType)} __V) {{ {subject.CsName} = __V; }}   // PROPERTY {m.PropertyName} SET (GR2)");
            w.Line();
            return;
        }
        // D7's TOTAL modifier table (the OVERRIDE/FINAL wave): virtual by default (§9.3.6 runtime-class
        // dispatch); an override emits `override` — `sealed override` when ITS FINAL and the class is not
        // already sealed; a FINAL root method (or ANY fresh slot in a FINAL class) emits NON-virtual — a
        // `virtual` member inside a `sealed` class is Roslyn CS0549 on EMITTED code (the loud-failure trap
        // this table exists for). COBOL never expresses C# `new`/hiding (SR4a), so the set is total.
        string modifier = m.OverrideOf is not null
            ? (m.IsFinal && !m.Owner.IsFinal ? "sealed override" : "override")
            : (m.IsFinal || m.Owner.IsFinal) ? ""
            : "virtual";
        using (w.Block($"public {(modifier.Length == 0 ? "" : modifier + " ")}{retType} {m.CsName}({sig})   // METHOD-ID {m.Name} (ISO §11.7)"))
        {
            // LINKAGE roots → locals: a formal seeds from its parameter (copy-in; the copy-out below realizes
            // the BY REFERENCE write-through at the method boundary); the RETURNING item and unattached
            // entries start at their initial state (§14.2.3 GR6 — callee-allocated).
            foreach (var root in m.LinkageRoots)
            {
                // A Tier-A (alias) view root forwards to its canonical's field — no local (symmetry with
                // BuildPhysicals; COBOLNET_DESIGN §4.1; M2-OO-1h review C).
                if (root.Class is { Tier: RedefinesTier.Alias } && !root.IsCanonical) continue;
                // A method Tier-B REDEFINES canonical's storage is its string backing, not the root struct (M2-OO-1h
                // step 3) — emit that as the local; a LINKAGE formal seeds it from the caller's image, width-normalized
                // to the class width (a wider redefiner needs the full backing — review D), else from the initializer.
                if (fields.MethodRedefinesBackingDecl(root) is { } bkl)
                {
                    var formalB = m.Formals.FirstOrDefault(f => ReferenceEquals(f.Item, root));
                    w.Line($"string {bkl.Name} = {(formalB is null ? bkl.Init : $"CobolString.Store({formalB.ParamName}, {root.Class!.Width})")};   "
                        + $"// LINKAGE Tier-B REDEFINES backing for {root.CobolName}");
                    continue;
                }
                var (type, init) = fields.RootDecl(root);
                var formal = m.Formals.FirstOrDefault(f => ReferenceEquals(f.Item, root));
                if (formal is null)
                    w.Line($"{type} {root.CsName} = {init};   // LINKAGE {root.CobolName} (§14.2.3 GR6)");
                else if (root.IsGroup)
                {
                    // The image crossing: construct (arrays allocated), then distribute the caller's image.
                    w.Line($"{type} {root.CsName} = {init};   // LINKAGE formal {root.CobolName} (group — image crossing)");
                    w.Line($"{root.CsName}.FromImage({formal.ParamName});");
                }
                else
                    w.Line($"{type} {root.CsName} = {formal.ParamName};   // LINKAGE formal {root.CobolName} (BY REFERENCE copy-in)");
            }
            foreach (var root in m.LocalRoots)
            {
                if (root.Class is { Tier: RedefinesTier.Alias } && !root.IsCanonical) continue;   // Tier-A view → no local (review C)
                if (fields.MethodRedefinesBackingDecl(root) is { } bkl)   // Tier-B canonical → the string backing local (M2-OO-1h step 3)
                {
                    w.Line($"string {bkl.Name} = {bkl.Init};   // LOCAL-STORAGE Tier-B REDEFINES backing for {root.CobolName} (§14.5.3)");
                    continue;
                }
                var (type, init) = fields.RootDecl(root);
                w.Line($"{type} {root.CsName} = {init};   // LOCAL-STORAGE {root.CobolName} — re-initialized each activation (§14.5.3)");
            }
            // A method LOCAL/LINKAGE table's INDEXED BY cell is a per-activation local (§14.5.3; M2-OO-1h step 4) —
            // the method's own cell (§11.7.4 GR5), reset to 1 each activation, never the shared class index field.
            foreach (var root in m.LocalRoots.Concat(m.LinkageRoots))
                foreach (var idx in DataBinder.IndexNamesUnder(root))
                    if (m.DataScope.IndexFields.TryGetValue(idx, out var cell))
                        w.Line($"long {cell} = 1;   // INDEX-NAME {idx} (LOCAL/LINKAGE table cell, §14.5.3)");
            if (m.EntryPc <= m.EndPc)
            {
                // The method's slice of the class's one pc space, as a LOCAL FUNCTION (captures the locals
                // above by reference — zero allocation for direct calls).
                string saved = _dispatchName;
                _dispatchName = "__MDispatch";
                EmitDispatchMethod(bound, w, "int __MDispatch(int __startPc, int __exitPc)", m.EntryPc, m.EndPc);
                _dispatchName = saved;
                w.Line($"try {{ __MDispatch({m.EntryPc}, {m.EndPc}); }} catch (MethodReturn) {{ }}   "
                    + "// GOBACK / falling off the last paragraph returns HERE (§14.9.18.4 GR4; deep-dive D8)");
            }
            // BY REFERENCE copy-out (§14.2.3 GR8) / RETURNING (§14.9.23.4 GR8). A Tier-B REDEFINES canonical's
            // storage IS its string backing (a width-correct image), not the suppressed root struct — write that
            // back / return that, else the generated C# names an undeclared local (review A/emission).
            foreach (var f in m.Formals)
            {
                string src = fields.MethodRedefinesBackingDecl(f.Item) is { } bk ? bk.Name
                    : f.Item.IsGroup ? $"{f.Item.CsName}.AsImage()" : f.Item.CsName;
                w.Line($"{f.ParamName} = {src};   // BY REFERENCE copy-out (§14.2.3 GR8)");
            }
            if (m.Returning is { } r)
            {
                string src = fields.MethodRedefinesBackingDecl(r) is { } bk ? bk.Name
                    : r.IsGroup ? $"{r.CsName}.AsImage()" : r.CsName;
                w.Line($"return {src};   // the invocation result (§14.9.23.4 GR8)");
            }
        }
        w.Line();
    }

    /// <summary>The C# (return-type, parameter-list) of a method or prototype — ONE builder shared by class
    /// method emission, interface member emission, and the covariant adapters, so the three can never drift
    /// (the same reasoning as the ONE DescriptionMismatch).</summary>
    private static (string RetType, string Sig) OoSignatureOf(OoMethodSymbol m)
    {
        string retType = m.Returning is { } ret ? (OoStringCarried(ret) ? "string" : ret.ElementType) : "void";
        string sig = string.Join(", ", m.Formals.Select(f =>
            $"ref {(OoStringCarried(f.Item) ? "string" : f.Item.ElementType)} {f.ParamName}"));
        return (retType, sig);
    }

    /// <summary>Emit one INTERFACE-ID as a C# interface (§11.6; D-I1): members are the prototypes' signatures
    /// (the SAME builder class methods use); the prototypes' numeric profiles and group struct types emit as
    /// interface STATICS (C# 8+) so cross-unit CONTENT conversions can qualify them.</summary>
    private void OoEmitInterfaceUnit(OoInterfaceSymbol iface, CodeWriter w)
    {
        var data = _ooIfaceData[iface];
        _ctx = new EmitContext(w, data);
        string bases = iface.Inherits.Count > 0
            ? " : " + string.Join(", ", iface.Inherits.Select(b => b.CsName))
            : "";
        using (w.Block($"public interface {iface.CsName}{bases}"))
        {
            new FieldEmitter(_ctx).Emit();   // profiles + struct types only (LINKAGE roots are suppressed)
            foreach (var proto in iface.Prototypes)
            {
                var (retType, sig) = OoSignatureOf(proto);
                w.Line($"{retType} {proto.CsName}({sig});   // METHOD-ID {proto.Name} (prototype, §10.6.2 SR4)");
            }
        }
        w.Line();
    }

    /// <summary>Thin forward to THE one crossing-form predicate, <see cref="OoClassTable.StringCarried"/>
    /// (relocated to Binding in P6 Step 5 — the bind-phase override harmonize in <c>StorageFormPass</c> and these
    /// emit-side signature/marshaling renders must consult the SAME definition).</summary>
    private static bool OoStringCarried(DataItem item) => OoClassTable.StringCarried(item);

    private int _ooInvokeCounter;

    /// <summary>Emit one bound INVOKE (deep-dive D5/D6 — the binder already resolved the call form and
    /// validated §14.8.2 strict conformance; this renders the type-preserving marshaling).</summary>
    private void OoEmitInvoke(BoundInvoke inv)
    {
        var w = _ctx.Writer;
        switch (inv.Form)
        {
            case InvokeForm.New:
                // §16.2.1 — the predefined NEW: the generated ctor allocates + VALUE-initializes (D4); the
                // reference is delivered through RETURNING (§14.9.23.4 GR8).
                w.Line(inv.Returning!.Write($"new {inv.ClassCsName}()") + "   // INVOKE … \"NEW\" RETURNING (§16.2.1)");
                return;
            case InvokeForm.NewSelf:
                // §16.2.1 GR1 — ACTIVE-CLASS creation in a factory method: the covariant __New override on
                // the RUNTIME factory creates the runtime class (SUPER "NEW" deliberately identical — the
                // restricted search finds the same predefined New, GR3/GR1).
                w.Line(inv.Returning!.Write("this.__New()")
                    + "   // INVOKE SELF|SUPER \"NEW\" (§16.2.1 — active-class creation via the covariant __New)");
                return;
            case InvokeForm.Instance:
            case InvokeForm.Self:
            case InvokeForm.Super:
            case InvokeForm.Factory:
                OoEmitInstanceInvoke(inv);
                return;
            default:
                w.Line(LoudStmt($"INVOKE call form '{inv.Form}'"));
                return;
        }
    }

    /// <summary>The instance-call marshaling (D6; §14.9.23.4 GR6/GR7a/GR8): every formal is a <c>ref</c>
    /// parameter — a plain field of matching storage passes DIRECTLY (aliasing; subscripts evaluate once at
    /// the call, the GR7a once-only rule); anything else lowers to a copy-in temp, <c>ref</c> the temp, and —
    /// for BY REFERENCE identifier args — a copy-out. BY REFERENCE crossings are TYPE-PRESERVING (the strict
    /// §14.8.2.3.2 bind rules); BY CONTENT crossings CONVERT into the formal's description per §14.8.2.3.3
    /// (COMPUTE/MOVE/SET), composing the formal's value/image through the OWNER class's internal profiles
    /// (<c>{OWNER}._P_n</c>). Order per GR8: the call, the BY REFERENCE copy-outs, then the RETURNING
    /// delivery — identifier-4's store is the FINAL effect (the review's overlap finding).</summary>
    private void OoEmitInstanceInvoke(BoundInvoke inv)
    {
        var w = _ctx.Writer;
        int id = _ooInvokeCounter++;
        var argExprs = new List<string>();
        var post = new List<string>();

        var args = inv.Args ?? [];
        for (int i = 0; i < args.Count; i++)
        {
            var a = args[i];
            bool stringCarried = OoStringCarried(a.Formal);
            string qualProfile = a.Formal.Pic is { Category: PicCategory.Numeric, IsFloat: false }
                ? $"{inv.OwnerCsName}{(inv.Form is InvokeForm.Factory ? "__FACTORY" : "")}.{a.Formal.ProfileName}" : "";

            // The direct-ref fast path: a MemberPlace whose STORAGE form matches the parameter type exactly
            // (BY REFERENCE identifiers only — CONTENT always copies).
            if (a.Source is MemberPlace mp && a.WriteBack
                && (stringCarried
                    ? !mp.Item.IsGroup && OoStringCarried(mp.Item)
                    : !OoStringCarried(mp.Item))
                && !a.Formal.IsGroup && !mp.Item.IsGroup)
            {
                argExprs.Add($"ref {mp.Path}");
                continue;
            }

            string tmp = $"__iv{id}_{i}";
            if (a.Formal.IsGroup || (stringCarried && a.Source?.Item.IsGroup == true))
            {
                // The image crossing. BY REFERENCE allows a SMALLER formal (§14.8.2.2 rule 1 — a PREFIX of
                // the argument): pass the leading formal-width characters; the write-back below splices the
                // prefix back, preserving the argument's tail. CONTENT pads/truncates per MOVE.
                int fw = a.Formal.IsGroup ? a.Formal.ImageWidth : Math.Max(1, a.Formal.Pic!.Length);
                string read = a.Source is { } gsp ? CallStringRead(gsp) : CsLiteral(a.StringLiteral ?? "");
                w.Line($"string {tmp} = CobolString.Store({read}, {fw});");
            }
            else if (stringCarried)
                w.Line(a.Source is { } sp
                    ? $"string {tmp} = {OoStringReadOf(sp, a)};"
                    : a.StringLiteral is { } slit
                    ? $"string {tmp} = CobolString.Store({CsLiteral(slit)}, {Math.Max(1, a.Formal.Pic!.Length)});"
                    // A numeric literal into an image-stored numeric formal: compose the zoned image through
                    // the OWNER's internal profile (the review's cross-class rule — qualified, never bare).
                    : $"string {tmp} = CobolNum.FormatDisplay({EmitText.UnscaledAtScale(a.NumericLiteral!, a.Formal.Pic!.Scale)}, {qualProfile});");
            else if (a.Formal.Pic is { Category: PicCategory.ObjectReference })
                w.Line($"{a.Formal.ElementType} {tmp} = {a.Source!.Read()};");
            else if (a.Formal.Pic is { IsFloat: true })
                // Same-usage float (bind-enforced): read the float value directly — never through the
                // scaled-integer path (the review's silent-truncation finding).
                w.Line($"{a.Formal.ElementType} {tmp} = {a.Source!.Read()};");
            else if (a.ByContent && a.Source is { } cp
                     && _num.AsNum(new BoundFieldOperand(cp), ReceiverContext.None) is var cx
                     && (cp.Item.Pic?.Digits != a.Formal.Pic!.Digits || cp.Item.Pic?.Scale != a.Formal.Pic.Scale))
                // CONTENT numeric conversion (COMPUTE rules, §14.8.2.3.3 2a): rescale + truncate into the
                // formal's description through the OWNER's internal profile.
                w.Line($"{a.Formal.ElementType} {tmp} = ({a.Formal.ElementType})CobolNum.Store({cx.Expr}, {cx.Scale}, {qualProfile});");
            else
                w.Line(a.Source is { } np
                    ? $"{a.Formal.ElementType} {tmp} = ({a.Formal.ElementType})({_num.AsNum(new BoundFieldOperand(np), ReceiverContext.None).Expr});"
                    : $"{a.Formal.ElementType} {tmp} = ({a.Formal.ElementType})CobolNum.Store({UnscaledLit(a.NumericLiteral!).Expr}, {UnscaledLit(a.NumericLiteral!).Scale}, {qualProfile});");
            argExprs.Add($"ref {tmp}");

            if (!a.WriteBack || a.Source is not { } src) continue;
            // Copy-out to the CALLER's storage (BY REFERENCE — §14.2.3 GR8 at statement granularity).
            if (a.Formal.IsGroup || src.Item.IsGroup)
            {
                int fw = a.Formal.IsGroup ? a.Formal.ImageWidth : Math.Max(1, a.Formal.Pic!.Length);
                // The §14.8.2.2 rule-1 prefix: splice the formal's characters back over the argument's
                // LEADING positions, preserving the tail beyond the formal's width.
                post.Add(CallStringWrite(src,
                    $"{tmp} + CobolString.RefMod({CallStringRead(src)}, {fw + 1}, -1)"));
            }
            else if (src is RefModPlace)
                post.Add(src.Write(tmp));   // RefModPlace.Write splices the window (§8.4.2.4)
            else if (stringCarried)
                post.Add(OoStringCarried(src.Item) ? src.Write(tmp) : new NumericImagePlace(src).Write(tmp));
            else
                post.Add(src.Item.StoreAsImage
                    ? src.Write($"CobolNum.FormatDisplay({tmp}, {src.Item.ProfileName})")
                    : src.Write(tmp));
        }

        string target = inv.Form switch
        {
            InvokeForm.Self => "this",
            InvokeForm.Super => "base",
            // The factory singleton is never null — no GR5 guard (brief D11); virtual dispatch through the
            // factory hierarchy realizes §9.3.6 factory resolution.
            InvokeForm.Factory => $"{inv.ClassCsName}__FACTORY.__Instance",
            _ => $"CobolObject.RequireNonNull({inv.Receiver!.Read()})",
        };
        string call = $"{target}.{inv.MethodCsName}(" + string.Join(", ", argExprs) + ")";

        if (inv.ReturningSource is { } rs && inv.Returning is { } recv)
        {
            // GR8 — capture the result AT RETURN, flush the BY REFERENCE copy-outs, and store into
            // identifier-4 LAST (the final effect of the INVOKE — a receiver overlapping a temp-lowered
            // argument must see the argument's write-back first).
            string tmp = $"__ivr{id}";
            bool retString = OoStringCarried(rs);
            w.Line($"var {tmp} = {call};   // INVOKE (§14.9.23; null receiver → EC-OO-NULL, GR5)");
            foreach (var pLine in post) w.Line(pLine);
            if (rs.IsGroup || recv.Item.IsGroup)
                w.Line(CallStringWrite(inv.Returning, tmp));
            else if (recv is RefModPlace)
                w.Line(recv.Write(tmp));
            else if (retString == OoStringCarried(recv.Item))
                w.Line(recv.Write(tmp));
            else if (retString)   // string-carried result into native-numeric storage
                w.Line(recv.Write($"({recv.Item.ElementType})CobolNum.ParseDisplay({tmp}, {recv.Item.ProfileName})"));
            else                  // native result into image-stored numeric storage
                w.Line(recv.Write($"CobolNum.FormatDisplay({tmp}, {recv.Item.ProfileName})"));
        }
        else
        {
            w.Line($"{call};   // INVOKE (§14.9.23; null receiver → EC-OO-NULL, §14.9.23.4 GR5)");
            foreach (var pLine in post) w.Line(pLine);
        }
        OoEmitInvokePickup();   // §14.6.13.1.5 — a method GOBACK RAISING obj is consumed HERE (after GR8)
    }

    /// <summary>The copy-in read of an identifier argument for a STRING-CARRIED formal: a reference-modified
    /// place reads its window verbatim (§8.4.2.4 — the operand IS elementary alphanumeric); a string-stored
    /// item reads directly; a native display-numeric item formats through its OWN profile (caller-side). A
    /// CONTENT crossing normalizes to the formal's width (MOVE pad/truncate).</summary>
    /// <summary>The INVOKE-site propagation pickup (D-EO6): a method GOBACK/EXIT … RAISING stages; the
    /// ACTIVATING site consumes — after the RETURNING delivery and copy-outs (GR1b ordering). Instance/
    /// Self/Super/Factory + UNIVERSAL dispatches all pick up; NEW needs none (the generated ctor runs no
    /// user statements, D4). Gated on <c>_ecActive</c>, which spans class units.</summary>
    private void OoEmitInvokePickup() => CallEmitPropagationPickup();

    private string OoStringReadOf(Place sp, BoundInvokeArg a)
    {
        string read = sp is RefModPlace ? sp.Read()
            : OoStringCarried(sp.Item) ? sp.Read()
            : new NumericImagePlace(sp).Read();
        return a.ByContent && a.Formal.Pic is { } fp && fp.Category is PicCategory.Alphanumeric
            ? $"CobolString.Store({read}, {Math.Max(1, fp.Length)})"
            : read;
    }

}
