// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Repository;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Binding.Passes;
using WiseOwl.Cobol.Binding.Procedure;
using WiseOwl.Cobol.Frontend.Common;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Frontend.Preprocessor;

using WiseOwl.Cobol.Compiler.Oo;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;
using WiseOwl.Cobol.Runtime;

/// <summary>
/// THE Binder phase (rearch PHASE-06 Step 2): binds a whole compilation group to an immutable
/// <see cref="BoundCompilation"/>, so the driver's Phase 2 is literally <c>Bind → VersionConformancePass → Emit</c>
/// and the emitter consumes the bound result instead of orchestrating binding. This class owns the formerly-hidden
/// second pass pipeline (the binder half of the once-fused bind+emit run-unit entry; the emit half is
/// <c>ProgramEmitter.Emit</c> since P7 Step 9n): TurnState → unit/class collection →
/// OO data/body binding → two-phase per-unit binding (ALL data before ANY procedure — the M2-UDF-1
/// forward-reference enabler) → the middle-end data-model passes (compiler-temp re-sync → UsageCollection →
/// image marking → OO harmonize → StorageForm) → the EC gate → file-connector registry-key qualification.
/// <para>The OO bind bodies live on <see cref="Compiler.Oo.OoDriver"/> (P9 R1 — a real binder collaborator;
/// the former emitter-hosted <c>IOoBindHost</c> seam is deleted). They only mutate binder state, never emit.</para>
/// </summary>
internal sealed class BinderDriver
{
    /// <summary>The binder-side half of this driver's bind (kb/Work PB2361): each source element's data binder and reference
    /// resolver (<see cref="BindSession.Binders"/>) and the group's class table, which the bound model handed to a backend does
    /// not carry. A binder-side reader after the bind (a test of a binder rule) asks it here; read before <see cref="Bind"/>
    /// started, it is an internal error.</summary>
    internal BindSession Session => _session
        ?? throw new InvalidOperationException("BinderDriver.Session was read before Bind made it");
    private BindSession? _session;

    /// <summary>Bind the WHOLE compilation group in <paramref name="tree"/> under the targeted EDITION
    /// (<paramref name="edition"/> — bind-time rejection diagnostics accumulate there; the driver fails the
    /// compile when any exist, BEFORE emit). <paramref name="turnEvents"/> are the frontend's <c>&gt;&gt;TURN</c>
    /// directive events (ISO §2023:7.3.25) — they build the group's compile-time TurnState (EC deep-dive D10);
    /// null/empty means the GR1 default, EC-ALL CHECKING OFF.</summary>
    /// <param name="repository">The external repository the group's specifiers resolve against when the group cannot
    /// answer them (ISO §2023:12.3.8.4 GR10 c) / GR11 c); DESIGN-external-repository §8.6, §9.3); none when null.</param>
    public BoundCompilation Bind(Core.CompilationUnitContext tree, EditionContext edition,
        Frontend.Preprocessor.DirectiveResults? directives = null, RepositorySearchSpace? repository = null,
        ExpansionIdentity? publishAs = null)
    {
        BindPipeline.ValidateFullChainOnce();   // the startup DAG assert over resolve prefix + group tail
        // The frontend's directive outputs travel as ONE record (kb/Work PB65); absent ⇒ every directive's default.
        // §2023:14.9.28.4 GR14's implicit PUSH ALL / POP ALL around every exception-checking PERFORM's handlers are
        // already replayed into every event timeline: the front end places them, because the conditional-compilation
        // driver's state needs them too (kb/Work PB1004, PB1066 — Frontend.Parse).
        directives ??= Frontend.Preprocessor.DirectiveResults.None;
        var turnEvents = directives.TurnEvents;
        var refModZlEvents = directives.RefModZeroLengthEvents;
        var flagEvents = directives.FlagEvents;
        var cobolWordsMap = directives.CobolWordsMap;

        // The LEVEL-NUMBER screen (ISO §2023:13.18.33.3) — run FIRST, before any binding. It is a pure SYNTAX rule
        // (a token plus the entry's section ancestry) and needs nothing the binder produces, while a level-number
        // outside its section's permitted set makes the storage tree it heads meaningless: `78 K VALUE 5.` used to
        // bind as a memberless group nested under the preceding entry and survive to a RUN-time abort. Screening
        // first means the user sees the level-number error instead of that cascade, and no bind-time failure on the
        // malformed structure can preempt it. The three sibling passes further down run POST-bind only because they
        // consume the bound model; this one has no such reason to wait. kb/Work PB485.
        global::WiseOwl.Cobol.Validation.LevelNumberPass.Run(tree, edition);
        // COBOL-85's areas A and B (kb/Work PB3147): where each token begins its fixed-form line, against the item it
        // heads. Pre-bind for the level-number screen's reason — a pure syntax rule over the raw tree — and silent from
        // COBOL-2002 on, which has no areas (ISO §2023:6.3.1).
        global::WiseOwl.Cobol.Validation.AreaPlacementPass.Run(tree, edition);
        // ISO §2023:5.5 1) — every integer-n is NONZERO unless an associated rule says otherwise (kb/Work PB859). Pre-bind
        // for the level-number screen's reason: a pure syntax rule over the raw tree, and a zero OCCURS bound left to
        // the binder used to reach Roslyn as CS0029 in generated C#.
        global::WiseOwl.Cobol.Validation.IntegerOperandPass.Run(tree, edition);
        // The procedure division's general-format screens the superset grammar leaves to a named refusal (kb/Work
        // PB351, PB1146): a conditional statement as an imperative-statement operand (§2023:14.5.1) and a body mixing
        // §2023:14.2.1 Format 1 and Format 2 (§2023:14.4.1). Pre-bind for the same reason: pure syntax over the raw tree.
        global::WiseOwl.Cobol.Validation.ProcedureFormatPass.Run(tree, edition);
        // ISO §2023:10.7.3 — every end marker against the definition it ends, and SR1's presence rule (kb/Work PB988).
        // Pre-bind for the same reason: a pure syntax rule over the raw tree, asked once of EVERY definition the
        // source states (parameterized skeletons included) before the OO table renames anything.
        global::WiseOwl.Cobol.Validation.EndMarkerPass.Run(tree, edition);
        // WHERE a directive may be written, for the placement rules that need the parse tree (kb/Work PB1005, PB1065,
        // PB1377, PB1378): PUSH ALL / POP ALL (§2023:7.3.22.3 SR3, §2023:7.3.20.3 SR3), FLAG-02 / FLAG-14 (§2023:7.3.14.3 SR1,
        // §2023:7.3.15.3 SR1) between clauses and statements; LEAP-SECOND / PROPAGATE outside every compilation unit
        // (§2023:7.3.17.3 SR1, §2023:7.3.21.3 SR1); a PUSH/POP naming one inherits its rule (§2023:7.3.20.3 SR2, §2023:7.3.22.3 SR2). Each
        // rule is data on the directive's the construct catalogue row; a pure position rule over the tree and the directive
        // sites, with no tree walk unless the source has a site the pass judges.
        global::WiseOwl.Cobol.Validation.DirectivePlacementPass.Run(tree, directives.DirectiveSites, edition);

        // The group's compile-time TurnState (ISO §2023:7.3.25; deep-dive D10) — built BEFORE binding so every unit's
        // statement binder folds the same source-ordered directive events (GR6: checking spans the compilation
        // group). Name/edition validation happens here (SR2 + the 2023-only families).
        var turn = TurnState.Build(turnEvents, edition);
        // The group's compile-time REF-MOD-ZERO-LENGTH resolution (ISO §2023:7.3.23) — the per-line zero-length
        // allowance fold every ReferenceResolver queries when building a ref-mod Place (§2023:8.4.3.3.4 item 5c).
        var refModZl = RefModZeroLengthState.Build(refModZlEvents);
        // The group's compile-time >>PROPAGATE resolution (ISO §2023:7.3.21; kb/Work PB1119) — folded once per source
        // element below (program units in BindUnitData, methods in the roster loop).
        var propagate = PropagateState.Build(directives.PropagateEvents);

        // The external repository's provider exists BEFORE the class table: the table asks it for a class a REPOSITORY specifier
        // names that no definition of the group claims (ISO §2023:12.3.8.3 SR6 b; DESIGN-external-repository §8.4 step 3), while it
        // resolves INHERITS and IMPLEMENTS in pass 1. Zero cost when unused: nothing is listed or opened until a question.
        var searchSpace = repository ?? RepositorySearchSpace.None;
        var external = new MetadataRepositoryProvider(searchSpace, edition);
        var words = cobolWordsMap ?? WiseOwl.Cobol.Editions.CobolWordsMap.Empty;
        var leapSecond = LeapSecondState.Build(directives.LeapSecondEvents);
        // The compile-time inputs a skeleton is published under and an expansion of one is made under (CompileState, DESIGN-external-repository §6.1).
        var compileInputs = new CompileInputs(edition, words, turn, propagate, refModZl, leapSecond, directives.CompilationVariables);
        var (units, classes, table, texts, sources) = CollectUnits(tree, edition, compileInputs, external);
        external.UseClassTable(table);
        var session = new BindSession
        {
            External = external,
            Turn = turn, OoClasses = table, Edition = edition, RefModZeroLength = refModZl, Propagate = propagate,
            DirectiveSites = directives.DirectiveSites,
            CobolWords = words,
            Retypes = tree.TokenRetypes,
            LeapSecond = leapSecond,
            CompilationVariables = directives.CompilationVariables,
            SearchSpace = searchSpace,
            Sources = sources,
        };
        _session = session;
        var oo = new OoDriver(session);   // P9 R1 — the OO bind driver is a binder collaborator, not an emitter seam
        foreach (var iface in table.Interfaces) oo.BindInterfaceData(iface);   // prototype formals (§2023:10.6.2 SR4)
        foreach (var cls in classes) oo.BindClassData(cls);   // ALL signatures before ANY body (D1 pass-1)
        // OVERRIDE / FINAL over COMPLETE rosters — a PROPERTY clause's accessors exist only once its class's data has
        // bound (ISO §2023:13.18.42.4 GR1/GR2; kb/Work PB1274), and every signature comparison below reads the marking.
        table.ResolveOverrides(edition);
        // The PARAMETERIZED definitions nothing expands bind as themselves, for their own rules, after every emitted
        // class (their uid bands follow) and outside every later pass: nothing emits a skeleton (kb/Work PB2051; OO
        // deep-dive D12). An expanded one is bound through its expansions only (OoExpansion.Result.ExpandedNames).
        foreach (var iface in table.ParameterizedInterfaces) oo.BindParameterized(iface);
        foreach (var cls in table.ParameterizedClasses) oo.BindParameterized(cls);
        // The checking slices of every class half and every method (kb/Work PB2296; CheckingFold), folded once every method
        // symbol exists and before any body binds: §2023:14.9.23.4 GR7 c)'s METHOD-side half at the method's PROCEDURE
        // DIVISION HEADER (TurnState.EnabledAtHeader; kb/Work PB1381), §2023:14.8.4.1's activated-method EC-EXTERNAL mask and
        // §2023:7.3.21.4's automatic propagation; and the group-level Describe gate, which reaches the class halves too (kb/Work
        // PB1138): a method activation registers its factory's or object's external descriptions (§2023:14.9.23.4 GR7 d)).
        foreach (var cls in table.Classes)
            foreach (var m in cls.Methods.Concat(cls.FactoryMethods))
                m.Checking = CheckingFold.Method(turn, propagate, m, cls, sources);
        foreach (var cls in classes)
        {
            session.Binders.ObjectOf(cls).Data.State.UnitChecking = CheckingFold.ClassHalf(turn);
            session.Binders.FactoryOf(cls).Data.State.UnitChecking = CheckingFold.ClassHalf(turn);
        }

        // TWO-PHASE binding (M2-UDF-1 key enabler): EVERY unit's DATA division binds before ANY procedure body
        // binds — a class's method bodies (below) as well as a program unit's (the ProcedureBinding group pass) —
        // so a function-identifier reference resolves the callee's RETURNING / USING signatures from a unit whose
        // DATA is bound, wherever in the group that unit sits (§2023:8.4.3.2.4 GR1 — the caller-side temporary takes the
        // callee's RETURNING description; the same forward-reference discipline OoClassTable D1 gives typed object
        // references). WHICH unit a REPOSITORY entry may take its details from is §2023:12.3.8.4 GR10 / GR11's order
        // rule, not this phase's: a definition that FOLLOWS the element is not "specified previously" and a
        // prototype ahead of it stands in (GroupRepositoryProvider, kb/Work PB989).
        foreach (var unit in units) BindUnitData(unit, session);
        // §2023:12.3.8.3 SR16, once every roster is complete (the classes' data bound above, ResolveOverrides run) and every REPOSITORY
        // paragraph seen: a PROPERTY specifier needs a property of the classes and interfaces of its own paragraph (kb/Work PB2104).
        table.CheckPropertySpecifiers((entry, word, externalized) =>
        {
            using var at = edition.At(entry);
            edition.Error(DiagnosticCatalog.RepositorySpecifierUnanswered,
                $"REPOSITORY PROPERTY {word}{(externalized is null ? "" : $" AS \"{externalized}\"")} names no property: no class or interface "
                + $"declared in the same REPOSITORY paragraph has an accessor of the property \"{externalized ?? word}\", in the compilation group "
                + "or in the external repository — ISO §2023:12.3.8.3 SR16");
        });
        // The group's REPOSITORY resolution tables (§2023:12.3.8.4 GR10 / GR11), built once every DATA division is
        // bound and BEFORE the first body: a CLASS definition's REPOSITORY reaches its methods (§2023:12.3.4 GR1 — "apply
        // to each directly or indirectly contained source unit"), and until kb/Work PB1100 the class bodies bound
        // first, against no table, so every user-function reference in a method drew COBOL1505.
        session.Repository = BuildRepositoryResolver(units, classes, session);
        // The unexpanded parameterized definitions' forests are resolved with the rest (kb/Work PB2937): their method
        // bodies bind below, and a restricted pointer in them is resolved to the prototype it names exactly as anywhere.
        ResolvePointerRestrictions(units, [.. classes, .. oo.SkeletonClasses],
            [.. session.Binders.Interfaces, .. oo.SkeletonInterfaceData], session);
        // ⛔ EVERY SIGNATURE COMPARISON RUNS AFTER THE POINTER RESTRICTIONS ARE RESOLVED (kb/Work PB2495): a formal or
        // returning item that is a restricted program- or function-pointer is described by the EXTERNALIZED name of the
        // prototype its TO phrase names (ISO §2023:13.18.60.4 GR25 / GR26; kb/Work PB4107, PB4491), which
        // ResolvePointerRestrictions has just attached. A check that runs before it reads the word as WRITTEN, and refuses
        // an override, an implementation or a prototype / definition pair whose two units name ONE prototype by two words
        // (a REPOSITORY specifier's AS phrase, §2023:12.3.8.2). Running after the DATA phase is NOT enough:
        // CheckPrototypeSignaturePairs refused such pairs from inside the resolver's builder, after every BindUnitData. So
        // each check is a top-level statement of Bind, after this call, and PrototypePointerTypeDriftTests pins that
        // execution order.
        CheckPrototypeSignaturePairs(units, session.Sources, session.Edition, session.Binders);   // §2023:10.6.2 SR2 / SR3
        // ISO §2023:8.13's "flag differences in prototypes and definitions in the compilation group from the information in
        // the external repository" (--repository-check, REPO-9 / REPO-12; DESIGN-external-repository §9.2) — after the
        // pointer types exist, as every signature comparison is.
        RepositoryCheck.Run(units, session);
        OoConformance.ValidateInterfaceInheritance(table, sources, edition);   // §2023:11.6.3 SR5 — an inheriting interface conforms to all it inherits (kb/Work PB1502)
        OoConformance.ValidateOverrideSignatures(table, sources, edition);   // §2023:9.3.8.2 — after all formals resolve (slice 3a)
        OoConformance.ValidatePropertyAccessorPairs(table, sources, edition);   // §2023:8.4.3.9.3 SR7 — get RETURNING = set USING, over the formals just resolved
        var ooAdapters = OoConformance.ValidateImplements(table, sources, edition);   // §2023:9.3.11 via §2023:9.3.8.2.3 (D-I1 — the binder is the authority; returns the covariant adapters)
        foreach (var cls in classes) oo.BindClassBody(cls);
        // The method BODIES of a parameterized definition nothing expands bind as themselves too, after every emitted
        // class's (kb/Work PB2937; OO deep-dive D12 determination 6): every rule of their text is asked, and a statement
        // that selects a method through a parameter-name is each expansion's (BoundDeferredToExpansion). Nothing emits
        // them; the edition pass walks them (VersionConformancePass.Run).
        foreach (var skeleton in oo.SkeletonClasses) oo.BindClassBody(skeleton);

        // The whole-group middle-end (P6 Steps 3–4): the DECLARED manifest — ProcedureBinding →
        // UsageCollectionPass → StorageFormPass → VersionConformancePass (the NAMED terminal pass, the SOLE
        // edition gate; each pass's doc lives with its body; the ORDER lives in BindPipeline.GroupTail,
        // DAG-validated against the resolve prefix above). Once the tail completes, the edition sink carries
        // EVERY edition diagnostic — the driver's CheckOnly verdict needs nothing beyond this Bind.
        var ctx = new GroupBindContext(tree, texts, units, classes, session.Binders.Interfaces, oo.SkeletonClasses, oo.SkeletonInterfaceData, session);
        foreach (var pass in BindPipeline.GroupTail())
        {
            RequireAll(ctx, pass);      // watermark gate: the prerequisite RAN on every binder (P6 Step 6)
            pass.Run(ctx);
            foreach (var d in ctx.AllBinders()) d.State.MarkProduced(pass.Produces);
        }

        // The migration-flagging pass (ISO §2023:7.3.14 FLAG-02 / §2023:7.3.15 FLAG-14) — a SIBLING to the terminal
        // VersionConformancePass, run right after it: it is an orthogonal axis (directive-state-driven, always a
        // Warning, fires regardless of --std), so it is NOT a GroupTail manifest pass. A no-op (no parse-tree walk)
        // when the source carries no >>FLAG directive — the zero-overhead invariant.
        global::WiseOwl.Cobol.Validation.FlagConformancePass.Run(ctx, FlagState.Build(flagEvents), edition);

        // The expression-formation pass (ISO §2023:8.8.1.2 Table 3 / §2023:8.8.2 Table 4) — a THIRD sibling on the same
        // "orthogonal axis ⇒ separate pass" precedent. It is not edition gating (the tables have no introducedIn:
        // an invalid symbol pair is invalid in 1985, 2002, 2014 and 2023 alike) and not directive-state flagging,
        // so neither existing pass's charter covers it. It walks the RAW parse tree once, never once per binding
        // site; the rule itself is the shared frontend ArithmeticFormationRules, which the compile-time
        // expression evaluator invokes on its own trees. kb/Work PB158.
        global::WiseOwl.Cobol.Validation.ExpressionFormationPass.Run(ctx, edition);

        // The literal-syntax pass (ISO §2023:8.3.3.2.3 SR1/SR6, §2023:8.3.3.4.3 SR1, §2023:8.3.3.5.3 SR1/SR5) — a sibling on the
        // same footing: the length and hexadecimal-grouping rules are properties of the literal TOKEN, whatever
        // position it is written in, so they are asked once per token of the raw tree rather than once per
        // consuming funnel (which is how VALUE / CONSTANT / ALL / concatenation-operand literals escaped them).
        // kb/Work PB1393. Over every text the group compiles (GroupBindContext.Texts), the text of an expansion of a skeleton read from the
        // repository too, whose tokens name their DiagnosticRegion so a report is attributed as the group's other screens attribute it (kb/Work PB4649).
        foreach (var text in ctx.Texts)
            global::WiseOwl.Cobol.Frontend.Parsing.LiteralScreen.Run(text, edition.Edition, edition, gateFormats: true, edition.ClientWritten);

        // The word-character screen (ISO §2023:8.3.2.1, §2023:8.1.3.2 GR4, Annex B.3) — the character half of the COBOL-word rule is a
        // property of the word TOKEN, whatever position it is written in, so it is asked once per word of the tree rather
        // than once per name slot. A compilation whose frontend reported ANY error (a failed parse, or a parse beside a
        // separator, PICTURE or preprocessor error) never reaches the binder and was screened by the frontend over the
        // lexer's whole stream (the same body), so this is the clean frontend's half. kb/Work PB3845. Each text of the group is screened as
        // a compilation of its own, so the once-per-word rule is per text (a word an expansion's text raises is not hidden by the same word in
        // another text), the expansion of a skeleton read from the repository included (kb/Work PB4649).
        foreach (var text in ctx.Texts)
            global::WiseOwl.Cobol.Frontend.Parsing.WordCharacterScreen.RunOnTree(text, edition.Edition, edition, edition.ClientWritten);

        // The declined-optional-element pass (ISO Annex A.4.1) — a THIRD sibling on the same footing: it answers
        // "does this implementation claim support for this optional module", which is orthogonal to both the
        // edition axis (VersionConformancePass) and the directive axis (FlagConformancePass). It refuses the
        // DATA DIVISION surfaces of the declined A.4.14 VALIDATE facility by name (COBOL1708); the declined
        // STATEMENTS keep their own bind-time
        // recognize-and-warn arm, and the declined modules' EXCEPTION-NAMES are refused in the one written-name
        // funnel (EcNameResolution → COBOL1710). A no-op below COBOL-2002.
        global::WiseOwl.Cobol.Validation.DeclinedFacilityPass.Run(ctx, edition);

        // The closed-general-format pass (ISO §2023:4.2.2) — a FOURTH sibling on the same footing, and the one that
        // ends the grammar's last total sink. `genericClause : IDENTIFIER (IDENTIFIER|literal)*` was reached from
        // six sites spanning eight closed general formats and swallowed any word run the format did not define,
        // at every edition and every strictness; it now spells the error production at every site (`unrecognizedClause`,
        // or `unrecognizedDataClause` in the data division, kb/Work PB3390) and this pass
        // refuses each instance by name, naming the format and its § from ClosedFormats.ByContext (COBOL1941
        // for §2023:13.16.2, 1970 for the clause-list formats, 1971 for the paragraph-list ones). Runs at EVERY
        // edition — unlike the declined pass there is no edition below which the sink cannot fire. kb/Work PB829.
        global::WiseOwl.Cobol.Validation.ClosedFormatPass.Run(ctx, edition);

        // The data binding is done (DESIGN-architecture-review §8.3 (1), step 11; kb/Work PB2361): the group tail has run, so the
        // procedure binders' two late doors (a compiler temporary's root, a function result temporary's cell) are shut and every
        // fact set is complete. Each element's DataBindResult — what a backend reads in place of the binder — is made here, once;
        // everything below reads the bound model, and the binders stay with the session.
        var interfaceData = session.Binders.Freeze(units, classes);

        // The group EC gate: ANY use of the EC model (an enabling TURN, a RAISE/RESUME/F3/RAISING, an
        // EXCEPTION-* function) turns the machinery on; otherwise the generated source is byte-identical to a
        // pre-EC build (the zero-scaffolding invariant, SSOT §18.16).
        bool ecActive = turn.AnyEnabled || units.Any(u => u.Bound.Ec is { Any: true })
            || classes.Any(c => c.Bound.Ec is { Any: true } || c.FactoryBound.Ec is { Any: true });

        // Per-program file-connector namespace (moved from the emit half — a BIND-phase model fact, so no CodeGen
        // write into the binding model remains; P6 exit criterion #2): the runtime file registry is
        // run-unit-global, but a file connector is INTERNAL to its program (ISO §2023:8.6.3): two programs declaring
        // the same file-name (the IC-suite PRINT-FILE pattern, e.g. IC101A's two units) must not clobber each
        // other's connectors. Name resolution is done (bound nodes hold FileModel references), so qualifying the
        // runtime key is purely a rename. An EXTERNAL FD instead keys by its run-unit EXTERNALIZED name
        // (ISO §2023:13.18.22.4 GR4a: ONE external file connector per run unit, shared by every describer — two units'
        // FileModels with the same external name converge on ONE registry key, hence one connector; GR5: the name
        // is the FD name). Each FileModel lives in exactly ONE unit's Files list (a container's GLOBAL file is
        // reached through the contained unit's VisibleFiles only), so no model is renamed twice.
        foreach (var unit in units)
            foreach (var file in unit.Data.Files.Declared)
                file.CobolName = file is { IsExternal: true, ExternalName: { } ext }
                    ? NamingConvention.ExternalFileBand + ext
                    : unit.RegistryKey + "::" + file.CobolName;
        foreach (var unit in units)
            foreach (var cd in unit.Data.Communication.Declared)
                cd.Key = unit.RegistryKey + "::" + cd.Name;

        // VCR 18/31 (ISO §2023:12.4.5.3 GR1(i)/(h); §2023:14.8.4.2; Annex E.2 items 12/24) — a COBOL-2023 requirement: all
        // corresponding file control entries of an EXTERNAL file connector in the run unit shall specify FILE STATUS
        // (VCR 18) and, for a relative file, RELATIVE KEY (VCR 31) naming the SAME corresponding external data item.
        // Version-conditioned structural SR ⇒ read DialectLevel directly (the binder-reads-edition doctrine). The
        // compile-time reach is the group's units only; true separate-compilation conformance is the VCR-15 runtime
        // EC-EXTERNAL-DATA-MISMATCH descriptor check.
        if (edition.DialectLevel >= 2023) CheckExternalFileConsistency(units, edition);
        // The OO analogue (M2-OO-1i): an OBJECT/FACTORY file connector is scoped to its class, not a program unit,
        // so the program loop above never sees it. A factory or instance file keys per object (a minted key held
        // in a __fkey field — see QualifyClassFiles); an EXTERNAL class file keys
        // by its run-unit external name, exactly like a program's.
        foreach (var cls in classes) QualifyClassFiles(cls);

        // Declaratives emit the __IoCheck/__RunUse machinery, which reads CobolFile even when the unit declares
        // NO files (IC401M: mode-scoped USE procedures in a file-less flagging program) — the IO using must
        // cover both. A class-only file program (M2-OO-1i — an OBJECT/FACTORY file with no program-unit file)
        // needs it too, or the generated <c>CobolFile.Register</c>/OPEN in the class body has no WiseOwl.Cobol.Runtime.IO
        // import (CS0103). So does a file-less unit whose statements carry EC-I-O checking: its __IoCheckEc reads
        // CobolFile too, and a COMMIT or ROLLBACK raises EC-I-O-PERMANENT-ERROR for a refused file of ANOTHER element's
        // APPLY COMMIT clause in an element that describes no file (kb/Work PB3338, review finding w1154a R1-3).
        bool anyFiles = units.Any(u => u.Data.Files.Declared.Count > 0)
            || units.Any(u => u.Bound.Declaratives is { Count: > 0 })
            || units.Any(u => u.Bound.Ec is { HasIoChecked: true })
            || classes.Any(c => c.Data.Files.Declared.Count > 0 || c.FactoryData.Files.Declared.Count > 0)
            // a METHOD's own declaratives emit the same __IoCheck (kb/Work PB1010)
            || classes.Any(c => (c.Bound.Methods ?? []).Concat(c.FactoryBound.Methods ?? []).Any(m => m.Declaratives.Count > 0));

        // The group is bound: what it publishes for its parameterized definitions, and where each expansion lives (§2023:10.5).
        var expansions = ExpansionPlacementPass.Run(table, sources, classes, session.Binders, external, edition, compileInputs, publishAs);
        // What the module publishes (ISO §2023:8.13; DESIGN-external-repository §7.2, §9.1; kb/Work PB4319): the entry units always, and
        // under --repository-update on each published unit's record, encoded LAST, over the finished bound data, so the backend
        // carries a value across the seam and never runs the encoder. A group the binder refused is never emitted (the driver
        // stops at its diagnostics), so it publishes nothing and the encoder never reads a tree the binder did not accept.
        var entryUnits = RepositoryPublication.EntryUnitsOf(units);
        bool publishes = edition.RepositoryUpdate && !edition.HasErrors;
        var interfaceRecords = publishes
            ? RepositoryPublication.Publish(entryUnits, classes, table, expansions, session.Binders)
            : new Dictionary<OoInterfaceSymbol, RepositoryRecord>();
        // The answers the backend reads of the binder's rule families (the activation descriptions, the RAISING resolution, an
        // external file's entry identity), written over the finished bind, so no backend asks the class table (kb/Work PB2403).
        BackendAnswers.Write(units, classes, table, entryUnits);
        return new BoundCompilation(units, classes, table.Interfaces, interfaceData, ooAdapters,
            new ImportedTypes(table.ImportedClasses, table.ImportedInterfaces), turn, ecActive,
            anyFiles, edition.DialectLevel, edition.SignEncoding, expansions,
            entryUnits, publishes, interfaceRecords);
    }

    /// <summary>The ≥2023 external-file conformance check (ISO §2023:14.8.4.2; §2023:12.4.5.3 GR1(i)/(h); Annex E.2 items 9/12/24).
    /// Two conjuncts of §2023:14.8.4.2. <b>Conjunct 1 — externality (COBOL1624, per connector, ANY describer count):</b>
    /// a specified FILE STATUS / RELATIVE KEY / LINAGE data item shall itself be an external data item. <b>Conjunct 2 —
    /// consistency (VCR 18/31, COBOL1573/1575, needs ≥2 in-group describers):</b> "if any specifies it, all shall",
    /// each naming the SAME corresponding external data item. Cross-compilation sameness (separately-built assemblies)
    /// is the runtime <c>ExternalTable</c> EC-EXTERNAL-DATA-MISMATCH check's face. (In-group LINAGE consistency,
    /// §2023:13.4.5.4 GR2(c), is a separate longstanding requirement — not this 2023-gated check.)</summary>
    private static void CheckExternalFileConsistency(IReadOnlyList<BoundUnit> units, EditionContext edition)
    {
        var byExternalName = units.SelectMany(u => u.Data.Files.Declared)
            .Where(f => f is { IsExternal: true, ExternalName: not null })
            .GroupBy(f => f.ExternalName!, CobolNames.Comparer);
        // The 2023 requirement REMOVES a prior-edition freedom (corresponding external-file SELECTs could name
        // inconsistent / non-external FILE STATUS + RELATIVE KEY items), so its severity follows the removal policy:
        // an Error under strict, downgraded to a Warning under --permissive migration mode — the same contract the
        // continuity witnesses need (an 85 NIST program that violates the new rule, e.g. IC227A with two differently-
        // named non-external FILE STATUS items, still COMPILES under permissive and rejects under strict).
        var severity = EditionSeverityPolicy.For(ConstructAvailability.Removed, edition.Edition);
        // §2023:14.8.4.2 conjunct 1 — EXTERNALITY: a FILE STATUS / RELATIVE KEY / LINAGE data item associated with an
        // external file connector shall ITSELF be an external data item. One shared clause-parameterized diagnostic
        // (the §2023:14.8.4.2 sentence names all three items together); the E.2-item-9 conformance-checking mechanism is
        // the 2023 addition, so it inherits the same Removed-freedom severity as the 1573/1575 consistency siblings.
        EditionDiagnostic Externality(string ext, string clause, string name) =>
            new(DiagnosticCatalog.ExternalFileItemNotExternal.Code, severity, DiagnosticCatalog.ExternalFileItemNotExternal.Id,
                $"external file '{ext}': the {clause} data item '{name}' shall be an external data item "
                + "(ISO §2023:14.8.4.2; Annex E.2 item 9)",
                $"external file '{ext}'", "ISO §2023:14.8.4.2 / Annex E.2 item 9");
        foreach (var group in byExternalName)
        {
            string externalName = group.Key.ToUpperInvariant();   // the name as the diagnostics show it; equality was the fold's above
            var conns = group.ToList();
            // Conjunct 1 (externality) is enforced for EVERY external connector, regardless of describer count — a
            // lone-program external file whose file-referencing item is non-external is a §2023:14.8.4.2 violation.
            // ⛔ EVERY REPORT HERE IS POSITIONED (kb/Work PB2034): this is a group-level pass, so no walker has set the
            // diagnostic cursor, and an EditionDiagnostic carries none of its own (the Editions assembly sits below the
            // frontend's SourceLocation). The cursor is the position, and each report is made at the clause it is about.
            foreach (var f in conns)
            {
                if (f.FileStatusName is not null && ExternalItemIdentity(f.FileStatusItem) is null)
                    using (edition.At(f.ClauseOrEntryAt(f.FileStatusAt)))
                        edition.Report(Externality(externalName, "FILE STATUS", f.FileStatusName));
                if (f.Organization == FileOrganization.Relative && f.RelativeKeyName is not null
                    && ExternalItemIdentity(f.RelativeKeyItem) is null)
                    using (edition.At(f.ClauseOrEntryAt(f.RelativeKeyAt)))
                        edition.Report(Externality(externalName, "RELATIVE KEY", f.RelativeKeyName));
                if (f.Linage is { } lin)
                    foreach (var op in lin.Operands)
                        if (op.DataName is not null && ExternalItemIdentity(op.Item) is null)   // literal operands are exempt
                            using (edition.At(f.ClauseOrEntryAt(op.At)))
                                edition.Report(Externality(externalName, "LINAGE", op.DataName));
            }
            // Conjunct 2 (cross-unit CONSISTENCY, §2023:12.4.5.3 GR1(h)/(i)) needs ≥2 in-group describers to reconcile; the
            // single-describer externality face is enforced above, and cross-compilation sameness stays with the
            // ExternalTable runtime EC-EXTERNAL-DATA-MISMATCH check.
            if (conns.Count < 2) continue;

            // VCR 18 — FILE STATUS: if ANY corresponding SELECT specifies FILE STATUS, ALL shall, each naming the
            // same corresponding external data item (§2023:12.4.5.3 GR1(i)).
            if (conns.Any(f => f.FileStatusName is not null))
            {
                var ids = conns.Select(f => ExternalItemIdentity(f.FileStatusItem)).ToList();
                if (ids.Any(id => id is null) || ids.Distinct(StringComparer.Ordinal).Count() > 1)
                {
                    var odd = conns[DeviatingDescriber(ids)];
                    using (edition.At(odd.ClauseOrEntryAt(odd.FileStatusAt)))
                        edition.Report(new EditionDiagnostic(DiagnosticCatalog.ExternalFileStatusConsistency.Code, severity,
                            DiagnosticCatalog.ExternalFileStatusConsistency.Id,
                            $"external file '{externalName}': all corresponding SELECT statements in the run unit shall "
                            + "specify FILE STATUS naming the same corresponding external data item "
                            + "(ISO §2023:12.4.5.3 GR1(i); Annex E.2 item 12)",
                            $"external file '{externalName}'", "ISO §2023:12.4.5.3 GR1(i) / §2023:14.8.4.2 / Annex E.2 item 12"));
                }
            }

            // VCR 31 — RELATIVE KEY: for an external RELATIVE file, if ANY corresponding SELECT specifies RELATIVE
            // KEY, ALL shall, each naming the same corresponding external data item (§2023:12.4.5.3 GR1(h)).
            if (conns.Any(f => f.Organization == FileOrganization.Relative)
                && conns.Any(f => f.RelativeKeyName is not null))
            {
                var ids = conns.Select(f => ExternalItemIdentity(f.RelativeKeyItem)).ToList();
                if (ids.Any(id => id is null) || ids.Distinct(StringComparer.Ordinal).Count() > 1)
                {
                    var odd = conns[DeviatingDescriber(ids)];
                    using (edition.At(odd.ClauseOrEntryAt(odd.RelativeKeyAt)))
                        edition.Report(new EditionDiagnostic(DiagnosticCatalog.ExternalRelativeKeyConsistency.Code, severity,
                            DiagnosticCatalog.ExternalRelativeKeyConsistency.Id,
                            $"external relative file '{externalName}': all corresponding SELECT statements in the run unit "
                            + "shall specify RELATIVE KEY naming the same corresponding external data item "
                            + "(ISO §2023:12.4.5.3 GR1(h); Annex E.2 item 24)",
                            $"external relative file '{externalName}'", "ISO §2023:12.4.5.3 GR1(h) / §2023:14.8.4.2 / Annex E.2 item 24"));
                }
            }
        }
    }

    /// <summary>The describer a "if any specifies it, all shall, naming the same item" diagnostic is about: the first
    /// that names no external item, else the first whose item differs from the first describer's. Callers have
    /// already established that one exists (<paramref name="identities"/> has a null or two distinct values).</summary>
    private static int DeviatingDescriber(IReadOnlyList<string?> identities)
    {
        for (int i = 0; i < identities.Count; i++)
            if (identities[i] is null) return i;
        for (int i = 1; i < identities.Count; i++)
            if (!string.Equals(identities[i], identities[0], StringComparison.Ordinal)) return i;
        return 0;
    }

    /// <summary>The "corresponding external data item" identity of <paramref name="item"/> (ISO §2023:14.8.4.2): the
    /// dotted qualified path from its EXTERNAL root (the root carries the EXTERNAL clause or an external TYPE), i.e.
    /// the externalized name plus the sub-path — null if the item is absent or is NOT an external data item. Two
    /// file-referencing items are the SAME corresponding external item iff their identities are equal. (Keys by the
    /// data-name, consistent with the run-unit ExternalStore cell key; the rare AS-literal externalized name is not
    /// honored there either.)</summary>
    internal static string? ExternalItemIdentity(DataItem? item)
    {
        if (item is null) return null;
        var path = new List<string>();
        var r = item;
        while (r.Parent is not null) { path.Add(r.CobolName ?? "?"); r = r.Parent; }
        if (!(r.HasExternalClause || r.ExternalFromType)) return null;   // root is not an external data item
        path.Add(r.CobolName ?? "?");
        path.Reverse();
        return CobolNames.UpperFold(string.Join(".", path));
    }

    /// <summary>Flatten the compilation group into the ordered unit lists — top-level program units in source
    /// order, each followed by its contained programs (containers precede containees; load-bearing for GLOBAL
    /// inheritance), plus the group's CLASS-ID units (the Phase-3 OO spine). The pass-1 class symbol table
    /// (deep-dive D1) is built HERE — before ANY unit binds — so a driver's typed object references and INVOKEs
    /// resolve classes defined later in the file. A contained <c>nestedProgram</c> parse context is re-shaped
    /// into a synthetic <c>programUnit</c> context (identical child shape) so the per-unit binders consume one
    /// context type.</summary>
    private static (List<BoundUnit> Programs, List<OoClassUnit> Classes, OoClassTable Table, IReadOnlyList<ParserRuleContext> Texts, SourceMap Sources) CollectUnits(
        Core.CompilationUnitContext tree, EditionContext edition, CompileInputs inputs, IOoImporter importer)
    {
        var variables = inputs.Variables;
        var all = new List<BoundUnit>();
        var sources = new SourceMap();
        var usedClassNames = new HashSet<string>(StringComparer.Ordinal);
        var classDefs = new List<Core.ClassDefinitionContext>();
        var ifaceDefs = new List<Core.InterfaceDefinitionContext>();

        foreach (var group in tree.compilationGroup())
        {
            PrototypeUnitRules.ScreenOrder(group, edition);   // §2023:10.6.2 SR1 (kb/Work PB894)
            classDefs.AddRange(group.classDefinition());
            ifaceDefs.AddRange(group.interfaceDefinition());   // §2023:11.6 — collected, NEVER silently dropped (the W2 rule)
            foreach (var pu in group.programUnit())
                Collect(pu, null);
        }
        // §2023:9.3.12 / §2023:9.3.13 (kb/Work PB759): a parameterized definition is a SKELETON, never a class; each
        // REPOSITORY EXPANDS phrase creates one, and from here on an expansion is an ordinary definition.
        var expanded = OoExpansion.Expand(tree, classDefs, ifaceDefs, edition, inputs.Words, importer, inputs);
        var table = OoClassTable.Build(expanded, sources, edition, variables, importer);
        var classes = table.Classes.Select(sym => new OoClassUnit { Symbol = sym }).ToList();
        // The texts this compilation screens (GroupBindContext.Texts, kb/Work PB4483): the written tree, then each expansion of a skeleton
        // read from the repository whose recorded state binds as this group's at the entry. Under another state the group's screens of the
        // text would not say what the skeleton's own compilation said (an edition gate, a permissive downgrade); such an expansion is
        // never compiled here (its own assembly, an imported one, or COBOL2240: ExpansionPlacementPass), and its own compilation screens it.
        List<ParserRuleContext> texts = [tree, .. expanded.RepositoryTexts()
            .Where(e => e.Origin.Skeleton.State.BindsAs(inputs.At(e.Origin.Entry.Start.Line))).Select(e => e.Text)];
        return (all, classes, table, texts, sources);

        void Collect(Core.ProgramUnitContext ctx, BoundUnit? parent)
        {
            var unit = MakeUnit(ctx, parent, all.Count, usedClassNames, edition, variables);
            sources.Add(unit, ctx);
            all.Add(unit);
            parent?.Children.Add(unit);
            foreach (var nested in ctx.nestedProgram())
                Collect(Reparent(nested), unit);
        }
    }

    /// <summary>Build one <see cref="BoundUnit"/> from a program unit's IDENTIFICATION DIVISION: the DECLARED
    /// name (program-name-1 / user-function-name-1), the EXTERNALIZED name the <c>AS</c> phrase gives it, and the
    /// COMMON / INITIAL / RECURSIVE attributes with their per-edition + placement gates (§2023:11.10.3 SR4–6).
    /// <para>ISO §2023:11.10.4 GR1 keeps the two names DISTINCT — "Program-name-1 names the program declared by this
    /// program definition. Literal-1, if specified, is the name of the program that is externalized to the
    /// operating environment" — and this used to collapse them onto one field (kb/Work PB303), which would have
    /// broken §2023:10.7.3 SR2's END PROGRAM match, §2023:14.9.4.3 SR15's AS NESTED lookup and the §2023:15.65.4 r4 MODULE-NAME
    /// determination the moment the phrase parsed. They are separate fields now.</para></summary>
    private static BoundUnit MakeUnit(
        Core.ProgramUnitContext ctx, BoundUnit? parent, int index, HashSet<string> usedClassNames, EditionContext edition,
        DirectiveTimeline<CompilationVariableEvent> variables)
    {
        var idBody = ctx.identificationDivision()?.identificationBody();
        var pid = idBody?.programIdParagraph();
        var fid = idBody?.functionIdParagraph();
        string name = pid?.programName()?.GetText()
            ?? fid?.programName()?.GetText()
            ?? $"PROGRAM{index}";
        bool isFunction = pid is null && fid is not null;
        // A signature-only prototype unit — a FUNCTION-ID prototype (M2-UDF-3) or a §2023:11.10.2 Format-2 PROGRAM-ID
        // prototype (kb/Work PB894; §2023:11.10.4 GR6 — program-prototype-name-1 identifies the prototype, literal-1
        // its externalized name, which the AS arm below already reads for either paragraph). ONE grammar node,
        // `prototypePhrase`, answers for both kinds — until PB894 this read the FUNCTION-ID paragraph alone, so
        // IsPrototype could never be true for a program. The COBOL-2002 introduction gates are
        // VersionConformancePass.Run (bound-arm over group.Units — BoundUnit.IsPrototype is drop-proof).
        bool isPrototype = (pid?.prototypePhrase() ?? fid?.prototypePhrase()) is not null;
        string what = $"{(isFunction ? "FUNCTION-ID" : "PROGRAM-ID")} '{name}'{(isPrototype ? " IS PROTOTYPE" : "")}";
        if (isPrototype)
            // §2023:10.6.2 SR4 — the BODY of a prototype, one screen for every prototype kind.
            PrototypeUnitRules.ScreenBody(what,
                idBody?.identificationParagraph().Select(p => p.optionsParagraph()).FirstOrDefault(o => o is not null),
                ctx.environmentDivision(), ctx.dataDivision(), ctx.procedureDivision(), edition);
        // §2023:10.6.1 — the SHAPE of EVERY unit kind (containment, end marker, the procedure-division bracket), one
        // table (kb/Work PB1507; it used to be asked of the prototype kinds alone).
        SourceUnitShape.Screen(what, SourceUnitShape.KindOf(isFunction, isPrototype),
            parent is null ? null : SourceUnitShape.KindOf(parent.IsFunction, parent.IsPrototype), ctx, edition);
        // §2023:11.10.2 Format 1's attribute group is a §2023:5.2.6.4 choice-indicator group — any order, each alternative at
        // most ONCE — and the grammar's `programIdAttribute+` is the superset the only-once half is read from.
        var attributes = pid?.programIdAttributes()?.programIdAttribute() ?? [];
        string group = $"PROGRAM-ID '{name}'";
        using var attributePosition = edition.At(pid);   // the PROGRAM-ID paragraph, for the repeat / 0886 / 0887 messages
        var initialAttr = ChoiceIndicators.AtMostOnce(edition,
            attributes.Where(a => a.commonProgramAttribute().INITIAL_() is not null).ToArray(),
            group, "the INITIAL attribute", "11.10.2");
        var commonAttr = ChoiceIndicators.AtMostOnce(edition,
            attributes.Where(a => a.commonProgramAttribute().COMMON() is not null).ToArray(),
            group, "the COMMON attribute", "11.10.2");
        var recursiveAttr = ChoiceIndicators.AtMostOnce(edition,
            attributes.Where(a => a.commonProgramAttribute().RECURSIVE() is not null).ToArray(),
            group, "the RECURSIVE attribute", "11.10.2");
        bool initial = initialAttr is not null, common = commonAttr is not null, recursive = recursiveAttr is not null;

        // §2023:11.10.3 SR5 / SR6 — the CONTAINER chain ("directly or indirectly contains this program"). SR5 reads the
        // container's EFFECTIVE recursive attribute (the one §2023:11.10.4 GR4 hands down is still "a recursive program");
        // SR6 reads a container's INITIAL clause, which is never inherited. The nearest offending container is named.
        if (initialAttr is not null)
            for (var anc = parent; anc is not null; anc = anc.Parent)
                if (anc.Recursive)
                {
                    using var _ = edition.At(initialAttr);
                    edition.Error(DiagnosticCatalog.ProgramAttributeContainment,
                        $"program '{name}' is INITIAL but is contained in the recursive program '{anc.Name}': the "
                        + "INITIAL clause shall not be specified if any program that directly or indirectly "
                        + "contains this program is a recursive program (ISO §2023:11.10.3 SR5)");
                    break;
                }
        if (recursiveAttr is not null)
            for (var anc = parent; anc is not null; anc = anc.Parent)
                if (anc.Initial)
                {
                    using var _ = edition.At(recursiveAttr);
                    edition.Error(DiagnosticCatalog.ProgramAttributeContainment,
                        $"program '{name}' is RECURSIVE but is contained in the initial program '{anc.Name}': the "
                        + "RECURSIVE clause shall not be specified if any program that directly or indirectly "
                        + "contains this program is an initial program (ISO §2023:11.10.3 SR6)");
                    break;
                }

        // §2023:11.10.2 / §2023:11.5.2's `[ AS literal-1 ]` — the externalized name (kb/Work PB303). Absent, §2023:8.3.2.2 2)
        // externalizes the user-defined word itself, so the two names coincide.
        string externalized = name;
        if ((pid?.externalizedNamePhrase() ?? fid?.externalizedNamePhrase()) is { } asPhrase)
        {
            using var _ = edition.At(asPhrase);
            string kind = isFunction ? "FUNCTION-ID" : "PROGRAM-ID";
            string rule = isFunction ? "ISO §2023:11.5.3 SR1" : "ISO §2023:11.10.3 SR1";
            // §2023:11.10.3 SR2 — FORMAT 1 only: "Literal-1 shall not be specified in a program that is contained
            // within another program." §2023:8.3.2.2 2) externalizes "program-names of OUTERMOST programs", so a
            // containee has no externalized name for the phrase to give. §2023:11.5.3 states no such rule (a
            // function definition is never contained), so the gate keys on the containment, not the kind.
            if (parent is not null)
                edition.Error(DiagnosticCatalog.ExternalizedNameContained,
                    $"{kind} '{name}' AS {ExternalizedName.Written(asPhrase)}: literal-1 shall not be specified in a "
                    + "program that is contained within another program (ISO §2023:11.10.3 SR2)");
            // The unit's DATA DIVISION is not bound yet, so a constant-name operand (§2023:13.10.3 SR2) is read off the
            // parse tree (kb/Work PB2588).
            else if (ExternalizedName.Screen(asPhrase, k => PreBindConstants.Read(asPhrase, k, variables), edition,
                         DiagnosticCatalog.ExternalizedNameLiteral,
                         $"{kind} '{name}' AS {ExternalizedName.Written(asPhrase)}", "literal-1", rule,
                         LiteralEnvironment.Unscoped) is { } lit1)
                externalized = lit1;
        }

        // program-id-recursive-2002: the pass owns the edition gate (Exec Step E).
        if (initial && recursive)
            edition.Error(DiagnosticCatalog.ProgramInitialRecursiveExclusive,
                $"program '{name}': INITIAL and RECURSIVE are mutually exclusive — §2023:11.10.2 Format 1 prints them as "
                + "the alternatives of ONE brace (ISO §2023:11.10.2); the containment conflicts of the two attributes are "
                + "§2023:11.10.3 SR5–6, checked above against the container chain");
        if (common && parent is null)
            edition.Error(DiagnosticCatalog.CommonProgramNotContained,
                $"program '{name}': COMMON may be specified only in a CONTAINED program (ISO §2023:11.10.3 SR4)");

        // §2023:8.6.6 (:8821) "Functions and methods are always recursive" / §2023:9.4 (:12529) "a user defined
        // function always possesses the recursive attribute and may call itself" — implicit, never the
        // explicit PROGRAM-ID attribute, so it rides AFTER the 0885/0886 gates. Registering Recursive here
        // is what keeps ProgramTable's §2023:14.9.4.4 GR3f re-entry rejection (EC-PROGRAM-RECURSIVE-CALL) from
        // firing on a function's self-activation and selects the per-activation instance model (D3/D4).
        if (isFunction) recursive = true;
        // §2023:11.10.4 GR4 (kb/Work PB133): "The RECURSIVE clause specifies that the program AND ANY PROGRAMS
        // CONTAINED WITHIN IT are recursive" — the attribute inherits down the containment tree (parents are
        // built before their children, so one parent read cascades transitively). The inherited attribute is
        // what SR5 reads of a container ("a recursive program"), so an INITIAL containee of a recursive
        // container is the nonconforming source SR5 refuses above — never a legal program carrying both
        // attributes (kb/Work PB1507; this comment used to call that shape legal). This is what lets the legal
        // R→C→R→C cycle through GR3f's re-entry check and lets a contained program call ITSELF (§2023:8.4.6.3 r1's "in
        // the program itself") — both drew EC-PROGRAM-RECURSIVE-CALL / NOT-FOUND before.
        if (parent is { Recursive: true }) recursive = true;

        string baseName = "_PRG_" + DataItem.WordIdentifier(name);
        string className = baseName;
        for (int n = 2; !usedClassNames.Add(className); n++) className = $"{baseName}_{n}";
        return new BoundUnit
        {
            Name = name, ExternalizedName = externalized, ClassName = className,
            SourcePosition = FirstToken(ctx)?.StartIndex ?? 0,
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
    /// shadow a container's), then inject the containers' GLOBAL names (ISO §2023:13.18.27 GR1–2 — nearest container
    /// first, each at its nesting depth; which one a reference names is §2023:8.4.6.2.1 3)'s question, asked at the
    /// reference by <c>SymbolTable.NearestDeclaring</c>) and record the <c>ref</c>-bridges the nested class needs to reach the
    /// container's storage. Every unit passes through here BEFORE any unit's procedure binds
    /// (<see cref="BindUnitProcedure"/>) — the forward-reference enabler for user-function signatures.</summary>
    /// <summary>The line of a unit's first token — where §2023:7.3.4 GR5's "all of the source text … that follows" is folded.
    /// A unit whose context carries no start token of its own (a header-less nested program, §2023:11.2.1) takes its first
    /// descendant's line.</summary>
    private static int UnitFirstLine(Antlr4.Runtime.ParserRuleContext c) => FirstToken(c)?.Line ?? 0;

    /// <summary>The first token of <paramref name="c"/>'s text — its own start token, or, for a re-shaped contained
    /// program (<see cref="Reparent"/>) that has none, the first token of its first descendant. The ONE reader of a
    /// unit's source position: the line (<see cref="UnitFirstLine"/>) and the character offset
    /// (<see cref="BoundUnit.SourcePosition"/>) both come from this token.</summary>
    private static Antlr4.Runtime.IToken? FirstToken(Antlr4.Runtime.ParserRuleContext c)
    {
        if (c.Start is { } s) return s;
        for (int i = 0; i < c.ChildCount; i++)
        {
            var token = c.GetChild(i) switch
            {
                Antlr4.Runtime.ParserRuleContext p => FirstToken(p),
                Antlr4.Runtime.Tree.ITerminalNode t => t.Symbol,
                _ => null,
            };
            if (token is not null) return token;
        }

        return null;
    }

    private static void BindUnitData(BoundUnit unit, BindSession session)
    {
        var definition = session.Sources.Of(unit);
        var edition = session.Edition;
        // The static-WS discriminator (ISO §2023:13.5.4 GR1 + §2023:14.6.2.3.2/.3; the full derivation is on
        // DataBinder.UnitStaticWs): a RECURSIVE-and-not-INITIAL unit — including every FUNCTION-ID unit
        // (§2023:8.6.6 :8821 / §2023:9.4 :12529, the implicit attribute set in MakeUnit) — owns ONE last-used WS copy
        // shared across activations, so its WS roots emit STATIC — with or without contained programs. A
        // containee's GLOBAL bridges alias the container's members (§2023:13.18.27 GR2): an INSTANCE member is reached
        // through the `__outer` chain, a STATIC one through the container's class name, which C# requires
        // (CS0176: `instance.staticField` is not a C# expression). BoundUnit.AnchorOf chooses per member, so the
        // composition is one rule in one place (kb/Work PB1133; the staged COBOL0899 refusal is gone).
        var data = new DataBinder(edition)
        {
            OoClasses = session.OoClasses,
            // The unit's own §2023:8.3.2.2 census, the one its class-name references record into too (kb/Work PB3132).
            OwnWords = session.OoClasses.CensusOf(definition.identificationDivision()) ?? new(),
            RefModZeroLength = session.RefModZeroLength,
            CobolWords = session.CobolWords,   // >>COBOL-WORDS intrinsic-function-name synonym/removal (§2023:7.3.10)
            Retypes = session.Retypes,         // the fragment re-parses read words as the tree does (kb/Work PB655)
            ClaimScans = session.ClaimScans,   // the group's one storage-claim walk, sliced per element (kb/Work PB4302)
            LeapSecond = session.LeapSecond.IsOnAt(UnitFirstLine(definition)),   // >>LEAP-SECOND ON at THIS unit — the §2023:15.3 seconds-subfield / time-form bound (§2023:7.3.17, §2023:7.3.4 GR5)
            CompilationVariables = session.CompilationVariables,   // CONSTANT … FROM reads the >>DEFINE table at its own line (§2023:7.3.11.4 GR1)
            // The ANY LENGTH placement facts (ISO §2023:13.18.2.3 SR2–SR4 — the rules differ for a contained
            // program, a function, and an outermost program): the unit kind is known only here.
            UnitIsContained = unit.Parent is not null,
            UnitIsFunction = unit.IsFunction,
            UnitIsRecursive = unit.Recursive,   // §2023:12.4.6.3.3 SR5 (kb/Work PB3338)
            // §2023:8.4.6.6 / §2023:8.4.6.8 — the no-specifier leg of the two prototype-name scope rules: the ONE set the
            // registries read too (ContainingDefinitions; kb/Work PB452/PB817, PB3121).
            ContainingDefinitionNames = new HashSet<string>(ContainingDefinitions(unit).Select(u => u.Name), CobolNames.Comparer),
            UnitStaticWs = unit.Recursive && !unit.Initial,
        };
        data.CallSeedUids(session.TakeUidBand());

        // The configuration slice (ISO §2023:12.3.4 GR1 / §2023:11.9.4 GR1; §2023:12.3.3 SR1 — a contained program cannot have its own
        // configuration section; kb/Work PB2296): ConfigurationStep, the slice's one producer, takes the WHOLE
        // configuration-derived state of the container — SPECIAL-NAMES (DECIMAL-POINT, CURRENCY, classes, alphabets,
        // switches, mnemonic-names), the PROGRAM COLLATING SEQUENCE, DEBUGGING MODE, the REPOSITORY specifiers — and the
        // OPTIONS baseline, BEFORE this unit binds so its first literal and PICTURE already see them (kb/Work PB60 /
        // AR-15.67.3-5: only the REPOSITORY sets were inherited, after Bind, and a contained program under
        // DECIMAL-POINT IS COMMA parsed NUMVAL("123,45") as 0). One level suffices: the container inherited
        // from ITS container before it bound (units bind container-first).
        ConfigurationStep.Produce(unit.Parent is { } container
            ? new ConfigurationSource.Contained(session.Binders.Of(container).Data.State.Configuration)
            : new ConfigurationSource.Own(), data.State.Configuration);
        // The inheritance merge's seed half (InheritStep.Seed; ISO §2023:13.18.27.4 GR1–GR2; kb/Work PB2296): every container's
        // GLOBAL constant-names, type declarations, file-names, reports and data roots, and the C# member names their
        // GLOBAL roots occupy, nearest container first, BEFORE this unit binds, because its own data division reads them.
        InheritStep.Seed(unit, data, session.Binders);
        data.Bind(definition);
        session.Binders.Add(unit, data);

        // The unit's checking slice (kb/Work PB2296): its EC-EXTERNAL enablement facts (ISO §2023:14.8.4.1), its
        // EC-PROGRAM-ARG-MISMATCH header fold (§2023:14.9.4.4 GR3 d)), the group-level Describe and EC-BOUND-ODO gates and its
        // automatic propagation (§2023:7.3.21.4 GR1/GR3), folded after its data phases and before its procedure division binds.
        data.State.UnitChecking = CheckingFold.Unit(session.Turn, session.Propagate, definition);

        // The inheritance merge's second half (InheritStep.Merge; kb/Work PB2296): the formals' omitted-argument guards,
        // the GLOBAL bridges and the drop of an inherited constant a nearer data-name hides. A container's GLOBAL FD needs
        // no merge into FilesByName: every file-name lookup reads the unit's own files first and its containers' GLOBAL
        // ones after (DataBindState.VisibleFiles), and the child's bound verbs hold the SHARED FileModel, so the owner's
        // one-time PROG::FILE qualification keys them to the owner's connector.
        InheritStep.Merge(unit, SourceElementKindOf(unit), session.Turn, session.Binders);
    }

    /// <summary>ALWAYS-ON (P6 Step 6): assert <paramref name="pass"/>'s declared prerequisite phase has been
    /// PRODUCED on every binder of the group before it runs (a per-pass integer compare per binder — immaterial;
    /// was Debug-only until CI's Release leg exposed the divergence, DEVLOG 774).</summary>
    private static void RequireAll(GroupBindContext ctx, GroupBindPass pass)
    {
        foreach (var d in ctx.AllBinders()) d.State.Require(pass.Requires, pass.Name);
    }

    /// <summary>The <c>ProcedureBinding</c> GROUP pass body (P6 Step 3 — <c>BindPipeline.GroupTail</c>, Requires
    /// <c>FilesResolved</c>, Produces <c>ProcedureBound</c>): bind every unit's PROCEDURE DIVISION against the
    /// group's REPOSITORY tables (<see cref="BindSession.Repository"/>, built by <see cref="Bind"/> before the
    /// class bodies).</summary>
    internal static void BindProcedures(GroupBindContext ctx)
    {
        foreach (var unit in ctx.Units) BindUnitProcedure(unit, ctx.Session);
    }

    /// <summary>Build the group's REPOSITORY specifier resolver (§2023:12.3.8.4 GR10 / GR11 — <see cref="RepositoryResolver"/>
    /// over <see cref="GroupRepositoryProvider"/> and <see cref="MetadataRepositoryProvider"/>, in that search order) —
    /// once, between the DATA phase and the first procedure body.</summary>
    private static RepositoryResolver BuildRepositoryResolver(IReadOnlyList<BoundUnit> units,
                                                              IReadOnlyList<OoClassUnit> classes, BindSession session)
    {
        // BEFORE the repository is read (kb/Work PB660): a compilation group that DEFINES one name twice
        // is nonconforming source, and the search silently keeps the first definition and drops the
        // second — the shape §2023:8.3.2.2 exists to forbid.
        CheckDefinitionNameUniqueness(units, classes, session.OoClasses, session.Sources, session.Edition, session.Binders);
        CheckFunctionUnits(units, session.Sources, session.Edition, session.Binders);
        // kb/Work PB237 / PB894 / PB989 — the compilation group's program and function units, the search space
        // §2023:12.3.8.4 GR10 / GR11 a) and b) name. Built once; each source element searches it AS OF ITS OWN POSITION.
        // c) is the external repository's (kb/Work PB2103): it reads nothing until a specifier the group cannot answer.
        return new RepositoryResolver(new GroupRepositoryProvider(units, session.Binders), session.External);
    }

    /// <summary>⛔ Give every restricted program- or function-pointer in the group its two restrictions — the prototype its
    /// TO phrase names (<see cref="DataBinder.ResolveRestrictedPrototypes"/>; the identity rules read its externalized
    /// name) and what it may contain, the identity of that prototype's signature (kb/Work PB2464; ISO §2023:13.18.60.4 GR25 /
    /// GR26; <see cref="PrototypeSignatures.StampContentRestrictions"/>) — once the REPOSITORY tables exist and BEFORE any
    /// signature is compared, any body binds or any description is built (the §2023:10.6.2 SR2 / SR3 prototype pairs and the
    /// §2023:9.3.8.2.3 method conformance checks run after it, kb/Work PB2495: before it, a TO phrase is the word as written,
    /// not the externalized name of the prototype it names). It covers EVERY forest that declares one — each program
    /// unit's, each class's OBJECT and FACTORY forests and each interface's prototype formals — through the same
    /// per-forest tables the procedure binders read, so that two items compared by the typed comparator or by a universal
    /// INVOKE's descriptions are never one resolved and one not.
    /// <para>TWO passes over the forests: the first attaches each pointer's resolved prototype, the second stamps what it
    /// may contain. A signature's identity describes its formals, and a formal that is itself a restricted pointer is
    /// described by the externalized name of the prototype IT names (kb/Work PB4491) — which may be declared in a forest
    /// later in the group, so every forest is resolved before any is stamped.</para></summary>
    private static void ResolvePointerRestrictions(IReadOnlyList<BoundUnit> units, IReadOnlyList<OoClassUnit> classes,
        IReadOnlyList<KeyValuePair<OoInterfaceSymbol, DataBinder>> interfaces, BindSession session)
    {
        var repository = session.Repository;
        var forests = new List<(DataBinder Data, BoundUnit? Unit, int Position)>();
        foreach (var unit in units) forests.Add((session.Binders.Of(unit).Data, unit, unit.SourcePosition));
        foreach (var cls in classes)
        {
            int position = session.Sources.Of(cls.Symbol).Start.StartIndex;   // the position OoDriver.BindClassBody searches the group at
            forests.Add((session.Binders.ObjectOf(cls).Data, null, position));
            forests.Add((session.Binders.FactoryOf(cls).Data, null, position));
        }
        foreach (var (iface, data) in interfaces) forests.Add((data, null, session.Sources.Of(iface).Start.StartIndex));
        foreach (var (data, unit, position) in forests)
            data.ResolveRestrictedPrototypes(
                ProgramPrototypesOf(data, unit, repository, position, session.Binders), UserFunctionsOf(data, unit, repository, position, session.Binders));
        PrototypeSignatures.StampContentRestrictions(forests.SelectMany(f => f.Data.RestrictedPrototypePointers()));
    }

    /// <summary>The PROCEDURE half of unit binding (phase 2): every unit's DATA is already bound
    /// (<see cref="BindUnitData"/>) and the group's <see cref="RepositoryResolver"/> is built, so a
    /// <c>FUNCTION user-name(args)</c> reference resolves its callee's RETURNING/USING descriptions from the
    /// definition or prototype §2023:12.3.8.4 GR11 a) / b) selects as of this unit's position (§2023:8.4.3.2.4 GR1).</summary>
    private static void BindUnitProcedure(BoundUnit unit, BindSession session)
    {
        var (data, refs) = session.Binders.Of(unit);
        var binder = new StatementBinder(data, refs)
        {
            OoClasses = session.OoClasses,
            UserFunctions = UserFunctionsOf(data, unit, session.Repository, unit.SourcePosition, session.Binders),
            // §2023:8.4.6.6 — inside a function definition its OWN name is a referable function-prototype-name
            // (self-recursion without a repository entry; §2023:12.3.8.3 SR11 makes a present self-entry a no-op —
            // UserFunctionsOf ignores it; both read ContainingDefinitions).
            UdfSelfName = unit.IsFunction ? unit.Name : null,
            // ISO §2023:14.2.2 SR10's enumeration of the five source elements that may carry a Format-1/2 procedure
            // division — the input to every EXIT / GOBACK placement rule (kb/Work PB403). Derived HERE, beside
            // UdfSelfName, from the same two unit facts, so the "am I a function?" answer cannot fork.
            UnitKind = SourceElementKindOf(unit),
            // §2023:15.65.3 argument rule 1 — MODULE-NAME NESTED requires a contained program.
            InNestedProgram = unit.Parent is not null,
            // §1985:VII.4.6.4 GR1 — the declaratives of the separately compiled program, a containing program's included, are applicable here (kb/Work PB5076).
            ContainerUses = unit.Parent is null ? null : () => ContainerUsesOf(unit),
            // kb/Work PB131 — the AS NESTED callee set (§2023:14.9.4.3 SR15): the caller's directly-contained
            // children, plus every COMMON program contained in a (transitive) ancestor — the §2023:10.7.2
            // visibility the runtime ResolveVisible applies, computed statically so GR9's formal-mode
            // lookup and SR15's scope check both happen at BIND time.
            NestedCallables = NestedCallablesOf(unit, session.Binders),
            // kb/Work PB237 — the unit's visible program prototypes (§2023:12.3.8.2 specifiers resolved through
            // §2023:12.3.8.4 GR10, plus §2023:8.4.6.8's containing-program spelling).
            ProgramPrototypes = ProgramPrototypesOf(data, unit, session.Repository, unit.SourcePosition, session.Binders),
            UnitRecursive = unit.Recursive,   // §2023:14.9.7.3 SR1 / §2023:14.9.36.3 SR1 (kb/Work PB137)
        };
        binder.ConfigureEc(session.Turn, session.DirectiveSites, unit.Name);   // the EC bind context (TURN fold + directive sites + §2023:15.30 location element)
        unit.Bound = binder.Bind(session.Sources.Of(unit));
        // The boundary-copied GROUP formals + RETURNING item are registered whole-group-referenced (so StorageFormPass
        // flips their numeric-DISPLAY leaves to image storage, and the formal's FromImage/AsImage round trip
        // type-checks — ISO §2023:14.2.3 GR8 / §2023:14.9.25.4 MOVE GR4) by the post-bind UsageCollectionPass, from data.Linkage.Formals
        // + data.Linkage.Returning. The pre-flip early-resolve of every formal existed ONLY for that side effect (which
        // ReferenceResolver no longer performs) — deleted, PHASE-05 Step 5.
    }

    /// <summary>Classify a bound unit as one of ISO §2023:14.2.2 SR10's source elements (kb/Work PB403). The two unit
    /// facts the classification needs — FUNCTION-ID vs PROGRAM-ID, and the <c>IS PROTOTYPE</c> tail — are already
    /// carried by <see cref="BoundUnit"/>; a method definition is NOT classified here, because a method's
    /// statements bind on its CLASS unit's binder under an entered method scope (<c>BinderContext.SourceElement</c>).
    /// <para>A FUNCTION prototype's procedure division is BOUND by this compiler — the §2023:10.1 general format
    /// admits one and the parser accepts its paragraphs, measured: a prototype carrying <c>EXIT PROGRAM</c>
    /// reaches <c>BindExit</c> (and is refused there by §2023:10.6.2 SR4 f) — COBOL2272 — before any placement
    /// rule matters). A PROGRAM prototype classifies the same way since kb/Work PB894 made §2023:11.10.2 Format 2
    /// writable and <c>MakeUnit</c> read <c>IsPrototype</c> off the shared <c>prototypePhrase</c>.</para></summary>
    private static SourceElementKind SourceElementKindOf(BoundUnit unit) => unit.IsFunction
        ? (unit.IsPrototype ? SourceElementKind.FunctionPrototype : SourceElementKind.FunctionDefinition)
        : (unit.IsPrototype ? SourceElementKind.ProgramPrototype : SourceElementKind.Program);

    /// <summary>The AS NESTED callee table for one caller (kb/Work PB131; §2023:14.9.4.3 SR15 + §2023:10.7.2):
    /// name → the callee's bound PD-header SIGNATURE. Directly-contained children first; a COMMON program
    /// contained in an ancestor is visible too (nearest wins on a name clash, matching §2023:10.7.2's scope) — EXCEPT from
    /// within that common program's own subtree unless it is RECURSIVE (§2023:8.4.6.3 2); kb/Work PB1460), the exception
    /// the run-time resolver applies through the same <see cref="WiseOwl.Cobol.Runtime.ProgramNameScope"/>.
    /// <para>The key is the child's program-name, and that IS its externalized name: §2023:11.10.3 SR2 ("Literal-1 shall not
    /// be specified in a program that is contained within another program", COBOL1795) means no contained program
    /// has an AS literal, so the run-time <c>ProgramTable</c>'s match on the externalized name finds the unit this
    /// table was keyed on (kb/Work PB1756 — no spelling pair exists that could disagree).</para>
    /// <para>The signature carries the RETURNING item as well as the formals (kb/Work PB204): §2023:14.9.4.3 SR25
    /// makes §2023:14.8.3, Returning items apply to a Format-2 CALL exactly as §2023:14.8.2 applies to its arguments, and
    /// with AS NESTED both halves of that pair are statically known. Carrying only the formals is what left the
    /// RETURNING half unchecked — the same shape as <c>UserFunctionSignature</c>, which has carried both since
    /// it was written.</para></summary>
    private static Dictionary<string, CalleeSignature> NestedCallablesOf(BoundUnit unit, ElementBinders binders)
    {
        var map = new Dictionary<string, CalleeSignature>(WiseOwl.Cobol.Runtime.ExternalizedNames.Comparer);
        foreach (var c in unit.Children)
            map.TryAdd(c.Name, binders.SignatureOf(c));
        for (var anc = unit.Parent; anc is not null; anc = anc.Parent)
            foreach (var c in anc.Children)
                if (c.Common && WiseOwl.Cobol.Runtime.ProgramNameScope.CommonProgramReferable(c, c.Recursive, unit, u => u.Parent))
                    map.TryAdd(c.Name, binders.SignatureOf(c));
        return map;
    }

    // ── The program-prototype registry (kb/Work PB237; ISO §2023:12.3.8.2 program-specifier → §2023:12.3.8.4 GR10) ──────
    // The PROGRAM twin of the function resolution below: GR10 a)/b)/c) has the identical shape to GR11 a)/b)/c), so
    // both are answered by the ONE search GroupRepositoryProvider.Find, built at the same point for the same reason — every
    // unit's DATA has bound, so a callee's PD-header signature is a fact — and run as of the referencing element's
    // position, because a) and GR11 a) say "specified PREVIOUSLY" (kb/Work PB989).

    /// <summary>⛔ THE ONE UNIQUENESS CHECK OVER A COMPILATION GROUP'S DEFINITION NAMES (kb/Work PB660), in
    /// the two scopes the standard gives them.
    /// <list type="number">
    /// <item><b>The EXTERNALIZED scope — the whole group</b> (§2023:8.3.2.2, COBOL2213). Its list item 1
    /// externalizes "program-names of OUTERMOST programs … and user-function-names", and the clause then states
    /// the rule twice over that one population: a CROSS-KIND pair is refused by <i>"all instances of a given
    /// name that is externalized to the operating environment shall identify the same kind of entity or
    /// item"</i>, and a SAME-KIND pair by <i>"when two or more source elements identify something with the same
    /// externalized name, they refer to the same instance"</i> — two distinct definitions cannot be ONE
    /// instance. Programs and functions are therefore ONE namespace here, not two, which is why this replaced
    /// the function-only duplicate report that used to live in <see cref="CheckFunctionUnits"/>.</item>
    /// <item><b>The CONTAINED scope — one outermost program</b> (§2023:8.4.6.3, COBOL2214): <i>"The names
    /// assigned to programs that are contained directly or indirectly within the same outermost program shall
    /// be unique within that outermost program."</i> A containee's name is not externalized at all, so its
    /// scope is its outermost program and two different outermost programs may each contain an <c>X</c>.</item>
    /// </list>
    /// <para>MEASURED BEFORE WRITING, because the note claimed the contained half was already enforced:
    /// NEITHER half was. Two outermost <c>PROGRAM-ID. P3DND.</c> definitions compiled and ran the SECOND; the
    /// same pair under one <c>AS "P3DUPX"</c> ran the FIRST; two same-named CONTAINED programs ran the first;
    /// and a program and a function sharing one name both registered and both ran. The registrar emitted
    /// <c>ProgramRegistry.Register("P3DND", …)</c> twice under one path — order-dependent, and silent.</para>
    /// <para>PROTOTYPES ARE EXCLUDED BY THE RULE, NOT FOR CONVENIENCE. §2023:10.6.2 says so twice, once per kind:
    /// SR2 — <i>"If a compilation group contains both a program definition and a program prototype definition
    /// with the same externalized name, the signatures of these two compilation units shall be the same"</i> —
    /// and SR3, the function twin. A prototype sharing a definition's externalized name is the shape the
    /// standard legislates FOR, and §2023:12.3.8.4 GR10 a) is what consumes it.</para></summary>
    private static void CheckDefinitionNameUniqueness(IReadOnlyList<BoundUnit> units, IReadOnlyList<OoClassUnit> classes,
                                                      OoClassTable oo, SourceMap sources, EditionContext edition, ElementBinders binders)
    {
        // (1) The group-wide EXTERNALIZED namespace. Its population is §2023:8.3.2.2's list items 1 AND 2 as far as
        //     this compiler models them: outermost program definitions, function definitions, object-class
        //     definitions and interface definitions (item 1), and the EXTERNAL data items, records and file
        //     connectors of every source element (item 2, kb/Work PB1404). A FUNCTION-ID unit is never
        //     contained, so one containment test covers both unit kinds; a class/interface definition is never a
        //     unit at all, which is why it arrives from the OO table (and is why a CLASS-ID sharing a PROGRAM-ID's
        //     externalized name used to compile clean — each namespace policed only ITSELF; the EXTERNAL items
        //     were the same hole, one list further down the same clause).
        var externalized =
            new Dictionary<string, (string Kind, string Spelling, string Word)>(WiseOwl.Cobol.Runtime.ExternalizedNames.Comparer);
        foreach (var (kind, spelling, word, name, at, shared) in ExternalizedDefinitions(units, classes, oo, sources, binders))
        {
            if (externalized.TryGetValue(name, out var first))
            {
                // §2023:8.4.6.4 states its OWN compilation-group uniqueness for object-class-names and
                // interface-names, and the OO class table enforces it on the declared WORD (COBOL0820,
                // OoDefinitionNameNotUnique). A pair already reported THERE is skipped here rather than doubled — but only
                // that pair: two class definitions whose words DIFFER and whose AS literals coincide are
                // §2023:8.3.2.2's business alone, and nothing else in the compiler looks at them.
                if (IsOo(kind) && IsOo(first.Kind) && CobolNames.Same(word, first.Word)) continue;
                // Two EXTERNAL items of ONE kind under one name are the SAME INSTANCE — that sharing is what the
                // EXTERNAL clause is for ("they refer to the same instance"), and whether their descriptions agree is
                // §2023:13.18.22.4 / EC-EXTERNAL-*'s question. Only a pair of DIFFERENT kinds conflicts.
                if (shared && first.Kind == kind) continue;
                using var _ = edition.At(at);
                string why = first.Kind == kind
                    ? $"two {kind} definitions cannot be one instance (§2023:8.3.2.2: \"when two or more source "
                      + "elements identify something with the same externalized name, they refer to the same "
                      + "instance\")"
                    : $"{Noun(kind)} and {Noun(first.Kind)} are not the same kind of entity or item "
                      + "(§2023:8.3.2.2: \"all instances of a given name that is externalized to the operating "
                      + "environment shall identify the same kind of entity or item\")";
                edition.Error(DiagnosticCatalog.DuplicateExternalizedDefinition,
                    $"{spelling} and {first.Spelling} both externalize the name "
                    + $"'{name}' in this compilation group — {why}");
                continue;   // the FIRST stays the survivor, so one duplicated name reports exactly once
            }
            externalized[name] = (kind, spelling, word);
        }

        // (2) The per-OUTERMOST-PROGRAM contained namespace (§2023:8.4.6.3). "Directly or indirectly", so the
        //     walk is each outermost program's whole containment subtree, flattened onto ONE set per root.
        foreach (var root in units)
        {
            if (root.Parent is not null || root.IsFunction) continue;
            var contained = new Dictionary<string, BoundUnit>(WiseOwl.Cobol.Runtime.ExternalizedNames.Comparer);
            foreach (var c in Containees(root))
                if (!contained.TryAdd(c.Name, c))
                {
                    using var _ = edition.At(sources.NameOf(c));
                    edition.Error(DiagnosticCatalog.DuplicateContainedProgramName,
                        $"program '{c.Name}' contained in '{root.Name}' repeats the name of another program "
                        + $"contained in '{root.Name}' — the names assigned to programs that are contained "
                        + "directly or indirectly within the same outermost program shall be unique within that "
                        + "outermost program (ISO §2023:8.4.6.3)");
                }
        }

        // Every program contained directly or indirectly in `root` — §2023:8.4.6.3's own scope words.
        static IEnumerable<BoundUnit> Containees(BoundUnit root)
        {
            foreach (var child in root.Children)
            {
                yield return child;
                foreach (var g in Containees(child)) yield return g;
            }
        }

        // §2023:8.3.2.2's list item 1, as this compiler models it — ONE sequence, so a definition kind cannot be
        // policed by its own namespace and by nothing else. Each element carries the KIND (for the two
        // messages), the SPELLING shown to the user, the EXTERNALIZED name that is the key, and where to
        // report. The list's remaining members are not definitions in a compilation group: method-names and
        // property-names are the two §2023:8.3.2.2 EXEMPTS from the same-instance sentence by name, and a
        // function-prototype-name / program-prototype-name names a definition elsewhere (§2023:12.3.8.4 GR10).
        static IEnumerable<(string Kind, string Spelling, string Word, string Name, DiagnosticCursor At, bool Shared)>
            ExternalizedDefinitions(IReadOnlyList<BoundUnit> units, IReadOnlyList<OoClassUnit> classes, OoClassTable oo, SourceMap sources,
                ElementBinders binders)
        {
            foreach (var u in units)
            {
                if (u.IsPrototype || u.Parent is not null) continue;
                string kind = u.IsFunction ? "FUNCTION-ID" : "PROGRAM-ID";
                yield return (kind, Spell(kind, u.Name, u.ExternalizedName), u.Name, u.ExternalizedName,
                              CursorOf(sources.NameOf(u)), false);
            }
            foreach (var c in oo.Classes)
                yield return ("CLASS-ID", Spell("CLASS-ID", c.Name, c.ExternalizedName), c.Name,
                              c.ExternalizedName, CursorOf(sources.Of(c)), false);
            foreach (var i in oo.Interfaces)
                yield return ("INTERFACE-ID", Spell("INTERFACE-ID", i.Name, i.ExternalizedName), i.Name,
                              i.ExternalizedName, CursorOf(sources.Of(i)), false);
            // §2023:8.3.2.2 list item 2 — the EXTERNAL data items, records and files of EVERY source element: a
            // contained program's and a class's (object and factory halves) included, since the clause names the
            // item, not the unit that holds it (kb/Work PB1404). ONE producer per source element
            // (LinkageBinder.ExternalizedSubjects), the same one its own §2023:13.18.22.3 SR2 screen reads.
            foreach (var data in units.Where(u => !u.IsPrototype).Select(u => binders.Of(u).Data)
                         .Concat(classes.SelectMany(cu => new[] { binders.ObjectOf(cu).Data, binders.FactoryOf(cu).Data })))
                foreach (var s in data.Linkage.ExternalizedSubjects())
                    yield return (s.Kind == ExternalizedSubjectKind.File ? "EXTERNAL file" : "EXTERNAL data item",
                                  $"EXTERNAL {s.Spelling}", s.Word, s.Name, s.At, true);
        }

        // A parse node's first token as a diagnostic position (an unset cursor leaves the current one standing).
        static DiagnosticCursor CursorOf(ParserRuleContext? ctx) => ExpansionToken.CursorOf(ctx?.Start);

        // How a KIND reads in the cross-kind sentence: a definition of the unit kinds, or the EXTERNAL item itself.
        static string Noun(string kind) => kind.StartsWith("EXTERNAL", StringComparison.Ordinal) ? $"an {kind}" : $"a {kind} definition";

        // The two kinds §2023:8.4.6.4 gives their own compilation-group uniqueness sentence, and whose check
        // therefore already exists elsewhere (OoClassTable).
        static bool IsOo(string kind) => kind is "CLASS-ID" or "INTERFACE-ID";

        // How a definition is named back to the user: the DECLARED word, plus the AS literal whenever one gave
        // the externalized name a different spelling (§2023:11.10.4 GR1 / §2023:11.5.4 GR1 / §2023:11.3.4 GR1 /
        // §2023:11.6.4 GR1) — without it a clash between two different words reads as a message about one word
        // written twice.
        static string Spell(string kind, string name, string externalized) =>
            string.Equals(name, externalized, StringComparison.Ordinal)
                ? $"{kind} '{name}'"
                : $"{kind} '{name}' AS \"{externalized}\"";
    }

    /// <summary>§2023:10.6.2 SR2 and SR3 — the SAME sentence written once per kind: "If a compilation group contains
    /// both a program definition and a program prototype definition with the same externalized name, the
    /// signatures of these two compilation units shall be the same" (SR2), and its function twin (SR3). ONE check
    /// for both, over the ONE signature comparison (<see cref="PrototypeSignatures.Same"/> — mode, OPTIONAL and
    /// §2023:14.8.2's description per formal, and the returning item). kb/Work PB894: SR2 had no check at all (a
    /// program prototype could not be written), and SR3's was an argument-COUNT comparison keyed on the
    /// declared word rather than the externalized name the rule names.</summary>
    private static void CheckPrototypeSignaturePairs(IReadOnlyList<BoundUnit> units, SourceMap sources, EditionContext edition,
        ElementBinders binders)
    {
        foreach (var proto in units)
        {
            if (!proto.IsPrototype) continue;
            var definition = units.FirstOrDefault(d => d is { IsPrototype: false, Parent: null }
                && d.IsFunction == proto.IsFunction && WiseOwl.Cobol.Runtime.ExternalizedNames.Same(d.ExternalizedName, proto.ExternalizedName));
            if (definition is null
                || PrototypeSignatures.Same(binders.SignatureOf(proto), binders.SignatureOf(definition)))
                continue;
            string kind = proto.IsFunction ? "function" : "program";
            using var _ = edition.At(sources.NameOf(proto));
            edition.Error(DiagnosticCatalog.PrototypeDefinitionSignatureMismatch,
                $"{kind} prototype '{proto.Name}' and the {kind} definition '{definition.Name}' share the "
                + $"externalized name \"{proto.ExternalizedName}\" but not a signature — the USING formals (count, "
                + "BY REFERENCE / BY VALUE, OPTIONAL, description — a group's subordinate entries included) or the "
                + "RETURNING item differ; the signatures of these two compilation units shall be the same (ISO "
                + $"§2023:10.6.2 {(proto.IsFunction ? "SR3" : "SR2")}; §2023:13.7.3 SR2)");
        }

    }

    /// <summary>The program prototypes ONE source element may name (ISO §2023:8.4.6.8, Scope of program-prototype-names:
    /// "Program-prototype-names referenced within a source element shall be either the program-name of a
    /// containing program definition or a program-prototype-name declared in the REPOSITORY paragraph").
    /// Both spellings register here, so §2023:14.9.4.3 SR16 and §2023:14.9.5.3 SR3 are ONE lookup:
    /// <list type="number">
    /// <item>every §2023:12.3.8.2 program-specifier visible to the element (<c>ConfigurationSlice.ProgramSpecifiers</c>, which
    /// already inherits its container's — §2023:12.3.8.4 GR10's "scope of the containing environment division"; for a
    /// CLASS's OBJECT or FACTORY forest, the class-level REPOSITORY, which §2023:12.3.4 GR1 applies to every method —
    /// kb/Work PB1100), resolved through GR10 by <paramref name="repository"/> AS OF
    /// <paramref name="position"/> — a definition specified LATER in the group is not a) and takes the element
    /// down to b) / c) (kb/Work PB989), c) being the external repository's entry; a specifier none answers is REPO-1 and
    /// is not registered (kb/Work PB2103);</item>
    /// <item>for a program <paramref name="unit"/>, its own name and every containing program definition's name,
    /// which §2023:8.4.6.8 admits with no specifier at all — and which is also exactly what §2023:12.3.8.3 syntax rule 15 means
    /// by "references to program-prototype-name-1 are to the named program definition and this program-specifier is
    /// ignored": a self- or container-named specifier is never resolved, and the definition, registered LAST, is the
    /// name's entry. A class forest
    /// (<paramref name="unit"/> null) has neither: a class is not a program definition and contains none.</item>
    /// </list>
    /// The table is the same at every edition. COBOL-85 has no program-prototype-names (ISO/IEC 1989:2002 introduced
    /// them), and the verbs that read a bare word against this table decide that: below 2002 CALL and CANCEL route the
    /// word as identifier-1 and never ask it (<c>CallBinder.ProgramPrototypeNamesExist</c>, kb/Work PB4509). The other
    /// readers are reached only through syntax that is itself refused below 2002 (the AS phrase, a USAGE … TO phrase, a
    /// program-address-identifier).</summary>
    internal static Dictionary<string, ProgramPrototype> ProgramPrototypesOf(
        DataBinder data, BoundUnit? unit, RepositoryResolver repository, int position, ElementBinders binders)
    {
        var map = new Dictionary<string, ProgramPrototype>(CobolNames.Comparer);
        var containing = ContainingDefinitions(unit).Where(u => !u.IsFunction).ToList();
        foreach (var (name, spec) in data.State.Configuration.ProgramSpecifiers)
        {
            // §2023:12.3.8.3 SR15: a specifier naming the element's own or a containing program definition "is ignored" — never
            // resolved (it would take c) and draw REPO-1: a definition does not precede itself); the self entry below is
            // the reference's target.
            if (containing.Exists(u => CobolNames.Same(name, u.Name))) continue;
            // GR10 a) / b): the in-group definition SPECIFIED PREVIOUSLY — else the in-group program PROTOTYPE
            // definition (kb/Work PB894) — supplies the details; c) the external repository entry that publishes the
            // externalized name (RepositoryResolver.ResolveProgram, the one search over both providers). None is REPO-1
            // (SR14), at the specifier, and registers no prototype: a CALL through it is COBOL1760 (DESIGN-external-repository §8.2 step 4).
            using var _ = data.Edition.At(spec.At);
            if (repository.ResolveProgram(spec.ExternalizedName, position) is { } details)
                map[name] = new ProgramPrototype(name, spec.ExternalizedName, details);
            else
            {
                repository.ReportUnanswered(spec, function: false, name, spec.ExternalizedName, data.Edition);
                // The refusal's route, for the CALL / CANCEL sentence (kb/Work PB4708): the external repository's finding
                // suppresses REPO-1, so no SR14 refusal exists to cite. The fact is position-independent, so every element
                // that inherits the specifier records the same entry.
                if (repository.External.Refused(RepositoryFamily.Program, name, spec.ExternalizedName))
                    data.State.Configuration.RefusedProgramSpecifiers.TryAdd(name, ProgramSpecifierRefusal.RepositoryFinding);
            }
        }
        // §2023:8.4.6.8's other spelling: "the program-name of a containing program definition" is a referable
        // program-prototype-name with NO specifier at all — the element's OWN program definition included, the one
        // set ContainingDefinitions names and DataBinder's TO-phrase screen asks (kb/Work PB3121). Registered LAST,
        // it also realizes §2023:12.3.8.3 SR15 — a specifier naming "the program definition in which this REPOSITORY
        // paragraph is specified or ... a containing program definition" is "ignored" and references go to that
        // definition, which is precisely what overwriting the specifier's entry does. (§2023:11.10.4 GR4's RECURSIVE
        // attribute is what makes a self-call reachable at run time; without it §2023:14.9.4.4 GR3 f) raises
        // EC-PROGRAM-RECURSIVE-CALL.)
        foreach (var u in ContainingDefinitions(unit))
            if (!u.IsFunction) map[u.Name] = SelfPrototype(u);
        return map;

        // The prototype NAME is the referable word (§2023:8.4.6.8), its EXTERNALIZED name is what CallBinder
        // emits as the activation target (§2023:14.9.4.4 GR3 b) → §2023:8.3.2.2) — so the two arguments are the unit's
        // two names, never the same one twice: a `PROGRAM-ID. P AS "PX".` that CALLs itself by the word P
        // must still activate "PX", the only name the run-unit registry holds it under (kb/Work PB303).
        ProgramPrototype SelfPrototype(BoundUnit u) =>
            new(u.Name, u.ExternalizedName,
                new ProgramAnswer(binders.SignatureOf(u), new GroupUnitOrigin(u)));
    }

    /// <summary>⛔ THE ONE "CONTAINING DEFINITION" SET of the two prototype-name scope rules (kb/Work PB3121): ISO
    /// §2023:8.4.6.6 admits "the user-function-name of the containing function definition" and §2023:8.4.6.8 "the program-name of
    /// a containing program definition" with no REPOSITORY entry. The two are the same sentence over two namespaces,
    /// and in §2023:8.4.6.6 "the containing function definition" can only be the element's OWN definition (a function
    /// definition contains no other source unit — §2023:10.2 gives nested programs to "a program definition for an
    /// outermost program" alone), so "containing" names the definition the reference is written in and every one
    /// enclosing it: <paramref name="unit"/> itself, then each program that contains it. All of them are of
    /// <paramref name="unit"/>'s own kind. A class forest (<paramref name="unit"/> null) has none: a class is
    /// neither definition kind. The program registry (<see cref="ProgramPrototypesOf"/>), the function registry's
    /// self entry (<see cref="UserFunctionsOf"/>) and the DATA-phase TO-phrase screen
    /// (<c>DataBinder.ContainingDefinitionNames</c>, seeded from here) all read THIS set, so "is this name in
    /// scope" has one answer in every phase — they used to disagree in both directions: the screen admitted the
    /// element's own program-name, which the registry registered only under a written specifier, and refused a
    /// containing program's, which the registry always held.</summary>
    internal static IEnumerable<BoundUnit> ContainingDefinitions(BoundUnit? unit)
    {
        for (var u = unit; u is not null; u = u.Parent) yield return u;
    }

    /// <summary>The USE declaratives of the programs that contain <paramref name="unit"/>, nearest first (§1985:VII.4.6.4 GR1:
    /// the declaratives of "the separately compiled program that contains the statement" are the ones invoked, and a
    /// contained program belongs to its container's separately compiled program). A container binds before the programs
    /// it contains, so its declaratives are known when a statement of <paramref name="unit"/> asks (kb/Work PB5076).</summary>
    private static IEnumerable<BoundDeclarative> ContainerUsesOf(BoundUnit unit)
    {
        for (var u = unit.Parent; u is not null; u = u.Parent)
            foreach (var d in u.Bound?.Declaratives ?? [])
                yield return d;
    }

    /// <summary>The user-defined functions ONE source element may reference, by the function-prototype-name it
    /// WRITES — the function twin of <see cref="ProgramPrototypesOf"/> (kb/Work PB974, PB989), resolved by
    /// <see cref="RepositoryResolver.ResolveFunction"/> as of <paramref name="position"/> (determination D-R3,
    /// DESIGN-external-repository §8.3): §2023:12.3.8.4 GR11 a) / b) keyed by the EXTERNALIZED name — literal-5 when the
    /// specifier writes <c>AS literal-5</c>, else the name itself (NOTE 2) — over the definitions specified BEFORE this
    /// element and then the prototypes, then c) the external repository's entry of that externalized name; when all miss,
    /// the function the WORD names (§2023:8.4.6.7), in the group and then in the repository. No hit is REPO-1 at the specifier
    /// (SR10; DESIGN-external-repository §8.3) and leaves the name unmapped; a definition that FOLLOWS the element and has
    /// no prototype is such a name. A hit that is NOT the function the word names, while a different function does carry
    /// the word, warns (COBOL2969): the externalized name wins, as GnuCOBOL's does.
    /// A specifier naming the function definition it is written in is IGNORED (§2023:12.3.8.3 SR11; the definition is
    /// <see cref="ContainingDefinitions"/>' function member) and the name resolves to its own signature — registered LAST,
    /// as <see cref="ProgramPrototypesOf"/> registers its self entry. <paramref name="data"/> is a program unit's
    /// forest or a CLASS's OBJECT / FACTORY forest (whose specifiers are the class REPOSITORY's — §2023:12.3.4 GR1,
    /// kb/Work PB1100; <paramref name="unit"/> is then null, and <paramref name="position"/> is the class's own start).
    /// A function definition's own name is ALWAYS referable inside it (§2023:8.4.6.6), so the self entry is registered
    /// whether or not a specifier writes it: a definition does not start strictly before itself, and the group's
    /// view as of <paramref name="position"/> would otherwise drop it. A forest with no function specifier and no
    /// self entry shares the group view (no copy).</summary>
    internal static IReadOnlyDictionary<string, UserFunctionSignature> UserFunctionsOf(
        DataBinder data, BoundUnit? unit, RepositoryResolver repository, int position, ElementBinders binders)
    {
        var group = repository.Group.FunctionsAt(position);
        // §2023:8.4.6.6's no-specifier leg — the containing function definition, i.e. the element's own when it is one.
        var selves = ContainingDefinitions(unit).Where(u => u.IsFunction).ToList();
        Dictionary<string, UserFunctionSignature>? own = null;
        foreach (var (name, specifier) in data.State.Configuration.FunctionSpecifiers)
        {
            // §2023:12.3.8.3 SR11: "If the specified function-prototype-name-1 is the name of the function definition in
            // which this REPOSITORY paragraph is specified, references to function-prototype-name-1 are to that
            // function definition and this function-specifier is ignored" — whatever its AS literal says, so it is
            // never resolved (a remap there bound the recursive self-call against another function's signature,
            // kb/Work PB1084); the self entry below is the reference's target.
            if (selves.Exists(u => CobolNames.Same(name, u.Name))) continue;
            own ??= new Dictionary<string, UserFunctionSignature>(group, CobolNames.Comparer);
            using var _ = data.Edition.At(specifier.At);
            if (repository.ResolveFunction(name, specifier.ExternalizedName, position) is not { } resolved)
            {
                // None of SR10's three alternatives, nor §2023:8.4.6.7's word, holds: REPO-1 at the specifier (DESIGN-external-repository §8.3), and the
                // name is no function of the element.
                own.Remove(name);
                repository.ReportUnanswered(specifier, function: true, name, specifier.ExternalizedName, data.Edition);
                continue;
            }

            own[name] = resolved.Signature;
            if (resolved.Shadowed is { } shadowed)
            {
                data.Edition.Warning(DiagnosticCatalog.FunctionWordNamesAnotherFunction,
                    $"FUNCTION {name} activates the function externalized \"{resolved.Signature.Externalized}\" "
                    + $"(§2023:12.3.8.4 GR11 a) / b) take the details by externalized name), but {shadowed.Origin.Describe("function")} "
                    + $"also has the user-function-name {name}: it is reachable only through a REPOSITORY entry "
                    + $"FUNCTION {name} AS \"{shadowed.Signature.Externalized}\" (§2023:8.4.6.7)");
            }
        }
        foreach (var u in selves)
        {
            own ??= new Dictionary<string, UserFunctionSignature>(group, CobolNames.Comparer);
            var signature = binders.SignatureOf(u);
            own[u.Name] = new UserFunctionSignature(u.Name, u.ExternalizedName, signature.Returning, signature.Formals,
                DefinitionCompiledHere: true);   // §2023:8.4.6.6: the element's own definition
        }
        return own ?? group;
    }


    /// <summary>The per-unit screens of the group's FUNCTION-ID units, run once before any source element searches the
    /// group (<see cref="GroupRepositoryProvider"/> is the search; the table this method used to build is its
    /// <see cref="GroupRepositoryProvider.FunctionsAt"/>). The §2023:14.2 procedure-division-header rule "The RETURNING phrase
    /// shall be specified in a function definition" (:23666) is checked HERE, once per unit — even an uncalled
    /// function without RETURNING is ill-formed — and so is the same-word duplicate (COBOL1508).</summary>
    private static void CheckFunctionUnits(IReadOnlyList<BoundUnit> units, SourceMap sources, EditionContext edition,
        ElementBinders binders)
    {
        // Partition the group's FUNCTION-ID units by name into DEFINITIONS (a real body) and PROTOTYPES
        // (signature-only, §2023:11.5 Format 2). A prototype precedes all other units (§2023:10.6.2 SR1), so a naive
        // first-wins TryAdd would false-report the FOLLOWING same-name definition as a duplicate (1508) — the
        // partition prevents that.
        var defs = new Dictionary<string, BoundUnit>(CobolNames.Comparer);
        var protos = new Dictionary<string, BoundUnit>(CobolNames.Comparer);
        foreach (var u in units)
        {
            if (!u.IsFunction) continue;
            using var functionAt = edition.At(sources.NameOf(u));   // the FUNCTION-ID word: a group-level pass has no walker cursor (kb/Work PB2034)
            if (binders.Of(u).Data.Linkage.Returning is null)
                edition.Error(DiagnosticCatalog.FunctionIdReturningRequired,
                    $"FUNCTION-ID '{u.Name}': the RETURNING phrase shall be specified in a function {(u.IsPrototype ? "prototype" : "definition")} "
                    + "(ISO §2023:14.2, procedure division header) — the function cannot deliver a result without it");
            // The duplicate report moved (kb/Work PB660). It used to key on the WORD and cite §2023:8.4.6.6,
            // which is the scope of function-prototype-NAMES and says nothing about uniqueness — a real
            // clause answering a different question. Two function DEFINITIONS collide because they
            // externalize one name (§2023:8.3.2.2), the same sentence two outermost PROGRAM definitions
            // collide under, so ONE check reports both: CheckDefinitionNameUniqueness, run before this.
            // COBOL1508 survives for the clash §2023:8.4.6.7 does own — a REPOSITORY `FUNCTION word` entry
            // names the user-function-NAME, so two definitions sharing a word are ambiguous even when
            // their AS literals differ — and for PROTOTYPE units, which the externalized check excludes
            // (§2023:10.6.2 SR3 pairs a function prototype with a same-name definition on purpose).
            var bucket = u.IsPrototype ? protos : defs;
            if (bucket.TryGetValue(u.Name, out var firstSameWord))
            {
                if (u.IsPrototype || !WiseOwl.Cobol.Runtime.ExternalizedNames.Same(u.ExternalizedName, firstSameWord.ExternalizedName))
                    edition.Error(DiagnosticCatalog.FunctionIdDuplicated,
                        $"duplicate FUNCTION-ID '{u.Name}' in the compilation group — two function "
                        + $"{(u.IsPrototype ? "prototypes" : "definitions")} share one user-function-name, which "
                        + "a REPOSITORY paragraph entry can no longer name unambiguously (ISO §2023:8.4.6.7: \"A "
                        + "user-function-name may be referenced in the REPOSITORY paragraph of any source element "
                        + "that follows that function definition within the compilation group\")");
            }
            else bucket[u.Name] = u;
        }
    }

    /// <summary>Qualify a class's OBJECT/FACTORY file connectors into the run-unit registry namespace (M2-OO-1i —
    /// the OO analogue of the per-program qualification in <see cref="Bind"/>). Both halves key a file
    /// per OBJECT (§2023:9.3.9: a subclass's factory object has its own copy of the inherited file definitions, as an
    /// instance object does): a class-qualified BASE key — <c>Class::INST::name</c> / <c>Class::FACT::name</c>, where
    /// <c>Class</c> is the class's run-unit root (<see cref="OoClassSymbol.RunUnitKey"/>: its EXTERNALIZED name,
    /// §2023:8.3.2.2 2), never the declared C# name — kb/Work PB2920) — plus a minted per-object <c>__fkey</c> field
    /// (inc 4; the FACTORY half, PB1086). An EXTERNAL class file keys by its run-unit
    /// external name (§2023:13.18.22.4 GR4a — one connector shared by every describer, inc 5). Name
    /// resolution is done (bound nodes hold FileModel references), so this is a pure rename. (Relocated from the
    /// emitter, P6 Step 5 — a Bind-phase FileModel mutation.)</summary>
    private static void QualifyClassFiles(OoClassUnit cls)
    {
        // OBJECT (instance) files: one connector per object (§2023:9.1.4). A non-EXTERNAL file keeps a class-qualified
        // BASE key (the seed MintInstanceKey suffixes with a per-object #N) and a minted-key FIELD; an EXTERNAL
        // instance file keys by its run-unit external name like any describer (§2023:13.18.22.4 GR4a — inc 5).
        foreach (var f in cls.Data.Files.Declared)
            if (f is { IsExternal: true, ExternalName: { } ext })
                f.CobolName = NamingConvention.ExternalFileBand + ext;
            else if (f.IsSortMerge)
                // An SD is NOT a host connector — its store is the name-keyed in-memory CobolSort (§2023:13.4.6), and
                // OoEmitFileMembers / EmitFileRegistration both skip SDs (host = !IsSortMerge). So it must keep a
                // STATIC key (no InstanceKeyField), or FileKeyExpr would emit an undeclared this.__fkey_X for a
                // SORT/MERGE/RELEASE/RETURN in a method (M2-OO-1i review). Class-qualified for cross-class uniqueness.
                f.CobolName = cls.Symbol.RunUnitKey + "::SORT::" + f.CobolName;
            else
            {
                f.InstanceKeyField = NamingConvention.InstanceFileKeyName(f.CobolName);
                f.CobolName = cls.Symbol.RunUnitKey + NamingConvention.InstanceFileBand + f.CobolName;
            }
        // FACTORY files: a factory object is an object too (§2023:9.3.14.2). §2023:9.3.9: "Each factory object has its own copy
        // of inherited data, distinct from the copy belonging to a factory object of an inherited class", and "The
        // subclass inherits all the file definitions in the same way as the data definitions". So a non-EXTERNAL
        // factory file keeps a class-qualified
        // BASE key (::FACT::) plus a minted-key FIELD, exactly as an instance file does: a subclass factory inherits
        // the base's constructor and field initializers and runs them on its OWN factory object, so its connector is
        // a second one (a static literal made it re-register, and so replace, the base factory's). An EXTERNAL factory
        // file keys by its run-unit external name; an SD keeps a STATIC key, as in the OBJECT loop above.
        foreach (var f in cls.FactoryData.Files.Declared)
            if (f is { IsExternal: true, ExternalName: { } ext })
                f.CobolName = NamingConvention.ExternalFileBand + ext;
            else if (f.IsSortMerge)
                f.CobolName = cls.Symbol.RunUnitKey + NamingConvention.FactoryFileBand + f.CobolName;
            else
            {
                f.InstanceKeyField = NamingConvention.InstanceFileKeyName(f.CobolName);
                f.CobolName = cls.Symbol.RunUnitKey + NamingConvention.FactoryFileBand + f.CobolName;
            }
    }
}
