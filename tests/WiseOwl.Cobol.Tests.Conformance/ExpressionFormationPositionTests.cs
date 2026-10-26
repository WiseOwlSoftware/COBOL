// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// kb/Work PB1154 (branch review round 1, R1-1) — the expression-formation refusals (COBOL1719, ISO §8.8.1.2
/// Table 3 and §8.8.2 Table 4) are reported AT THE OFFENDING SYMBOL, in the same (line, column) convention every other
/// diagnostic prints. The diagnostic cursor already holds an ANTLR token's 0-based column; the pass added one more, so
/// the second sign, the second B-NOT and the shift count each printed one column to the right of the symbol. The
/// sibling cursors fixed in the same change (COBOL2927, COBOL2158) had the same defect. Each of the three arms
/// is asserted at the column of its offending token, with its legal twin compiling clean.
/// </summary>
public sealed class ExpressionFormationPositionTests
{
    private const string Head =
        "IDENTIFICATION DIVISION.\nPROGRAM-ID. PB1154X.\nDATA DIVISION.\nWORKING-STORAGE SECTION.\n"
        + "01 X PIC S9(4) VALUE 5.\n01 BX PIC 1(4) VALUE B\"1100\".\n01 BR PIC 1(4).\nPROCEDURE DIVISION.\nMAIN.\n";

    private const string Tail = "    STOP RUN.\n";

    /// <summary>The 1-based line and column, as the compiler prints them, of the character at index
    /// <paramref name="at"/>.</summary>
    private static (int Line, int Col) LineCol(string source, int at) =>
        (source.Take(at).Count(c => c == '\n') + 1, at - (source.LastIndexOf('\n', at) + 1) + 1);

    private static List<(int Line, int Col)> Positions1719(IEnumerable<string> diagnostics) =>
        diagnostics.Where(d => d.Contains("error COBOL1719:", StringComparison.Ordinal))
            .Select(d => Regex.Match(d, @"\((\d+),(\d+)\): error"))
            .Where(m => m.Success)
            .Select(m => (int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value))).ToList();

    /// <summary>Each row: the statement, then the text of the offending token's neighborhood located INSIDE the
    /// statement — <paramref name="before"/> is the statement text up to (not including) the offending token.</summary>
    [Theory]
    [InlineData("COMPUTE X = - - 2.", "COMPUTE X = - ")]                  // §8.8.1.2 Table 3: (unary sign, unary sign) = '—'; the second sign
    [InlineData("COMPUTE BR = B-NOT B-NOT BX.", "COMPUTE BR = B-NOT ")]    // §8.8.2 Table 4: (B-NOT, B-NOT) = '—'; the second B-NOT
    [InlineData("COMPUTE BR = BX B-SHIFT-L 1 + 1.", "COMPUTE BR = BX B-SHIFT-L ")]   // Table 4 shift row: the count is one symbol; its first token
    public void StackedOperatorOrCompoundShiftCount_IsReportedAtTheOffendingToken(string statement, string before)
    {
        string src = Head + "    " + statement + "\n" + Tail;
        var (ok, diag) = EditionHarness.Compile(src, 2023);
        Assert.False(ok, "expected COBOL1719; the program compiled");
        var at = Positions1719(diag);
        Assert.True(at.Count == 1, $"expected exactly one COBOL1719; got [{string.Join(" | ", diag)}]");
        Assert.Equal(LineCol(src, src.IndexOf(statement, StringComparison.Ordinal) + before.Length), at[0]);
    }

    [Theory]
    [InlineData("COMPUTE X = - 2.")]
    [InlineData("COMPUTE BR = B-NOT BX.")]
    [InlineData("COMPUTE BR = BX B-SHIFT-L 1.")]
    public void LegalTwins_CompileClean(string statement)
    {
        var (ok, diag) = EditionHarness.Compile(Head + "    " + statement + "\n" + Tail, 2023);
        Assert.True(ok, $"expected the legal twin to compile; got [{string.Join(" | ", diag)}]");
    }
}
