// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using WiseOwl.Cobol.Runtime.Collation.Cache;
using WiseOwl.Cobol.Runtime.Collation.Cldr;
using WiseOwl.Cobol.Runtime.Collation.Locale;
using WiseOwl.Cobol.Runtime.Unicode;
using WiseOwl.Cobol.Runtime.Unicode.Segmentation;

namespace WiseOwl.Cobol.Runtime.Collation;

/// <summary>What <see cref="CollationRuntime.Status"/> / <see cref="CollationRuntime.Warmup"/> report.</summary>
/// <param name="Initialized">Whether <see cref="CollationRuntime.Initialize"/> has run.</param>
/// <param name="UcaVersion">The derived collation table's UCA/CLDR data version.</param>
/// <param name="UnicodeVersion">The grapheme segmentation table's Unicode version.</param>
/// <param name="CldrRelease">The embedded CLDR collation pack's release.</param>
/// <param name="CldrFiles">The number of locale files in the pack.</param>
/// <param name="CacheConfig">The key cache configuration new caches take.</param>
/// <param name="DefaultLocale">The ambient run unit's LC_COLLATE locale.</param>
/// <param name="DefaultLocaleResolved">Whether the default locale resolved (built) — false with <paramref name="Warning"/> set when it could not.</param>
/// <param name="NfcAvailable">Whether the host can compose (NFC).</param>
/// <param name="Warmed">Whether the tables and the default locale were loaded eagerly.</param>
/// <param name="WarmupTime">How long the eager load took (null when not warmed).</param>
/// <param name="Warning">A problem the warm-up met (an unselectable default locale, a malformed site file …), or null.</param>
public sealed record CollationRuntimeStatus(bool Initialized, string UcaVersion, string UnicodeVersion, string CldrRelease, int CldrFiles,
    CacheConfig CacheConfig, string DefaultLocale, bool DefaultLocaleResolved, bool NfcAvailable, bool Warmed, TimeSpan? WarmupTime, string? Warning);

/// <summary>
/// The INITIALIZATION of the text-processing subsystems — collation engine, CLDR locale loader, locale selection,
/// key cache, normalization, grapheme segmentation — in one place (kb/Work PB101/PB104–PB106; the "runtime
/// initialization" of the integration plan). Two levels:
/// <list type="bullet">
/// <item><see cref="Initialize"/> — CHEAP, idempotent and safe to race (a concurrent caller waits for the winner's
/// configuration and returns only once <see cref="IsInitialized"/> is true, kb/Work PB380); every run unit calls it
/// when it is created (<see cref="RunUnit"/>): reads the environment's cache configuration (<see cref="CacheConfig.FromEnvironment"/>)
/// into <see cref="CollationKeyCache.DefaultConfig"/> unless a host already set one, and — only when
/// <c>COBOL_COLLATION_WARMUP</c> is set — warms up. Nothing else is loaded eagerly: the derived tables decode
/// lazily on first use (a program that never collates under a locale never pays for them), which is the same
/// behaviour every table had before this class existed.</item>
/// <item><see cref="Warmup"/> — EAGER: decodes the root collation table, the grapheme table and the CLDR pack,
/// resolves and builds the default locale's collation (the ambient run unit's LC_COLLATE — owner decision Q2's
/// <c>COBOL_USER_LOCALE</c>, else the process culture, else the root) and touches the normalizer, so the first
/// comparison of a latency-sensitive host pays nothing. Reports what it did; a default locale that cannot be
/// resolved is a <see cref="CollationRuntimeStatus.Warning"/>, never an exception — the program still runs, under
/// the root order.</item>
/// </list>
/// The COMPILER (<c>cobol.exe</c>) does not call either: compiling never collates. It reaches the same subsystem
/// only when it VALIDATES a name at compile time — an <c>ORDER TABLE</c> literal (COBOL1662 when
/// <see cref="CollationEngine.TryGetOrderingTable"/> cannot resolve it) — and that resolution loads what it needs on
/// demand.
/// </summary>
public static class CollationRuntime
{
    /// <summary>Set (to anything but 0/false/off) to warm the subsystems up when the first run unit is created.</summary>
    public const string WarmupVariable = "COBOL_COLLATION_WARMUP";

    // Guards the one-shot's WORK (kb/Work PB380): the latch s_initialized is published only after the cache
    // configuration is in place, and a concurrent caller blocks here until the winner has done it — a caller that
    // returns from Initialize, or that reads IsInitialized true, therefore never sees the compiled-in default.
    private static readonly object s_initLock = new();
    private static int s_initialized;
    private static bool s_hostConfigured;     // read and written only under s_initLock
    private static CollationRuntimeStatus? s_lastWarmup;

    /// <summary>True after <see cref="Initialize"/> ran.</summary>
    public static bool IsInitialized => Volatile.Read(ref s_initialized) != 0;

    /// <summary>Tell the runtime the host configured the cache itself (before any run unit) — <see cref="Initialize"/>
    /// then leaves <see cref="CollationKeyCache.DefaultConfig"/> alone.</summary>
    public static void ConfigureCache(CacheConfig config)
    {
        lock (s_initLock)
        {
            CollationKeyCache.DefaultConfig = config;
            s_hostConfigured = true;
        }
    }

    /// <summary>The cheap, idempotent initialization every run unit performs (see the class summary).</summary>
    public static void Initialize()
    {
        if (IsInitialized) return;                                  // the common case: the latch is published after the work
        lock (s_initLock)
        {
            if (IsInitialized) return;                              // a racing caller finished while this one waited
            if (!s_hostConfigured) CollationKeyCache.DefaultConfig = CacheConfig.FromEnvironment();
            Volatile.Write(ref s_initialized, 1);                   // publish AFTER the work (release): see s_initLock
        }
        // The warm-up is an optimization, not configuration, and Warmup itself re-enters Initialize (and creates a
        // run unit, whose constructor does too), so it runs by the winner alone, outside the lock and past the latch.
        string? warm = Environment.GetEnvironmentVariable(WarmupVariable)?.Trim();
        if (!string.IsNullOrEmpty(warm) && warm is not ("0" or "false" or "off" or "no"))
            s_lastWarmup = Warmup();
    }

    /// <summary>The eager load (see the class summary). <paramref name="localeTag"/> null = the ambient run unit's
    /// LC_COLLATE locale.</summary>
    public static CollationRuntimeStatus Warmup(string? localeTag = null)
    {
        Initialize();
        var sw = Stopwatch.StartNew();
        string? warning = null;
        bool resolved = false;
        string tag = localeTag ?? RunUnit.Current.Locale.Current(LocaleCategory.Collate);
        try
        {
            _ = CollationTable.Root;                       // the derived root table
            _ = GraphemeBreaker.UnicodeVersion;            // the grapheme property table
            _ = CldrLocaleLoader.PackRelease;              // the CLDR pack (parsed on demand per file)
            _ = UnicodeNormalizer.IsNfcAvailable;          // the host's composer, probed once
            var info = LocaleManager.GetLocale(tag);       // resolve + build the default locale's collation
            _ = info.Collator;
            _ = CollationKeyCache.For(info.Collator);      // its key cache
            resolved = true;
        }
        // Exactly the two ways a default locale cannot be resolved: it names nothing this system knows, or its site
        // collation data cannot be used (LocaleCollationException, the ONE failure of the resolution path — a malformed
        // or stale site file used to escape this filter as an InvalidOperationException and kill the first run unit
        // at start-up; kb/Work PB2773). Anything else is a defect of the runtime and is not hidden here.
        catch (Exception ex) when (ex is System.Globalization.CultureNotFoundException or LocaleCollationException)
        {
            warning = $"warm-up: {ex.Message}";
        }
        sw.Stop();
        var status = Describe(warmed: true, sw.Elapsed, resolved, warning);
        s_lastWarmup = status;
        return status;
    }

    /// <summary>The current state of the subsystems (never loads anything that is not loaded, except the versions
    /// it reports).</summary>
    public static CollationRuntimeStatus Status =>
        s_lastWarmup ?? Describe(warmed: false, null, resolved: false, warning: null);

    private static CollationRuntimeStatus Describe(bool warmed, TimeSpan? elapsed, bool resolved, string? warning) => new(
        IsInitialized,
        CollationTable.Root.UcaVersion,
        GraphemeBreaker.UnicodeVersion,
        CldrLocaleLoader.PackRelease,
        CldrLocaleLoader.PackLocales.Count,
        CollationKeyCache.DefaultConfig,
        RunUnit.TryCurrent?.Locale.Current(LocaleCategory.Collate) ?? LocaleConfig.DefaultLocale,
        resolved,
        UnicodeNormalizer.IsNfcAvailable,
        warmed,
        elapsed,
        warning);
}
