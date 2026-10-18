// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;

namespace CobolNet.Runtime.IO.Store;

/// <summary>A payload stored as a CHAIN of <see cref="PageKind.Overflow"/> pages (docs/rearchitecture/DESIGN-runtime-library.md
/// 2.2.1.4, the OVERFLOW layout): each page's body begins with the next page's generation (8 bytes), so the chain is
/// stamped like every other pointer (2.2.1.6 R3), then up to <see cref="PayloadBytes"/> payload bytes; the header's
/// <c>entries</c> is the payload byte count of the page, its <c>next</c> the next page, and flags bit 0 marks the last.
/// <para>⛔ INTERIM. Until the B+ tree of kb/Work PB3332 lands, the LEDGER (<see cref="Ledger"/>) and the FREE set are
/// serialized as one chain each (kb/Work PB3331, "Ledger"), so the page layer's crash matrix runs over real pages. PB3332
/// REPLACES both serializations with trees of its engine; it does not wrap them. The chain stays the overflow-value
/// codec.</para></summary>
internal static class PageChain
{
    /// <summary>The payload bytes one chain page carries.</summary>
    public const int PayloadBytes = PageHeader.BodyBytes - NextGenerationBytes;

    private const int NextGenerationBytes = 8;

    private const byte LastFlag = 0x01;

    /// <summary>The pages a payload of <paramref name="bytes"/> bytes occupies (0 for none: an empty structure has no
    /// chain and a <see cref="PagePointer.None"/> root).</summary>
    public static int PagesFor(long bytes) => (int)((bytes + PayloadBytes - 1) / PayloadBytes);

    /// <summary>Compose <paramref name="payload"/> over exactly <paramref name="pages"/> (in chain order; a page whose
    /// share is empty carries no payload), every page stamped <paramref name="generation"/>, and hand each page to
    /// <paramref name="write"/>; false as soon as a write is refused. The head pointer is <paramref name="head"/>.</summary>
    public static bool Compose(ReadOnlySpan<byte> payload, IReadOnlyList<uint> pages, ulong generation,
        Func<uint, byte[], bool> write, out PagePointer head)
    {
        head = PagePointer.None;
        if (pages.Count == 0)
            return payload.IsEmpty ? true : throw new InvalidOperationException("a payload needs at least one chain page");
        if (PagesFor(payload.Length) > pages.Count)
            throw new InvalidOperationException("the chain's pages cannot hold its payload");
        int at = 0;
        for (int i = 0; i < pages.Count; i++)
        {
            int share = Math.Min(PayloadBytes, payload.Length - at);
            bool last = i == pages.Count - 1;
            var page = new byte[PageHeader.PageSize];
            new PageHeader(PageKind.Overflow, last ? LastFlag : (byte)0, (ushort)share, generation, pages[i],
                last ? 0 : pages[i + 1]).Encode(page);
            BinaryPrimitives.WriteUInt64LittleEndian(page.AsSpan(PageHeader.Bytes), last ? 0 : generation);
            payload.Slice(at, share).CopyTo(page.AsSpan(PageHeader.Bytes + NextGenerationBytes));
            at += share;
            PageHeader.Seal(page);
            if (!write(pages[i], page)) return false;
        }
        head = new PagePointer(pages[0], generation);
        return true;
    }

    /// <summary>Read the chain whose head is <paramref name="head"/>, verifying every page through its stamped pointer
    /// (2.2.1.6 R3) with <paramref name="read"/>; the payload and the chain's pages (with the generation each was read
    /// at) on <see cref="PageCheck.Whole"/>, the first failing page's check otherwise.</summary>
    public static PageCheck Read(PagePointer head, Func<PagePointer, byte[], PageCheck> read,
        out byte[] payload, out List<PagePointer> chain)
    {
        payload = [];
        chain = [];
        if (head.IsNone) return PageCheck.Whole;
        var bytes = new MemoryStream();
        var at = head;
        var page = new byte[PageHeader.PageSize];
        while (true)
        {
            var check = read(at, page);
            if (check != PageCheck.Whole) return check;
            var h = PageHeader.Decode(page);
            if (h.Kind != PageKind.Overflow || h.Entries > PayloadBytes) return PageCheck.ChecksumFails;
            chain.Add(at);
            bytes.Write(page, PageHeader.Bytes + NextGenerationBytes, h.Entries);
            if ((h.Flags & LastFlag) != 0) break;
            at = new PagePointer(h.Next, BinaryPrimitives.ReadUInt64LittleEndian(page.AsSpan(PageHeader.Bytes)));
            if (at.IsNone || chain.Count > int.MaxValue / PageHeader.PageSize) return PageCheck.ChecksumFails;
        }
        payload = bytes.ToArray();
        return PageCheck.Whole;
    }

    /// <summary>The FREE set's interim payload: each page number, 4 bytes little-endian, ascending.</summary>
    public static byte[] EncodePageSet(IReadOnlyCollection<uint> pages)
    {
        var bytes = new byte[4 * pages.Count];
        int at = 0;
        foreach (uint p in pages)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at), p);
            at += 4;
        }
        return bytes;
    }

    /// <summary>The inverse of <see cref="EncodePageSet"/>; null for a payload that is not one.</summary>
    public static SortedSet<uint>? DecodePageSet(ReadOnlySpan<byte> payload)
    {
        if (payload.Length % 4 != 0) return null;
        var set = new SortedSet<uint>();
        for (int at = 0; at < payload.Length; at += 4)
            if (!set.Add(BinaryPrimitives.ReadUInt32LittleEndian(payload[at..]))) return null;
        return set;
    }
}
