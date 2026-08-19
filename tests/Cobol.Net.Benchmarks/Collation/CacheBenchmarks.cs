// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using BenchmarkDotNet.Attributes;
using CobolNet.Runtime.Collation;
using CobolNet.Runtime.Collation.Cache;

namespace CobolNet.Benchmarks.Collation;

/// <summary>
/// The collation key cache (<c>Runtime/Collation/Cache/</c>, kb/Work PB106): what a HIT costs against building the
/// key, what a MISS costs against a plain build (the price of storing), and — the design question the cache exists
/// to answer — where comparing through cached keys beats the allocation-free streaming comparison. The README's
/// integration rule ("SORT/MERGE and indexed keys go through the cache; a relation condition does not") is what
/// these numbers justify: a short-string hit is a dictionary lookup, which the streaming compare beats for
/// short operands and loses to for long ones compared many times.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class CacheBenchmarks
{
    private const string Hit = "Cache-Hit";
    private const string Miss = "Cache-Miss";
    private const string CompareShort = "Cache-Compare-Short";
    private const string CompareLong = "Cache-Compare-Long";
    private const string CompareCase = "Cache-Compare-CaseDifferent";
    private const int Ops = 64;

    private static readonly string[] Short =
    [
        "apple", "banana", "cherry", "damson", "elder", "fig", "grape", "honey", "ivory", "jasper", "kiwi", "lemon", "mango", "nectar", "olive", "peach",
        "station", "statute", "balance", "balcony", "account", "accrual", "invoice", "involve", "payment", "payroll", "customer", "customary", "shipping", "shipment", "register", "registry",
        "Apple", "BANANA", "Cherry", "DELTA", "Echo", "FOXTROT", "Golf", "HOTEL", "identical", "account", "balance", "customer", "invoice", "payment", "shipping", "register",
        "00012345", "00012346", "2024-01-01", "2024-01-02", "A1000", "A1001", "PO-7781", "PO-7782", "9999", "10000", "ITEM0001", "ITEM0002", "X42", "X43", "0.00", "0.01",
    ];

    private string[] _long = null!;
    private string[] _lower = null!, _upper = null!;
    private string[] _uniqueShort = null!;
    private CollationKeyCache _warm = null!;
    private CollationKeyCache _warmLong = null!;
    private CollationKeyCache _warmCase = null!;
    private CollationKeyCache _churn = null!;
    private CollationKeyCache _disabled = null!;
    private int _uniqueCursor;

    [GlobalSetup]
    public void Setup()
    {
        // 64 distinct 400-character texts sharing a 390-character stem: the shape of a long INDEXED key or SORT key
        // where the streaming compare must walk far before it decides.
        const string Filler = "the quick brown fox jumps over the lazy dog while the ledger balances and the invoice register reconciles every payment ";
        var stem = string.Concat(Enumerable.Repeat(Filler, 4))[..390];
        _long = Enumerable.Range(0, Ops).Select(i => stem + i.ToString("D10")).ToArray();
        // 64 pairs of 120-character texts that differ ONLY in case throughout ("INVOICE 0007 …" vs "invoice 0007 …"):
        // no identical prefix to skip, equal at levels 1 and 2 over their whole length, decided at level 3 — the
        // shape of records a SORT compares again and again, and where a materialized key pays.
        _lower = Enumerable.Range(0, Ops).Select(i => string.Concat(Enumerable.Repeat($"invoice {i:D4} due on the first ", 5))[..120]).ToArray();
        _upper = _lower.Select(s => s.ToUpperInvariant()).ToArray();
        _uniqueShort = Enumerable.Range(0, 1 << 16).Select(i => "value-" + i.ToString("D6")).ToArray();
        _warm = new CollationKeyCache(Collator.Root, new CacheConfig(MaxEntries: 1 << 16));
        _warmLong = new CollationKeyCache(Collator.Root, new CacheConfig(MaxEntries: 1 << 16));
        _warmCase = new CollationKeyCache(Collator.Root, new CacheConfig(MaxEntries: 1 << 16));
        // A small cache fed with ever-new texts: every lookup misses, and eviction runs every few invocations —
        // the steady state of a stream of distinct values (miss + store + amortized eviction).
        _churn = new CollationKeyCache(Collator.Root, new CacheConfig(MaxEntries: 1024));
        _disabled = new CollationKeyCache(Collator.Root, CacheConfig.Disabled);
        foreach (var s in Short) _warm.GetKey(s);
        foreach (var s in _long) _warmLong.GetKey(s);
        foreach (var s in _lower) _warmCase.GetKey(s);
        foreach (var s in _upper) _warmCase.GetKey(s);
    }

    // ---- hit / miss --------------------------------------------------------------------------------------------

    /// <summary>A hit: the dictionary lookup plus the access stamp — no key is built.</summary>
    [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory(Hit)]
    public int Hit_ShortStrings()
    {
        int acc = 0;
        var c = _warm;
        for (int i = 0; i < Short.Length; i++) acc += c.GetKey(Short[i]).LevelCount;
        return acc;
    }

    /// <summary>The same texts keyed WITHOUT a cache — what a hit saves.</summary>
    [Benchmark(OperationsPerInvoke = Ops, Baseline = true), BenchmarkCategory(Hit)]
    public int Build_ShortStrings_NoCache()
    {
        int acc = 0;
        var c = Collator.Root;
        for (int i = 0; i < Short.Length; i++) acc += c.GetKey(Short[i]).LevelCount;
        return acc;
    }

    /// <summary>A hit on a 400-character text: the lookup hashes the text (O(n)) but builds nothing.</summary>
    [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory(Hit)]
    public int Hit_LongStrings()
    {
        int acc = 0;
        var c = _warmLong;
        for (int i = 0; i < _long.Length; i++) acc += c.GetKey(_long[i]).LevelCount;
        return acc;
    }

    /// <summary>A miss in steady state: build + store (the entry, the dictionary insert) + the amortized eviction
    /// of a full 1,024-entry cache fed with texts it has never seen (65,536 distinct texts cycle through).</summary>
    [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory(Miss)]
    public int Miss_ShortStrings()
    {
        int acc = 0;
        var c = _churn;
        int start = _uniqueCursor;
        _uniqueCursor = (start + Ops) & 0xFFFF;
        for (int i = 0; i < Ops; i++) acc += c.GetKey(_uniqueShort[(start + i) & 0xFFFF]).LevelCount;
        return acc;
    }

    /// <summary>The disabled cache over the same moving texts: the pass-through cost (build only, a counter increment).</summary>
    [Benchmark(OperationsPerInvoke = Ops, Baseline = true), BenchmarkCategory(Miss)]
    public int Miss_ShortStrings_Disabled()
    {
        int acc = 0;
        var c = _disabled;
        int start = _uniqueCursor;
        for (int i = 0; i < Ops; i++) acc += c.GetKey(_uniqueShort[(start + i) & 0xFFFF]).LevelCount;
        return acc;
    }

    // ---- compare through keys vs streaming -----------------------------------------------------------------

    /// <summary>Compare short pairs through cached keys (two hits + a key compare).</summary>
    [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory(CompareShort)]
    public int Compare_ShortStrings_ViaCache()
    {
        int acc = 0;
        var c = _warm;
        for (int i = 0; i < Short.Length; i++) acc += c.Compare(Short[i], Short[(i + 1) % Short.Length]);
        return acc;
    }

    /// <summary>The streaming comparison of the same pairs — the baseline the cache must beat to be used on a path.</summary>
    [Benchmark(OperationsPerInvoke = Ops, Baseline = true), BenchmarkCategory(CompareShort)]
    public int Compare_ShortStrings_Streaming()
    {
        int acc = 0;
        var c = Collator.Root;
        for (int i = 0; i < Short.Length; i++) acc += c.Compare(Short[i], Short[(i + 1) % Short.Length]);
        return acc;
    }

    /// <summary>Compare long pairs (equal for 390 characters) through cached keys.</summary>
    [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory(CompareLong)]
    public int Compare_LongStrings_ViaCache()
    {
        int acc = 0;
        var c = _warmLong;
        for (int i = 0; i < _long.Length; i++) acc += c.Compare(_long[i], _long[(i + 1) % _long.Length]);
        return acc;
    }

    /// <summary>The streaming comparison of the same long pairs — the identical-prefix skip lets it win here too.</summary>
    [Benchmark(OperationsPerInvoke = Ops, Baseline = true), BenchmarkCategory(CompareLong)]
    public int Compare_LongStrings_Streaming()
    {
        int acc = 0;
        var c = Collator.Root;
        for (int i = 0; i < _long.Length; i++) acc += c.Compare(_long[i], _long[(i + 1) % _long.Length]);
        return acc;
    }

    /// <summary>Compare 120-character texts differing only in case (equal through level 2, decided at level 3)
    /// through cached keys — the record-comparison shape where the key wins.</summary>
    [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory(CompareCase)]
    public int Compare_CaseDifferent_ViaCache()
    {
        int acc = 0;
        var c = _warmCase;
        for (int i = 0; i < _lower.Length; i++) acc += c.Compare(_lower[i], _upper[i]);
        return acc;
    }

    /// <summary>The streaming comparison of the same pairs: no prefix to skip, three full walks before it decides.</summary>
    [Benchmark(OperationsPerInvoke = Ops, Baseline = true), BenchmarkCategory(CompareCase)]
    public int Compare_CaseDifferent_Streaming()
    {
        int acc = 0;
        var c = Collator.Root;
        for (int i = 0; i < _lower.Length; i++) acc += c.Compare(_lower[i], _upper[i]);
        return acc;
    }
}
