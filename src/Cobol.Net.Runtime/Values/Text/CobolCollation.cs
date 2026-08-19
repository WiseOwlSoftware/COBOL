// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime;

/// <summary>
/// THE ONE collating-sequence carrier (DESIGN-locale-facility §4.4.1; kb/Work PB101): every consumer of a COBOL
/// collating sequence — relation and condition-name comparisons (ISO §8.8.4.2.7/.9), SORT/MERGE keys (§14.9.40 GR5),
/// indexed-file keys (§12.4.5.7), MAX/MIN (§15.61/§15.63), ORD/CHAR (§15.70/§15.15) and the HIGH-VALUE/LOW-VALUE
/// extremes (§8.3.3.6.4 GR6/GR7) — takes one of these, and the compiler emits one per declared sequence
/// (<c>__COLLATE</c> for the alphanumeric program collating sequence, <c>__COLLATE_NAT</c> for the national one, an
/// inline instance for a statement or file alphabet). The arms:
/// <list type="bullet">
/// <item><see cref="AlphanumericCollation"/> — an <c>ALPHABET</c> literal phrase over the alphanumeric set (a 256-entry
/// position table, arithmetic above it — §12.3.7.4 GR7 1.3).</item>
/// <item><see cref="NationalCollation"/> — its <c>FOR NATIONAL</c> twin (sparse over the 65,536 code units).</item>
/// <item><see cref="LocaleCollation"/> — a LOCALE-based sequence (§8.8.4.2.11) or an <c>ORDER TABLE</c>: the derived
/// CLDR/UCA collation engine of <c>Runtime/Collation/</c>.</item>
/// </list>
/// The NATIVE sequence has no carrier: it is the two-argument <see cref="CobolString.Compare(string?,string?,char)"/>
/// and a <c>null</c> in every optional collation slot, so an ordinary program's generated text is unchanged by the
/// existence of this class. Before PB101 the runtime had three <c>CobolString.Compare</c> overloads (a <c>char</c>
/// pad, a raw <c>ushort[]</c>, a <c>NationalCollation</c>) and every emitter re-chose among them; a fourth arm could
/// not be added without a fourth overload at every site — this class is the collapse.
/// </summary>
public abstract class CobolCollation
{
    /// <summary>Compare two character values under this sequence: &lt;0, 0, &gt;0. Each arm applies ITS OWN
    /// operand rule — the table arms space-extend the shorter operand (§8.8.4.2.1, the pad itself weighed through
    /// the sequence), the locale arm truncates trailing spaces (§8.8.4.2.11) — so a caller never pads or trims.</summary>
    public abstract int Compare(string? left, string? right);

    /// <summary>The 0-based collating position of <paramref name="c"/> — the ORD arithmetic (§15.70.4 r1).</summary>
    public abstract int Weight(char c);

    /// <summary>The number of collating positions the sequence defines (§15.15.3 r2's domain bound for CHAR).</summary>
    public abstract int PositionCount { get; }

    /// <summary>The character at 0-based <paramref name="position"/> — the CHAR inverse (§15.15.4 r2: the FIRST
    /// character defined for a shared position), or −1 outside the sequence.</summary>
    public abstract int CharAt(long position);

    /// <summary>The HIGH-VALUE character of the sequence (§8.3.3.6.4 GR6 — the highest ordinal position).</summary>
    public abstract char HighValue { get; }

    /// <summary>The LOW-VALUE character of the sequence (§8.3.3.6.4 GR7 — the lowest ordinal position).</summary>
    public abstract char LowValue { get; }

    /// <summary>Membership of <paramref name="read"/> in the THROUGH range [<paramref name="lo"/>, <paramref name="hi"/>]
    /// under this sequence (ISO §14.7.8; a level-88 VALUE THRU or an EVALUATE WHEN range). When <paramref name="lo"/>
    /// collates AFTER <paramref name="hi"/> (rule 2) the nonfatal EC-RANGE-INVALID is set and the range is treated as
    /// EMPTY (false); otherwise the inclusive bound test. ONE implementation for every arm — the three copies the
    /// overload set used to need are gone.</summary>
    public bool ThruMember(string? read, string? lo, string? hi)
    {
        if (Compare(lo, hi) > 0) { ExceptionState.Set("EC-RANGE-INVALID", fatal: false); return false; }
        return Compare(read, lo) >= 0 && Compare(read, hi) <= 0;
    }
}
