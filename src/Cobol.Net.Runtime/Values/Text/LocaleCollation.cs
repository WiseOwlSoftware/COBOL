// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Concurrent;
using CobolNet.Runtime.Collation;
using CobolNet.Runtime.Collation.Cache;
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime;

/// <summary>
/// The LOCALE-based collating sequence (ISO/IEC 1989:2023 §8.8.4.2.11; DESIGN-locale-facility §4.4.3, kb/Work
/// PB101): an <c>ALPHABET … IS LOCALE [locale-name]</c> made the program collating sequence, a SORT/MERGE or file
/// key sequence. Comparison is the derived CLDR/UCA engine (<c>Runtime/Collation/</c>) for the locale — the one
/// NAMED at construction, or, for the phrase without a locale-name, the run unit's CURRENT LC_COLLATE locale
/// resolved AT EACH USE (§12.3.7.4 GR7e: "*otherwise by the locale that is current at the time the collating
/// sequence is used at runtime*"; §12.3.6.4 GR11/GR12).
/// <para>The §8.8.4.2.11 operand rule is applied HERE, not by callers: "*trailing spaces are truncated from the
/// operands except that an operand consisting of all spaces is truncated to a single space*" — no space padding —
/// then "*comparison proceeds by the algorithm associated with the collating sequence defined by category
/// LC_COLLATE from the current locale. This may be a culturally-sensitive comparison, and is not necessarily
/// performed character-by-character*"; two zero-length operands are equal (they both trim to "").</para>
/// <para><b>⚖ DETERMINATION L6 (re-derived over the derived table):</b> the table orders every well-formed UTF-16
/// text (assigned, unassigned and noncharacter code points alike take explicit or implicit weights), so the one
/// case in which "*the locale does not define a collating sequence for all characters of the operands*" is an
/// operand that is not well-formed — an unpaired surrogate. That sets EC-LOCALE-INCOMPATIBLE (fatal, §14.6.13.1.6);
/// the comparison still returns a deterministic order.</para>
/// <para><b>⚖ DETERMINATION L11:</b> the algorithm "associated with LC_COLLATE" is the CLDR default for the locale —
/// the locale's tailoring over the root table at TERTIARY strength with NON-IGNORABLE variables (case and accents
/// distinguish; punctuation weighs at level 1), the order ICU/CLDR give <c>strcoll</c>-style comparison. The four-level
/// ISO/IEC 14651 default ordering is what STANDARD-COMPARE provides.</para>
/// <para><b>⚖ DETERMINATION L7 (ORD/CHAR, HIGH-VALUE/LOW-VALUE):</b> a locale sequence is an algorithm, not a
/// position table, so its positions are MATERIALIZED once per resolved collator: the 65,536 native code units
/// sorted by the collator (ties by code unit); ORD is the rank + 1, CHAR the lowest-coded member of a rank,
/// PositionCount the number of distinct ranks (characters the locale collates equally share a position — the case
/// §15.15.4 r2 anticipates). HIGH-VALUE is the highest-coded member of the last rank (U+FFFF under every CLDR
/// table — its primary is the maximum), LOW-VALUE the lowest-coded member of the first (U+0000, completely
/// ignorable) — §8.3.3.6.4 GR6/GR7 as run-time values.</para>
/// </summary>
public sealed class LocaleCollation : CobolCollation
{
    private static readonly ConcurrentDictionary<Collator, OrderVector> s_orders = new();

    private readonly string? _localeTag;

    /// <summary>True for a <see cref="Snapshot"/>: the sequence resolves its collator ONCE, at its first use, and keeps
    /// it (<see cref="_pinned"/>).</summary>
    private readonly bool _pins;

    /// <summary>A snapshot's collator, set by its first <see cref="Resolve"/>; null before it and for every sequence that
    /// is not a snapshot.</summary>
    private Collator? _pinned;

    /// <summary>The <c>IS LOCALE</c> phrase without a locale-name: the run unit's current LC_COLLATE locale at each use.</summary>
    public static LocaleCollation Current { get; } = new(null);

    /// <summary>A sequence bound to one locale (a SPECIAL-NAMES locale-name's L1-normalized tag — "" IS the root,
    /// a locale like any other, e.g. <c>LOCALE INV IS "INVARIANT"</c> — or an ORDER TABLE locale tag); NULL = the
    /// current locale at each use. ⚠ "" and null differ: a sequence bound to the root stays the root after a SET
    /// LOCALE; the current-locale form follows it.</summary>
    public LocaleCollation(string? localeTag) : this(localeTag, pins: false) { }

    private LocaleCollation(string? localeTag, bool pins)
    {
        _localeTag = localeTag?.Trim();
        _pins = pins;
    }

    /// <summary>The bound locale tag, or null for "the current locale at each use".</summary>
    public string? LocaleTag => _localeTag;

    /// <summary>The collator this sequence uses right now (re-resolved per use for the current-locale form). A NAMED
    /// locale that is not available in this operating environment is EC-LOCALE-MISSING at the point of use (§8.2.1;
    /// DESIGN-locale-facility L1 item 4 — availability is a run-time property, the compiler never resolved it);
    /// with checking off the sequence answers the locale's nearest available order (the CLDR parent chain / root).
    /// <para>A <see cref="Snapshot"/> resolves at its first use and answers that collator from then on, so every key it
    /// builds comes from ONE collator: the engine's caches can be dropped between two uses (<c>LocaleManager.ClearCache</c>,
    /// which makes the next resolution a new collator), and <see cref="CollationKey.CompareTo"/> refuses keys of two.</para></summary>
    public Collator Resolve()
    {
        if (_pins) return _pinned ?? Interlocked.CompareExchange(ref _pinned, ResolveNow(), null) ?? _pinned;
        return ResolveNow();
    }

    private Collator ResolveNow()
    {
        if (_localeTag is { } tag)
        {
            if (!LocaleIdentification.IsAvailable(tag))
                ExceptionState.LocaleMissingError($"the locale '{tag}' of the IS LOCALE collating sequence is not available in this operating environment (ISO §8.2.1 / §12.3.7.4 GR5)");
            return CollationEngine.ForLocale(tag);
        }
        return CollationEngine.ForLocale(RunUnit.Current.Locale.Current(LocaleCategory.Collate));
    }

    /// <summary>§14.6.6 r5 — "A locale switch during execution of a SORT or MERGE statement has no effect on the
    /// processing of that SORT or MERGE statement" — and §12.4.5.7.4 GR1 — "Each collating sequence is a fixed file
    /// attribute": the sequence frozen for one statement or one open file. The current-locale form is bound to the
    /// locale current NOW (a SET LOCALE in the input procedure, or between two statements on the open file, must not
    /// move the order), and either form PINS its collator at its first use (<see cref="Resolve"/>), so the keys of one
    /// statement or one open file are all one collator's. A snapshot is its own snapshot.</summary>
    public override CobolCollation Snapshot() =>
        _pins ? this : new LocaleCollation(_localeTag ?? RunUnit.Current.Locale.Current(LocaleCategory.Collate), pins: true);

    /// <inheritdoc/>
    public override int Compare(string? left, string? right)
    {
        ReadOnlySpan<char> a = TrimForLocale(left.AsSpan()), b = TrimForLocale(right.AsSpan());
        if (!Collator.IsWellFormed(a) || !Collator.IsWellFormed(b))
            ExceptionState.LocaleIncompatibleError("a locale-based comparison over an operand the locale's collating sequence does not order — an ill-formed UTF-16 operand (ISO §8.8.4.2.11)");   // L6
        return Resolve().Compare(a, b);
    }

    /// <inheritdoc/>
    public override bool SupportsKeys => true;

    /// <summary>A locale-based comparison (§8.8.4.2.7: <i>"Locale-based comparison is used when the alphanumeric program
    /// collating sequence in effect is locale based"</i>; §8.8.4.2.11): the engine's multi-level comparison, never
    /// position by position.</summary>
    internal override bool UsesStandardComparison => false;

    /// <summary>The engine's key of the operand (§8.8.4.2.11-trimmed) through the <see cref="CollationKeyCache"/> of the
    /// collator in effect — what SORT/MERGE and the indexed-file connector compare instead of walking the elements
    /// again for every comparison. An ill-formed operand sets EC-LOCALE-INCOMPATIBLE like <see cref="Compare"/> does.</summary>
    public override CollationKey KeyOf(string? value) => KeyOf(value, CollationKeyCache.For(Resolve()));

    /// <summary>Two operands' keys built by ONE resolution of the sequence (<see cref="CobolCollation.CompareByKeys"/>):
    /// a key cache the process drops between two lookups (<c>LocaleManager.ClearCache</c>) can then never hand the
    /// comparison keys of two collators, which <see cref="CollationKey.CompareTo"/> refuses.</summary>
    internal override int CompareByKeys(string? left, string? right)
    {
        var cache = CollationKeyCache.For(Resolve());
        return KeyOf(left, cache).CompareTo(KeyOf(right, cache));
    }

    private static CollationKey KeyOf(string? value, CollationKeyCache cache)
    {
        string trimmed = TrimForLocale(value);
        if (!Collator.IsWellFormed(trimmed))
            ExceptionState.LocaleIncompatibleError("a locale-based key over an operand the locale's collating sequence does not order — an ill-formed UTF-16 operand (ISO §8.8.4.2.11)");   // L6
        return cache.GetKey(trimmed);
    }

    /// <summary>⛔ The prefix shape of a stored key (<see cref="CobolCollation.PrefixShapeOf"/>) over the operand
    /// §8.8.4.2.11 compares — <i>"trailing spaces are truncated from the operands except that an operand consisting of
    /// all spaces is truncated to a single space"</i> — so a key's padding never counts: a truncation that reaches into
    /// it leaves the whole trimmed key, whose weights are all the order's. A tailoring with a contraction that holds a
    /// space would let the trimming of a truncation change the mapping before it, so under one the shape is the key's
    /// whole length, which gives every partial key the empty bound: GR17's walk as written.</summary>
    internal override PrefixShape PrefixShapeOf(string value)
    {
        var collator = Resolve();
        return PadOf(collator).InContraction ? new PrefixShape(value.Length, 0) : collator.PrefixShapeOf(TrimForLocale(value.AsSpan()));
    }

    /// <summary>⛔ The bound of a partial key (<see cref="CobolCollation.PrimaryBound"/>): the first level-1 weights of the
    /// prefix's own key, at most <paramref name="margin"/>, and none from the first one that weighs less than a space
    /// on. That cut is what the trimming asks: a stored key's truncation that ends in spaces of the key's interior loses
    /// them to §8.8.4.2.11, so its weights are the stored key's up to them and then the prefix-shorter ones; the
    /// stored key's own space weights then meet the bound, and a bound weight below a space's would order such a key
    /// ABOVE the bound while its truncation (a proper prefix of the probe at level 1) is below the probe.</summary>
    internal override int[] PrimaryBound(string prefix, int margin)
    {
        if (margin <= 0) return [];
        var primary = KeyOf(prefix).Level(1);
        int floor = PadOf(Resolve()).Floor, n = Math.Min(margin, primary.Length);
        for (int i = 0; i < n; i++)
            if (primary[i] < floor)
            {
                n = i;
                break;
            }
        return primary[..n].ToArray();
    }

    /// <inheritdoc/>
    internal override int ComparePrimary(string value, int[] bound)
    {
        if (bound.Length == 0) return 0;
        var primary = KeyOf(value).Level(1);
        return primary[..Math.Min(primary.Length, bound.Length)].SequenceCompareTo(bound);
    }

    private static readonly ConcurrentDictionary<Collator, Pad> s_pads = new();

    private static Pad PadOf(Collator collator) => s_pads.GetOrAdd(collator, static c => new Pad(
        c.Table.Contractions().Any(x => x.CodePoints.Contains(' ')),
        c.GetKey(" ").Primary.DefaultIfEmpty(0).Max()));

    /// <summary>What the §8.8.4.2.11 trimming needs to know of one collator's SPACE: whether a contraction holds one,
    /// and the largest level-1 weight a space has (0 when it has none, as under shifted variables).</summary>
    private sealed record Pad(bool InContraction, int Floor);

    /// <summary>§8.8.4.2.11 sentence 1: trailing spaces off; an all-space operand becomes ONE space; "" stays "".
    /// A SPAN, so a fixed-width (space-padded) operand costs no substring per comparison.</summary>
    internal static ReadOnlySpan<char> TrimForLocale(ReadOnlySpan<char> s)
    {
        int end = s.Length;
        while (end > 0 && s[end - 1] == ' ') end--;
        if (end == 0) return s.Length == 0 ? s : " ";
        return s[..end];
    }

    /// <inheritdoc cref="TrimForLocale(ReadOnlySpan{char})"/>
    internal static string TrimForLocale(string? s)
    {
        var t = TrimForLocale(s.AsSpan());
        return t.Length == (s?.Length ?? 0) ? s ?? "" : t.ToString();
    }

    /// <inheritdoc/>
    public override int Weight(char c) => Order().Rank[c];

    /// <inheritdoc/>
    public override int PositionCount => Order().Distinct;

    /// <inheritdoc/>
    public override int CharAt(long position)
    {
        var o = Order();
        return position < 0 || position >= o.Distinct ? -1 : o.FirstOfRank[position];
    }

    /// <inheritdoc/>
    public override char HighValue { get { var o = Order(); return (char)o.LastOfRank[o.Distinct - 1]; } }

    /// <inheritdoc/>
    public override char LowValue { get { var o = Order(); return (char)o.FirstOfRank[0]; } }

    private OrderVector Order() => s_orders.GetOrAdd(Resolve(), static c => OrderVector.Build(c));

    /// <summary>The materialized L7 positions of one collator over the 65,536 native code units.</summary>
    private sealed class OrderVector
    {
        public required int[] Rank { get; init; }          // code unit → 0-based position (equal-collating units share one)
        public required int[] FirstOfRank { get; init; }   // position → the LOWEST code unit collating there (§15.15.4 r2)
        public required int[] LastOfRank { get; init; }    // position → the HIGHEST code unit collating there (GR6 tie)
        public required int Distinct { get; init; }

        public static OrderVector Build(Collator collator)
        {
            const int N = 0x10000;
            var keys = new CollationKey[N];
            for (int c = 0; c < N; c++) keys[c] = collator.GetKey(((char)c).ToString());
            var order = new int[N];
            for (int c = 0; c < N; c++) order[c] = c;
            Array.Sort(order, (x, y) =>
            {
                int k = keys[x].CompareTo(keys[y]);
                return k != 0 ? k : x.CompareTo(y);        // ties by code unit — the deterministic §15.15.4 r2 duty
            });
            var rank = new int[N];
            var first = new List<int>(N);
            var last = new List<int>(N);
            int r = -1;
            for (int i = 0; i < N; i++)
            {
                int c = order[i];
                if (i == 0 || keys[c].CompareTo(keys[order[i - 1]]) != 0)
                {
                    r++;
                    first.Add(c);
                    last.Add(c);
                }
                else last[r] = c;
                rank[c] = r;
            }
            return new OrderVector { Rank = rank, FirstOfRank = first.ToArray(), LastOfRank = last.ToArray(), Distinct = r + 1 };
        }
    }
}
