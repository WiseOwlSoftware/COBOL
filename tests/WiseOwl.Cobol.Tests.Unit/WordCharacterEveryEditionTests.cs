// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Common;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ISO §8.3.2.1 and §8.1.3.2 GR4 over Annex B (kb/Work PB4130): a character that NO edition's Annex B holds is no
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
        foreach (int level in new[] { 1985, 2002, 2014, 2023 })
        {
            string? message = CobolWordRule.CharacterViolation(word, level);
            Assert.NotNull(message);
            Assert.Contains(character, message);
        }
        Assert.Contains("in no edition's Annex B", CobolWordRule.CharacterViolation(word, 1985));
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

    /// <summary>ISO §8.3.2.1 (kb/Work PB4234): "The hyphen or underscore shall not appear as the first or last character in such
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
        foreach (int level in new[] { 1985, 2002, 2014, 2023 })
        {
            string? message = CobolWordRule.CharacterViolation(word, level);
            Assert.NotNull(message);
            Assert.Contains($"as its {position} character", message);
            Assert.Contains("§8.3.2.1", message);
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
        Assert.Null(CobolCharacterRepertoire.SeparatorPositionViolation(word));
        Assert.Null(CobolWordRule.CharacterViolation(word, 2023));
        Assert.True(CobolCharacterRepertoire.IsWordShape(word));
    }

    [Theory]
    [InlineData("CAFÉ")]
    [InlineData("STRASSE-ß")]
    [InlineData("A\U0001E900")]
    public void AnAnnexBLetter_IsNoCharacterViolationBelow2002(string word) =>
        Assert.Null(CobolWordRule.CharacterViolation(word, 1985));
}
