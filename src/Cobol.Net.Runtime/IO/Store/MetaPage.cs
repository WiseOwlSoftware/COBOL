// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;

namespace CobolNet.Runtime.IO.Store;

/// <summary>A META page of the paged keyed store (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.4): pages 0 and 1
/// are the two COMMITTED slots, written alternately by promotions, and page 2 is the WORK slot, written by every
/// statement that changes the file (§2.2.1.6, §2.2.1.15). One encoder (<see cref="Encode"/>) and one decoder
/// (<see cref="Decode"/>); <see cref="Describes"/> is the §14.9.27.4 GR10 read of the attributes the file records.
/// <para>The layout after the 32-byte page header: the magic <c>CBNFSTR</c> and the format version, the §9.1.6
/// attributes (organization, record type, minimum and maximum record size, key count), the coherence
/// <see cref="Generation"/>, the three mints, the page count, the five root POINTERS, the base
/// (<see cref="CommittedGeneration"/>), the flags byte whose bit 0 is <see cref="Promoting"/>,
/// <see cref="Committing"/>, and from offset 169 each key's descriptor in the whole-store header's encoding
/// (<see cref="RecordFraming.WriteKeyDescriptor"/>, the ONE key codec), an alternate key's descriptor followed by the
/// POINTER to its tree.</para></summary>
internal sealed record MetaPage(
    FixedFileAttributes Attributes,
    ulong Generation,
    ulong ReleaseMint,
    ulong PageCount,
    ulong TransactionMint,
    PagePointer PrimeRoot,
    PagePointer FreeRoot,
    PagePointer LedgerRoot,
    PagePointer TransactionsRoot,
    ulong CommittedGeneration,
    ulong StampMint,
    bool Promoting,
    ulong Committing,
    PagePointer HoldingsRoot,
    IReadOnlyList<PagePointer> AlternateRoots)
{
    /// <summary>The first COMMITTED slot.</summary>
    public const uint SlotA = 0;

    /// <summary>The second COMMITTED slot.</summary>
    public const uint SlotB = 1;

    /// <summary>The WORK slot.</summary>
    public const uint WorkSlot = 2;

    /// <summary>The pages a newly created store holds: the three meta pages.</summary>
    public const uint MetaPages = 3;

    /// <summary>The paged format's version, the byte after the magic. Version 5 is the WHOLE-STORE header of
    /// <see cref="RecordFraming"/> (its magic at offset 0, where a paged store has its page header), so the paged format
    /// takes 6 and a file of either is <see cref="StoreFormat.Foreign"/> to the other's reader: no reader of the old
    /// format is kept (§2.2.1.3 decision 1).</summary>
    public const byte FormatVersion = 6;

    private static ReadOnlySpan<byte> Magic => "CBNFSTR"u8;

    private const int MagicOffset = 32;
    private const int OrganizationOffset = 40;
    private const int RecordTypeOffset = 41;
    private const int MinimumOffset = 42;
    private const int MaximumOffset = 46;
    private const int KeyCountOffset = 50;
    private const int GenerationOffset = 52;
    private const int ReleaseMintOffset = 60;
    private const int PageCountOffset = 68;
    private const int TransactionMintOffset = 76;
    private const int PrimeRootOffset = 84;
    private const int FreeRootOffset = 96;
    private const int LedgerRootOffset = 108;
    private const int TransactionsRootOffset = 120;
    private const int CommittedGenerationOffset = 132;
    private const int StampMintOffset = 140;
    private const int FlagsOffset = 148;
    private const int CommittingOffset = 149;
    private const int HoldingsRootOffset = 157;

    /// <summary>Where the per-key descriptors begin.</summary>
    public const int KeysOffset = 169;

    /// <summary>The flags byte's PROMOTING bit (§2.2.1.6 R5).</summary>
    private const byte PromotingFlag = 0x01;

    /// <summary>The empty store's meta for <paramref name="attributes"/> — what creation writes to all three slots
    /// (§14.9.27.4 GR18: <i>"After the creation of the file, the file contains no records"</i>).</summary>
    public static MetaPage Empty(FixedFileAttributes attributes) => new(
        attributes, Generation: 0, ReleaseMint: 0, PageCount: MetaPages, TransactionMint: 0,
        PagePointer.None, PagePointer.None, PagePointer.None, PagePointer.None,
        CommittedGeneration: 0, StampMint: 0, Promoting: false, Committing: 0, PagePointer.None,
        Enumerable.Repeat(PagePointer.None, AlternateKeyCount(attributes)).ToArray());

    /// <summary>The alternate keys of <paramref name="attributes"/>: every key after the prime key of an INDEXED file;
    /// none for a RELATIVE one (whose record-number tree is <see cref="PrimeRoot"/>).</summary>
    public static int AlternateKeyCount(FixedFileAttributes attributes) => Math.Max(0, attributes.Keys.Count - 1);

    /// <summary>Whether the attributes' key descriptors fit the meta page. A declaration past the page's capacity is
    /// refused at the OPEN with '39' (§2.2.1.4; DOC-A.1-129).</summary>
    public static bool Fits(FixedFileAttributes attributes) => KeyBytes(attributes) <= PageHeader.PageSize - KeysOffset;

    /// <summary>The page this meta is written as at slot <paramref name="self"/>, sealed.</summary>
    public byte[] Encode(uint self)
    {
        if (!Fits(Attributes))
            throw new InvalidOperationException("the key descriptors exceed the meta page; MetaPage.Fits refuses the OPEN first");
        if (AlternateRoots.Count != AlternateKeyCount(Attributes))
            throw new InvalidOperationException("one alternate root per alternate key");
        var page = new byte[PageHeader.PageSize];
        new PageHeader(PageKind.Meta, 0, 0, Generation, self, 0).Encode(page);
        var s = page.AsSpan();
        Magic.CopyTo(s[MagicOffset..]);
        s[MagicOffset + Magic.Length] = FormatVersion;
        s[OrganizationOffset] = Attributes.Organization == FixedFileAttributes.Indexed ? (byte)'I' : (byte)'R';
        s[RecordTypeOffset] = Attributes.Varying ? (byte)'V' : (byte)'F';
        BinaryPrimitives.WriteInt32LittleEndian(s[MinimumOffset..], Attributes.MinRecordSize);
        BinaryPrimitives.WriteInt32LittleEndian(s[MaximumOffset..], Attributes.MaxRecordSize);
        BinaryPrimitives.WriteUInt16LittleEndian(s[KeyCountOffset..], (ushort)Attributes.Keys.Count);
        BinaryPrimitives.WriteUInt64LittleEndian(s[GenerationOffset..], Generation);
        BinaryPrimitives.WriteUInt64LittleEndian(s[ReleaseMintOffset..], ReleaseMint);
        BinaryPrimitives.WriteUInt64LittleEndian(s[PageCountOffset..], PageCount);
        BinaryPrimitives.WriteUInt64LittleEndian(s[TransactionMintOffset..], TransactionMint);
        PrimeRoot.Write(s[PrimeRootOffset..]);
        FreeRoot.Write(s[FreeRootOffset..]);
        LedgerRoot.Write(s[LedgerRootOffset..]);
        TransactionsRoot.Write(s[TransactionsRootOffset..]);
        BinaryPrimitives.WriteUInt64LittleEndian(s[CommittedGenerationOffset..], CommittedGeneration);
        BinaryPrimitives.WriteUInt64LittleEndian(s[StampMintOffset..], StampMint);
        s[FlagsOffset] = Promoting ? PromotingFlag : (byte)0;
        BinaryPrimitives.WriteUInt64LittleEndian(s[CommittingOffset..], Committing);
        HoldingsRoot.Write(s[HoldingsRootOffset..]);
        using (var keys = new MemoryStream(page, KeysOffset, PageHeader.PageSize - KeysOffset, writable: true))
        {
            Span<byte> pointer = stackalloc byte[PagePointer.Bytes];
            for (int k = 0; k < Attributes.Keys.Count; k++)
            {
                RecordFraming.WriteKeyDescriptor(keys, Attributes.Keys[k]);
                if (k == 0) continue;
                AlternateRoots[k - 1].Write(pointer);
                keys.Write(pointer);
            }
        }
        PageHeader.Seal(page);
        return page;
    }

    /// <summary>The meta <paramref name="page"/> holds as slot <paramref name="self"/>, or null when it does not
    /// VALIDATE (§2.2.1.6 R6: its checksum, its kind, its <c>self</c>, the magic and version, and a layout this build
    /// writes) — a torn, foreign or never-written slot.</summary>
    public static MetaPage? Decode(ReadOnlySpan<byte> page, uint self)
    {
        if (page.Length < PageHeader.PageSize || !PageHeader.ChecksumHolds(page)) return null;
        var header = PageHeader.Decode(page);
        if (header.Kind != PageKind.Meta || header.Self != self || !CarriesMagic(page)) return null;
        string organization = page[OrganizationOffset] switch
        {
            (byte)'R' => FixedFileAttributes.Relative,
            (byte)'I' => FixedFileAttributes.Indexed,
            _ => "",
        };
        if (organization.Length == 0 || page[RecordTypeOffset] is not ((byte)'F' or (byte)'V')) return null;
        int min = BinaryPrimitives.ReadInt32LittleEndian(page[MinimumOffset..]);
        int max = BinaryPrimitives.ReadInt32LittleEndian(page[MaximumOffset..]);
        if (min < 0 || max < 0) return null;
        int keyCount = BinaryPrimitives.ReadUInt16LittleEndian(page[KeyCountOffset..]);
        if (page[FlagsOffset] is not (0 or PromotingFlag)) return null;
        var keys = new List<FixedFileAttributes.KeyDescriptor>(keyCount);
        var alternates = new List<PagePointer>(Math.Max(0, keyCount - 1));
        using (var stream = new MemoryStream(page[KeysOffset..PageHeader.PageSize].ToArray(), writable: false))
        {
            var pointer = new byte[PagePointer.Bytes];
            for (int k = 0; k < keyCount; k++)
            {
                if (RecordFraming.ReadKeyDescriptor(stream) is not { } key) return null;
                keys.Add(key);
                if (k == 0) continue;
                if (stream.Read(pointer, 0, pointer.Length) != pointer.Length) return null;
                alternates.Add(PagePointer.Read(pointer));
            }
        }
        return new MetaPage(
            new FixedFileAttributes(organization, page[RecordTypeOffset] == (byte)'V', min, max, keys),
            Generation: BinaryPrimitives.ReadUInt64LittleEndian(page[GenerationOffset..]),
            ReleaseMint: BinaryPrimitives.ReadUInt64LittleEndian(page[ReleaseMintOffset..]),
            PageCount: BinaryPrimitives.ReadUInt64LittleEndian(page[PageCountOffset..]),
            TransactionMint: BinaryPrimitives.ReadUInt64LittleEndian(page[TransactionMintOffset..]),
            PrimeRoot: PagePointer.Read(page[PrimeRootOffset..]),
            FreeRoot: PagePointer.Read(page[FreeRootOffset..]),
            LedgerRoot: PagePointer.Read(page[LedgerRootOffset..]),
            TransactionsRoot: PagePointer.Read(page[TransactionsRootOffset..]),
            CommittedGeneration: BinaryPrimitives.ReadUInt64LittleEndian(page[CommittedGenerationOffset..]),
            StampMint: BinaryPrimitives.ReadUInt64LittleEndian(page[StampMintOffset..]),
            Promoting: page[FlagsOffset] == PromotingFlag,
            Committing: BinaryPrimitives.ReadUInt64LittleEndian(page[CommittingOffset..]),
            HoldingsRoot: PagePointer.Read(page[HoldingsRootOffset..]),
            AlternateRoots: alternates);
    }

    /// <summary>Whether <paramref name="page"/> carries this format's magic and version at the meta offset, whether or
    /// not it validates: the test that tells a DAMAGED paged store from a foreign file.</summary>
    public static bool CarriesMagic(ReadOnlySpan<byte> page) =>
        page.Length >= MagicOffset + Magic.Length + 1
        && page.Slice(MagicOffset, Magic.Length).SequenceEqual(Magic)
        && page[MagicOffset + Magic.Length] == FormatVersion;

    /// <summary>The COMMITTED meta among the decoded slots <paramref name="a"/> (page 0) and <paramref name="b"/>
    /// (page 1): the valid slot with the higher generation (§2.2.1.6 R6 step 1), page 0 on a tie (creation writes both
    /// at generation 0); null when neither validates.</summary>
    public static (MetaPage Meta, uint Slot)? Committed(MetaPage? a, MetaPage? b) => (a, b) switch
    {
        (null, null) => null,
        ({ } x, null) => (x, SlotA),
        (null, { } y) => (y, SlotB),
        ({ } x, { } y) => y.Generation > x.Generation ? (y, SlotB) : (x, SlotA),
    };

    /// <summary>The §9.1.6 fixed file attributes the paged store at <paramref name="path"/> records — the read half of
    /// §14.9.27.4 GR10 (<i>"The implementor defines which of the fixed-file attributes are validated during the execution
    /// of the OPEN statement"</i>). It reads pages 0 and 1 through a short-lived auxiliary handle and takes the valid slot
    /// with the higher generation; a file with no bytes is <see cref="StoreFormat.Empty"/>, one whose meta pages carry
    /// this format's magic but whose committed slots both fail is <see cref="StoreFormat.Damaged"/> (the adoption rule's
    /// '30'), and anything else is <see cref="StoreFormat.Foreign"/> ('39') — a whole-store file of format 5 included.
    /// <para>An I/O failure answers <see cref="StoreFormat.Empty"/>, never a conflict: this bounded read is bookkeeping,
    /// and the authority questions are answered by the connector's own handle (<c>RecordFraming.ReadHeader</c>'s
    /// rule).</para></summary>
    public static StoreFormat Describes(string path, out FixedFileAttributes? attributes)
    {
        attributes = null;
        try
        {
            using var fs = HostFile.OpenAuxiliary(path, FileMode.Open, FileAccess.Read);
            if (fs.Length == 0) return StoreFormat.Empty;
            var pages = new byte[MetaPages][];
            for (uint p = 0; p < MetaPages; p++)
            {
                pages[p] = new byte[PageHeader.PageSize];
                fs.Position = (long)p * PageHeader.PageSize;
                fs.ReadAtLeast(pages[p], PageHeader.PageSize, throwOnEndOfStream: false);
            }
            if (Committed(Decode(pages[SlotA], SlotA), Decode(pages[SlotB], SlotB)) is { } committed)
            {
                attributes = committed.Meta.Attributes;
                return StoreFormat.Described;
            }
            return pages.Any(p => CarriesMagic(p)) ? StoreFormat.Damaged : StoreFormat.Foreign;
        }
        catch (IOException) { return StoreFormat.Empty; }
        catch (UnauthorizedAccessException) { return StoreFormat.Empty; }
        catch (ArgumentException) { return StoreFormat.Empty; }
        catch (NotSupportedException) { return StoreFormat.Empty; }
    }

    /// <summary>The bytes the key descriptors (and the alternate roots) of <paramref name="attributes"/> occupy, measured
    /// with the one key encoder so the capacity test cannot drift from the layout.</summary>
    private static long KeyBytes(FixedFileAttributes attributes)
    {
        using var measure = new MemoryStream();
        foreach (var key in attributes.Keys) RecordFraming.WriteKeyDescriptor(measure, key);
        return measure.Length + (long)PagePointer.Bytes * AlternateKeyCount(attributes);
    }
}
