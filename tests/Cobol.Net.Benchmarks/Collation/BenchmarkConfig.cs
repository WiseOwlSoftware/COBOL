// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using Perfolizer.Horology;

namespace CobolNet.Benchmarks.Collation;

/// <summary>
/// The BenchmarkDotNet configuration every benchmark class in this assembly applies through
/// <c>[Config(typeof(BenchmarkConfig))]</c>:
/// <list type="bullet">
/// <item><b>Runtime</b> — the host runtime, i.e. the project's TFM (net10.0, from the repository-root
/// Directory.Build.props). To pin another runtime, add <c>.WithRuntime(CoreRuntime.Core90)</c> (etc.) to the job;
/// the referenced runtime assembly must be buildable for it.</item>
/// <item><b>Counts</b> — 3 warm-up + 12 measured iterations, at least 250 ms of work per iteration, 1 launch.
/// Enough for a stable median on the sub-microsecond collation paths, quick enough for an ad-hoc run.</item>
/// <item><b>Diagnosers</b> — <see cref="MemoryDiagnoser"/>: allocated bytes per operation plus Gen0/1/2 counts, so
/// the collation engine's "allocation-free on the common path" claim (Collation/README.md §4) is MEASURED here
/// rather than asserted. A per-operation allocation appearing where the README says there is none is a
/// regression even when the time column does not move.</item>
/// <item><b>Grouping</b> — logical groups by <c>[BenchmarkCategory]</c>, so the <c>Ratio</c> column compares each
/// benchmark against the baseline OF ITS OWN CATEGORY (short-string comparison against ordinal, and so on) and
/// never against an unrelated one.</item>
/// <item><b>Output</b> — BenchmarkDotNet's defaults (the console summary plus GitHub-flavoured Markdown, HTML and
/// CSV under <c>BenchmarkDotNet.Artifacts/results/</c>) with Median and P95 added to the standard
/// Mean/Error/StdDev columns. P95 is what catches a path that is usually fast and occasionally re-normalizes or
/// re-allocates — a mean alone hides that.</item>
/// </list>
/// Command-line switches still override everything here (<c>--job short</c>, <c>--filter</c>, <c>--exporters</c>, …).
/// </summary>
public sealed class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddJob(Job.Default
            .WithStrategy(RunStrategy.Throughput)
            .WithWarmupCount(3)
            .WithIterationCount(12)
            .WithMinIterationTime(TimeInterval.FromMilliseconds(250))
            .WithLaunchCount(1)
            .WithId("collation"));

        AddDiagnoser(MemoryDiagnoser.Default);

        AddColumn(StatisticColumn.Median, StatisticColumn.P95);
        AddColumn(CategoriesColumn.Default);
        AddLogicalGroupRules(BenchmarkLogicalGroupRule.ByCategory);

        // NOTE — the console logger and the GitHub-Markdown / HTML / CSV exporters are NOT added here. They are
        // already in BenchmarkDotNet's DefaultConfig, which is unioned with this one, and adding them again makes
        // the runner print "The exporter … is already present in configuration. There may be unexpected results."
        // for each. Only genuinely non-default output belongs in this method.

        // A Debug-built dependency is REPORTED as a warning instead of aborting the run. The measured numbers in
        // README.md come from a Release solution build; this only keeps an exploratory run from being refused.
        WithOptions(ConfigOptions.DisableOptimizationsValidator);

        WithSummaryStyle(SummaryStyle.Default.WithRatioStyle(RatioStyle.Trend));
    }
}
