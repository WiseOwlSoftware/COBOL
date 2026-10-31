// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime.Collation;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Collation;

/// <summary>
/// The table-driven NFD (Runtime/Collation/Normalizer.cs; The Unicode Standard §3.11, the Canonical Ordering
/// Algorithm), in its two shapes: the string form and the span form a comparison uses. The span form must give the
/// string form's text in a caller buffer when it fits and in a pooled array when it does not, and a comparison of texts
/// that need their NFD must allocate nothing (kb/Work PB107: the NFD cost 394 B on accented text, 93 B per comparison).
/// </summary>
public sealed class NormalizerTests
{
    private static readonly CollationTable Table = CollationTable.Root;

    public static TheoryData<string> Texts() => new()
    {
        "abc",
        "e\u0301",                                   // already NFD
        "é",                                    // precomposed é
        "a\u0323\u0301",                             // marks in canonical order
        "a\u0301\u0323",                             // marks out of canonical order: reordered (ccc 230 after 220)
        "ệ",                                    // ệ: a decomposition of two marks, ordered
        "각",                                    // a Hangul LVT syllable
        "가",                                    // a Hangul LV syllable
        "\U0001D15E",                                // a supplementary character with a canonical decomposition
        "a\U0001D165\U0001D16D",                     // supplementary non-starters, ccc 216 then 226
        "x\uD800y",                                  // an unpaired high surrogate passes through
        "x\uDC00\u0301",                             // an unpaired low surrogate followed by a mark
        "",
        string.Concat(Enumerable.Repeat("éa\u0301\u0323", 200)),   // longer than the 128-char scratch
    };

    [Theory]
    [MemberData(nameof(Texts))]
    public void SpanForm_EqualsTheStringForm(string text)
    {
        string expected = Normalizer.ToNfd(text, Table);
        Span<char> scratch = stackalloc char[128];
        var span = Normalizer.ToNfd(text, Table, scratch, out var rented);
        try
        {
            Assert.Equal(expected, span.ToString());
            Assert.Equal(expected.Length > scratch.Length, rented is not null);   // the pool only past the caller's buffer
        }
        finally { Normalizer.Return(rented); }
    }

    [Fact]
    public void CanonicalOrdering_SortsMarksByCombiningClass_AndCarriesTheirOrigin()
    {
        // a + acute (230) + dot below (220): NFD puts the dot below first; each char's origin travels with it.
        var origin = new List<int>();
        string nfd = Normalizer.ToNfd("a\u0301\u0323", Table, origin);
        Assert.Equal("a\u0323\u0301", nfd);
        Assert.Equal([0, 2, 1], origin);
        // A precomposed character's whole decomposition comes from it: é (1 char) → e + acute.
        origin.Clear();
        Assert.Equal("e\u0301", Normalizer.ToNfd("é", Table, origin));
        Assert.Equal([0, 0], origin);
        // A supplementary character is two UTF-16 units, both from the same source char index.
        origin.Clear();
        Assert.Equal("\U0001D157\U0001D165", Normalizer.ToNfd("\U0001D15E", Table, origin));   // U+1D15E → U+1D157 U+1D165
        Assert.Equal([0, 0, 0, 0], origin);
    }

    /// <summary>A comparison of texts that hold combining marks decomposes them into stack buffers: nothing is
    /// allocated per comparison, the short or the long text alike. (Measured over many comparisons after a warm-up, so
    /// a one-time allocation — the pool's first array — is not counted.)</summary>
    [Fact]
    public void Compare_OfTextsNeedingNfd_AllocatesNothing()
    {
        string shortA = "e\u0301cole", shortB = "e\u0323\u0301cole";
        string longA = string.Concat(Enumerable.Repeat("a\u0301\u0323b", 100)), longB = string.Concat(Enumerable.Repeat("a\u0323\u0301b", 100)) + "c";
        var collator = Collator.Root;
        Assert.NotEqual(0, collator.Compare(shortA, shortB));
        Assert.True(collator.Compare(longA, longB) < 0);
        for (int i = 0; i < 50; i++) { collator.Compare(shortA, shortB); collator.Compare(longA, longB); }   // warm the pool
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++) { collator.Compare(shortA, shortB); collator.Compare(longA, longB); }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        // Nothing per comparison. The bound allows the shared array pool to rebuild itself once or twice when a GC
        // (another test's) trims it in the window; the list-and-StringBuilder NFD this replaced cost ~130 B per
        // comparison, 260 KB over the 2000 here.
        Assert.True(allocated < 16 * 1024, $"{allocated} B allocated over 2000 comparisons");
    }
}
