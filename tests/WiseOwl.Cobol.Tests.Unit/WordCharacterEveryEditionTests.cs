// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Common;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ISO §2023:8.3.2.1 and §2023:8.1.3.2 GR4 over Annex B (kb/Work PB4130): a character that NO edition's Annex B holds is no
/// extended letter, so it is refused at every target, COBOL-85 included, as a word-character violation (COBOL2773) that
/// says what the character is. A letter some edition's Annex B holds is left to the construct gate below 2002 and to
/// the edition's own table from 2002.
/// </summary>
public sealed class WordCharacterEveryEditionTests
{
    [Theory]
    [InlineData("PREIS€", "U+20AC")]
    [InlineData("PREIS ", "U+00A0")]
    [InlineData("CAFÉ- X", "U+00A0")]
    public void ACharacterInNoAnnexB_IsRefusedAtEveryTarget_NamingTheCharacter(string word, string character)
    {
        foreach (int level in new[] { 85, 2002, 2014, 2023 })
        {
            string? message = CobolWordRule.CharacterViolation(word, level);
            Assert.NotNull(message);
            Assert.Contains(character, message);
        }
        Assert.Contains("in no edition's Annex B", CobolWordRule.CharacterViolation(word, 85));
    }

    /// <summary>The extended-letter question is per CHARACTER (kb/Work PB4156): a word holds one when ANY character is in some
    /// edition's Annex B, whatever else it holds; a word of basic characters and characters in no Annex B holds none.</summary>
    [Theory]
    [InlineData("CAFÉ", true)]
    [InlineData("CAFÉ€", true)]
    [InlineData("€CAFÉ", true)]
    [InlineData("A\U0001E900 ", true)]
    [InlineData("PREIS€", false)]
    [InlineData("PREIS ", false)]
    [InlineData("PREIS-1_X", false)]
    public void HasExtendedLetter_IsAskedOfEachCharacter(string word, bool expected) =>
        Assert.Equal(expected, CobolCharacterRepertoire.HasExtendedLetter(word));

    /// <summary>ISO §2023:8.3.2.1 (kb/Work PB4234): "The hyphen or underscore shall not appear as the first or last character in such
    /// words." The rule is edition-free and the one place that states it is <see cref="CobolCharacterRepertoire.SeparatorPositionViolation"/>,
    /// which every consumer asks: the screen and the directive stages (<see cref="CobolWordRule.CharacterViolation"/>), the
    /// text-stage word readers (<see cref="CobolCharacterRepertoire.IsWordShape"/>) and the parse layer's word test.</summary>
    [Theory]
    [InlineData("_A", "first")]
    [InlineData("A_", "last")]
    [InlineData("-A", "first")]
    [InlineData("A-", "last")]
    [InlineData("_", "first")]
    [InlineData("A-B_", "last")]
    public void AHyphenOrUnderscoreAtAWordEdge_IsRefusedAtEveryTarget_NamingTheRule(string word, string position)
    {
        foreach (int level in new[] { 85, 2002, 2014, 2023 })
        {
            string? message = CobolWordRule.CharacterViolation(word, level);
            Assert.NotNull(message);
            Assert.Contains($"as its {position} character", message);
            // The rule is the edition's own (kb/Work PB4950): COBOL-85's word clause names no underscore at all.
            Assert.Contains(level < 2002 ? "§1985:IV.4.2.2.1" : "§2023:8.3.2.1", message);
            Assert.False(CobolWordRule.RemovedAt2023(word, level));
        }
        Assert.False(CobolCharacterRepertoire.IsWordShape(word));
        Assert.False(CobolCharacterRepertoire.IsWordAtSomeEdition(word));
    }

    [Theory]
    [InlineData("A_B")]
    [InlineData("MY_NAME-2")]
    [InlineData("1_A")]
    [InlineData("CAFÉ_X")]
    public void AnInnerHyphenOrUnderscore_IsNoPositionViolation(string word)
    {
        Assert.Null(CobolCharacterRepertoire.SeparatorPositionViolation(word, 2023));
        Assert.Null(CobolWordRule.CharacterViolation(word, 2023));
        Assert.True(CobolCharacterRepertoire.IsWordShape(word));
    }

    [Theory]
    [InlineData("CAFÉ")]
    [InlineData("STRASSE-ß")]
    [InlineData("A\U0001E900")]
    public void AnAnnexBLetter_IsNoCharacterViolationBelow2002(string word) =>
        Assert.Null(CobolWordRule.CharacterViolation(word, 85));

    /// <summary>kb/Work PB6175: each arm of the repertoire's judgment cites the rule of the edition judging the word, never
    /// COBOL-2023's at an earlier edition. U+1E900 (ADLAM, added at 2023) is in neither the 2002 Annex C nor the 2014 Annex B;
    /// U+00B5 (MICRO SIGN) is a special character of both, which may not begin a word there (GR4b) and may at 2023; U+30FB
    /// (KATAKANA MIDDLE DOT) is a B.3 item 3 character at 2023.</summary>
    [Theory]
    [InlineData("A\U0001E900B", 2002, "extended letters of Annex C (ISO §2002:8.3.1.1; §2002:8.1.2.1 GR4)")]
    [InlineData("A\U0001E900B", 2014, "extended letters of Annex B (ISO §2014:8.3.1.1; §2014:8.1.2.1 GR4)")]
    [InlineData("µA", 2002, "neither the first nor the last character of a word (COBOL-2002; ISO §2002:8.1.2.1 GR4b)")]
    [InlineData("µA", 2014, "may not be the first character of a word (COBOL-2014; ISO §2014:8.1.2.1 GR4b)")]
    [InlineData("・A", 2023, "which Annex B.3 item 3 permits only inside a word (COBOL-2023; ISO §2023:8.3.2.1)")]
    public void EachArm_CitesTheJudgingEditionsRule(string word, int level, string ending) =>
        Assert.EndsWith(ending, CobolCharacterRepertoire.Violation(word, level), StringComparison.Ordinal);
}
