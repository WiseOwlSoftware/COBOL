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
            bool sharesByte = IsBitLeaf(c) && prev is not null && IsBitLeaf(prev) && prev.Level == c.Level;
            if (!sharesByte) cursor = RoundUpToByte(cursor);   // rules 2 and 3 — the same advance, different reasons

            cursor += WidthBits(c) * (c.Occurs ?? 1);
            prev = c;
        }
        // Rule 4 — the trailing filler. Unconditional here because a group reaching this walk is an alphanumeric
        // group item (§13.18.29.4 GR3: no GROUP-USAGE clause specified or implied), which is exactly the case the
        // trailing-filler rule names.
        return RoundUpToByte(cursor);
    }

    /// <summary>The next byte boundary at or after <paramref name="bits"/> — "the first bit position of the first
    /// available byte" (§8.5.1.6.3), and equally the implicit-filler advance to a natural boundary. Already
    /// byte-aligned input is returned unchanged, so no phantom filler byte is generated.</summary>
    private static int RoundUpToByte(int bits) =>
        (bits + BitsPerCharacter - 1) / BitsPerCharacter * BitsPerCharacter;

    /// <summary>The character positions a bit extent occupies — the ceiling, which is also the shape
    /// <b>§15.50.4 r9</b> requires ("if argument-1 does not occupy an integral number of positions, the returned
    /// value is rounded to the next larger integer value"). With rule 4 above the extent is already integral for a
    /// group; the ceiling is what makes an ELEMENTARY bit item's character occupancy right.</summary>
    public static int Characters(int bits) => (bits + BitsPerCharacter - 1) / BitsPerCharacter;
}
