// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime.Collation;
using WiseOwl.Cobol.Runtime.Collation.Cldr;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Collation;

/// <summary>
/// The CLDR collation settings and relations kb/Work PB107 added to the engine, each against UTS #35 Part 5: the
/// QUATERNARY relation <c>&lt;&lt;&lt;&lt;</c> (a level-4 weight line; Japanese <c>private-kana</c>, where Hiragana
/// precedes Katakana) and the CASE LEVEL (<c>[caseLevel on]</c>, <c>-u-kc-true</c>: "a level consisting only of case
/// characteristics will be inserted in front of tertiary level"). Compare and GetKey must agree under every setting.
/// </summary>
public sealed class CldrCollationSettingsTests
{
    private static Collator Ja(CollationStrength? strength = null, AlternateHandling? alternate = null) =>
        CollationEngine.ForLocale("ja", strength, alternate);

    // ---- the quaternary relation ----------------------------------------------------------------------------------

    [Fact]
    public void QuaternaryRelations_AreApplied_NotReportedAsUnsupported()
    {
        var resolved = CollationEngine.ResolveLocale("ja");
        Assert.Empty(resolved.Unsupported);
        Assert.True(resolved.Table.HasQuaternary);
        Assert.False(CollationTable.Root.HasQuaternary);
        Assert.False(CollationEngine.ResolveLocale("es").Table.HasQuaternary);
        Assert.False(CollationEngine.ResolveLocale("da").Table.HasQuaternary);
    }

    /// <summary>ja.xml (private-kana) `&amp;ぁ&lt;&lt;&lt;&lt;ァ`: katakana small a is the hiragana's with a new level-4
    /// slot after it. Through level 3 they are equal (ICU gives the same); at level 4 the hiragana precedes (JIS X 4061
    /// 4.4.10.2 (3): "Hiragana &lt; Katakana"). `&amp;ゝ&lt;&lt;&lt;&lt;ヽ` is the same for the iteration marks.</summary>
    [Theory]
    [InlineData("ぁ", "ァ")]
    [InlineData("か", "カ")]
    [InlineData("が", "ガ")]
    [InlineData("ゝ", "ヽ")]
    [InlineData("あー", "アー")]
    [InlineData("ぁー", "ァー")]     // a `ァ|ー` prefix context: ァ's elements are its final ones, not those of the rule's moment
    public void HiraganaPrecedesKatakana_OnlyAtLevel4(string hiragana, string katakana)
    {
        Assert.Equal(0, Ja().Compare(hiragana, katakana));
        Assert.Equal(0, Ja(CollationStrength.Tertiary).Compare(hiragana, katakana));
        var level4 = Ja(CollationStrength.Quaternary);
        Assert.True(level4.Compare(hiragana, katakana) < 0, $"{hiragana} vs {katakana}");
        Assert.True(level4.Compare(katakana, hiragana) > 0);
        Assert.True(Ja(CollationStrength.Identical).Compare(hiragana, katakana) < 0);
    }

    [Fact]
    public void Level4_DoesNotOverrideLevels1To3()
    {
        var level4 = Ja(CollationStrength.Quaternary);
        Assert.True(level4.Compare("カ", "き") < 0);     // primary decides before the quaternary slot
        Assert.True(level4.Compare("ガ", "か") > 0);     // voiced > voiceless at level 3, whatever the scripts
        Assert.True(level4.Compare("ぁ", "あ") < 0);     // small < large at level 3
    }

    [Fact]
    public void Level4_TogetherWithShifted_KeepsBothTheVariableAndTheQuaternaryWeights()
    {
        var shifted = Ja(CollationStrength.Quaternary, AlternateHandling.Shifted);
        Assert.True(shifted.Compare("ぁ", "ァ") < 0);                 // the slot refines "no variable here"
        Assert.NotEqual(0, shifted.Compare("か-か", "かか"));         // a variable still weighs at level 4
        Assert.True(shifted.Compare("かカ", "かか") > 0);
    }

    [Fact]
    public void Keys_AgreeWithCompare_AtEveryStrength_OverTheKana()
    {
        string[] corpus = ["ぁ", "ァ", "あ", "ア", "か", "カ", "が", "ガ", "ゝ", "ヽ", "ぁー", "ァー", "あー", "アー", "かー", "カー", "a", "一", "", "ー", "かカ", "カか"];
        foreach (var strength in new[] { CollationStrength.Primary, CollationStrength.Secondary, CollationStrength.Tertiary, CollationStrength.Quaternary, CollationStrength.Identical })
            foreach (var alternate in new[] { AlternateHandling.NonIgnorable, AlternateHandling.Shifted })
            {
                var c = Ja(strength, alternate);
                foreach (string x in corpus)
                    foreach (string y in corpus)
                        Assert.Equal(Math.Sign(c.Compare(x, y)), Math.Sign(c.GetKey(x).CompareTo(c.GetKey(y))));
            }
    }

    [Fact]
    public void QuaternaryKey_CarriesTheSlot_OnlyForTheTailoredString()
    {
        var level4 = Ja(CollationStrength.Quaternary);
        Assert.Empty(level4.GetKey("ぁ").Quaternary);
        Assert.NotEmpty(level4.GetKey("ァ").Quaternary);
        Assert.Equal(4, level4.GetKey("ァ").LevelCount);
        Assert.Equal(3, Ja().GetKey("ァ").LevelCount);          // tertiary strength: no fourth level
    }

    // ---- the case level --------------------------------------------------------------------------------------------

    private static Collator Root(CollationStrength strength, CaseFirst caseFirst = CaseFirst.Off) =>
        Collator.Root.With(strength: strength, caseLevel: true, caseFirst: caseFirst);

    /// <summary>"To ignore accents but take cases into account, set strength to primary and case level to on."</summary>
    [Fact]
    public void CaseLevel_AtPrimaryStrength_IgnoresAccents_ButNotCase()
    {
        var c = Root(CollationStrength.Primary);
        Assert.Equal(0, c.Compare("a", "á"));
        Assert.Equal(0, c.Compare("résumé", "resume"));
        Assert.True(c.Compare("a", "A") < 0);             // lowercase before uppercase
        Assert.True(c.Compare("á", "A") < 0);
        Assert.True(c.Compare("A", "á") > 0);
        Assert.True(c.Compare("ab", "aB") < 0);
        Assert.True(c.Compare("aB", "Ab") < 0);           // the first case difference decides
        Assert.True(c.Compare("Z", "a") > 0);             // …but a primary difference always comes first
        Assert.True(c.Compare("A", "b") < 0);
    }

    /// <summary>The case level sits between level 2 and level 3: a secondary difference decides before it.</summary>
    [Fact]
    public void CaseLevel_IsAfterSecondary_BeforeTertiary()
    {
        var c = Root(CollationStrength.Secondary);
        Assert.True(c.Compare("á", "A") > 0);             // the accent (level 2) outranks the case (case level)
        Assert.True(c.Compare("a", "A") < 0);
        var t = Root(CollationStrength.Tertiary);
        Assert.True(t.Compare("a", "ａ") < 0);             // equal case: the tertiary width weight decides
        Assert.True(t.Compare("A", "ａ") > 0);             // case first: uppercase after every lowercase variant
        Assert.True(Collator.Root.Compare("A", "ａ") > 0); // (without it the tertiary weights alone decide: 0008 for A, 0003 for the fullwidth a)
    }

    [Fact]
    public void CaseLevel_WithCaseFirstUpper_PutsUppercaseFirst()
    {
        var c = Root(CollationStrength.Primary, CaseFirst.Upper);
        Assert.True(c.Compare("A", "a") < 0);
        Assert.True(c.Compare("Ab", "ab") < 0);
        Assert.True(c.Compare("aB", "Ab") > 0);
        var lower = Root(CollationStrength.Primary, CaseFirst.Lower);
        Assert.True(lower.Compare("a", "A") < 0);
    }

    [Fact]
    public void CaseLevel_Off_ChangesNothing()
    {
        Assert.Equal(CollationOptions.Default, Collator.Root.With(caseLevel: false).Options);
        Assert.Equal(0, Collator.Root.With(strength: CollationStrength.Primary).Compare("a", "A"));
        Assert.True(Collator.Root.Compare("a", "A") < 0);
    }

    [Fact]
    public void CaseLevel_Keys_AgreeWithCompare_AndCarryTheLevel()
    {
        string[] corpus = ["a", "A", "á", "Á", "ab", "aB", "Ab", "AB", "áb", "b", "ß", "ss", "Ss", "SS", "æ", "Æ", "ａ", "Ａ", "é", "É", "a-b", "A-B", "", "ǅ", "ǆ"];
        foreach (var strength in new[] { CollationStrength.Primary, CollationStrength.Secondary, CollationStrength.Tertiary, CollationStrength.Quaternary, CollationStrength.Identical })
            foreach (var caseFirst in new[] { CaseFirst.Off, CaseFirst.Upper, CaseFirst.Lower })
                foreach (var alternate in new[] { AlternateHandling.NonIgnorable, AlternateHandling.Shifted })
                {
                    var c = Root(strength, caseFirst).With(alternate: alternate);
                    foreach (string x in corpus)
                        foreach (string y in corpus)
                            Assert.Equal(Math.Sign(c.Compare(x, y)), Math.Sign(c.GetKey(x).CompareTo(c.GetKey(y))));
                }
        var key = Root(CollationStrength.Tertiary).GetKey("aB");
        Assert.Equal(2, key.CaseLevel.Count);
        Assert.True(key.CaseLevel[0] < key.CaseLevel[1]);           // lower, then upper
        Assert.Equal(3, key.LevelCount);                            // the case level is not one of the four weight levels
        Assert.Equal(2, key.Tertiary.Count);
        Assert.Empty(Collator.Root.GetKey("aB").CaseLevel);
        var bytes = key.ToByteArray();
        Assert.Equal(4 * 3 + (2 + 2 + 2 + 2) * 3, bytes.Length);     // four levels' terminators + 8 weights
    }

    [Fact]
    public void CaseLevel_ComesFromTheTagAndFromTheRules_AndIsNoLongerReportedUnsupported()
    {
        var tag = CollationEngine.ResolveLocale("en-u-ks-level1-kc-true");
        Assert.True(tag.Options.CaseLevel);
        Assert.Equal(CollationStrength.Primary, tag.Options.Strength);
        Assert.Empty(tag.Unsupported);
        var c = tag.Collator;
        Assert.Equal(0, c.Compare("é", "e"));
        Assert.True(c.Compare("e", "E") < 0);
        // cu.xml (Church Slavic, UTN #41) states `[caseLevel on]` with `[caseFirst upper]`.
        var cu = CollationEngine.ResolveLocale("cu");
        Assert.True(cu.Options.CaseLevel);
        Assert.Equal(CaseFirst.Upper, cu.Options.CaseFirst);
        Assert.Empty(cu.Unsupported);
        Assert.False(CollationEngine.ResolveLocale("en").Options.CaseLevel);
        Assert.NotSame(CollationEngine.For(CollationTable.Root, CollationOptions.Default), CollationEngine.For(CollationTable.Root, CollationOptions.Default with { CaseLevel = true }));
    }
}
