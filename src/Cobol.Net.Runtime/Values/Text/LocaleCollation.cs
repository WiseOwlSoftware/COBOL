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

    /// <summary>The <c>IS LOCALE</c> phrase without a locale-name: the run unit's current LC_COLLATE locale at each use.</summary>
    public static LocaleCollation Current { get; } = new(null);

    /// <summary>A sequence bound to one locale (a SPECIAL-NAMES locale-name's L1-normalized tag — "" IS the root,
    /// a locale like any other, e.g. <c>LOCALE INV IS "INVARIANT"</c> — or an ORDER TABLE locale tag); NULL = the
    /// current locale at each use. ⚠ "" and null differ: a sequence bound to the root stays the root after a SET
    /// LOCALE; the current-locale form follows it.</summary>
    public LocaleCollation(string? localeTag)
    {
        _localeTag = localeTag?.Trim();
    }

    /// <summary>The bound locale tag, or null for "the current locale at each use".</summary>
    public string? LocaleTag => _localeTag;

    /// <summary>The collator this sequence uses right now (re-resolved per use for the current-locale form). A NAMED
    /// locale that is not available in this operating environment is EC-LOCALE-MISSING at the point of use (§8.2.1;
    /// DESIGN-locale-facility L1 item 4 — availability is a run-time property, the compiler never resolved it);
    /// with checking off the sequence answers the locale's nearest available order (the CLDR parent chain / root).</summary>
    public Collator Resolve()
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
    /// processing of that SORT or MERGE statement": the current-locale form frozen to the locale current NOW (the
    /// statement's start; a SET LOCALE in the input procedure must not move the sort's sequence); a named form is
    /// already fixed and returns itself.</summary>
    public override CobolCollation Snapshot() =>
        _localeTag is null ? new LocaleCollation(RunUnit.Current.Locale.Current(LocaleCategory.Collate)) : this;

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

    /// <summary>The engine's key of the operand (§8.8.4.2.11-trimmed) through the <see cref="CollationKeyCache"/> of the
    /// collator in effect — what SORT/MERGE and the indexed-file connector compare instead of walking the elements
    /// again for every comparison. An ill-formed operand sets EC-LOCALE-INCOMPATIBLE like <see cref="Compare"/> does.</summary>
    public override CollationKey KeyOf(string? value)
    {
        string trimmed = TrimForLocale(value);
        if (!Collator.IsWellFormed(trimmed))
            ExceptionState.LocaleIncompatibleError("a locale-based key over an operand the locale's collating sequence does not order — an ill-formed UTF-16 operand (ISO §8.8.4.2.11)");   // L6
        return CollationKeyCache.For(Resolve()).GetKey(trimmed);
    }

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
