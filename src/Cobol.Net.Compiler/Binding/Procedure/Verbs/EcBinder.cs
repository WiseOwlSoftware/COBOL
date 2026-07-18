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
internal sealed class EcBinder(BinderContext ctx, StatementBinder host)
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

    /// <summary>Resolve and validate a written exception-name: must exist in the §14.6.13.1 catalog (or be a
    /// valid EC-USER-/EC-IMP- name), be LEVEL-3 (§14.9.29.3 SR1 / §14.9.18.3 SR2 — the RAISE/RAISING contexts
    /// take level-3 names only), and fall inside the targeted edition's window (the 2023-only families —
    /// VERSION_CHANGE_REFERENCE rows 40/61). Null after diagnosing.</summary>
    private EcInfo? EcResolveLevel3(string name, string context)
    {
        if (!ExceptionCatalog.TryGet(name, out var info))
        {
            ctx.Edition.Error("COBOLNET0711", $"{context}: '{name}' is not an exception-name of ISO/IEC 1989 "
                + "§14.6.13.1 (and not a valid EC-USER-/EC-IMP- name)");
            return null;
        }
        if (info.Level != 3)
        {
            ctx.Edition.Error("COBOLNET0710", $"{context}: exception-name '{info.Name}' is a level-{info.Level} "
                + "name; only a LEVEL-3 exception-name may be raised (ISO §14.9.29.3 SR1)");
            return null;
        }
        if (info.IntroducedIn > ctx.Edition.DialectLevel)
        {
            ctx.Edition.Error("COBOLNET0878", $"exception-name {info.Name} was introduced by ISO/IEC "
                + $"1989:{info.IntroducedIn} — it requires --std {info.IntroducedIn} or later "
                + $"(targeting COBOL-{ctx.Edition.DialectLevel})");
            return null;
        }
        return info;
    }

    // ── RESUME (§14.9.33) ────────────────────────────────────────────────────────────────────────────────────

    public BoundStatement BindResume(Core.ResumeStatementContext r)
    {
        // resume-statement-2002: the pass owns the edition gate (Exec Step E).
        // SR1 — only in a declarative (the exception-checking PERFORM WHEN form is 2023, a later wave). The
        // declarative sections occupy the pcs below EntryPc (StatementBinder.Declaratives.cs).
        var decl = ctx.Table.Declaratives.FirstOrDefault(d => ctx.BindCursor >= d.StartPc && ctx.BindCursor <= d.EndPc);
        if (ctx.BindCursor >= ctx.Table.EntryPc || decl is null)
        {
            ctx.Edition.Error("COBOLNET0712", "RESUME may be specified only in a declarative (ISO §14.9.33.3 SR1)");
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
        return new BoundRaising(info.Name, IsLast: false,
            Fatal: info.Fatality is not EcFatality.Nonfatal, Enabled: ctx.EcState.Turn.Enabled(info.Name, null, line));
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
            if (info.Level is 3 && info.Level2Parent is "EC-USER") ctx.EcState.PdRaising.Add(up);
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
        ["EC-SIZE-TRUNCATION", "EC-SIZE-OVERFLOW", "EC-SIZE-ZERO-DIVIDE", "EC-SIZE-EXPONENTIATION"];

    /// <summary>The EC-I-O family raised from I-O status values (§9.1.13.1 correspondence) — THE canonical
    /// mask order (<see cref="ExceptionCatalog.IoMaskNames"/>; the emitter's per-statement mask bits).</summary>
    private static readonly string[] IoNames = ExceptionCatalog.IoMaskNames;

    /// <summary>The EC-PROGRAM family a CALL/CANCEL raises through <c>CobolCallException</c>.</summary>
    private static readonly string[] ProgramNames =
    [
        "EC-PROGRAM-NOT-FOUND", "EC-PROGRAM-RECURSIVE-CALL", "EC-PROGRAM-CANCEL-ACTIVE", "EC-PROGRAM-ARG-OMITTED",
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
                case BoundAddTo or BoundAddGiving or BoundSubtractFrom or BoundSubtractGiving or BoundMultiplyBy
                    or BoundMultiplyGiving or BoundDivideInto or BoundDivideGiving or BoundDivideRemainder
                    or BoundCompute or BoundCorresponding:
                    Query(SizeNames);
                    break;
                case BoundStringStmt:
                    Query(["EC-OVERFLOW-STRING"]);
                    break;
                case BoundUnstringStmt:
                    Query(["EC-OVERFLOW-UNSTRING"]);
                    break;
                case BoundOpen o:
                    foreach (var (file, _, _) in o.Files) Query(IoNames, file);
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
                case BoundKeyedStart k: Query(IoNames, k.File); break;
                case BoundCallProgram or BoundCancel:
                    Query(ProgramNames);
                    break;
                case BoundFree:
                    Query(["EC-STORAGE-NOT-ALLOC"]);   // §14.9.15 GR1c (nonfatal; Phase-4b inc 2)
                    break;
            }
            // EC-ARGUMENT-FUNCTION rides any intrinsic-bearing statement (the ambient statement gate — the
            // intrinsic renders inline inside expressions, so the guard wraps the STATEMENT).
            if (ctx.EcState.Turn.Enabled("EC-ARGUMENT-FUNCTION", null, line) && ContainsIntrinsic(node))
                enabled.Add(("EC-ARGUMENT-FUNCTION", null));
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
        }
        QueryFor(bound);

        if (enabled.Count == 0) return bound;
        // A sequence's steps can re-contribute a family (two hoisted activations ⇒ ProgramNames twice) —
        // the checked wrapper carries each (name, connector) once.
        if (bound is BoundSequence) enabled = enabled.Distinct().ToList();
        ctx.EcState.Checked = true;
        if (enabled.Any(e => e.Ec.StartsWith("EC-I-O", StringComparison.Ordinal))) ctx.EcState.IoChecked = true;
        bool withLoc = enabled.Any(e => ctx.EcState.Turn.WithLocation(e.Ec, e.File?.CobolName, line));
        return new BoundEcChecked(bound, new EcStatementInfo(
            enabled, withLoc, s.Start.Text.ToUpperInvariant(), EcLocation(line)));
    }

    /// <summary>The §15.30.3 r2 location string for a statement on <paramref name="line"/>:
    /// "element-name; paragraph[ OF section]|section; line-id" (the line-id is the final preprocessed-text line
    /// number — the implementor-defined identifier of the source line).</summary>
    private string EcLocation(int line)
    {
        string para = ctx.BindCursor >= 0 && ctx.BindCursor < ctx.Table.Paragraphs.Count ? ctx.Table.Paragraphs[ctx.BindCursor].Cobol : "";
        string? sec = ctx.BindCursor >= 0 && ctx.BindCursor < ctx.Table.ParaSections.Count ? ctx.Table.ParaSections[ctx.BindCursor]?.Name : null;
        string proc = sec is not null && para.Equals(sec, StringComparison.OrdinalIgnoreCase)
            ? sec
            : para + (sec is not null ? " OF " + sec : "");
        return $"{ctx.EcState.ProgramName}; {proc}; {line}";
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
    /// those are the recursion's job via <see cref="BoundStatementTree.StatementChildren"/>).</summary>
    private static bool DirectIntrinsic(BoundStatement s) => s switch
    {
        BoundDisplay d => d.Operands.Any(OpHasIntrinsic),
        BoundMove m => OpHasIntrinsic(m.Source),
        BoundCompute c => ExprHasIntrinsic(c.Rhs),
        BoundComputeBoolean cb => BoolExprHasIntrinsic(cb.Rhs),
        BoundAddTo a => a.Addends.Any(ExprHasIntrinsic),
        BoundAddGiving a => a.Addends.Any(ExprHasIntrinsic),
        BoundSubtractFrom a => a.Minuends.Any(ExprHasIntrinsic),
        BoundSubtractGiving a => a.Minuends.Any(ExprHasIntrinsic) || ExprHasIntrinsic(a.From),
        BoundMultiplyBy a => ExprHasIntrinsic(a.A),
        BoundMultiplyGiving a => ExprHasIntrinsic(a.A) || ExprHasIntrinsic(a.B),
        BoundDivideInto a => ExprHasIntrinsic(a.Divisor),
        BoundDivideGiving a => ExprHasIntrinsic(a.Dividend) || ExprHasIntrinsic(a.Divisor),
        BoundDivideRemainder a => ExprHasIntrinsic(a.Dividend) || ExprHasIntrinsic(a.Divisor),
        BoundIf i => CondHasIntrinsic(i.Condition),   // Then/Else recursed by StatementChildren
        BoundSetTo st => ExprHasIntrinsic(st.Value),
        BoundEvaluate ev => ev.Whens.Any(wn => CondHasIntrinsic(wn.Match)),   // when/Other statements recursed
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
