// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Collation;
using Xunit;

namespace CobolNet.Tests.Unit.Collation;

/// <summary>
/// The materialized sort KEY (Runtime/Collation/CollationKey.cs — UTS #10 S3 "form sort key"): the per-level weight
/// sequences <see cref="CollationKey.Build(string?)"/> produces under the root default, their shape for one, several
/// and expanding code points, equality of keys for equal texts, the case-only and accent-only differences landing on
/// exactly the tertiary / secondary level, the byte image, and the guard against comparing keys of different collators.
/// </summary>
public sealed class CollationKeyTests
{
    [Fact]
    public void Build_SingleLetter_HasOneWeightPerLevel()
    {
        var k = CollationKey.Build("a");
        Assert.Single(k.Primary);
        Assert.Single(k.Secondary);
        Assert.Single(k.Tertiary);
        Assert.Empty(k.Quaternary);                        // the root default is non-ignorable: no level 4
        Assert.Equal(3, k.LevelCount);
        // …and the weights are the table's for 'a' (source [.23EC.0020.0002], primary scaled by the table's shift).
        var t = CollationTable.Root;
        Assert.Equal(t.Lookup('a').Primary, k.Primary[0]);
        Assert.Equal(0x0020, k.Secondary[0]);
        Assert.Equal(0x0002, k.Tertiary[0]);
        Assert.Same(Collator.Root, k.Collator);
    }

    [Fact]
    public void Build_ThreeLetters_HasThreePrimaries_InTextOrder()
    {
        var k = CollationKey.Build("abc");
        Assert.Equal(3, k.Primary.Count);
        Assert.Equal(3, k.Secondary.Count);
        Assert.Equal(3, k.Tertiary.Count);
        var t = CollationTable.Root;
        Assert.Equal(new[] { t.Lookup('a').Primary, t.Lookup('b').Primary, t.Lookup('c').Primary }, k.Primary);
        Assert.True(k.Primary[0] < k.Primary[1] && k.Primary[1] < k.Primary[2]);
    }

    /// <summary>Ignorable and expanding code points change the SHAPE: a control character contributes nothing;
    /// ß contributes two primaries (s, s) and three secondaries (its ligature mark sits between them).</summary>
    [Fact]
    public void Build_IgnorablesContributeNothing_ExpansionsContributeSeveral()
    {
        Assert.Equal(CollationKey.Build("ab"), CollationKey.Build("a\U00000001b"));   // U+0001 is completely ignorable
        Assert.Empty(CollationKey.Build("").Primary);
        Assert.Empty(CollationKey.Build(null).Primary);
        var eszett = CollationKey.Build("ß");
        var ss = CollationKey.Build("ss");
        Assert.Equal(ss.Primary, eszett.Primary);          // s s at level 1
        Assert.Equal(3, eszett.Secondary.Count);           // 0020, 011F (the ligature mark), 0020
        Assert.Equal(2, ss.Secondary.Count);
        Assert.True(eszett.CompareTo(ss) > 0);             // …so ß > ss, at the secondary level
    }

    [Fact]
    public void Build_EqualTexts_GiveEqualKeys()
    {
        var a = CollationKey.Build("Hello, World");
        var b = CollationKey.Build("Hello, World");
        Assert.NotSame(a, b);
        Assert.Equal(0, a.CompareTo(b));
        Assert.True(a.Equals(b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.Equal(a.ToByteArray(), b.ToByteArray());
        Assert.Equal(a.ToString(), b.ToString());
        // Canonically equivalent spellings give equal keys too (é vs e + COMBINING ACUTE).
        Assert.Equal(0, CollationKey.Build("caf\U000000E9").CompareTo(CollationKey.Build("cafe\U00000301")));
    }

    /// <summary>A case-only difference is a TERTIARY difference: identical primary and secondary sequences,
    /// tertiary sequences that differ — and the lowercase key sorts first.</summary>
    [Fact]
    public void Build_CaseOnlyDifference_DiffersOnlyAtTertiary()
    {
        var lower = CollationKey.Build("abc");
        var upper = CollationKey.Build("Abc");
        Assert.Equal(lower.Primary, upper.Primary);
        Assert.Equal(lower.Secondary, upper.Secondary);
        Assert.NotEqual(lower.Tertiary, upper.Tertiary);
        Assert.Equal(lower.Tertiary.Count, upper.Tertiary.Count);
        Assert.Equal(0x0002, lower.Tertiary[0]);           // lowercase
        Assert.Equal(0x0008, upper.Tertiary[0]);           // uppercase
        Assert.Equal(lower.Tertiary[1], upper.Tertiary[1]);
        Assert.True(lower.CompareTo(upper) < 0);
        Assert.True(upper.CompareTo(lower) > 0);
    }

    /// <summary>An accent-only difference is a SECONDARY difference: identical primaries, a longer/greater
    /// secondary sequence for the accented text — and the plain key sorts first.</summary>
    [Fact]
    public void Build_AccentOnlyDifference_DiffersOnlyAtSecondary()
    {
        var plain = CollationKey.Build("resume");
        var accented = CollationKey.Build("r\U000000E9sum\U000000E9");
        Assert.Equal(plain.Primary, accented.Primary);
        Assert.NotEqual(plain.Secondary, accented.Secondary);
        Assert.Equal(6, plain.Secondary.Count);
        Assert.Equal(8, accented.Secondary.Count);         // each é adds the acute's secondary (0024)
        Assert.Contains(0x0024, accented.Secondary);
        Assert.True(plain.CompareTo(accented) < 0);
        // The primaries decide before any accent does: "resumf" > "résumé".
        Assert.True(CollationKey.Build("resumf").CompareTo(accented) > 0);
    }

    [Fact]
    public void Build_UnderOtherCollators_TakesTheirLevels()
    {
        var primaryOnly = CollationKey.Build("Abc", Collator.Root.With(strength: CollationStrength.Primary));
        Assert.Equal(1, primaryOnly.LevelCount);
        Assert.Equal(3, primaryOnly.Primary.Count);
        Assert.Empty(primaryOnly.Secondary);
        Assert.Equal(0, primaryOnly.CompareTo(CollationKey.Build("abc", primaryOnly.Collator)));
        var standard = CollationKey.Build("a-b", CollationEngine.Standard);
        Assert.Equal(4, standard.LevelCount);
        Assert.Equal(2, standard.Primary.Count);           // the hyphen is shifted out of levels 1–3…
        Assert.Equal(3, standard.Quaternary.Count);        // …and weighs at level 4 between the two letters
        Assert.Throws<ArgumentException>(() => standard.CompareTo(CollationKey.Build("a-b")));   // different collators
    }

    [Fact]
    public void ToByteArray_OrdersLikeCompareTo()
    {
        string[] words = ["b", "B", "a", "A", "ab", "aB", "Ab", "", "\U000000E1", "abc", "a b", "a-b"];
        var keys = words.Select(w => CollationKey.Build(w)).ToArray();
        for (int i = 0; i < keys.Length; i++)
            for (int j = 0; j < keys.Length; j++)
            {
                int viaKeys = Math.Sign(keys[i].CompareTo(keys[j]));
                int viaBytes = Math.Sign(keys[i].ToByteArray().AsSpan().SequenceCompareTo(keys[j].ToByteArray()));
                Assert.True(viaKeys == viaBytes, $"'{words[i]}' vs '{words[j]}': keys {viaKeys}, bytes {viaBytes}");
                Assert.Equal(viaKeys, Math.Sign(CollationEngine.Compare(words[i], words[j])));
            }
    }
}
