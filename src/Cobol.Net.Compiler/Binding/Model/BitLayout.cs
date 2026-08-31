// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Model;

/// <summary>
/// ⛔ THE ONE BIT-GRANULAR LAYOUT (ISO §8.5.1.6.3 "Alignment of data items of usage bit"; design D19, fix-queue
/// PB43). A <c>USAGE BIT</c> item occupies BITS — <b>§13.18.60.4 GR5</b>: "The USAGE BIT clause specifies that
/// bits shall be used to represent a boolean data item … The alignment of a data item described with USAGE BIT is
/// specified in 8.5.1.6.3." A boolean item WITHOUT a USAGE clause is a different question and is NOT handled here:
/// §13.18.60.3 SR13(b) implies USAGE DISPLAY and §13.18.60.4 GR7 makes DISPLAY "an alphanumeric coded character
/// set", so one character per boolean position is REQUIRED for it (D-B1's surviving half).
///
/// <para><b>Why a separate walk instead of teaching <c>ImageWidth</c> to add bits.</b> Every other width in this
/// model is a SUM of per-child widths, which works because each child starts where the previous one ended. Bit
/// items break that: two same-level bit items SHARE a byte, and a bit item after anything else skips to the next
/// byte. Position therefore depends on the PREVIOUS sibling's kind and level, which a sum cannot express — so the
/// layout is a cursor walk, and the character widths derive FROM it rather than the other way round.</para>
///
/// <para>⛔ <b>ONLY reached for a subtree that actually contains a USAGE BIT leaf</b>
/// (<see cref="DataItem.HasBitDescendant"/>). Without one there are no sub-byte runs, so this walk and the plain
/// character sum agree BY CONSTRUCTION — gating on it makes the change provably byte-identical for every program
/// that writes no <c>USAGE BIT</c>, which is the entire pre-existing corpus. Same discipline as PB41's scale-0
/// fast path: the new machinery must not perturb the text it is not needed for.</para>
/// </summary>
internal static class BitLayout
{
    /// <summary>Bits per character position. §8.1.2 makes this implementor-specified; COBOL.NET pins <b>8</b>,
    /// consistent with <see cref="DataItem.ByteWidth"/>'s "DISPLAY = 1 byte per character position" and recorded
    /// in <c>docs/CONFORMANCE.md</c> §4.2.16.</summary>
    public const int BitsPerCharacter = 8;

    /// <summary>True when this leaf occupies BITS — a boolean item whose usage is explicitly BIT.</summary>
    public static bool IsBitLeaf(DataItem item) =>
        item.IsElementary && item.Pic is { Category: PicCategory.Boolean, Usage: Usage.Bit };

    /// <summary>True for a bit item in §8.5.1.6.3's sense — "an elementary bit data item or bit group item": a bit
    /// leaf, or a GROUP-USAGE BIT group (§13.18.29.4 GR1a — "a bit group and also a bit data item"; D20/PB79).</summary>
    public static bool IsBitItem(DataItem item) => IsBitLeaf(item) || item.GroupUsage is GroupUsage.Bit;

    /// <summary>The bit positions a §8.5.1.6.3 run MEMBER contributes — a bit leaf's declared boolean positions, a
    /// bit group's exact extent (its as-if PICTURE 1(m) length), times its OCCURS (D20/PB79).</summary>
    public static int RunBits(DataItem m) => (m.IsGroup ? m.AsIfPic!.Length : m.Pic!.Length) * (m.Occurs ?? 1);

    /// <summary>The bit extent of one item PER OCCURRENCE — a bit leaf's declared boolean-position count, else the
    /// item's byte extent expressed in bits. A group defers to <see cref="ExtentBits"/> so a nested bit run is laid
    /// out by the same rules (§8.5.1.6.3 applies "within that group" at every level).</summary>
    public static int WidthBits(DataItem item) =>
        IsBitLeaf(item) ? item.Pic!.Length
        : item.IsElementary ? item.ElementaryByteWidth * BitsPerCharacter
        : ExtentBits(item);

    /// <summary>
    /// The total bit extent of <paramref name="group"/> — the §8.5.1.6.3 cursor walk over its NON-redefining
    /// children (a redefining child overlays its target and adds no storage, §13.18.44).
    ///
    /// <para>The four placement rules, in the order the standard states them:</para>
    /// <list type="number">
    ///   <item>A bit item <b>immediately following an elementary bit data item or bit group item OF THE SAME
    ///         LEVEL</b> goes at the next bit position — the ONLY case that shares a byte.</item>
    ///   <item>Any OTHER bit item is aligned "at the first bit position of the first available byte" — after a
    ///         character item, after a bit item of a DIFFERENT level, or as the group's first item.</item>
    ///   <item>A non-bit item advances to its natural boundary first; the skipped bits are §8.5.1.6.3's implicit
    ///         filler "as needed to advance alignment to a required natural boundary for the next item".</item>
    ///   <item>A trailing partial byte is filled: "Following a bit data item that is the last data item in a
    ///         record that is an alphanumeric group or strongly-typed group item, as needed to increase the number
    ///         of bits to fill an integral number of characters." §13.18.29.4 GR3 makes every group without a
    ///         GROUP-USAGE clause an alphanumeric group item, so this fires for all of them.</item>
    /// </list>
    /// <para>⚠ Every filler bit counted above is REQUIRED to be counted by <b>§15.50.4 r5</b> ("the returned
    /// length shall include the number of implicit FILLER positions"). It is counted by construction — the cursor
    /// advances through filler rather than skipping it.</para>
    /// <para>⚠ The NOTE under the trailing-filler rule excludes "the end of a record that is entirely a bit group,
    /// the end of a level 77 item, or the end of a level 1 elementary item" — which is why an ELEMENTARY bit item
    /// never comes through here and keeps its exact bit count (see <see cref="WidthBits"/>).</para>
    /// </summary>
    public static int ExtentBits(DataItem group)
    {
        int cursor = 0;
        DataItem? prev = null;
        foreach (var c in group.Children)
        {
            if (c.RedefinesTargetName is not null) continue;   // overlays its target — no advance (§13.18.44)

            // Rule 1 vs 2: sharing a byte requires the PREVIOUS sibling to be a bit item AT THE SAME LEVEL. Any
            // other predecessor (a character item, a differently-levelled bit item, or nothing at all) sends this
            // item to the first bit of the next available byte.
            bool sharesByte = IsBitItem(c) && prev is not null && IsBitItem(prev) && prev.Level == c.Level;
            if (!sharesByte) cursor = RoundUpToByte(cursor);   // rules 2 and 3 — the same advance, different reasons

            cursor += WidthBits(c) * (c.Occurs ?? 1);
            prev = c;
        }
        // Rule 4 — the trailing filler: stated for "a record that is an alphanumeric group or strongly-typed group
        // item" (§13.18.29.4 GR3 makes every group WITHOUT a GROUP-USAGE clause alphanumeric), and its NOTE excludes
        // "the end of a record that is entirely a bit group" — so a GROUP-USAGE BIT group (D20/PB79) keeps its EXACT
        // bit extent (its PICTURE 1(m) length, §15.50.4 r1); its character OCCUPANCY is still Characters(extent).
        return group.GroupUsage is GroupUsage.Bit ? cursor : RoundUpToByte(cursor);
    }

    /// <summary>The start-bit offset of DIRECT child <paramref name="child"/> within <paramref name="group"/>,
    /// by the same four placement rules as <see cref="ExtentBits"/> — one walk, one law (kb/Work PB132;
    /// §14.9.4.3 SR6/SR8 ask whether a BY REFERENCE bit argument "is aligned on a byte boundary"). A
    /// redefining child overlays its target (§13.18.44), so its offset IS the target's — resolved here by
    /// name. Returns -1 when the child is not found (an unmodelled overlay chain — callers must not reject
    /// on -1).</summary>
    public static int StartBitWithin(DataItem group, DataItem child)
    {
        // §13.18.44.3 SR17 bars a dynamic item under REDEFINES, so chasing the overlay chain terminates.
        for (int hops = 0; child.RedefinesTargetName is not null && hops < 64; hops++)
        {
            DataItem? target = null;
            foreach (var c in group.Children)
                if (string.Equals(c.CobolName, child.RedefinesTargetName, System.StringComparison.OrdinalIgnoreCase))
                { target = c; break; }
            if (target is null) return -1;
            child = target;
        }
        int cursor = 0;
        DataItem? prev = null;
        foreach (var c in group.Children)
        {
            if (c.RedefinesTargetName is not null) continue;
            bool sharesByte = IsBitItem(c) && prev is not null && IsBitItem(prev) && prev.Level == c.Level;
            if (!sharesByte) cursor = RoundUpToByte(cursor);
            if (ReferenceEquals(c, child)) return cursor;
            cursor += WidthBits(c) * (c.Occurs ?? 1);
            prev = c;
        }
        return -1;
    }

    /// <summary>The next byte boundary at or after <paramref name="bits"/> — "the first bit position of the first
    /// available byte" (§8.5.1.6.3), and equally the implicit-filler advance to a natural boundary. Already
    /// byte-aligned input is returned unchanged, so no phantom filler byte is generated.</summary>
    private static int RoundUpToByte(int bits) =>
        (bits + BitsPerCharacter - 1) / BitsPerCharacter * BitsPerCharacter;

    /// <summary>The bit offset of <paramref name="item"/> within <paramref name="ancestor"/> — the sum of each
    /// intervening level's <see cref="StartBitWithin"/> placement, so a DEEP descendant is located by the same four
    /// rules as a direct child. Returns -1 when any level cannot be resolved (an unmodelled overlay chain, or
    /// <paramref name="item"/> not being a descendant); callers must not compute a layout from -1.
    /// <para>⛔ This is the honest "fixed prefix" for an OCCURS DEPENDING extent over a bit-bearing subtree
    /// (§13.18.38.4 GR8 read through §8.5.1.6.3), and it is NOT <c>ExtentBits(ancestor) − elem × max</c>: rule 4's
    /// trailing filler belongs AFTER the variable tail, so subtracting the tail from a byte-rounded total charges
    /// the filler to the prefix. The group's own filler is recovered by taking the CEILING of the current extent
    /// (<see cref="Characters"/>), which is what rule 4 says it is.</para></summary>
    public static int StartBitOf(DataItem ancestor, DataItem item)
    {
        int off = 0;
        for (DataItem? n = item; !ReferenceEquals(n, ancestor); n = n.Parent)
        {
            if (n?.Parent is not { } parent) return -1;
            int at = StartBitWithin(parent, n);
            if (at < 0) return -1;
            off += at;
        }
        return off;
    }

    /// <summary>The character positions a bit extent occupies — the ceiling, which is also the shape
    /// <b>§15.50.4 r9</b> requires ("if argument-1 does not occupy an integral number of positions, the returned
    /// value is rounded to the next larger integer value"). With rule 4 above the extent is already integral for a
    /// group; the ceiling is what makes an ELEMENTARY bit item's character occupancy right.</summary>
    public static int Characters(int bits) => (bits + BitsPerCharacter - 1) / BitsPerCharacter;
}
