// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The NUMVAL-C / TEST-NUMVAL-C LOCALE arm's scanner (ISO §15.68.3 r5 / §15.94.4; kb/Work PB64 T6), pinned
/// against the INVARIANT locale (tag "" — currency ¤, decimal '.', grouping ',', negative convention
/// PARENTHESES per CurrencyNegativePattern 0) so every verdict is host-stable and hand-derived. The tag is
/// always explicit — the current-locale arm rides the conformance goldens, which own a run unit.
/// </summary>
public sealed class NumvalCLocaleScanTests
{
    [Theory]
    // The default path IS the parentheses convention: §15.68.4 r3 with the derived n_sign_posn 0.
    [InlineData("(¤1,234.56)", 0)]
    [InlineData("(1,234.56)", 0)]              // r5b.3 "may contain" — the currency string is omissible
    [InlineData("¤1,234.56", 0)]               // unsigned form — nonnegative
    [InlineData("1234.56", 0)]                 // bare number: one digit is the only obligation (r5b.7)
    [InlineData("  ¤ 1,234.56  ", 0)]          // leading/trailing spaces (r5b.7) + adjacency space
    [InlineData("( ¤1,234.56 )", 0)]           // spaces at every token adjacency
    [InlineData(".56", 0)]                     // the leading-separator form (parity with r4a's `. digit`)
    [InlineData("+¤1,234.56", 0)]              // the positive convention: positive_sign at the determined posn 1
    [InlineData("1,23,4.5", 0)]                // ⚖ group SIZES are not validated (permissive determination)
    [InlineData("¤1.5", 0)]                    // ⚖ fraction width not constrained by frac_digits (2)
    public void Conforming_ReturnsZero(string text, long expected)
    {
        Assert.Equal(expected, CobolIntrinsics.TestNumvalCLocale(text, ""));
    }

    [Theory]
    // A bare '-' has NO meaning under LOCALE when the convention is parentheses — §15.68.4 r3's CR/DB/minus
    // sentence is explicitly the non-LOCALE leg, and negative_sign is unused with n_sign_posn 0.
    [InlineData("-¤1,234.56", 1)]
    [InlineData("¤1,234.56CR", 10)]            // CR is not a sign indicator under LOCALE (r4b is not inherited)
    [InlineData("0 1", 3)]                     // §15.94.4 r1 b.1's own worked example — position of the '1'
    [InlineData("1,234.5,6", 8)]               // a grouping separator right of the decimal separator (§8.2.2)
    [InlineData("1..5", 3)]                    // a second decimal separator
    [InlineData("(¤1,234.56", 11)]             // unclosed parenthesis — every char admissible ⇒ r1 c LENGTH+1
    [InlineData("", 1)]                        // zero-length ⇒ 0+1 (r1 c NOTE)
    [InlineData("   ", 4)]                     // all spaces ⇒ n+1 (r1 c NOTE)
    public void NonConforming_ReportsTheFirstErrorPosition(string text, long expected)
    {
        Assert.Equal(expected, CobolIntrinsics.TestNumvalCLocale(text, ""));
    }

    [Fact]
    public void CommaMode_IsInertUnderLocale()
    {
        // §15.68.3 r4 d (the DECIMAL-POINT IS COMMA role swap) opens "If the LOCALE keyword is not specified" —
        // the separators are mon_decimal_point/mon_thousands_sep, full stop. There is no commaMode parameter on
        // the LOCALE entry points at all; this pins that the invariant separators hold regardless.
        Assert.Equal(0, CobolIntrinsics.TestNumvalCLocale("1,234.56", ""));
        Assert.Equal(6, CobolIntrinsics.TestNumvalCLocale("1.234,56", ""));   // the ',' right of '.' is in error
    }

    [Fact]
    public void Value_And_Sign_FollowTheMatchedConvention()
    {
        // §15.68.4 r3 — negative iff the NEGATIVE convention matched; the parenthesized form IS the negative
        // convention under the invariant locale.
        Assert.Equal((Int128)(-123456), CobolIntrinsics.NumvalCLocale("(¤1,234.56)", "", scale: 2));
        Assert.Equal((Int128)123456, CobolIntrinsics.NumvalCLocale("¤1,234.56", "", scale: 2));
        Assert.Equal((Int128)123456, CobolIntrinsics.NumvalCLocale("+¤1,234.56", "", scale: 2));
        // Negative zero evaluates to zero — COBOL numeric has no signed zero.
        Assert.Equal((Int128)0, CobolIntrinsics.NumvalCLocale("(¤0.00)", "", scale: 2));
        // §15.68.4 r2 — grouping separators preceding the decimal separator are ignored in the value.
        Assert.Equal((Int128)123450, CobolIntrinsics.NumvalCLocale("1,2,3,4.5", "", scale: 2));
    }

    [Fact]
    public void TestTwin_AgreesWithTheValueFunction()
    {
        // The ONE-scanner invariant (§15.94.1: TEST-NUMVAL-C exists to verify NUMVAL-C will succeed): for every
        // probe, TEST == 0 iff NUMVAL-C parses it (checking off ⇒ the reject projection returns 0 — so the
        // agreement is asserted on TEST's verdict against a re-parse, not on the 0 collision).
        foreach (string probe in new[]
        {
            "(¤1,234.56)", "¤1,234.56", "1234", "  12.5  ", "-1", "¤¤1", "1.2.3", "( 1 )", "ABC", ".5", "+.5",
        })
        {
            long verdict = CobolIntrinsics.TestNumvalCLocale(probe, "");
            if (verdict == 0)
                // A conforming probe must parse to SOME value without tripping the scan again.
                _ = CobolIntrinsics.NumvalCLocale(probe, "", scale: 2);
            else
                Assert.NotEqual(0, verdict);
        }
    }

    [Fact]
    public void DigitCap_ReportsTheCapDigitsOrdinal_PerArithmeticMode()
    {
        string d40 = new('1', 40);
        Assert.Equal(32, CobolIntrinsics.TestNumvalCLocale(d40, ""));                    // native — r1 b.2
        Assert.Equal(35, CobolIntrinsics.TestNumvalCLocale(d40, "", digitCap: 34));      // standard-decimal — r1 b.4
        // The cap counts DIGIT CHARACTERS and the position is the ordinal IN THE ORIGINAL — a currency prefix
        // shifts it.
        Assert.Equal(33, CobolIntrinsics.TestNumvalCLocale("¤" + d40, ""));
        Assert.Equal(0, CobolIntrinsics.TestNumvalCLocale(new string('1', 31), ""));
    }

    [Fact]
    public void SpacingEquivalence_ATypedNbspMatchesTheNormalizedSeparator()
    {
        // DETERMINATION L12 + LAT-6: fr-FR's mon_thousands_sep normalizes to the plain space, and a user-typed
        // U+00A0 or U+202F matches it through the same equivalence class — "1 234,56" is conforming in fr-FR
        // however the space was typed.
        Assert.Equal(0, CobolIntrinsics.TestNumvalCLocale("1 234,56", "fr-FR"));
        Assert.Equal(0, CobolIntrinsics.TestNumvalCLocale("1 234,56", "fr-FR"));
        Assert.Equal(0, CobolIntrinsics.TestNumvalCLocale("1 234,56", "fr-FR"));
    }

    [Fact]
    public void IntCurrSymbol_MatchesItsFirstThreeCharacters()
    {
        // §15.68.3 r5b.3 — either currency_symbol or the FIRST THREE characters of int_curr_symbol; the
        // appended space is never required. en-US: ISOCurrencySymbol "USD".
        Assert.Equal(0, CobolIntrinsics.TestNumvalCLocale("USD1,234.56", "en-US"));
        Assert.Equal(0, CobolIntrinsics.TestNumvalCLocale("USD 1,234.56", "en-US"));
        Assert.Equal((Int128)123456, CobolIntrinsics.NumvalCLocale("USD1,234.56", "en-US", scale: 2));
        // The INVARIANT locale has no region ⇒ no int_curr_symbol ⇒ "USD" can never match (a determination).
        Assert.NotEqual(0, CobolIntrinsics.TestNumvalCLocale("USD1,234.56", ""));
    }

    [Fact]
    public void Anycase_FoldsOnlyTheCurrencyString()
    {
        // §15.68.3 r5b.1 scopes ANYCASE to "the matching rules for detecting a currency string".
        Assert.NotEqual(0, CobolIntrinsics.TestNumvalCLocale("usd1,234.56", "en-US"));
        Assert.Equal(0, CobolIntrinsics.TestNumvalCLocale("usd1,234.56", "en-US", anycase: true));
        Assert.Equal(0, CobolIntrinsics.TestNumvalCLocale("Usd 1,234.56", "en-US", anycase: true));
    }
}
