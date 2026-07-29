// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE ONE-WIDTH INVARIANT (V59). An item that participates in the record/group character image must occupy
/// THE SAME number of positions there as it occupies in storage — <see cref="DataItem.ImageWidth"/> and
/// <see cref="DataItem.ByteWidth"/> are two views of one fact, never two answers.
/// <para>
/// This is a CLASS-retiring test, not an instance test. The compiler shipped two answers for two whole phases:
/// <c>PicInfo.StorageWidth</c> pinned BINARY at 1-2-4-8 and PACKED at <c>Digits/2+1</c> BCD, <c>ByteWidth</c>
/// documented them and <c>FUNCTION BYTE-LENGTH</c> reported them, while the image used <c>Pic.Digits</c>. For
/// <c>05 G-COMP PIC 9(4) COMP. 05 G-PACK PIC 9(4) COMP-3.</c> the compiler answered BYTE-LENGTH(G) = 5 and
/// LENGTH(G) = 8 and accepted <c>REDEFINES G PIC X(8)</c>. ISO §15.14.4 GR1 returns the length in BYTES and
/// §15.50.4 GR3 in ALPHANUMERIC CHARACTER POSITIONS; in a single-byte-character model those cannot disagree, and
/// a CONFORMING program observes the disagreement with no file, no REDEFINES and no byte pun at all. Nothing in
/// the battery said so, because nothing compared the two.
/// </para>
/// <para>
/// Scope of the invariant, and why each exclusion is principled rather than convenient:
/// <list type="bullet">
/// <item>NATIONAL is excluded because it is deliberately 2 bytes per character POSITION (D-N1/D-N3) — the two
/// views legitimately differ by exactly that factor, which this test asserts rather than skips.</item>
/// <item>BOOLEAN is excluded because its unit is a boolean position, not a character position (§15.50.4 GR1).</item>
/// <item>Everything NOT <see cref="DataItem.IsImageCapable"/> is excluded because it never reaches an image at
/// all — float / COMP-5 / INDEX are the loud Tier-C island. An item outside the image cannot contradict it.</item>
/// </list>
/// </para>
/// </summary>
public sealed class ImageWidthIsStorageWidthTests
{
    private static DataItem Leaf(PicInfo pic) => new() { Level = 5, CobolName = "X", CsName = "X", Pic = pic };

    private static PicInfo Numeric(Usage usage, int digits, int scale = 0, bool signed = false) =>
        new(PicCategory.Numeric, usage, Length: digits, Digits: digits, Scale: scale, Signed: signed);

    /// <summary>Every fixed-point numeric usage that reaches an image, across the digit counts where the pinned
    /// width table steps (1-2-4-8 for BINARY; every packed byte boundary), signed and unsigned.</summary>
    public static TheoryData<Usage, int, bool> ImageCapableNumerics()
    {
        var data = new TheoryData<Usage, int, bool>();
        foreach (var usage in new[] { Usage.Display, Usage.Binary, Usage.Packed })
            foreach (int digits in new[] { 1, 2, 3, 4, 5, 6, 8, 9, 10, 15, 18 })
                foreach (bool signed in new[] { false, true })
                    data.Add(usage, digits, signed);
        return data;
    }

    [Theory]
    [MemberData(nameof(ImageCapableNumerics))]
    public void ImageWidth_EqualsByteWidth_ForEveryImageCapableNumeric(Usage usage, int digits, bool signed)
    {
        var item = Leaf(Numeric(usage, digits, signed: signed));
        if (!item.IsImageCapable) return;   // outside the image entirely — cannot contradict it
        Assert.Equal(item.ByteWidth, item.ImageWidth);
    }

    /// <summary>The pinned widths themselves, stated as a table so a silent change to the representation is a
    /// RED TEST rather than a surprise on disk. These ARE the documented on-disk form (ISO §13.18.60.4 GR4 leaves
    /// BINARY's representation and GR11 PACKED's to the implementor; §4.2.16 obliges us to document the choice).
    /// BINARY = 1-2-4-8 by digit count; PACKED = <c>Digits/2 + 1</c> BCD bytes (sign nibble included).</summary>
    [Theory]
    [InlineData(Usage.Binary, 1, 1)]
    [InlineData(Usage.Binary, 2, 1)]
    [InlineData(Usage.Binary, 3, 2)]
    [InlineData(Usage.Binary, 4, 2)]
    [InlineData(Usage.Binary, 5, 4)]
    [InlineData(Usage.Binary, 9, 4)]
    [InlineData(Usage.Binary, 10, 8)]
    [InlineData(Usage.Binary, 18, 8)]
    // The 16-byte tier: §13.18.60.4 GR4 requires storage SUFFICIENT for the range the picture implies, and a
    // signed 19-digit picture (10^19−1) does not fit 2^63−1. Legal at COBOL-2002+ and stored as Int128.
    [InlineData(Usage.Binary, 19, 16)]
    [InlineData(Usage.Binary, 31, 16)]
    [InlineData(Usage.Packed, 1, 1)]
    [InlineData(Usage.Packed, 2, 2)]
    [InlineData(Usage.Packed, 3, 2)]
    [InlineData(Usage.Packed, 4, 3)]
    [InlineData(Usage.Packed, 9, 5)]
    [InlineData(Usage.Packed, 18, 10)]
    public void PinnedStorageWidths_AreTheDocumentedOnDiskForm(Usage usage, int digits, int expectedBytes)
    {
        Assert.Equal(expectedBytes, Numeric(usage, digits).StorageWidth);
        Assert.Equal(expectedBytes, Leaf(Numeric(usage, digits)).ByteWidth);
    }

    /// <summary>A DISPLAY leaf is one byte per character position — the two views agree trivially, which is what
    /// makes the BINARY/PACKED disagreement a defect rather than a modelling choice.</summary>
    [Fact]
    public void DisplayLeaf_ImageAndByteWidthsAgree()
    {
        var item = Leaf(Numeric(Usage.Display, 4));
        Assert.Equal(4, item.ImageWidth);
        Assert.Equal(4, item.ByteWidth);
    }

    /// <summary>NATIONAL is the ONE sanctioned divergence and it is exactly 2× (D-N1/D-N3) — asserted, so a
    /// future change cannot quietly widen the exemption.</summary>
    [Fact]
    public void NationalLeaf_IsExactlyTwoBytesPerPosition()
    {
        var item = Leaf(new PicInfo(PicCategory.National, Usage.National, Length: 4, Digits: 0, Scale: 0, Signed: false));
        Assert.Equal(4, item.ImageWidth);
        Assert.Equal(8, item.ByteWidth);
    }

    /// <summary>A group's two widths compose from its leaves', so the invariant holds transitively — this is the
    /// shape the original defect actually presented as (BYTE-LENGTH(G) = 5 vs LENGTH(G) = 8).</summary>
    [Fact]
    public void MixedUsageGroup_ImageWidthEqualsByteWidth()
    {
        var group = new DataItem { Level = 1, CobolName = "G", CsName = "G" };
        group.Children.Add(Leaf(Numeric(Usage.Binary, 4)));    // 2 bytes
        group.Children.Add(Leaf(Numeric(Usage.Packed, 4)));    // 3 bytes
        group.Children.Add(Leaf(Numeric(Usage.Display, 4)));   // 4 bytes
        Assert.Equal(9, group.ByteWidth);
        Assert.Equal(group.ByteWidth, group.ImageWidth);
    }
}
