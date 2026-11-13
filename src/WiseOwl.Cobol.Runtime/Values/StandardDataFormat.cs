// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace WiseOwl.Cobol.Runtime;

/// <summary>One part of a group's layout in STANDARD DATA FORMAT (§1985:IV.4.3.4: "The size of an elementary data item or a
/// group item is the number of characters in standard data format of the item. Synchronization and usage may cause a
/// difference between this size and that required for internal representation"), the compiler's description of where a
/// part sits in the group's storage image and whether it is stored in a form other than its standard characters
/// (kb/Work PB5378). A part is an elementary item (<see cref="Members"/> null) or a group, and repeats
/// <see cref="Count"/> times, <see cref="Width"/> storage bytes apart.</summary>
public sealed class StandardDataPart
{
    internal int Offset { get; }
    internal int Width { get; }
    internal NumProfile? Profile { get; }
    internal int Count { get; }
    internal StandardDataPart[]? Members { get; }

    /// <param name="offset">The first occurrence's byte offset from the start of the enclosing group (the whole group for a top
    /// part, the enclosing element for a member).</param>
    /// <param name="width">The storage bytes of one occurrence.</param>
    /// <param name="profile">The numeric profile of an elementary item stored as binary or packed decimal, whose standard
    /// data format is its digits; null for an item whose storage IS its standard characters.</param>
    /// <param name="count">The occurrences (an OCCURS DEPENDING table at its maximum).</param>
    /// <param name="members">The parts of a group item's one occurrence, null for an elementary item.</param>
    public StandardDataPart(int offset, int width, NumProfile? profile = null, int count = 1, StandardDataPart[]? members = null)
    {
        Offset = offset;
        Width = width;
        Profile = profile;
        Count = count;
        Members = members;
    }
}

/// <summary>⛔ THE ONE COMPOSER OF A GROUP'S CHARACTERS IN STANDARD DATA FORMAT (kb/Work PB5378): the image a statement that
/// takes "the number of characters in standard data format" of a group (§1985:IV.4.3.4), such as SEND's text length
/// (§1985:XIV.3.6.4 GR3), counts and transfers. An elementary item of binary or packed usage contributes the characters
/// <see cref="CobolNum.FormatStandardData"/> gives it (its digits, a sign fused onto the last), the same <see cref="Characters"/> gives an elementary operand of the same
/// statement, and every other part contributes its storage characters.</summary>
public static class CobolStandardData
{
    /// <summary>The standard data format characters of the group whose storage image is <paramref name="storage"/>. An OCCURS
    /// DEPENDING table past the storage the image holds (the image of such a group ends at the table's current count)
    /// contributes nothing.</summary>
    public static string Image(string storage, StandardDataPart[] parts)
    {
        var text = new StringBuilder(storage.Length);
        Append(text, storage, 0, parts);
        return text.ToString();
    }

    /// <summary>The standard data format characters of an elementary binary or packed item whose DISPLAY image (a leading minus when
    /// negative) is <paramref name="displayImage"/>: the characters <see cref="Image"/> gives the same item inside a group.</summary>
    public static string Characters(string displayImage, in NumProfile profile) =>
        CobolNum.FormatStandardData(CobolNum.ParseDisplay(displayImage, profile), profile);

    private static void Append(StringBuilder text, string storage, int origin, StandardDataPart[] parts)
    {
        foreach (var part in parts)
            for (int occurrence = 0; occurrence < part.Count; occurrence++)
            {
                int at = origin + part.Offset + occurrence * part.Width;
                if (at >= storage.Length) break;
                if (part.Members is { } members) Append(text, storage, at, members);
                else if (part.Profile is { } profile && at + part.Width <= storage.Length)
                    text.Append(CobolNum.FormatStandardData(CobolNum.ParseImage(storage.Substring(at, part.Width), profile), profile));
                else text.Append(storage, at, Math.Min(part.Width, storage.Length - at));
            }
    }
}
