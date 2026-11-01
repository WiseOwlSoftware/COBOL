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
    private const int Trials = 300;

    private static readonly Type s_runtime = typeof(CollationRuntime);

    private static FieldInfo Field(string name) =>
        s_runtime.GetField(name, BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException($"CollationRuntime.{name} is gone: update this test's reset to the new one-shot state.");

    [Fact]
    public void ConcurrentInitializers_AllSeeTheEnvironmentsCacheConfiguration_NeverTheCompiledInDefault()
    {
        var initialized = Field("s_initialized");
        var hostConfigured = Field("s_hostConfigured");
        var lastWarmup = Field("s_lastWarmup");
        object? savedInitialized = initialized.GetValue(null);
        object? savedHost = hostConfigured.GetValue(null);
        object? savedWarmup = lastWarmup.GetValue(null);
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
                initialized.SetValue(null, 0);                       // a fresh process, as far as the one-shot can tell
                hostConfigured.SetValue(null, false);
                lastWarmup.SetValue(null, null);
                CollationKeyCache.DefaultConfig = CacheConfig.Default;
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
            CollationKeyCache.DefaultConfig = savedConfig;
            initialized.SetValue(null, savedInitialized);
            hostConfigured.SetValue(null, savedHost);
            lastWarmup.SetValue(null, savedWarmup);
        }
        Assert.True(stale == 0, $"{stale} caller(s) read a cache size other than {ConfiguredSize} after Initialize returned: {string.Join(",", seen.Distinct())}");
    }

    [Fact]
    public void ConfigureCache_ByTheHost_IsNeverOverwrittenByTheEnvironment()
    {
        var initialized = Field("s_initialized");
        var hostConfigured = Field("s_hostConfigured");
        object? savedInitialized = initialized.GetValue(null);
        object? savedHost = hostConfigured.GetValue(null);
        var savedConfig = CollationKeyCache.DefaultConfig;
        string? savedEnv = Environment.GetEnvironmentVariable(CacheConfig.SizeVariable);
        try
        {
            Environment.SetEnvironmentVariable(CacheConfig.SizeVariable, ConfiguredSize.ToString());
            initialized.SetValue(null, 0);
            hostConfigured.SetValue(null, false);
            CollationRuntime.ConfigureCache(new CacheConfig(MaxEntries: 123));
            CollationRuntime.Initialize();
            Assert.Equal(123, CollationKeyCache.DefaultConfig.MaxEntries);
            Assert.True(CollationRuntime.IsInitialized);
        }
        finally
        {
            Environment.SetEnvironmentVariable(CacheConfig.SizeVariable, savedEnv);
            CollationKeyCache.DefaultConfig = savedConfig;
            initialized.SetValue(null, savedInitialized);
            hostConfigured.SetValue(null, savedHost);
        }
    }
}
