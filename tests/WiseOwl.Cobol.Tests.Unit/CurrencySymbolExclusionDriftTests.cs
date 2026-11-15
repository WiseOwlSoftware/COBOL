// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Binding;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EACH EDITION'S FORBIDDEN CURRENCY-SYMBOL SET IS THE LIST ITS RULE PRINTS (kb/Work PB5173). The compiler held ONE
/// list, the 2023 one, at every edition, so COBOL-85 refused <c>CURRENCY SIGN IS "E"</c> and accepted <c>"m"</c>.
/// <see cref="CurrencySymbolExclusion"/> is the one home of the rows; this test reads each row's letters and special
/// characters back out of the rule's own words, quoted below exactly as the transcription prints them (each quotation
/// checked with <c>scripts/spec/cite.py --check</c>), so a row cannot drift from its rule. The quotations live here, not
/// in a read of the Markdown transcription, because no compiler test opens a Markdown file (kb/Work PB4453).
/// </summary>
public sealed class CurrencySymbolExclusionDriftTests
{
    /// <summary>§1985:VI.4.5.4 GR11 b. and c.</summary>
    private const string Cobol85Letters =
        "b. alphabetic characters consisting of the uppercase letters A, B, C, D, P, R, S, V, X, Z; the lowercase letters a through z; or the space;";
    private const string Cobol85Specials = "c. special characters * + - , . ; ( ) \" = /";

    /// <summary>§2023:12.3.7.3 SR22 b) and c) (the same list as §2002:12.2.6.2 SR19).</summary>
    private const string Cobol2002Letters =
        "b) alphabetic characters A, B, C, D, E, N, P, R, S, V, X, Z, or their lowercase equivalents; or the space;";
    private const string Cobol2002Specials = "c) characters '+' '-' ',' '.' '*' '/' ';' '(' ')' '\"' '='";

    private static string Letters(string rule) =>
        string.Concat(Regex.Match(rule, @"((?:[A-Z], )+[A-Z])[;,]").Groups[1].Value.Where(char.IsAsciiLetterUpper));

    [Fact]
    public void TheCobol85Row_IsGeneralRule11()
    {
        Assert.Equal(Letters(Cobol85Letters), CurrencySymbolExclusion.Cobol85.UppercaseLetters);
        Assert.Equal(Cobol85Letters.Contains("the lowercase letters a through z"), CurrencySymbolExclusion.Cobol85.EveryLowercaseLetter);
        Assert.Equal(Set(" " + Cobol85Specials["c. special characters ".Length..].Replace(" ", "")),
            Set(CurrencySymbolExclusion.SpecialCharacters));
    }

    [Fact]
    public void TheCobol2002Row_IsSyntaxRule22()
    {
        Assert.Equal(Letters(Cobol2002Letters), CurrencySymbolExclusion.Cobol2002.UppercaseLetters);
        Assert.Equal(Cobol2002Letters.Contains("lowercase letters a through z"), CurrencySymbolExclusion.Cobol2002.EveryLowercaseLetter);
        string listed = string.Concat(Regex.Matches(Cobol2002Specials, "'(.)'").Select(q => q.Groups[1].Value));
        Assert.Equal(Set(" " + listed), Set(CurrencySymbolExclusion.SpecialCharacters));
    }

    [Theory] // the rows as the binder asks them: below 2002 the 1985 list, from 2002 the 2002 list
    [InlineData(85, 'E', false)]
    [InlineData(85, 'N', false)]
    [InlineData(85, 'L', false)]
    [InlineData(85, 'm', true)]
    [InlineData(85, 'A', true)]
    [InlineData(85, '7', true)]
    [InlineData(2002, 'E', true)]
    [InlineData(2002, 'n', true)]
    [InlineData(2002, 'm', false)]
    [InlineData(2023, 'L', false)]
    [InlineData(2023, '=', true)]
    public void ForTheEdition_ExcludesTheRuleCharacters(int year, char c, bool excluded) =>
        Assert.Equal(excluded, CurrencySymbolExclusion.For(year).Excludes(c));

    private static SortedSet<char> Set(string chars) => new(chars);
}
