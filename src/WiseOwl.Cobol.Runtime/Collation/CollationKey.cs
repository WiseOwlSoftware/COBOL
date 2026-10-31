// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime.Collation;

/// <summary>
/// The materialized sort key of one text under one <see cref="Collator"/> (UTS #10 S3 "form sort key"): the
/// non-zero weights of each level in order — <see cref="Primary"/>, <see cref="Secondary"/>, the <see cref="CaseLevel"/>
/// when the collator has one, <see cref="Tertiary"/> and, under <see cref="AlternateHandling.Shifted"/> (or on a table
/// with tailored quaternary weights), <see cref="Quaternary"/> — compared level by level, a shorter level sorting before
/// a longer one it prefixes. Two keys built by the same collator order exactly as
/// <see cref="Collator.Compare(string?,string?)"/> orders their texts; build keys once when a text is compared many
/// times (a SORT of many records, an index).
/// </summary>
public sealed class CollationKey : IComparable<CollationKey>, IEquatable<CollationKey>
{
    // The levels in COMPARISON order, by level number: the four weight levels, the case level between 2 and 3.
    private static readonly int[] s_order = [1, 2, Collator.CaseLevelNumber, 3, 4];

    private readonly int[]?[] _byLevel;      // indexed by level number (1–4, Collator.CaseLevelNumber); null = not carried
    private readonly string? _identical;

    internal CollationKey(int[]?[] byLevel, string? identical, Collator collator)
    {
        _byLevel = byLevel;
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
    public IReadOnlyList<int> Primary => Weights(1);

    /// <summary>Level-2 weights (accents); empty when the collator's strength is primary.</summary>
    public IReadOnlyList<int> Secondary => Weights(2);

    /// <summary>The case level's weights (one per element that has a level-2 weight, one per element with a primary at
    /// primary strength: lowercase, then mixed, then uppercase — reversed under <see cref="CaseFirst.Upper"/>); empty
    /// unless the collator has <see cref="CollationOptions.CaseLevel"/> on.</summary>
    public IReadOnlyList<int> CaseLevel => Weights(Collator.CaseLevelNumber);

    /// <summary>Level-3 weights (case/width/variant); empty below tertiary strength.</summary>
    public IReadOnlyList<int> Tertiary => Weights(3);

    /// <summary>Level-4 weights (the shifted variable positions, and the tailored quaternary weights); empty unless the
    /// collator is at quaternary strength or above and is <see cref="AlternateHandling.Shifted"/> or tailored with
    /// quaternary relations.</summary>
    public IReadOnlyList<int> Quaternary => Weights(4);

    /// <summary>The number of weight levels this key carries (1–4; the case level is not counted).</summary>
    public int LevelCount
    {
        get
        {
            int n = 0;
            for (int level = 1; level <= 4; level++)
                if (_byLevel[level] is not null) n++;
            return n;
        }
    }

    /// <summary>The weights of a level (1-based) — a small, allocation-free view.</summary>
    public ReadOnlySpan<int> Level(int level) => (uint)level < (uint)_byLevel.Length && _byLevel[level] is { } weights ? weights : [];

    private int[] Weights(int level) => _byLevel[level] ?? [];

    /// <summary>Level-by-level lexicographic comparison; at <see cref="CollationStrength.Identical"/> the NFD code
    /// point sequence breaks the last tie. Keys from different collators are not comparable (an ArgumentException).</summary>
    public int CompareTo(CollationKey? other)
    {
        if (other is null) return 1;
        if (!ReferenceEquals(Collator, other.Collator))
            throw new ArgumentException("collation keys built by different collators are not comparable", nameof(other));
        foreach (int level in s_order)
        {
            int c = Level(level).SequenceCompareTo(other.Level(level));
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
        foreach (int level in s_order)
            foreach (int w in Level(level)) h.Add(w);
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
        foreach (int level in s_order)
            if (_byLevel[level] is { } weights) size += weights.Length * 3 + 3;
        var bytes = new byte[size];
        int p = 0;
        foreach (int level in s_order)
        {
            if (_byLevel[level] is not { } weights) continue;
            foreach (int w in weights)
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
        bool first = true;
        foreach (int level in s_order)
        {
            if (_byLevel[level] is not { } weights) continue;
            if (!first) sb.Append(" | ");
            first = false;
            foreach (int w in weights) sb.Append(w.ToString("X")).Append(' ');
        }
        return sb.ToString().TrimEnd();
    }
}
