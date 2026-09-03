// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace CobolNet.Runtime.Collation.Cache;

/// <summary>
/// A thread-safe cache of <see cref="CollationKey"/>s for ONE <see cref="Collator"/> (keys are comparable only within
/// a collator, so a cache is bound to one): <see cref="GetKey"/> returns the key of a text, building it once and
/// then sharing it. Where it pays: the places that compare the SAME text values again and again — a SORT/MERGE
/// (every record's key columns are keyed once and the sort compares keys), an INDEXED file (every stored key is
/// compared on every lookup and insert), MAX/MIN over repeated values — and there the saving is the whole
/// collation-element walk per comparison. Where it does not: a single relation condition between two short
/// operands, which the streaming comparison decides in ~45 ns, faster than two dictionary lookups (measured:
/// <c>tests/Cobol.Net.Benchmarks</c>) — so <c>Collator.Compare</c> itself never consults a cache.
/// <para><b>Structure.</b> A <see cref="ConcurrentDictionary{TKey,TValue}"/> from the text (ordinal) to a
/// <see cref="CacheEntry"/>; hits touch the entry's access stamp and hit counter with interlocked operations. When the
/// count exceeds <see cref="CacheConfig.MaxEntries"/>, ONE thread evicts a batch — the least recently used entries
/// (<see cref="CacheEvictionStrategy.LeastRecentlyUsed"/>, by access stamp) or the oldest
/// (<see cref="CacheEvictionStrategy.SizeBased"/>, by insertion stamp), gathered by a lock-free enumeration — down to
/// (1 − <see cref="CacheConfig.EvictionFraction"/>) of the maximum, so the eviction pass is amortized over many
/// inserts and lookups never block. Texts longer than <see cref="CacheConfig.MaxTextLength"/> are built but not stored.</para>
/// <para><b>What <see cref="CacheConfig.MaxEntries"/> guarantees.</b> It is a QUIESCENCE bound, and it is a real one:
/// while callers are inserting concurrently the count rides above the maximum (an insert has to push it over before
/// anything notices, and a batch's removals are counted once at the end), but <b>once the last caller has returned
/// from <see cref="GetKey"/> the cache holds at most <see cref="CacheConfig.MaxEntries"/> keys</b> — the thread that
/// evicts keeps going until that is true and re-checks after it releases the flag, so an overflow raised by a caller
/// that arrived mid-pass is never dropped. (kb/Work PB377: it used to be dropped, and the maximum was then not a
/// bound at all — measured at 213 live entries for a cache configured with 64.)</para>
/// <para><b>Instances.</b> <see cref="For"/> hands out the cache of a collator (one per collator, created on demand,
/// held weakly so a discarded collator's cache is collected); <see cref="Shared"/> is the root collator's; the static
/// <see cref="GetOrBuild(string)"/> is the root-order convenience the design brief names. All configured by
/// <see cref="DefaultConfig"/> (settable before use; <see cref="CollationRuntime"/> initializes it from the
/// environment).</para>
/// </summary>
public sealed class CollationKeyCache
{
    private static readonly ConditionalWeakTable<Collator, CollationKeyCache> s_perCollator = new();
    private static CacheConfig s_defaultConfig = CacheConfig.Default;

    private readonly ConcurrentDictionary<string, CacheEntry> _map = new(StringComparer.Ordinal);
    private long _hits, _misses, _evictions;
    private int _count;          // tracked with interlocked updates: ConcurrentDictionary.Count takes every bucket lock
    private int _evicting;

    /// <summary>A cache for <paramref name="collator"/> with <paramref name="config"/> (null = <see cref="DefaultConfig"/>).</summary>
    public CollationKeyCache(Collator collator, CacheConfig? config = null)
    {
        ArgumentNullException.ThrowIfNull(collator);
        Collator = collator;
        Config = (config ?? DefaultConfig).Validated();
    }

    /// <summary>The configuration new caches (and <see cref="Shared"/>) take. Set it before the first use of a cache
    /// to change it; existing caches keep theirs.</summary>
    public static CacheConfig DefaultConfig
    {
        get => s_defaultConfig;
        set => s_defaultConfig = (value ?? CacheConfig.Default).Validated();
    }

    /// <summary>The cache of the root collator (the CLDR root order).</summary>
    public static CollationKeyCache Shared => For(Collator.Root);

    /// <summary>The cache of <paramref name="collator"/> — one per collator, created on first use.</summary>
    public static CollationKeyCache For(Collator collator)
    {
        ArgumentNullException.ThrowIfNull(collator);
        return s_perCollator.GetValue(collator, static c => new CollationKeyCache(c));
    }

    /// <summary>The root-order key of <paramref name="s"/> through the shared cache (the design brief's static form;
    /// the per-collator form is <c>CollationKeyCache.For(collator).GetKey(s)</c>).</summary>
    public static CollationKey GetOrBuild(string s) => Shared.Lookup(s ?? "", build: true)!;

    /// <summary>The collator whose keys this cache holds.</summary>
    public Collator Collator { get; }

    /// <summary>The configuration.</summary>
    public CacheConfig Config { get; }

    /// <summary>The number of cached keys.</summary>
    public int Count => Volatile.Read(ref _count);

    /// <summary>Lookups that found a key.</summary>
    public long Hits => Volatile.Read(ref _hits);

    /// <summary>Lookups that had to build a key.</summary>
    public long Misses => Volatile.Read(ref _misses);

    /// <summary>Entries removed by eviction.</summary>
    public long Evictions => Volatile.Read(ref _evictions);

    /// <summary>The key of <paramref name="text"/> under this cache's collator: cached when present, else built (by
    /// <see cref="Collator.GetKey"/>) and — when the configuration allows — stored.</summary>
    public CollationKey GetKey(string? text) => Lookup(text ?? "", build: true)!;

    /// <summary>The cached key of <paramref name="text"/>, or false without building one.</summary>
    public bool TryGet(string? text, out CollationKey? key)
    {
        key = Lookup(text ?? "", build: false);
        return key is not null;
    }

    /// <summary>Compare two texts through their cached keys — the same order as <see cref="Collator.Compare(string?,string?)"/>.</summary>
    public int Compare(string? a, string? b) => GetKey(a).CompareTo(GetKey(b));

    /// <summary>A snapshot of the entries (diagnostics; not in any particular order).</summary>
    public IReadOnlyList<CacheEntry> Entries => _map.Values.ToArray();

    /// <summary>Remove every entry (the hit/miss/eviction counters keep counting). A reset, meant for a quiescent
    /// cache: it is atomic against nothing, so the count is re-read from the map rather than assumed to be zero —
    /// writing a flat zero would DISCARD the increment of a lookup that inserted between the two statements, and the
    /// counter would then under-report the map for the rest of the process, which is the unsafe direction (the cache
    /// would sail past <see cref="CacheConfig.MaxEntries"/> without ever reaching the eviction trigger).</summary>
    public void Clear()
    {
        _map.Clear();
        Interlocked.Exchange(ref _count, _map.Count);
    }

    private CollationKey? Lookup(string text, bool build)
    {
        if (!Config.Enabled || text.Length > Config.MaxTextLength)
        {
            if (!build) return null;
            Interlocked.Increment(ref _misses);
            return Collator.GetKey(text);
        }
        long now = System.Diagnostics.Stopwatch.GetTimestamp();
        if (_map.TryGetValue(text, out var entry))
        {
            Interlocked.Increment(ref _hits);
            entry.Touch(now);
            return entry.Key;
        }
        if (!build) return null;
        Interlocked.Increment(ref _misses);
        var key = Collator.GetKey(text);
        var mine = new CacheEntry(text, key, now);
        var added = _map.GetOrAdd(text, mine);
        // Count the insert only if OUR entry is the one in the map — asking whether our ENTRY won, not whether the
        // returned KEY is ours, is what keeps the counter right no matter what Collator.GetKey does (it builds a
        // fresh key per call today; if it ever memoized, a key-identity test would let both racers count one entry).
        if (!ReferenceEquals(added, mine)) return added.Key;      // another thread won the race: one key per text
        if (Interlocked.Increment(ref _count) > Config.MaxEntries) Evict();
        return key;
    }

    /// <summary>Bring the cache back within <see cref="CacheConfig.MaxEntries"/>: one thread at a time; the others go
    /// on without waiting. It evicts BATCH BY BATCH until the count is back under the maximum, re-deriving the budget
    /// from the live count each time, because a batch's own budget goes stale the moment a concurrent caller inserts.
    /// <para>The batch flag is deliberately a try-once, not a wait — but the thread that holds it owns the whole job,
    /// including the overflows raised while it held it. Those callers found the flag taken and went on
    /// (<c>CompareExchange</c> failed), leaving no trace of themselves anywhere else, so if this loop did not pick
    /// them up they would be dropped and the cache would stay over its maximum until some later insert happened to
    /// find the flag clear — which, when the workload ends inside an eviction pass, never comes. That is exactly
    /// kb/Work PB377: a <c>Parallel.For</c> whose eight workers all finished within one pass left 73 keys in a cache
    /// configured with 64, and under heavier oversubscription 213.</para>
    /// <para>The release must be a full barrier (<see cref="Interlocked.Exchange(ref int,int)"/>, not
    /// <c>Volatile.Write</c>): a racer's <c>Interlocked.Increment</c> of <see cref="_count"/> happens-before its
    /// failed <c>CompareExchange</c> on <see cref="_evicting"/>; that failed RMW and this release are RMW/atomic
    /// operations on the SAME location and so are totally ordered, and the CAS failed only because the flag was still
    /// ours, so it precedes this release. A release-store followed by an acquire-load still permits StoreLoad
    /// reordering — the one reordering that would let the loop's re-read miss that increment — and a full barrier is
    /// what forbids it.</para></summary>
    private void Evict()
    {
        while (Volatile.Read(ref _count) > Config.MaxEntries)
        {
            if (Interlocked.CompareExchange(ref _evicting, 1, 0) != 0) return;   // another thread owns the job, and re-checks
            long evicted;
            try
            {
                evicted = EvictOneBatch();
            }
            finally
            {
                Interlocked.Exchange(ref _evicting, 0);
            }
            if (evicted == 0) return;   // no progress (the map holds fewer than the counter claims): do not spin
        }
    }

    /// <summary>One eviction batch: the entries are gathered by enumerating the dictionary (lock-free — never
    /// <c>ToArray</c>/<c>Count</c>, which take every bucket lock), sorted by their stamp (last access for LRU,
    /// creation for size-based) and the oldest removed, down to (1 − <see cref="CacheConfig.EvictionFraction"/>) of
    /// the maximum. O(n log n) per batch, amortized over a quarter of the capacity's worth of inserts (~150 ns per
    /// miss at 1,024 entries). Returns how many entries it removed.</summary>
    private long EvictOneBatch()
    {
        int max = Config.MaxEntries;
        int keep = Math.Max(0, max - (int)Math.Ceiling(max * Config.EvictionFraction));
        int toRemove = Count - keep;
        if (toRemove <= 0) return 0;
        // Parallel arrays — the stamps sort with the primitive comparer (no delegate per comparison) and carry
        // the entries along.
        var entries = new List<KeyValuePair<string, CacheEntry>>(Count + 16);
        foreach (var kv in _map) entries.Add(kv);
        var stamps = new long[entries.Count];
        bool lru = Config.Eviction == CacheEvictionStrategy.LeastRecentlyUsed;
        for (int i = 0; i < stamps.Length; i++) stamps[i] = lru ? entries[i].Value.LastAccess : entries[i].Value.CreatedAt;
        var items = entries.ToArray();
        Array.Sort(stamps, items);
        var collection = (ICollection<KeyValuePair<string, CacheEntry>>)_map;
        long evicted = 0;
        for (int i = 0; i < toRemove && i < items.Length; i++)
            if (collection.Remove(items[i])) evicted++;   // remove only the exact entry (a text re-inserted meanwhile keeps its new entry)
        Interlocked.Add(ref _evictions, evicted);
        Interlocked.Add(ref _count, (int)-evicted);
        return evicted;
    }

    public override string ToString() => $"CollationKeyCache({Collator.Table.Name}/{Collator.Strength}): {Count} keys, {Hits} hits, {Misses} misses, {Evictions} evictions";
}
