// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Validation;

/// <summary>
/// The POSITION rule of the ALL form of <c>&gt;&gt;PUSH</c> / <c>&gt;&gt;POP</c> (kb/Work PB1005):
/// <list type="bullet">
/// <item>ISO §7.3.22.3 SR3 — "If ALL is specified, the PUSH directive shall be specified only in a compilation unit
/// between clauses in divisions other than the procedure division and between statements in the procedure
/// division."</item>
/// <item>ISO §7.3.20.3 SR3 — the same for POP.</item>
/// </list>
/// <para>A compiler directive is in no parse tree, so its position is the GAP between the last token before its
/// line and the first token after it — the final line frame, where a directive line and a token line are the same
/// number (<see cref="DirectiveSiteProcessor"/>). Two arms, one warning (<c>COBOLNET2344</c>, §4.2.2):</para>
/// <list type="number">
/// <item>OUTSIDE a compilation unit — before the first unit's first token, or after a unit's END marker and before
/// the next unit. A program with no END PROGRAM header runs on to the end of the text, so a directive after
/// its last token is still inside it.</item>
/// <item>INSIDE a clause or statement — the innermost clause (a <c>…Clause</c> rule) or <see
/// cref="CobolParserCore.StatementContext"/> spanning the gap, unless the gap is itself a boundary of a nested
/// clause or statement (the next token STARTS one, or the previous token ENDS one): <c>IF X = 1 &gt;&gt;PUSH ALL
/// DISPLAY …</c> is between statements; <c>PERFORM VARYING I FROM 1 &gt;&gt;PUSH ALL BY 1 …</c> is not.</item>
/// </list>
/// <para>The rule reads "between clauses", and an entry's name, a paragraph header or a division header is not a
/// clause; those gaps are not diagnosed — the reading that cannot reject a position the rule admits.</para>
/// </summary>
internal static class PushPopAllPlacementPass
{
    public static void Run(CobolParserCore.CompilationUnitContext tree, IReadOnlyList<DirectiveSite> sites,
        CobolNet.Binding.EditionContext sink)
    {
        List<DirectiveSite>? all = null;
        foreach (var s in sites) if (s.AllForm) (all ??= []).Add(s);
        if (all is null) return;   // the common case: no tree walk at all

        var tokens = new List<ITerminalNode>();
        Collect(tree, tokens);
        var units = TopLevelUnits(tree);
        foreach (var site in all)
        {
            // The gap: the last token before the directive's line and the first token after it.
            int k = FirstAfter(tokens, site.Line);
            ITerminalNode? next = k < tokens.Count ? tokens[k] : null;
            ITerminalNode? prev = k > 0 ? tokens[k - 1] : null;
            string? where = !InsideUnit(units, prev) ? "outside every compilation unit"
                : InnermostContainer(prev!, next) is { } c ? $"inside {Describe(c)}"
                : null;
            if (where is null) continue;
            var (rule, text) = site.Word == "PUSH" ? PushRule : PopRule;
            using var _ = sink.At(site.Line, 0);
            sink.Warning(DiagnosticCatalog.PushPopAllPlacement,
                $"the >>{site.Word} ALL directive on line {site.Line} is written {where} — \"{text}\" (ISO {rule}). "
                + "The directive is still processed. Move it to a point between two clauses or statements of the "
                + "compilation unit.");
        }
    }

    /// <summary>The two rules, each in its own words (the PUSH sentence is printed without the POP sentence's commas).</summary>
    private static readonly (string Rule, string Text) PushRule = ("§7.3.22.3 SR3",
        "If ALL is specified, the PUSH directive shall be specified only in a compilation unit between clauses in "
        + "divisions other than the procedure division and between statements in the procedure division");
    private static readonly (string Rule, string Text) PopRule = ("§7.3.20.3 SR3",
        "If ALL is specified, the POP directive shall be specified only in a compilation unit, between clauses in "
        + "divisions other than the procedure division, and between statements in the procedure division");

    /// <summary>Every token of the tree in source order, EOF excluded.</summary>
    private static void Collect(IParseTree node, List<ITerminalNode> into)
    {
        if (node is ITerminalNode t)
        {
            if (t.Symbol.Type != TokenConstants.EOF) into.Add(t);
            return;
        }
        for (int i = 0; i < node.ChildCount; i++) Collect(node.GetChild(i), into);
    }

    /// <summary>The index of the first token on a line after <paramref name="line"/> (tokens are line-ordered).</summary>
    private static int FirstAfter(List<ITerminalNode> tokens, int line)
    {
        int lo = 0, hi = tokens.Count;
        while (lo < hi)
        {
            int mid = (lo + hi) >> 1;
            if (tokens[mid].Symbol.Line <= line) lo = mid + 1; else hi = mid;
        }
        return lo;
    }

    /// <summary>Each top-level compilation unit's first and last token index, and whether it ends with an END
    /// marker (a class or interface always does; a program only with its END PROGRAM header).</summary>
    private static List<(int Start, int Stop, bool Closed)> TopLevelUnits(CobolParserCore.CompilationUnitContext tree)
    {
        var units = new List<(int, int, bool)>();
        foreach (var group in tree.compilationGroup())
            for (int i = 0; i < group.ChildCount; i++)
                if (group.GetChild(i) is ParserRuleContext u)
                    units.Add((u.Start.TokenIndex, u.Stop.TokenIndex,
                        u is not CobolParserCore.ProgramUnitContext p || p.endProgramHeader() is not null));
        return units;
    }

    private static bool InsideUnit(List<(int Start, int Stop, bool Closed)> units, ITerminalNode? prev)
    {
        if (prev is null) return false;   // before the first token of the group
        int p = prev.Symbol.TokenIndex;
        foreach (var (start, stop, closed) in units)
        {
            if (p < start) continue;
            if (p < stop) return true;                      // strictly inside the unit's token span
            if (p == stop) return !closed;   // after an END marker: outside; with none, the unit runs on
        }
        return false;
    }

    /// <summary>The innermost clause or statement that spans the gap between <paramref name="prev"/> and
    /// <paramref name="next"/>, when the gap is not a boundary of a nested one; null when the gap lies between
    /// clauses or statements.</summary>
    private static ParserRuleContext? InnermostContainer(ITerminalNode prev, ITerminalNode? next)
    {
        if (next is null) return null;   // after the last token: the end of the unit, not inside a construct
        int p = prev.Symbol.TokenIndex, n = next.Symbol.TokenIndex;
        // Below the lowest common ancestor, a clause or statement that STARTS at next (or ENDS at prev) makes the
        // gap a boundary between two such constructs.
        for (var a = next.Parent as ParserRuleContext; a is not null; a = a.Parent as ParserRuleContext)
        {
            if (a.Start.TokenIndex <= p) break;
            if (IsContainer(a) && a.Start.TokenIndex == n) return null;
        }
        ParserRuleContext? lca = null;
        for (var a = prev.Parent as ParserRuleContext; a is not null; a = a.Parent as ParserRuleContext)
        {
            if (a.Stop is not null && a.Stop.TokenIndex >= n) { lca = a; break; }
            if (IsContainer(a) && a.Stop?.TokenIndex == p) return null;
        }
        for (var a = lca; a is not null; a = a.Parent as ParserRuleContext)
            if (IsContainer(a)) return a;
        return null;
    }

    private static bool IsContainer(ParserRuleContext c) =>
        c is CobolParserCore.StatementContext
        || CobolParserCore.ruleNames[c.RuleIndex].EndsWith("Clause", StringComparison.Ordinal);

    private static string Describe(ParserRuleContext c) =>
        c is CobolParserCore.StatementContext s
            ? $"the {s.Start.Text.ToUpperInvariant()} statement starting on line {s.Start.Line}"
            : $"a clause ({CobolParserCore.ruleNames[c.RuleIndex]}) starting on line {c.Start.Line}";
}
