// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers;

namespace CobolNet.Runtime.Collation;

/// <summary>
/// The SETTINGS of a comparison over a <see cref="CollationTable"/> — the UTS #35 collation settings this engine
/// implements, in one immutable value: <see cref="Strength"/> (how many levels decide), <see cref="Alternate"/> (how
/// variable elements take part), <see cref="MaxVariable"/> (which groups are variable), <see cref="CaseFirst"/> (case
/// before the other tertiary distinctions) and <see cref="BackwardsSecondary"/> (level 2 compared from the end —
/// Canadian French). A CLDR collation's own settings (<c>[caseFirst upper]</c>, <c>[backwards 2]</c>, <c>[alternate
/// shifted]</c>, <c>[strength n]</c>, <c>[maxVariable g]</c>) and the BCP 47 <c>-u-</c> keys (<c>ks</c>, <c>ka</c>,
/// <c>kv</c>, <c>kf</c>, <c>kb</c>) both resolve to this. <see cref="Default"/> is the CLDR default.
/// <para>Not represented, because the engine does not implement them (documented in Collation/CLDR/README.md; a
/// CLDR file that asks for one is loaded with the setting reported as unsupported): <c>caseLevel</c>,
/// <c>numericOrdering</c>, <c>hiraganaQuaternary</c>.</para>
/// </summary>
public sealed record CollationOptions(
    CollationStrength Strength = CollationStrength.Tertiary,
    AlternateHandling Alternate = AlternateHandling.NonIgnorable,
    MaxVariable MaxVariable = MaxVariable.Punct,
    CaseFirst CaseFirst = CaseFirst.Off,
    bool BackwardsSecondary = false)
{
    /// <summary>The CLDR default: tertiary, non-ignorable, maxVariable punct, caseFirst off, forward secondaries.</summary>
    public static CollationOptions Default { get; } = new();

    /// <summary>The ISO/IEC 14651-style default STANDARD-COMPARE uses: four levels, variables shifted.</summary>
    public static CollationOptions Standard { get; } = new(CollationStrength.Quaternary, AlternateHandling.Shifted);

    /// <summary>True when only <see cref="Strength"/> and <see cref="Alternate"/> may differ from the defaults —
    /// the configurations the fast paths and the shared caches are keyed on.</summary>
    public bool IsPlain => MaxVariable == MaxVariable.Punct && CaseFirst == CaseFirst.Off && !BackwardsSecondary;
}

/// <summary>
/// A configured comparison over one <see cref="CollationTable"/>: the table (root or tailored) and the
/// <see cref="CollationOptions"/> (strength, alternate handling, maxVariable, caseFirst, backwards secondaries).
/// Immutable and thread-safe; <see cref="With"/> derives variants. <see cref="Root"/> is the CLDR-default
/// configuration (root table, tertiary, non-ignorable); <see cref="CollationEngine"/> is the static façade that hands
/// out the standard configurations (root, ISO/IEC 14651-style, per-locale).
/// <para>Comparison is STREAMED level by level (UTS #10 S3/S4 without materializing the key): level 1 walks both
/// strings' collation elements comparing the non-zero primaries; only on a tie is level 2 walked, and so on — the
/// common unequal-at-primary case costs one pass and no allocation. <see cref="GetKey"/> materializes the same
/// weights into a <see cref="CollationKey"/> for callers that compare one string many times (a SORT, an index).</para>
/// </summary>
public sealed class Collator
{
    private static readonly Lazy<Collator> s_root = new(() => new Collator(CollationTable.Root), LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>The CLDR root collation at its defaults: root table, <see cref="CollationStrength.Tertiary"/>,
    /// <see cref="AlternateHandling.NonIgnorable"/> — the order ICU and CLDR give an untailored locale.</summary>
    public static Collator Root => s_root.Value;

    // The primary ranges (table scale) that decide "variable" when MaxVariable is not the table's own marking (Punct):
    // Space narrows the marked set to the space group; Symbol / Currency widen it by the symbol (and currency) groups.
    private readonly (int First, int Last)[] _variableRanges;
    private readonly bool _variableByFlag;       // MaxVariable.Punct — the element's own flag is the answer

    public Collator(CollationTable table, CollationStrength strength = CollationStrength.Tertiary,
        AlternateHandling alternate = AlternateHandling.NonIgnorable)
        : this(table, new CollationOptions(strength, alternate))
    {
    }

    public Collator(CollationTable table, CollationOptions options)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(options);
        if (options.Strength is < CollationStrength.Primary or > CollationStrength.Identical)
            throw new ArgumentOutOfRangeException(nameof(options), options.Strength, "collation strength must be 1 (primary) … 5 (identical)");
        Table = table;
        Options = options;
        _variableByFlag = options.MaxVariable == MaxVariable.Punct;
        _variableRanges = _variableByFlag ? [] : VariableRanges(table, options.MaxVariable);
    }

    /// <summary>The table the comparison reads.</summary>
    public CollationTable Table { get; }

    /// <summary>The settings.</summary>
    public CollationOptions Options { get; }

    /// <summary>How many weight levels decide the order.</summary>
    public CollationStrength Strength => Options.Strength;

    /// <summary>How variable (space/punctuation/symbol) elements take part.</summary>
    public AlternateHandling Alternate => Options.Alternate;

    /// <summary>A collator differing from this one in the given settings only.</summary>
    public Collator With(CollationStrength? strength = null, AlternateHandling? alternate = null, CollationTable? table = null,
        MaxVariable? maxVariable = null, CaseFirst? caseFirst = null, bool? backwardsSecondary = null) =>
        new(table ?? Table, Options with
        {
            Strength = strength ?? Options.Strength,
            Alternate = alternate ?? Options.Alternate,
            MaxVariable = maxVariable ?? Options.MaxVariable,
            CaseFirst = caseFirst ?? Options.CaseFirst,
            BackwardsSecondary = backwardsSecondary ?? Options.BackwardsSecondary,
        });

    /// <summary>A collator over the same table with other settings.</summary>
    public Collator With(CollationOptions options) => new(Table, options);

    /// <summary>Three-way comparison; null is the empty string. Returns &lt;0, 0 or &gt;0.</summary>
    public int Compare(string? a, string? b) => Compare(a.AsSpan(), b.AsSpan());

    /// <summary>Three-way comparison of two texts under this collator's table and settings.
    /// Canonically equivalent texts compare equal (the text is walked in NFD when it holds a combining mark);
    /// at <see cref="CollationStrength.Identical"/> the NFD code point sequence breaks every remaining tie.</summary>
    public int Compare(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        // Fast path: identical code units are identical at every level.
        if (a.SequenceEqual(b)) return 0;
        bool forIdentical = Strength == CollationStrength.Identical;
        if (Normalizer.NeedsNfd(a, Table, forIdentical)) a = Normalizer.ToNfd(a, Table);
        if (Normalizer.NeedsNfd(b, Table, forIdentical)) b = Normalizer.ToNfd(b, Table);

        // Identical-prefix skip: an identical prefix yields identical elements at every level, so the walks may
        // start at its end — provided the boundary is CONTEXT-SAFE (see SafeBoundary). Under Shifted the level-4
        // "following a variable" state depends on the prefix, and under backwards secondaries the level-2 sequence
        // is compared from its END (the prefix's weights are compared last, after the differing tail — a shorter
        // tail then meets the prefix's weights, not "end of text"), so the skip is taken only for the state-free
        // forward non-ignorable collators (every default locale sequence; STANDARD-COMPARE's arguments are short).
        int skip = Alternate == AlternateHandling.NonIgnorable && !Options.BackwardsSecondary ? SafeBoundary(a, b, a.CommonPrefixLength(b)) : 0;
        ReadOnlySpan<char> ta = a[skip..], tb = b[skip..];

        int c = CompareLevel(ta, tb, 1);
        if (c != 0 || Strength == CollationStrength.Primary) return c;
        c = Options.BackwardsSecondary ? CompareLevelBackwards(ta, tb) : CompareLevel(ta, tb, 2);
        if (c != 0 || Strength == CollationStrength.Secondary) return c;
        c = CompareLevel(ta, tb, 3);
        if (c != 0 || Strength == CollationStrength.Tertiary) return c;
        if (Alternate == AlternateHandling.Shifted)
        {
            c = CompareLevel(ta, tb, 4);
            if (c != 0) return c;
        }
        if (Strength == CollationStrength.Quaternary) return 0;
        return CompareCodePoints(ta, tb);   // Identical: the canonically decomposed code point sequences
    }

    /// <summary>Back the common-prefix length <paramref name="p"/> of two (normalized) texts up to a boundary at
    /// which the element sequences on either side cannot depend on each other: not inside a surrogate pair, between
    /// two STARTERS (a combining-mark run is never split — a discontiguous contraction may reach across marks), and
    /// with no contraction START within <see cref="CollationTable.MaxContractionLength"/> code points before it (a
    /// contraction, contiguous or discontiguous, extends forward from its first code point only). Returns 0 when no
    /// safe boundary exists in the prefix.</summary>
    private int SafeBoundary(ReadOnlySpan<char> a, ReadOnlySpan<char> b, int p)
    {
        if (p == 0) return 0;
        var t = Table;
        while (p > 0)
        {
            // Never split a surrogate pair.
            if (char.IsHighSurrogate(a[p - 1]) && p < a.Length) { p--; continue; }
            // Between two starters: the char before p and the (first differing) chars at p in BOTH texts.
            if (t.IsNonStarter(CodePointBefore(a, p)) || (p < a.Length && t.IsNonStarter(CodePointAt(a, p))) || (p < b.Length && t.IsNonStarter(CodePointAt(b, p))))
            { p--; continue; }
            // No contraction may start within reach of the boundary.
            bool clear = true;
            int i = p, remaining = t.MaxContractionLength - 1;
            while (remaining-- > 0 && i > 0)
            {
                int cp = CodePointBefore(a, i);
                if (t.StartsContraction(cp)) { clear = false; break; }
                i -= cp > 0xFFFF ? 2 : 1;
            }
            if (clear) return p;
            p--;
        }
        return 0;
    }

    private static int CodePointBefore(ReadOnlySpan<char> s, int end)
    {
        char c = s[end - 1];
        return end >= 2 && char.IsLowSurrogate(c) && char.IsHighSurrogate(s[end - 2]) ? char.ConvertToUtf32(s[end - 2], c) : c;
    }

    private static int CodePointAt(ReadOnlySpan<char> s, int index)
    {
        char c = s[index];
        return char.IsHighSurrogate(c) && index + 1 < s.Length && char.IsLowSurrogate(s[index + 1]) ? char.ConvertToUtf32(c, s[index + 1]) : c;
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
    /// SAME collator with <see cref="CollationKey.CompareTo"/>; the order equals <see cref="Compare(string?,string?)"/>.
    /// (Under <see cref="CollationOptions.BackwardsSecondary"/> the level-2 weights are stored reversed, so the key
    /// order stays a plain level-by-level comparison.)</summary>
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
            if (level == 2 && Options.BackwardsSecondary) weights.Reverse();
            perLevel[level - 1] = weights.ToArray();
        }
        return new CollationKey(perLevel, Strength == CollationStrength.Identical ? walked : null, this);
    }

    /// <summary>The key of <paramref name="text"/> through this collator's <see cref="Cache.CollationKeyCache"/> — the
    /// same key as <see cref="GetKey"/>, built once per distinct text.</summary>
    public CollationKey GetKeyCached(string? text) => Cache.CollationKeyCache.For(this).GetKey(text);

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

    /// <summary>Level 2 compared from the END of each text (UTS #35 <c>backwards</c> level 2 — Canadian French,
    /// where the LAST accent difference decides): the non-zero level-2 weights of both texts are gathered into pooled
    /// buffers and compared right to left, a shorter sequence still sorting first when it is a (reversed) prefix.</summary>
    private int CompareLevelBackwards(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        int[] bufA = ArrayPool<int>.Shared.Rent(a.Length + 8), bufB = ArrayPool<int>.Shared.Rent(b.Length + 8);
        try
        {
            int na = Gather(a, ref bufA), nb = Gather(b, ref bufB);
            int i = na - 1, j = nb - 1;
            while (i >= 0 && j >= 0)
            {
                int wa = bufA[i--], wb = bufB[j--];
                if (wa != wb) return wa < wb ? -1 : 1;
            }
            return i >= 0 ? 1 : j >= 0 ? -1 : 0;
        }
        finally
        {
            ArrayPool<int>.Shared.Return(bufA);
            ArrayPool<int>.Shared.Return(bufB);
        }

        int Gather(ReadOnlySpan<char> text, ref int[] buffer)
        {
            var it = new CollationElementIterator(text, Table);
            bool afterVariable = false;
            int n = 0;
            while (it.TryNext(out var ce))
            {
                int w = WeightAt(ce, 2, ref afterVariable);
                if (w == 0) continue;
                if (n == buffer.Length)
                {
                    var bigger = ArrayPool<int>.Shared.Rent(buffer.Length * 2);
                    buffer.AsSpan(0, n).CopyTo(bigger);
                    ArrayPool<int>.Shared.Return(buffer);
                    buffer = bigger;
                }
                buffer[n++] = w;
            }
            return n;
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

    /// <summary>Is the element VARIABLE under this collator's <see cref="MaxVariable"/>: its own marking (spaces and
    /// punctuation) for the default, else membership of the chosen groups' primary ranges.</summary>
    private bool IsVariable(in CollationElement element)
    {
        if (_variableByFlag) return element.IsVariable;
        if (element.Primary == 0) return false;
        if (Options.MaxVariable == MaxVariable.Space) return element.IsVariable && InVariableRange(element.Primary);
        return element.IsVariable || InVariableRange(element.Primary);
    }

    private bool InVariableRange(int primary)
    {
        foreach (var (first, last) in _variableRanges)
            if (primary >= first && primary <= last) return true;
        return false;
    }

    private static (int First, int Last)[] VariableRanges(CollationTable table, MaxVariable maxVariable)
    {
        var wanted = maxVariable switch
        {
            MaxVariable.Space => new[] { "space" },
            MaxVariable.Symbol => new[] { "symbol" },
            MaxVariable.Currency => new[] { "symbol", "currency" },
            _ => [],
        };
        var ranges = new List<(int, int)>();
        foreach (string code in wanted)
            if (table.TryGetReorderGroup(code, out var g)) ranges.Add((g.FirstPrimary, g.LastPrimary));
        return ranges.ToArray();
    }

    /// <summary>The weight of <paramref name="element"/> at <paramref name="level"/> under this collator's settings —
    /// UTS #10 Table 12 for <see cref="AlternateHandling.Shifted"/>: a variable element moves its primary to level 4
    /// and is 0 at levels 1–3; a primary-ignorable element FOLLOWING a variable one is 0 everywhere; every other
    /// element keeps its three weights and takes the maximum ("no variable here") at level 4. Under
    /// <see cref="AlternateHandling.NonIgnorable"/> the three weights are used as they are and level 4 is unused (0).
    /// With <see cref="CaseFirst"/> on, the level-3 weight is prefixed by the case bit (upper-first: uppercase 0,
    /// other 1; lower-first: the reverse) so case decides before width/compat/font/circled variants.</summary>
    internal int WeightAt(in CollationElement element, int level, ref bool afterVariable)
    {
        if (Alternate == AlternateHandling.Shifted)
        {
            if (IsVariable(element))
            {
                afterVariable = true;
                return level == 4 ? element.Primary : 0;
            }
            if (element.IsCompletelyIgnorable) return 0;
            if (element.Primary == 0)
            {
                if (afterVariable) return 0;
                return level switch { 2 => element.Secondary, 3 => Tertiary(element), 4 => Table.MaxPrimary, _ => 0 };
            }
            afterVariable = false;
            return level switch { 1 => element.Primary, 2 => element.Secondary, 3 => Tertiary(element), 4 => Table.MaxPrimary, _ => 0 };
        }
        return level switch { 1 => element.Primary, 2 => element.Secondary, 3 => Tertiary(element), _ => 0 };
    }

    /// <summary>The level-3 weight with the case bits in front when <see cref="CaseFirst"/> is on: upper-first ranks
    /// Upper 0, Mixed 1, Lower 2 (ICU's inverted case bits); lower-first ranks Lower 0, Mixed 1, Upper 2.</summary>
    private int Tertiary(in CollationElement element)
    {
        int t = element.Tertiary;
        if (t == 0 || Options.CaseFirst == CaseFirst.Off) return t;
        int rank = Options.CaseFirst == CaseFirst.Upper ? 2 - (int)element.Case : (int)element.Case;
        return (rank << 16) | t;
    }
}
