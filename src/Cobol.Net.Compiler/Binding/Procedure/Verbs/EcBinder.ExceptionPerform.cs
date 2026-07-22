// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// The exception-checking (Format-3) PERFORM binder (ISO §14.9.28 Format 3, COBOL-2023). Produces a
/// <see cref="BoundExceptionPerform"/> and emits the Format-3 syntax-rule + cross-statement-ban diagnostics
/// (COBOLNET1597-1617; see <c>docs/rearchitecture/evidence/PHASE-13-c5-perform-format3-DESIGN.md</c>). The version
/// gate (COBOLNET0900, below 2023) is the VersionConformancePass's <c>VisitPerformStatement</c> arm — not here.
/// </summary>
internal sealed partial class EcBinder
{
    /// <summary>Bind an inline PERFORM that carries a WHEN / FINALLY phrase or a LOCATION head (§14.9.28 Format 3).
    /// imp-1 binds under the GR14 implicit-TURN overlay (the WHEN-named ECs enabled over its extent); imp-2/3/4
    /// bind with <see cref="EcBindState.InF3When"/> set (RESUME NEXT STATEMENT is legal there, §14.9.33.3 SR1);
    /// imp-5 (FINALLY) binds without it (RESUME is not a WHEN phrase). imp-2..5 bind against the BASE TurnState
    /// (GR21).</summary>
    public BoundStatement EcBindExceptionPerform(Core.PerformStatementContext p)
    {
        ctx.EcState.F3Perform = true;   // this unit installs the F3-frame interceptor (emitter gate — §14.9.28)

        var whenPhrases = p.performWhenPhrase();
        bool withLocation = p.performInlineHead()?.performLocationPhrase()?.LOCATION() is not null;

        // PF3-STRUCT-WHEN-REQUIRED (COBOLNET1597): ≥1 ordinary WHEN — the outer brace is a required group in the
        // §14.9.28.2 Format-3 general format (PDF-resolved). WHEN OTHER / WHEN COMMON / FINALLY alone is not admitted.
        if (whenPhrases.Length == 0)
            ctx.Edition.Error("COBOLNET1597", "an exception-checking (Format-3) PERFORM requires at least one WHEN "
                + "phrase (ISO §14.9.28.2 Format 3)");

        // Resolve each WHEN's operands (mode / exception-name / file) → the descriptor header, the GR14 overlay
        // enables, and the SR14/SR15 operand census.
        var headers = new List<(string? Mode, List<BoundWhenOperand> Ops)>();
        var overlay = new List<(string Ec, string? File)>();
        var census = new List<(string? Ec, string? File)>();
        foreach (var w in whenPhrases)
            headers.Add(ResolveWhenOperands(w, overlay, census));

        CheckSr14Sr15(census);

        // GR14 overlay: imp-1 binds with the WHEN-named ECs implicitly enabled over its extent (WITH LOCATION iff
        // the PERFORM specifies LOCATION). The overlay is popped (base restored) after imp-1 — imp-2..5 bind
        // against the base state (GR21/GR22).
        var savedTurn = ctx.EcState.Turn;
        // imp-1's line ≈ the PERFORM statement's line (imp-1's statements are at ≥ this line, pre-PERFORM
        // directives at < it) — the GR14 synthetic is placed here so a pre-PERFORM >>TURN OFF loses to it.
        ctx.EcState.Turn = savedTurn.WithImplicitEnable(overlay, withLocation, p.Start.Line);
        var imp1 = host.BindBlocks(p.statementBlock());
        ctx.EcState.Turn = savedTurn;

        CheckCrossStatementBans(p);   // parse-subtree walks — order-independent; run once for both paths below

        // OO-method F3 PERFORM is a STAGED GAP (COBOLNET0899): the appended handler pc-ranges fall outside the
        // method's contiguous dispatch slice, so a WHEN body would silently never run. Reject loud (§9.7).
        if (ctx.CurrentMethodScope is not null)
            return F3StagedInMethodStub(p, imp1, withLocation);

        // imp-2/3/4 (WHEN / OTHER / COMMON bodies) bind IN LEXICAL CONTEXT with InF3When (RESUME-NEXT relaxation;
        // base TurnState per GR21) and are REDIRECTED into synthetic pc-range paragraphs (the pc-RANGE interceptor,
        // §9.1-B) run by the reused __RunUse; imp-5 (FINALLY) stays inline. The GR14 overlay is already popped, so
        // these bind against the base state (GR21/GR22).
        int performId = ctx.EcState.NextF3PerformId();
        int line = p.Start.Line;
        bool savedInWhen = ctx.EcState.InF3When;
        ctx.EcState.InF3When = true;
        var whens = new List<BoundExceptionMatch>();
        for (int i = 0; i < whenPhrases.Length; i++)
        {
            var body = host.BindBlocks(whenPhrases[i].statementBlock());
            int pc = ctx.Table.AddF3Handler(body, performId, line);
            whens.Add(new BoundExceptionMatch(headers[i].Mode, headers[i].Ops, pc));
        }
        int? otherPc = p.performWhenOther() is { } o
            ? ctx.Table.AddF3Handler(host.BindBlocks(o.statementBlock()), performId, line) : null;
        int? commonPc = p.performWhenCommon() is { } c
            ? ctx.Table.AddF3Handler(host.BindBlocks(c.statementBlock()), performId, line) : null;
        ctx.EcState.InF3When = savedInWhen;
        var final = p.performFinally() is { } f ? (IReadOnlyList<BoundStatement>)host.BindBlocks(f.statementBlock()) : null;

        bool handlerHasExit = HandlerBodiesContainExitPerform(p);
        return new BoundExceptionPerform(imp1, whens, otherPc, commonPc, final, withLocation, performId, handlerHasExit);
    }

    /// <summary>The OO-method staging stub (§9.7): reject an F3 PERFORM inside a method (COBOLNET0899 — the handler
    /// pc-ranges fall outside the method's contiguous dispatch slice), but still bind the handler bodies for their
    /// own diagnostics (discarded — a rejected program is never emitted) so a syntax error in a handler is reported.
    /// Returns a node with no handler pcs (never emitted).</summary>
    private BoundStatement F3StagedInMethodStub(Core.PerformStatementContext p, IReadOnlyList<BoundStatement> imp1, bool withLocation)
    {
        ctx.Edition.Error(DiagnosticCatalog.ConstructStagedNotImplemented,
            "an exception-checking (Format-3) PERFORM inside a method is recognized and fully syntax-checked, but "
            + "its runtime interception is not yet implemented for methods — the WHEN handler pc-ranges fall outside "
            + "the method's contiguous dispatch slice (a P14 GAP; ISO §14.9.28.4 GR17)");
        bool savedInWhen = ctx.EcState.InF3When;
        ctx.EcState.InF3When = true;
        foreach (var w in p.performWhenPhrase()) host.BindBlocks(w.statementBlock());
        if (p.performWhenOther() is { } o) host.BindBlocks(o.statementBlock());
        if (p.performWhenCommon() is { } c) host.BindBlocks(c.statementBlock());
        ctx.EcState.InF3When = savedInWhen;
        var final = p.performFinally() is { } f ? (IReadOnlyList<BoundStatement>)host.BindBlocks(f.statementBlock()) : null;
        return new BoundExceptionPerform(imp1, [], null, null, final, withLocation, ctx.EcState.NextF3PerformId(), false);
    }

    /// <summary>True when any handler body (imp-2/3/4 — the WHEN / WHEN OTHER / WHEN COMMON statement blocks) contains
    /// a plain <c>EXIT PERFORM</c> (recursion stops at a nested inline <c>performStatement</c>, whose EXIT PERFORM
    /// refers to that inner loop, §14.9.14.4 GR5a). Gates the emitted <c>catch (ExitPerformSignal)</c> boundary so a
    /// PERFORM whose handlers never EXIT-PERFORM emits no catch.</summary>
    private static bool HandlerBodiesContainExitPerform(Core.PerformStatementContext p)
    {
        var bodies = p.performWhenPhrase().SelectMany(w => (IEnumerable<IParseTree>)w.statementBlock())
            .Concat(p.performWhenOther() is { } o ? o.statementBlock() : [])
            .Concat(p.performWhenCommon() is { } c ? c.statementBlock() : []);
        return bodies.Any(ExitPerformOfThisPerform);
    }

    // A plain EXIT PERFORM (no CYCLE) whose nearest enclosing inline PERFORM is THIS Format-3 PERFORM (recursion
    // stops at a nested performStatement, which owns its own EXIT PERFORM — §14.9.14.4 GR5a).
    private static bool ExitPerformOfThisPerform(IParseTree node)
    {
        for (int i = 0; i < node.ChildCount; i++)
        {
            var child = node.GetChild(i);
            if (child is Core.ExitStatementContext ex && ex.PERFORM() is not null && ex.CYCLE() is null)
                return true;
            if (child is Core.PerformStatementContext) continue;   // a nested inline PERFORM owns its own EXIT PERFORM
            if (ExitPerformOfThisPerform(child)) return true;
        }
        return false;
    }

    /// <summary>Resolve ONE WHEN phrase's operands. The mode form (WHEN EXCEPTION INPUT|OUTPUT|I-O|EXTEND) is a
    /// STAGED runtime match (COBOLNET0899, §5.4-1). The name/file forms resolve each exception-name against the
    /// §14.6.13.1 catalog at ANY level (the USE GR3a-3g tiers select by level — NOT the RAISE level-3-only rule),
    /// enforce SR16 (a FILE-paired name shall begin EC-I-O), and the per-name edition window. Contributes GR14
    /// overlay enables and the SR14/SR15 census.</summary>
    private (string? Mode, List<BoundWhenOperand> Ops) ResolveWhenOperands(
        Core.PerformWhenPhraseContext w, List<(string, string?)> overlay, List<(string?, string?)> census)
    {
        var ops = new List<BoundWhenOperand>();

        // Alt 1: WHEN EXCEPTION { INPUT | OUTPUT | I-O | EXTEND | file-name-1… }
        if (w.performWhenModeList() is { } ml)
        {
            string? mode = ml.INPUT() is not null ? "INPUT" : ml.OUTPUT() is not null ? "OUTPUT"
                : ml.I_O() is not null ? "I-O" : ml.EXTEND() is not null ? "EXTEND" : null;
            if (mode is not null)
            {
                // The open-mode operand form's runtime match (by the raising file's current open mode) is an
                // additional staged sub-GAP beyond the general interception (§5.4-1) — covered by the one
                // COBOLNET0899 raised at the top of EcBindExceptionPerform.
                overlay.Add(("EC-I-O", null));   // enable EC-I-O checking over imp-1 for the eventual runtime
                return (mode, ops);
            }
            foreach (var fn in ml.fileName())
                if (ResolveWhenFile(fn.GetText()) is { } f)
                {
                    ops.Add(new BoundWhenOperand(null, f));
                    overlay.Add(("EC-I-O", f.CobolName));
                    census.Add((null, f.CobolName.ToUpperInvariant()));
                }
            return (null, ops);
        }

        // Alt 2: WHEN { exception-name-1 | exception-name-2 FILE file-name-2… }…
        foreach (var item in w.performWhenEcList()!.performWhenEcItem())
        {
            string raw = item.cobolWord().GetText();
            if (!ExceptionCatalog.TryGet(raw, out var info))
            {
                ctx.Edition.Error("COBOLNET0711", $"WHEN '{raw}': not an exception-name of ISO/IEC 1989 "
                    + "§14.6.13.1 (and not a valid EC-USER-/EC-IMP- name)");
                continue;
            }
            if (info.IntroducedIn > ctx.Edition.DialectLevel)
            {
                ctx.Edition.Error("COBOLNET0878", $"exception-name {info.Name} was introduced by ISO/IEC "
                    + $"1989:{info.IntroducedIn} — it requires --std {info.IntroducedIn} or later "
                    + $"(targeting COBOL-{ctx.Edition.DialectLevel})");
                continue;
            }
            var files = item.fileName();
            if (files.Length > 0 && !ExceptionCatalog.IsIoName(info.Name))
            {
                // PF3-SR16 (COBOLNET1601): if file-name-2 is specified, exception-name-2 shall begin 'EC-I-O'.
                ctx.Edition.Error("COBOLNET1601", $"WHEN exception-name '{info.Name}' is paired with FILE but does "
                    + "not begin with the COBOL characters 'EC-I-O' (ISO §14.9.28.3 SR16)");
                continue;
            }
            if (files.Length == 0)
            {
                ops.Add(new BoundWhenOperand(info.Name, null));
                overlay.Add((info.Name, null));
                census.Add((info.Name.ToUpperInvariant(), null));
                continue;
            }
            foreach (var fn in files)
                if (ResolveWhenFile(fn.GetText()) is { } f)
                {
                    ops.Add(new BoundWhenOperand(info.Name, f));
                    overlay.Add((info.Name, f.CobolName));
                    census.Add((info.Name.ToUpperInvariant(), f.CobolName.ToUpperInvariant()));
                }
        }
        return (null, ops);
    }

    private FileModel? ResolveWhenFile(string name)
    {
        if (ctx.Data.FilesByName.TryGetValue(name, out var file)) return file;
        ctx.Edition.Error("COBOLNET0897", $"WHEN names unknown file '{name}' (ISO §14.9.28.2 Format 3)");
        return null;
    }

    /// <summary>PF3-SR14 (COBOLNET1599) — a file-name shall not appear more than once across the WHEN phrases
    /// unless every instance is paired with an exception-name (§14.9.28.3 SR14). PF3-SR15 (COBOLNET1600) — an
    /// exception-name shall appear only once unless each occurrence pairs a DIFFERENT file-name (§14.9.28.3
    /// SR15).</summary>
    private void CheckSr14Sr15(List<(string? Ec, string? File)> census)
    {
        // SR14: group by file-name; a file that appears >1 time with ANY bare (unpaired) instance is illegal.
        foreach (var g in census.Where(c => c.File is not null).GroupBy(c => c.File))
            if (g.Count() > 1 && g.Any(c => c.Ec is null))
                ctx.Edition.Error("COBOLNET1599", $"file-name '{g.Key}' is specified in more than one WHEN phrase "
                    + "without an exception-name pairing (ISO §14.9.28.3 SR14)");

        // SR15: an exception-name may repeat ONLY "in conjunction with different file-names". A repeat is illegal
        // if ANY occurrence is bare (no file — not "in conjunction with a file-name"), or two occurrences share a
        // file. A bare occurrence's null is NOT a licensing "distinct file-name" (so bare+FILE-paired same name is
        // rejected, not just bare+bare).
        foreach (var g in census.Where(c => c.Ec is not null).GroupBy(c => c.Ec))
        {
            if (g.Count() <= 1) continue;
            var files = g.Select(c => c.File).ToList();
            if (files.Any(f => f is null) || files.Distinct().Count() != files.Count)
                ctx.Edition.Error("COBOLNET1600", $"exception-name '{g.Key}' is specified more than once without a "
                    + "distinct file-name (ISO §14.9.28.3 SR15)");
        }
    }

    /// <summary>The cross-statement bans (§14.9.28 + the cited sibling-statement SRs), by lexical region over the
    /// PARSE subtree of the Format-3 PERFORM. Region A = imperative-statement-1; region B = the whole PERFORM;
    /// region C = the WHEN phrases (imp-2/3/4); region D = imperative-statement 2..5.</summary>
    private void CheckCrossStatementBans(Core.PerformStatementContext p)
    {
        // Region content (statement-block subtrees).
        var regionA = p.statementBlock();
        var whenBodies = p.performWhenPhrase().SelectMany(w => (IEnumerable<IParseTree>)w.statementBlock())
            .Concat(p.performWhenOther() is { } o ? o.statementBlock() : [])
            .Concat(p.performWhenCommon() is { } c ? c.statementBlock() : []).ToList();   // region C
        var finallyBody = p.performFinally() is { } f ? f.statementBlock() : [];
        var regionD = whenBodies.Concat(finallyBody).ToList();
        var regionB = ((IEnumerable<IParseTree>)regionA).Concat(regionD).ToList();

        // ── Region B (whole PERFORM) ──
        // XS-EXIT-PERFORM-CYCLE (COBOLNET1604, §14.9.14.3 SR8) — plain EXIT PERFORM is legal (goto END-PERFORM);
        // EXIT PERFORM CYCLE is not (an F3 PERFORM is not a loop). A CYCLE bound to a NESTED inline PERFORM refers
        // to that loop, so recursion stops at a nested performStatement.
        foreach (var ex in regionB.SelectMany(ExitCyclesOfThisPerform))
            ctx.Edition.Error("COBOLNET1604", "EXIT PERFORM CYCLE shall not appear within an exception-checking "
                + "PERFORM (ISO §14.9.14.3 SR8)");
        foreach (var init in regionB.SelectMany(Descendants<Core.InitiateStatementContext>).Where(s => s.reportName().Length > 1))
            ctx.Edition.Error("COBOLNET1605", "an INITIATE naming more than one report-name shall not appear within "
                + "an exception-checking PERFORM (ISO §14.9.21.3 SR3)");
        foreach (var term in regionB.SelectMany(Descendants<Core.TerminateStatementContext>).Where(s => s.reportName().Length > 1))
            ctx.Edition.Error("COBOLNET1606", "a TERMINATE naming more than one report-name shall not appear within "
                + "an exception-checking PERFORM (ISO §14.9.46.3 SR3)");
        foreach (var val in regionB.SelectMany(Descendants<Core.ValidateFacilityStatementContext>).Where(s => s.dataReference().Length > 1))
            ctx.Edition.Error("COBOLNET1607", "a VALIDATE naming more than one identifier shall not appear within an "
                + "exception-checking PERFORM (ISO §14.9.50.3 SR6)");

        // ── Region A (imperative-statement-1 only) ──
        // XS-DELETE-FILE-MULTI (COBOLNET1613, §14.9.10.3 SR4) is UNREACHABLE under the current grammar — the
        // `deleteFileStatement : DELETE FILE fileName` rule admits only ONE file-name (the 2023 Format-2
        // `DELETE FILE [OVERRIDE] {file-name-1}…` multi-file shape is itself a separate unimplemented grammar
        // gap). 1613 stays RESERVED, to be emitted when that grammar lands (like the >>POP/>>PUSH-gated 1602/1603).
        foreach (var cl in regionA.SelectMany(Descendants<Core.CloseStatementContext>).Where(s => s.closeFilePhrase().Length > 1))
            ctx.Edition.Error("COBOLNET1612", "a multi-file CLOSE shall not appear in imperative-statement-1 of an "
                + "exception-checking PERFORM (ISO §14.9.6.3 SR3)");
        foreach (var ini in regionA.SelectMany(Descendants<Core.InitializeStatementContext>))
        {
            var names = ini.dataReferenceList().dataReference().Select(d => d.GetText().ToUpperInvariant()).ToList();
            foreach (var dup in names.GroupBy(n => n).Where(g => g.Count() > 1))
                ctx.Edition.Error("COBOLNET1614", $"identifier '{dup.Key}' is specified more than once in an "
                    + "INITIALIZE in imperative-statement-1 of an exception-checking PERFORM (ISO §14.9.20.3 SR2)");
        }
        foreach (var _ in regionA.SelectMany(Descendants<Core.MergeStatementContext>))
            ctx.Edition.Error("COBOLNET1615", "MERGE shall not appear in imperative-statement-1 of an "
                + "exception-checking PERFORM (ISO §14.9.24.3 SR1)");
        foreach (var op in regionA.SelectMany(Descendants<Core.OpenStatementContext>))
        {
            var names = op.openClause().SelectMany(oc => oc.openFileSpec())
                .Select(fs => fs.dataReference().GetText().ToUpperInvariant()).ToList();
            foreach (var dup in names.GroupBy(n => n).Where(g => g.Count() > 1))
                ctx.Edition.Error("COBOLNET1616", $"file-name '{dup.Key}' is specified more than once in an OPEN in "
                    + "imperative-statement-1 of an exception-checking PERFORM (ISO §14.9.27.3 SR3)");
        }
        foreach (var _ in regionA.SelectMany(Descendants<Core.SortStatementContext>))
            ctx.Edition.Error("COBOLNET1617", "SORT shall not appear in imperative-statement-1 of an "
                + "exception-checking PERFORM (ISO §14.9.40.3 SR3)");

        // ── Region C (WHEN phrases) ──
        foreach (var _ in whenBodies.SelectMany(Descendants<Core.GoToStatementContext>))
            ctx.Edition.Error("COBOLNET1608", "GO TO shall not appear in a WHEN phrase of an exception-checking "
                + "PERFORM (ISO §14.9.17.3 SR3)");

        // ── Region D (imperative-statement 2..5) ──
        foreach (var _ in regionD.SelectMany(Descendants<Core.RaiseStatementContext>))
            ctx.Edition.Error("COBOLNET1611", "RAISE shall appear only in imperative-statement-1 of an "
                + "exception-checking PERFORM (ISO §14.9.29.3 SR4)");
    }

    // EXIT PERFORM CYCLE statements whose nearest enclosing inline PERFORM is THIS Format-3 PERFORM (recursion
    // stops at a nested performStatement, whose EXIT PERFORM CYCLE refers to that nested loop).
    private static IEnumerable<Core.ExitStatementContext> ExitCyclesOfThisPerform(IParseTree node)
    {
        for (int i = 0; i < node.ChildCount; i++)
        {
            var child = node.GetChild(i);
            if (child is Core.ExitStatementContext ex && ex.PERFORM() is not null && ex.CYCLE() is not null)
                yield return ex;
            if (child is Core.PerformStatementContext) continue;   // a nested inline PERFORM owns its own CYCLE
            foreach (var d in ExitCyclesOfThisPerform(child)) yield return d;
        }
    }

    private static IEnumerable<T> Descendants<T>(IParseTree node) where T : class
    {
        for (int i = 0; i < node.ChildCount; i++)
        {
            var child = node.GetChild(i);
            if (child is T t) yield return t;
            foreach (var d in Descendants<T>(child)) yield return d;
        }
    }
}
