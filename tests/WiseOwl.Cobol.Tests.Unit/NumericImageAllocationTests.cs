// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE NUMERIC IMAGE CODEC WRITES ONE STRING AND READS NONE (kb/Work PB2656, the R2 performance review of the
/// MOVEHOT hot path). Every <see cref="CobolNum.FormatImage(Int128, in NumProfile)"/> arm, the DISPLAY formatters
/// and <see cref="CobolNum.FormatStandardData"/> allocate exactly the image string; <see cref="CobolNum.ParseDisplay"/>
/// and <see cref="CobolNum.ImageIsNegative"/> allocate nothing. Before the change a signed zoned image under the
/// default IBM convention cost five allocations (ToString, PadLeft, two substrings, the concatenation), and every
/// zoned decode copied the image to a char array. The guard is structural (bytes counted on this thread), never a
/// wall-clock limit.
/// <para>The image vectors are computed from the pinned representation: the over-punch characters are the IBM
/// table <c>{ABCDEFGHI</c> / <c>}JKLMNOPQR</c> and the ASCII table <c>0123456789</c> / <c>pqrstuvwxy</c>
/// (<see cref="ZonedSign"/>; §2023:13.18.52.4 GR4 leaves the representation of the operational sign to the
/// implementor, GR5b the valid signs), a separate sign is <c>+</c>/<c>-</c>.</para>
/// </summary>
public sealed class NumericImageAllocationTests
{
    private static NumProfile Zoned(int digits, NumericSign kind, SignEncoding encoding = SignEncoding.Ibm, bool signed = true) => new()
    {
        Digits = digits,
        FractionDigits = 0,
        Signed = signed,
        SignKind = kind,
        SignEncoding = encoding,
        Truncation = NumericTruncation.DigitCount,
        ByteForm = NumericByteForm.Zoned,
    };

    private static NumProfile Bytes(int digits, NumericByteForm form, int width) => new()
    {
        Digits = digits,
        FractionDigits = 0,
        Signed = true,
        SignKind = NumericSign.BinaryMinus,
        Truncation = NumericTruncation.DigitCount,
        ByteForm = form,
        StorageLength = width,
    };

    public static TheoryData<int, NumericSign, SignEncoding, long, string> ZonedImages => new()
    {
        { 3, NumericSign.TrailingOverpunch, SignEncoding.Ibm, -12, "01K" },
        { 3, NumericSign.TrailingOverpunch, SignEncoding.Ibm, 12, "01B" },
        { 3, NumericSign.TrailingOverpunch, SignEncoding.Ibm, 0, "00{" },
        { 3, NumericSign.LeadingOverpunch, SignEncoding.Ibm, -112, "J12" },
        { 3, NumericSign.TrailingOverpunch, SignEncoding.Ascii, -12, "01r" },
        { 3, NumericSign.TrailingOverpunch, SignEncoding.Ascii, 12, "012" },
        { 3, NumericSign.LeadingSeparate, SignEncoding.Ibm, -12, "-012" },
        { 3, NumericSign.TrailingSeparate, SignEncoding.Ibm, 12, "012+" },
        { 3, NumericSign.BinaryMinus, SignEncoding.Ibm, -12, "-012" },
        { 3, NumericSign.BinaryMinus, SignEncoding.Ibm, 12, "012" },
        { 18, NumericSign.TrailingOverpunch, SignEncoding.Ibm, -123456789012345678, "12345678901234567Q" },
        // The picture holds the low digits only (§2023:14.6.8.2 GR4: "zero fill or truncation on either end").
        { 2, NumericSign.TrailingOverpunch, SignEncoding.Ibm, -1234, "3M" },
    };

    [Theory]
    [MemberData(nameof(ZonedImages))]
    public void A_zoned_image_is_one_string_and_its_decode_allocates_nothing(
        int digits, NumericSign kind, SignEncoding encoding, long value, string expected)
    {
        var item = Zoned(digits, kind, encoding);
        Assert.Equal(expected, CobolNum.FormatDisplay(value, item));
        Assert.Equal(StringCost(expected.Length), Allocated(() => CobolNum.FormatDisplay(value, item)));

        Int128 truncated = value % (Int128)Math.Pow(10, digits);
        Assert.Equal(truncated, CobolNum.ParseDisplay(expected, item));
        Assert.Equal(value < 0, CobolNum.ImageIsNegative(expected, item));
        Assert.Equal(0, Allocated(() => CobolNum.ParseDisplay(expected, item)));
        Assert.Equal(0, Allocated(() => CobolNum.ImageIsNegative(expected, item)));
    }

    [Fact]
    public void A_wide_unsigned_image_is_one_string()
    {
        // 340282366920938463463374607431768211455 (39 digits): a 38-digit picture holds its low 38, past the 64-bit range
        var max = UInt128.MaxValue;
        Assert.Equal("40282366920938463463374607431768211455", CobolNum.FormatUnsignedDisplayU(max, 38));
        Assert.Equal("1768211455", CobolNum.FormatUnsignedDisplayU(max, 10));
        Assert.Equal(StringCost(38), Allocated(() => CobolNum.FormatUnsignedDisplayU(max, 38)));
        Assert.Equal(StringCost(31), Allocated(() => CobolNum.FormatUnsignedDisplay(Int128.MinValue, 31)));
    }

    [Fact]
    public void A_standard_data_format_image_fuses_the_sign_and_is_one_string()
    {
        var item = Zoned(4, NumericSign.LeadingSeparate);   // SEND counts the S on the last digit whatever SIGN says
        Assert.Equal("012M", CobolNum.FormatStandardData(-124, item));
        Assert.Equal(StringCost(4), Allocated(() => CobolNum.FormatStandardData(-124, item)));
    }

    [Fact]
    public void A_packed_or_binary_image_is_one_string()
    {
        var packed = Bytes(5, NumericByteForm.Packed, 3);       // S9(5) COMP-3: 12 34 5D
        Assert.Equal(new string([StorageByte.ToChar(0x12), StorageByte.ToChar(0x34), StorageByte.ToChar(0x5D)]),
            CobolNum.FormatImage(-12345, packed));
        Assert.Equal(StringCost(3), Allocated(() => CobolNum.FormatImage(-12345, packed)));
        var binary = Bytes(9, NumericByteForm.Binary, 4);       // S9(9) COMP: two's complement, big-endian
        Assert.Equal(-2, (int)CobolNum.ParseImage(CobolNum.FormatImage(-2, binary), binary));
        Assert.Equal(StringCost(4), Allocated(() => CobolNum.FormatImage(-2, binary)));
    }

    /// <summary>The bytes one string of <paramref name="length"/> characters costs on this runtime, measured.</summary>
    private static long StringCost(int length) => Allocated(() => new string('0', length));

    private static long Allocated<T>(Func<T> act)
    {
        act();   // JIT and first-call initialization are not the call's cost
        long before = GC.GetAllocatedBytesForCurrentThread();
        act();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }
}
