// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using Antlr4.Runtime.Atn;
using WiseOwl.Cobol.Frontend.Common;
using WiseOwl.Cobol.Frontend.Parsing;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

using Core = CobolParserCore;

/// <summary>
/// kb/Work PB2725 — <b>A QUOTED LITERAL'S CLASS IS DECIDED ONCE.</b> The four delimited-literal tokens (STRINGLIT, HEXLIT,
/// NATLIT, BOOLLIT) were listed in three grammar rules and a fourth residue rule, and five binders plus the compile-time
/// evaluator each mapped the token kind to a class and a decoder by hand beside <see cref="CobolLiteral.ClassOf"/>, two of
/// them with a different decoder. kb/Work PB1627 was the cost: <c>allLiteral</c> kept its own token list and missed the
/// operands <c>concatOperand</c> had gained. The tokens are now the one <c>quotedLiteral</c> rule, read only through
/// <c>Parsing.QuotedLiteral</c>; these pins keep a second list and a second ladder from coming back.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class QuotedLiteralDriftTests
{
    private static readonly int[] QuotedTokens = [CobolLexer.STRINGLIT, CobolLexer.HEXLIT, CobolLexer.NATLIT, CobolLexer.BOOLLIT];

    /// <summary>No parser rule but <c>quotedLiteral</c> offers two or more of the four tokens as alternatives at its left
    /// edge: a rule that admits a quoted literal names <c>quotedLiteral</c>.</summary>
    [Fact]
    public void OnlyQuotedLiteral_ListsTheQuotedLiteralTokens()
    {
        var atn = Core._ATN;
        var offenders = new List<string>();
        for (int rule = 0; rule < atn.ruleToStartState.Length; rule++)
        {
            if (rule == Core.RULE_quotedLiteral) continue;
            var edge = LeftEdgeTokens(atn, rule);
            if (QuotedTokens.Count(edge.Contains) >= 2) offenders.Add(Core.ruleNames[rule]);
        }
        Assert.True(offenders.Count == 0, "these rules list the quoted-literal tokens instead of naming quotedLiteral "
            + "(kb/Work PB2725): " + string.Join(", ", offenders));
        Assert.True(QuotedTokens.All(LeftEdgeTokens(atn, Core.RULE_quotedLiteral).Contains),
            "quotedLiteral no longer offers all four quoted-literal tokens — the scan is blind");
    }

    /// <summary>The product's committed C# (the generated parser is not committed).</summary>
    private static readonly TreeScope ProductSources = new(Under: ["src/"], Kinds: [".cs"]);

    /// <summary>A parse-tree read of one of the four tokens by its accessor.</summary>
    private static readonly Regex TokenAccessor = new(@"\.(STRINGLIT|HEXLIT|NATLIT|BOOLLIT)\(\)");

    /// <summary>No hand-written source reads a quoted literal through its token accessor: the class and value come from
    /// <c>QuotedLiteral</c>.</summary>
    [Fact]
    public void NoSource_ReadsAQuotedLiteralByItsTokenKind()
    {
        Assert.Matches(TokenAccessor, "nn.HEXLIT() is { } x");   // the detector sees the shape it bars
        var offenders = TrackedTree.Files(ProductSources)
            .SelectMany(f => File.ReadLines(TestRepo.Within("src/", f)).Select((t, i) => (File: f, Line: i + 1, Text: t)))
            .Where(l => !l.Text.TrimStart().StartsWith("//", StringComparison.Ordinal) && TokenAccessor.IsMatch(l.Text))
            .Select(l => $"{l.File}:{l.Line}  {l.Text.Trim()}")
            .ToList();
        Assert.True(offenders.Count == 0, "a quoted literal is read by its token kind instead of through QuotedLiteral "
            + "(kb/Work PB2725):" + Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    /// <summary>The class follows the written prefix (§2023:8.3.3.2.1, §2023:8.3.3.4.1, §2023:8.3.3.5.1), and the format is
    /// hexadecimal for <c>X</c>, <c>NX</c> and <c>BX</c> (each class's Format 2 general format), closed or
    /// not: an open literal is still its token's class.</summary>
    [Theory]
    [InlineData("\"AB\"", LiteralClass.Alphanumeric, false)]
    [InlineData("'AB'", LiteralClass.Alphanumeric, false)]
    [InlineData("X\"41\"", LiteralClass.Alphanumeric, true)]
    [InlineData("x'41'", LiteralClass.Alphanumeric, true)]
    [InlineData("N\"AB\"", LiteralClass.National, false)]
    [InlineData("NX\"0041\"", LiteralClass.National, true)]
    [InlineData("B\"01\"", LiteralClass.Boolean, false)]
    [InlineData("BX\"F\"", LiteralClass.Boolean, true)]
    [InlineData("\"AB", LiteralClass.Alphanumeric, false)]
    [InlineData("N\"AB", LiteralClass.National, false)]
    public void FormOfToken_ReadsThePrefix(string token, LiteralClass cls, bool hexadecimal) =>
        Assert.Equal((cls, hexadecimal), CobolLiteral.FormOfToken(token));

    /// <summary>Text that opens no delimiter is no quoted-literal token, and is refused rather than given a class.</summary>
    [Fact]
    public void FormOfToken_RefusesAWord() =>
        Assert.Throws<ArgumentException>(() => CobolLiteral.FormOfToken("ABC"));

    /// <summary>The tokens at <paramref name="rule"/>'s left edge, reached by epsilon edges without entering a called rule.</summary>
    private static HashSet<int> LeftEdgeTokens(ATN atn, int rule)
    {
        var tokens = new HashSet<int>();
        var seen = new HashSet<ATNState>();
        var work = new Stack<ATNState>([atn.ruleToStartState[rule]]);
        while (work.TryPop(out var s))
        {
            if (!seen.Add(s)) continue;
            for (int i = 0; i < s.NumberOfTransitions; i++)
            {
                var t = s.Transition(i);
                if (t is RuleTransition) continue;
                if (t.IsEpsilon) work.Push(t.target);
                else if (t.Label is { } label) tokens.UnionWith(label.ToList());
            }
        }
        return tokens;
    }
}
