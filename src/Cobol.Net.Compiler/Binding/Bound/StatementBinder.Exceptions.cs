// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;
using CobolSharp.Compiler.Generated;

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
    private readonly HashSet<string> _declEcPairs = new(StringComparer.OrdinalIgnoreCase); // USE F3 SR14 cross-USE (ec,file) pairs
    private int _currentBindPc = -1;   // the pc whose sentences are being bound (RESUME SR1/SR2 context)

    // EcFeatures accumulators (the emitter's gating summary — BoundProgram.Ec).
    private bool _ecChecked, _ecIoChecked, _ecRaise, _ecResume, _ecF3, _ecFunctions, _ecRaising;

    /// <summary>Configure the EC bind context (called by the emitter's per-unit bind, CSharpEmitter.Call.cs):
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
            // RAISE identifier-1 — an exception OBJECT (SR2/SR3, §14.6.13.1.5) — parses; awaits the OO wave.
            return new BoundUnsupported("RAISE identifier (exception object — the OO wave; ISO §14.9.29.2)");

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
        if (raising.cobolWord() is not { } ecWord) return null;   // identifier form — OO wave

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
        foreach (var w in rc.cobolWord()) _pdRaising.Add(w.GetText().ToUpperInvariant());
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

        switch (bound)
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
        }
        // EC-ARGUMENT-FUNCTION rides any intrinsic-bearing statement (the ambient statement gate — the
        // intrinsic renders inline inside expressions, so the guard wraps the STATEMENT).
        if (_turn.Enabled("EC-ARGUMENT-FUNCTION", null, line) && ContainsIntrinsic(bound))
            enabled.Add(("EC-ARGUMENT-FUNCTION", null));

        if (enabled.Count == 0) return bound;
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

    /// <summary>Does a bound statement contain an intrinsic-function call (the EC-ARGUMENT-FUNCTION wrap test)?
    /// Walks the operand/expression shapes of the value-bearing statements; containers recurse. A miss on an
    /// exotic shape under-checks (no guard) — never mis-executes.</summary>
    private static bool ContainsIntrinsic(BoundStatement s) => s switch
    {
        BoundDisplay d => d.Operands.Any(OpHasIntrinsic),
        BoundMove m => OpHasIntrinsic(m.Source),
        BoundCompute c => ExprHasIntrinsic(c.Rhs),
        BoundAddTo a => a.Addends.Any(ExprHasIntrinsic),
        BoundAddGiving a => a.Addends.Any(ExprHasIntrinsic),
        BoundSubtractFrom a => a.Minuends.Any(ExprHasIntrinsic),
        BoundSubtractGiving a => a.Minuends.Any(ExprHasIntrinsic) || ExprHasIntrinsic(a.From),
        BoundMultiplyBy a => ExprHasIntrinsic(a.A),
        BoundMultiplyGiving a => ExprHasIntrinsic(a.A) || ExprHasIntrinsic(a.B),
        BoundDivideInto a => ExprHasIntrinsic(a.Divisor),
        BoundDivideGiving a => ExprHasIntrinsic(a.Dividend) || ExprHasIntrinsic(a.Divisor),
        BoundDivideRemainder a => ExprHasIntrinsic(a.Dividend) || ExprHasIntrinsic(a.Divisor),
        BoundIf i => CondHasIntrinsic(i.Condition) || i.Then.Any(ContainsIntrinsic) || i.Else.Any(ContainsIntrinsic),
        BoundInlinePerform p => p.Body.Any(ContainsIntrinsic),
        BoundSetTo st => ExprHasIntrinsic(st.Value),
        BoundEvaluate ev => ev.Whens.Any(wn => CondHasIntrinsic(wn.Match) || wn.Statements.Any(ContainsIntrinsic))
                            || (ev.Other?.Any(ContainsIntrinsic) ?? false),
        _ => false,
    };

    private static bool OpHasIntrinsic(BoundOperand op) => op switch
    {
        BoundComputedOperand c => ExprHasIntrinsic(c.Expr),
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
        _ => false,
    };
}
