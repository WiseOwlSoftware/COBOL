// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EACH EDITION'S FORBIDDEN CURRENCY-SYMBOL SET IS THE LIST ITS RULE PRINTS (kb/Work PB5173). The compiler held ONE
/// list, the 2023 one, at every edition, so COBOL-85 refused <c>CURRENCY SIGN IS "E"</c> and accepted <c>"m"</c>.
/// <see cref="CurrencySymbolExclusion"/> is the one home of the rows; this test re-reads each row's letters and special
/// characters from the transcription of the edition that states them, so a row cannot drift from its rule.
/// </summary>
public sealed class CurrencySymbolExclusionDriftTests
{
    private static string Spec(string file) => File.ReadAllText(TestRepo.Specs(file));

    private static string Letters(string text, string pattern)
    {
        var m = Regex.Match(text, pattern);
        Assert.True(m.Success, $"the rule's letter list was not found by /{pattern}/ — the transcription is what changed");
        return string.Concat(m.Groups[1].Value.Where(char.IsAsciiLetterUpper));
    }

    [Fact] // §1985:VI.4.5.4 GR11 b. and c.
    public void TheCobol85Row_IsGeneralRule11()
    {
        string text = Spec("ANSI_COBOL_1985.md");
        Assert.Equal(Letters(text, @"uppercase letters ((?:[A-Z], )+[A-Z]); the lowercase letters a through z; or the space"),
            CurrencySymbolExclusion.Cobol85.UppercaseLetters);
        Assert.True(CurrencySymbolExclusion.Cobol85.EveryLowercaseLetter);
        var specials = Regex.Match(text, @"c\. special characters (.+)");
        Assert.True(specials.Success, "§1985:VI.4.5.4 GR11 c. was not found");
        Assert.Equal(Set(" " + specials.Groups[1].Value.Replace("\\", "").Replace(" ", "")),
            Set(CurrencySymbolExclusion.SpecialCharacters));
    }

    [Fact] // §2023:12.3.7.3 SR22 b) and c)
    public void TheCobol2002Row_IsSyntaxRule22()
    {
        string text = Spec("ISO_COBOL_2023.md");
        Assert.Equal(Letters(text, @"alphabetic characters ((?:[A-Z], )+[A-Z]), or their lowercase equivalents; or the space"),
            CurrencySymbolExclusion.Cobol2002.UppercaseLetters);
        Assert.False(CurrencySymbolExclusion.Cobol2002.EveryLowercaseLetter);
        var specials = Regex.Match(text, @"c\) characters ((?:'\\?.' ?)+)");
        Assert.True(specials.Success, "§2023:12.3.7.3 SR22 c) was not found");
        string listed = string.Concat(Regex.Matches(specials.Groups[1].Value, @"'\\?(.)'").Select(q => q.Groups[1].Value));
        Assert.Equal(Set(" " + listed), Set(CurrencySymbolExclusion.SpecialCharacters));
    }

    [Theory] // the rows as the binder asks them: below 2002 the 1985 list, from 2002 the 2002 list
    [InlineData(85, 'E', false)]
    [InlineData(85, 'N', false)]
    [InlineData(85, 'L', false)]
    [InlineData(85, 'm', true)]
    [InlineData(85, 'A', true)]
    [InlineData(2002, 'E', true)]
    [InlineData(2002, 'n', true)]
    [InlineData(2002, 'm', false)]
    [InlineData(2023, 'L', false)]
    [InlineData(2023, '=', true)]
    [InlineData(85, '7', true)]
    public void ForTheEdition_ExcludesTheRuleCharacters(int year, char c, bool excluded) =>
        Assert.Equal(excluded, CurrencySymbolExclusion.For(year).Excludes(c));

    private static SortedSet<char> Set(string chars) => new(chars);
}
