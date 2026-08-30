// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolNet.Binding.Bound;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Binding.Model;
using CobolNet.Binding.Passes;
using CobolNet.Common;
using CobolNet.Frontend.Generated;

using CobolNet.Compiler.Oo;

namespace CobolNet.Binding;

using Core = CobolParserCore;

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
    /// <summary>Bind the WHOLE compilation group in <paramref name="tree"/> under the targeted EDITION
    /// (<paramref name="edition"/> — bind-time rejection diagnostics accumulate there; the driver fails the
    /// compile when any exist, BEFORE emit). <paramref name="turnEvents"/> are the frontend's <c>&gt;&gt;TURN</c>
    /// directive events (ISO §7.3.25) — they build the group's compile-time TurnState (EC deep-dive D10);
    /// null/empty means the GR1 default, EC-ALL CHECKING OFF.</summary>
    public BoundCompilation Bind(Core.CompilationUnitContext tree, EditionContext edition,
        Frontend.Preprocessor.DirectiveResults? directives = null)
    {
        BindPipeline.ValidateFullChainOnce();   // the startup DAG assert over resolve prefix + group tail
        // The frontend's directive outputs travel as ONE record (kb/Work PB65); absent ⇒ every directive's default.
        directives ??= Frontend.Preprocessor.DirectiveResults.None;
        var turnEvents = directives.TurnEvents;
        var refModZlEvents = directives.RefModZeroLengthEvents;
        var flagEvents = directives.FlagEvents;
        var cobolWordsMap = directives.CobolWordsMap;

        // The group's compile-time TurnState (ISO §7.3.25; deep-dive D10) — built BEFORE binding so every unit's
        // statement binder folds the same source-ordered directive events (GR6: checking spans the compilation
        // group). Name/edition validation happens here (SR2 + the 2023-only families).
        var turn = TurnState.Build(turnEvents, edition);
        // The group's compile-time REF-MOD-ZERO-LENGTH resolution (ISO §7.3.23) — the per-line zero-length
        // allowance fold every ReferenceResolver queries when building a ref-mod Place (§8.4.3.3.4 item 5c).
        var refModZl = RefModZeroLengthState.Build(refModZlEvents);

        var (units, classes, table) = CollectUnits(tree, edition);
        var session = new BindSession
        {
            Turn = turn, OoClasses = table, Edition = edition, RefModZeroLength = refModZl,
            CobolWords = cobolWordsMap ?? CobolNet.Editions.CobolWordsMap.Empty,
            LeapSecond = directives.LeapSecondOn,
        };
        var oo = new OoDriver(session);   // P9 R1 — the OO bind driver is a binder collaborator, not an emitter seam
        foreach (var iface in table.Interfaces) oo.BindInterfaceData(iface);   // prototype formals (§10.6.2 SR4)
        foreach (var cls in classes) oo.BindClassData(cls);   // ALL signatures before ANY body (D1 pass-1)
        // §14.9.23.4 GR7c's METHOD-side half, folded once now that every method symbol exists. See
        // OoMethodSymbol.OoUniversalCheckingHere for why this is bind-time and why the METHOD-ID line is the
        // query point.
        foreach (var cls in table.Classes)
            foreach (var m in cls.Methods.Concat(cls.FactoryMethods))
                // A PROPERTY accessor SYNTHESIZED from a PROPERTY clause (§13.18.42) has no METHOD-ID in source
                // and therefore no method context at all — reading `m.Ctx.Start` on one is a NullReferenceException
                // that takes down the whole bind, which is exactly what it did to oo_property/oo_property_ref.
                // Its enclosing CLASS-ID line is the right query point: the accessor is part of that definition
                // and has no source position of its own to disagree with.
                m.OoUniversalCheckingHere = turn.Enabled(
                    "EC-OO-UNIVERSAL", null, (m.Ctx?.Start ?? cls.Ctx.Start).Line);

        OoConformance.ValidateOverrideSignatures(table, edition);   // §9.3.8.2 — after all formals resolve (slice 3a)
        var ooAdapters = OoConformance.ValidateImplements(table, edition);   // §9.3.11 via §9.3.8.2.3 (D-I1 — the binder is the authority; returns the covariant adapters)
        foreach (var cls in classes) oo.BindClassBody(cls);
        // TWO-PHASE program-unit binding (M2-UDF-1 key enabler): EVERY unit's DATA division binds before ANY
        // unit's procedure body binds (the ProcedureBinding group pass below), so a function-identifier reference
        // resolves the callee's RETURNING / USING signatures even when the FUNCTION-ID unit FOLLOWS the caller in
        // the compilation group (§8.4.3.2.4 GR1 — the caller-side temporary takes the callee's RETURNING
        // description; the same forward-reference discipline OoClassTable D1 gives typed object references).
        foreach (var unit in units) BindUnitData(unit, session);

        // The whole-group middle-end (P6 Steps 3–4): the DECLARED manifest — ProcedureBinding →
        // UsageCollectionPass → StorageFormPass → VersionConformancePass (the NAMED terminal pass, the SOLE
        // edition gate; each pass's doc lives with its body; the ORDER lives in BindPipeline.GroupTail,
        // DAG-validated against the resolve prefix above). Once the tail completes, the edition sink carries
        // EVERY edition diagnostic — the driver's CheckOnly verdict needs nothing beyond this Bind.
        var ctx = new GroupBindContext(tree, units, classes, oo.InterfaceData, session);
        foreach (var pass in BindPipeline.GroupTail())
        {
            RequireAll(ctx, pass);      // watermark gate: the prerequisite RAN on every binder (P6 Step 6)
            pass.Run(ctx);
            foreach (var d in ctx.AllBinders()) d.MarkProduced(pass.Produces);
        }

        // The migration-flagging pass (ISO §7.3.14 FLAG-02 / §7.3.15 FLAG-14) — a SIBLING to the terminal
        // VersionConformancePass, run right after it: it is an orthogonal axis (directive-state-driven, always a
        // Warning, fires regardless of --std), so it is NOT a GroupTail manifest pass. A no-op (no parse-tree walk)
        // when the source carries no >>FLAG directive — the zero-overhead invariant.
        global::CobolNet.Validation.FlagConformancePass.Run(ctx, FlagState.Build(flagEvents), edition);

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
                    ? NamingConvention.ExternalFileBand + ext
                    : unit.Path + "::" + file.CobolName;

        // VCR 18/31 (ISO §12.4.5.3 GR1(i)/(h); §14.8.4.2; Annex E.2 items 12/24) — a COBOL-2023 requirement: all
        // corresponding file control entries of an EXTERNAL file connector in the run unit shall specify FILE STATUS
        // (VCR 18) and, for a relative file, RELATIVE KEY (VCR 31) naming the SAME corresponding external data item.
        // Version-conditioned structural SR ⇒ read DialectLevel directly (the binder-reads-edition doctrine). The
        // compile-time reach is the group's units only; true separate-compilation conformance is the VCR-15 runtime
        // EC-EXTERNAL-DATA-MISMATCH descriptor check.
        if (edition.DialectLevel >= 2023) CheckExternalFileConsistency(units, edition);
        // The OO analogue (M2-OO-1i): an OBJECT/FACTORY file connector is scoped to its class, not a program unit,
        // so the program loop above never sees it. A factory file (singleton) keys by class; an instance file keys
        // per object (a minted key held in a __fkey field — see QualifyClassFiles); an EXTERNAL class file keys
        // by its run-unit external name, exactly like a program's.
        foreach (var cls in classes) QualifyClassFiles(cls);

        // Declaratives emit the __IoCheck/__RunUse machinery, which reads CobolFile even when the unit declares
        // NO files (IC401M: mode-scoped USE procedures in a file-less flagging program) — the IO using must
        // cover both. A class-only file program (M2-OO-1i — an OBJECT/FACTORY file with no program-unit file)
        // needs it too, or the generated <c>CobolFile.Register</c>/OPEN in the class body has no CobolNet.Runtime.IO
        // import (CS0103).
        bool anyFiles = units.Any(u => u.Data.Files.Count > 0)
            || units.Any(u => u.Bound.Declaratives is { Count: > 0 })
            || classes.Any(c => c.Data.Files.Count > 0 || c.FactoryData.Files.Count > 0);

        return new BoundCompilation(tree, units, classes, table, oo.InterfaceData, ooAdapters, turn, ecActive,
            anyFiles);
    }

    /// <summary>The ≥2023 external-file conformance check (ISO §14.8.4.2; §12.4.5.3 GR1(i)/(h); Annex E.2 items 9/12/24).
    /// Two conjuncts of §14.8.4.2. <b>Conjunct 1 — externality (COBOLNET1624, per connector, ANY describer count):</b>
    /// a specified FILE STATUS / RELATIVE KEY / LINAGE data item shall itself be an external data item. <b>Conjunct 2 —
    /// consistency (VCR 18/31, COBOLNET1573/1575, needs ≥2 in-group describers):</b> "if any specifies it, all shall",
    /// each naming the SAME corresponding external data item. Cross-compilation sameness (separately-built assemblies)
    /// is the runtime <c>ExternalTable</c> EC-EXTERNAL-DATA-MISMATCH check's face. (In-group LINAGE consistency,
    /// §13.4.5.4 GR2(c), is a separate longstanding requirement — not this 2023-gated check.)</summary>
    private static void CheckExternalFileConsistency(IReadOnlyList<BoundUnit> units, EditionContext edition)
    {
        var byExternalName = units.SelectMany(u => u.Data.Files)
            .Where(f => f is { IsExternal: true, ExternalName: not null })
            .GroupBy(f => f.ExternalName!.ToUpperInvariant());
        // The 2023 requirement REMOVES a prior-edition freedom (corresponding external-file SELECTs could name
        // inconsistent / non-external FILE STATUS + RELATIVE KEY items), so its severity follows the removal policy:
        // an Error under strict, downgraded to a Warning under --permissive migration mode — the same contract the
        // continuity witnesses need (an 85 NIST program that violates the new rule, e.g. IC227A with two differently-
        // named non-external FILE STATUS items, still COMPILES under permissive and rejects under strict).
        var severity = EditionSeverityPolicy.For(ConstructAvailability.Removed, edition.Edition);
        // §14.8.4.2 conjunct 1 — EXTERNALITY: a FILE STATUS / RELATIVE KEY / LINAGE data item associated with an
        // external file connector shall ITSELF be an external data item. One shared clause-parameterized diagnostic
        // (the §14.8.4.2 sentence names all three items together); the E.2-item-9 conformance-checking mechanism is
        // the 2023 addition, so it inherits the same Removed-freedom severity as the 1573/1575 consistency siblings.
        EditionDiagnostic Externality(string ext, string clause, string name) =>
            new("COBOLNET1624", severity, "external-file-item-not-external",
                $"external file '{ext}': the {clause} data item '{name}' shall be an external data item "
                + "(ISO §14.8.4.2; Annex E.2 item 9)",
                $"external file '{ext}'", "ISO §14.8.4.2 / Annex E.2 item 9");
        foreach (var group in byExternalName)
        {
            var conns = group.ToList();
            // Conjunct 1 (externality) is enforced for EVERY external connector, regardless of describer count — a
            // lone-program external file whose file-referencing item is non-external is a §14.8.4.2 violation.
            foreach (var f in conns)
            {
                if (f.FileStatusName is not null && ExternalItemIdentity(f.FileStatusItem) is null)
                    edition.Report(Externality(group.Key, "FILE STATUS", f.FileStatusName));
                if (f.Organization == FileOrganization.Relative && f.RelativeKeyName is not null
                    && ExternalItemIdentity(f.RelativeKeyItem) is null)
                    edition.Report(Externality(group.Key, "RELATIVE KEY", f.RelativeKeyName));
                if (f.Linage is { } lin)
                    foreach (var op in lin.Operands)
                        if (op.DataName is not null && ExternalItemIdentity(op.Item) is null)
                            edition.Report(Externality(group.Key, "LINAGE", op.DataName));   // literal operands are exempt
            }
            // Conjunct 2 (cross-unit CONSISTENCY, §12.4.5.3 GR1(h)/(i)) needs ≥2 in-group describers to reconcile; the
            // single-describer externality face is enforced above, and cross-compilation sameness stays with the
            // ExternalTable runtime EC-EXTERNAL-DATA-MISMATCH check.
            if (conns.Count < 2) continue;

            // VCR 18 — FILE STATUS: if ANY corresponding SELECT specifies FILE STATUS, ALL shall, each naming the
            // same corresponding external data item (§12.4.5.3 GR1(i)).
            if (conns.Any(f => f.FileStatusName is not null))
            {
                var ids = conns.Select(f => ExternalItemIdentity(f.FileStatusItem)).ToList();
                if (ids.Any(id => id is null) || ids.Distinct(StringComparer.Ordinal).Count() > 1)
                    edition.Report(new EditionDiagnostic("COBOLNET1573", severity, "external-file-status-consistency",
                        $"external file '{group.Key}': all corresponding SELECT statements in the run unit shall "
                        + "specify FILE STATUS naming the same corresponding external data item "
                        + "(ISO §12.4.5.3 GR1(i); Annex E.2 item 12)",
                        $"external file '{group.Key}'", "ISO §12.4.5.3 GR1(i) / §14.8.4.2 / Annex E.2 item 12"));
            }

            // VCR 31 — RELATIVE KEY: for an external RELATIVE file, if ANY corresponding SELECT specifies RELATIVE
            // KEY, ALL shall, each naming the same corresponding external data item (§12.4.5.3 GR1(h)).
            if (conns.Any(f => f.Organization == FileOrganization.Relative)
                && conns.Any(f => f.RelativeKeyName is not null))
            {
                var ids = conns.Select(f => ExternalItemIdentity(f.RelativeKeyItem)).ToList();
                if (ids.Any(id => id is null) || ids.Distinct(StringComparer.Ordinal).Count() > 1)
                    edition.Report(new EditionDiagnostic("COBOLNET1575", severity, "external-relative-key-consistency",
                        $"external relative file '{group.Key}': all corresponding SELECT statements in the run unit "
                        + "shall specify RELATIVE KEY naming the same corresponding external data item "
                        + "(ISO §12.4.5.3 GR1(h); Annex E.2 item 24)",
                        $"external relative file '{group.Key}'", "ISO §12.4.5.3 GR1(h) / §14.8.4.2 / Annex E.2 item 24"));
            }
        }
    }

    /// <summary>The "corresponding external data item" identity of <paramref name="item"/> (ISO §14.8.4.2): the
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
        return string.Join(".", path).ToUpperInvariant();
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

        // program-id-recursive-2002: the pass owns the edition gate (Exec Step E).
        if (initial && recursive)
            edition.Error("COBOLNET0886",
                $"program '{name}': INITIAL and RECURSIVE are mutually exclusive (ISO §11.10.3 SR5–6)");
        if (common && parent is null)
            edition.Error("COBOLNET0887",
                $"program '{name}': COMMON may be specified only in a CONTAINED program (ISO §11.10.3 SR4)");

        // §8.6.6 (:8821) "Functions and methods are always recursive" / §9.4 (:12529) "a user defined
        // function always possesses the recursive attribute and may call itself" — implicit, never the
        // explicit PROGRAM-ID attribute, so it rides AFTER the 0885/0886 gates. Registering Recursive here
        // is what keeps ProgramTable's §14.9.4.4 GR3f re-entry rejection (EC-PROGRAM-RECURSIVE-CALL) from
        // firing on a function's self-activation and selects the per-activation instance model (D3/D4).
        if (isFunction) recursive = true;
        // §11.10.4 GR4 (kb/Work PB133): "The RECURSIVE clause specifies that the program AND ANY PROGRAMS
        // CONTAINED WITHIN IT are recursive" — the attribute inherits down the containment tree (parents are
        // built before their children, so one parent read cascades transitively). The §11.10.3 SR5–6
        // exclusivity gate above sees only the WRITTEN clauses, so an INITIAL containee of a recursive
        // container is legal and carries both attributes. This is what lets the legal R→C→R→C cycle
        // through GR3f's re-entry check and lets a contained program call ITSELF (§8.4.6.3 r1's "in the
        // program itself") — both drew EC-PROGRAM-RECURSIVE-CALL / NOT-FOUND before.
        if (parent is { Recursive: true }) recursive = true;

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
        // The static-WS discriminator (ISO §13.5.4 GR1 + §14.6.2.3.2/.3; the full derivation is on
        // DataBinder.UnitStaticWs): a RECURSIVE-and-not-INITIAL unit — including every FUNCTION-ID unit
        // (§8.6.6 :8821 / §9.4 :12529, the implicit attribute set in MakeUnit) — owns ONE last-used WS copy
        // shared across activations, so its WS roots emit STATIC. Scoped to units WITHOUT contained programs:
        // a containee's GLOBAL/__outer ref-bridges alias the CONTAINER INSTANCE's fields (§13.18.27 GR2), and
        // C# forbids `instance.staticField` — the composition is staged LOUD below, never half-wired (§1.4).
        // (A FUNCTION cannot contain programs, so every UDF takes the static leg.)
        bool staticWs = unit.Recursive && !unit.Initial && unit.Children.Count == 0;
        if (unit.Recursive && !unit.Initial && unit.Children.Count > 0
            && unit.Ctx.dataDivision() is { } recDd
            && (recDd.workingStorageSection() is not null || recDd.fileSection() is not null))
            // The FILE SECTION joined the staged loud with kb/Work PB168: §8.6.4 puts its record areas in
            // the SAME static one-copy sentence as WS, and the same GLOBAL/__outer bridge composition gap
            // applies (an FD may be GLOBAL, §13.18.27).
            edition.Error(DiagnosticCatalog.RecursiveContainedWs,
                $"program '{unit.Name}': a RECURSIVE program that directly contains programs and declares "
                + "WORKING-STORAGE or a FILE SECTION is recognized but not yet implemented — the "
                + "shared-static storage model (ISO §13.5.4 GR1 / §8.6.4 / §14.6.2.3.3) does not yet "
                + "compose with contained-program GLOBAL bridges (§13.18.27 GR2)");
        var data = new DataBinder(edition)
        {
            OoClasses = session.OoClasses,
            RefModZeroLength = session.RefModZeroLength,
            CobolWords = session.CobolWords,   // >>COBOL-WORDS intrinsic-function-name synonym/removal (§7.3.10)
            LeapSecond = session.LeapSecond,   // >>LEAP-SECOND ON — the §15.3 seconds-subfield / time-form bound (§7.3.17)
            // The ANY LENGTH placement facts (ISO §13.18.2.3 SR2–SR4 — the rules differ for a contained
            // program, a function, and an outermost program): the unit kind is known only here.
            UnitIsContained = unit.Parent is not null,
            UnitIsFunction = unit.IsFunction,
            UnitStaticWs = staticWs,
            // The FILE-CONNECTOR twin of staticWs, WITHOUT the childless conjunct (kb/Work PB168): the
            // static registration flag never crosses an __outer bridge, so containees do not constrain it.
            UnitStaticFiles = unit.Recursive && !unit.Initial,
        };
        data.CallSeedUids(session.TakeUidBand());

        // Pre-seed inherited GLOBAL-table index names BEFORE Bind: the child's own INDEXED BY registrations then
        // allocate from a later ordinal and can never collide with a bridged container index field. The seeded
        // fields are SUPPRESSED from this unit's field emission — a global index-name is SHARED storage
        // (ISO §13.18.27 GR2), reached through the ref-bridge, never re-declared locally. (Writes through the
        // ONE domain mutator — the collections are read-only views since P6 Step 5.)
        for (var anc = unit.Parent; anc is not null; anc = anc.Parent)
            foreach (var g in anc.Data.CallGlobalRoots)
                foreach (string idxName in IndexNamesUnder(g))
                    if (anc.Data.IndexFields.TryGetValue(idxName, out string? field))
                        data.SeedInheritedGlobalIndex(idxName, field);

        // Configuration-section + OPTIONS inheritance (ISO §12.3.4 GR1 / §11.9.4 GR1; §12.3.3 SR1 — a contained
        // program cannot have its own configuration section): the WHOLE configuration-derived state of the
        // container — SPECIAL-NAMES (DECIMAL-POINT, CURRENCY, classes, alphabets, switches), the PROGRAM
        // COLLATING SEQUENCE, DEBUGGING MODE, the REPOSITORY specifiers — and the OPTIONS baseline, copied in
        // BEFORE this unit binds so its first literal and PICTURE already see them (kb/Work PB60 /
        // AR-15.67.3-5: only the REPOSITORY sets were inherited, after Bind, and a contained program under
        // DECIMAL-POINT IS COMMA parsed NUMVAL("123,45") as 0). One level suffices: the container inherited
        // from ITS container before it bound (units bind container-first).
        if (unit.Parent is not null) data.InheritConfiguration(unit.Parent.Data);

        data.Bind(unit.Ctx);
        unit.Data = data;

        // The unit's EC-EXTERNAL enablement facts (ISO §14.8.4.1): the ACTIVATED-element mask is the group
        // TurnState folded at the unit's first post-Identification division header line ("which for activated
        // runtime elements shall be before the Environment division"); the group-level Describe gate is any
        // EC-EXTERNAL enabling event anywhere in the group (a mask-zero unit still registers its descriptions
        // so a later-enabled element can check against them; no event anywhere ⇒ zero-scaffolding).
        int divLine = unit.Ctx.environmentDivision()?.Start.Line
            ?? unit.Ctx.dataDivision()?.Start.Line
            ?? unit.Ctx.procedureDivision()?.Start.Line
            ?? int.MaxValue;
        int extMask = 0;
        if (session.Turn.Enabled("EC-EXTERNAL-FORMAT-CONFLICT", null, divLine)) extMask |= (int)Runtime.ExternalChecks.FormatConflict;
        if (session.Turn.Enabled("EC-EXTERNAL-DATA-MISMATCH", null, divLine)) extMask |= (int)Runtime.ExternalChecks.DataMismatch;
        if (session.Turn.Enabled("EC-EXTERNAL-FILE-MISMATCH", null, divLine)) extMask |= (int)Runtime.ExternalChecks.FileMismatch;
        data.ExternalCheckMask = extMask;
        // §14.9.4.4 GR3d's ACTIVATED half (kb/Work PB133 wave C2b) — the same before-Environment-division
        // fold as the EC-EXTERNAL mask above.
        data.ArgMismatchChecking = session.Turn.Enabled("EC-PROGRAM-ARG-MISMATCH", null, divLine);
        data.ExternalDescribe = Procedure.EcBinder.ExternalNames.Any(session.Turn.AnyEnabledFor);

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

    /// <summary>ALWAYS-ON (P6 Step 6): assert <paramref name="pass"/>'s declared prerequisite phase has been
    /// PRODUCED on every binder of the group before it runs (a per-pass integer compare per binder — immaterial;
    /// was Debug-only until CI's Release leg exposed the divergence, DEVLOG 774).</summary>
    private static void RequireAll(GroupBindContext ctx, GroupBindPass pass)
    {
        foreach (var d in ctx.AllBinders()) d.Require(pass.Requires, pass.Name);
    }

    /// <summary>The <c>ProcedureBinding</c> GROUP pass body (P6 Step 3 — <c>BindPipeline.GroupTail</c>, Requires
    /// <c>FilesResolved</c>, Produces <c>ProcedureBound</c>): build the group's user-function signature table, then
    /// bind every unit's PROCEDURE DIVISION.</summary>
    internal static void BindProcedures(GroupBindContext ctx)
    {
        var userFunctions = BuildUserFunctionTable(ctx.Units, ctx.Session.Edition);
        foreach (var unit in ctx.Units) BindUnitProcedure(unit, userFunctions, ctx.Session);
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
            // kb/Work PB131 — the AS NESTED callee set (§14.9.4.3 SR15): the caller's directly-contained
            // children, plus every COMMON program contained in a (transitive) ancestor — the §10.7.2
            // visibility the runtime ResolveVisible applies, computed statically so GR9's formal-mode
            // lookup and SR15's scope check both happen at BIND time.
            NestedCallables = NestedCallablesOf(unit),
            UnitRecursive = unit.Recursive,   // §14.9.7.3 SR1 / §14.9.36.3 SR1 (kb/Work PB137)
        };
        binder.ConfigureEc(session.Turn, unit.Name);   // the EC bind context (TURN fold + §15.30 location element)
        unit.Bound = binder.Bind(unit.Ctx);
        // The boundary-copied GROUP formals + RETURNING item are registered whole-group-referenced (so StorageFormPass
        // flips their numeric-DISPLAY leaves to image storage, and the formal's FromImage/AsImage round trip
        // type-checks — ISO §14.2.3 GR8 / §14.9 MOVE GR4) by the post-bind UsageCollectionPass, from data.LinkageFormals
        // + data.LinkageReturning. The pre-flip early-resolve of every formal existed ONLY for that side effect (which
        // ReferenceResolver no longer performs) — deleted, PHASE-05 Step 5.
    }

    /// <summary>The AS NESTED callee table for one caller (kb/Work PB131; §14.9.4.3 SR15 + §10.7.2):
    /// name → the callee's bound PD-header formals. Directly-contained children first; a COMMON program
    /// contained in an ancestor is visible too (nearest wins on a name clash, matching §10.7.2's scope).</summary>
    private static Dictionary<string, IReadOnlyList<LinkageFormal>> NestedCallablesOf(BoundUnit unit)
    {
        var map = new Dictionary<string, IReadOnlyList<LinkageFormal>>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in unit.Children)
            map.TryAdd(c.Name, c.Data.LinkageFormals);
        for (var anc = unit.Parent; anc is not null; anc = anc.Parent)
            foreach (var c in anc.Children)
                if (c.Common)
                    map.TryAdd(c.Name, c.Data.LinkageFormals);
        return map;
    }

    /// <summary>Build the compilation group's user-function signature table (name → bound RETURNING item +
    /// USING formals), between the DATA and PROCEDURE bind phases: FUNCTION-ID units only (ISO §9.4 — the
    /// binder's function namespace never sees PROGRAM-ID units; §8.4.6.6 scope of function-prototype-names).
    /// The §14.2 procedure-division-header rule "The RETURNING phrase shall be specified in a function
    /// definition" (:23666) is checked HERE, once per unit — even an uncalled function without RETURNING is
    /// ill-formed.</summary>
    private static Dictionary<string, UserFunctionSignature> BuildUserFunctionTable(
        IReadOnlyList<BoundUnit> units, EditionContext edition)
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

    /// <summary>Qualify a class's OBJECT/FACTORY file connectors into the run-unit registry namespace (M2-OO-1i —
    /// the OO analogue of the per-program qualification in <see cref="Bind"/>). A FACTORY file (the class
    /// singleton, §9.3.14.2) keys by class — <c>Class::FACT::name</c>; an EXTERNAL class file keys by its run-unit
    /// external name (§13.18.22.4 GR4a — one connector shared by every describer, inc 5). An OBJECT (instance) file
    /// is per-object (inc 4): a class-qualified BASE key plus a minted per-object <c>__fkey</c> field. Name
    /// resolution is done (bound nodes hold FileModel references), so this is a pure rename. (Relocated from the
    /// emitter, P6 Step 5 — a Bind-phase FileModel mutation.)</summary>
    private static void QualifyClassFiles(OoClassUnit cls)
    {
        // OBJECT (instance) files: one connector per object (§9.1.4). A non-EXTERNAL file keeps a class-qualified
        // BASE key (the seed MintInstanceKey suffixes with a per-object #N) and a minted-key FIELD; an EXTERNAL
        // instance file keys by its run-unit external name like any describer (§13.18.22.4 GR4a — inc 5).
        foreach (var f in cls.Data.Files)
            if (f is { IsExternal: true, ExternalName: { } ext })
                f.CobolName = NamingConvention.ExternalFileBand + ext;
            else if (f.IsSortMerge)
                // An SD is NOT a host connector — its store is the name-keyed in-memory CobolSort (§13.4.6), and
                // OoEmitFileMembers / EmitFileRegistration both skip SDs (host = !IsSortMerge). So it must keep a
                // STATIC key (no InstanceKeyField), or FileKeyExpr would emit an undeclared this.__fkey_X for a
                // SORT/MERGE/RELEASE/RETURN in a method (M2-OO-1i review). Class-qualified for cross-class uniqueness.
                f.CobolName = cls.CsName + "::SORT::" + f.CobolName;
            else
            {
                f.InstanceKeyField = "__fkey_" + DataItem.Sanitize(f.CobolName);
                f.CobolName = cls.CsName + NamingConvention.InstanceFileBand + f.CobolName;
            }
        // FACTORY files: the class singleton (§9.3.14.2) — a static class-qualified key (no per-object field).
        foreach (var f in cls.FactoryData.Files)
            f.CobolName = f is { IsExternal: true, ExternalName: { } ext }
                ? NamingConvention.ExternalFileBand + ext
                : cls.CsName + NamingConvention.FactoryFileBand + f.CobolName;
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
