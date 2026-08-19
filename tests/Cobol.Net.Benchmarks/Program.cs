// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using BenchmarkDotNet.Running;

namespace CobolNet.Benchmarks;

/// <summary>
/// The benchmark entry point. `BenchmarkSwitcher` exposes every <c>[Benchmark]</c> class in this assembly to the
/// BenchmarkDotNet command line, so:
/// <code>
///   dotnet run -c Release --project tests/Cobol.Net.Benchmarks                        # interactive menu / all
///   dotnet run -c Release --project tests/Cobol.Net.Benchmarks -- --filter *Collation*  # the collation class
///   dotnet run -c Release --project tests/Cobol.Net.Benchmarks -- --list flat           # list benchmarks
///   dotnet run -c Release --project tests/Cobol.Net.Benchmarks -- --job short           # a quicker run
/// </code>
/// With no arguments and exactly one class the switcher runs it directly (the equivalent of
/// <c>BenchmarkRunner.Run&lt;CollationBenchmarks&gt;()</c>). The configuration (jobs, warmup/iteration counts,
/// diagnosers, exporters) is <see cref="Collation.BenchmarkConfig"/>, applied by the class's <c>[Config]</c> attribute.
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        // A non-zero exit when any benchmark failed to build/run, so a scripted invocation notices.
        return summaries.Any(s => s.HasCriticalValidationErrors || s.Reports.Any(r => !r.Success)) ? 1 : 0;
    }
}
