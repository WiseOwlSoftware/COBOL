// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>The ONE literal codec (rearchitecture PHASE 05, Step 1): ISO/IEC 1989:2023 §8.3.3.1 — the
/// quotation-mark and apostrophe delimiter forms are EQUAL-STANDING, and a doubled OPENING delimiter is one
/// embedded delimiter. Locks the apostrophe-delimited handling the retired hard-coded <c>'"'</c>-only guards
/// silently miscompiled.</summary>
public sealed class CobolLiteralTests
{
    [Theory]
    // Both delimiters decode; a doubled opening delimiter collapses to one embedded delimiter.
    [InlineData("\"AB\"", "AB")]
    [InlineData("'AB'", "AB")]
    [InlineData("'AB''C'", "AB'C")]        // apostrophe form with an embedded apostrophe
    [InlineData("\"AB\"\"C\"", "AB\"C")]  // quote form with an embedded quote
    [InlineData("N'XY'", "XY")]            // national, apostrophe form (prefix stripped)
    [InlineData("B'01'", "01")]            // boolean, apostrophe form
    [InlineData("N\"XY\"", "XY")]         // national, quote form
    [InlineData("42", "42")]               // not a string literal → returned unchanged
    public void Decode_BothDelimiters(string raw, string expected) => Assert.Equal(expected, CobolLiteral.Decode(raw));

    [Theory]
    [InlineData("'x'", true)]
    [InlineData("\"x\"", true)]
    [InlineData("N'x'", true)]
    [InlineData("B\"0\"", true)]
    [InlineData("x", false)]               // bare word
    [InlineData("42", false)]              // numeric
    [InlineData("'x", false)]              // unterminated
    [InlineData("\"x'", false)]           // mismatched delimiters
    public void IsStringLiteral_BothDelimiters(string raw, bool expected) =>
        Assert.Equal(expected, CobolLiteral.IsStringLiteral(raw));

    /// <summary>kb/Work PB1615 — the literal LENGTH bound is the edition's: 8,191 positions from COBOL-2014 (§8.3.3.2.3 SR1,
    /// §8.3.3.4.3 SR1, §8.3.3.5.3 SR1), 160 below it (derived, VCR row 7.41). The boundary from both sides, per class and
    /// delimiter, with the measure the rule uses: the literal's VALUE in positions of its class (a doubled embedded
    /// delimiter is one position; a prefix letter and the delimiters are none; a <c>BX</c> digit is four).</summary>
    [Theory]
    [InlineData(85, 160)]
    [InlineData(2002, 160)]
    [InlineData(2014, 8191)]
    [InlineData(2023, 8191)]
    public void MaxPositions_IsTheEditionsBound(int edition, int expected) =>
        Assert.Equal(expected, CobolLiteral.MaxPositions(edition));

    [Fact]
    public void LengthViolation_TheBoundaryFromBothSides_PerEdition()
    {
        string Alnum(int n) => "\"" + new string('A', n) + "\"";
        foreach (int edition in new[] { 85, 2002 })
        {
            Assert.Null(CobolLiteral.LengthViolation(Alnum(160), edition));
            Assert.Null(CobolLiteral.LengthViolation("'" + new string('A', 160) + "'", edition));
            string over = CobolLiteral.LengthViolation(Alnum(161), edition)!;
            Assert.Contains("161 alphanumeric", over);
            Assert.Contains("an alphanumeric literal to 160", over);       // the edition's bound, with its article
            Assert.Contains($"COBOL-{edition}", over);                      // and whose it is
            Assert.Contains("8,191 from COBOL-2014", over);                 // where the 2023 text's number begins
        }
        foreach (int edition in new[] { 2014, 2023 })
        {
            Assert.Null(CobolLiteral.LengthViolation(Alnum(161), edition));
            Assert.Null(CobolLiteral.LengthViolation(Alnum(8191), edition));
            string over = CobolLiteral.LengthViolation(Alnum(8192), edition)!;
            Assert.EndsWith("limits an alphanumeric literal to 8,191", over);   // no edition tail from 2014
        }
    }

    [Theory]
    [InlineData("\"{0}\"", 160, 161, 85)]                 // alphanumeric
    [InlineData("N\"{0}\"", 160, 161, 2002)]              // national
    [InlineData("B\"{0}\"", 160, 161, 2002)]              // boolean, the digits themselves
    public void LengthViolation_MeasuresThePositionsOfTheClass(string template, int atBound, int over, int edition)
    {
        string Fill(int n) => string.Format(template, new string(template[0] == 'B' ? '1' : 'A', n));
        Assert.Null(CobolLiteral.LengthViolation(Fill(atBound), edition));
        Assert.NotNull(CobolLiteral.LengthViolation(Fill(over), edition));
    }

    [Fact]
    public void LengthViolation_CountsADoubledDelimiterOnce_AHexPairOnce_AndABooleanHexDigitFourTimes()
    {
        // 160 doubled quotation marks are 160 positions (§8.3.3.2.3 SR3: two contiguous quotation symbols represent a single occurrence of that quotation symbol
        // character), not 320 and not 162 with the delimiters; 161 are one over.
        Assert.Null(CobolLiteral.LengthViolation("\"" + string.Concat(Enumerable.Repeat("\"\"", 160)) + "\"", 85));
        Assert.NotNull(CobolLiteral.LengthViolation("\"" + string.Concat(Enumerable.Repeat("\"\"", 161)) + "\"", 85));
        // X"41" repeated: two digits per alphanumeric position (§8.3.3.2.3 SR6) — 160 positions are 320 source digits.
        Assert.Null(CobolLiteral.LengthViolation("X\"" + string.Concat(Enumerable.Repeat("41", 160)) + "\"", 2002));
        Assert.NotNull(CobolLiteral.LengthViolation("X\"" + string.Concat(Enumerable.Repeat("41", 161)) + "\"", 2002));
        // BX: §8.3.3.4.4 GR5 — each digit IS four boolean characters, so 40 digits are 160 positions and 41 are 164. The
        // token is 43 characters long at 41 digits: past a quarter of the bound, so the fast path must not take it.
        Assert.Null(CobolLiteral.LengthViolation("BX\"" + new string('F', 40) + "\"", 2002));
        Assert.NotNull(CobolLiteral.LengthViolation("BX\"" + new string('F', 41) + "\"", 2002));
        Assert.Null(CobolLiteral.LengthViolation("BX\"" + new string('F', 41) + "\"", 2014));
    }

    [Theory]
    [InlineData("ALL 'x'", "x")]
    [InlineData("ALL \"x\"", "x")]
    [InlineData("ALL'ab'", "ab")]          // tolerant of the missing space
    [InlineData("ALL ZEROS", null)]        // a figurative word, not a quoted literal
    [InlineData("'x'", null)]              // not the ALL form
    public void AllLiteralText_BothDelimiters(string raw, string? expected) =>
        Assert.Equal(expected, CobolLiteral.AllLiteralText(raw));
}
