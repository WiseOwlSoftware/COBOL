// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using CobolNet.Runtime.Collation;
using CobolNet.Runtime.Collation.Cache;
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit.Collation;

/// <summary>
/// The collation key cache (Runtime/Collation/Cache/, kb/Work PB106): a cached key IS the collator's key (same
/// object on a hit, equal order always); the counters count; LRU keeps the recently used and evicts the rest,
/// size-based (FIFO) evicts the oldest; a disabled cache is a pass-through; over-long texts are built but not stored;
/// concurrent callers get one key per text; the per-collator instances stay apart; and the consumers that compare the
/// same values many times — SORT/MERGE key columns and the LOCALE key sequence — go through it and order exactly as
/// the streaming comparison does.
/// </summary>
public sealed class CollationKeyCacheTests
{
    [Fact]
    public void GetKey_CachesOneKeyPerText_AndCounts()
    {
        var cache = new CollationKeyCache(Collator.Root, new CacheConfig(MaxEntries: 100));
        var k1 = cache.GetKey("hello");
        var k2 = cache.GetKey("hello");
        Assert.Same(k1, k2);
        Assert.Equal(1, cache.Count);
        Assert.Equal(1, cache.Hits);
        Assert.Equal(1, cache.Misses);
        Assert.Equal(0, k1.CompareTo(Collator.Root.GetKey("hello")));
        Assert.True(cache.TryGet("hello", out var found) && ReferenceEquals(found, k1));
        Assert.False(cache.TryGet("world", out _));
        Assert.Equal(1, cache.Count);                       // TryGet never builds
        var entry = Assert.Single(cache.Entries);
        Assert.Equal("hello", entry.Text);
        Assert.Equal(2, entry.HitCount);                    // the second GetKey and the TryGet
        Assert.True(entry.LastAccess >= entry.CreatedAt);
        Assert.True(entry.Age >= TimeSpan.Zero && entry.IdleFor <= entry.Age);
        Assert.Equal(cache.Compare("apple", "banana"), Math.Sign(Collator.Root.Compare("apple", "banana")));
        cache.Clear();
        Assert.Equal(0, cache.Count);
        Assert.Equal(2, cache.Hits);                        // counters survive Clear (the second GetKey and the TryGet)
    }

    [Fact]
    public void StaticForm_UsesTheSharedRootCache()
    {
        var k = CollationKeyCache.GetOrBuild("shared-form");
        Assert.Same(k, CollationKeyCache.Shared.GetKey("shared-form"));
        Assert.Same(Collator.Root, CollationKeyCache.Shared.Collator);
        Assert.Same(CollationKeyCache.Shared, CollationKeyCache.For(Collator.Root));
        Assert.Equal(0, k.CompareTo(CollationKey.Build("shared-form")));
    }

    [Fact]
    public void Lru_EvictsTheLeastRecentlyUsed()
    {
        // 8 entries max; a full cache evicts down to 8 − ceil(8 × 0.375) = 5 entries.
        var cache = new CollationKeyCache(Collator.Root, new CacheConfig(MaxEntries: 8, EvictionFraction: 0.375));
        for (int i = 0; i < 8; i++) cache.GetKey($"k{i}");
        Assert.Equal(8, cache.Count);
        // Touch k0..k3 so k4..k7 are the least recently used.
        Thread.Sleep(2);
        for (int i = 0; i < 4; i++) cache.GetKey($"k{i}");
        Thread.Sleep(2);
        cache.GetKey("k8");                                 // 9 > 8: evict the 4 least recently used
        Assert.Equal(4, cache.Evictions);
        Assert.Equal(5, cache.Count);
        for (int i = 0; i < 4; i++) Assert.True(cache.TryGet($"k{i}", out _), $"k{i} (recently used) should survive");
        Assert.True(cache.TryGet("k8", out _));
        for (int i = 4; i < 8; i++) Assert.False(cache.TryGet($"k{i}", out _), $"k{i} (least recently used) should be gone");
        // Correctness never depends on presence.
        Assert.Equal(0, cache.GetKey("k5").CompareTo(Collator.Root.GetKey("k5")));
    }

    [Fact]
    public void SizeBased_EvictsTheOldest()
    {
        var cache = new CollationKeyCache(Collator.Root, new CacheConfig(MaxEntries: 8, Eviction: CacheEvictionStrategy.SizeBased, EvictionFraction: 0.5));
        for (int i = 0; i < 8; i++) { cache.GetKey($"k{i}"); Thread.Sleep(1); }
        for (int i = 0; i < 4; i++) cache.GetKey($"k{i}");  // touching does not matter for FIFO
        cache.GetKey("k8");
        Assert.True(cache.Count <= 4);
        for (int i = 0; i < 4; i++) Assert.False(cache.TryGet($"k{i}", out _), $"k{i} (oldest) should be gone");
        Assert.True(cache.TryGet("k7", out _));
        Assert.True(cache.TryGet("k8", out _));
    }

    [Fact]
    public void Disabled_AndOverlongTexts_AreBuiltNotStored()
    {
        var off = new CollationKeyCache(Collator.Root, CacheConfig.Disabled);
        var k = off.GetKey("x");
        Assert.NotSame(k, off.GetKey("x"));
        Assert.Equal(0, off.Count);
        Assert.Equal(2, off.Misses);
        Assert.Equal(0, k.CompareTo(Collator.Root.GetKey("x")));
        var small = new CollationKeyCache(Collator.Root, new CacheConfig(MaxTextLength: 4));
        small.GetKey("short");                              // 5 > 4: not stored
        small.GetKey("ok");
        Assert.Equal(1, small.Count);
        Assert.Throws<ArgumentOutOfRangeException>(() => new CacheConfig(MaxEntries: -1).Validated());
        Assert.Throws<ArgumentOutOfRangeException>(() => new CacheConfig(EvictionFraction: 0).Validated());
    }

    [Fact]
    public void Config_FromEnvironment()
    {
        string? oldSize = Environment.GetEnvironmentVariable(CacheConfig.SizeVariable);
        string? oldEv = Environment.GetEnvironmentVariable(CacheConfig.EvictionVariable);
        try
        {
            Environment.SetEnvironmentVariable(CacheConfig.SizeVariable, "1234");
            Environment.SetEnvironmentVariable(CacheConfig.EvictionVariable, "fifo");
            var c = CacheConfig.FromEnvironment();
            Assert.True(c.Enabled);
            Assert.Equal(1234, c.MaxEntries);
            Assert.Equal(CacheEvictionStrategy.SizeBased, c.Eviction);
            Environment.SetEnvironmentVariable(CacheConfig.SizeVariable, "off");
            Assert.False(CacheConfig.FromEnvironment().Enabled);
            Environment.SetEnvironmentVariable(CacheConfig.SizeVariable, "nonsense");
            Environment.SetEnvironmentVariable(CacheConfig.EvictionVariable, "lru");
            Assert.Equal(CacheConfig.Default.MaxEntries, CacheConfig.FromEnvironment().MaxEntries);
            Assert.Equal(CacheEvictionStrategy.LeastRecentlyUsed, CacheConfig.FromEnvironment().Eviction);
        }
        finally
        {
            Environment.SetEnvironmentVariable(CacheConfig.SizeVariable, oldSize);
            Environment.SetEnvironmentVariable(CacheConfig.EvictionVariable, oldEv);
        }
    }

    [Fact]
    public void ConcurrentCallers_GetOneKeyPerText_AndNeverAWrongOne()
    {
        var cache = new CollationKeyCache(Collator.Root, new CacheConfig(MaxEntries: 64, EvictionFraction: 0.25));
        string[] texts = Enumerable.Range(0, 200).Select(i => $"text-{i % 90}").ToArray();
        var seen = new System.Collections.Concurrent.ConcurrentDictionary<string, CollationKey>();
        Parallel.For(0, 4000, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i =>
        {
            string t = texts[i % texts.Length];
            var k = cache.GetKey(t);
            Assert.Equal(0, k.CompareTo(Collator.Root.GetKey(t)));   // always the right key …
            seen.AddOrUpdate(t, k, (_, prev) => prev);
        });
        Assert.True(cache.Count <= 64);
        Assert.True(cache.Hits + cache.Misses == 4000);
        // … and while an entry lives, every caller sees the same instance.
        var e = cache.Entries.FirstOrDefault();
        if (e is not null) Assert.Same(e.Key, cache.GetKey(e.Text));
    }

    [Fact]
    public void PerCollatorCaches_StayApart_AndAreReused()
    {
        var es = CollationEngine.ForLocale("es");
        var esCache = CollationKeyCache.For(es);
        Assert.Same(esCache, CollationKeyCache.For(es));
        Assert.NotSame(esCache, CollationKeyCache.Shared);
        var k = esCache.GetKey("ñ");
        Assert.Throws<ArgumentException>(() => k.CompareTo(CollationKeyCache.Shared.GetKey("ñ")));   // different collators
        Assert.Same(k, es.GetKeyCached("ñ"));
        Assert.Same(k, CollationEngine.GetKey("ñ", "es"));
    }

    // ---- integration: the LOCALE sequence, SORT/MERGE, the indexed key comparison --------------------------

    [Fact]
    public void LocaleCollation_KeyOf_UsesTheCache_AndOrdersLikeCompare()
    {
        var seq = new LocaleCollation("da");
        Assert.True(seq.SupportsKeys);
        var k1 = seq.KeyOf("Åse   ");                       // §8.8.4.2.11: trailing spaces trimmed before keying
        var k2 = seq.KeyOf("Åse");
        Assert.Same(k1, k2);
        string[] words = ["Aase", "Åse", "aase", "øl", "Øl", "z", "æble", "a", "A", "AA", "Aa", "aa", "", "   "];
        foreach (string a in words)
            foreach (string b in words)
                Assert.Equal(Math.Sign(seq.Compare(a, b)), Math.Sign(seq.KeyOf(a)!.CompareTo(seq.KeyOf(b))));
        Assert.False(new AlphanumericCollation(new ushort[256], new ushort[256], 256).SupportsKeys);
    }

    [Fact]
    public void Sort_UnderALocaleSequence_OrdersLikeCompare_AndUsesKeys()
    {
        // A SORT with one alphanumeric key under the Swedish sequence: the result equals a stable sort by Compare.
        var seq = new LocaleCollation("sv");
        string[] records = ["ölm  ", "zed  ", "åsa  ", "abc  ", "ärlig", "Zoe  ", "øre  ", "aa   ", "ÅSA  ", "zed  "];
        const string name = "SD-CACHE-TEST";
        CobolSort.Init(name);
        foreach (var r in records) CobolSort.Release(name, r);
        var keys = new[] { new CobolSort.Key(0, 5, Descending: false, Numeric: false, default) };
        var before = CollationKeyCache.For(seq.Resolve()).Misses;
        CobolSort.Sort(name, keys, seq, duplicatesInOrder: true);
        var after = CollationKeyCache.For(seq.Resolve()).Misses;
        var sorted = new List<string>();
        while (CobolSort.Return(name, out string? rec)) sorted.Add(rec!);
        CobolSort.Close(name);
        var expected = records.Select((r, i) => (r, i)).OrderBy(x => x.r, Comparer<string>.Create(seq.Compare)).ThenBy(x => x.i).Select(x => x.r).ToList();
        Assert.Equal(expected, sorted);
        Assert.True(after > before, "the sort keyed its records through the cache");
        Assert.Equal("aa   ", sorted[0]);
        Assert.Equal("øre  ", sorted[^1]);                  // Swedish: ö/ø last (ø a secondary variant of ö; "ölm" < "øre" at level 1 on l < r)
    }
}
