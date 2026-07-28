// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime;

/// <summary>
/// The data-pointer runtime operations (Phase-4b increment 2 — ADDRESS OF / BASED / SET ADDRESS OF /
/// ALLOCATE-FREE over the ONE <see cref="ManagedPointer"/> carrier and the ONE <see cref="StorageCell"/>
/// shared-storage shape; PHASE4_RECONCILIATION "M2-DATA-5 / M2-PROC-5 — increment 2" design). Helpers, not a
/// second carrier: every pointer VALUE stays a <see cref="ManagedPointer"/>; every pointed storage stays a
/// character-image cell (never a byte substrate). Fatal exception conditions throw
/// <see cref="CobolFatalException"/> (the documented runtime raise-point channel; §14.6.13.1.3 #7/#8
/// loud-failure doctrine); nonfatal ones report through out-parameters for the emitter's TurnState-gated
/// <c>ExceptionState.Set</c> block.
/// </summary>
public static class CobolPtr
{
    /// <summary>Dereference a BASED item's implicit pointer for a view of <paramref name="classWidth"/>
    /// characters (ISO §13.18.5): GR3 — a reference while the pointer is NULL is <c>EC-DATA-PTR-NULL</c>
    /// (Fatal); GR4 — an invalid address (a freed cell, a non-window carrier, or a window that does not fit
    /// inside the cell) is <c>EC-BOUND-PTR</c> (Fatal). Returns the cell whose <c>Ref</c> the generated
    /// bridge property aliases.</summary>
    public static StorageCell Deref(ManagedPointer? p, long classWidth)
    {
        // ⛔ THESE FOUR THROW WHETHER OR NOT CHECKING IS ON, and that is the owner's decided rule rather than an
        // oversight: checking-off is lenient only where the standard NAMES the outcome, and §13.18.5.4 GR3/GR4
        // name none. This method must return a StorageCell, so "lenient" could only mean fabricating one and
        // letting the program run on garbage — which no other COBOL does (GnuCOBOL and gcobol list both
        // conditions fatal and otherwise SIGSEGV; Micro Focus traps to RTS 114; IBM abends S0C4; NetCOBOL
        // JMP0071I-U). The helper is still called FIRST when checking is on, so the last-exception status is set
        // and the statement guard's `catch … when (EcName == …)` can select a USE declarative.
        if (p is null || p.IsNull)
        {
            ExceptionState.DataPtrNullError(
                "reference to a based item whose data-address pointer is NULL (ISO 13.18.5.4 GR3)");
            throw new CobolFatalException("EC-DATA-PTR-NULL",
                "reference to a based item whose data-address pointer is NULL (ISO 13.18.5.4 GR3)");
        }
        if (p is not CellPointer w)
        {
            ExceptionState.BoundPtrError(
                "reference to a based item whose data-address pointer does not address data storage (ISO 13.18.5.4 GR4)");
            throw new CobolFatalException("EC-BOUND-PTR",
                "reference to a based item whose data-address pointer does not address data storage (ISO 13.18.5.4 GR4)");
        }
        if (w.Cell.Freed)
        {
            ExceptionState.BoundPtrError(
                "reference to a based item addressing storage released by FREE (ISO 14.9.15 GR1a / 13.18.5.4 GR4)");
            throw new CobolFatalException("EC-BOUND-PTR",
                "reference to a based item addressing storage released by FREE (ISO 14.9.15 GR1a / 13.18.5.4 GR4)");
        }
        if (w.Offset < 0 || w.Offset + classWidth > w.Cell.Ref.Length)
        {
            string oob = $"reference to a based item outside its addressed storage (offset {w.Offset} + width "
                + $"{classWidth} over {w.Cell.Ref.Length} positions — ISO 13.18.5.4 GR4)";
            ExceptionState.BoundPtrError(oob);
            throw new CobolFatalException("EC-BOUND-PTR", oob);
        }
        return w.Cell;
    }

    /// <summary>The window pointer's character offset (0 for the null carrier — <see cref="Deref"/> trips
    /// FIRST on every generated read/write path, so this never masks a null dereference).</summary>
    public static long OffsetOf(ManagedPointer? p) => p is CellPointer w ? w.Offset : 0;

    /// <summary>SET pointer UP/DOWN BY (ISO §14.9.39 Format 10): GR18 — a NULL operand is
    /// <c>EC-DATA-PTR-NULL</c> (Fatal); GR20 — the address moves by <paramref name="by"/> bytes (character
    /// positions here). The implementor data-pointer range is UNBOUNDED (the recorded implementor choice):
    /// EC-RANGE-PTR is never raised at SET time — an out-of-cell address surfaces at the next dereference as
    /// EC-BOUND-PTR.</summary>
    public static ManagedPointer UpBy(ManagedPointer? p, long by)
    {
        // §14.9.39 Format 10 GR19 states the unsuccessful outcome for this statement — "the execution of the SET
        // statement is unsuccessful, and the content of identifier-9 is unchanged" — so with checking OFF these
        // return the operand UNCHANGED rather than terminating. Unlike Deref there IS a defined thing to return.
        if (p is null || p.IsNull)
        {
            ExceptionState.DataPtrNullError(
                "SET pointer UP/DOWN BY with a NULL pointer operand (ISO 14.9.39 Format 10 GR18)");
            // `ManagedPointer.Null` IS the unchanged value here: the operand already held the predefined
            // address NULL (that is the condition), and the C#-null carrier normalises to the same thing.
            return ManagedPointer.Null;
        }
        if (p is not CellPointer w)
        {
            ExceptionState.BoundPtrError(
                "SET pointer UP/DOWN BY over a pointer that does not address data storage (ISO 14.9.39 Format 10)");
            return p;   // unchanged — a non-null carrier that simply does not address storage
        }
        return new CellPointer(w.Cell, w.Offset + by);
    }

    /// <summary>SET pointer UP/DOWN BY with a SCALED amount (ISO §14.9.39 Format 10 GR19): "if the value …
    /// is not an integer, the EC-SIZE-ADDRESS exception condition is set to exist" (Fatal) — the amount
    /// arrives as its scaled fixed-point value and the divisibility test IS the integrality test; an integer
    /// value at any scale (e.g. 2.0) moves normally. The unscaled fast path is <see cref="UpBy"/>.</summary>
    public static ManagedPointer UpByScaled(ManagedPointer? p, long scaledBy, int scale)
    {
        long pow = 1;
        for (int i = 0; i < scale; i++) pow *= 10;
        if (scaledBy % pow != 0)
        {
            ExceptionState.SizeAddressError(
                "SET pointer UP/DOWN BY a non-integer amount (ISO 14.9.39 Format 10 GR19)");
            return p ?? ManagedPointer.Null;   // GR19 verbatim — unsuccessful; identifier-9 unchanged
        }
        return UpBy(p, scaledBy / pow);
    }

    /// <summary>ALLOCATE (ISO §14.9.3): a fresh <paramref name="size"/>-character cell (GR1 — the requested
    /// bytes; the binder rounds a fractional request UP). GR2: a request of zero or less returns the NULL
    /// pointer and no exception condition exists. GR6: INITIALIZED with CHARACTERS fills with binary zeros
    /// (the faithful <c>'\0'</c> image in the character model); otherwise the content is undefined (GR8) —
    /// this implementation space-fills, a conformant choice.</summary>
    public static ManagedPointer Allocate(long size, bool zeroFill = false)
    {
        if (size <= 0) return ManagedPointer.Null;
        var cell = new StorageCell { Ref = new string(zeroFill ? '\0' : ' ', checked((int)size)), Allocated = true };
        return new CellPointer(cell, 0);
    }

    /// <summary>FREE (ISO §14.9.15 GR1): (a) a pointer addressing the START of storage obtained by ALLOCATE
    /// and not yet freed — release it (the cell is marked <see cref="StorageCell.Freed"/> and its image
    /// dropped; every dangling alias then fails loud at <see cref="Deref"/>, the "contents become undefined"
    /// license made loud) and the operand becomes NULL; (b) a NULL operand — no operation; (c) anything else
    /// — the operand is unchanged and <paramref name="notAlloc"/> reports the nonfatal
    /// <c>EC-STORAGE-NOT-ALLOC</c> for the emitter's TurnState-gated status block.</summary>
    public static ManagedPointer Free(ManagedPointer? p, out bool notAlloc)
    {
        notAlloc = false;
        if (p is null || p.IsNull) return ManagedPointer.Null;   // GR1b — no-op
        if (p is CellPointer { Offset: 0, Cell: { Allocated: true, Freed: false } } w)
        {
            w.Cell.Freed = true;   // GR1a — released; dangling aliases trip Deref loud
            w.Cell.Ref = "";
            return ManagedPointer.Null;
        }
        notAlloc = true;           // GR1c — not the start of an allocation
        return p;
    }
}
