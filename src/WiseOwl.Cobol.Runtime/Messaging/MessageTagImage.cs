// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// ⛔ THE ONE STORAGE-IMAGE CODEC FOR A MESSAGE-TAG ITEM (kb/Work PB4838; docs/CONFORMANCE.md §7 DOC-A.1-212). A message tag's
/// VALUE is a <see cref="MessageTag"/> and its STORAGE is the 8 character positions every managed slot reserves; the standard
/// leaves a message-tag item's representation to the implementor (§2023:13.18.60.4 GR9), and the owner decided (2026-11-06) that
/// a file carries those 8 positions as a token the run unit reads back as the same tag. The token is the tag's identifier in
/// the run unit's <see cref="SlotImageTable"/> (the process epoch in the high four positions, a process-wide block number and the low 12 bits of the message serial number in the low four, kb/Work PB4926), big-endian like every binary item (DOC-A.1-205/207), and NULL is
/// eight X"00" positions, the same NULL image every pointer category has (<see cref="PointerImage.NullImage"/>).
/// <para>A token the run unit never assigned — one a file brought in from another run unit, or short-record padding — is NOT
/// NULL: it decodes to a tag of party <see cref="MessageParty.Unresolved"/> that keeps its positions and images back unchanged, and
/// naming it in SEND or RECEIVE raises <c>EC-MCS-INVALID-TAG</c> (the tag belongs to no message control system of this run unit).</para>
/// </summary>
public static class MessageTagImage
{
    /// <summary>The storage image of a message-tag VALUE (<see cref="MessageTag.Null"/> → <see cref="PointerImage.NullImage"/>).</summary>
    public static string Of(MessageTag tag)
    {
        if (tag.IsNull) return PointerImage.NullImage;
        if (tag.Party == MessageParty.Unresolved) return tag.Name!;
        long id = RunUnit.Current.SlotImages.TagId(tag);
        return string.Create(PointerImage.Width, id, static (span, v) =>
        {
            for (int i = 0; i < PointerImage.Width; i++) span[i] = StorageByte.ToChar((byte)(v >> (8 * (PointerImage.Width - 1 - i))));
        });
    }

    /// <summary>The message-tag VALUE an image stands for — the inverse of <see cref="Of"/>.</summary>
    public static MessageTag TagOf(string image)
    {
        if (image == PointerImage.NullImage) return MessageTag.Null;
        if (image.Length == PointerImage.Width && image.All(StorageByte.HasByte))
        {
            long id = 0;
            foreach (char c in image) id = (id << 8) | StorageByte.ToByte(c);
            if (RunUnit.Current.SlotImages.TagOf(id) is { } tag) return tag;
        }
        return new MessageTag(null!, MessageParty.Unresolved, image, 0);
    }
}
