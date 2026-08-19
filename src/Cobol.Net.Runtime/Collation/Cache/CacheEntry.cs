// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.Collation.Cache;

/// <summary>
/// One entry of a <see cref="CollationKeyCache"/>: the cached <see cref="CollationKey"/>, when it was built, when it
/// was last used, and how often it was hit. Mutable only through the cache (the stamps and the counter are updated
/// with interlocked operations, so a read on another thread sees a consistent value).
/// </summary>
public sealed class CacheEntry
{
    private long _lastAccess;
    private long _hits;

    internal CacheEntry(string text, CollationKey key, long now)
    {
        Text = text;
        Key = key;
        CreatedAt = now;
        _lastAccess = now;
    }

    /// <summary>The text the key was built from.</summary>
    public string Text { get; }

    /// <summary>The cached key.</summary>
    public CollationKey Key { get; }

    /// <summary>When the entry was built (<see cref="System.Diagnostics.Stopwatch.GetTimestamp"/> ticks — monotonic, high resolution).</summary>
    public long CreatedAt { get; }

    /// <summary>When the entry was last returned by a lookup (same clock as <see cref="CreatedAt"/>).</summary>
    public long LastAccess => Volatile.Read(ref _lastAccess);

    /// <summary>How many lookups returned this entry after it was built.</summary>
    public long HitCount => Volatile.Read(ref _hits);

    /// <summary>How long ago the entry was built (from the monotonic stamp — the runtime reads no wall clock outside
    /// the run unit's clock seam).</summary>
    public TimeSpan Age => System.Diagnostics.Stopwatch.GetElapsedTime(CreatedAt);

    /// <summary>How long ago the entry was last hit.</summary>
    public TimeSpan IdleFor => System.Diagnostics.Stopwatch.GetElapsedTime(LastAccess);

    internal void Touch(long now)
    {
        Volatile.Write(ref _lastAccess, now);
        Interlocked.Increment(ref _hits);
    }

    public override string ToString() => $"'{Text}' → {Key} (hits {HitCount})";
}
