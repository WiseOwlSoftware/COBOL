// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using CobolNet.Runtime.Exceptions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// PICTURE format 2 locale editing / de-editing (ISO §13.18.40.4 GR16–18, §13.18.40.5 r9–r15; kb/Work PB64 T6),
/// pinned against the INVARIANT locale (tag "") whose LC_MONETARY is stable by definition: currency string ¤
/// (U+00A4), decimal '.', grouping ',' by threes, CurrencyPositivePattern 0 (<c>¤n</c>) and
/// CurrencyNegativePattern 0 (<c>(¤n)</c> — the PARENTHESES are the negative convention; negative_sign unused).
/// Every expected string is hand-derived from the cited rule, never from the implementation.
/// </summary>
public sealed class CobolLocaleEditTests
{
    // ── Format — §13.18.40.5 r14/r15 over the invariant conventions ────────────────────────────────────────────

    [Theory]
    // r14 a) — larger item: right-justified, space fill left. Suppressed zeros sit BETWEEN the currency
    // string and the first significant digit (nothing in format 2 floats cs rightward).
    [InlineData(123450, 2, "+$ZZZZZZ9.99", 20, "      +¤    1,234.50")]
    // r14 exact fit.
    [InlineData(123450, 2, "+$ZZZZZZ9.99", 14, "+¤    1,234.50")]
    // GR18 '+' absent — the item is UNSIGNED: a negative value edits as its absolute value, no exception.
    [InlineData(-1500, 2, "$Z9.99", 8, "  ¤15.00")]
    // r15 b) — every digit position Z and the value zero: ALL item positions are spaces (no separator, no
    // currency string, no sign).
    [InlineData(0, 2, "$ZZZZ.ZZ", 10, "          ")]
    // r15 a) — suppression stops at the first position with no suppression specified (the '9').
    [InlineData(0, 2, "$ZZZ9.99", 10, " ¤    0.00")]
    // A grouping separator inside the suppressed run becomes a space too (r15 a "any character position").
    [InlineData(23400, 2, "$ZZZZZZ9.99", 13, "¤      234.00")]
    // No '.' in the picture: no decimal separator at all (GR18 '.' is the only source of one).
    [InlineData(1234567, 0, "$ZZZZZZ9", 12, "  ¤1,234,567")]
    // The fraction width is the PICTURE's (three digits), never the locale's frac_digits (two) — §13.18.40.5
    // r12 hands LC_MONETARY only the separators and group sizes.
    [InlineData(1234895, 3, "$9999.999", 12, "  ¤1,234.895")]
    public void Format_InvariantLocale_EditsPerTheRules(long unscaled, int scale, string picture, int size, string expected)
    {
        Assert.Equal(expected, CobolLocaleEdit.Format(unscaled, scale, picture, "", size));
    }

    [Fact]
    public void Format_Negative_UsesTheParenthesesConvention()
    {
        // The invariant CurrencyNegativePattern is 0: the DEFAULT rendering of a negative is PARENTHESIZED,
        // negative_sign unused (§13.18.40.5 r13 + the derived n_sign_posn 0). A leading '-' would be wrong.
        Assert.Equal("     (¤    1,234.50)", CobolLocaleEdit.Format(-123450, 2, "+$ZZZZZZ9.99", "", 20));
    }

    [Fact]
    public void Format_BlankWhenZero_TakesPrecedenceOverLocaleEditing()
    {
        // §13.18.40.5 r10 — no locale is even consulted, so this holds for an unavailable locale too.
        Assert.Equal(new string(' ', 8), CobolLocaleEdit.Format(0, 0, "+$ZZZ9", "", 8, blankWhenZero: true));
        Assert.Equal(new string(' ', 8), CobolLocaleEdit.Format(0, 0, "+$ZZZ9", "zz-ZZ-nowhere", 8, blankWhenZero: true));
    }

    [Fact]
    public void Format_LengthIsAlwaysSize_OnEveryPath()
    {
        // Invariant I2 — §13.18.40.4 GR17: the item's character positions are integer-1, on every path
        // including both short-circuits and the truncation arm.
        foreach ((long v, int sc, string pic, int size) in new[]
        {
            (123450L, 2, "+$ZZZZZZ9.99", 30), (0L, 2, "$ZZZZ.ZZ", 7), (0L, 0, "$9", 3),
            (-987654321L, 2, "+$ZZZZZZ9.99", 9), (5L, 2, "ZZZZZZ9.99", 4),
        })
            Assert.Equal(size, CobolLocaleEdit.Format(v, sc, pic, "", size).Length);
    }

    [Fact]
    public void Format_StageOneAlignment_TruncatesSilently_NeverRaises()
    {
        // §13.18.40.5 r14 sentence 1: alignment truncation on either end is ORDINARY MOVE alignment — the
        // exception belongs only to the hypothetical→item move. 12345.678 into 9.99: high-order digits and the
        // low-order fraction digit go silently.
        ExceptionState.LocaleSizeChecking = true;
        try
        {
            Assert.Equal("¤5.67", CobolLocaleEdit.Format(12345678, 3, "$9.99", "", 5));
        }
        finally { ExceptionState.LocaleSizeChecking = false; }
    }

    // ── EC-LOCALE-SIZE — §13.18.40.5 r14 b), the condition's FIRST raise site ──────────────────────────────────

    [Fact]
    public void LocaleSize_SilentArm_TruncatingOnlySuppressedSpacesAndZeros()
    {
        // "ZZZZZZ9.99" value 1234.50 → hypothetical "    1,234.50" (12): a SIZE of 8 truncates exactly the four
        // suppressed-zero spaces — "neither a zero nor a space caused by a suppressed zero" is false for every
        // truncated character, so NO exception even with checking on.
        ExceptionState.LocaleSizeChecking = true;
        try
        {
            Assert.Equal("1,234.50", CobolLocaleEdit.Format(123450, 2, "ZZZZZZ9.99", "", 8));
            // Truncating a literal zero digit is equally silent: "9999999.99" value 1234.50 → "0,001,234.50";
            // SIZE 10 cuts "0," — a zero and a grouping separator... the separator is NOT exempt, so this one
            // RAISES; the genuinely silent literal-zero case cuts only the '0': SIZE 11.
            Assert.Equal(",001,234.50", CobolLocaleEdit.Format(123450, 2, "9999999.99", "", 11));
        }
        finally { ExceptionState.LocaleSizeChecking = false; }
    }

    [Fact]
    public void LocaleSize_RaisingArm_IsCharacterBased()
    {
        ExceptionState.LocaleSizeChecking = true;
        try
        {
            // A truncated NONZERO DIGIT raises...
            var ex1 = Assert.Throws<CobolFatalException>(() => CobolLocaleEdit.Format(123450, 2, "ZZZZZZ9.99", "", 7));
            Assert.Contains("EC-LOCALE-SIZE", ex1.Message);
            // ...and so does a truncated CURRENCY character or GROUPING SEPARATOR — r14 b) is character-based,
            // NOT Table 13's narrower "digits were truncated" gloss.
            Assert.Throws<CobolFatalException>(() => CobolLocaleEdit.Format(123450, 2, "$ZZZZZZ9.99", "", 12));
            Assert.Throws<CobolFatalException>(() => CobolLocaleEdit.Format(123450, 2, "9999999.99", "", 10));
        }
        finally { ExceptionState.LocaleSizeChecking = false; }
    }

    [Fact]
    public void LocaleSize_CheckingOff_StoresTheTruncatedRemainder()
    {
        // r14 b)'s own text stores the truncated result; with checking off execution continues with it.
        Assert.Equal("234.50", CobolLocaleEdit.Format(123450, 2, "ZZZZZZ9.99", "", 6));
    }

    // ── DeEdit — §14.9.25.4 GR5/GR6 d, §14.6.13.2 r4 ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData(123450, 2, "+$ZZZZZZ9.99", 20)]
    [InlineData(-123450, 2, "+$ZZZZZZ9.99", 20)]
    [InlineData(0, 2, "$ZZZZ.ZZ", 10)]
    [InlineData(-1500, 2, "$Z9.99", 8)]           // unsigned picture: de-edits to +15.00 (I4 — the sign is GONE)
    [InlineData(1234567, 0, "$ZZZZZZ9", 12)]
    [InlineData(90210, 2, "ZZZ9.99", 9)]
    public void DeEdit_InvertsFormat_InTheNoTruncationRegime(long unscaled, int scale, string picture, int size)
    {
        string edited = CobolLocaleEdit.Format(unscaled, scale, picture, "", size);
        Int128 back = CobolLocaleEdit.DeEdit(edited, picture, "");
        // The round trip returns the value at the PICTURE's scale; an unsigned picture loses a negative sign
        // permanently (§13.18.40.4 GR18 '+' — invariant I4: never a negative de-edit).
        Int128 expected = picture.Contains('+') ? unscaled : Math.Abs(unscaled);
        // Rescale the expectation from the sender's scale to the picture's (digits right of '.').
        int picScale = picture.Contains('.') ? picture.Length - picture.IndexOf('.') - 1 : 0;
        for (; scale < picScale; scale++) expected *= 10;
        for (; scale > picScale; scale--) expected /= 10;
        Assert.Equal(expected, back);
    }

    [Fact]
    public void DeEdit_AllSpaces_IsZeroOnlyWhereEditingCouldHaveWrittenIt()
    {
        // r15 b) makes all-spaces a possible result for an all-Z picture; §13.18.8.4 GR1 for BLANK WHEN ZERO.
        Assert.Equal((Int128)0, CobolLocaleEdit.DeEdit("        ", "$ZZZZ.ZZ", ""));
        Assert.Equal((Int128)0, CobolLocaleEdit.DeEdit("        ", "$ZZZ9.99", "", blankWhenZero: true));
        // For ZZZ9.99 without BLANK WHEN ZERO, all-spaces is NOT a possible result — §14.6.13.2 r4.
        ExceptionState.DataIncompatibleChecking = true;
        try
        {
            var ex = Assert.Throws<CobolFatalException>(() => CobolLocaleEdit.DeEdit("       ", "ZZZ9.99", ""));
            Assert.Contains("EC-DATA-INCOMPATIBLE", ex.Message);
        }
        finally { ExceptionState.DataIncompatibleChecking = false; }
    }

    [Fact]
    public void DeEdit_ContentNoEditCouldProduce_IsIncompatibleData()
    {
        ExceptionState.DataIncompatibleChecking = true;
        try
        {
            // A minus sign on an item whose picture has no '+' — Format can never have written one.
            Assert.Throws<CobolFatalException>(() => CobolLocaleEdit.DeEdit("  -15.00", "$Z9.99", ""));
            // Arbitrary text (the ref-mod escape hatch).
            Assert.Throws<CobolFatalException>(() => CobolLocaleEdit.DeEdit("HELLO !!", "$Z9.99", ""));
            // A missing MANDATORY currency string (GR18 cs is unconditional in every edited result).
            Assert.Throws<CobolFatalException>(() => CobolLocaleEdit.DeEdit("   15.00", "$Z9.99", ""));
            // The wrong fraction width for the picture.
            Assert.Throws<CobolFatalException>(() => CobolLocaleEdit.DeEdit(" ¤15.0", "$Z9.99", ""));
        }
        finally { ExceptionState.DataIncompatibleChecking = false; }
    }

    [Fact]
    public void DeEdit_AcceptsALeftTruncatedIntegerPart()
    {
        // r14 b) may have truncated the leftmost positions: fewer integer digits than digitsLeft is a possible
        // result and de-edits to the digits present.
        Assert.Equal((Int128)123450, CobolLocaleEdit.DeEdit("1,234.50", "ZZZZZZ9.99", ""));
    }

    [Fact]
    public void Format_NeverLeaksThePicturePeriod()
    {
        // Invariant I1 — the picture's '.' is an alignment symbol; the OUTPUT separator is the locale's
        // (§13.18.40.4 GR18 '.', §12.3.7.4 GR14 NOTE 3). Under the invariant locale they coincide, so pin it
        // with a locale whose decimal separator differs: fr-FR uses ','.
        string s = CobolLocaleEdit.Format(123450, 2, "ZZZZZZ9.99", "fr-FR", 14);
        Assert.DoesNotContain(".", s);
        Assert.Contains(",", s);   // fr-FR mon_decimal_point
    }
}
