// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime;

/// <summary>
/// A DYNAMIC-capacity table (ISO/IEC 1989:2023 §13.18.38 Format 4 / §8.5.1.9; data-model design D9). Out-of-line
/// growable storage for a COBOL <c>OCCURS DYNAMIC</c> table: a backing array plus a current-capacity counter (=
/// the table's <b>current capacity</b> — the number of occurrences allocated now, §8.5.1.9.1). The CAPACITY register
/// is a view over <see cref="Capacity"/>. New/intermediate occurrences are seeded with the one-occurrence element
/// initializer (INITIALIZED, §8.5.1.9.5). Element access returns <c>ref T</c> so a subscripted write goes through the
/// single ref every <c>Place.Write</c> relies on; <c>T</c> is the element value type (a record struct) or string.
/// </summary>
public sealed class CobolDynTable<T>
{
    private T[] _store;
    private int _count;                 // current capacity (§8.5.1.9.1)
    private int _searching;             // >0 while a SEARCH of THIS table is in progress (EC-FLOW-SEARCH guard)
    private T _scratch = default!;      // the benign out-of-range slot (COBOL-85 / checking-off policy)
    private readonly Func<int, T> _seedAt;   // occurrence-indexed seed (1-based); a Format 2 (table) VALUE varies by occurrence
    private readonly int _min;
    private readonly int? _expected;    // TO integer-5 — the expected capacity (nonfatal to exceed)

    /// <summary>The implementor maximum occurrence count (§8.5.1.9.1 — resource-bounded). A growth request past it
    /// raises EC-BOUND-TABLE-LIMIT (fatal) with the current capacity left unchanged.</summary>
    public const int MaxOccurrences = 0x3FFF_FFFF;   // ~Array.MaxLength headroom

    /// <param name="seed">Produces one freshly-initialized occurrence (the element's VALUE-THEN-DEFAULT image).</param>
    /// <param name="min">FROM integer-4 — the minimum / initial current capacity (§13.18.38 GR16); the table opens
    /// at this capacity, seeded.</param>
    /// <param name="expected">TO integer-5 — the expected capacity (§13.18.38 GR17), or null if unbounded.</param>
    /// <param name="initialized">The INITIALIZED phrase (§8.5.1.9.5). New occurrences are ALWAYS seeded here (a
    /// crash-safe well-formed default; the INITIALIZED-absent "undefined" case permits any content).</param>
    public CobolDynTable(Func<T> seed, int min, int? expected, bool initialized)
        : this((Func<int, T>)(_ => seed()), min, expected, initialized, min) { }

    /// <summary>The Format 2 (table) VALUE overload (ISO §13.18.63.2/GR12–GR16): a PER-OCCURRENCE seed (1-based)
    /// and an explicit initial current capacity (<paramref name="initialCapacity"/> — the GR16 value, ≥ the FROM
    /// minimum). Occurrences 1..initialCapacity take their keyed VALUE literal; growth beyond re-seeds through the
    /// same <paramref name="seedAt"/> (occurrences outside the VALUE range yield the element default).</summary>
    public CobolDynTable(Func<int, T> seedAt, int min, int? expected, bool initialized, int initialCapacity)
    {
        _seedAt = seedAt;
        _min = min < 0 ? 0 : min;
        _expected = expected;
        int open = Math.Max(_min, initialCapacity < 0 ? 0 : initialCapacity);
        _store = new T[Math.Max(open, 4)];
        _count = 0;
        GrowTo(open);   // initial current capacity = FROM (§8.5.1.9.1) raised to the VALUE's GR16 capacity
    }

    /// <summary>The current capacity — the number of occurrences allocated now (§8.5.1.9.1). The source-level
    /// CAPACITY register reads this; SEARCH/PERFORM VARYING bound to it.</summary>
    public long Capacity => _count;

    /// <summary>A SENDING element reference (§8.5.1.9.2): a 1-based occurrence in 1..current-capacity. An
    /// out-of-range occurrence continues benignly through a fresh scratch slot — the COBOL-85 / subscript-checking-off
    /// policy (matches <see cref="CobolTable.At{T}"/>); EC-BOUND-SUBSCRIPT under CHECKING ON is a later wiring.</summary>
    public ref T RefSending(long occ)
    {
        if (occ >= 1 && occ <= _count) return ref _store[(int)(occ - 1)];
        _scratch = _seedAt((int)occ);
        return ref _scratch;
    }

    /// <summary>A RECEIVING element reference (§8.5.1.9.3): an occurrence &gt; the current capacity GROWS the table to
    /// it, seeding any skipped intermediate occurrences. An occurrence &lt; 1 is benign scratch. When the growth
    /// FIRST crosses the expected capacity (TO integer-5) it sets the nonfatal EC-BOUND-OVERFLOW (§8.5.1.9.6 GR1)
    /// under CHECKING ON — GR1's "already exceeded before an implicit change ⇒ no exception" is the
    /// <c>!wasExceeded</c> guard (only the first crossing raises); the growth proceeds regardless.</summary>
    public ref T RefReceiving(long occ)
    {
        if (occ < 1) { _scratch = _seedAt((int)occ); return ref _scratch; }
        if (occ > _count)
        {
            if (_expected is { } exp && occ > exp && _count <= exp)   // first implicit crossing of the expected capacity
                ExceptionState.BoundOverflowError(
                    $"OCCURS DYNAMIC implicit growth to {occ} exceeds the expected capacity {exp} (ISO §8.5.1.9.6 GR1)");
            GrowTo((int)occ);
            // ⛔ GrowTo may now DECLINE (GR30 leaves the capacity unchanged when checking is off), so the
            // occurrence it was asked for can still not exist. Falling through to `_store[occ-1]` here would be
            // an IndexOutOfRangeException — a raw .NET failure on user source, from the one path where a benign
            // scratch slot is exactly what the model already provides for an unreachable occurrence.
            if (occ > _count) { _scratch = _seedAt((int)occ); return ref _scratch; }
        }
        return ref _store[(int)(occ - 1)];
    }

    /// <summary>Raise the current capacity to <paramref name="newCount"/>, seeding new occurrences [old..new)
    /// (§8.5.1.9.5). A request past <see cref="MaxOccurrences"/> raises EC-BOUND-TABLE-LIMIT (fatal, capacity
    /// unchanged). This is the pure grow primitive: EC-BOUND-OVERFLOW on implicit growth past the expected capacity
    /// (§8.5.1.9.6 GR1) is raised by <see cref="RefReceiving"/> BEFORE calling here (only implicit growth qualifies);
    /// EC-BOUND-SET on an explicit SET past the expected capacity (§14.9.39 GR30) stays a nonfatal staged follow-on —
    /// being nonfatal, it produces identical observable results with checking OFF (the default).</summary>
    private void GrowTo(int newCount)
    {
        if (newCount <= _count) return;
        if (newCount > MaxOccurrences)
        {
            // §14.9.39.4 GR30 states the outcome outright — "the EC-BOUND-TABLE-LIMIT exception condition is set
            // to exist AND THE CAPACITY OF THE TABLE IS UNCHANGED" — so with checking off this returns and the
            // table keeps its capacity, rather than the unconditional throw that used to abort the run unit.
            ExceptionState.BoundTableLimitError(
                $"OCCURS DYNAMIC growth to {newCount} exceeds the implementor maximum ({MaxOccurrences}) "
                + "— ISO §14.9.39.4 GR30");
            return;   // GR30: capacity unchanged
        }
        if (newCount > _store.Length)
        {
            int cap = _store.Length < 4 ? 4 : _store.Length;
            while (cap < newCount) cap = cap >= MaxOccurrences / 2 ? MaxOccurrences : cap * 2;
            Array.Resize(ref _store, cap);
        }
        for (int i = _count; i < newCount; i++) _store[i] = _seedAt(i + 1);
        _count = newCount;
    }

    /// <summary>SET Format 14 <c>… TO n</c> (§14.9.39 GR29): set the current capacity to n (raise OR lower), clamped
    /// to ≥ the minimum. Lowering frees the highest occurrences. Illegal during a SEARCH of this same table
    /// (EC-FLOW-SEARCH, GR31). Growth beyond the implementor max raises EC-BOUND-TABLE-LIMIT (capacity unchanged).</summary>
    public void SetCapacity(long n)
    {
        if (_searching > 0)
        {
            // §14.9.39.4 GR31 states the outcome outright — "the EC-FLOW-SEARCH exception condition is set to
            // exist AND THE SET STATEMENT IS NOT EXECUTED" — so with checking off this returns having done
            // nothing, rather than the unconditional throw that used to abort the run unit.
            ExceptionState.FlowSearchError(
                "SET of a dynamic-capacity table's capacity during a SEARCH of that same table "
                + "(ISO §14.9.39.4 GR31)");
            return;   // GR31: the SET statement is not executed
        }
        long target = n < _min ? _min : n;
        if (target > _count) GrowTo((int)target);
        else if (target < _count) _count = (int)target;   // free the highest occurrences (§8.5.1.9.4)
    }

    /// <summary>SET Format 14 <c>… UP BY n</c> (§14.9.39): raise the current capacity by n.</summary>
    public void CapacityUpBy(long n) => SetCapacity(_count + n);

    /// <summary>SET Format 14 <c>… DOWN BY n</c> (§14.9.39): lower the current capacity by n.</summary>
    public void CapacityDownBy(long n) => SetCapacity(_count - n);

    /// <summary>INITIALIZE of the whole dynamic table (§14.9 INITIALIZE GR10): re-seed occurrences [1..current];
    /// the current capacity is unchanged.</summary>
    public void InitializeAll() { for (int i = 0; i < _count; i++) _store[i] = _seedAt(i + 1); }

    /// <summary>The table's CURRENT-EXTENT character image: every occurrence up to the current capacity,
    /// rendered by <paramref name="imageOf"/> and concatenated in occurrence order (kb/Work PB164 — the
    /// §14.9.11.4 GR7 documented DISPLAY format for a variable-length group renders each dynamic-capacity
    /// table "at its current capacity", the same extent §15.50.4 r7c counts for FUNCTION LENGTH).</summary>
    public string CurrentImage(Func<T, string> imageOf)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < _count; i++) sb.Append(imageOf(_store[i]));
        return sb.ToString();
    }

    /// <summary>Mark the start of a SEARCH of this table (a SET Format 14 on it while active raises EC-FLOW-SEARCH,
    /// §14.9.39 GR31). Nestable (re-entrant SEARCH).</summary>
    public void EnterSearch() => _searching++;

    /// <inheritdoc cref="EnterSearch"/>
    public void ExitSearch() { if (_searching > 0) _searching--; }
}
