// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding.Model;

/// <summary>
/// ⛔ THE ONE ISO §2023:15.50.4 r1/r2/r3 LENGTH OF A FIXED ITEM — the value <c>FUNCTION LENGTH</c> folds to and the value
/// §2023:13.10.4 GR6 gives a <c>CONSTANT AS LENGTH OF data-name-2</c> ("determined as specified in the LENGTH intrinsic
/// function with the exception that when data-name-2 is an occurs-depending group item, the maximum size of the data
/// item is used"). It lived inside <c>IntrinsicBinder</c> while the CONSTANT entry read <see cref="DataItem.ImageWidth"/>
/// — a second, drifted copy that answered 4 for an alphanumeric group of X(2) + N(2), where the function answered 6,
/// and a bit item's byte OCCUPANCY where r1 counts its boolean positions (kb/Work PB1213). A data-model fact, so the
/// data-division and procedure-division binders both read it here.
/// </summary>
internal static class ItemLength
{
    /// <summary>§2023:15.50.4 r1/r2/r3 for a FIXED item (kb/Work PB61): an elementary boolean item's BOOLEAN positions
    /// (r1), an elementary usage-national item's NATIONAL positions (r2), and for everything else — an
    /// alphanumeric group, a DISPLAY leaf, a COMP/PACKED leaf, an INDEX/POINTER/PROGRAM-POINTER/COMP-1/COMP-2
    /// carrier — the length "in alphanumeric character positions" (r3), which is the byte width under WiseOwl COBOL's
    /// 1-byte-per-alphanumeric-position model (D-N1: national = 2 bytes = 2 positions inside an alphanumeric group).
    /// A group's byte width is its MAXIMUM allocation, so an occurs-depending group answers its maximum — GR6's
    /// exception for the CONSTANT entry; the FUNCTION LENGTH fold never reaches an ODO group (r4 is a runtime
    /// length). A dynamic-length item (r6) is never folded here: FUNCTION LENGTH reads it at run time and §2023:13.10.3
    /// SR12 bars it from a CONSTANT entry.</summary>
    internal static int Positions(DataItem item)
    {
        // THE ONE category reader (D20/PB79): an elementary item's own picture, a bit / national GROUP's as-if picture
        // — a bit group's boolean positions are its exact bit extent (no trailing filler), a national group's national
        // positions its character image width. Only an alphanumeric group falls to r3.
        if (item.OperandPic is { } pic)
        {
            if (pic.Category is PicCategory.Boolean) return pic.Length;                     // r1 — boolean positions
            // r2 — NATIONAL character positions, counted by THE ONE national-position authority
            // (Place.NationalWindow.PositionsOf, the same count the storage geometry and the byte-window gate
            // read), never re-derived as pic.Length. ⛔ The two differ for exactly the shape §2023:13.18.60.3 SR12's
            // national-form numeric made reachable (kb/Work PB646): a SIGN IS SEPARATE position is a character
            // position and not a digit position (§2023:13.18.52 GR6a), so `PIC S9(3) USAGE NATIONAL SIGN IS LEADING
            // SEPARATE` is FOUR national positions while pic.Length is its three digits — and r3's DISPLAY arm
            // below already counted the separate sign, so the two arms of ONE rule disagreed.
            // A national GROUP is not elementary: PositionsOf answers null and §2023:13.18.29.4 GR2b's as-if
            // PICTURE N(m) length is the count (the `?? pic.Length` arm).
            // r2 is worded over the USAGE ("an elementary data item of usage national"), and a category-national item
            // is always of usage national, so the usage alone is the test (kb/Work PB546).
            if (pic.Usage is Usage.National)
                return NationalWindow.PositionsOf(item) ?? pic.Length;                      // r2 — national positions
        }
        return item.ByteWidth;                                                              // r3 — alphanumeric positions ≡ bytes
    }

    /// <summary>⛔ THE X3.23-1985 SIZE OF AN ITEM: "The size of an elementary data item or a group item is the number of characters in standard data format of
    /// the item. Synchronization and usage may cause a difference between this size and the size of the storage the item occupies" (§1985:IV.4.3.4), the size every
    /// 1985 rule that names a size reads (the Communication Section records, kb/Work PB5290). An elementary item is its characters in standard data format: the
    /// positions of a DISPLAY item (its separate sign included), the digits of a numeric item of any other usage (a COMP 9(8) is 8, not the 4 bytes it occupies); a
    /// group is the sum of its subordinate items' sizes, each table counted for its occurrences and a redefining entry for nothing (its storage is its target's), so the
    /// slack bytes SYNCHRONIZED adds are not in it. An item whose standard data format the 1985 text leaves to the implementor (an index data item, a floating-point
    /// one) is its storage size.</summary>
    internal static int StandardDataFormatCharacters(DataItem item)
    {
        if (item.Pic is { } pic && !item.IsGroup)
            return IsStoredAsDigits(pic) ? pic.Digits : item.ByteWidth;
        return item.Children.Where(child => child.RedefinesTarget is null).Sum(child => StandardDataFormatCharacters(child) * (child.Occurs ?? 1));
    }

    /// <summary>Whether an elementary item's storage is not its standard data format: a numeric item of usage binary, packed
    /// decimal or COMP-5 is stored in a radix-2 or BCD form whose standard characters are its digits
    /// (<see cref="StandardDataFormatCharacters"/>).</summary>
    internal static bool IsStoredAsDigits(PicInfo pic) =>
        pic is { Category: PicCategory.Numeric, Usage: Usage.Binary or Usage.Packed or Usage.Comp5 };

    /// <summary>One part of <see cref="StandardLayout"/>: an elementary item or a group occurring <paramref name="Count"/> times,
    /// <paramref name="Width"/> storage bytes apart from <paramref name="Offset"/> in the enclosing group; <paramref name="Converted"/> is
    /// the elementary item whose storage is not its standard characters (null for an item stored as its own characters, and for
    /// a run of such items merged into one part), <paramref name="Members"/> the parts of a group's one occurrence.</summary>
    internal sealed record StandardPart(int Offset, int Width, DataItem? Converted, int Count, IReadOnlyList<StandardPart>? Members);

    /// <summary>⛔ THE LAYOUT OF A FIXED GROUP IN STANDARD DATA FORMAT (§1985:IV.4.3.4; kb/Work PB5378), or null when the group's storage
    /// image already IS its standard characters (no item of it is stored as binary or packed decimal) or has no fixed byte layout (a
    /// variable-length group, one holding a USAGE BIT item). WiseOwl COBOL lays no SYNCHRONIZED padding, so each item follows the
    /// last at the sum of the widths before it; a redefining entry is no part of the size
    /// (<see cref="StandardDataFormatCharacters"/>). The runtime composes the characters from it
    /// (<c>CobolStandardData.Image</c>).</summary>
    internal static IReadOnlyList<StandardPart>? StandardLayout(DataItem group)
    {
        if (!group.IsGroup || DataItem.HasVariableLengthSubordinate(group) || group.HasBitDescendant) return null;
        var parts = PartsOf(group);
        return parts.Any(ConvertsSomething) ? parts : null;

        static bool ConvertsSomething(StandardPart part) => part.Converted is not null || (part.Members?.Any(ConvertsSomething) ?? false);
    }

    private static List<StandardPart> PartsOf(DataItem group)
    {
        var parts = new List<StandardPart>();
        int offset = 0;
        foreach (var child in group.Children.Where(c => c.RedefinesTarget is null))
        {
            int count = child.Occurs ?? 1;
            int width = child.ByteWidth;
            if (child.IsGroup)
                parts.Add(new StandardPart(offset, width, null, count, PartsOf(child)));
            else if (child.Pic is { } pic && IsStoredAsDigits(pic))
                parts.Add(new StandardPart(offset, width, child, count, null));
            // An item stored as its own characters runs on from the plain run before it when no table sits between (the run's storage is contiguous).
            else if (count == 1 && parts.Count > 0 && parts[^1] is { Converted: null, Members: null, Count: 1 } run && run.Offset + run.Width == offset)
                parts[^1] = run with { Width = run.Width + width };
            else
                parts.Add(new StandardPart(offset, width, null, count, null));
            offset += width * count;
        }
        return parts;
    }
}
