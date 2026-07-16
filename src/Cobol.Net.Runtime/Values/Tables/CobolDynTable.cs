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
    private readonly Func<T> _seed;
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
    {
        _seed = seed;
        _min = min < 0 ? 0 : min;
        _expected = expected;
        _store = new T[Math.Max(_min, 4)];
        _count = 0;
        GrowTo(_min);   // initial current capacity = FROM (§8.5.1.9.1 line 8199); seeds occurrences 1.._min
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
        _scratch = _seed();
        return ref _scratch;
    }

    /// <summary>A RECEIVING element reference (§8.5.1.9.3): an occurrence &gt; the current capacity GROWS the table to
    /// it, seeding any skipped intermediate occurrences. An occurrence &lt; 1 is benign scratch.</summary>
    public ref T RefReceiving(long occ)
    {
        if (occ < 1) { _scratch = _seed(); return ref _scratch; }
        if (occ > _count) GrowTo((int)occ);
        return ref _store[(int)(occ - 1)];
    }

    /// <summary>Raise the current capacity to <paramref name="newCount"/>, seeding new occurrences [old..new)
    /// (§8.5.1.9.5). A request past <see cref="MaxOccurrences"/> raises EC-BOUND-TABLE-LIMIT (fatal, capacity
    /// unchanged). NOTE: the nonfatal capacity-overflow exceptions are NOT yet raised — EC-BOUND-OVERFLOW on implicit
    /// growth past the expected capacity (§8.5.1.9.6 item 1) and EC-BOUND-SET on an explicit SET past it (§14.9.39
    /// GR30) are checking-gated and, being nonfatal, produce identical observable results with checking OFF (the
    /// default); <see cref="_expected"/> is captured for that future wiring (data-model D9 flagged follow-on).</summary>
    private void GrowTo(int newCount)
    {
        if (newCount <= _count) return;
        if (newCount > MaxOccurrences)
            throw new CobolFatalException("EC-BOUND-TABLE-LIMIT",
                $"OCCURS DYNAMIC growth to {newCount} exceeds the implementor maximum ({MaxOccurrences}) — ISO §8.5.1.9.6");
        if (newCount > _store.Length)
        {
            int cap = _store.Length < 4 ? 4 : _store.Length;
            while (cap < newCount) cap = cap >= MaxOccurrences / 2 ? MaxOccurrences : cap * 2;
            Array.Resize(ref _store, cap);
        }
        for (int i = _count; i < newCount; i++) _store[i] = _seed();
        _count = newCount;
    }

    /// <summary>SET Format 14 <c>… TO n</c> (§14.9.39 GR29): set the current capacity to n (raise OR lower), clamped
    /// to ≥ the minimum. Lowering frees the highest occurrences. Illegal during a SEARCH of this same table
    /// (EC-FLOW-SEARCH, GR31). Growth beyond the implementor max raises EC-BOUND-TABLE-LIMIT (capacity unchanged).</summary>
    public void SetCapacity(long n)
    {
        if (_searching > 0)
            throw new CobolFatalException("EC-FLOW-SEARCH",
                "SET of a dynamic-capacity table's capacity during a SEARCH of that same table (ISO §14.9.39 GR31)");
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
    public void InitializeAll() { for (int i = 0; i < _count; i++) _store[i] = _seed(); }

    /// <summary>Mark the start of a SEARCH of this table (a SET Format 14 on it while active raises EC-FLOW-SEARCH,
    /// §14.9.39 GR31). Nestable (re-entrant SEARCH).</summary>
    public void EnterSearch() => _searching++;

    /// <inheritdoc cref="EnterSearch"/>
    public void ExitSearch() { if (_searching > 0) _searching--; }
}
