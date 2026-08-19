// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using BenchmarkDotNet.Attributes;
using CobolNet.Runtime.Collation;
using CobolNet.Runtime.Collation.Cldr;

namespace CobolNet.Benchmarks.Collation;

/// <summary>
/// The CLDR locale loader and tailoring builder (<c>Runtime/Collation/CLDR/</c>, kb/Work PB105): what it costs to
/// PARSE a CLDR file (the pack entry → <see cref="CldrLocaleData"/>), to BUILD a locale's table from its rules
/// (Spanish: 3 rules; Danish: 40-odd with caseFirst; Vietnamese: closure over 900 composites; Chinese pinyin: some
/// 40,000 relations with a reorder), and what a RESOLVED locale costs afterwards (a cache hit —
/// <see cref="CollationEngine.ForLocale"/>). The one-off costs are paid once per process per locale; the resolved
/// cost is what every comparison pays.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class CldrBenchmarks
{
    private const string Parse = "CLDR-Parse";
    private const string Build = "CLDR-Build";
    private const string Resolve = "CLDR-Resolve";

    private byte[] _esXml = null!, _daXml = null!, _zhXml = null!;
    private CldrCollationSelection _es = null!, _da = null!, _vi = null!, _zh = null!;

    [GlobalSetup]
    public void Setup()
    {
        _esXml = CldrLocaleLoader.ReadPackEntry("collation/es.xml")!;
        _daXml = CldrLocaleLoader.ReadPackEntry("collation/da.xml")!;
        _zhXml = CldrLocaleLoader.ReadPackEntry("collation/zh.xml")!;
        _es = CldrLocaleLoader.ResolveCollation("es");
        _da = CldrLocaleLoader.ResolveCollation("da");
        _vi = CldrLocaleLoader.ResolveCollation("vi");
        _zh = CldrLocaleLoader.ResolveCollation("zh");
        _ = CollationEngine.ForLocale("es");
        _ = CollationEngine.ForLocale("zh");
    }

    /// <summary>Parse es.xml (1 KB: three collations, a handful of rules).</summary>
    [Benchmark, BenchmarkCategory(Parse)]
    public int Parse_Es() => CldrParser.ParseXml(new MemoryStream(_esXml), "es").Collations.Count;

    /// <summary>Parse da.xml.</summary>
    [Benchmark, BenchmarkCategory(Parse)]
    public int Parse_Da() => CldrParser.ParseXml(new MemoryStream(_daXml), "da").Collations.Count;

    /// <summary>Parse zh.xml (1.1 MB: pinyin, stroke, zhuyin, unihan — some 40,000 relations each).</summary>
    [Benchmark, BenchmarkCategory(Parse)]
    public int Parse_Zh() => CldrParser.ParseXml(new MemoryStream(_zhXml), "zh").Collations.Count;

    /// <summary>Build the Spanish table (ñ after n) from its parsed rules — the whole pipeline after parsing.</summary>
    [Benchmark, BenchmarkCategory(Build)]
    public int Build_Es() => CldrTailoringBuilder.Build(_es, "es-bench").Table.MappingCount;

    /// <summary>Build Danish (caseFirst upper, æ ø å after z, aa as a variant of å).</summary>
    [Benchmark, BenchmarkCategory(Build)]
    public int Build_Da() => CldrTailoringBuilder.Build(_da, "da-bench").Table.MappingCount;

    /// <summary>Build Vietnamese (tone marks reordered → canonical closure over ~900 precomposed letters).</summary>
    [Benchmark, BenchmarkCategory(Build)]
    public int Build_Vi() => CldrTailoringBuilder.Build(_vi, "vi-bench").Table.MappingCount;

    /// <summary>Build Chinese pinyin (imports, ~40,000 relations, [reorder Hani]) — the heaviest locale in CLDR.</summary>
    [Benchmark, BenchmarkCategory(Build)]
    public int Build_Zh() => CldrTailoringBuilder.Build(_zh, "zh-bench").Table.MappingCount;

    /// <summary>What a program pays once the locale is resolved: the engine's per-tag cache lookup.</summary>
    [Benchmark(Baseline = true), BenchmarkCategory(Resolve)]
    public int Resolve_Cached_Es() => CollationEngine.ForLocale("es").Table.MappingCount;

    /// <summary>The same for the heaviest locale — identical, by design: resolution is per process.</summary>
    [Benchmark, BenchmarkCategory(Resolve)]
    public int Resolve_Cached_Zh() => CollationEngine.ForLocale("zh").Table.MappingCount;
}
