// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// ⛔ THE ONE LAYOUT OF A VARIABLE-LENGTH RECORD'S CONTIGUOUS IMAGE (docs/CONFORMANCE.md §3 determinations D-FRA
/// and D-KWV; kb/Work PB981, PB1025). A WRITE / RELEASE sends a variable-length record "as though it were in fact
/// contiguous with its neighbors" (ISO §8.5.1.11.2), so a record read back — or a record held in a sort store or
/// an indexed file — carries no marker of where a dynamic member ends. This object is that record type's
/// component layout, emitted ONCE per record type beside its <c>FromContiguousImage</c>, and it answers the two
/// questions every reader of such an image asks with ONE take step:
/// <list type="bullet">
/// <item><see cref="Decompose"/> — the whole record back into its carrier (the READ / RETURN half);</item>
/// <item><see cref="Position"/> — where a FIXED member (a SORT/MERGE key, an indexed RECORD KEY) sits in THIS
/// record, which varies from record to record when a dynamic member precedes it.</item>
/// </list>
/// Walking the components left to right, each takes as many whole units (one character of a dynamic-length item,
/// one element of a dynamic-capacity table) as the record holds beyond the FIXED material still to come, up to its
/// maximum — so both answers agree by construction, and a key is found exactly where the decomposition puts it.
/// </summary>
/// <param name="FixedTotal">The width of the FIXED run — the record with every variable-length component
/// collapsed to nothing (the §8.5.1.12.3 zero-length accounting).</param>
/// <param name="FixedAt">Component k's offset in the FIXED run.</param>
/// <param name="Unit">Component k's unit width in characters.</param>
/// <param name="MaxUnits">Component k's maximum size in units (§8.5.1.10.1 / the table's maximum capacity).</param>
public sealed class CobolContiguousLayout(int FixedTotal, int[] FixedAt, int[] Unit, long[] MaxUnits)
{
    /// <summary>The record decomposed into its carrier — <see cref="CobolVarGroup.FromContiguous"/>'s rule.</summary>
    public CobolVarGroup Decompose(string record) =>
        CobolVarGroup.FromContiguous(record, FixedTotal, FixedAt, Unit, MaxUnits);

    /// <summary>The character position, in <paramref name="record"/>, of the fixed material at
    /// <paramref name="fixedOffset"/> of the FIXED run: that offset plus what every variable-length component
    /// PRECEDING it took from this record. A component at <c>FixedAt[k] ≤ fixedOffset</c> precedes the member —
    /// a fixed member cannot start where a following component starts, because it occupies at least one position
    /// of the fixed run first.</summary>
    public int Position(string record, int fixedOffset)
    {
        long excess = Math.Max(0, (record?.Length ?? 0) - FixedTotal);
        long at = fixedOffset;
        for (int k = 0; k < FixedAt.Length && FixedAt[k] <= fixedOffset; k++)
            at += CobolVarGroup.ContiguousTake(ref excess, Unit[k], MaxUnits[k]);
        return (int)Math.Min(int.MaxValue, at);
    }
}
