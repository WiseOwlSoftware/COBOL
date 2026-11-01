// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using WiseOwl.Cobol.Runtime.Collation;
using WiseOwl.Cobol.Runtime.Collation.Cache;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Collation;

/// <summary>
/// <see cref="CollationRuntime.Initialize"/> is a one-shot whose LOSERS WAIT (kb/Work PB380): the latch is published
/// AFTER the work, so a caller that sees <see cref="CollationRuntime.IsInitialized"/> true — or that returns from
/// <c>Initialize</c> because another caller is mid-way — reads the cache configuration the environment named, never
/// the compiled-in default. Before the fix the latch was a <c>CompareExchange</c> taken BEFORE the work, so the losers
/// of a race returned at once and read <see cref="CacheConfig.Default"/> (8,192 keys) instead of
/// <c>COBOL_COLLATION_CACHE</c> (stale in ~69 % of trials of 32 concurrent run-unit constructions).
/// </summary>
[Collection("process-globals")]   // mutates COBOL_COLLATION_CACHE, CollationKeyCache.DefaultConfig and the one-shot latch (restored in a finally)
public sealed class CollationRuntimeInitializeTests
{
    private const int ConfiguredSize = 777;      // distinguishable from CacheConfig.Default.MaxEntries (8192)
    private const int Threads = 16;
    private const int Trials = 100;              // the old body read stale in 808 of 4,800 reads over 300 trials; 300 took 27 s under a loaded gate

    private static readonly Type s_runtime = typeof(CollationRuntime);

    private static FieldInfo Field(string name) =>
        s_runtime.GetField(name, BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException($"CollationRuntime.{name} is gone: update this test's reset to the new one-shot state.");

    /// <summary>
    /// Sets the whole one-shot state (default configuration, host flag, last warm-up, then the latch) as ONE step under the
    /// runtime's own <c>s_initLock</c>. Run units built by tests of other collections (<c>new RunUnit()</c> calls
    /// <see cref="CollationRuntime.Initialize"/>) run in parallel with this class; were the reset a series of bare field
    /// writes, such a call could win the latch BETWEEN them and the test would then overwrite the configuration that
    /// winner computed (review R1-1). Held under the lock, a foreign winner either finishes before the reset or starts
    /// after it, and the environment variable is process-global, so it reads the same configured size this test expects.
    /// </summary>
    private static void SetOneShotState(object? latch, object? hostConfigured, object? lastWarmup, CacheConfig config)
    {
        object gate = Field("s_initLock").GetValue(null)!;
        lock (gate)
        {
            CollationKeyCache.DefaultConfig = config;
            Field("s_hostConfigured").SetValue(null, hostConfigured);
            Field("s_lastWarmup").SetValue(null, lastWarmup);
            Field("s_initialized").SetValue(null, latch);        // last: the latch is what publishes the state above
        }
    }

    [Fact]
    public void ConcurrentInitializers_AllSeeTheEnvironmentsCacheConfiguration_NeverTheCompiledInDefault()
    {
        object? savedInitialized = Field("s_initialized").GetValue(null);
        object? savedHost = Field("s_hostConfigured").GetValue(null);
        object? savedWarmup = Field("s_lastWarmup").GetValue(null);
        var savedConfig = CollationKeyCache.DefaultConfig;
        string? savedEnv = Environment.GetEnvironmentVariable(CacheConfig.SizeVariable);
        string? savedWarm = Environment.GetEnvironmentVariable(CollationRuntime.WarmupVariable);
        Assert.NotEqual(ConfiguredSize, CacheConfig.Default.MaxEntries);
        int stale = 0;
        var seen = new List<int>();
        try
        {
            Environment.SetEnvironmentVariable(CacheConfig.SizeVariable, ConfiguredSize.ToString());
            Environment.SetEnvironmentVariable(CollationRuntime.WarmupVariable, null);
            for (int trial = 0; trial < Trials; trial++)
            {
                SetOneShotState(0, false, null, CacheConfig.Default);   // a fresh process, as far as the one-shot can tell
                using var barrier = new Barrier(Threads);
                var sizes = new int[Threads];
                var notInitialized = new bool[Threads];
                var workers = new Thread[Threads];
                for (int i = 0; i < Threads; i++)
                {
                    int slot = i;
                    workers[i] = new Thread(() =>
                    {
                        barrier.SignalAndWait();
                        CollationRuntime.Initialize();
                        sizes[slot] = CollationKeyCache.DefaultConfig.MaxEntries;     // what a cache created now would be sized by
                        notInitialized[slot] = !CollationRuntime.IsInitialized;
                    });
                    workers[i].Start();
                }
                foreach (var w in workers) w.Join();
                Assert.DoesNotContain(true, notInitialized);         // a returned Initialize is an initialized runtime
                foreach (int size in sizes)
                    if (size != ConfiguredSize) { stale++; seen.Add(size); }
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable(CacheConfig.SizeVariable, savedEnv);
            Environment.SetEnvironmentVariable(CollationRuntime.WarmupVariable, savedWarm);
            SetOneShotState(savedInitialized, savedHost, savedWarmup, savedConfig);
        }
        Assert.True(stale == 0, $"{stale} caller(s) read a cache size other than {ConfiguredSize} after Initialize returned: {string.Join(",", seen.Distinct())}");
    }

    [Fact]
    public void ConfigureCache_ByTheHost_IsNeverOverwrittenByTheEnvironment()
    {
        object? savedInitialized = Field("s_initialized").GetValue(null);
        object? savedHost = Field("s_hostConfigured").GetValue(null);
        object? savedWarmup = Field("s_lastWarmup").GetValue(null);
        var savedConfig = CollationKeyCache.DefaultConfig;
        string? savedEnv = Environment.GetEnvironmentVariable(CacheConfig.SizeVariable);
        try
        {
            Environment.SetEnvironmentVariable(CacheConfig.SizeVariable, ConfiguredSize.ToString());
            SetOneShotState(0, false, null, CacheConfig.Default);
            CollationRuntime.ConfigureCache(new CacheConfig(MaxEntries: 123));
            CollationRuntime.Initialize();
            Assert.Equal(123, CollationKeyCache.DefaultConfig.MaxEntries);
            Assert.True(CollationRuntime.IsInitialized);
        }
        finally
        {
            Environment.SetEnvironmentVariable(CacheConfig.SizeVariable, savedEnv);
            SetOneShotState(savedInitialized, savedHost, savedWarmup, savedConfig);
        }
    }
}
