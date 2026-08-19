// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.Collation;

/// <summary>
/// A configured comparison over one <see cref="CollationTable"/>: the table (root or tailored), the
/// <see cref="CollationStrength"/> (how many levels decide) and the <see cref="AlternateHandling"/> (how variable
/// elements take part). Immutable and thread-safe; <see cref="With"/> derives variants. <see cref="Root"/> is the
/// CLDR-default configuration (root table, tertiary, non-ignorable); <see cref="CollationEngine"/> is the static
/// façade that hands out the standard configurations (root, ISO/IEC 14651-style, per-locale).
/// <para>Comparison is STREAMED level by level (UTS #10 S3/S4 without materializing the key): level 1 walks both
/// strings' collation elements comparing the non-zero primaries; only on a tie is level 2 walked, and so on — the
/// common unequal-at-primary case costs one pass and no allocation. <see cref="GetKey"/> materializes the same
/// weights into a <see cref="CollationKey"/> for callers that compare one string many times (a SORT).</para>
/// </summary>
public sealed class Collator
{
    private static readonly Lazy<Collator> s_root = new(() => new Collator(CollationTable.Root), LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>The CLDR root collation at its defaults: root table, <see cref="CollationStrength.Tertiary"/>,
    /// <see cref="AlternateHandling.NonIgnorable"/> — the order ICU and CLDR give an untailored locale.</summary>
    public static Collator Root => s_root.Value;

    public Collator(CollationTable table, CollationStrength strength = CollationStrength.Tertiary,
        AlternateHandling alternate = AlternateHandling.NonIgnorable)
    {
        ArgumentNullException.ThrowIfNull(table);
        if (strength is < CollationStrength.Primary or > CollationStrength.Identical)
            throw new ArgumentOutOfRangeException(nameof(strength), strength, "collation strength must be 1 (primary) … 5 (identical)");
        Table = table;
        Strength = strength;
        Alternate = alternate;
    }

    /// <summary>The table the comparison reads.</summary>
    public CollationTable Table { get; }

    /// <summary>How many weight levels decide the order.</summary>
    public CollationStrength Strength { get; }

    /// <summary>How variable (space/punctuation/symbol) elements take part.</summary>
    public AlternateHandling Alternate { get; }

    /// <summary>A collator differing from this one in the given settings only.</summary>
    public Collator With(CollationStrength? strength = null, AlternateHandling? alternate = null, CollationTable? table = null) =>
        new(table ?? Table, strength ?? Strength, alternate ?? Alternate);

    /// <summary>Three-way comparison; null is the empty string. Returns &lt;0, 0 or &gt;0.</summary>
    public int Compare(string? a, string? b) => Compare(a.AsSpan(), b.AsSpan());

    /// <summary>Three-way comparison of two texts under this collator's table, strength and alternate handling.
    /// Canonically equivalent texts compare equal (the text is walked in NFD when it holds a combining mark);
    /// at <see cref="CollationStrength.Identical"/> the NFD code point sequence breaks every remaining tie.</summary>
    public int Compare(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        // Fast path: identical code units are identical at every level.
        if (a.SequenceEqual(b)) return 0;
        bool forIdentical = Strength == CollationStrength.Identical;
        if (Normalizer.NeedsNfd(a, Table, forIdentical)) a = Normalizer.ToNfd(a, Table);
        if (Normalizer.NeedsNfd(b, Table, forIdentical)) b = Normalizer.ToNfd(b, Table);

        int c = CompareLevel(a, b, 1);
        if (c != 0 || Strength == CollationStrength.Primary) return c;
        c = CompareLevel(a, b, 2);
        if (c != 0 || Strength == CollationStrength.Secondary) return c;
        c = CompareLevel(a, b, 3);
        if (c != 0 || Strength == CollationStrength.Tertiary) return c;
        if (Alternate == AlternateHandling.Shifted)
        {
            c = CompareLevel(a, b, 4);
            if (c != 0) return c;
        }
        if (Strength == CollationStrength.Quaternary) return 0;
        return CompareCodePoints(a, b);   // Identical: the canonically decomposed code point sequences
    }

    /// <summary>Ordinal comparison by CODE POINT (not UTF-16 code unit — a supplementary character must sort after
    /// every BMP character, where code-unit order would put its surrogates before U+E000..U+FFFF): the tie-break of
    /// <see cref="CollationStrength.Identical"/> and of the UCA conformance ordering.</summary>
    public static int CompareCodePoints(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        int i = 0, j = 0;
        while (i < a.Length && j < b.Length)
        {
            int ca = Decode(a, ref i), cb = Decode(b, ref j);
            if (ca != cb) return ca < cb ? -1 : 1;
        }
        return i < a.Length ? 1 : j < b.Length ? -1 : 0;

        static int Decode(ReadOnlySpan<char> s, ref int pos)
        {
            char c = s[pos++];
            if (char.IsHighSurrogate(c) && pos < s.Length && char.IsLowSurrogate(s[pos])) return char.ConvertToUtf32(c, s[pos++]);
            return c;
        }
    }

    /// <summary>The materialized sort key of <paramref name="text"/> under this collator — compare keys built by the
    /// SAME collator with <see cref="CollationKey.CompareTo"/>; the order equals <see cref="Compare(string?,string?)"/>.</summary>
    public CollationKey GetKey(string? text)
    {
        text ??= "";
        string walked = NeedsNormalization(text) ? Normalizer.ToNfd(text, Table) : text;
        int levels = Strength switch
        {
            CollationStrength.Primary => 1,
            CollationStrength.Secondary => 2,
            CollationStrength.Tertiary => 3,
            _ => Alternate == AlternateHandling.Shifted ? 4 : 3,
        };
        var perLevel = new int[levels][];
        for (int level = 1; level <= levels; level++)
        {
            var weights = new List<int>(walked.Length + 4);
            var it = new CollationElementIterator(walked, Table);
            bool afterVariable = false;
            while (it.TryNext(out var ce))
            {
                int w = WeightAt(ce, level, ref afterVariable);
                if (w != 0) weights.Add(w);
            }
            perLevel[level - 1] = weights.ToArray();
        }
        return new CollationKey(perLevel, Strength == CollationStrength.Identical ? walked : null, this);
    }

    /// <summary>True when <paramref name="text"/> is well-formed UTF-16 — the derived table orders every well-formed
    /// text; an unpaired surrogate is walked as a code unit (deterministically) but has no defined collation, which
    /// is what the COBOL layer's EC-LOCALE-INCOMPATIBLE reports.</summary>
    public static bool IsWellFormed(ReadOnlySpan<char> text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (!char.IsSurrogate(c)) continue;
            if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1])) { i++; continue; }
            return false;
        }
        return true;
    }

    /// <summary>Does the text need canonical decomposition before it is walked under this collator — it holds a
    /// non-starter (a combining mark, so canonical reordering may apply), or, at <see cref="CollationStrength.Identical"/>,
    /// any decomposable character (the tie-break compares NFD forms)?</summary>
    public bool NeedsNormalization(ReadOnlySpan<char> text) =>
        Normalizer.NeedsNfd(text, Table, forIdentical: Strength == CollationStrength.Identical);

    /// <summary>The NFD of <paramref name="text"/> under this collator's table data (see <see cref="Normalizer"/>).</summary>
    public string Normalize(ReadOnlySpan<char> text) => Normalizer.ToNfd(text, Table);

    private int CompareLevel(ReadOnlySpan<char> a, ReadOnlySpan<char> b, int level)
    {
        var ia = new CollationElementIterator(a, Table);
        var ib = new CollationElementIterator(b, Table);
        bool va = false, vb = false;
        while (true)
        {
            int wa = NextWeight(ref ia, level, ref va);
            int wb = NextWeight(ref ib, level, ref vb);
            if (wa != wb) return wa < wb ? -1 : 1;     // -1 (end) sorts before every weight: a proper prefix is less
            if (wa < 0) return 0;
        }
    }

    /// <summary>The next NON-ZERO weight of <paramref name="level"/> from the iterator, or −1 at the end.</summary>
    private int NextWeight(ref CollationElementIterator it, int level, ref bool afterVariable)
    {
        while (it.TryNext(out var ce))
        {
            int w = WeightAt(ce, level, ref afterVariable);
            if (w != 0) return w;
        }
        return -1;
    }

    /// <summary>The weight of <paramref name="element"/> at <paramref name="level"/> under this collator's alternate
    /// handling — UTS #10 Table 12 for <see cref="AlternateHandling.Shifted"/>: a variable element moves its primary
    /// to level 4 and is 0 at levels 1–3; a primary-ignorable element FOLLOWING a variable one is 0 everywhere; every
    /// other element keeps its three weights and takes the maximum ("no variable here") at level 4. Under
    /// <see cref="AlternateHandling.NonIgnorable"/> the three weights are used as they are and level 4 is unused (0).</summary>
    internal int WeightAt(in CollationElement element, int level, ref bool afterVariable)
    {
        if (Alternate == AlternateHandling.Shifted)
        {
            if (element.IsVariable)
            {
                afterVariable = true;
                return level == 4 ? element.Primary : 0;
            }
            if (element.IsCompletelyIgnorable) return 0;
            if (element.Primary == 0)
            {
                if (afterVariable) return 0;
                return level switch { 2 => element.Secondary, 3 => element.Tertiary, 4 => Table.MaxPrimary, _ => 0 };
            }
            afterVariable = false;
            return level switch { 1 => element.Primary, 2 => element.Secondary, 3 => element.Tertiary, 4 => Table.MaxPrimary, _ => 0 };
        }
        return level switch { 1 => element.Primary, 2 => element.Secondary, 3 => element.Tertiary, _ => 0 };
    }
}
