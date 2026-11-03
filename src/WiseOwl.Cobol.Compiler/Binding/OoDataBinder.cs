// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;

using WiseOwl.Cobol.Binding.Model;

using WiseOwl.Cobol.Compiler.Oo;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;
using WiseOwl.Cobol.Runtime;

/// <summary>What an OO source element's data binder is bound as (<c>Oo/OoDriver.ElementBinder</c>): a CLASS unit's half — the
/// OBJECT paragraph, the FACTORY paragraph — or an INTERFACE definition. <see cref="OwnerClassName"/> is what
/// <c>USAGE OBJECT REFERENCE [FACTORY OF] ACTIVE-CLASS</c> binds to — §13.18.60.4 GR22 e), the class of the object that
/// invoked the containing method (kb/Work PB389) — and is null for an interface, which has no containing class.</summary>
internal sealed record OoUnitRole(string? OwnerClassName);

/// <summary>
/// The OO half of the data binder (docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1); kb/Work PB2360, step 10 of the
/// <see cref="DataBinder"/> extract; OO deep-dive D6): a class unit's METHOD data — each method's LINKAGE SECTION (→ typed C#
/// parameters via capturable locals) and LOCAL-STORAGE SECTION (→ C# locals, re-initialized each activation, §8.6.4) — its PD-header
/// formals, the PROPERTY clauses' accessors, the <c>USAGE OBJECT REFERENCE</c> adjudication, and the per-method name scopes
/// (<see cref="OoMethodDataScope"/>). A method has NO WORKING-STORAGE at any edition: §13.5.3 SR1 allows it "only in a factory
/// definition or an instance definition, but not in a method definition", so one is refused (COBOL1519; kb/Work PB1308, decision
/// R62) and its entries bind as LOCAL-STORAGE for recovery only. Items bind into the CLASS's one forest (so USAGE/SIGN inheritance,
/// object-reference resolution, profiles, and struct types all apply unchanged) but their NAMES join the method's own scope as they
/// are declared (<see cref="DataBinder.NameScopeOf"/>; kb/Work PB3192).
/// <para>One per <see cref="DataBinder"/> (<see cref="DataBinder.Oo"/>), holding the OO state of that binder's forest
/// and asking the binder for the entry binding it shares with every other element kind.</para>
/// </summary>
internal sealed class OoDataBinder(DataBinder data, OoUnitRole? role)
{
    private EditionContext Edition => data.Edition;
    private OptionsModel Options => data.State.Configuration.Options;
    private OoClassTable? OoClasses => data.OoClasses;
    private IReadOnlyList<DataItem> Roots => data.State.Forest.Roots;

    /// <summary>The unit's OO role, set ONCE by <c>Oo/OoDriver</c> when it makes the binder (null for a program or a
    /// function unit); the binder's <see cref="DataBinder.OoRole"/> is where it is read.</summary>
    private readonly OoUnitRole? _role = role;

    /// <summary>True when this binder binds a CLASS unit (per-binder-instance configuration, set ONCE at
    /// construction by <c>Oo/OoDriver</c> through <see cref="DataBinder.OoRole"/> — init-only, so a mid-bind mutation
    /// cannot compile): a root that is NOT method-scoped is then OBJECT data — §14.9.23.3 SR 10 bans it from crossing an
    /// INVOKE BY REFERENCE (a bare object-data argument is assumed BY CONTENT instead, §14.9.23.4 GR6a2).</summary>
    public bool IsClassUnit => _role is not null;

    /// <summary>The COBOL name of the class this binder's entries are written in — set once by <c>Oo/OoDriver</c>
    /// for the instance and factory halves alike, null for a program/function unit and for an interface. It is what
    /// <c>USAGE OBJECT REFERENCE ACTIVE-CLASS</c> resolves to: §13.18.60.4 GR22 e) makes the referenced object
    /// "of the same class as the object that was used to invoke the method in which this data description entry
    /// is specified", and §13.18.60.3 SR16 guarantees such a containing class exists wherever the phrase is
    /// legal. kb/Work PB389.</summary>
    public string? OwnerClassName => _role?.OwnerClassName;

    /// <summary>Every METHOD whose data this binder bound, in bind order — the roster whose procedure divisions
    /// the data-pointer pre-scan walks (a class unit's synthetic program carries no procedure division of its
    /// own; kb/Work PB956).</summary>
    internal IReadOnlyList<OoMethodSymbol> BoundMethods => _boundMethods;
    private readonly List<OoMethodSymbol> _boundMethods = [];

    /// <summary>Every method-scoped root of the class (union over all methods) — the SR 10 discriminator:
    /// an item whose 01/77 root is NOT in this set, in a class unit, is object data.</summary>
    public HashSet<DataItem> MethodScopedRoots { get; } = [];

    /// <summary>Method root (01/77) or method TYPE DECLARATION → its owning method symbol (M2-OO-1h), recorded when the
    /// method's entry opens it (<see cref="BindingMethod"/>; kb/Work PB3192). The name registrations read it
    /// (<see cref="DataBinder.NameScopeOf"/>), and the post-build passes (OdoResolve, ResolveRedefines, ExpandTypes), which run with
    /// <see cref="ProcedureBindState.ActiveMethodScope"/> null, resolve a method item's data-name-1 / REDEFINES target / type-name through
    /// its OWNING method's scope (§11.7.4 GR5), keyed off the item's root, never the class globals.</summary>
    internal Dictionary<DataItem, OoMethodSymbol> RootOwner { get; } = new(ReferenceEqualityComparer.Instance);

    /// <summary>Set while a METHOD's declarations are bound or collected (<see cref="EnterMethodDeclarations"/>):
    /// INDEXED BY index-names register into the method's own scope, never the unit's (M2-OO-1h step 4), and every
    /// user-defined word lands in the method's own §8.3.2.2 census (kb/Work PB2921). Null at program/object scope.</summary>
    internal OoMethodDataScope? BindingMethodScope { get; private set; }

    /// <summary>The METHOD whose data entries are being bound (<see cref="BindMethodData"/>; cleared with the declaration
    /// scope, <see cref="MethodDeclarationScope"/>), null otherwise: every record and type declaration its entries open is
    /// the method's (<see cref="RootOwner"/>) from the moment it is opened (kb/Work PB3192), so its names land in the
    /// method's scope as they are declared.</summary>
    internal OoMethodSymbol? BindingMethod { get; private set; }

    /// <summary>Declare into <paramref name="scope"/>'s method until the returned token is disposed: its data entries
    /// and METHOD-ID (<see cref="BindMethodData"/>) and its procedure-names (the class-body collection,
    /// <c>StatementBinder.BindClassBody</c>). One method source element at a time, so no scope nests.</summary>
    internal MethodDeclarationScope EnterMethodDeclarations(OoMethodDataScope scope)
    {
        BindingMethodScope = scope;
        return new MethodDeclarationScope(this);
    }

    /// <summary>The restore token of <see cref="EnterMethodDeclarations"/>.</summary>
    internal readonly struct MethodDeclarationScope(OoDataBinder binder) : IDisposable
    {
        public void Dispose() => (binder.BindingMethodScope, binder.BindingMethod) = (null, null);
    }

    /// <summary>The METHOD a reference made now is made in: the method whose statements are being bound
    /// (<see cref="ProcedureBindState.ActiveMethodScope"/>), else the method whose data division is being bound (<see cref="BindingMethodScope"/>);
    /// null at program, object, factory and interface level. ⛔ THE ONE ANSWER every "which element am I in" lookup reads
    /// (kb/Work PB3192): a method's data division is part of the method's source element, so a reference its entries make
    /// (a constant's <c>LENGTH OF</c> operand, a TYPE clause's type-name) is answered from the method first (§11.7.4 GR5)
    /// exactly as one its statements make — <see cref="DataBinder.ActiveScope"/> used to read only the statement half, so the
    /// method's own data-names were invisible to its own data division.</summary>
    internal OoMethodDataScope? CurrentMethodScope => data.Procedure.ActiveMethodScope.Current ?? BindingMethodScope;

    /// <summary>Whether the method a reference is made in (<see cref="CurrentMethodScope"/>) declares <paramref name="word"/>
    /// as a data-name, a condition-name, an index-name, a CAPACITY register, a type-name or a constant-name of its own
    /// (<see cref="OoMethodDataScope.Declares"/>) — the NEAREST tier a reference made in that method can reach, so such a declaration hides every constant-name and
    /// symbolic-character of the spelling the class's binder holds (ISO §8.4.6.2.1 3) a) "If the name is declared in
    /// source element B, the item in source element B is the referenced item"; §8.4.6.1 "even when it is a different
    /// type of user-defined word"; kb/Work PB3154 review R1-1). Asked by the two literal-alias lookups,
    /// <see cref="ConstantEntryBinder.FindConstant"/> and <c>SpecialNamesBinder.SymbolicOf</c>; always false outside a method.</summary>
    internal bool ActiveMethodDeclares(string word) => CurrentMethodScope?.Declares(word) == true;

    /// <summary>Reserve the names the C# class this binder's data is emitted into already holds, BEFORE the data
    /// division binds: its own type name <paramref name="classClrName"/> (the instance class beside the object data,
    /// the factory singleton class beside the factory data) and the C# member names of the methods emitted into it.
    /// A data root spelled like one of them takes a distinct field name (<c>PQ_2</c>) instead of a member named like
    /// its enclosing type (CS0542) or a second member <c>PQ</c> (CS0102). The COBOL is legal: the METHOD-ID's
    /// method-name belongs to the method's own source element (§8.4.6.5, §10.5; kb/Work PB2921) and the CLASS-ID's
    /// object-class-name to the class definition's (kb/Work PB3154), and §8.3.2.2 bars one word as two types only
    /// within one source element. The data root is private, so its C# name is free to move; the type's and the
    /// methods' are their contract.</summary>
    internal void ReserveClassMemberNames(string classClrName, IEnumerable<OoMethodSymbol> methods) =>
        data.RootNames.Reserve([classClrName, .. methods.Select(static m => m.ClrName)]);

    /// <summary>True when <paramref name="item"/> is OBJECT data of a class unit (§14.9.23.3 SR 10): its 01/77
    /// root is not method-scoped. Always false outside a class unit.</summary>
    public bool IsObjectData(DataItem item)
    {
        if (!IsClassUnit) return false;
        return !MethodScopedRoots.Contains(item.Root);
    }

    /// <summary>
    /// ⛔ THE ONE ADJUDICATION OF A <c>USAGE OBJECT REFERENCE</c> PHRASE (ISO §13.18.60.2 general format) — the
    /// written syntax in, an <see cref="ObjectRefDescriptor"/> out. The format prints THREE stacked alternatives
    /// inside one bracket pair, over four independent axes:
    /// <code>
    /// OBJECT REFERENCE ⎡ interface-name-1                            ⎤
    ///                  ⎢ [ FACTORY OF ] ACTIVE-CLASS                 ⎥
    ///                  ⎣ [ FACTORY OF ] object-class-name-1 [ ONLY ] ⎦
    /// </code>
    /// <para>The grammar cannot tell interface-name-1 from object-class-name-1 (both are one <c>cobolWord</c>),
    /// so it parses the superset <c>[FACTORY OF] className [ONLY]</c> and THIS method makes the general-format
    /// rejection once the name resolves: the interface alternative carries neither FACTORY nor ONLY.</para>
    /// <para>Three verdicts live here and nowhere else — COBOL0813 (the name is neither a class nor an
    /// interface of the compilation group), COBOL1925 (FACTORY/ONLY on the interface-name alternative) and
    /// COBOL1924 (§13.18.60.3 SR16, ACTIVE-CLASS outside a factory definition, an instance definition, or the
    /// linkage or local-storage section of a method definition). kb/Work PB389.</para>
    /// </summary>
    internal ObjectRefDescriptor BindObjectRefDescriptor(
        Core.ObjectReferenceUsageContext? oru, string entryWhere, EntrySection section)
    {
        // No phrase at all — §13.18.60.4 GR22 b), the universal object reference. (Also the defensive answer for
        // an object-reference usage acquired without its own written clause: the description it inherits already
        // carries its own descriptor, so nothing reads this arm.)
        if (oru is null) return ObjectRefDescriptor.Universal;
        bool factory = oru.FACTORY() is not null;

        if (oru.ACTIVE_CLASS() is not null)
        {
            // §13.18.60.3 SR16: "The ACTIVE-CLASS phrase may be specified only in a factory definition, an
            // instance definition, or the linkage or local-storage section of a method definition." Two arms:
            // outside a class body entirely (a program, a function or a program/function prototype is not a method
            // definition), and inside a METHOD's data but in a section SR16 excludes (a method's WORKING-STORAGE is
            // the reachable case — a method has no other sections, §13.4.3 SR1 and friends being enforced by
            // OoDefinitionRules). A method PROTOTYPE of an interface definition IS a method definition (§10.6.1
            // NOTE), so its linkage section is admitted — with NO containing class (kb/Work PB1497): the interface
            // binder has no OwnerClassName, and the description is owner-less (ObjectRefDescriptor.ActiveClass).
            if (!IsClassUnit)
            {
                Edition.Error(DiagnosticCatalog.ObjectReferenceActiveClassPlacement, $"{entryWhere}: USAGE OBJECT "
                    + "REFERENCE ACTIVE-CLASS is written outside a class definition — the ACTIVE-CLASS phrase may "
                    + "be specified only in a factory definition, an instance definition, or the linkage or "
                    + "local-storage section of a method definition (ISO §13.18.60.3 SR16)");
                // No class body → nothing to bind to. Fall back to the UNIVERSAL description so the erroring
                // compile still emits a well-typed field (CobolObject?) instead of a dangling type name.
                return ObjectRefDescriptor.Universal;
            }
            if (BindingMethodScope is not null
                ? section is not (EntrySection.LocalStorage or EntrySection.Linkage)
                : OwnerClassName is null)   // object/factory data has a class; an interface has no data outside its prototypes
            {
                Edition.Error(DiagnosticCatalog.ObjectReferenceActiveClassPlacement, $"{entryWhere}: USAGE OBJECT "
                    + "REFERENCE ACTIVE-CLASS is written "
                    + (BindingMethodScope is not null ? "in a method definition's WORKING-STORAGE SECTION" : "outside a class definition")
                    + " — the phrase may be specified only in a factory definition, an instance definition, or the "
                    + "linkage or local-storage section of a method definition (ISO §13.18.60.3 SR16)");
                return ObjectRefDescriptor.Universal;
            }
            return ObjectRefDescriptor.ActiveClass(OwnerClassName, factory);
        }

        if (oru.className()?.GetText() is not { } name) return ObjectRefDescriptor.Universal;
        return DescribeNamedReference(oru, name, factory, only: oru.ONLY() is not null,
            $"{entryWhere}: USAGE OBJECT REFERENCE", "ISO §13.18.60.2/.4",
            DiagnosticCatalog.ObjectReferenceInterfacePhrase, "ISO §13.18.60.2; §13.18.60.4 GR22 c)");
    }

    /// <summary>⛔ THE ONE READING OF A WRITTEN <c>[FACTORY OF] class-or-interface-name [ONLY]</c> DESCRIPTION — the
    /// <c>USAGE OBJECT REFERENCE</c> clause (§13.18.60.2) and the object-view's AS phrase (§8.4.3.5.2, kb/Work PB1425)
    /// print the same three axes over one word that may name a class or an interface, so both ask this: the name
    /// resolves through the one funnel in the REFERRING source element's scope (§8.4.6.4), and FACTORY OF / ONLY on an
    /// interface is refused by the asking format's own rule (<paramref name="interfacePhrase"/>). An unresolved name
    /// keeps the word written (the funnel has reported why), so every later diagnostic still says what the program
    /// wrote.</summary>
    internal ObjectRefDescriptor DescribeNamedReference(Antlr4.Runtime.ParserRuleContext site, string name,
        bool factory, bool only, string where, string resolveRule,
        DiagnosticDescriptor interfacePhrase, string interfaceRule)
    {
        // A TYPED reference (spine part 2 — LIVE): the declared name must resolve — its emitted C# field type
        // IS the named class's or interface's emitted type (PicInfo.ClrType), so an unresolved name would
        // surface as a Roslyn CS0246 on user source (a loud-failure violation). §13.18.60.2: the operand is
        // an interface-name or an object-class-name, so the funnel is asked for `Either`.
        // ⛔ The set is the REFERRING SOURCE ELEMENT's (§8.4.6.4), NOT the compilation group's: this site
        // called `OoClasses.Find`/`FindInterface` directly and so accepted a class the source element may not
        // reference (kb/Work PB365 — the same widened set as the USE Format-4 arm; §13.18.60 states no
        // REPOSITORY rule of its own, so §8.4.6.4 IS the rule). Resolving the name HERE, through the one
        // funnel, is what makes that scope travel with the descriptor: `oru` is the reference site the
        // ancestor walk starts from.
        var resolved = Compiler.Oo.OoNameResolution.Resolve(OoClasses, Edition, site, name,
            Compiler.Oo.OoNameResolution.Want.Either, where, "COBOL0813", resolveRule);
        // A PARAMETER-NAME of the parameterized definition being bound as itself (kb/Work PB2051, PB2937): it stands where
        // an object-class-name or an interface-name is permitted (§11.3.4 GR6), so the description takes the alternative
        // its own REPOSITORY specifier declares — an interface formal is the bare-name alternative — marked FORMAL: what it
        // names is each expansion's actual (§9.3.8.2.4, §12.3.8.4 GR5), so a question that turns on which class it is
        // waits for the expansion, and every other one is asked here (ObjectRefDescriptor.Formal). Nothing emits from it.
        bool isInterface = resolved.Interface is not null
            || resolved.IsFormal && !Compiler.Oo.OoNameResolution.Lookup(OoClasses, site, name,
                Compiler.Oo.OoNameResolution.Want.Class).IsFormal;
        if (isInterface && (factory || only))
            // The general format's interface-name-1 alternative is the BARE name: FACTORY OF and ONLY belong to
            // the object-class-name-1 alternative alone, and GR22 c) states the interface reading with no
            // subordinate rules at all ("the object referenced by this data item shall implement interface-1").
            Edition.Error(interfacePhrase, $"{where} names the interface '{name}' with "
                + (factory && only ? "the FACTORY OF and ONLY phrases" : factory ? "the FACTORY OF phrase" : "the ONLY phrase")
                + " — those phrases belong to the object-class-name-1 alternative of the general format; the "
                + $"interface-name-1 alternative carries neither ({interfaceRule})");
        if (resolved.IsFormal) return ObjectRefDescriptor.FormalParameter(name, isInterface, factory, only);
        if (!resolved.Ok)
            // The funnel has already reported WHICH of the two failures this is (defined in the group but out
            // of scope vs. defined nowhere). Keep the name so every later diagnostic about this item still
            // says what the programmer wrote.
            return ObjectRefDescriptor.ObjectClass(name, factory, only);
        if (isInterface)
            return ObjectRefDescriptor.Interface(resolved.Interface!.Name).Resolved(resolved.Interface.ExternalizedName,
                resolved.Interface.Origin is Compiler.Oo.OoSymbolOrigin.Imported ? resolved.Interface.ClrName : null);
        // ⛔ The descriptor carries the RESOLVED definition's name, not the word written: a REPOSITORY specifier's
        // `AS literal-1` makes the local word (BOXY) name the class whose externalized name is the literal
        // (§12.3.8.4 GR2, kb/Work PB974), and every downstream reader of the descriptor looks the class up by
        // this name in the group's table. It also carries the class's IDENTITY — its externalized name — and, for a
        // class imported from another assembly, its full CLR name (DESIGN-external-repository §6.4, §10.1).
        var cls = resolved.Class!;
        return ObjectRefDescriptor.ObjectClass(cls.Name, factory, only)
            .Resolved(cls.ExternalizedName, cls.Origin is Compiler.Oo.OoSymbolOrigin.Imported ? cls.ClrName : null);
    }

    /// <summary>
    /// Bind one METHOD's data sections into the class forest and its own name scope, and resolve the method's
    /// PROCEDURE DIVISION USING/RETURNING formals (positional, §14.9.23.4 GR3). Runs between
    /// <see cref="DataBinder.BindDeclarations"/> and <see cref="DataBinder.BindResolve"/> so the post-build passes cover method items.
    /// REDEFINES, OCCURS INDEXED BY, OCCURS DEPENDING and level-66 entries in method data are carried (M2-OO-1h).
    /// The sections a method may not carry and every GLOBAL clause written in it are refused by
    /// <see cref="OoDefinitionRules.Screen"/>; an EXTERNAL clause by the clause-placement screen (§13.18.22.3 SR1
    /// admits it only in file description entries, level 1 working-storage entries and level 1 type declarations).
    /// </summary>
    internal void BindMethodData(OoMethodSymbol m)
    {
        string where = $"method '{m.Name}'";
        m.Binding ??= new OoMethodBinding();   // the after-data-bind half attaches HERE (P9 R7 — phase-explicit)
        _boundMethods.Add(m);
        // A method's environment division may not carry the configuration section (§12.3.3 SR2) or the
        // input-output section (§12.4.3 SR1) — a bare header is legal (§10.6.1, §12.2.1). One placement table
        // for every OO definition kind (kb/Work PB1076). A method references the class's files via §11.7.4 GR5.
        OoEnvironmentRules.Screen(OoDefinition.Method, where, m.Ctx.environmentDivision(), Edition);

        var dd = m.Ctx.dataDivision();
        BindingMethod = m;
        using (EnterMethodDeclarations(m.DataScope))
        {
            // The METHOD-ID's word belongs to the METHOD's source element (§8.4.6.5: "The method-name of a method is
            // declared in the METHOD-ID paragraph"), not its class's, so a METHOD-ID spelled like its CLASS-ID is legal
            // (§8.4.6.1; kb/Work PB2921): a method-name, or for a GET/SET PROPERTY method the property-name it names.
            // The accessors a PROPERTY clause synthesizes carry no METHOD-ID and never come here.
            using (Edition.At(m.Ctx))
                data.DeclareUserWord(m.PropertyName ?? m.Name,
                    m.PropertyName is null ? UserWordKind.MethodName : UserWordKind.PropertyName);
            // The method's constant entries are declared before its OPTIONS paragraph and its sections bind, so one may be
            // referenced before the entry that defines it (kb/Work PB1231); BindRemainingConstants below binds what
            // nothing needed.
            if (dd is not null) data.Constants.DeclareDataEntries(dd);
            // §10.6.1's method skeleton prints [options-paragraph] (kb/Work PB135): fold it over the class
            // channel's model per §11.9.4 GR1; the roster swaps it in around this method's body bind. Bound inside the
            // method's scope, after its constant entries are declared: the INITIALIZE clause's literal-1 may be one of
            // its constant-names, or one of its object's (§13.10.3 SR2, §8.4.6.2.2; kb/Work PB780).
            if (m.Ctx.optionsParagraph() is { } mo)
                m.MethodOptions = OptionsBinder.BindParagraph(mo, Edition, Options, data.Constants.FindConstant);
            if (dd is not null)
            {
                // The sections a method definition may not carry — FILE, WORKING-STORAGE, REPORT, SCREEN (§13.4.3,
                // §13.5.3, §13.8.3, §13.9.3 SR1) — are judged once, at EVERY edition, by the one DATA-division placement
                // table (kb/Work PB1251, PB1308 decision R62). A method's own data division is LOCAL-STORAGE (§13.6.3) and
                // LINKAGE (§13.7.3), both bound below. The same screen refuses every GLOBAL clause written in the method
                // (§13.18.27.3 SR4, COBOL1520 — a method prototype of an interface included; kb/Work PB1045).
                OoDefinitionRules.Screen(OoDefinition.Method, where, dd, Edition);
                if (dd.workingStorageSection() is { } ws)
                {
                    // §13.5.3 SR1 refused this section above (OoDefinitionRules, at EVERY edition — 2002, 2014 and 2023
                    // alike, kb/Work PB1308 decision R62; never an edition gate, never relaxed by --permissive). The
                    // entries are still bound — in their own (working-storage) section, so every section rule keeps
                    // answering — into the method's local roots: recovery only (the compile has failed), so the method
                    // body's references to these names do not draw a cascade of "not defined" errors that would bury the
                    // real one. There is no static-field mapping of method data any more.
                    m.Binding!.LocalRoots.AddRange(data.BindEntries(ws.dataDescriptionEntry(), data.RootNames));
                }
                if (dd.localStorageSection() is { } ls)
                    m.Binding!.LocalRoots.AddRange(data.BindEntries(ls.dataDescriptionEntry(), data.RootNames, EntrySection.LocalStorage));
                if (dd.linkageSection() is { } lk)
                    m.Binding!.LinkageRoots.AddRange(data.BindEntries(lk.dataDescriptionEntry(), data.RootNames, EntrySection.Linkage));
                data.Constants.BindRemainingConstants();
            }
        }

        // Each root is the method's since it was opened (RootOwner, AttachToHierarchy; kb/Work PB3192), and its names
        // joined the method's scope as they were declared.
        foreach (var root in m.Binding!.LocalRoots.Concat(m.Binding!.LinkageRoots))
            MethodScopedRoots.Add(root);
        // M2-OO-1h step 4: a method table's index cell is a per-activation method local (emitted in OoEmitMethod).
        // LINKAGE + LOCAL-STORAGE roots are LOCALS of the emitted method (their struct types and numeric profiles still
        // emit at class level) — never instance fields — so each root's storage is named by its local's identity, never by
        // a CLR name (ISO §8.6.4; kb/Work PB4416): every access path to it starts at that local (PlaceBuilder.AddRoot).
        foreach (var root in m.Binding!.LocalRoots.Concat(m.Binding!.LinkageRoots))
        {
            root.ActivationLocal = new MethodDataLocal(root);
            data.Linkage.SuppressRootField(root.ClrName);
        }

        // The PD header formals (§14.2.2 SR1 — level-01/77 LINKAGE entries; correspondence is positional).
        // The using-phrase modes (§14.2.1: { [BY REFERENCE] { [OPTIONAL] d }… | BY VALUE { d }… }…) are TRANSITIVE
        // across the parameters that follow until the other phrase appears, BY REFERENCE assumed before the first
        // (§14.2.3 GR4) — threaded flat over the grammar's one-parameter-per-node shape, exactly as the program arm
        // does (CallBindLinkage). A BY VALUE formal is recorded (OoFormal.ByValue): the activator hands it a detached
        // value and the method never copies it back (kb/Work PB1051). The OPTIONAL phrase CARRIES (kb/Work PB757): the
        // formal records it, and the method ABI pairs every formal with its omitted-presence parameter
        // (OoFormal.OmittedFlag) — the method arm of the one presence fact the program arm's null carrier realizes
        // (kb/Work PB133 wave C).
        var pd = m.Ctx.procedureDivision();
        int pos = 0;
        bool byValue = false;
        // The formals whose ADDRESS OF the body takes are AREA formals (FormalIsCarrierResident — the program arm's test).
        var addressed = (pd is null ? [] : data.ClaimScans.AddressTargets(pd)).Select(t => t.Name).ToHashSet(CobolNames.Comparer);
        var header = new ProcedureHeaderScreen(Edition, where);   // §14.2.2 SR1/SR2/SR5/SR6 — the ONE header screen (kb/Work PB1145)
        foreach (var prm in pd?.usingClause()?.usingParameter() ?? [])
        {
            using var _ = Edition.At(prm);
            Core.DataReferenceContext dref;
            bool optional = false;
            if (prm.usingByValue() is { } vb) { byValue = true; dref = vb.dataReference(); }
            else if (prm.usingByReference() is { } rb)
            {
                byValue = false;
                dref = rb.dataReference();
                optional = rb.OPTIONAL() is not null;
            }
            else { dref = prm.dataReference(); optional = prm.OPTIONAL() is not null; }
            string pname = dref.GetText();
            var item = m.Binding!.LinkageRoots.FirstOrDefault(r =>
                CobolNames.Same(r.CobolName, pname));
            if (item is null)
                Edition.Error(DiagnosticCatalog.ProcedureHeaderItemNotLinkageRoot, $"{where}: PROCEDURE DIVISION USING parameter '{pname}' is not "
                    + "a level-01/77 LINKAGE SECTION item of the method (ISO §14.2.2 SR1)");
            else if (header.AdmitFormal(pname, item))
            {
                if (optional && byValue) header.OptionalNeedsByReference(pname);   // §14.2.1: OPTIONAL is a BY REFERENCE word
                if (byValue)
                {
                    data.CompleteDescriptionAhead(item);   // a TYPE-typed formal has no class until its subject is composed (kb/Work PB3222)
                    header.ByValueClass(pname, item); // §14.2.2 SR2
                }
                // ⛔ A RESIDENT formal's every reference reads and writes its argument's storage through the method-local
                // carrier (ISO §14.2.3 GR8; kb/Work PB2087, the method arm of CallBindLinkage's rule): every place built
                // over it starts at the carrier's local root (PlaceBuilder.AddRoot; kb/Work PB4320, PB4380), and no
                // local of its own is declared — the method's linkage roots were all suppressed above.
                bool resident = LinkageBinder.FormalIsCarrierResident(item, m.Binding!.LinkageRoots, addressed);
                var formal = new OoFormal(item, pos, m.Binding!.FormalName(item, pos), optional, byValue, resident);
                if (resident) item.ResidentCarrier = new FormalCarrier.Local(formal.Carrier);
                m.Binding!.Formals.Add(formal);
            }
            pos++;
        }
        if (pd?.returningClause()?.dataReference() is { } rref)
        {
            using var returningAt = Edition.At(rref);   // the RETURNING item's own position, as each USING parameter's (kb/Work PB2034)
            m.Binding!.Returning = m.Binding!.LinkageRoots.FirstOrDefault(r =>
                CobolNames.Same(r.CobolName, rref.GetText()));
            if (m.Binding!.Returning is null)
                Edition.Error(DiagnosticCatalog.ProcedureHeaderItemNotLinkageRoot, $"{where}: PROCEDURE DIVISION RETURNING item '{rref.GetText()}' "
                    + "is not a level-01/77 LINKAGE SECTION item of the method (ISO §14.2.2 SR1)");
            else
                header.CheckReturning(rref.GetText(), m.Binding!.Returning);   // SR5 / SR6 — SR6 was mis-cited "SR4" here
        }
        ScreenPropertyMethodHeader(m, pd?.usingClause()?.usingParameter().Length ?? 0,
            m.Owner is { } owner ? $"class '{owner.Name}'{(m.IsFactory ? " (FACTORY)" : "")}: " : "");
        // §13.7.3 SR4: which of the method's linkage records its procedure division may reference — the ONE decision
        // the program/function arm takes at the same point (SR5 bars a FUNCTION's formals, so a method has none).
        data.Linkage.SealLinkageRules(m.Binding!.LinkageRoots, m.Binding!.Formals.Select(f => f.Item).ToList(),
            m.Binding!.Returning, function: false);
        // The method's PD-header RAISING phrase (§14.2.1): the ONE partition every header arm calls
        // (RaisingPhrase — kb/Work PB815/PB814; D-EO8), so the method arm and the program arm cannot disagree.
        m.Raising.AddRange(RaisingPhrase.Partition(pd?.raisingClause(), OoClasses, Edition,
            $"{where}: METHOD-ID RAISING"));
        // A GROUP formal/RETURNING item crosses the boundary as its character image (§14.2.3 GR8) and so must be
        // whole-group-referenced (its numeric-DISPLAY leaves image-stored, untouched caller bytes round-tripping) —
        // registered post-bind by UsageCollectionPass (PHASE-05 Step 5), which receives these formals from the emitter.

        // ── The ANY LENGTH placement sweep, METHOD path (ISO §13.18.2.3 SR2/SR3; the CallBindLinkage sweep is
        // the program/function/object path). SR2: LINKAGE only, elementary, and the containing method shall not
        // be a PROPERTY method (an explicit METHOD-ID GET|SET PROPERTY — Accessor != '\0'; a PROPERTY-clause-
        // synthesized accessor clones object data, where the clause is already rejected). SR3: referenced in the
        // method's PD header as a BY REFERENCE formal (SR3a — a BY VALUE formal does not qualify) or the RETURNING
        // item (SR3b). Violations clear the flag (the IsBased discipline). ──
        foreach (var root in m.Binding!.LocalRoots)
            if (root.IsAnyLength)
            {
                using var localAt = Edition.At(root);   // the entry's own position, as AnyLengthValidateUnit's (kb/Work PB2034)
                Edition.Error(DiagnosticCatalog.AnyLengthPlacement, $"{where}: data item '{root.CobolName ?? "FILLER"}': the ANY "
                    + "LENGTH clause may be specified only in the LINKAGE SECTION (ISO §13.18.2.3 SR2)");
                root.IsAnyLength = false;
            }
        foreach (var root in m.Binding!.LinkageRoots)
        {
            if (!root.IsAnyLength) continue;
            using var linkageAt = Edition.At(root);   // the entry's own position, as AnyLengthValidateUnit's (kb/Work PB2034)
            string rw = $"{where}: data item '{root.CobolName ?? "FILLER"}'";
            if (root.IsGroup)
                Edition.Error(DiagnosticCatalog.AnyLengthPlacement, $"{rw}: the subject of an ANY LENGTH clause shall be ELEMENTARY "
                    + "— this entry has subordinate items (ISO §13.18.2.3 SR2)");
            else if (m.Accessor != '\0')
                Edition.Error(DiagnosticCatalog.AnyLengthPlacement, $"{rw}: the ANY LENGTH clause may not be specified in a PROPERTY "
                    + "method (ISO §13.18.2.3 SR2 — a method that is not a property method)");
            else if (!m.Binding!.Formals.Any(f => ReferenceEquals(f.Item, root) && !f.ByValue)   // SR3 a): a BY VALUE formal does not qualify
                && !ReferenceEquals(m.Binding!.Returning, root))
                Edition.Error(DiagnosticCatalog.AnyLengthSubjectNotInHeader, $"{rw}: the subject of an ANY LENGTH clause shall be referenced "
                    + "in the method's procedure division header as a BY REFERENCE formal parameter or as the "
                    + "RETURNING item (ISO §13.18.2.3 SR3)");
            else
                continue;   // conformant — keep the flag. A RETURNING item (SR3 b)) receives the INVOKE receiver's length
                            // through the method's trailing __retLen parameter (§13.18.2.4 GR1 b); OoEmitter.OoSignatureOf).
            root.IsAnyLength = false;
        }
    }

    /// <summary>ISO §11.7.3 SR6 / SR7 — the header a GET or SET PROPERTY method shall have, asked of the BOUND header
    /// (kb/Work PB1503): every METHOD-ID with the GET or SET phrase passes here — a class's instance and factory
    /// methods and an interface's prototypes alike — and nowhere else. SR6: "If the GET phrase is specified, then the
    /// method shall have no USING phrase parameters specified in the procedure division header and shall have a single
    /// RETURNING phrase. The returning item shall not be an object reference described with the ACTIVE-CLASS phrase."
    /// SR7: "If the SET phrase is specified, then the method shall have a single USING parameter specified in the
    /// procedure division header and no RETURNING phrase. The USING parameter shall not be an object reference
    /// described with the ACTIVE-CLASS phrase." <paramref name="usingCount"/> counts the parameters WRITTEN (a formal
    /// the header screen refused still counts), so the emitter's "the setter's one formal" is an invariant.</summary>
    private void ScreenPropertyMethodHeader(OoMethodSymbol m, int usingCount, string owner)
    {
        if (m.Accessor == '\0') return;
        var b = m.Binding!;
        bool get = m.Accessor == 'G';
        string? fault = get
            ? usingCount > 0 || !m.HasReturning
                ? "a get property method shall have no USING phrase parameters specified in the procedure division "
                  + "header and shall have a single RETURNING phrase (ISO §11.7.3 SR6)"
              : IsActiveClassReference(b.Returning)
                ? "the returning item of a get property method shall not be an object reference described with the "
                  + "ACTIVE-CLASS phrase (ISO §11.7.3 SR6)"
              : null
            : usingCount != 1 || m.HasReturning
                ? "a set property method shall have a single USING parameter specified in the procedure division header "
                  + $"and no RETURNING phrase — this one has {usingCount} USING parameter(s)"
                  + (m.HasReturning ? " and a RETURNING phrase" : "") + " (ISO §11.7.3 SR7)"
              : b.Formals.Count == 1 && IsActiveClassReference(b.Formals[0].Item)
                ? "the USING parameter of a set property method shall not be an object reference described with the "
                  + "ACTIVE-CLASS phrase (ISO §11.7.3 SR7)"
              : null;
        if (fault is not null)
        {
            using var _ = Edition.At(m.Ctx);
            Edition.Error(DiagnosticCatalog.PropertyMethodRule,
                $"{owner}METHOD-ID {(get ? "GET" : "SET")} PROPERTY {m.PropertyName}: {fault}");
        }

        static bool IsActiveClassReference(DataItem? item) => item?.Pic?.ObjectRef is { Kind: ObjectRefKind.ActiveClass };
    }


    /// <summary>SYNTHESIZE the accessor method symbols (D-P1 — the PINNED §11.7.4 GR1a implementor naming
    /// <c>__GET_&lt;P&gt;</c>/<c>__SET_&lt;P&gt;</c>) for every OBJECT/FACTORY working-storage item that carries a
    /// PROPERTY clause (§13.18.42): GET returns the SUBJECT item's description; SET takes one formal of it. The
    /// emitter renders DIRECT field bodies — observably identical to the spec's implicit MOVE methods (§13.18.42.4
    /// GR1/GR2) because the descriptions are identical by construction. WITH NO GET/SET suppresses the accessor; an
    /// explicit GET/SET PROPERTY method of the subject's name (already on the roster) is §11.7.3 SR5, asked here as its
    /// own rule (COBOL2790), and the clause then defines nothing.
    /// <para>⛔ THE SUBJECT IS THE ITEM THAT CARRIES THE CLAUSE (<see cref="DataItem.Property"/>, kb/Work PB1273).
    /// This used to walk the PARSE entries and find each subject by FIRST NAME MATCH over the whole forest, so with
    /// two same-named items the accessor bound whichever came first in tree order — a wrong answer, and the one
    /// SR3 exists to prevent. The §13.18.42.3 screens asked here are the ones that need the finished object name
    /// space and ancestry: SR2 (subject to an OCCURS clause, by ancestry too), SR3's qualification half, SR5
    /// (CONSTANT RECORD requires WITH NO SET) and SR6 (no ACTIVE-CLASS object reference). SR1 is <c>BindEntry</c>'s;
    /// SR3's elementary half and §13.16.3 SR21 are the clause-placement table's; SR4 (a superclass's property-name)
    /// is <c>OoClassTable.ResolveOverrides</c>', asked once every class's accessors exist (kb/Work PB1274).</para>
    /// <para>The accessors this pass adds are what <c>OoClassTable.ResolveOverrides</c> resolves OVERRIDE / FINAL over
    /// (§13.18.42.4 GR3 — a FINAL clause's accessors are FINAL), so it runs before that pass for every class.</para></summary>
    internal void BindPropertyClauses(OoClassSymbol cls, bool factory)
    {
        string where = $"class '{cls.Name}'{(factory ? " (FACTORY)" : "")}";
        // The object (or factory) data — this binder's roots that are not a METHOD's (IsObjectData), in
        // working storage (SR1 has refused every other placement at BindEntry, where the clause stays recorded).
        var objectItems = Roots.Where(r => !MethodScopedRoots.Contains(r)).SelectMany(Flatten).ToList();
        foreach (var subject in objectItems)
        {
            if (subject.Property is not { } pc || subject.Section is not EntrySection.WorkingStorage) continue;
            // SR3's ELEMENTARY half is the placement table's ElementaryOnly row (CheckElementaryOnlyClauses), which
            // runs over the finished forest in BindResolve — after this pass — and reports it. A group defines no
            // accessor meanwhile.
            if (subject.Children.Count > 0) continue;
            using var _ = Edition.At(subject);
            if (subject.CobolName is not { } subjName)
            {
                Edition.Error(DiagnosticCatalog.PropertyClauseUnnamedSubject, $"{where}: a PROPERTY clause requires a named data item "
                    + "(ISO §13.18.42 — FILLER cannot be a property subject)");
                continue;
            }
            string? fault =
                RecordLayout.OccursSubjectOf(subject) is { } table
                    ? (ReferenceEquals(table, subject) ? "it is" : $"it is subordinate to '{table.CobolName ?? "FILLER"}', which is")
                      + " described with an OCCURS clause; the PROPERTY clause shall not be specified for data items "
                      + "subject to an OCCURS clause (ISO §13.18.42.3 SR2)"
                : objectItems.Count(i => CobolNames.Same(i.CobolName, subjName)) > 1
                    ? $"the name '{subjName}' is not unique in the {(factory ? "factory" : "object")} data, so a "
                      + "reference to it requires qualification; the PROPERTY clause may be specified only for an "
                      + "elementary item whose name does not require qualification for uniqueness of reference "
                      + "(ISO §13.18.42.3 SR3)"
                : data.IsConstantRecordItem(subject) && !pc.NoSet
                    ? "it is in a CONSTANT RECORD, which cannot be stored into, and the clause does not say WITH NO "
                      + "SET; if the PROPERTY clause is specified in a data item described with the CONSTANT RECORD "
                      + "clause, or in any data item subordinate to one, the SET phrase shall be specified "
                      + "(ISO §13.18.42.3 SR5)"
                : subject.Pic?.ObjectRef is { Kind: ObjectRefKind.ActiveClass }
                    ? "it is an object reference described with the ACTIVE-CLASS phrase; the PROPERTY clause shall not "
                      + "be specified for data items of usage object reference described with an ACTIVE-CLASS phrase "
                      + "(ISO §13.18.42.3 SR6)"
                : null;
            if (fault is not null)
            {
                Edition.Error(DiagnosticCatalog.PropertyClauseRule, $"{where}: property subject '{subjName}': {fault}");
                continue;
            }
            data.DeclareUserWord(subjName, UserWordKind.PropertyName);   // §8.3.2.2 — the one declaration funnel (kb/Work PB1083)
            // §13.18.42.3 SR4 (a superclass's property-name) is asked by OoClassTable.ResolveOverrides once EVERY roster of
            // the group is complete — asked here, it saw a base's clause accessors only when the base was bound first.

            // §11.7.3 SR5 — "If property-name-1 is specified as a data-name in the working-storage section of the
            // containing object definition, the PROPERTY clause shall not be specified in the data description entry of
            // that data-name": ANY explicit GET or SET PROPERTY method of this name, whatever the clause's WITH NO GET /
            // WITH NO SET says (kb/Work PB1503 — the rule used to be a by-product of an accessor-name COLLISION, so the
            // NO GET + explicit GET pair, which synthesizes no colliding accessor, compiled). The clause then defines no
            // accessor: the explicit method keeps the property.
            if ((factory ? cls.FactoryMethods : cls.Methods).FirstOrDefault(m =>
                    m.Origin is OoSymbolOrigin.Source && CobolNames.Same(m.PropertyName, subjName)) is { } explicitAccessor)
            {
                Edition.Error(DiagnosticCatalog.PropertyMethodRule, $"{where}: METHOD-ID "
                    + $"{(explicitAccessor.Accessor == 'G' ? "GET" : "SET")} PROPERTY {subjName} names a data-name of the "
                    + $"{(factory ? "factory" : "object")} working-storage section that carries the PROPERTY clause; if "
                    + "property-name-1 is specified as a data-name in the working-storage section of the containing "
                    + "object definition, the PROPERTY clause shall not be specified in the data description entry of "
                    + "that data-name (ISO §11.7.3 SR5)");
                continue;
            }

            if (!pc.NoGet)
                AddAccessor('G', NamingConvention.GetAccessorName(subjName));
            if (!pc.NoSet)
                AddAccessor('S', NamingConvention.SetAccessorName(subjName));

            void AddAccessor(char kind, string csName)
            {
                var m = new OoMethodSymbol(csName,
                    hasUsing: kind == 'S', hasReturning: kind == 'G',
                    OoSymbolOrigin.Generated)
                {
                    ClrName = csName, Owner = cls, IsFactory = factory,
                    Accessor = kind, PropertyName = subjName, PropertySubject = subject,
                    IsFinal = pc.IsFinal,
                };
                m.Binding = new OoMethodBinding();   // synthesized accessors carry their signature immediately
                if (kind == 'G') m.Binding.Returning = subject; else m.Binding.Formals.Add(new OoFormal(subject, 0, "__V"));
                // The roster key cannot be taken: an explicit accessor of this name was refused above (SR5), a second
                // subject of this name was refused as needing qualification (§13.18.42.3 SR3), and no METHOD-ID name
                // maps into the `__` accessor family (§11.7.4 GR1 a)'s pinned names).
                if (!(factory ? cls.TryAddFactoryMethod(m) : cls.TryAddMethod(m)))
                    throw new InvalidOperationException(
                        $"{where}: PROPERTY accessor '{csName}' is already on the roster — §11.7.3 SR5 and §13.18.42.3 SR3 "
                        + "were to make that impossible");
            }
        }

        static IEnumerable<DataItem> Flatten(DataItem i)
        {
            yield return i;
            foreach (var c in i.Children)
                foreach (var d in Flatten(c))
                    yield return d;
        }
    }

    /// <summary>Route a method-scoped Tier-B REDEFINES class's ONE string backing to its method LOCAL (M2-OO-1h
    /// step 3): a method LOCAL-STORAGE / LINKAGE canonical → a method LOCAL (suppressed from the class-level field
    /// loop via <see cref="LinkageBinder.SuppressedRootFields"/>, emitted in <c>OoEmitMethod</c>). A method owns no
    /// WORKING-STORAGE (§13.5.3 SR1 — COBOL1519), so there is no static arm. Runs after
    /// <c>ClassifyRedefinesClasses</c>. A subordinate (02) canonical rides its root's composed initializer and
    /// needs no routing (its backing is a member of the method-local root struct already).</summary>
    internal void RouteMethodRedefinesBackings()
    {
        foreach (var root in MethodScopedRoots)
            if (root.Class is { Tier: RedefinesTier.StringCanonical } cls && ReferenceEquals(cls.Canonical, root))
                data.Linkage.SuppressRootField(cls.BackingClrName);   // emitted as a method local
    }
}
