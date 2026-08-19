// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.Collation.Cache;

/// <summary>How a <see cref="CollationKeyCache"/> makes room when it is full.</summary>
public enum CacheEvictionStrategy
{
    /// <summary>Evict the entries used least recently (by last-access time) — the default; a SORT or an index that
    /// revisits the same key values keeps them.</summary>
    LeastRecentlyUsed = 0,
    /// <summary>Evict the OLDEST entries (by insertion time) — first in, first out: cheaper bookkeeping (no access
    /// stamp on hits), right for a stream of mostly-new values.</summary>
    SizeBased = 1,
}

/// <summary>
/// The configuration of a <see cref="CollationKeyCache"/>: whether caching is on at all, how many keys a cache
/// holds, how it evicts, and how long a text may be to be cached. Immutable; <see cref="Default"/> is what the
/// runtime uses unless <see cref="FromEnvironment"/> (read once by <see cref="CollationRuntime"/>) or a host says
/// otherwise.
/// </summary>
/// <param name="Enabled">False turns every cache into a pass-through (<c>GetOrBuild</c> builds, stores nothing).</param>
/// <param name="MaxEntries">The number of keys a cache holds before it evicts (per cache — one per collator).</param>
/// <param name="Eviction">The eviction strategy.</param>
/// <param name="MaxTextLength">Texts longer than this (in UTF-16 code units) are built but never stored — a single
/// multi-megabyte key must not displace thousands of useful ones.</param>
/// <param name="EvictionFraction">When full, the fraction of <see cref="MaxEntries"/> evicted at once (amortizes the
/// eviction scan; 0.25 = evict down to three quarters).</param>
public sealed record CacheConfig(
    bool Enabled = true,
    int MaxEntries = 8192,
    CacheEvictionStrategy Eviction = CacheEvictionStrategy.LeastRecentlyUsed,
    int MaxTextLength = 512,
    double EvictionFraction = 0.25)
{
    /// <summary>The environment variable: <c>off</c> / <c>0</c> disables caching; a positive number sets
    /// <see cref="MaxEntries"/>; anything else (or unset) keeps the default.</summary>
    public const string SizeVariable = "COBOL_COLLATION_CACHE";

    /// <summary>The environment variable: <c>lru</c> (default) or <c>fifo</c> / <c>size</c>.</summary>
    public const string EvictionVariable = "COBOL_COLLATION_CACHE_EVICTION";

    /// <summary>The defaults: enabled, 8,192 keys per collator, LRU, texts up to 512 code units, evict a quarter at a time.</summary>
    public static CacheConfig Default { get; } = new();

    /// <summary>A pass-through configuration.</summary>
    public static CacheConfig Disabled { get; } = new(Enabled: false, MaxEntries: 0);

    /// <summary>The configuration the environment asks for (see <see cref="SizeVariable"/>, <see cref="EvictionVariable"/>),
    /// over <see cref="Default"/>. Never throws: an unparsable value keeps the default.</summary>
    public static CacheConfig FromEnvironment()
    {
        var config = Default;
        string? size = Environment.GetEnvironmentVariable(SizeVariable)?.Trim();
        if (!string.IsNullOrEmpty(size))
        {
            if (size.Equals("off", StringComparison.OrdinalIgnoreCase) || size.Equals("false", StringComparison.OrdinalIgnoreCase) || size == "0")
                config = config with { Enabled = false, MaxEntries = 0 };
            else if (int.TryParse(size, out int n) && n > 0)
                config = config with { MaxEntries = n };
        }
        string? eviction = Environment.GetEnvironmentVariable(EvictionVariable)?.Trim();
        if (!string.IsNullOrEmpty(eviction))
        {
            if (eviction.Equals("fifo", StringComparison.OrdinalIgnoreCase) || eviction.Equals("size", StringComparison.OrdinalIgnoreCase))
                config = config with { Eviction = CacheEvictionStrategy.SizeBased };
            else if (eviction.Equals("lru", StringComparison.OrdinalIgnoreCase))
                config = config with { Eviction = CacheEvictionStrategy.LeastRecentlyUsed };
        }
        return config;
    }

    /// <summary>Validate the values (a negative size, a fraction outside (0, 1]).</summary>
    public CacheConfig Validated()
    {
        if (MaxEntries < 0) throw new ArgumentOutOfRangeException(nameof(MaxEntries), MaxEntries, "must be ≥ 0");
        if (MaxTextLength < 0) throw new ArgumentOutOfRangeException(nameof(MaxTextLength), MaxTextLength, "must be ≥ 0");
        if (EvictionFraction is <= 0 or > 1) throw new ArgumentOutOfRangeException(nameof(EvictionFraction), EvictionFraction, "must be in (0, 1]");
        return this;
    }
}
