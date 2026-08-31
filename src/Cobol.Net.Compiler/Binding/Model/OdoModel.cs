// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Model;

/// <summary>
/// The structured OCCURS description (ISO/IEC 1989:2023 §13.18.38) attached to a table's <see cref="DataItem"/>:
/// the Format-2 occurrence bounds (<c>OCCURS integer-1 TO integer-2 TIMES DEPENDING ON data-name-1</c>), the
/// DEPENDING ON data-name and its post-build-resolved item, and the ASCENDING/DESCENDING KEY data-names (Formats
/// 1 and 2 alike — §13.18.38 GR3, consumed by SEARCH ALL and the table SORT). The ARRAY itself is always
/// allocated at <see cref="Max"/> occurrences — §8.5.1.8: for an occurs-depending table "the physical capacity is
/// fixed at compile time; the logical capacity may vary" — so only the GR7 current-count machinery (group-operand
/// extents per GR8, the SEARCH bound per §14.9.37.4 GR4) consults this object; layout, width, and VALUE
/// initialization (§13.18.63 GR6 — as if the count were the maximum) ride on <see cref="DataItem.Occurs"/>.
/// </summary>
public sealed class OccursSpec
{
    /// <summary>integer-1 — the minimum occurrence count (<c>0 ≤ Min &lt; Max</c>, §13.18.38 SR16). Equals
    /// <see cref="Max"/> for a fixed (Format 1) table.</summary>
    public required int Min { get; init; }

    /// <summary>integer-2 — the maximum occurrence count, the allocated physical capacity (§8.5.1.8).</summary>
    public required int Max { get; init; }

    /// <summary>The <c>DEPENDING ON data-name-1</c> name as written, or <see langword="null"/> for a fixed
    /// (Format 1) table.</summary>
    public string? DependingName { get; init; }

    /// <summary>The resolved data-name-1 item — set by the post-build <c>DataBinder.OdoResolve</c> pass
    /// (data-name-1 may legally be declared anywhere outside the span the table starts, §13.18.38 SR20, so
    /// resolution must wait for the complete forest).</summary>
    public DataItem? Depending { get; set; }

    /// <summary>ASCENDING KEY data-names in declaration order (§13.18.38 GR3 — the significance order SEARCH ALL
    /// and the table SORT consult; captured for model completeness, the scan implementation is key-order-free).</summary>
    public List<string> AscendingKeyNames { get; } = [];

    /// <summary>DESCENDING KEY data-names in declaration order (§13.18.38 GR3).</summary>
    public List<string> DescendingKeyNames { get; } = [];

    // ── Format 4: DYNAMIC-capacity table (ISO §13.18.38 Format 4, COBOL-2014; data-model D9) ──────────────────

    /// <summary>True for a Format-4 DYNAMIC-capacity table (§8.5.1.9) — its capacity varies at run time. Mutually
    /// exclusive with the Format-2 DEPENDING form. A dynamic table has no fixed <see cref="DataItem.Occurs"/>.</summary>
    public bool IsDynamic { get; init; }

    /// <summary><c>CAPACITY IN data-name-3</c> — the current-capacity register name (§13.18.38 GR15), or null.</summary>
    public string? CapacityName { get; init; }

    /// <summary>The synthetic CAPACITY register item — a view over the table's Capacity, minted for EVERY dynamic
    /// table (FUNCTION LENGTH's §15.50.4 r7c reads it; kb/Work PB61) and NAMED only under CAPACITY IN; set by the post-build
    /// <c>DataBinder.DynamicResolve</c> pass (data-name-3 is implicitly defined at the OCCURS entry, SR30).</summary>
    public DataItem? CapacityRegister { get; set; }

    /// <summary><c>FROM integer-4</c> — the minimum / initial current capacity (§13.18.38 GR16); null ⇒ 0.</summary>
    public int? InitialCap { get; init; }

    /// <summary><c>TO integer-5</c> — the expected capacity (§13.18.38 GR17); null ⇒ unlimited.</summary>
    public int? ExpectedMax { get; init; }

    /// <summary>The INITIALIZED phrase — seed each new/intermediate occurrence per §8.5.1.9.5.</summary>
    public bool Initialized { get; init; }
}

/// <summary>Pure model helpers for the OCCURS DEPENDING ON subsystem (ISO/IEC 1989:2023 §13.18.38).
/// (<c>OdoGroupPlace</c> — the GR8 group-operand decoration — lives with the rest of the <see cref="Place"/>
/// hierarchy in <c>Place.cs</c>, P5.11a.)</summary>
public static class OdoModel
{
    /// <summary>The occurs-depending table among <paramref name="group"/>'s STRICT descendants, or
    /// <see langword="null"/>. At most one exists in a legal program: §13.18.38 SR22 makes it the unique trailing
    /// variable part of its record, and SR1(b)/SR10 forbid nesting it under another OCCURS — both validated by
    /// <c>DataBinder.OdoResolve</c>, so a first-match scan is exact.</summary>
    public static DataItem? TableUnder(DataItem group)
    {
        foreach (var c in group.Children)
        {
            if (c.OccursSpec is { DependingName: not null }) return c;
            if (TableUnder(c) is { } nested) return nested;
        }
        return null;
    }

    /// <summary>True when <paramref name="item"/> is <paramref name="ancestor"/> itself or lies within its
    /// subtree (the GR8a/GR8b "data item that describes the group" containment test).</summary>
    public static bool IsWithin(DataItem item, DataItem ancestor)
    {
        for (DataItem? n = item; n is not null; n = n.Parent)
            if (ReferenceEquals(n, ancestor)) return true;
        return false;
    }

    /// <summary>Wrap a resolved GROUP place whose subtree contains the occurs-depending <paramref name="table"/>
    /// (ISO §13.18.38 GR8). The fixed prefix is everything before the table in the group's emitted image — exact
    /// because the table is the record's trailing storage (SR22, enforced at bind).
    /// <para>⛔ <b>THE EXTENT IS COUNTED IN THE GROUP'S OWN POSITION UNIT, WHICH IS BITS WHEN THE SUBTREE HOLDS A
    /// USAGE BIT LEAF</b> (kb/Work PB173). <c>fixed + occ × elem</c> is only an identity in a unit where each
    /// occurrence starts where the previous one ended, and §8.5.1.6.3's shared-byte runs break that in CHARACTERS:
    /// for <c>05 BT PIC 1(3) OCCURS 1 TO 4 DEPENDING ON N</c> the per-occurrence CHARACTER width rounds 3 bits up
    /// to 1, so three occurrences measured as 3 characters occupy 9 bits = 2 — and the fixed prefix computed as
    /// <c>PhysicalWidth − elem × max</c> came out NEGATIVE (an 8-bit group: 2 − 4 = −2), which made the whole
    /// group operand render as the EMPTY string. In BIT positions the same arithmetic is exact, because
    /// <see cref="BitLayout.ExtentBits"/> advances its cursor by <c>WidthBits × Occurs</c> in ONE contiguous step
    /// and every alignment round-up happens BEFORE the trailing table (SR22). <see cref="OdoGroupPlace.PositionsPerCharacter"/>
    /// then records the unit so the CHARACTER channel (<c>AsImage</c>/<c>FromImage</c>) takes the ceiling — the same
    /// ceiling <see cref="BitLayout.Characters"/> gives every other bit width — while the BIT channel
    /// (<c>AsBits</c>/<c>FromBits</c>, §13.18.29.4 GR1b's as-if PICTURE 1(m)) uses the positions directly.</para></summary>
    public static OdoGroupPlace WrapGroup(Place inner, Place depending, DataItem group, DataItem table)
    {
        // Bit-granular whenever the subtree holds a USAGE BIT leaf — not merely when the group is GROUP-USAGE BIT:
        // an ordinary alphanumeric group with a trailing bit ODO table has exactly the same sub-byte arithmetic.
        // The prefix is the table's own placement offset (never `total − elem × max`, which would charge rule 4's
        // trailing filler to the prefix); -1 means an unmodelled overlay chain, and the character path stands in.
        bool bitUnits = group.HasBitDescendant && BitLayout.StartBitOf(group, table) >= 0;
        int max = table.Occurs ?? 1;                          // the allocated capacity = integer-2 (§8.5.1.8)
        // integer-1 — the LOWER bound §13.18.38.4 GR7 requires the control value to fall within. Carried so the
        // extent computation can raise EC-BOUND-ODO below it; the runtime floor used to be hardcoded 0, which
        // made a below-minimum DEPENDING value clamp silently instead of setting the condition.
        int min = table.OccursSpec?.Min ?? 0;
        int elem = bitUnits ? BitLayout.WidthBits(table) : table.ImageWidth;   // per-occurrence positions
        int fixedUnits = bitUnits
            ? BitLayout.StartBitOf(group, table)
            : Model.RecordLayout.PhysicalWidth(group) - elem * max;            // SR22 — the variable tail is trailing
        return new OdoGroupPlace(inner, depending, fixedUnits, elem, min, max, IsWithin(depending.Item, group),
            bitUnits ? BitLayout.BitsPerCharacter : 1);
    }

    /// <summary>The SEARCH / SEARCH ALL depending item for an OCCURS DEPENDING table (ISO §14.9.37.4 GR4/GR9 →
    /// §13.18.38 GR7): the <see cref="Place"/> of data-name-1, whose CURRENT count bounds the scan (the backend
    /// renders <c>CobolTable.Occ(place)</c>, storage-form agnostic). <see langword="null"/> for a fixed table (the
    /// caller uses the compile-time maximum) or a DYNAMIC table (a runtime <c>Capacity</c>, rendered from the table
    /// path — mutually exclusive with the Format-2 DEPENDING form). data-name-1 is resolved post-build (SR20).</summary>
    public static Place? SearchDepending(DataItem table, ReferenceResolver refs) =>
        table.OccursSpec is { Depending: { } dep } ? refs.ResolveItem(dep) : null;
}

