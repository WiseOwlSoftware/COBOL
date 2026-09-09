// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Editions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The COMPOSITION validator's own behaviour (kb/Work PB528): the §13.18.40.3 rules it enforces and — the half
/// that matters more — the legal shapes it must never reject. The conformance corpus proves the same facts end to
/// end; these run at the unit boundary so a refactor that stops CALLING the validator, or one that widens a
/// prohibition by a symbol, fails in seconds rather than in the battery.
/// </summary>
public sealed class PictureCompositionTests
{
    private static string[] Diagnose(string picture, bool blankWhenZero = false, bool decimalComma = false)
    {
        var ed = new EditionContext(2023);
        PictureAnalyzer.Analyze(picture, Usage.Display, ed, "data item 'T'", blankWhenZero: blankWhenZero,
            decimalPointIsComma: decimalComma);
        return ed.Diagnostics.ToArray();
    }

    [Theory]
    // SR16 — a continuous string of 'P's at the leftmost or rightmost digit positions
    [InlineData("9P9")]
    [InlineData("PP9PP")]
    // SR17 / SR20 — 'P' and 'V' are each mutually exclusive with the decimal separator
    [InlineData("PP99.99")]
    [InlineData("9V9.9")]
    // SR18 — 'S' shall be the first symbol
    [InlineData("9S9")]
    [InlineData("999S")]
    // SR19 — 'V' adjacent to the 'P' string
    [InlineData("9V9PP")]
    // SR21 — 'Z' and '*'
    [InlineData("ZZ**9")]
    // SR23 — the editing sign control symbols are mutually exclusive
    [InlineData("+999CR")]
    // SR24 — one FIXED currency symbol, one FIXED editing sign control symbol
    [InlineData("+999+")]
    [InlineData("$999$")]
    // SR12 b — the once-only set
    [InlineData("9V9V9")]
    [InlineData("99.99.99")]
    [InlineData("999CRCR")]
    // SR12 a — a character-string with no digit position and no character position
    [InlineData("BBB")]
    [InlineData("+")]
    public void ACompositionViolation_IsCOBOLNET1934(string picture)
        => Assert.Contains(Diagnose(picture), d => d.Contains("COBOLNET1934"));

    [Theory]
    // Table 10 blanks: no '9' before a zero-suppression symbol; no 'A'/'X' before 'V'; nothing after CR/DB;
    // a lone sign or currency symbol that is neither leftmost nor rightmost (SR25 / SR26 without a rule of
    // their own); two floating strings (SR27); a 'V' contradicting a trailing 'P' string's implied point.
    [InlineData("9ZZ")]
    [InlineData("XV9")]
    [InlineData("99CR99")]
    [InlineData("9+9")]
    [InlineData("9$9")]
    [InlineData("++$$$")]
    [InlineData("99VPP")]
    public void APrecedenceViolation_IsCOBOLNET1935(string picture)
        => Assert.Contains(Diagnose(picture), d => d.Contains("COBOLNET1935"));

    [Theory]
    // SR22 — neither 'S' nor '*' beside a BLANK WHEN ZERO clause. The 'S' leg was worse than unenforced: the
    // numeric-edited promotion required !signed, so the clause was silently DISCARDED.
    [InlineData("***9")]
    [InlineData("S9(5)")]
    public void BlankWhenZeroWithSOrAsterisk_IsCOBOLNET1934(string picture)
        => Assert.Contains(Diagnose(picture, blankWhenZero: true), d => d.Contains("COBOLNET1934"));

    [Theory]
    // ⛔ THE HALF THAT MATTERS MORE. A prohibition written one symbol too wide rejects LEGAL source, which is
    // worse than the under-rejection it replaces; these are the shapes the standard permits.
    [InlineData("ZZ9")] [InlineData("99PP")] [InlineData("PP99")] [InlineData("S999")]
    [InlineData("99PPV")] [InlineData("VPP99")] [InlineData("***9")] [InlineData("$999+")]
    [InlineData("$$$9.99")] [InlineData("+$$99")] [InlineData("999$+")] [InlineData("++")]
    [InlineData("999.99CR")] [InlineData("ZZZ,ZZ9.99")] [InlineData("XXBXX")] [InlineData("9,999")]
    [InlineData("-999")] [InlineData("999-")] [InlineData("9V9")] [InlineData("$$$$PP")]
    [InlineData("S9(5)V99")] [InlineData("X(120)")] [InlineData("$**.99")] [InlineData("$99.99CR")]
    [InlineData("+$$$,$$9.99")] [InlineData("Z(5)")] [InlineData("9(18)")] [InlineData("AAAA")]
    public void ALegalCharacterString_IsAccepted(string picture)
        => Assert.DoesNotContain(Diagnose(picture), d => d.Contains("COBOLNET1934") || d.Contains("COBOLNET1935"));

    /// <summary>
    /// ISO §13.18.40.3 SR13 and §13.18.40.6's closing paragraph interchange the comma and period ROLES, so the
    /// whole rule set transfers with one flag. Without it the NIST comma-mode shapes (NC107A's
    /// <c>9.999.999,99</c>, SM103A's <c>ZZ.ZZZ.ZZZ,99</c>) are rejected as legal source — the exact regression a
    /// composition validator that forgot SR13 would cause, and the reason the flag is threaded from DataBinder.
    /// </summary>
    [Theory]
    [InlineData("9.999.999,99", true, false)]     // legal in comma mode: two GROUPING periods
    [InlineData("ZZ.ZZZ.ZZZ,99", true, false)]
    [InlineData("9.999.999,99", false, true)]     // the same string in period mode: two DECIMAL separators
    [InlineData("9V9,9", true, true)]             // SR20 transferred to the comma
    [InlineData("PP99,99", true, true)]           // SR17 transferred to the comma
    [InlineData("ZZZ,ZZ9.99", false, false)]      // legal in period mode
    public void TheCommaPeriodRolesFollowDecimalPointIsComma(string picture, bool commaMode, bool rejected)
    {
        var diags = Diagnose(picture, decimalComma: commaMode);
        bool got = diags.Any(d => d.Contains("COBOLNET1934") || d.Contains("COBOLNET1935"));
        Assert.True(got == rejected,
            $"PIC {picture} with DECIMAL-POINT IS COMMA={commaMode} should be "
            + $"{(rejected ? "REJECTED" : "accepted")}: {string.Join("; ", diags)}");
    }

    /// <summary>
    /// ⛔ ONE floating-string definition (§13.18.40.5 rule 6), read by the composition rules AND by
    /// <c>PictureAnalyzer</c>'s geometry. Floating-ness is ADJACENCY ("a string of at least two identical
    /// floating insertion editing symbols", with the simple insertion symbols and — rule 6 b — the decimal point
    /// embedded), never a bare occurrence count: the count this replaced read <c>+999+</c> and <c>$999$</c> as
    /// floating strings and silently answered a question the source never asked.
    /// </summary>
    [Theory]
    [InlineData("$$$9.99", '$', 3)]
    [InlineData("$$,$$9", '$', 4)]
    [InlineData("$$$.$$", '$', 5)]      // rule 6 b: the string spans the decimal point
    [InlineData("+$$99", '$', 2)]       // the lone '+' is FIXED; the two '$' are the floating string
    [InlineData("++", '+', 2)]
    [InlineData("$999$", '\0', 0)]      // separated by digits: two FIXED currency symbols, not a string
    [InlineData("+999+", '\0', 0)]
    [InlineData("$999+", '\0', 0)]
    [InlineData("999", '\0', 0)]
    public void AFloatingStringIsAdjacency_NotACount(string expanded, char symbol, int occurrences)
        => Assert.Equal((symbol, occurrences),
            PictureComposition.FloatingString(expanded, '$', new HashSet<char>(), '.', ','));
    /// <summary>
    /// ⛔ THE SCALE GATE. §13.18.40.3 SR4 caps character-string-1 at 63 characters AS WRITTEN, which
    /// <c>PIC X(30000)</c> obeys — so <c>ExpandRepeats</c> hands the composition validator a 30 000-SYMBOL
    /// string, and Table 10's relation binds every ordered PAIR of them. Walked as pairs that is 9x10^8
    /// comparisons inside the binder; walked as a bit set of the 24 ROLES seen so far it is one test per
    /// symbol. This pins the difference: the quadratic form takes minutes on these inputs and the linear one
    /// milliseconds, so the bound is generous and still cannot be met by an accidental regression.
    /// </summary>
    [Fact]
    public void ALargeRepeatExpandedPicture_CostsLinearTime()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        foreach (string picture in new[] { "X(30000)", "9(30000)", "Z(30000)", "$(30000)", "9(20000)V9(9000)" })
            Diagnose(picture);
        sw.Stop();
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(5),
            $"the §13.18.40.6 Table 10 walk took {sw.Elapsed.TotalSeconds:F1}s over five repeat-expanded "
            + "pictures — it is quadratic in the symbol count again");
    }
}
