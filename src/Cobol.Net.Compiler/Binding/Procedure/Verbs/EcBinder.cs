// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Generated;
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// The EC exception-condition binder (P7 Step 10r; ISO/IEC 1989:2023 §14.6.13;
/// COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN D9–D12): binds RAISE (§14.9.29), RESUME (§14.9.33), SET LAST
/// EXCEPTION TO OFF (§14.9.39 F13), the GOBACK/EXIT RAISING phrase (§14.9.18/§14.9.14), and — because bound
/// nodes carry no parse context — performs the compile-time TurnState fold HERE (the statement's source line
/// is a parse-tree property): a statement whose kind has any enabled relevant exception-name is wrapped in
/// <see cref="BoundEcChecked"/> carrying the bind-time decision; a statement with none binds UNwrapped, so
/// checking-off emits nothing new (deep-dive D10 / SSOT §18.16). State lives on
/// <see cref="BinderContext.EcState"/> (shared with the Declaratives half) and the bind cursor on
/// <see cref="BinderContext.BindCursor"/>; <c>EcWrap</c> is still invoked at the host BindStatement exit.
/// The intrinsic-presence walks moved VERBATIM — their generated-visitor conversion stays FLAGGED as a
/// behavior-sensitive follow-up (the plan block). Host edges (InMethod/OoClasses/ResolveProcedure/
/// Declaratives/EntryPc/Paragraphs/ParaSections) flip at 10t.
/// </summary>
internal sealed partial class EcBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>Configure the EC bind context (called per bound unit — <see cref="BinderDriver"/> for program
    /// units, the OO bind half for class rosters):
    /// the compilation group's TurnState and this unit's PROGRAM-ID name (the §15.30.3 r2 location element).</summary>
    public void ConfigureEc(TurnState turn, string programName)
    {
        ctx.EcState.Turn = turn;
        ctx.EcState.ProgramName = programName;
    }

    /// <summary>Record a FUNCTION EXCEPTION-* binding (sets the group EC gate so the generated source carries
    /// the runtime using; called from the intrinsic bind when the catalog row is an Ec* runtime method).</summary>
    public void EcNoteFunction() => ctx.EcState.Functions = true;

    // ── RAISE (§14.9.29) ─────────────────────────────────────────────────────────────────────────────────────

    public BoundStatement BindRaise(Core.RaiseStatementContext r)
    {
        // raise-statement-2002: the pass owns the edition gate (Exec Step E).
        if (r.cobolWord() is not { } ecWord)
        {
            // RAISE identifier-1 — an exception OBJECT (§14.9.29.3 SR2/SR3; §14.6.13.1.5). NOT TURN-gated
            // (§7.3.25 takes exception-NAMES only) and never fatal by itself (GR2).
            var oref = r.objectReference();
            if (oref.NULL_() is not null || oref.SUPER() is not null)
            {
                ctx.Edition.Error("COBOLNET0848",
                    $"RAISE {(oref.NULL_() is not null ? "NULL" : "SUPER")}: NULL and SUPER shall not be "
                    + "specified as the raised object (ISO §14.9.29.3 SR2)");
                return new BoundNop();
            }
            ctx.EcState.Raise = true;   // the machinery gate — the object channel is live once used
            if (oref.SELF() is not null)
            {
                if (!host.InMethod)
                {
                    ctx.Edition.Error("COBOLNET0848",
                        "RAISE SELF may be specified only within a method definition (ISO §8.4.3.8)");
                    return new BoundNop();
                }
                return new BoundRaiseObject(null);
            }
            if (ctx.Refs.Resolve(oref.dataReference()!) is not { } op
                || op.Item.Pic?.Category is not PicCategory.ObjectReference)
            {
                ctx.Edition.Error("COBOLNET0848",
                    $"RAISE '{oref.GetText()}': identifier-1 shall be a USAGE OBJECT REFERENCE data item "
                    + "(ISO §14.9.29.3 SR2)");
                return new BoundNop();
            }
            return new BoundRaiseObject(op);
        }

        if (EcResolveLevel3(ecWord.GetText(), "RAISE") is not { } info)
            return new BoundNop();   // diagnosed — fail the compile, bind a placeholder
        ctx.EcState.Raise = true;
        int line = r.Start.Line;
        bool enabled = ctx.EcState.Turn.Enabled(info.Name, null, line);
        bool withLoc = enabled && ctx.EcState.Turn.WithLocation(info.Name, null, line);
        return new BoundRaise(info.Name, info.Fatality is not EcFatality.Nonfatal, enabled, withLoc, EcLocation(line));
    }

    /// <summary>Resolve and validate a written exception-name for the RAISE/RAISING contexts — the ONE funnel
    /// (kb/Work R05) plus this site's LEVEL-3 requirement (§14.9.29.3 SR1 / §14.9.18.3 SR2, checked before the
    /// introduction gate so the level error keeps priority). Null after diagnosing.</summary>
    private EcInfo? EcResolveLevel3(string name, string context) =>
        EcNameResolution.TryResolve(ctx.Edition, name, context, out var info, requireLevel3: true)
            ? info : null;

    // ── RESUME (§14.9.33) ────────────────────────────────────────────────────────────────────────────────────

    public BoundStatement BindResume(Core.ResumeStatementContext r)
    {
        // resume-statement-2002: the pass owns the edition gate (Exec Step E).
        // SR1 — RESUME may appear in a declarative OR a WHEN phrase of an exception-checking PERFORM (§14.9.33.3
        // SR1). In a WHEN phrase it shall specify NEXT STATEMENT (XS-RESUME-OPERAND, COBOLNET1610) — the
        // ResumeSignal(targetPc) pc-jump path is bound ONLY for a declarative RESUME AT procedure-name.
        if (ctx.EcState.InF3When)
        {
            ctx.EcState.Resume = true;
            if (r.NEXT() is not null) return new BoundResume(ResumeSignal.NextStatement);
            ctx.Edition.Error("COBOLNET1610", "RESUME in a WHEN phrase of an exception-checking PERFORM shall "
                + "specify NEXT STATEMENT (ISO §14.9.33.3 SR1)");
            return new BoundNop();
        }
        // The declarative sections occupy the pcs below EntryPc (StatementBinder.Declaratives.cs).
        var decl = ctx.Table.Declaratives.FirstOrDefault(d => ctx.BindCursor >= d.StartPc && ctx.BindCursor <= d.EndPc);
        if (ctx.BindCursor >= ctx.Table.EntryPc || decl is null)
        {
            // XS-RESUME-PLACEMENT (§14.9.28.3): a RESUME in imperative-statement-1 or FINALLY of an F3 PERFORM
            // (neither a declarative nor a WHEN phrase) lands here too — the same "declarative or WHEN only" rule.
            ctx.Edition.Error("COBOLNET0712", "RESUME may be specified only in a declarative or a WHEN phrase of "
                + "an exception-checking PERFORM (ISO §14.9.33.3 SR1)");
            return new BoundNop();
        }
        // SR2 — not in a GLOBAL-phrase declarative (a RESUME executed within a global declarative's DYNAMIC
        // scope is CONTINUE, GR1 — realized by __RunGlobalUse swallowing the signal; the STATIC case rejects).
        if (decl.Global)
        {
            ctx.Edition.Error("COBOLNET0713", "RESUME shall not be specified in a declarative procedure whose "
                + "USE statement carries the GLOBAL phrase (ISO §14.9.33.3 SR2)");
            return new BoundNop();
        }
        ctx.EcState.Resume = true;
        if (r.NEXT() is not null) return new BoundResume(ResumeSignal.NextStatement);

        // SR3 — procedure-name-1 shall be in the NONdeclarative portion.
        var pn = r.procedureName()!;
        if (ctx.Table.ResolveProcedure(pn) is not { } target)
            return new BoundUnsupported($"RESUME AT unknown procedure '{pn.GetText()}'");
        if (target.Start < ctx.Table.EntryPc)
        {
            ctx.Edition.Error("COBOLNET0714", $"RESUME AT '{pn.GetText()}': the procedure shall be in the "
                + "nondeclarative portion of the program (ISO §14.9.33.3 SR3)");
            return new BoundNop();
        }
        return new BoundResume(target.Start);   // GR3 — as if GO TO procedure-name-1
    }

    // ── SET LAST EXCEPTION TO OFF (§14.9.39 Format 13) ───────────────────────────────────────────────────────

    public BoundStatement BindSetLastException()
    {
        // set-last-exception-2002: the pass owns the edition gate (Exec Step E).
        ctx.EcState.Functions = true;   // touches the runtime last-exception register — the group EC gate
        return new BoundSetLastException();
    }

    // ── The GOBACK / EXIT PROGRAM RAISING phrase (§14.9.18 / §14.9.14 F2) ────────────────────────────────────

    /// <summary>Bind a RAISING phrase. Returns null for the identifier (exception-object) form — the caller
    /// degrades to a loud placeholder until the OO wave.</summary>
    public BoundRaising? EcBindRaising(Core.RaisingPhraseContext raising, int line, string verb)
    {
        // statement-raising-2002: the pass owns the edition gate (Exec Step E).
        ctx.EcState.Raising = true;
        if (raising.LAST() is not null) return new BoundRaising(null, IsLast: true, Fatal: false, Enabled: true);
        if (raising.cobolWord() is not { } ecWord)
        {
            // The identifier leg (§14.9.18.3 SR4 / §14.9.14.3 — the EC-OO wave): propagate an exception
            // OBJECT to the activator. SR4d: never a universal reference. SR4a: the DECLARED class (or a
            // superclass) shall appear in the containing source element's PD-header RAISING phrase —
            // discharged at COMPILE time, which makes the activated-side EC-OO-EXCEPTION rule-1 check
            // STATICALLY true in v1 (D-EO5: a typed reference only ever holds a conforming object, no
            // universal identifier-1 exists, and factory objects cannot enter a typed reference).
            if (raising.dataReference() is not { } dref) return null;
            if (ctx.Refs.Resolve(dref) is not { } op
                || op.Item.Pic is not { Category: PicCategory.ObjectReference } opic)
            {
                ctx.Edition.Error("COBOLNET0849",
                    $"{verb} RAISING '{dref.GetText()}': identifier-1 shall be a USAGE OBJECT REFERENCE "
                    + "data item (ISO §14.9.18.3 SR4)");
                return null;
            }
            if (opic.ObjectClassName is not { } declared)
            {
                ctx.Edition.Error("COBOLNET0849",
                    $"{verb} RAISING '{op.Item.CobolName}': identifier-1 shall not be a UNIVERSAL object "
                    + "reference (ISO §14.9.18.3 SR4d)");
                return null;
            }
            bool listed = false;
            for (var c = host.OoClasses?.Find(declared); c is not null; c = c.Base)
                if (ctx.EcState.PdRaisingClasses.Contains(c.Name)) { listed = true; break; }
            if (!listed)
                ctx.Edition.Error("COBOLNET0849",
                    $"{verb} RAISING '{op.Item.CobolName}': its declared class '{declared}' (or a "
                    + "superclass) shall be specified in the RAISING phrase of the procedure division "
                    + "header of the containing source element (ISO §14.9.18.3 SR4a)");
            return new BoundRaising(null, IsLast: false, Fatal: false, Enabled: true, ObjectSource: op);
        }

        if (EcResolveLevel3(ecWord.GetText(), $"{verb} RAISING") is not { } info)
            return new BoundRaising("EC-RAISING-IMP", false, false, false);   // diagnosed; placeholder
        // SR2 (§14.9.18.3 / 27684): an EC-USER name shall appear in the PD-header RAISING phrase — the
        // statically detectable half binds as an error; the runtime condition is EC-RAISING-NOT-SPECIFIED.
        if (info.Level2Parent is "EC-USER" && !ctx.EcState.PdRaising.Contains(info.Name))
            ctx.Edition.Error("COBOLNET0717", $"{verb} RAISING {info.Name}: an EC-USER exception-name shall be "
                + "specified in the RAISING phrase of the procedure division header (ISO §14.9.18.3 SR2 — "
                + "otherwise EC-RAISING-NOT-SPECIFIED, Table 13)");
        // kb/Work R07: the location operands travel like BoundRaise's — WITH LOCATION per THIS name at THIS
        // line (§7.3.25.4 GR7); the statement name is the Table 12 row (verb's first word: GOBACK, or EXIT —
        // EXIT PROGRAM / FUNCTION / METHOD are formats of the EXIT statement).
        return new BoundRaising(info.Name, IsLast: false,
            Fatal: info.Fatality is not EcFatality.Nonfatal, Enabled: ctx.EcState.Turn.Enabled(info.Name, null, line),
            WithLocation: ctx.EcState.Turn.WithLocation(info.Name, null, line),
            StatementName: verb.Split(' ')[0], Location: EcLocation(line));
    }

    /// <summary>Capture the PROCEDURE DIVISION header RAISING list (§14.2.1; consumed by the SR2 check above;
    /// classes/interfaces in the list resolve at the OO wave — names are recorded uninterpreted).</summary>
    public void EcCollectPdRaising(Core.ProcedureDivisionContext pd)
    {
        if (pd.raisingClause() is not { } rc) return;
        foreach (var w in rc.cobolWord()) EcAddPdRaisingWord(w.GetText());
    }

    /// <summary>Load a METHOD's pre-partitioned header RAISING lists as the current source element's
    /// sets (per-method reset — methods of one class bind through ONE binder).</summary>
    public void EcLoadPdRaising(IReadOnlyList<string> ecNames, IReadOnlyList<string> classes)
    {
        ctx.EcState.PdRaising.Clear();
        ctx.EcState.PdRaisingClasses.Clear();
        foreach (var n in ecNames) ctx.EcState.PdRaising.Add(n);
        foreach (var c in classes) ctx.EcState.PdRaisingClasses.Add(c);
    }

    /// <summary>Partition ONE PD-header RAISING operand (§14.2.2 — the EC-OO wave, D-EO8): a catalog EC
    /// name must be level-3 EC-USER (SR7 → 0858 otherwise); a class of the group joins the SR4a class list;
    /// anything else is 0858 (SR8/SR9 — interface names are the interface-RAISING refinement).</summary>
    public void EcAddPdRaisingWord(string word)
    {
        string up = word.ToUpperInvariant();
        if (CobolNet.Runtime.Exceptions.ExceptionCatalog.TryGet(up, out var info))
        {
            // Direct TryGet, not the funnel: an unresolved word here may legally be a CLASS name (SR8/SR9),
            // so the funnel's unknown-name error does not apply — but the accepted names still get the
            // §15.33 width advisory (kb/Work R05).
            if (info.Level is 3 && info.Level2Parent is "EC-USER")
            {
                EcNameResolution.Advise(ctx.Edition, info);
                ctx.EcState.PdRaising.Add(up);
            }
            else
                ctx.Edition.Error("COBOLNET0858",
                    $"PROCEDURE DIVISION RAISING {up}: an exception-name here shall be a level-3 EC-USER "
                    + "name (ISO §14.2.2 SR7)");
            return;
        }
        if (host.OoClasses?.Find(up) is not null) { ctx.EcState.PdRaisingClasses.Add(up); return; }
        ctx.Edition.Error("COBOLNET0858",
            $"PROCEDURE DIVISION RAISING {up}: not an exception-name or a class of the compilation group "
            + "(ISO §14.2.2 SR7–SR9; interface names are a later refinement of the EC-OO wave)");
    }

    // ── The per-statement TurnState fold (deep-dive D10) ─────────────────────────────────────────────────────

    /// <summary>The EC-SIZE family an arithmetic statement can raise through the checked-store path
    /// (§14.7.5 size error ↔ Table 13: store truncation, intermediate overflow, zero divide, exponentiation).</summary>
    private static readonly string[] SizeNames =
        // EC-SIZE-UNDERFLOW was missing from this enumeration (kb/Work PB145 — the same missing-member shape
        // as DELETE FILE in QueryFor): >>TURN EC-SIZE-UNDERFLOW CHECKING ON never reached a statement's mask,
        // so a §8.8.1.5.2 r2 too-small raise was caught by ON SIZE ERROR but NEVER RECORDED — FUNCTION
        // EXCEPTION-STATUS read the stale prior name inside the phrase.
        ["EC-SIZE-TRUNCATION", "EC-SIZE-OVERFLOW", "EC-SIZE-UNDERFLOW", "EC-SIZE-ZERO-DIVIDE", "EC-SIZE-EXPONENTIATION"];

    /// <summary>The EC-I-O family raised from I-O status values (§9.1.13.1 correspondence) — THE canonical
    /// mask order (<see cref="ExceptionCatalog.IoMaskNames"/>; the emitter's per-statement mask bits).</summary>
    private static readonly string[] IoNames = ExceptionCatalog.IoMaskNames;

    /// <summary>The OO fatal conditions an INVOKE raises (§14.9.23.4 GR5 EC-OO-NULL, GR7b EC-OO-METHOD).
    /// A PRECISE per-node gate, not an ambient tail one: an INVOKE is a distinguishable bound node, so the guard
    /// binds only on an actual INVOKE under <c>&gt;&gt;TURN EC-OO-* CHECKING ON</c>.</summary>
    private static readonly string[] OoInvokeNames = ["EC-OO-NULL", "EC-OO-METHOD", "EC-OO-UNIVERSAL"];

    /// <summary>EC-FLOW-SEARCH (§14.9.39.4 GR31) — a capacity SET executed during a SEARCH of the same table.
    /// PRECISE: the only statement that can raise it is the capacity SET itself.</summary>
    private static readonly string[] FlowSearchNames = ["EC-FLOW-SEARCH"];

    /// <summary>The EC-PROGRAM family a CALL/CANCEL raises through <c>CobolCallException</c>.</summary>
    private static readonly string[] ProgramNames =
    [
        "EC-PROGRAM-NOT-FOUND", "EC-PROGRAM-RECURSIVE-CALL", "EC-PROGRAM-CANCEL-ACTIVE", "EC-PROGRAM-ARG-OMITTED",
        // §14.8.2.1 via §14.9.4.4 GR3d (kb/Work PB133 wave C2b) — the dynamic Format-1 count check at
        // activation; membership here is BOTH what lets the site's catch arm name it AND what makes
        // CallEmitter pass the ACTIVATING half of GR3d's enabled-in-both gate (siteArgMismatchChecking).
        "EC-PROGRAM-ARG-MISMATCH",
        // §14.9.4.4 GR3b — CALL through a program-pointer holding the predefined address NULL. Membership here is
        // what makes QueryFor(BoundCallProgram) report the CALL as checkable, so EcWrap wraps it in
        // BoundEcChecked and CallEmitter emits the name-filtered catch. Without it the runtime raise (ProgramTable
        // .CallPointer) has no guard to be caught by; without that raise using this name, the guard never matches.
        "EC-PROGRAM-PTR-NULL",
    ];

    /// <summary>The condition a USER-FUNCTION activation raises through <c>CobolCallException</c> that a CALL
    /// statement does not: ISO §8.4.3.2.4 GR6b — "If the function is not found, the EC-FUNCTION-NOT-FOUND
    /// exception condition is set to exist, the function is not activated, and execution continues as specified
    /// in General rule 6f", and GR6f runs "any declarative … associated with that exception condition".
    /// Queried only for a <c>BoundCallProgram</c> with <c>IsFunction</c> — the hoisted activation of a
    /// function-identifier — because that is the only node whose emitted invocation passes
    /// <c>notFoundEc: "EC-FUNCTION-NOT-FOUND"</c>. (EC-FUNCTION-PTR-NULL / EC-FUNCTION-ARG-OMITTED, GR6c/GR8,
    /// have no raise site yet and so are not named here: an unraisable name in this list would make every
    /// activation report as checkable and emit a dead catch arm.)</summary>
    private static readonly string[] FunctionActivationNames = ["EC-FUNCTION-NOT-FOUND"];

    /// <summary>The EC-EXTERNAL family a CALL raises through <c>CobolCallException</c> when the activated
    /// element's external descriptions do not conform (ISO §14.8.4 / §14.9.4.4 GR3e; the checkable trio —
    /// EC-EXTERNAL-IMP has no raise site, this implementation defines no implementor-specific external checks).
    /// The site-enabled subset ALSO drives the emitted CALL-site mask (§14.8.4.1's activating-element half).</summary>
    internal static readonly string[] ExternalNames =
    [
        "EC-EXTERNAL-FORMAT-CONFLICT", "EC-EXTERNAL-DATA-MISMATCH", "EC-EXTERNAL-FILE-MISMATCH",
    ];

    /// <summary>Wrap <paramref name="bound"/> in <see cref="BoundEcChecked"/> when the TurnState enables any
    /// exception-name RELEVANT to its kind at this statement's line (§7.3.25.4 GR6); otherwise return it
    /// untouched — the zero-scaffolding gate. The relevant set is the statement kind's raise points
    /// (the implemented families; names this implementation does not yet raise bind no wrapper — the
    /// §14.6.13.1.1 unimplemented-element license, recorded in the deep-dive).</summary>
    public BoundStatement EcWrap(Core.StatementContext s, BoundStatement bound)
    {
        if (!ctx.EcState.Turn.AnyEnabled) return bound;
        int line = s.Start.Line;
        var enabled = new List<(string Ec, FileModel? File)>();
        void Query(IEnumerable<string> names, FileModel? file = null)
        {
            foreach (string n in names)
                if (ctx.EcState.Turn.Enabled(n, file?.CobolName, line))
                    enabled.Add((n, file));
        }

        // A desugar wrapper (a hoisted user-function activation / property-op sequence) is TRANSPARENT to
        // the family selection: the carrying statement keeps ITS families, and each hoisted
        // BoundCallProgram step contributes the EC-PROGRAM family — otherwise a checked COMPUTE that also
        // carries a function reference would silently lose its EC-SIZE wrap (the M2-UDF-1 review finding;
        // the property-op sequence had the same latent hole since DEVLOG 607).
        void QueryFor(BoundStatement node)
        {
            if (node is BoundSequence seq)
            {
                foreach (var step in seq.Steps) QueryFor(step);
                return;
            }
            switch (node)
            {
                // The arithmetic statements — the structural marker, not a name list (kb/Work PB75): the statement's
                // own §14.7.5 shape (EmitArith) takes the EC-SIZE family.
                case IArithmeticStatement:
                    Query(SizeNames);
                    break;
                case BoundStringStmt:
                    Query(["EC-OVERFLOW-STRING"]);
                    break;
                case BoundUnstringStmt:
                    Query(["EC-OVERFLOW-UNSTRING"]);
                    break;
                case BoundOpen o:
                    foreach (var f in o.Files) Query(IoNames, f.File);
                    break;
                case BoundClose c:
                    foreach (var (file, _) in c.Files) Query(IoNames, file);
                    break;
                case BoundUnlock ul: Query(IoNames, ul.File); break;   // §14.9.47: UNLOCK is an I-O operation
                case BoundRead rd: Query(IoNames, rd.File); break;
                case BoundWrite wr: Query(IoNames, wr.File); break;
                case BoundRewrite rw: Query(IoNames, rw.File); break;
                case BoundKeyedRead k: Query(IoNames, k.File); break;
                case BoundKeyedWrite k: Query(IoNames, k.File); break;
                case BoundKeyedRewrite k: Query(IoNames, k.File); break;
                case BoundKeyedDelete k: Query(IoNames, k.File); break;
                // DELETE FILE (§14.9.10 F2, 2023) was the ONE I-O statement missing from this enumeration —
                // no (EC-I-O, file) pair, no __IoCheckEc mask, so GR20 b)'s enabled EC never set even after
                // the onExceptionHandled fix threaded through (kb/Work PB141).
                case BoundKeyedDeleteFile k: Query(IoNames, k.File); break;
                case BoundKeyedStart k: Query(IoNames, k.File); break;
                case BoundInvoke or BoundInvokeUniversal:
                    Query(OoInvokeNames);   // §14.9.23.4 GR5 / GR7b
                    break;
                // CA37 is PRECISE: EC-FLOW-SEARCH can only arise from a capacity SET (§14.9.39.4 GR31), which is
                // one bound node, so the guard binds exactly there. Its twin EC-BOUND-TABLE-LIMIT is NOT precise
                // — growth also happens on IMPLICIT receiving-reference growth, which renders inline — so it
                // takes the ambient tail gate below. The two are deliberately not merged.
                case BoundSetCapacity:
                    Query(FlowSearchNames);
                    break;
                // The SEARCH range conditions (§14.9.37.4 GR4/GR6/GR9) RAISE via BoundSearch's own bound
                // Check* flags — this arm exists so the statement carries the wrapper's AMBIENT context
                // (kb/Work R14): the emitted 2-argument Set calls in EmitSearchScan pick up the §15.32.3 r2 /
                // §15.30.3 r2 pair from it. Before, a WITH LOCATION no-match SEARCH answered 63 spaces.
                case BoundSearch:
                    Query(["EC-RANGE-SEARCH-INDEX", "EC-RANGE-SEARCH-NO-MATCH"]);
                    break;
                // CONTINUE AFTER (§14.9.9.4 GR1 — kb/Work PB138 fixed the §14.9.8.4 miscite, which is
                // COMPUTE's clause): the raise is CobolTiming's — bound through
                // BoundContinueAfter.CheckLessThanZero — and 2-argument; the arm supplies the ambient pair
                // exactly like SEARCH (kb/Work R14).
                case BoundContinueAfter:
                    Query(["EC-CONTINUE-LESS-THAN-ZERO"]);
                    break;
                case BoundCallProgram call:
                    Query(ProgramNames);
                    Query(ExternalNames);   // §14.9.4.4 GR3e — the CALL is the EC-EXTERNAL raise point (§14.8.4)
                    // A USER-FUNCTION activation is a DIFFERENT raise: a locate miss is EC-FUNCTION-NOT-FOUND
                    // (§8.4.3.2.4 GR6b), not EC-PROGRAM-NOT-FOUND, and GR6f sends it to "any declarative … that
                    // is associated with that exception condition". Without this Query the name never entered
                    // the statement's enabled set, so >>TURN EC-FUNCTION-NOT-FOUND CHECKING ON wrapped nothing
                    // and the condition could reach no declarative at all (kb/Work PB233).
                    if (call.IsFunction) Query(FunctionActivationNames);
                    break;
                case BoundCancel:
                    Query(ProgramNames);    // CANCEL raises no EC-EXTERNAL — external state persists (§14.9.5 GR8)
                    break;
                case BoundFree:
                    Query(["EC-STORAGE-NOT-ALLOC"]);   // §14.9.15 GR1c (nonfatal; Phase-4b inc 2)
                    break;
                // EC-RANGE-PERFORM-VARYING (fatal, §14.9.28.4 GR3): a PERFORM VARYING that initializes an index-name
                // from a non-positive FROM item raises it. Unlike the blanket ambient gates below, a PERFORM is ONE
                // identifiable node, so a PRECISE case (not a whole-statement gate) drives the FatalAmbientGates
                // wrapper — the emitted index-init check (ControlFlowEmitter) throws inside that try for USE-F3.
                case BoundInlinePerform { Control: PerformVarying { CheckIndexRange: true } }:
                case BoundOutOfLinePerform { Control: PerformVarying { CheckIndexRange: true } }:
                    Query(["EC-RANGE-PERFORM-VARYING"]);
                    break;
            }
            // ⛔ THE EC-SIZE FAMILY IS AMBIENT FOR EVERY OTHER STATEMENT TOO (kb/Work PB75). §14.7.5: the size error
            // condition "may occur as a result of … the evaluation of an arithmetic expression" — a condition, a
            // function argument, a subscript, an INVOKE argument all render inline — and without a SIZE ERROR
            // phrase the level-3 EC-SIZE-* "is set to exist, and processing proceeds as specified in 14.6.13.1.3".
            // The raise sites are unconditional (CobolSizeError, a CobolFatalException), so the guard around a
            // size-error-free statement is harmless; an ARITHMETIC statement took its family above and EmitArith
            // owns its handling — the emitter's generic guard skips it (IArithmeticStatement), so the family is not
            // queried twice here. `IF 10 ** 100000 > 5` under STANDARD-DECIMAL was an unhandled stack trace.
            if (node is not IArithmeticStatement) Query(SizeNames);
            // EC-ARGUMENT-FUNCTION rides any intrinsic-bearing statement (the ambient statement gate — the
            // intrinsic renders inline inside expressions, so the guard wraps the STATEMENT).
            if (ctx.EcState.Turn.Enabled("EC-ARGUMENT-FUNCTION", null, line) && ContainsIntrinsic(node))
                enabled.Add(("EC-ARGUMENT-FUNCTION", null));
            // EC-ORDER-NOT-SUPPORTED (fatal, §15.85.4 r2) rides the SAME ambient gate, for the same reason:
            // FUNCTION STANDARD-COMPARE renders inline inside an arbitrary expression, so the guard wraps the
            // statement and CobolIntrinsics.StandardCompare consults the flag (kb/Work PB101 T7). Harmless around
            // a STANDARD-COMPARE-free statement — no other site sets it.
            if (ctx.EcState.Turn.Enabled("EC-ORDER-NOT-SUPPORTED", null, line) && ContainsIntrinsic(node))
                enabled.Add(("EC-ORDER-NOT-SUPPORTED", null));
            // The EC-LOCALE family (kb/Work PB64 T1; DESIGN-locale-facility §4.10) rides ambient per-statement gates:
            // EC-LOCALE-MISSING and EC-LOCALE-INVALID-PTR are PRECISE — only SET LOCALE (§14.9.39.4 GR24 / GR21) and a
            // NAMED IS LOCALE collating sequence at use (§8.2.1 — inline in a relation, a SORT key, a MAX/MIN …) can
            // raise them, and the named sequence is not one node kind, so MISSING rides any statement while INVALID-PTR
            // rides the SET; EC-LOCALE-INCOMPATIBLE (§8.8.4.2.11, L6) is an inline comparison outcome — any statement.
            // Each raise fires only at its site, so a guard around an unrelated statement never catches anything.
            if (ctx.EcState.Turn.Enabled("EC-LOCALE-MISSING", null, line))
                enabled.Add(("EC-LOCALE-MISSING", null));
            if (node is BoundSetLocale && ctx.EcState.Turn.Enabled("EC-LOCALE-INVALID-PTR", null, line))
                enabled.Add(("EC-LOCALE-INVALID-PTR", null));
            if (ctx.EcState.Turn.Enabled("EC-LOCALE-INCOMPATIBLE", null, line))
                enabled.Add(("EC-LOCALE-INCOMPATIBLE", null));
            // EC-LOCALE-INVALID (§8.2.1 — incomplete locale content) rides the LOCALE intrinsics (T4: LOCALE-DATE/-TIME/
            // -TIME-FROM-SECONDS; T5 the case functions' LOCALE phrase; T6 the monetary operations), which render
            // inline — any intrinsic-bearing statement, like EC-ARGUMENT-FUNCTION — and, in a module WITH a CHARACTER
            // CLASSIFICATION (T5), ANY statement: a class test (§12.3.6.4 GR7b) is not an intrinsic-bearing statement
            // and can raise it at use (LocaleFacts.Require), as can a case function without a phrase (GR7a). Each raise
            // fires only at its site, so the guard around an unrelated statement never catches anything.
            if (ctx.EcState.Turn.Enabled("EC-LOCALE-INVALID", null, line) && (ContainsIntrinsic(node) || ctx.Data.Classification is not null))
                enabled.Add(("EC-LOCALE-INVALID", null));
            // EC-LOCALE-SIZE (§13.18.40.5 r14 b; PB64 T6 — the ONE raise site is CobolLocaleEdit.Format's move of
            // the hypothetical item into the SIZE-declared item): any statement that stores into a format-2 item
            // can raise it — a MOVE, an arithmetic store, INITIALIZE, a VALUE-composed level-88 compare never
            // (reads don't edit). Wrapped conservatively (any statement in a checking-on region), the
            // EC-BOUND-OVERFLOW precedent: the raise fires only at its site, so the guard around a
            // locale-item-free statement never catches anything.
            if (ctx.EcState.Turn.Enabled("EC-LOCALE-SIZE", null, line))
                enabled.Add(("EC-LOCALE-SIZE", null));
            // EC-DATA-CONVERSION (nonfatal, §15.19.4 r1/r3) rides any intrinsic-bearing statement too — FUNCTION
            // CONVERT sets it when an untranslatable character forces the substitution character; the ambient
            // gate records it while checking is enabled (harmless around a non-CONVERT intrinsic — no site sets it).
            if (ctx.EcState.Turn.Enabled("EC-DATA-CONVERSION", null, line) && ContainsIntrinsic(node))
                enabled.Add(("EC-DATA-CONVERSION", null));
            // EC-BOUND-OVERFLOW (nonfatal, §8.5.1.9.6 GR1) rides an ambient per-statement gate: a dynamic-capacity
            // table's implicit growth past its expected capacity records the last exception status while checking is
            // enabled. Wrapped conservatively (any statement in a checking-on region) — the raise site
            // (CobolDynTable.RefReceiving) fires ONLY on an actual dyn-table receiving grow-past-expected, so the
            // flag around a dyn-table-free statement is a harmless no-op (nonfatal, no site sets it). A precise
            // "references a dynamic table" filter is a documented future refinement.
            if (ctx.EcState.Turn.Enabled("EC-BOUND-OVERFLOW", null, line))
                enabled.Add(("EC-BOUND-OVERFLOW", null));
            // EC-BOUND-REF-MOD (fatal, §8.4.3.3.4) rides an ambient per-statement gate: a reference modification
            // whose leftmost/length is out of range (or an unallowed zero-length) raises it while checking is
            // enabled. Wrapped conservatively (any statement in a checking-on region) — the raise fires only at an
            // actual out-of-range ref-mod evaluation, so the guard around a ref-mod-free statement is harmless (the
            // catch never fires). A precise ContainsRefMod filter is a documented follow-on.
            if (ctx.EcState.Turn.Enabled("EC-BOUND-REF-MOD", null, line))
                enabled.Add(("EC-BOUND-REF-MOD", null));
            // EC-DATA-PTR-NULL / EC-BOUND-PTR (fatal, §13.18.5.4 GR3/GR4) and EC-SIZE-ADDRESS (fatal, §14.9.39
            // Format 10 GR19) ride ambient per-statement gates for the same reason EC-BOUND-REF-MOD does: a BASED
            // dereference renders INLINE through the generated bridge property that aliases CobolPtr.Deref, so it
            // is not one node kind that a precise QueryFor case could match. Wrapped conservatively — any
            // statement in a checking-on region — which is harmless, because each raise fires only at an actual
            // pointer operation and the guard around a pointer-free statement never catches anything.
            if (ctx.EcState.Turn.Enabled("EC-DATA-PTR-NULL", null, line))
                enabled.Add(("EC-DATA-PTR-NULL", null));
            if (ctx.EcState.Turn.Enabled("EC-BOUND-PTR", null, line))
                enabled.Add(("EC-BOUND-PTR", null));
            if (ctx.EcState.Turn.Enabled("EC-SIZE-ADDRESS", null, line))
                enabled.Add(("EC-SIZE-ADDRESS", null));
            // EC-BOUND-SUBSCRIPT (§8.4.2.3.4 GR2) and EC-BOUND-ODO (§13.18.38.4 GR7) are ambient for the same
            // reason: a subscripted reference renders inline through CobolTable.At and an ODO group extent
            // through CobolTable.OdoExtent, neither of which is a distinguishable node kind at the statement
            // level. The guard around a table-free statement never catches anything.
            if (ctx.EcState.Turn.Enabled("EC-BOUND-SUBSCRIPT", null, line))
                enabled.Add(("EC-BOUND-SUBSCRIPT", null));
            if (ctx.EcState.Turn.Enabled("EC-BOUND-ODO", null, line))
                enabled.Add(("EC-BOUND-ODO", null));
            // EC-BOUND-TABLE-LIMIT (§14.9.39.4 GR30) is ambient, unlike its CA37 twin: a dynamic table grows
            // both from an explicit capacity SET and from an IMPLICIT receiving reference, and the latter renders
            // inline through CobolDynTable.RefReceiving with no statement-level node of its own.
            if (ctx.EcState.Turn.Enabled("EC-BOUND-TABLE-LIMIT", null, line))
                enabled.Add(("EC-BOUND-TABLE-LIMIT", null));
            // EC-STORAGE-NOT-AVAIL (§14.9.2.4 ALLOCATE GR / CobolDynString's growth sites) and EC-RANGE-INVALID
            // (§14.9.13.4 — a THRU range with the ends reversed; CobolString.ThruMember) raise from RUNTIME
            // sites that render inline with no statement-level node, exactly like the gates above — ambient,
            // conservative, harmless around statements that never reach the raise. They ride the tail so the
            // 2-argument Set at those sites picks up the ambient (statement, location) pair (kb/Work R14).
            if (ctx.EcState.Turn.Enabled("EC-STORAGE-NOT-AVAIL", null, line))
                enabled.Add(("EC-STORAGE-NOT-AVAIL", null));
            if (ctx.EcState.Turn.Enabled("EC-RANGE-INVALID", null, line))
                enabled.Add(("EC-RANGE-INVALID", null));
            // EC-DATA-NOT-FINITE (fatal, §14.6.13.2 item 3) rides an ambient per-statement gate: any non-exempt read
            // of a NaN/±Inf standard-float sending operand raises it while checking is enabled. Wrapped conservatively
            // (any statement in a checking-on region) — the always-emitted CobolFloat.Sending wrap at the two float
            // read chokepoints raises only on an actual non-finite float read, so the guard around a float-free
            // statement is harmless. A precise "references a float sending operand" filter is a documented follow-on.
            if (ctx.EcState.Turn.Enabled("EC-DATA-NOT-FINITE", null, line))
                enabled.Add(("EC-DATA-NOT-FINITE", null));
            // EC-DATA-OVERFLOW (fatal, §14.9.25.4 GR6 d)4.a) is MOVE-only: a MOVE whose finite algebraic value
            // overflows a single-precision float receiver to ±Inf. A precise "has a single-float receiver" filter is
            // a documented follow-on (like the ContainsRefMod note); MOVE CORRESPONDING expands to BoundMove steps
            // which this sees through the BoundSequence recursion.
            if (node is BoundMove && ctx.EcState.Turn.Enabled("EC-DATA-OVERFLOW", null, line))
                enabled.Add(("EC-DATA-OVERFLOW", null));
            // EC-DATA-INCOMPATIBLE (fatal, §14.6.13.2) rides an AMBIENT per-statement gate, exactly like its own
            // sibling EC-DATA-NOT-FINITE above — the two are rules 3 and 2/4 of ONE clause about one subject, the
            // content of a sending operand that is not valid, and they get one shape.
            // ⛔ THIS USED TO READ `node is BoundMove &&`, AND THAT NODE-KIND TEST WAS THE WHOLE DEFECT
            // (kb/Work PB230). It scoped the family to rule 4 — "a numeric-edited data item is the sending operand
            // of a de-editing MOVE" — while rule 2 is not MOVE-specific at all: "When the content of a numeric
            // sending item that is not described with a standard floating-point usage is REFERENCED DURING THE
            // EXECUTION OF A STATEMENT and the content of that sending operand would evaluate to false in a
            // numeric class condition … an EC-DATA-INCOMPATIBLE exception condition is set to exist". So ADD,
            // SUBTRACT, MULTIPLY, DIVIDE, COMPUTE, every CORRESPONDING form (§14.7.6's last paragraph aggregates
            // the implied statements' condition), every comparison, DISPLAY, STRING, SORT — and a plain numeric
            // MOVE — were not even made CHECKABLE, so a program that explicitly asked to be checked was not.
            // A node-kind list is what produced that, so there is no node-kind list any more: the raise fires
            // only at an actual windowed sending read (CobolNum.ParseImageSending / SendingImage, and
            // CobolEdit.DeEdit for rule 4), so the guard around a statement with no such read is a no-op —
            // the same conservative-wrap argument every ambient family above rests on.
            if (ctx.EcState.Turn.Enabled("EC-DATA-INCOMPATIBLE", null, line))
                enabled.Add(("EC-DATA-INCOMPATIBLE", null));
        }
        QueryFor(bound);

        if (enabled.Count == 0) return bound;
        // A sequence's steps can re-contribute a family (two hoisted activations ⇒ ProgramNames twice) —
        // the checked wrapper carries each (name, connector) once.
        if (bound is BoundSequence) enabled = enabled.Distinct().ToList();
        ctx.EcState.Checked = true;
        if (enabled.Any(e => e.Ec.StartsWith("EC-I-O", StringComparison.Ordinal))) ctx.EcState.IoChecked = true;
        // §15.32.3 r3: the recorded name comes from Table 12's 'Statement name' column, resolved from the
        // statement KIND (the parse rule) — the first TOKEN gave GO where Table 12 requires GO TO, and no token
        // can repair it because TO is an optional word (`GO PARA.` never spells one). kb/Work R04.
        // WITH LOCATION is resolved PER (name, file) pair — §15.32.3 r1 keys the answer on the TURN option of
        // the condition that was RAISED, so one WITH LOCATION directive must not contaminate the statement's
        // other enabled conditions (kb/Work R06; the former statement-level Any() did exactly that).
        return new BoundEcChecked(bound, new EcStatementInfo(
            [.. enabled.Select(e => (e.Ec, e.File,
                ctx.EcState.Turn.WithLocation(e.Ec, e.File?.CobolName, line)))],
            Table12StatementNames.NameOf(s), EcLocation(line)));
    }

    /// <summary>The §15.30.3 r2 location string for a statement on <paramref name="line"/>:
    /// "element-name; paragraph[ OF section]|section; line-id" (the line-id is the final preprocessed-text line
    /// number — the implementor-defined identifier of the source line).</summary>
    /// <summary>The §15.30.3 r2b location string (kb/Work PB63): part 1 the element name — "as specified in the
    /// FUNCTION-ID, METHOD-ID, or PROGRAM-ID paragraph of the function, method, or program containing the
    /// statement", so a statement inside a METHOD names the method, not its class; part 2 the procedure field —
    /// (a) no paragraph-name and no section-name: empty ("; ; "), (b) a paragraph-name, plus " OF section" when
    /// the paragraph is within a section, (c) a section-name and no paragraph-name: the section-name alone
    /// (the paragraph-name-OMITTED paragraph carries the empty name, never a placeholder — ProcedureTableBuilder);
    /// part 3 the implementor-defined line identifier (docs/CONFORMANCE.md §4 determination; kb/Work PB82) — the
    /// line of the statement's first token IN THE FILE THAT PHYSICALLY HOLDS IT: a bare number for the main
    /// source, <c>copybook-name(line)</c> for a statement inside COPY-incorporated text — <paramref name="line"/>
    /// is the RESULTANT (token) line, mapped here through the preprocessing chain's origin table.</summary>
    private string EcLocation(int line)
    {
        var origin = ctx.Edition.OriginOf(line);
        string lineId = string.Equals(origin.File, ctx.Edition.SourceFile, StringComparison.Ordinal)
            ? origin.Line.ToString()
            : $"{Path.GetFileName(origin.File)}({origin.Line})";
        string para = ctx.BindCursor >= 0 && ctx.BindCursor < ctx.Table.Paragraphs.Count ? ctx.Table.Paragraphs[ctx.BindCursor].Cobol : "";
        string? sec = ctx.BindCursor >= 0 && ctx.BindCursor < ctx.Table.ParaSections.Count ? ctx.Table.ParaSections[ctx.BindCursor]?.Name : null;
        string proc = para.Length == 0
            ? sec ?? ""                                          // (c) the section alone, or (a) nothing at all
            : para + (sec is not null ? " OF " + sec : "");      // (b) paragraph [OF section]
        string element = ctx.CurrentMethodScope?.MethodName ?? ctx.EcState.ProgramName;
        return $"{element}; {proc}; {lineId}";
    }

    /// <summary>Does a bound statement (or a statement nested inside it) contain an intrinsic-function call — the
    /// EC-ARGUMENT-FUNCTION wrap test? Checks THIS statement's own operand/expression shapes via <see cref="DirectIntrinsic"/>,
    /// then recurses EVERY nested statement through the generated <see cref="BoundStatementTree.StatementChildren"/>
    /// (PHASE-07 Step 6h) — so the walk is now TOTAL over containers (the former hand-list missed SEARCH/keyed/WRITE/…
    /// phrase bodies). A wrap around a statement whose intrinsic argument is in fact valid is a no-op, so a wider walk
    /// is conservative — never mis-executes.</summary>
    private static bool ContainsIntrinsic(BoundStatement s) =>
        DirectIntrinsic(s) || s.StatementChildren().Any(ContainsIntrinsic);

    /// <summary>The intrinsic in THIS statement's OWN operands/expressions/conditions (not its nested statements —
    /// those are the recursion's job via <see cref="BoundStatementTree.StatementChildren"/>).
    ///
    /// <para>⛔ <b>THIS WAS A HAND-WRITTEN SWITCH OVER ~17 STATEMENT KINDS WITH <c>_ => false</c>, AND THE DEFAULT
    /// ARM WAS A SILENT WRONG ANSWER</b> (fix-queue PB26). <b>ISO §15.3 item 14 attaches EC-ARGUMENT-FUNCTION to
    /// the FUNCTION REFERENCE</b> — "If the evaluation of an argument results in an incorrect value … the
    /// EC-ARGUMENT-FUNCTION exception condition is set to exist" — with no statement-kind qualification anywhere in
    /// it. So the ambient checking gate must be emitted wherever a function reference is, and the switch made it
    /// depend on whether someone had remembered to add an arm: <c>FUNCTION LOG10(0)</c> raised in COMPUTE, MOVE,
    /// DISPLAY and IF, and was SILENT in STRING and every other unlisted kind. Measured, not reasoned.</para>
    ///
    /// <para>The list is now a STRUCTURE (CLAUDE.md rule 5): <see cref="BoundStatementTree.OwnValueParts"/> is
    /// generated from the semantic model by reading every property of every statement leaf, so a statement kind
    /// added tomorrow is covered without an edit here — and <c>EcArgumentFunctionGateDriftTests</c> fails the build
    /// if that ever stops being true.</para></summary>
    private static bool DirectIntrinsic(BoundStatement s) => s.OwnValueParts().Any(PartHasIntrinsic);

    /// <summary>One generated value part → does it carry an intrinsic call? The four value hierarchies each have
    /// their own walker below; a part of any other shape (a <c>Place</c>, a receiver) carries no expression and
    /// answers false.</summary>
    private static bool PartHasIntrinsic(object part) => part switch
    {
        BoundExpr e => ExprHasIntrinsic(e),
        BoundCondition c => CondHasIntrinsic(c),
        BoundOperand o => OpHasIntrinsic(o),
        BoundBoolExpr b => BoolExprHasIntrinsic(b),
        _ => false,
    };

    private static bool OpHasIntrinsic(BoundOperand op) => op switch
    {
        BoundComputedOperand c => ExprHasIntrinsic(c.Expr),
        BoundBoolOperand b => BoolExprHasIntrinsic(b.Expr),
        _ => false,
    };

    /// <summary>The intrinsic walk over the boolean channel (ISO §8.8.2). Boolean-op operands are boolean items/
    /// literals today — intrinsic operands inside a boolean expression are named residue — but the walk is TOTAL
    /// from day one (the DEVLOG-607 rule: a new node must register in every exhaustive walk).</summary>
    private static bool BoolExprHasIntrinsic(BoundBoolExpr e) => e switch
    {
        BoundBoolBinary b => BoolExprHasIntrinsic(b.Left) || BoolExprHasIntrinsic(b.Right),
        BoundBoolNot n => BoolExprHasIntrinsic(n.Operand),
        BoundBoolShift s => BoolExprHasIntrinsic(s.Operand) || ExprHasIntrinsic(s.Count),   // the count is a numeric expr
        BoundBoolCall => true,   // a boolean-result function reference (kb/Work PB68)
        _ => false,
    };

    private static bool ExprHasIntrinsic(BoundExpr e) => e switch
    {
        BoundIntrinsicCall => true,
        BoundBinary b => ExprHasIntrinsic(b.Left) || ExprHasIntrinsic(b.Right),
        BoundNegate n => ExprHasIntrinsic(n.Operand),
        BoundPower p => ExprHasIntrinsic(p.Base) || ExprHasIntrinsic(p.Exp),
        _ => false,
    };

    private static bool CondHasIntrinsic(BoundCondition c) => c switch
    {
        BoundRelational r => OpHasIntrinsic(r.Left) || OpHasIntrinsic(r.Right),
        BoundLogical l => l.Operands.Any(CondHasIntrinsic),
        BoundNot n => CondHasIntrinsic(n.Operand),
        BoundSignCondition s => ExprHasIntrinsic(s.Expr),
        BoundBooleanCondition bc => BoolExprHasIntrinsic(bc.Expr),
        _ => false,
    };
}
