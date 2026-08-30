// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>The image predicate's VALUE pin (kb/Work PB164): <see cref="PicInfo.HasImageByteForm"/> answers
/// true for exactly the ByteForm-pinned set and false for ByteForm-less usages, and the binary forms' image
/// width is StorageWidth (the V59 ONE-WIDTH invariant). ⚠ What this CANNOT catch: a consumer hand-rolling
/// its own usage union instead of reading the predicate (the drift that excluded COMP-5 for a year, and the
/// carrier-name list the PB164 review fleet caught surviving in PhysicalModel) — that class is caught only
/// by consumers DERIVING from the predicate, which the PB164 conversions made structural, and by the
/// end-to-end goldens (<c>pb164_comp5_group_image</c>) that run the codec over the widened shapes.</summary>
public class ImagePredicateDriftTests
{
    private static PicInfo Pic(Usage usage, int digits = 4) =>
        new(PicCategory.Numeric, usage, Length: digits, Digits: digits, Scale: 0, Signed: false);

    [Theory]
    [InlineData(Usage.Display)]
    [InlineData(Usage.Binary)]
    [InlineData(Usage.Packed)]
    [InlineData(Usage.Comp5)]
    [InlineData(Usage.BinaryChar)]
    [InlineData(Usage.BinaryShort)]
    [InlineData(Usage.BinaryLong)]
    [InlineData(Usage.BinaryDouble)]
    [InlineData(Usage.Float)]
    [InlineData(Usage.Double)]
    [InlineData(Usage.FloatShort)]
    [InlineData(Usage.FloatLong)]
    [InlineData(Usage.FloatExtended)]
    [InlineData(Usage.FloatBinary32)]
    [InlineData(Usage.FloatBinary64)]
    public void EveryPinnedByteForm_IsImageCapable(Usage usage)
    {
        var pic = Pic(usage);
        Assert.NotEqual(NumericByteForm.None, pic.ByteForm);
        Assert.True(pic.HasImageByteForm,
            $"{usage} has ByteForm {pic.ByteForm} — the image predicate must admit it (kb/Work PB164)");
    }

    [Theory]
    [InlineData(Usage.Index)]
    public void NoByteForm_IsNotImageCapable(Usage usage)
    {
        var pic = Pic(usage);
        Assert.Equal(NumericByteForm.None, pic.ByteForm);
        Assert.False(pic.HasImageByteForm);
    }

    [Theory]
    // The binary forms occupy StorageWidth bytes in the image — never a byte per decimal digit (V59).
    [InlineData(Usage.Comp5, 4, 2)]
    [InlineData(Usage.Comp5, 9, 4)]
    [InlineData(Usage.Comp5, 18, 8)]
    [InlineData(Usage.BinaryChar, 3, 1)]
    [InlineData(Usage.BinaryShort, 5, 2)]
    [InlineData(Usage.BinaryLong, 10, 4)]
    [InlineData(Usage.BinaryDouble, 19, 8)]
    public void BinaryImageWidth_IsStorageWidth(Usage usage, int digits, int expectedBytes)
    {
        var pic = Pic(usage, digits);
        Assert.Equal(expectedBytes, pic.StorageWidth);
        Assert.Equal(NumericByteForm.Binary, pic.ByteForm);
    }

    [Theory]
    // The IEEE interchange widths (kb/Work PB164 wave 2 — §13.18.60.4 GR13–GR15, big-endian).
    [InlineData(Usage.Float, NumericByteForm.Ieee32, 4)]
    [InlineData(Usage.FloatShort, NumericByteForm.Ieee32, 4)]
    [InlineData(Usage.FloatBinary32, NumericByteForm.Ieee32, 4)]
    [InlineData(Usage.Double, NumericByteForm.Ieee64, 8)]
    [InlineData(Usage.FloatLong, NumericByteForm.Ieee64, 8)]
    [InlineData(Usage.FloatExtended, NumericByteForm.Ieee64, 8)]
    [InlineData(Usage.FloatBinary64, NumericByteForm.Ieee64, 8)]
    public void FloatImageWidth_IsIeeeInterchangeWidth(Usage usage, NumericByteForm expectedForm, int expectedBytes)
    {
        var pic = Pic(usage, digits: 0);
        Assert.Equal(expectedForm, pic.ByteForm);
        Assert.Equal(expectedBytes, pic.StorageWidth);
        Assert.True(pic.HasImageByteForm);
    }
}
