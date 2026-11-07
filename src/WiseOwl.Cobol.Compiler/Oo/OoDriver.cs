// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Compiler.Oo;

using Core = CobolParserCore;

/// <summary>
/// The OO bind driver (P9 R1 — the OO orchestration is a real BINDER collaborator, owned and sequenced by
/// <c>BinderDriver.Bind</c>; the former emitter-hosted <c>IOoBindHost</c> seam is DELETED): binds each
/// INTERFACE's prototype formals, each class's OBJECT + FACTORY data halves and method signatures (pass-1
/// discipline — every signature before any body, deep-dive D1), and each class's method bodies into the
/// class's one pc-dispatch space. Consumes ONLY binder state (<see cref="BindSession"/> + the binder types);
/// it emits nothing — emission renders the bound facts from <c>BoundCompilation</c>.
/// </summary>
internal sealed class OoDriver(BindSession session)
{
    /// <summary>Bind one INTERFACE's prototype formals (§2023:10.6.2 SR4 — LINKAGE-only data divisions; the
    /// prototypes reuse the whole OoDataBinder.BindMethodData machinery with no bodies).</summary>
    public void BindInterfaceData(OoInterfaceSymbol iface) => session.Binders.AddInterface(iface, BindInterface(iface));

    /// <summary>⛔ A PARAMETERIZED definition binds AS ITSELF, for its own rules, and nothing emits the result
    /// (kb/Work PB2051, PB2937; OO deep-dive D12). §2023:9.3.12 makes it a skeleton whose expansions are the classes, but it
    /// is a class definition all the same: every syntax rule of its data, environment and method headers — a BASED
    /// entry's level (§2023:13.16.3 SR16), a data-division section's placement (§2023:13.7.3 SR1), a duplicate METHOD-ID, a USAGE
    /// the targeted edition does not have — governs its text whether or not anything expands it, and until this bind
    /// those rules were asked of an expansion only, so an unexpanded definition compiled whatever its body held. Its
    /// parameter-names resolve through the one funnel as formals (<c>OoNameResolution.Result.IsFormal</c>), and a
    /// reference typed by a formal takes the alternative its REPOSITORY specifier declares, marked
    /// <see cref="ObjectRefDescriptor.Formal"/>: what the formal names is known only in an expansion, where §2023:9.3.8.2.4
    /// asks every question that turns on it "as if the actual parameter classes or interfaces were substituted". The
    /// uid band it draws is its own and comes after every emitted class's, so emission is unchanged. The bound forests
    /// are KEPT (<see cref="SkeletonClasses"/>, <see cref="SkeletonInterfaceData"/>); no pass that produces code sees
    /// them. Its method BODIES bind through <see cref="BindClassBody"/> too, after every emitted class's
    /// (<c>BinderDriver.Bind</c>): a statement that selects a method through a formal is each expansion's
    /// (<c>BoundDeferredToExpansion</c>), every other statement rule is asked here, and the edition pass walks the
    /// result (D12 determination 6; kb/Work PB2937).</summary>
    public void BindParameterized(OoClassSymbol skeleton)
    {
        var unit = new OoClassUnit { Symbol = skeleton };
        BindClassData(unit);
        _skeletonClasses.Add(unit);
    }

    /// <summary>The interface twin of <see cref="BindParameterized(OoClassSymbol)"/>: the prototypes' formals bind and
    /// the forest is kept for the data gates (nothing renders an interface skeleton).</summary>
    public void BindParameterized(OoInterfaceSymbol skeleton) => _skeletonInterfaceData[skeleton] = BindInterface(skeleton);

    /// <summary>The unexpanded parameterized CLASS definitions, their data and method headers bound as themselves
    /// (<see cref="BindParameterized(OoClassSymbol)"/>), and their method bodies (<see cref="BindClassBody"/>, which
    /// <c>BinderDriver.Bind</c> runs over them after every emitted class; kb/Work PB2937).</summary>
    public IReadOnlyList<OoClassUnit> SkeletonClasses => _skeletonClasses;
    private readonly List<OoClassUnit> _skeletonClasses = [];

    /// <summary>The unexpanded parameterized INTERFACE definitions' prototype forests
    /// (<see cref="BindParameterized(OoInterfaceSymbol)"/>).</summary>
    public IReadOnlyDictionary<OoInterfaceSymbol, DataBinder> SkeletonInterfaceData => _skeletonInterfaceData;
    private readonly Dictionary<OoInterfaceSymbol, DataBinder> _skeletonInterfaceData = [];

    private DataBinder BindInterface(OoInterfaceSymbol iface)
    {
        var definition = session.Sources.Of(iface);
        var data = ElementBinder(definition, definition, ownerClassName: null);
        data.CallSeedUids(session.TakeUidBand());
        // §2023:10.6.1 / §2023:11.9.4 GR1: the INTERFACE skeleton carries its own OPTIONS paragraph, and nothing bound it
        // — so its clauses had no effect on the prototypes AND, because OptionsBinder is where the
        // processor-dependence screens live, an interface could select a declined arithmetic mode in silence
        // (kb/Work PB197: the second hollow arm, found by sweeping all six optionsParagraph-bearing productions
        // rather than only the METHOD one the finding named). BindDeclarations binds it (kb/Work PB780).
        ConfigurationStep.Produce(new ConfigurationSource.Own(definition.optionsParagraph()), data.State.Configuration);
        OoEnvironmentRules.Screen(OoDefinition.Interface, $"interface '{iface.Name}'",
            definition.environmentDivision(), session.Edition);
        var synthetic = OoReparent(definition.environmentDivision(), dataDivision: null);
        data.BindDeclarations(synthetic);
        using (session.Edition.At(definition)) data.DeclareUserWord(iface.Name, UserWordKind.InterfaceName);
        foreach (var proto in iface.Prototypes)
            data.Oo.BindMethodData(proto, session.Sources.Of(proto));
        data.BindResolve(synthetic);
        return data;
    }

    /// <summary>Phase A of class binding — the DATA + SIGNATURES: the OBJECT paragraph's data division binds
    /// through the STANDARD DataBinder over a synthetic program-unit context (the <c>CallReparent</c>
    /// discipline — direct-children accessors see exactly the class's own divisions) producing INSTANCE
    /// fields; each METHOD's LINKAGE/LOCAL-STORAGE/WS sections and PD USING/RETURNING formals bind between
    /// the declaration and resolve halves (slice 2 — <c>OoDataBinder.BindMethodData</c>). Runs for EVERY class
    /// before ANY body binds, so a method of class A INVOKEing class B sees B's full signature regardless of
    /// source order (the pass-1 discipline, deep-dive D1).</summary>
    public void BindClassData(OoClassUnit cls)
    {
        var edition = session.Edition;
        // The environment-division placement of each definition of the class, judged once (kb/Work PB1076 + PB813).
        var classCtx = session.Sources.Of(cls.Symbol);
        OoEnvironmentRules.Screen(OoDefinition.Class, $"class '{cls.Name}'", classCtx.environmentDivision(), edition);
        OoEnvironmentRules.Screen(OoDefinition.Factory, $"class '{cls.Name}' FACTORY paragraph",
            classCtx.factoryParagraph()?.environmentDivision(), edition);
        OoEnvironmentRules.Screen(OoDefinition.Instance, $"class '{cls.Name}' OBJECT paragraph",
            classCtx.objectParagraph()?.environmentDivision(), edition);
        // The DATA-division twin: a factory or instance definition carries no LINKAGE or LOCAL-STORAGE SECTION
        // (§2023:13.7.3 SR1, §2023:13.6.3 SR1; kb/Work PB1251) and no GLOBAL clause in any entry (§2023:13.18.27.3 SR4; kb/Work
        // PB1045). Its methods' own data divisions are asked in OoDataBinder.BindMethodData.
        OoDefinitionRules.Screen(OoDefinition.Factory, $"class '{cls.Name}' FACTORY paragraph",
            classCtx.factoryParagraph()?.dataDivision(), edition);
        OoDefinitionRules.Screen(OoDefinition.Instance, $"class '{cls.Name}' OBJECT paragraph",
            classCtx.objectParagraph()?.dataDivision(), edition);
        // ⛔ THE CLASS DEFINITION IS A SOURCE ELEMENT OF ITS OWN (kb/Work PB3154). §2023:10.5: "A source element is a source
        // unit excluding any contained source units", and the factory and instance definitions are source units the
        // class definition contains (§2023:3.165). So the words of the class definition's environment division (its
        // REPOSITORY specifiers, its SPECIAL-NAMES names) and its CLASS-ID are declared ONCE, here, in the class's
        // §2023:8.3.2.2 census, and the factory and instance binders INHERIT the configuration they establish (§2023:12.3.4
        // GR1; ConfigurationStep.Produce, the contained-program model of kb/Work PB990) without declaring a word
        // of it: §2023:8.4.6.1 lets a contained source element "use identical user-defined words to name items independent
        // of the use of these user-defined words by other source elements". Both halves used to bind the class's
        // environment division as their own, so a factory or object data item spelled like a class the REPOSITORY
        // names, an alphabet SPECIAL-NAMES declares or the CLASS-ID itself was refused COBOL2692 on legal source.
        // Nothing here draws a uid (the division declares names, never data items), so it takes no uid band.
        var classData = ElementBinder(classCtx, classCtx, cls.Name);
        // §2023:10.6.1 / §2023:11.9.4 GR1 (kb/Work PB135): the class skeleton's OPTIONS paragraph is the contained
        // definitions' baseline (ConfigurationStep carries the class OPTIONS model to them); the OBJECT and FACTORY
        // paragraphs' own override it clause by clause. Each binds in its binder's BindDeclarations, after the
        // definition's constant entries are declared (kb/Work PB780).
        ConfigurationStep.Produce(new ConfigurationSource.Own(classCtx.optionsParagraph()), classData.State.Configuration);
        var classSynthetic = OoReparent(classCtx.environmentDivision(), dataDivision: null);
        classData.BindDeclarations(classSynthetic);
        using (edition.At(classCtx)) classData.DeclareUserWord(cls.Name, UserWordKind.ObjectClassName);
        classData.BindResolve(classSynthetic);
        var classConfiguration = ConfigurationStep.ForClass(classData.State.Configuration);   // what both halves inherit

        // The INSTANCE half: the OBJECT paragraph's own source element.
        var obj = classCtx.objectParagraph();
        var data = ElementBinder(obj, classCtx, cls.Name);
        data.CallSeedUids(session.TakeUidBand());
        // The class's configuration and OPTIONS model (the §2023:11.9.4 GR1 baseline), then the half's own OPTIONS paragraph.
        ConfigurationStep.Produce(new ConfigurationSource.ClassHalf(classConfiguration, obj?.optionsParagraph()), data.State.Configuration);
        var synthetic = OoReparent(obj?.environmentDivision(), obj?.dataDivision());
        data.Oo.ReserveClassMemberNames(cls.Symbol.ClrName, cls.Symbol.Methods);   // one C# class holds the object data AND the methods
        data.BindDeclarations(synthetic);
        foreach (var m in cls.Symbol.Methods.ToList())   // snapshot — property synthesis appends accessors
            data.Oo.BindMethodData(m, session.Sources.Of(m));
        data.Oo.BindPropertyClauses(cls.Symbol, factory: false);
        data.BindResolve(synthetic);
        // The class's symbolic-characters an OBJECT data-name of the spelling hides (§2023:8.4.6.1; kb/Work PB3154 R1-1).
        InheritStep.DropShadowed(data);
        session.Binders.AddObject(cls, data);

        // The FACTORY half (§2023:11.4; brief D11/D13): its OWN forest + uid band — factory data names are
        // invisible to instance methods and vice versa (separate source elements, §2023:10.6), realized exactly
        // like method scoping: a second binder, never a merged namespace. SR 10 (INVOKE-argument ban on
        // factory WS) works free: the factory binder's WS roots are not method-scoped → OoDataBinder.IsObjectData.
        var fac = classCtx.factoryParagraph();
        var fdata = ElementBinder(fac, classCtx, cls.Name);
        fdata.CallSeedUids(session.TakeUidBand());
        ConfigurationStep.Produce(new ConfigurationSource.ClassHalf(classConfiguration, fac?.optionsParagraph()), fdata.State.Configuration);   // §2023:11.9.4 GR1 (kb/Work PB135)
        var fsynthetic = OoReparent(fac?.environmentDivision(), fac?.dataDivision());
        fdata.Oo.ReserveClassMemberNames(cls.Symbol.FactoryClrName, cls.Symbol.FactoryMethods);   // the factory class holds both, likewise
        fdata.BindDeclarations(fsynthetic);
        foreach (var m in cls.Symbol.FactoryMethods.ToList())
            fdata.Oo.BindMethodData(m, session.Sources.Of(m));
        fdata.Oo.BindPropertyClauses(cls.Symbol, factory: true);
        fdata.BindResolve(fsynthetic);
        InheritStep.DropShadowed(fdata);   // …and a FACTORY data-name, likewise
        session.Binders.AddFactory(cls, fdata);
    }

    /// <summary>Phase B — the method BODIES bind into the class's one pc space (per-method paragraph AND data
    /// scopes — §2023:11.7; <c>StatementBinder.BindMethodRoster</c>). Runs after every program unit's DATA division and
    /// the group's REPOSITORY tables (<see cref="BindSession.Repository"/>): the class definition's REPOSITORY
    /// paragraph applies to its factory, its object and every method (ISO §2023:12.3.4 GR1: "The entries explicitly or
    /// implicitly specified in the configuration section of a source unit that contains other source units apply to
    /// each directly or indirectly contained source unit"), so a method's function-identifier and CALL
    /// program-prototype-name resolve through the SAME two lookups a program's do (kb/Work PB1100).</summary>
    public void BindClassBody(OoClassUnit cls)
    {
        var repository = session.Repository;
        // §2023:12.3.8.4 GR10 a) / GR11 a): "specified PREVIOUSLY" is relative to the class definition whose REPOSITORY
        // paragraph holds the specifier (kb/Work PB989) — an expansion's text starts where its skeleton does.
        int position = session.Sources.Of(cls.Symbol).Start.StartIndex;
        var (data, refs) = session.Binders.ObjectOf(cls);
        var binder = new StatementBinder(data, refs)
        {
            OoClasses = session.OoClasses,
            OoCurrentClass = cls.Symbol,   // the SELF/SUPER resolution root (§2023:8.4.3.8; slice 3b)
            UserFunctions = Binding.BinderDriver.UserFunctionsOf(data, unit: null, repository, position, session.Binders),
            ProgramPrototypes = Binding.BinderDriver.ProgramPrototypesOf(data, unit: null, repository, position, session.Binders),
        };
        binder.ConfigureEc(session.Turn, session.DirectiveSites, cls.Name);   // methods fold the same source-ordered >>TURN state (§2023:7.3.25 GR6)
        // kb/Work PB971 — the method formals' EC-OO-ARG-OMITTED guards (§2023:14.9.23.4 GR10), before any body binds.
        Binding.Procedure.EcBinder.MarkFormals(cls.Symbol.Methods, session.Turn);
        Binding.Procedure.EcBinder.MarkFormals(cls.Symbol.FactoryMethods, session.Turn);
        cls.Bound = binder.BindMethodRoster(cls.Symbol, cls.Symbol.Methods);

        // The FACTORY roster binds through a SEPARATE binder over the factory forest, with the factory
        // SELF/SUPER context (§2023:14.9.23.3 SR4f/h; §2023:16.2.1 SELF|SUPER "NEW" — OoInFactory).
        var (fdata, frefs) = session.Binders.FactoryOf(cls);
        var fbinder = new StatementBinder(fdata, frefs)
        {
            OoClasses = session.OoClasses,
            OoCurrentClass = cls.Symbol,
            OoInFactory = true,
            UserFunctions = Binding.BinderDriver.UserFunctionsOf(fdata, unit: null, repository, position, session.Binders),
            ProgramPrototypes = Binding.BinderDriver.ProgramPrototypesOf(fdata, unit: null, repository, position, session.Binders),
        };
        fbinder.ConfigureEc(session.Turn, session.DirectiveSites, cls.Name);
        cls.FactoryBound = fbinder.BindMethodRoster(cls.Symbol, cls.Symbol.FactoryMethods);
    }

    /// <summary>Re-shape one OO source element's own divisions into a synthetic <c>programUnit</c> context for the
    /// per-element DataBinder (the CallReparent pattern — the generated accessors scan DIRECT children only, so the
    /// binder sees exactly this element's environment and data divisions): a class, factory, instance or interface
    /// definition. A containing definition's environment division is never one of them: its configuration reaches
    /// the element through <c>ConfigurationStep.Produce</c> (§2023:12.3.4 GR1; kb/Work PB3154).</summary>
    private static Core.ProgramUnitContext OoReparent(Core.EnvironmentDivisionContext? environmentDivision,
        Core.DataDivisionContext? dataDivision)
    {
        var unit = new Core.ProgramUnitContext(null!, -1);
        if (environmentDivision is not null) unit.AddChild(environmentDivision);
        if (dataDivision is not null) unit.AddChild(dataDivision);
        return unit;
    }

    /// <summary>A DataBinder for one OO source element (§2023:10.5): <paramref name="element"/> is the parse node whose
    /// §2023:8.3.2.2 census the binder declares into (<see cref="OoClassTable.CensusOf"/>, the census the element's
    /// class-name references record into too; a null node, an absent FACTORY or OBJECT paragraph, gets a census of
    /// its own), <paramref name="definition"/> the class or interface definition whose first line decides the
    /// >>LEAP-SECOND state, and <paramref name="ownerClassName"/> what USAGE OBJECT REFERENCE [FACTORY OF]
    /// ACTIVE-CLASS binds to — §2023:13.18.60.4 GR22 e), the class of the object that invoked the containing method
    /// (kb/Work PB389); null for an interface.</summary>
    private DataBinder ElementBinder(Antlr4.Runtime.RuleContext? element, Antlr4.Runtime.ParserRuleContext definition,
        string? ownerClassName) => new(session.Edition)
    {
        OoClasses = session.OoClasses,
        OwnWords = session.OoClasses.CensusOf(element) ?? new(),
        OoRole = new OoUnitRole(ownerClassName),
        RefModZeroLength = session.RefModZeroLength,
        CobolWords = session.CobolWords,
        Retypes = session.Retypes,
        LeapSecond = session.LeapSecond.IsOnAt(definition.Start.Line),
        CompilationVariables = session.CompilationVariables,
    };
}
