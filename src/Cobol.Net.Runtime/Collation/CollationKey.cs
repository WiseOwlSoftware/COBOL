// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.Collation;

/// <summary>
/// The materialized sort key of one text under one <see cref="Collator"/> (UTS #10 S3 "form sort key"): the
/// non-zero weights of each level in order — <see cref="Primary"/>, <see cref="Secondary"/>, <see cref="Tertiary"/>
/// and, under <see cref="AlternateHandling.Shifted"/>, <see cref="Quaternary"/> — compared level by level, a
/// shorter level sorting before a longer one it prefixes. Two keys built by the same collator order exactly as
/// <see cref="Collator.Compare(string?,string?)"/> orders their texts; build keys once when a text is compared many
/// times (a SORT of many records, an index).
/// </summary>
public sealed class CollationKey : IComparable<CollationKey>, IEquatable<CollationKey>
{
    private readonly int[][] _levels;
    private readonly string? _identical;

    internal CollationKey(int[][] levels, string? identical, Collator collator)
    {
        _levels = levels;
        _identical = identical;
        Collator = collator;
    }

    /// <summary>The key of <paramref name="text"/> under the CLDR root default (<see cref="Collator.Root"/>).</summary>
    public static CollationKey Build(string? text) => Collator.Root.GetKey(text);

    /// <summary>The key of <paramref name="text"/> under <paramref name="collator"/>.</summary>
    public static CollationKey Build(string? text, Collator collator) => (collator ?? Collator.Root).GetKey(text);

    /// <summary>The collator that built this key — keys are comparable only within one collator.</summary>
    public Collator Collator { get; }

    /// <summary>Level-1 weights (base letters).</summary>
    public IReadOnlyList<int> Primary => _levels[0];

    /// <summary>Level-2 weights (accents); empty when the collator's strength is primary.</summary>
    public IReadOnlyList<int> Secondary => _levels.Length > 1 ? _levels[1] : [];

    /// <summary>Level-3 weights (case/width/variant); empty below tertiary strength.</summary>
    public IReadOnlyList<int> Tertiary => _levels.Length > 2 ? _levels[2] : [];

    /// <summary>Level-4 weights (the shifted variable positions); empty unless the collator is
    /// <see cref="AlternateHandling.Shifted"/> at quaternary strength or above.</summary>
    public IReadOnlyList<int> Quaternary => _levels.Length > 3 ? _levels[3] : [];

    /// <summary>The number of weight levels this key carries (1–4).</summary>
    public int LevelCount => _levels.Length;

    /// <summary>The weights of a level (1-based) — a small, allocation-free view.</summary>
    public ReadOnlySpan<int> Level(int level) => (uint)(level - 1) < (uint)_levels.Length ? _levels[level - 1] : [];

    /// <summary>Level-by-level lexicographic comparison; at <see cref="CollationStrength.Identical"/> the NFD code
    /// point sequence breaks the last tie. Keys from different collators are not comparable (an ArgumentException).</summary>
    public int CompareTo(CollationKey? other)
    {
        if (other is null) return 1;
        if (!ReferenceEquals(Collator, other.Collator))
            throw new ArgumentException("collation keys built by different collators are not comparable", nameof(other));
        int n = Math.Max(_levels.Length, other._levels.Length);
        for (int i = 0; i < n; i++)
        {
            ReadOnlySpan<int> a = i < _levels.Length ? _levels[i] : [];
            ReadOnlySpan<int> b = i < other._levels.Length ? other._levels[i] : [];
            int c = a.SequenceCompareTo(b);
            if (c != 0) return c < 0 ? -1 : 1;
        }
        if (_identical is not null || other._identical is not null)
            return Collator.CompareCodePoints(_identical ?? "", other._identical ?? "");
        return 0;
    }

    public bool Equals(CollationKey? other) => other is not null && ReferenceEquals(Collator, other.Collator) && CompareTo(other) == 0;

    public override bool Equals(object? obj) => obj is CollationKey k && Equals(k);

    public override int GetHashCode()
    {
        var h = new HashCode();
        foreach (var level in _levels)
            foreach (int w in level) h.Add(w);
        h.Add(_identical);
        return h.ToHashCode();
    }

    /// <summary>A byte image of the key — the levels' weights big-endian, each level terminated by a 0x00 0x00 0x00
    /// pair no weight can produce (weights are ≥ 1) — so byte-wise ordinal comparison of two images from the same
    /// collator equals <see cref="CompareTo"/> through the last weight level (the identical tie-break is not encoded).
    /// Suitable as an opaque key for external index structures.</summary>
    public byte[] ToByteArray()
    {
        int size = 0;
        foreach (var level in _levels) size += level.Length * 3 + 3;
        var bytes = new byte[size];
        int p = 0;
        foreach (var level in _levels)
        {
            foreach (int w in level)
            {
                bytes[p++] = (byte)(w >> 16);
                bytes[p++] = (byte)(w >> 8);
                bytes[p++] = (byte)w;
            }
            p += 3;   // level terminator: 00 00 00
        }
        return bytes;
    }

    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < _levels.Length; i++)
        {
            if (i > 0) sb.Append(" | ");
            foreach (int w in _levels[i]) sb.Append(w.ToString("X")).Append(' ');
        }
        return sb.ToString().TrimEnd();
    }
}
