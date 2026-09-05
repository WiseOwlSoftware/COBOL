// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// One shared, aliasable character-storage cell (the Tier-B string-canonical backing lifted onto the heap —
/// never a byte substrate): EXTERNAL records, ADDRESS-OF-taken items, and ALLOCATEd areas all live in one of
/// these; <see cref="Ref"/> is a FIELD so the generated <c>ref</c>-returning bridge property can alias it.
/// </summary>
public sealed class StorageCell
{
    /// <summary>The storage's character image (its full width; every view windows it).</summary>
    public string Ref = "";

    /// <summary>True for a cell obtained by ALLOCATE (ISO §14.9.3) — the only cells FREE releases (GR1a).</summary>
    public bool Allocated;

    /// <summary>True once FREE released the cell (§14.9.15.4 GR1a — "the contents of any data items located
    /// within the released storage area become undefined"; this implementation makes any later dereference
    /// loud, EC-BOUND-PTR). ⚠ The clause and the quotation were re-derived here: this comment and its twin
    /// below carried "§14.9.15 GR1a — the contents become undefined", a PARAPHRASE at a clause number one
    /// level short of the rule (CLAUDE.md rule 1's inherited-citation failure mode).</summary>
    public bool Freed;

    /// <summary>⛔ THE MANAGED SLOTS OF THE SAME STORAGE AREA, keyed by the slot's BYTE OFFSET within it
    /// (kb/Work PB231 — the pointer third). A data item of class pointer or class object holds a managed
    /// REFERENCE, which is not a byte sequence and therefore has no image in <see cref="Ref"/>; its bytes
    /// there are reserved placeholder positions so that §14.9.3.4 GR3's "the amount of storage to be
    /// allocated is the number of bytes required to hold an item as described by data-name-1" — and every
    /// following member's offset — stay exactly what a byte-addressed area says they are.
    /// <para>⛔ THE SLOTS BELONG TO THE CELL, NOT TO THE ITEM, and that is the whole reason they are here:
    /// EXTERNAL sharing, ADDRESS OF aliasing and <c>SET ADDRESS OF</c> re-pointing all mean "two descriptions
    /// of ONE storage area", so a pointer member re-pointed through one description must be visible through
    /// every other. A per-instance field could not do that.</para>
    /// <para>⛔ AN UNWRITTEN SLOT READS NULL, AND THAT *IS* §14.9.3.4 GR9 — "data items of class object or
    /// class pointer in the allocated storage are initialized to null" — realized by construction rather
    /// than by a seeding loop that a future allocation path could forget. The table is allocated lazily, so
    /// a cell with no managed member costs one null field.</para></summary>
    private Dictionary<int, object?>? _slots;

    /// <summary>The managed value at <paramref name="byteOffset"/>, or <see langword="null"/> when nothing
    /// has been stored there (ISO §14.9.3.4 GR9's null initial state — see <see cref="_slots"/>).</summary>
    public object? SlotAt(int byteOffset) =>
        _slots is { } m && m.TryGetValue(byteOffset, out object? v) ? v : null;

    /// <summary>Store a managed value at <paramref name="byteOffset"/> — the receiving twin of
    /// <see cref="SlotAt"/>.</summary>
    public void SetSlotAt(int byteOffset, object? value) => (_slots ??= [])[byteOffset] = value;

    /// <summary>Drop every managed slot — FREE (§14.9.15.4 GR1a, "the contents of any data items located
    /// within the released storage area become undefined"): the byte image and the managed slots are one
    /// storage area and are released together, so a released cell cannot keep a dangling pointer alive.</summary>
    internal void ClearSlots() => _slots = null;
}
