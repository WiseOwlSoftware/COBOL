// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;
using System.Numerics;

namespace CobolNet.Runtime.IO.Store;

/// <summary>The kind byte of a keyed store page (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.4, the PAGE layout):
/// the first byte of every page, so a page read from anywhere says what it claims to be before its body is trusted.</summary>
internal enum PageKind : byte
{
    /// <summary>Pages 0 and 1 (the two COMMITTED slots) and page 2 (the WORK slot): <see cref="MetaPage"/>.</summary>
    Meta = (byte)'M',

    /// <summary>A B+ tree internal page (kb/Work PB3332).</summary>
    Internal = (byte)'I',

    /// <summary>A B+ tree leaf page (kb/Work PB3332).</summary>
    Leaf = (byte)'L',

    /// <summary>A page of an overflow value's chain: 8 bytes of the next page's generation, then payload, chained by
    /// <see cref="PageHeader.Next"/> (<see cref="OverflowChain"/>).</summary>
    Overflow = (byte)'O',

    /// <summary>A page no tree references; it keeps its header so a stale read of it is recognizable.</summary>
    Free = (byte)'F',
}

/// <summary>What a read through a stamped <see cref="PagePointer"/> found (2.2.1.6 R3): the page is WHOLE only when its
/// checksum holds, its <see cref="PageHeader.Self"/> is the page number read and its generation equals the pointer's
/// stamp. Every other answer is a TORN read — torn, read from the wrong offset, or an older version of the page (a lost
/// write over a reused page) — and is answered by the statement as §9.1.13.6 item 1's '30', never swallowed.</summary>
internal enum PageCheck
{
    /// <summary>The page is the one the pointer names.</summary>
    Whole,

    /// <summary>The CRC-32C over bytes 0..3 and 8..4095 (every byte but the checksum's own, 2.2.1.4) does not match: a
    /// torn or damaged page.</summary>
    ChecksumFails,

    /// <summary>The page's <c>self</c> is not the page number read.</summary>
    WrongSelf,

    /// <summary>The page is whole but carries another generation than the stamp: a lost or stale write.</summary>
    StaleGeneration,

    /// <summary>The medium refused the read (an <see cref="IOException"/>): the page's state is unknown.</summary>
    Unreadable,
}

/// <summary>The 32-byte header every page of a keyed store carries (2.2.1.4): kind, flags, entries, the CRC-32C checksum
/// of every other byte of the page — bytes 0..3 and 8..4095 (the ONE torn-page detector) — the generation that last wrote
/// the page, the page's own number and the next page of its chain. (The first draft of 2.2.1.4 checksummed bytes 8..4095
/// only, which left the kind, flags and entry count unprotected: a flipped bit in a leaf's entry count passed the check and
/// mis-sized the page. The layout golden flips every bit of every page kind.) One encoder (<see cref="Encode"/> + <see cref="Seal"/>) and one decoder (<see cref="Decode"/>
/// + <see cref="Check"/>). A tree page's <see cref="Marks"/> (bytes 24..27) are its tree's high-water marks on the tree's
/// ROOT and zeros on every other page; bytes 28..31 are reserved, 0.</summary>
internal readonly record struct PageHeader(PageKind Kind, byte Flags, ushort Entries, ulong Generation, uint Self, uint Next,
    TreeMarks Marks = default)
{
    /// <summary>The page size, a format constant (2.2.1.3 decision 2): the host's I/O block, the size the costs of
    /// 2.2.1.2 were measured at, and <c>HostFile.InputOutputAreaBytes</c>.</summary>
    public const int PageSize = HostFile.InputOutputAreaBytes;

    /// <summary>The header's size; the body starts here.</summary>
    public const int Bytes = 32;

    /// <summary>The body's size.</summary>
    public const int BodyBytes = PageSize - Bytes;

    private const int KindOffset = 0;
    private const int FlagsOffset = 1;
    private const int EntriesOffset = 2;
    private const int ChecksumOffset = 4;

    /// <summary>The first byte after the checksum field; the checksum covers bytes [0, ChecksumOffset) and
    /// [AfterChecksum, PageSize).</summary>
    private const int AfterChecksum = 8;

    private const int GenerationOffset = 8;
    private const int SelfOffset = 16;
    private const int NextOffset = 20;
    private const int DeficitOffset = 24;
    private const int OverhangOffset = 26;
    private const int ReservedOffset = 28;

    /// <summary>Write the header's fields into <paramref name="page"/> (the reserved bytes 28..31 zero, the checksum left
    /// for <see cref="Seal"/>, which runs once the body is in place).</summary>
    public void Encode(Span<byte> page)
    {
        page[KindOffset] = (byte)Kind;
        page[FlagsOffset] = Flags;
        BinaryPrimitives.WriteUInt16LittleEndian(page[EntriesOffset..], Entries);
        BinaryPrimitives.WriteUInt64LittleEndian(page[GenerationOffset..], Generation);
        BinaryPrimitives.WriteUInt32LittleEndian(page[SelfOffset..], Self);
        BinaryPrimitives.WriteUInt32LittleEndian(page[NextOffset..], Next);
        BinaryPrimitives.WriteUInt16LittleEndian(page[DeficitOffset..], Marks.Deficit);
        BinaryPrimitives.WriteUInt16LittleEndian(page[OverhangOffset..], Marks.Overhang);
        page[ReservedOffset..Bytes].Clear();
    }

    /// <summary>Stamp the checksum of a fully composed page.</summary>
    public static void Seal(Span<byte> page) =>
        BinaryPrimitives.WriteUInt32LittleEndian(page[ChecksumOffset..], Checksum(page));

    /// <summary>The header of <paramref name="page"/>, as written; trust it only after <see cref="Check"/>.</summary>
    public static PageHeader Decode(ReadOnlySpan<byte> page) => new(
        (PageKind)page[KindOffset],
        page[FlagsOffset],
        BinaryPrimitives.ReadUInt16LittleEndian(page[EntriesOffset..]),
        BinaryPrimitives.ReadUInt64LittleEndian(page[GenerationOffset..]),
        BinaryPrimitives.ReadUInt32LittleEndian(page[SelfOffset..]),
        BinaryPrimitives.ReadUInt32LittleEndian(page[NextOffset..]),
        new TreeMarks(BinaryPrimitives.ReadUInt16LittleEndian(page[DeficitOffset..]), BinaryPrimitives.ReadUInt16LittleEndian(page[OverhangOffset..])));

    /// <summary>2.2.1.6 R3's verification of <paramref name="page"/>, read from page number <paramref name="self"/>
    /// through a pointer stamped <paramref name="stamp"/>.</summary>
    public static PageCheck Check(ReadOnlySpan<byte> page, uint self, ulong stamp)
    {
        if (!ChecksumHolds(page)) return PageCheck.ChecksumFails;
        var h = Decode(page);
        if (h.Self != self) return PageCheck.WrongSelf;
        return h.Generation == stamp ? PageCheck.Whole : PageCheck.StaleGeneration;
    }

    /// <summary>Whether the checksum of <paramref name="page"/> holds (a meta page is reached by its fixed number, never
    /// through a stamp, so its reader asks only this and <c>self</c>).</summary>
    public static bool ChecksumHolds(ReadOnlySpan<byte> page) =>
        BinaryPrimitives.ReadUInt32LittleEndian(page[ChecksumOffset..]) == Checksum(page);

    /// <summary>The page's checksum: CRC-32C over every byte but the checksum field itself.</summary>
    public static uint Checksum(ReadOnlySpan<byte> page) =>
        ~Crc32CUpdate(Crc32CUpdate(uint.MaxValue, page[..ChecksumOffset]), page[AfterChecksum..PageSize]);

    /// <summary>CRC-32C (Castagnoli, reflected polynomial 0x82F63B78, initial value and final XOR all ones) — the
    /// checksum iSCSI, ext4 and Btrfs use, computed by the processor's CRC32 instruction where it has one
    /// (<see cref="BitOperations.Crc32C(uint, ulong)"/>).</summary>
    public static uint Crc32C(ReadOnlySpan<byte> bytes) => ~Crc32CUpdate(uint.MaxValue, bytes);

    private static uint Crc32CUpdate(uint crc, ReadOnlySpan<byte> bytes)
    {
        int i = 0;
        for (; i + 8 <= bytes.Length; i += 8)
            crc = BitOperations.Crc32C(crc, BinaryPrimitives.ReadUInt64LittleEndian(bytes[i..]));
        for (; i < bytes.Length; i++)
            crc = BitOperations.Crc32C(crc, bytes[i]);
        return crc;
    }
}
