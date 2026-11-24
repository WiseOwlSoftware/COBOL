// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE EXCEPTION-SELECTION OPERANDS ARE WRITTEN ONCE, AND THE USE AND PERFORM STATEMENTS BOTH NAME THEM (kb/Work PB6139).
///
/// <para><b>The rule.</b> §2023:14.9.28.4 GR17: "The rules for determining a match are specified in General rules 3a to 3g of
/// the USE statement." A Format-3 PERFORM's WHEN operand is therefore a USE operand: <c>exception-name-1 | exception-name-2
/// { FILE file-name-2 }…</c> (USE Format 3, WHEN) and the open mode <c>INPUT | OUTPUT | I-O | EXTEND</c> (USE Format 1, WHEN
/// EXCEPTION). The grammar wrote each twice (<c>useEcEntry</c> / <c>performWhenEcItem</c>, and the four mode alternatives inside
/// <c>useOnTarget</c> / <c>performWhenModeList</c>), and two binders resolved them. They are <c>exceptionSelectionOperand</c> and
/// <c>openMode</c> now (the OPEN statement's own mode rule, a third copy this test found: the GR3b tier selects by the mode a
/// file is open in), read by <c>ExceptionSelectionOperands.Resolve</c> and <c>OpenModeOperand.Of</c>.</para>
///
/// <para><b>The property.</b> No rule but <c>exceptionSelectionOperand</c> writes <c>cobolWord (FILE fileName)*</c>; no rule
/// but <c>openMode</c> offers INPUT, OUTPUT, I_O and EXTEND as bare sibling alternatives; and the five operand
/// positions name the shared rules. The file-name LISTS stay per statement on purpose: a WHEN list's continuation is gated
/// by <c>whenOperandAhead()</c> because imperative-statement-2 follows it, and a USE list is followed by a period.</para>
///
/// <para>⚠ It reads the GRAMMAR SOURCE: a re-inlined copy parses exactly the same input, so only the <c>.g4</c> shows it.</para>
/// </summary>
public sealed class ExceptionSelectionOperandDriftTests
{
    private const string OperandRule = "exceptionSelectionOperand";
    private const string OpenModeRule = "openMode";
    private static readonly string[] OpenModeTokens = ["INPUT", "OUTPUT", "I_O", "EXTEND"];

    private static Dictionary<string, string> Rules() =>
        Table12StatementNameDriftTests.ScrapeGrammarRules()
            .ToDictionary(r => r.Key, r => GrammarSource.WithoutLabels(r.Value), StringComparer.Ordinal);

    [Fact]
    public void TheExceptionNameFileOperand_IsWrittenOnlyByTheSharedRule()
    {
        var rules = Rules();
        var pairShape = new Regex(@"\bcobolWord\s*\(\s*FILE\s+fileName\s*\)\s*[*+]");
        Assert.Matches(pairShape, rules[OperandRule]);
        var copies = rules.Where(r => r.Key != OperandRule && pairShape.IsMatch(r.Value)).Select(r => r.Key).ToList();
        Assert.True(copies.Count == 0, "rule(s) writing the exception-name { FILE file-name }… operand instead of naming "
            + $"{OperandRule}: {string.Join(", ", copies)}");
    }

    [Fact]
    public void TheOpenModeOperand_IsWrittenOnlyByTheSharedRule()
    {
        var rules = Rules();
        Assert.Equal(OpenModeTokens, BareTokenAlternatives(rules[OpenModeRule]));
        var copies = rules.Where(r => r.Key != OpenModeRule
                && AlternativeLists(r.Value).Any(list => OpenModeTokens.All(list.Contains)))
            .Select(r => r.Key).ToList();
        Assert.True(copies.Count == 0, "rule(s) offering INPUT | OUTPUT | I-O | EXTEND as sibling alternatives instead of "
            + $"naming {OpenModeRule}: {string.Join(", ", copies)}");
    }

    [Theory]
    [InlineData("useStatement", OperandRule)]
    [InlineData("performWhenEcList", OperandRule)]
    [InlineData("useOnTarget", OpenModeRule)]
    [InlineData("performWhenModeList", OpenModeRule)]
    [InlineData("openClause", OpenModeRule)]
    public void EachOperandPosition_NamesTheSharedRule(string position, string shared) =>
        Assert.Matches(new Regex($@"\b{shared}\b"), Rules()[position]);

    /// <summary>The bare-token alternatives of every alternative list of a rule body (its top level and each parenthesized
    /// group), each list in written order.</summary>
    private static IEnumerable<List<string>> AlternativeLists(string body)
    {
        yield return BareTokenAlternatives(body);
        for (int open = body.IndexOf('('); open >= 0; open = body.IndexOf('(', open + 1))
        {
            int depth = 0, close = open;
            for (; close < body.Length; close++)
                if (body[close] == '(') depth++;
                else if (body[close] == ')' && --depth == 0) break;
            yield return BareTokenAlternatives(body[(open + 1)..close]);
        }
    }

    private static List<string> BareTokenAlternatives(string list)
    {
        var alternatives = new List<string>();
        int depth = 0, start = 0;
        for (int i = 0; i <= list.Length; i++)
        {
            char c = i < list.Length ? list[i] : '|';
            if (c == '(') depth++;
            else if (c == ')') depth--;
            else if (c == '|' && depth == 0)
            {
                alternatives.Add(list[start..i].Trim());
                start = i + 1;
            }
        }
        return alternatives.Where(a => Regex.IsMatch(a, "^[A-Z_]+$")).ToList();
    }
}
