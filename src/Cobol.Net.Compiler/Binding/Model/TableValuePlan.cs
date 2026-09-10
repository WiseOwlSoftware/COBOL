// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics.CodeAnalysis;

namespace CobolNet.Binding.Model;

/// <summary>A Format-2 (table) VALUE SUBSCRIPT TUPLE — one occurrence number per dimension, in the order
/// ISO §13.18.63.3 SR20 fixes ("one subscript-1 specified for each OCCURS clause for the subject of the entry or
/// superordinate to that entry, specified in the same order as a subscripted reference to the subject of the entry
/// would be specified"): the MOST inclusive dimension first. A value type with structural equality so it can key
/// the <see cref="TableValuePlan.Literals"/> map.</summary>
public readonly struct Subscripts : IEquatable<Subscripts>
{
    private readonly int[] _v;

    public Subscripts(params int[] values) => _v = values;

    /// <summary>This tuple with <paramref name="next"/> appended — how an emitter descends one OCCURS level.</summary>
    public Subscripts With(int next)
    {
        var a = new int[Count + 1];
        for (int i = 0; i < Count; i++) a[i] = _v[i];
        a[Count] = next;
        return new Subscripts(a);
    }

    public int Count => _v?.Length ?? 0;

    public int this[int i] => _v[i];

    public bool Equals(Subscripts other)
    {
        if (Count != other.Count) return false;
        for (int i = 0; i < Count; i++) if (_v[i] != other._v[i]) return false;
        return true;
    }

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Subscripts s && Equals(s);

    public override int GetHashCode()
    {
        var h = new HashCode();
        for (int i = 0; i < Count; i++) h.Add(_v[i]);
        return h.ToHashCode();
    }

    public override string ToString() => Count == 0 ? "()" : "(" + string.Join(" ", _v) + ")";

    /// <summary>ODOMETER (= lexicographic) ORDER, the order §13.18.63.4 GR12 initializes table elements in: the
    /// LEAST inclusive (rightmost) subscript advances first and carries into the next MOST inclusive one, so a
    /// tuple's position in the fill sequence is exactly its lexicographic rank. §13.18.63.3 SR21's "the table
    /// element associated with subscript-2 is the same occurrence or a successive occurrence of the table element
    /// associated with the corresponding subscript-1" is therefore <c>Compare(to, from) &gt;= 0</c> — and it needs
    /// no dimension maxima, which matters because a DYNAMIC dimension may not have one.</summary>
    public static int Compare(Subscripts a, Subscripts b)
    {
        int n = Math.Min(a.Count, b.Count);
        for (int i = 0; i < n; i++) if (a[i] != b[i]) return a[i].CompareTo(b[i]);
        return a.Count.CompareTo(b.Count);
    }
}

/// <summary>ONE DIMENSION of a Format-2 (table) VALUE's subscript tuple: the entry whose OCCURS clause creates it
/// (<paramref name="Owner"/> — the subject of the entry itself, or an entry superordinate to it, ISO §13.18.63.3
/// SR20), and the maximum occurrence number the odometer counts to.
/// <para><paramref name="Max"/> is §13.18.63.4 GR12's "the maximum number of occurrences, or, in the case of a
/// dynamic-capacity table, the expected number of occurrences, specified by its corresponding OCCURS clause" —
/// NULL only for a DYNAMIC table declared without an OCCURS TO (expected) capacity, where the standard gives the
/// dimension no ceiling. SR22 forbids a no-TO VALUE over such a dimension and SR23 forbids the odometer from
/// carrying past it, so a null <paramref name="Max"/> is never a modulus the fill needs.</para></summary>
/// <param name="Dynamic">True when <paramref name="Owner"/> is a Format-4 DYNAMIC-capacity table (§13.18.38).</param>
public sealed record TableValueDim(DataItem Owner, int? Max, bool Dynamic)
{
    /// <summary>The §13.18.63.3 SR22/SR23 subject: "an OCCURS clause with a DYNAMIC phrase but no TO phrase".</summary>
    public bool DynamicWithoutTo => Dynamic && Max is null;
}

/// <summary>⛔ THE RESOLVED Format-2 (table) VALUE of one entry (ISO §13.18.63.4 GR12–GR15) — the SUBSCRIPT TUPLE →
/// LITERAL map both emit lanes read, built ONCE by the binder's <c>ResolveTableValues</c> pass where the complete
/// forest makes the OCCURS chain knowable.
///
/// <para><b>Why the binder and not the emitter.</b> The map is a pure function of the declared clause and the
/// entry's OCCURS chain — no emit context enters it — and the two lanes that consume it
/// (<c>ValueInitializer.FieldInit</c> for the record-struct fields, <c>GroupImageCodec.ImageInitOf</c> for the
/// character-image backings) must never disagree about a table element's value. Its predecessor lived in the
/// emitter, keyed by a SINGLE occurrence number, and could therefore express only a one-dimensional table on its
/// own OCCURS entry; every other shape §13.18.63.3 SR18/SR20/SR21/SR22/SR23 makes legal was refused by a staged
/// diagnostic (kb/Work PB505).</para></summary>
public sealed class TableValuePlan
{
    /// <summary>The subject's OCCURS chain, MOST inclusive first (ISO §13.18.63.3 SR20's order). Never empty —
    /// an entry with no dimension violates SR18 and never gets a plan.</summary>
    public required IReadOnlyList<TableValueDim> Dims { get; init; }

    /// <summary>The §13.18.63.4 GR12–GR15 fill: every table element the clause initializes, keyed by its full
    /// subscript tuple (<see cref="Dims"/>.Count entries). An element absent here is outside every FROM..TO range
    /// and takes the element default — §13.18.63.4 does not define its content.</summary>
    public required IReadOnlyDictionary<Subscripts, string> Literals { get; init; }

    /// <summary>The literal this clause gives the table element at <paramref name="subs"/>, or null when the
    /// element is outside every FROM..TO range (or the tuple is not this plan's shape).</summary>
    public string? LiteralAt(Subscripts subs) =>
        subs.Count == Dims.Count && Literals.TryGetValue(subs, out string? lit) ? lit : null;
}

/// <summary>⛔ THE §13.18.63.4 GR12–GR16 ODOMETER, in ONE place: the pure resolution of a Format-2 (table) VALUE's
/// phrases over a dimension list. The binder's <c>ResolveTableValues</c> pass validates §13.18.63.3 SR18–SR23 and
/// then calls this; nothing else computes a table element's initial value.</summary>
public static class TableValueOdometer
{
    /// <summary>⛔ THE §8.5.1.9.1 MAXIMUM CAPACITY of a dynamic-capacity table, as this implementation defines it:
    /// "The actual limit for the current capacity imposed by the implementor and by current resource availability
    /// is referred to as the maximum capacity." A DYNAMIC table declared without an OCCURS TO phrase specifies NO
    /// maximum number of occurrences, so §13.18.63.3 SR21's ceiling sentence has no operand for that dimension —
    /// this number stands in its place, and a Format-2 VALUE whose subscript-2 exceeds it is rejected rather than
    /// silently materialized. (Without a ceiling the §13.18.63.4 GR12 fill has none either: the pre-existing
    /// single-dimension loop ran from subscript-1 to subscript-2 with no bound at all.)</summary>
    public const int MaxDynamicCapacity = 1_000_000;

    /// <summary>The defensive ceiling on ONE phrase's fill — see <see cref="Resolve"/>. Never reached by source
    /// the binder pass has screened; every declared dimension bounds its own subscript-2, and an undeclared
    /// (dynamic, no OCCURS TO) one is bounded by <see cref="MaxDynamicCapacity"/>.</summary>
    private const int MaxFillElements = 64_000_000;

    /// <summary>§13.18.63.4 GR14 — "If the TO phrase is not specified, it is as if the TO phrase were specified
    /// with each subscript-2 as the maximum number of occurrences, or, in the case of a dynamic-capacity table,
    /// the expected number of occurrences, of the table associated with each corresponding subscript-1." Returns
    /// null when a dimension has no such number (a DYNAMIC table with no OCCURS TO) — SR22 rejects that source, so
    /// the caller has already diagnosed it.</summary>
    public static Subscripts? DefaultTo(IReadOnlyList<TableValueDim> dims)
    {
        var a = new int[dims.Count];
        for (int i = 0; i < dims.Count; i++)
        {
            if (dims[i].Max is not { } m) return null;
            a[i] = m;
        }
        return new Subscripts(a);
    }

    /// <summary>ONE odometer step (ISO §13.18.63.4 GR12): "Consecutive table elements are referenced by
    /// incrementing by 1 the subscript that represents the least inclusive dimension of the table. When any
    /// reference to a subscript, prior to incrementing it, is equal to the maximum number of occurrences … that
    /// subscript is set to 1 and the subscript for the next most inclusive dimension of the table is incremented
    /// by 1." Mutates <paramref name="cur"/>; false when the whole odometer has run past its last element.
    /// <para>A dimension with no ceiling (a DYNAMIC table with no OCCURS TO) simply increments and never carries —
    /// SR23 requires every MORE inclusive subscript to be equal across FROM and TO, so no carry out of it is ever
    /// needed to reach subscript-2.</para></summary>
    public static bool Step(int[] cur, IReadOnlyList<TableValueDim> dims)
    {
        for (int k = cur.Length - 1; k >= 0; k--)
        {
            if (dims[k].Max is not { } max) { cur[k]++; return true; }
            if (cur[k] < max) { cur[k]++; return true; }
            cur[k] = 1;   // exhausted this dimension — carry into the next most inclusive one
        }
        return false;
    }

    /// <summary>Resolve the phrases into the §13.18.63.4 GR12–GR15 element map: each phrase fills from its
    /// subscript-1 tuple through its subscript-2 tuple in odometer order, reusing its literal list cyclically
    /// (GR13; GR14 supplies the missing subscript-2), and a LATER phrase overwrites an element an earlier one
    /// already keyed (GR15 — "the value defined by the last specified FROM phrase in the VALUE clause is assigned
    /// to the table element").
    /// <para>Only phrases whose tuples are well-formed for <paramref name="dims"/> contribute; the caller has
    /// already diagnosed the rest (SR20/SR21), and a mis-shaped phrase must not silently seed the wrong element.</para></summary>
    public static Dictionary<Subscripts, string> Resolve(
        IReadOnlyList<TableValueDim> dims, IReadOnlyList<TableValueSpec> specs)
    {
        var map = new Dictionary<Subscripts, string>();
        foreach (var spec in specs.OrderBy(s => s.Ordinal))
        {
            if (spec.Literals.Count == 0 || spec.From.Count != dims.Count) continue;
            if (spec.To is { } t && t.Count != dims.Count) continue;
            var from = new Subscripts([.. spec.From]);
            var to = spec.To is { } tl ? new Subscripts([.. tl]) : DefaultTo(dims);
            if (to is not { } stop || Subscripts.Compare(stop, from) < 0) continue;

            var cur = new int[dims.Count];
            for (int i = 0; i < dims.Count; i++) cur[i] = spec.From[i];
            // The odometer visits tuples in strictly increasing lexicographic order (a carry LOWERS an inner
            // subscript but RAISES a more inclusive one), so `>= stop` is both the GR13 stop condition and the
            // anti-runaway invariant. The extra element cap is unreachable for conforming source — the pass
            // screens subscript-2 against every dimension's ceiling, including the §8.5.1.9.1 implementor
            // maximum capacity for a dimension the OCCURS clause gives none — and is kept as the defensive
            // statement that this loop is finite whatever a future caller hands it.
            for (int k = 0; k < MaxFillElements; k++)
            {
                var here = new Subscripts([.. cur]);
                map[here] = spec.Literals[k % spec.Literals.Count];
                if (Subscripts.Compare(here, stop) >= 0) break;
                if (!Step(cur, dims)) break;
            }
        }
        return map;
    }

    /// <summary>§13.18.63.4 GR16 — the INITIAL CAPACITY a Format-2 VALUE gives the dynamic-capacity table at
    /// dimension <paramref name="dim"/>: (a) with a TO phrase, "the initial capacity is increased, if necessary,
    /// to the value of the corresponding subscript-2, provided that this value does not lie outside the range
    /// defined by the minimum and expected capacity specified in the OCCURS clause. If the value of subscript-2
    /// lies outside this range, the initial capacity is unchanged"; (b) with no TO phrase, "the initial capacity
    /// is set equal to the expected capacity specified in the OCCURS clause". Null when this phrase raises
    /// nothing.</summary>
    public static int? InitialCapacity(TableValueSpec spec, int dim, int min, int? expected)
    {
        if (spec.To is not { } to)
            return expected;                       // GR16b
        if (dim >= to.Count) return null;
        int want = to[dim];
        if (want < min) return null;               // GR16a proviso — outside [min, expected]: unchanged
        if (expected is { } e && want > e) return null;
        return want;                               // GR16a
    }
}
