// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;
using CobolNet.Frontend.Generated;

using CobolNet.Binding.Model;

namespace CobolNet.Binding.Bound;

using Core = CobolParserCore;

/// <summary>
/// The EC exception-condition half of the binder (ISO/IEC 1989:2023 §14.6.13; COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN
/// D9–D12): binds RAISE (§14.9.29), RESUME (§14.9.33), SET LAST EXCEPTION TO OFF (§14.9.39 F13), the GOBACK/EXIT
/// RAISING phrase (§14.9.18/§14.9.14), and — because bound nodes carry no parse context — performs the compile-time
/// TurnState fold HERE (the statement's source line is a parse-tree property): a statement whose kind has any
/// enabled relevant exception-name is wrapped in <see cref="BoundEcChecked"/> carrying the bind-time decision; a
/// statement with none binds UNwrapped, so checking-off emits nothing new (deep-dive D10 / SSOT §18.16).
/// </summary>
public sealed partial class StatementBinder
{
    private TurnState _turn = TurnState.Empty;
    private string _programName = "";
    private readonly HashSet<string> _pdRaising = new(StringComparer.OrdinalIgnoreCase);   // PD-header RAISING names (§14.2.1)
    private readonly HashSet<string> _pdRaisingClasses = new(StringComparer.OrdinalIgnoreCase);   // header RAISING class names (§14.2.2 SR8; the SR4a check)
    private readonly HashSet<string> _declEcPairs = new(StringComparer.OrdinalIgnoreCase); // USE F3 SR14 cross-USE (ec,file) pairs
    private int _currentBindPc = -1;   // the pc whose sentences are being bound (RESUME SR1/SR2 context)

    // EcFeatures accumulators (the emitter's gating summary — BoundProgram.Ec).
    private bool _ecChecked, _ecIoChecked, _ecRaise, _ecResume, _ecF3, _ecFunctions, _ecRaising;

    /// <summary>Configure the EC bind context (called per bound unit — <see cref="BinderDriver"/> for program
    /// units, the OO bind half for class rosters):
    /// the compilation group's TurnState and this unit's PROGRAM-ID name (the §15.30.3 r2 location element).</summary>
    public void ConfigureEc(TurnState turn, string programName)
    {
        _turn = turn;
        _programName = programName;
    }

    private EcFeatures BuildEcFeatures() =>
        new(_ecChecked, _ecIoChecked, _ecRaise, _ecResume, _ecF3, _ecFunctions, _ecRaising);

    /// <summary>Record a FUNCTION EXCEPTION-* binding (sets the group EC gate so the generated source carries
    /// the runtime using; called from the intrinsic bind when the catalog row is an Ec* runtime method).</summary>
    private void EcNoteFunction() => _ecFunctions = true;

    // ── RAISE (§14.9.29) ─────────────────────────────────────────────────────────────────────────────────────

    private BoundStatement BindRaise(Core.RaiseStatementContext r)
    {
        if (data.Edition.DialectLevel < 2002)
            data.Edition.Error("COBOLNET0876",
                "RAISE is the COBOL-2002+ exception-condition statement (ISO §14.9.29) — it requires --std 2002 "
                + $"or later (targeting COBOL-{data.Edition.DialectLevel})");
        if (r.cobolWord() is not { } ecWord)
        {
            // RAISE identifier-1 — an exception OBJECT (§14.9.29.3 SR2/SR3; §14.6.13.1.5). NOT TURN-gated
            // (§7.3.25 takes exception-NAMES only) and never fatal by itself (GR2).
            var oref = r.objectReference();
            if (oref.NULL_() is not null || oref.SUPER() is not null)
            {
                data.Edition.Error("COBOLNET0848",
                    $"RAISE {(oref.NULL_() is not null ? "NULL" : "SUPER")}: NULL and SUPER shall not be "
                    + "specified as the raised object (ISO §14.9.29.3 SR2)");
                return new BoundNop();
            }
            _ecRaise = true;   // the machinery gate — the object channel is live once used
            if (oref.SELF() is not null)
            {
                if (!InMethod)
                {
                    data.Edition.Error("COBOLNET0848",
                        "RAISE SELF may be specified only within a method definition (ISO §8.4.3.8)");
                    return new BoundNop();
                }
                return new BoundRaiseObject(null);
            }
            if (refs.Resolve(oref.dataReference()!) is not { } op
                || op.Item.Pic?.Category is not PicCategory.ObjectReference)
            {
                data.Edition.Error("COBOLNET0848",
                    $"RAISE '{oref.GetText()}': identifier-1 shall be a USAGE OBJECT REFERENCE data item "
                    + "(ISO §14.9.29.3 SR2)");
                return new BoundNop();
            }
            return new BoundRaiseObject(op);
        }

        if (EcResolveLevel3(ecWord.GetText(), "RAISE") is not { } info)
            return new BoundNop();   // diagnosed — fail the compile, bind a placeholder
        _ecRaise = true;
        int line = r.Start.Line;
        bool enabled = _turn.Enabled(info.Name, null, line);
        bool withLoc = enabled && _turn.WithLocation(info.Name, null, line);
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
            data.Edition.Error("COBOLNET0711", $"{context}: '{name}' is not an exception-name of ISO/IEC 1989 "
                + "§14.6.13.1 (and not a valid EC-USER-/EC-IMP- name)");
            return null;
        }
        if (info.Level != 3)
        {
            data.Edition.Error("COBOLNET0710", $"{context}: exception-name '{info.Name}' is a level-{info.Level} "
                + "name; only a LEVEL-3 exception-name may be raised (ISO §14.9.29.3 SR1)");
            return null;
        }
        if (info.IntroducedIn > data.Edition.DialectLevel)
        {
            data.Edition.Error("COBOLNET0878", $"exception-name {info.Name} was introduced by ISO/IEC "
                + $"1989:{info.IntroducedIn} — it requires --std {info.IntroducedIn} or later "
                + $"(targeting COBOL-{data.Edition.DialectLevel})");
            return null;
        }
        return info;
    }

    // ── RESUME (§14.9.33) ────────────────────────────────────────────────────────────────────────────────────

    private BoundStatement BindResume(Core.ResumeStatementContext r)
    {
        if (data.Edition.DialectLevel < 2002)
            data.Edition.Error("COBOLNET0876",
                "RESUME is the COBOL-2002+ exception-recovery statement (ISO §14.9.33) — it requires --std 2002 "
                + $"or later (targeting COBOL-{data.Edition.DialectLevel})");
        // SR1 — only in a declarative (the exception-checking PERFORM WHEN form is 2023, a later wave). The
        // declarative sections occupy the pcs below EntryPc (StatementBinder.Declaratives.cs).
        var decl = _declaratives.FirstOrDefault(d => _currentBindPc >= d.StartPc && _currentBindPc <= d.EndPc);
        if (_currentBindPc >= _entryPc || decl is null)
        {
            data.Edition.Error("COBOLNET0712", "RESUME may be specified only in a declarative (ISO §14.9.33.3 SR1)");
            return new BoundNop();
        }
        // SR2 — not in a GLOBAL-phrase declarative (a RESUME executed within a global declarative's DYNAMIC
        // scope is CONTINUE, GR1 — realized by __RunGlobalUse swallowing the signal; the STATIC case rejects).
        if (decl.Global)
        {
            data.Edition.Error("COBOLNET0713", "RESUME shall not be specified in a declarative procedure whose "
                + "USE statement carries the GLOBAL phrase (ISO §14.9.33.3 SR2)");
            return new BoundNop();
        }
        _ecResume = true;
        if (r.NEXT() is not null) return new BoundResume(ResumeSignal.NextStatement);

        // SR3 — procedure-name-1 shall be in the NONdeclarative portion.
        var pn = r.procedureName()!;
        if (ResolveProcedure(pn) is not { } target)
            return new BoundUnsupported($"RESUME AT unknown procedure '{pn.GetText()}'");
        if (target.Start < _entryPc)
        {
            data.Edition.Error("COBOLNET0714", $"RESUME AT '{pn.GetText()}': the procedure shall be in the "
                + "nondeclarative portion of the program (ISO §14.9.33.3 SR3)");
            return new BoundNop();
        }
        return new BoundResume(target.Start);   // GR3 — as if GO TO procedure-name-1
    }

    // ── SET LAST EXCEPTION TO OFF (§14.9.39 Format 13) ───────────────────────────────────────────────────────

    private BoundStatement BindSetLastException()
    {
        if (data.Edition.DialectLevel < 2002)
            data.Edition.Error("COBOLNET0879",
                "SET LAST EXCEPTION TO OFF is the COBOL-2002+ saved-exception form (ISO §14.9.39 Format 13) — it "
                + $"requires --std 2002 or later (targeting COBOL-{data.Edition.DialectLevel})");
        _ecFunctions = true;   // touches the runtime last-exception register — the group EC gate
        return new BoundSetLastException();
    }

    // ── The GOBACK / EXIT PROGRAM RAISING phrase (§14.9.18 / §14.9.14 F2) ────────────────────────────────────

    /// <summary>Bind a RAISING phrase. Returns null for the identifier (exception-object) form — the caller
    /// degrades to a loud placeholder until the OO wave.</summary>
    private BoundRaising? EcBindRaising(Core.RaisingPhraseContext raising, int line, string verb)
    {
        if (data.Edition.DialectLevel < 2002)
            data.Edition.Error("COBOLNET0879",
                $"{verb} … RAISING is the COBOL-2002+ exception-propagation phrase (ISO §14.9.18 / §14.9.14) — it "
                + $"requires --std 2002 or later (targeting COBOL-{data.Edition.DialectLevel})");
        _ecRaising = true;
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
            if (refs.Resolve(dref) is not { } op
                || op.Item.Pic is not { Category: PicCategory.ObjectReference } opic)
            {
                data.Edition.Error("COBOLNET0849",
                    $"{verb} RAISING '{dref.GetText()}': identifier-1 shall be a USAGE OBJECT REFERENCE "
                    + "data item (ISO §14.9.18.3 SR4)");
                return null;
            }
            if (opic.ObjectClassName is not { } declared)
            {
                data.Edition.Error("COBOLNET0849",
                    $"{verb} RAISING '{op.Item.CobolName}': identifier-1 shall not be a UNIVERSAL object "
                    + "reference (ISO §14.9.18.3 SR4d)");
                return null;
            }
            bool listed = false;
            for (var c = OoClasses?.Find(declared); c is not null; c = c.Base)
                if (_pdRaisingClasses.Contains(c.Name)) { listed = true; break; }
            if (!listed)
                data.Edition.Error("COBOLNET0849",
                    $"{verb} RAISING '{op.Item.CobolName}': its declared class '{declared}' (or a "
                    + "superclass) shall be specified in the RAISING phrase of the procedure division "
                    + "header of the containing source element (ISO §14.9.18.3 SR4a)");
            return new BoundRaising(null, IsLast: false, Fatal: false, Enabled: true, ObjectSource: op);
        }

        if (EcResolveLevel3(ecWord.GetText(), $"{verb} RAISING") is not { } info)
            return new BoundRaising("EC-RAISING-IMP", false, false, false);   // diagnosed; placeholder
        // SR2 (§14.9.18.3 / 27684): an EC-USER name shall appear in the PD-header RAISING phrase — the
        // statically detectable half binds as an error; the runtime condition is EC-RAISING-NOT-SPECIFIED.
        if (info.Level2Parent is "EC-USER" && !_pdRaising.Contains(info.Name))
            data.Edition.Error("COBOLNET0717", $"{verb} RAISING {info.Name}: an EC-USER exception-name shall be "
                + "specified in the RAISING phrase of the procedure division header (ISO §14.9.18.3 SR2 — "
                + "otherwise EC-RAISING-NOT-SPECIFIED, Table 13)");
        return new BoundRaising(info.Name, IsLast: false,
            Fatal: info.Fatality is not EcFatality.Nonfatal, Enabled: _turn.Enabled(info.Name, null, line));
    }

    /// <summary>Capture the PROCEDURE DIVISION header RAISING list (§14.2.1; consumed by the SR2 check above;
    /// classes/interfaces in the list resolve at the OO wave — names are recorded uninterpreted).</summary>
    private void EcCollectPdRaising(Core.ProcedureDivisionContext pd)
    {
        if (pd.raisingClause() is not { } rc) return;
        foreach (var w in rc.cobolWord()) EcAddPdRaisingWord(w.GetText());
    }

    /// <summary>Load a METHOD's pre-partitioned header RAISING lists as the current source element's
    /// sets (per-method reset — methods of one class bind through ONE binder).</summary>
    internal void EcLoadPdRaising(IReadOnlyList<string> ecNames, IReadOnlyList<string> classes)
    {
        _pdRaising.Clear();
        _pdRaisingClasses.Clear();
        foreach (var n in ecNames) _pdRaising.Add(n);
        foreach (var c in classes) _pdRaisingClasses.Add(c);
    }

    /// <summary>Partition ONE PD-header RAISING operand (§14.2.2 — the EC-OO wave, D-EO8): a catalog EC
    /// name must be level-3 EC-USER (SR7 → 0858 otherwise); a class of the group joins the SR4a class list;
    /// anything else is 0858 (SR8/SR9 — interface names are the interface-RAISING refinement).</summary>
    internal void EcAddPdRaisingWord(string word)
    {
        string up = word.ToUpperInvariant();
        if (CobolNet.Runtime.Exceptions.ExceptionCatalog.TryGet(up, out var info))
        {
            if (info.Level is 3 && info.Level2Parent is "EC-USER") _pdRaising.Add(up);
            else
                data.Edition.Error("COBOLNET0858",
                    $"PROCEDURE DIVISION RAISING {up}: an exception-name here shall be a level-3 EC-USER "
                    + "name (ISO §14.2.2 SR7)");
            return;
        }
        if (OoClasses?.Find(up) is not null) { _pdRaisingClasses.Add(up); return; }
        data.Edition.Error("COBOLNET0858",
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
    private BoundStatement EcWrap(Core.StatementContext s, BoundStatement bound)
    {
        if (!_turn.AnyEnabled) return bound;
        int line = s.Start.Line;
        var enabled = new List<(string Ec, FileModel? File)>();
        void Query(IEnumerable<string> names, FileModel? file = null)
        {
            foreach (string n in names)
                if (_turn.Enabled(n, file?.CobolName, line))
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
            if (_turn.Enabled("EC-ARGUMENT-FUNCTION", null, line) && ContainsIntrinsic(node))
                enabled.Add(("EC-ARGUMENT-FUNCTION", null));
            // EC-DATA-CONVERSION (nonfatal, §15.19.4 r1/r3) rides any intrinsic-bearing statement too — FUNCTION
            // CONVERT sets it when an untranslatable character forces the substitution character; the ambient
            // gate records it while checking is enabled (harmless around a non-CONVERT intrinsic — no site sets it).
            if (_turn.Enabled("EC-DATA-CONVERSION", null, line) && ContainsIntrinsic(node))
                enabled.Add(("EC-DATA-CONVERSION", null));
        }
        QueryFor(bound);

        if (enabled.Count == 0) return bound;
        // A sequence's steps can re-contribute a family (two hoisted activations ⇒ ProgramNames twice) —
        // the checked wrapper carries each (name, connector) once.
        if (bound is BoundSequence) enabled = enabled.Distinct().ToList();
        _ecChecked = true;
        if (enabled.Any(e => e.Ec.StartsWith("EC-I-O", StringComparison.Ordinal))) _ecIoChecked = true;
        bool withLoc = enabled.Any(e => _turn.WithLocation(e.Ec, e.File?.CobolName, line));
        return new BoundEcChecked(bound, new EcStatementInfo(
            enabled, withLoc, s.Start.Text.ToUpperInvariant(), EcLocation(line)));
    }

    /// <summary>The §15.30.3 r2 location string for a statement on <paramref name="line"/>:
    /// "element-name; paragraph[ OF section]|section; line-id" (the line-id is the final preprocessed-text line
    /// number — the implementor-defined identifier of the source line).</summary>
    private string EcLocation(int line)
    {
        string para = _currentBindPc >= 0 && _currentBindPc < _paras.Count ? _paras[_currentBindPc].Cobol : "";
        string? sec = _currentBindPc >= 0 && _currentBindPc < _paraSection.Count ? _paraSection[_currentBindPc]?.Name : null;
        string proc = sec is not null && para.Equals(sec, StringComparison.OrdinalIgnoreCase)
            ? sec
            : para + (sec is not null ? " OF " + sec : "");
        return $"{_programName}; {proc}; {line}";
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
