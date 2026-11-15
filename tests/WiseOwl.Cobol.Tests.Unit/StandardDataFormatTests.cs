// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A GROUP'S CHARACTERS IN STANDARD DATA FORMAT (§1985:IV.4.3.4; kb/Work PB5378): <see cref="CobolStandardData.Image"/> composes
/// them from the storage image and the layout the compiler states. A binary or packed decimal item is its digits, every other
/// part its storage characters, a table repeats its element, and an OCCURS DEPENDING table past the storage the image holds adds
/// nothing. The expected strings are the standard's: the digits of the value, never the bytes.
/// </summary>
public sealed class StandardDataFormatTests
{
    private static NumProfile Profile(Usage usage, int digits, bool signed = false)
    {
        var pic = new PicInfo(PicCategory.Numeric, usage, Length: digits, Digits: digits, Scale: 0, Signed: signed);
        return new NumProfile
        {
            Digits = digits,
            FractionDigits = 0,
            Signed = signed,
            SignKind = NumericSign.BinaryMinus,
            Truncation = pic.Truncation,
            ByteForm = pic.ByteForm,
            StorageLength = pic.StorageWidth,
        };
    }

    [Fact]
    public void BinaryAndPackedItemsAreTheirDigits_OtherPartsTheirStorage()
    {
        var binary = Profile(Usage.Binary, 4);
        var packed = Profile(Usage.Packed, 3);
        string storage = "AB" + CobolNum.FormatImage(12, binary) + CobolNum.FormatImage(123, packed);
        Assert.NotEqual(9, storage.Length);   // the storage is not the standard characters: that is the point
        var parts = new[]
        {
            new StandardDataPart(0, 2),
            new StandardDataPart(2, binary.StorageLength, binary),
            new StandardDataPart(2 + binary.StorageLength, packed.StorageLength, packed),
        };
        Assert.Equal("AB0012123", CobolStandardData.Image(storage, parts));
    }

    [Fact]
    public void ATableRepeatsItsElement_AndAnImageEndingInsideTheTableEndsThere()
    {
        var binary = Profile(Usage.Binary, 2);
        int width = binary.StorageLength;
        string two = CobolNum.FormatImage(5, binary) + CobolNum.FormatImage(7, binary);
        var element = new[] { new StandardDataPart(0, width, binary) };
        var table = new[] { new StandardDataPart(0, width, null, 3, element) };
        Assert.Equal("0507", CobolStandardData.Image(two, table));
        Assert.Equal("0507", CobolStandardData.Image(two + CobolNum.FormatImage(9, binary), new[] { new StandardDataPart(0, width, null, 2, element) }));
    }

    [Fact]
    public void ASignedItemIsItsDigitsWithTheSignFusedOntoTheLast_NotACharacterMore()
    {
        // §1985:VI.5.9.4 GR8d: the S "is not counted in determining the size", so -12 in S9(4) is four characters, in a group or alone.
        var binary = Profile(Usage.Binary, 4, signed: true);
        string negative = CobolStandardData.Characters("-0012", binary);
        string positive = CobolStandardData.Characters("0012", binary);
        Assert.Equal(4, negative.Length);
        Assert.Equal(4, positive.Length);
        Assert.Equal("001", negative[..3]);
        Assert.NotEqual(negative, positive);   // the sign is carried, by the last character
        string storage = "AB" + CobolNum.FormatImage(-12, binary);
        Assert.Equal("AB" + negative, CobolStandardData.Image(storage, new[] { new StandardDataPart(0, 2), new StandardDataPart(2, binary.StorageLength, binary) }));
    }
}
