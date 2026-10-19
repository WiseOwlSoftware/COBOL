// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;

namespace CobolNet.Runtime.IO.Store;

/// <summary>A value too large for a leaf, stored as a CHAIN of <see cref="PageKind.Overflow"/> pages
/// (docs/rearchitecture/DESIGN-runtime-library.md 2.2.1.4, the OVERFLOW layout): each page's body begins with the next
/// page's generation (8 bytes), so the chain is stamped like every other pointer (2.2.1.6 R3), then up to
/// <see cref="PayloadBytes"/> payload bytes; the header's <c>entries</c> is the page's payload byte count, its <c>next</c>
/// the next page, and flags bit 0 marks the last. The leaf holds the chain's head POINTER and the value's total length,
/// so a reader knows where the value ends before it reads a page of it.</summary>
internal static class OverflowChain
{
    /// <summary>The payload bytes one chain page carries.</summary>
    public const int PayloadBytes = PageHeader.BodyBytes - NextGenerationBytes;

    private const int NextGenerationBytes = 8;

    private const byte LastFlag = 0x01;

    /// <summary>The pages a value of <paramref name="bytes"/> bytes occupies.</summary>
    public static int PagesFor(long bytes) => (int)((bytes + PayloadBytes - 1) / PayloadBytes);

    /// <summary>Write <paramref name="payload"/> as a chain through <paramref name="sink"/>, last page first, so every page
    /// is written once and knows its successor's page and generation; the head POINTER, or
    /// <see cref="PagePointer.None"/> once the medium has refused a write.</summary>
    public static PagePointer Write(IPageSink sink, ReadOnlySpan<byte> payload)
    {
        if (payload.IsEmpty) throw new ArgumentException("an empty value is stored inline", nameof(payload));
        int pages = PagesFor(payload.Length);
        var next = PagePointer.None;
        var body = new byte[PageHeader.BodyBytes];
        for (int i = pages - 1; i >= 0; i--)
        {
            int at = i * PayloadBytes;
            int share = Math.Min(PayloadBytes, payload.Length - at);
            Array.Clear(body);
            BinaryPrimitives.WriteUInt64LittleEndian(body, next.Generation);
            payload.Slice(at, share).CopyTo(body.AsSpan(NextGenerationBytes));
            next = sink.Write(PageKind.Overflow, body, next.IsNone ? LastFlag : (byte)0, (ushort)share, next.Page, default);
            if (next.IsNone) return PagePointer.None;
        }
        return next;
    }

    /// <summary>Read the chain whose head is <paramref name="head"/> and whose value is <paramref name="total"/> bytes,
    /// verifying every page through its stamped pointer (2.2.1.6 R3) with <paramref name="read"/>. The value on
    /// <see cref="PageCheck.Whole"/>; a chain whose pages do not add up to <paramref name="total"/> is damaged
    /// (<see cref="PageCheck.ChecksumFails"/>: its pages verified, its content cannot be the value).</summary>
    public static PageCheck Read(PagePointer head, uint total, Func<PagePointer, byte[], PageCheck> read, out byte[] value)
    {
        var bytes = new byte[total];
        int filled = 0;
        var check = Walk(head, total, read, (page, share) =>
        {
            page.AsSpan(PageHeader.Bytes + NextGenerationBytes, share).CopyTo(bytes.AsSpan(filled));
            filled += share;
        }, null);
        value = check == PageCheck.Whole ? bytes : [];
        return check;
    }

    /// <summary>The pages of the chain (for their release), each with the stamp it was read through.</summary>
    public static PageCheck Pages(PagePointer head, uint total, Func<PagePointer, byte[], PageCheck> read, List<PagePointer> into) =>
        Walk(head, total, read, null, into);

    private static PageCheck Walk(PagePointer head, uint total, Func<PagePointer, byte[], PageCheck> read,
        Action<byte[], int>? payload, List<PagePointer>? pages)
    {
        var page = new byte[PageHeader.PageSize];
        var at = head;
        long seen = 0;
        while (true)
        {
            if (at.IsNone) return PageCheck.ChecksumFails;
            var check = read(at, page);
            if (check != PageCheck.Whole) return check;
            var h = PageHeader.Decode(page);
            if (h.Kind != PageKind.Overflow || h.Entries > PayloadBytes || seen + h.Entries > total)
                return PageCheck.ChecksumFails;
            payload?.Invoke(page, h.Entries);
            pages?.Add(at);
            seen += h.Entries;
            if ((h.Flags & LastFlag) != 0) return seen == total ? PageCheck.Whole : PageCheck.ChecksumFails;
            at = new PagePointer(h.Next, BinaryPrimitives.ReadUInt64LittleEndian(page.AsSpan(PageHeader.Bytes)));
        }
    }
}
