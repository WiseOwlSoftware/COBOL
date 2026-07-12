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
    internal OoClassTable _ooClasses = null!;

    /// <summary>The per-interface DATA forests (prototype LINKAGE formals — bound so ValidateImplements has
    /// resolved descriptions, and so the interface emission can render the formals' numeric profiles and
    /// group struct types as INTERFACE statics, which CONTENT conversions through interface-typed receivers
    /// qualify as <c>{IFACE}._P_n</c>; C# 8+ interfaces carry static members natively).</summary>
    internal readonly Dictionary<OoInterfaceSymbol, DataBinder> _ooIfaceData = [];

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
    // The OO EMIT half lives on Verbs/OoEmitter.cs since Step 9m (BATCH-3b); this partial keeps the
    // OO BIND half (the IOoBindHost bodies) + the bind-session forests until P9 relocates them.
}
