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

    /// <summary>Under non-ignorable handling the POSITION of a quaternary difference decides (ICU's compareUpToQuaternary
    /// gives every regular element a common level-4 weight): the first difference is hiragana on one side, katakana on the other.</summary>
    [Theory]
    [InlineData("かカ", "カか")]
    [InlineData("かカか", "カかか")]
    [InlineData("かかカ", "かカか")]
    public void Level4_NonIgnorable_OrdersByThePositionOfTheDifference(string x, string y)
    {
        var level4 = Ja(CollationStrength.Quaternary);
        Assert.True(level4.Compare(x, y) < 0, $"{x} vs {y}");
        Assert.True(level4.Compare(y, x) > 0);
        Assert.True(level4.GetKey(x).CompareTo(level4.GetKey(y)) < 0);
        Assert.Equal(0, Ja().Compare(x, y));                  // below quaternary strength they stay equal
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
        var small = level4.GetKey("ぁ").Quaternary;
        var katakana = level4.GetKey("ァ").Quaternary;
        Assert.Single(small);                                  // every element weighs the common level-4 weight ...
        Assert.Single(katakana);
        Assert.True(small[0] < katakana[0]);                   // ... and the tailored slot raises ァ's above ぁ's
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
        string[] corpus = ["a", "A", "á", "Á", "ab", "aB", "Ab", "AB", "áb", "b", "ß", "ss", "Ss", "SS", "æ", "Æ", "ａ", "Ａ", "e\u0301", "E\u0301", "a-b", "A-B", "", "ǅ", "ǆ"];
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

    // ---- numeric ordering ------------------------------------------------------------------------------------------

    private static readonly Collator Numeric = Collator.Root.With(numericOrdering: true);

    /// <summary>"If set to on, any sequence of Decimal Digits (General_Category = Nd) is sorted at a primary level with
    /// its numeric value. For example, "A-21" &lt; "A-123"."</summary>
    [Fact]
    public void NumericOrdering_SortsADigitRunByItsValue()
    {
        Assert.True(Collator.Root.Compare("A-21", "A-123") > 0);      // without it: '2' after '1'
        Assert.True(Numeric.Compare("A-21", "A-123") < 0);
        Assert.True(Numeric.Compare("a2", "a10") < 0);
        Assert.True(Numeric.Compare("a9", "a10") < 0);
        Assert.True(Numeric.Compare("a19", "a100") < 0);              // the shared prefix "a1" is no boundary inside a number
        Assert.True(Numeric.Compare("a1b", "a1c") < 0);               // after the number, the text goes on as usual
        Assert.True(Numeric.Compare("a12b", "a12") > 0);
        Assert.True(Numeric.Compare("a", "a0") < 0);
        Assert.True(Numeric.Compare("a0", "a1") < 0);
        Assert.True(Numeric.Compare("1", "a") < 0);                   // numbers stand where digits do: before letters
        Assert.True(Numeric.Compare("-", "1") < 0);                   // … and after punctuation
    }

    /// <summary>Leading zeros and the digit's script and width are not part of a number's value.</summary>
    [Fact]
    public void NumericOrdering_ValueIsAllThatCounts()
    {
        Assert.Equal(0, Numeric.Compare("a02", "a2"));
        Assert.Equal(0, Numeric.Compare("a00", "a0"));
        Assert.Equal(0, Numeric.Compare("a٢", "a2"));            // ARABIC-INDIC DIGIT TWO
        Assert.Equal(0, Numeric.Compare("a２０", "a20"));     // fullwidth 2 0
        Assert.Equal(0, Numeric.Compare("a\U0001D7D0", "a2"));        // MATHEMATICAL BOLD DIGIT TWO (a supplementary Nd)
        Assert.True(Numeric.Compare("a٢٠", "a3") > 0);      // 20 > 3 across scripts
        Assert.NotEqual(0, Collator.Root.Compare("a02", "a2"));
    }

    /// <summary>A mark after a digit ends the run (the digits are separate numbers on either side of it).</summary>
    [Fact]
    public void NumericOrdering_ARunEndsAtANonDigit()
    {
        Assert.True(Numeric.Compare("1\u030110", "1\u03019") > 0);    // 1 acute 10 vs 1 acute 9: the second number decides
        Assert.True(Numeric.Compare("1\u03012", "1\u03013") < 0);
        Assert.True(Numeric.Compare("12", "1\u03012") > 0);           // the number 12 against the number 1 (then an acute, then 2)
        Assert.True(Numeric.Compare("2", "10") < 0);
    }

    /// <summary>The standard words the order by "numeric value": a run longer than 254 digits is not split into
    /// 254-digit numbers (ICU splits it, and so orders a 255-digit number before a 254-digit one).</summary>
    [Fact]
    public void NumericOrdering_AVeryLongRunIsOneNumber()
    {
        string big = "1" + new string('0', 254), small = "2" + new string('0', 253);   // 10^254 (255 digits) and 2*10^253 (254 digits)
        Assert.True(Numeric.Compare(big, small) > 0);
        Assert.True(Numeric.Compare(new string('1', 600), new string('1', 599) + "2") < 0);     // the same length, so position by position
        Assert.True(Numeric.Compare(new string('1', 599) + "2", new string('1', 599)) > 0);
        Assert.Equal(0, Numeric.Compare(new string('0', 400) + "7", "7"));
    }

    [Fact]
    public void NumericOrdering_Keys_AgreeWithCompare_AtEveryStrength()
    {
        string[] corpus = ["a1", "a2", "a10", "a02", "a٢", "A1", "a1b", "a1B", "1", "10", "9", "0", "00", "", "x-1", "x-10", "x 10", "1\u03012", "12", "\U0001D7D0", "１"];
        foreach (var strength in new[] { CollationStrength.Primary, CollationStrength.Secondary, CollationStrength.Tertiary, CollationStrength.Quaternary, CollationStrength.Identical })
            foreach (var alternate in new[] { AlternateHandling.NonIgnorable, AlternateHandling.Shifted })
                foreach (bool caseLevel in new[] { false, true })
                {
                    var c = Collator.Root.With(strength: strength, alternate: alternate, caseLevel: caseLevel, numericOrdering: true);
                    foreach (string x in corpus)
                        foreach (string y in corpus)
                            Assert.Equal(Math.Sign(c.Compare(x, y)), Math.Sign(c.GetKey(x).CompareTo(c.GetKey(y))));
                }
    }

    /// <summary>Against the host's ICU (which .NET 10 reaches through <see cref="System.Globalization.CompareOptions.NumericOrdering"/>):
    /// a deterministic corpus of random strings over digits of several scripts, letters, a mark and punctuation.</summary>
    [Fact]
    public void NumericOrdering_AgreesWithTheHostIcu()
    {
        var icu = System.Globalization.CultureInfo.GetCultureInfo("en-US").CompareInfo;
        if (icu.Compare("a", "A", System.Globalization.CompareOptions.None) >= 0) return;   // globalization-invariant host: ordinal
        string[] alphabet = ["a", "b", "A", "0", "1", "2", "5", "9", "-", " ", "١", "٣", "１", "\u0301", "x", "10", "00", "007", "๑"];
        var rng = new Random(12345);
        string Make() { var sb = new System.Text.StringBuilder(); int n = rng.Next(0, 7); for (int i = 0; i < n; i++) sb.Append(alphabet[rng.Next(alphabet.Length)]); return sb.ToString(); }
        var disagreements = new List<string>();
        for (int k = 0; k < 20000; k++)
        {
            string x = Make(), y = Make();
            int mine = Math.Sign(Numeric.Compare(x, y)), theirs = Math.Sign(icu.Compare(x, y, System.Globalization.CompareOptions.NumericOrdering));
            if (mine != theirs) disagreements.Add($"[{x}] vs [{y}]: ours {mine}, ICU {theirs}");
        }
        Assert.True(disagreements.Count == 0, $"{disagreements.Count} disagreement(s):\n" + string.Join("\n", disagreements.Take(20)));
    }

    [Fact]
    public void NumericOrdering_ComesFromTheTag_AndThePrefixShapeCountsARunAsOneMapping()
    {
        var plain = CollationEngine.ResolveLocale("en");
        var tag = CollationEngine.ResolveLocale("en-u-kn-true");
        Assert.False(plain.Options.NumericOrdering);
        Assert.True(tag.Options.NumericOrdering);
        Assert.Empty(tag.Unsupported);
        Assert.True(plain.Collator.Compare("a2", "a10") > 0);
        Assert.True(tag.Collator.Compare("a2", "a10") < 0);
        // A digit run is ONE mapping: three digits are two characters of overhang; "000" weighs a lead and a count for three characters.
        var shape = tag.Collator.PrefixShapeOf("x007");
        Assert.Equal(2, shape.Overhang);
        Assert.Equal(0, shape.Deficit);
        Assert.Equal(1, tag.Collator.PrefixShapeOf("x000").Deficit);
        Assert.Equal(0, plain.Collator.PrefixShapeOf("x007").Overhang);
    }

    [Fact]
    public void TheTableKnowsItsDecimalDigits()
    {
        var t = CollationTable.Root;
        foreach (var (first, name) in new[] { (0x0030, "ASCII"), (0x0660, "Arabic-Indic"), (0x06F0, "Extended Arabic-Indic"), (0x0966, "Devanagari"), (0xFF10, "fullwidth"), (0x1D7CE, "math bold"), (0x1D7F6, "math monospace") })
            for (int v = 0; v < 10; v++)
            {
                Assert.True(t.TryGetDecimalDigit(first + v, out int value), $"{name} {v}");
                Assert.Equal(v, value);
            }
        foreach (int notDigit in new[] { 0x002F, 0x003A, 0x00B2, 0x00BD, 0x2460, 0x2080, 0x0041, 0x065F, 0x066A, 0xFF0F })
            Assert.False(t.TryGetDecimalDigit(notDigit, out _), $"U+{notDigit:X4}");
        // The table's runs ARE the UCD's Nd blocks: every General_Category=Nd line of the pinned UnicodeData.txt, with its
        // decimal digit value (field 6), and no other code point.
        int digits = 0;
        foreach (string line in File.ReadLines(Path.Combine(RepoRoot(), "data", "unicode", "UnicodeData.txt")))
        {
            string[] f = line.Split(';');
            if (f[2] != "Nd") continue;
            int cp = Convert.ToInt32(f[0], 16);
            Assert.True(t.TryGetDecimalDigit(cp, out int v), $"U+{cp:X4}");
            Assert.Equal(int.Parse(f[6]), v);
            digits++;
        }
        Assert.Equal(770, digits);        // 77 sets of ten (Unicode 17.0)
        for (int cp = 0; cp <= 0x1FFFF; cp++)
            if (t.TryGetDecimalDigit(cp, out _)) digits--;
        Assert.Equal(0, digits);
    }

    private static string RepoRoot()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "WiseOwl.Cobol.sln"))) dir = Path.GetDirectoryName(dir);
        return dir ?? throw new DirectoryNotFoundException("repository root (WiseOwl.Cobol.sln) not found above " + AppContext.BaseDirectory);
    }
}
