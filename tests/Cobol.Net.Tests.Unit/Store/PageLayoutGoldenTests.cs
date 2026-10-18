// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;
using System.Text;
using CobolNet.Runtime.IO;
using CobolNet.Runtime.IO.Store;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace CobolNet.Tests.Unit.Store;

/// <summary>
/// ⛔ THE PAGED STORE'S BYTES ARE THE LAYOUT OF docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.4, AND EVERY PAGE
/// IS VERIFIED BY ITS CHECKSUM, ITS SELF AND ITS STAMP (2.2.1.6 R3; kb/Work PB3331). The expected bytes below are read
/// off the design's PAGE and META tables, offset by offset, never off the encoder: an empty relative store and an empty
/// two-key indexed store (the three meta pages), the checksum detecting one flipped bit anywhere in every page kind,
/// <c>self</c> catching a page read from the wrong offset and the stamp catching an older version of a page.
/// </summary>
public sealed class PageLayoutGoldenTests
{
    /// <summary>The CRC-32C check value of the nine ASCII digits — the standard test vector of the Castagnoli polynomial
    /// (RFC 3720 §B.4's polynomial; the "check" value 0xE3069283 of the CRC catalogue).</summary>
    [Fact]
    public void Crc32C_of_the_check_string_is_the_catalogued_value() =>
        Assert.Equal(0xE3069283u, PageHeader.Crc32C("123456789"u8));

    [Fact]
    public void An_empty_relative_store_is_three_meta_pages_of_the_design_layout()
    {
        var (medium, _, _) = StoreKit.NewStore();
        Assert.Equal(MetaPage.MetaPages * (long)PageHeader.PageSize, medium.Length);
        for (uint p = 0; p < 3; p++)
        {
            var page = medium.Snapshot(p);
            AssertHeader(page, PageKind.Meta, self: p, generation: 0);
            AssertMetaFixedPart(page, organization: 'R', recordType: 'F', min: 80, max: 80, keys: 0);
            Assert.True(page.AsSpan(MetaPage.KeysOffset).IndexOfAnyExcept((byte)0) < 0, "no key descriptor and nothing after it");
        }
    }

    [Fact]
    public void An_empty_two_key_indexed_store_records_both_descriptors_and_the_alternate_root()
    {
        var medium = new MemoryPageMedium();
        Assert.Equal(StoreStatus.Ok, PagedFile.Create(medium.Open("A"), StoreKit.TwoKeyIndexed, coherent: true, out _));
        for (uint p = 0; p < 3; p++)
        {
            var page = medium.Snapshot(p);
            AssertHeader(page, PageKind.Meta, self: p, generation: 0);
            AssertMetaFixedPart(page, organization: 'I', recordType: 'V', min: 10, max: 120, keys: 2);
            // From offset 169: the prime key's descriptor (the whole-store header's encoding), then the alternate key's
            // descriptor followed by the POINTER to its tree (empty: 12 zero bytes).
            var expected = new MemoryStream();
            RecordFraming.WriteKeyDescriptor(expected, StoreKit.TwoKeyIndexed.Keys[0]);
            RecordFraming.WriteKeyDescriptor(expected, StoreKit.TwoKeyIndexed.Keys[1]);
            expected.Write(new byte[PagePointer.Bytes]);
            var keys = expected.ToArray();
            // The descriptor bytes themselves, read off the encoding: segment count 1, offset 0, length 8, no duplicates,
            // no SUPPRESS value (biased count 0), an empty collation fingerprint.
            Assert.Equal(new byte[] { 1, 0, 0, 0, 0, 0, 0, 0, 8, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, keys[..18]);
            Assert.Equal(keys, page.AsSpan(MetaPage.KeysOffset, keys.Length).ToArray());
            Assert.True(page.AsSpan(MetaPage.KeysOffset + keys.Length).IndexOfAnyExcept((byte)0) < 0);
            var decoded = MetaPage.Decode(page, p)!;
            Assert.Equal(StoreKit.TwoKeyIndexed.Keys.Count, decoded.Attributes.Keys.Count);
            Assert.False(decoded.Attributes.Conflicts(StoreKit.TwoKeyIndexed));
            Assert.Equal([PagePointer.None], decoded.AlternateRoots);
        }
    }

    /// <summary>The checksum covers every byte but its own four, so ONE flipped bit anywhere else in a page of any kind
    /// fails it — the kind, flags and entry-count bytes included (the correction to 2.2.1.4's "bytes 8..4095").</summary>
    [Theory]
    [InlineData(nameof(PageKind.Meta))]
    [InlineData(nameof(PageKind.Internal))]
    [InlineData(nameof(PageKind.Leaf))]
    [InlineData(nameof(PageKind.Overflow))]
    [InlineData(nameof(PageKind.Free))]
    public void The_checksum_detects_a_single_flipped_bit_anywhere_in_every_page_kind(string kindName)
    {
        var page = new byte[PageHeader.PageSize];
        var kind = Enum.Parse<PageKind>(kindName);
        new PageHeader(kind, 1, 7, 42, 9, 11).Encode(page);
        for (int i = PageHeader.Bytes; i < page.Length; i++) page[i] = (byte)(i * 31);
        PageHeader.Seal(page);
        Assert.Equal(PageCheck.Whole, PageHeader.Check(page, 9, 42));
        for (int bit = 0; bit < page.Length * 8; bit++)
        {
            if (bit / 8 is >= 4 and < 8) continue;   // the checksum field itself
            page[bit / 8] ^= (byte)(1 << (bit % 8));
            Assert.False(PageHeader.ChecksumHolds(page), $"bit {bit} flipped and the checksum still held");
            page[bit / 8] ^= (byte)(1 << (bit % 8));
        }
    }

    [Fact]
    public void Self_catches_a_page_read_from_the_wrong_offset_and_the_stamp_catches_an_older_version()
    {
        var page = new byte[PageHeader.PageSize];
        new PageHeader(PageKind.Leaf, 0, 0, Generation: 5, Self: 12, Next: 0).Encode(page);
        PageHeader.Seal(page);
        Assert.Equal(PageCheck.Whole, PageHeader.Check(page, 12, 5));
        Assert.Equal(PageCheck.WrongSelf, PageHeader.Check(page, 13, 5));
        Assert.Equal(PageCheck.StaleGeneration, PageHeader.Check(page, 12, 6));
        Assert.Equal(PageCheck.ChecksumFails, PageHeader.Check(new byte[PageHeader.PageSize], 12, 5));
    }

    /// <summary>A page body larger than the page is the caller's defect and is refused loudly — never cut to fit, which
    /// would write a page whose checksum holds over a truncated tree node.</summary>
    [Fact]
    public void A_statement_refuses_a_page_body_larger_than_the_page()
    {
        var (_, _, store) = StoreKit.NewStore();
        Assert.Equal(StoreStatus.Ok, store.Begin(1, out var st));
        Assert.Throws<ArgumentException>(() => st!.Write(PageKind.Leaf, new byte[PageHeader.BodyBytes + 1]));
        Assert.False(st!.Write(PageKind.Leaf, new byte[PageHeader.BodyBytes]).IsNone);
        st.Abandon();
    }

    [Fact]
    public void The_meta_page_refuses_a_key_declaration_past_its_capacity()
    {
        var wide = new FixedFileAttributes.KeyDescriptor([new KeySegment(0, 8)], false, new string('x', 1000), "");
        Assert.True(MetaPage.Fits(new FixedFileAttributes(FixedFileAttributes.Indexed, false, 8, 8, [wide])));
        Assert.False(MetaPage.Fits(new FixedFileAttributes(FixedFileAttributes.Indexed, false, 8, 8, [wide, wide])));
    }

    /// <summary><c>MetaPage.Describes</c>, the §14.9.27.4 GR10 read: Empty, Described, Foreign (a whole-store file of format
    /// 5, whose magic sits at offset 0) and Damaged (this format's magic, neither committed slot valid: R6's '30').</summary>
    [Fact]
    public void Describes_tells_empty_described_foreign_and_damaged_files_apart()
    {
        string path = Path.Combine(Path.GetTempPath(), $"pb3331-describes-{Guid.NewGuid():N}.dat");
        try
        {
            File.WriteAllBytes(path, []);
            Assert.Equal(StoreFormat.Empty, MetaPage.Describes(path, out _));

            var whole = new byte[64];
            "CBNFSTR"u8.CopyTo(whole);
            whole[7] = 5;
            File.WriteAllBytes(path, whole);
            Assert.Equal(StoreFormat.Foreign, MetaPage.Describes(path, out _));

            using (var handle = File.OpenHandle(path, FileMode.Create, FileAccess.ReadWrite))
                Assert.Equal(StoreStatus.Ok, PagedFile.Create(new HandlePageMedium(handle), StoreKit.TwoKeyIndexed, true, out _));
            Assert.Equal(StoreFormat.Described, MetaPage.Describes(path, out var recorded));
            Assert.False(recorded!.Conflicts(StoreKit.TwoKeyIndexed));

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
            {
                fs.Position = 100;
                fs.WriteByte(0xEE);   // page 0 torn
                fs.Position = PageHeader.PageSize + 100;
                fs.WriteByte(0xEE);   // page 1 torn
            }
            Assert.Equal(StoreFormat.Damaged, MetaPage.Describes(path, out _));
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>The production medium: a store created, written, closed and reopened over a real file through
    /// <see cref="HandlePageMedium"/>, then both committed slots torn — the OPEN answers '30' (2.2.1.6 R6 step 1).</summary>
    [Fact]
    public void A_store_round_trips_over_a_real_file_and_both_slots_torn_answers_30()
    {
        string path = Path.Combine(Path.GetTempPath(), $"pb3331-handle-{Guid.NewGuid():N}.dat");
        try
        {
            using (SafeFileHandle handle = File.OpenHandle(path, FileMode.Create, FileAccess.ReadWrite))
            {
                Assert.Equal(StoreStatus.Ok, PagedFile.Create(new HandlePageMedium(handle), StoreKit.Relative, true, out var store));
                Assert.Equal(StoreStatus.Ok, StoreKit.Put(store!, 1, "one"));
                Assert.Equal(StoreStatus.Ok, StoreKit.Put(store!, 2, "two", blobPages: 3));
                Assert.Equal(StoreStatus.Ok, store!.Close());
            }
            using (SafeFileHandle handle = File.OpenHandle(path, FileMode.Open, FileAccess.ReadWrite))
            {
                Assert.Equal(StoreStatus.Ok, PagedFile.Open(new HandlePageMedium(handle), writable: true, coherent: true, out var store));
                Assert.Equal("1=one;2=two;", StoreKit.Show(store!));
                Assert.Equal(StoreStatus.Ok, store!.Close());
            }
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
            {
                fs.Position = 200;
                fs.WriteByte(0x5A);
                fs.Position = PageHeader.PageSize + 200;
                fs.WriteByte(0x5A);
            }
            long length = new FileInfo(path).Length;
            using (SafeFileHandle handle = File.OpenHandle(path, FileMode.Open, FileAccess.ReadWrite))
                Assert.Equal(StoreStatus.PermanentError, PagedFile.Open(new HandlePageMedium(handle), true, true, out _));
            Assert.Equal(length, new FileInfo(path).Length);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static void AssertHeader(byte[] page, PageKind kind, uint self, ulong generation)
    {
        Assert.Equal((byte)kind, page[0]);                                                  // 0  kind
        Assert.Equal(generation, BinaryPrimitives.ReadUInt64LittleEndian(page.AsSpan(8)));   // 8  generation
        Assert.Equal(self, BinaryPrimitives.ReadUInt32LittleEndian(page.AsSpan(16)));         // 16 self
        Assert.Equal(0u, BinaryPrimitives.ReadUInt32LittleEndian(page.AsSpan(20)));           // 20 next (M: 0)
        Assert.True(page.AsSpan(24, 8).IndexOfAnyExcept((byte)0) < 0);                        // 24 reserved
        Assert.Equal(PageHeader.Checksum(page), BinaryPrimitives.ReadUInt32LittleEndian(page.AsSpan(4)));   // 4 checksum
    }

    private static void AssertMetaFixedPart(byte[] page, char organization, char recordType, int min, int max, int keys)
    {
        Assert.Equal("CBNFSTR", Encoding.ASCII.GetString(page, 32, 7));                      // 32 magic
        Assert.Equal(MetaPage.FormatVersion, page[39]);                                      //    + version
        Assert.Equal((byte)organization, page[40]);
        Assert.Equal((byte)recordType, page[41]);
        Assert.Equal(min, BinaryPrimitives.ReadInt32LittleEndian(page.AsSpan(42)));
        Assert.Equal(max, BinaryPrimitives.ReadInt32LittleEndian(page.AsSpan(46)));
        Assert.Equal(keys, BinaryPrimitives.ReadUInt16LittleEndian(page.AsSpan(50)));
        Assert.Equal(0ul, BinaryPrimitives.ReadUInt64LittleEndian(page.AsSpan(52)));          // generation
        Assert.Equal(0ul, BinaryPrimitives.ReadUInt64LittleEndian(page.AsSpan(60)));          // release mint
        Assert.Equal(3ul, BinaryPrimitives.ReadUInt64LittleEndian(page.AsSpan(68)));          // page count
        Assert.Equal(0ul, BinaryPrimitives.ReadUInt64LittleEndian(page.AsSpan(76)));          // transaction mint
        Assert.True(page.AsSpan(84, 4 * PagePointer.Bytes).IndexOfAnyExcept((byte)0) < 0);    // the four roots
        Assert.Equal(0ul, BinaryPrimitives.ReadUInt64LittleEndian(page.AsSpan(132)));         // committed generation
        Assert.Equal(0ul, BinaryPrimitives.ReadUInt64LittleEndian(page.AsSpan(140)));         // stamp mint
        Assert.Equal(0, page[148]);                                                           // flags
        Assert.Equal(0ul, BinaryPrimitives.ReadUInt64LittleEndian(page.AsSpan(149)));         // committing
        Assert.True(page.AsSpan(157, PagePointer.Bytes).IndexOfAnyExcept((byte)0) < 0);       // holdings root
    }

    /// <summary>The PRODUCTION medium's length is the file's length in BYTES (finding R2-1): over a 5 000-byte file — a
    /// format-5 whole store, not a whole number of pages — <see cref="HandlePageMedium.Length"/> is 5 000, a read of the
    /// partly held page serves its bytes then zeros, and <see cref="HandlePageMedium.SetLength"/> after a write past the
    /// end gives the file back exactly its 5 000 bytes — what a refused creation's undo relies on (§14.9.27.4 GR25,
    /// <i>"the file is not affected"</i>). A creation over the same file then leaves the three-page empty store.</summary>
    [Fact]
    public void The_host_medium_keeps_a_previous_files_length_to_the_byte()
    {
        string path = Path.Combine(Path.GetTempPath(), $"pb3331-length-{Guid.NewGuid():N}.dat");
        try
        {
            var previous = Enumerable.Range(0, 5_000).Select(i => (byte)(i * 7 + 3)).ToArray();
            "CBNFSTR"u8.CopyTo(previous);
            previous[7] = 5;
            File.WriteAllBytes(path, previous);
            using (var handle = File.OpenHandle(path, FileMode.Open, FileAccess.ReadWrite))
            {
                var medium = new HandlePageMedium(handle);
                Assert.Equal(5_000, medium.Length);
                var page = new byte[PageHeader.PageSize];
                medium.Read(1, page);
                Assert.True(previous.AsSpan(PageHeader.PageSize).SequenceEqual(page.AsSpan(0, 5_000 - PageHeader.PageSize)));
                Assert.True(page.AsSpan(5_000 - PageHeader.PageSize).IndexOfAnyExcept((byte)0) < 0);
                medium.Write(2, new byte[PageHeader.PageSize]);
                Assert.Equal(3L * PageHeader.PageSize, medium.Length);
                medium.SetLength(5_000);
            }
            Assert.Equal(previous, File.ReadAllBytes(path));
            using (var handle = File.OpenHandle(path, FileMode.Open, FileAccess.ReadWrite))
                Assert.Equal(StoreStatus.Ok, PagedFile.Create(new HandlePageMedium(handle), StoreKit.Relative, true, out _));
            Assert.Equal(3L * PageHeader.PageSize, new FileInfo(path).Length);
            Assert.Equal(StoreFormat.Described, MetaPage.Describes(path, out _));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
