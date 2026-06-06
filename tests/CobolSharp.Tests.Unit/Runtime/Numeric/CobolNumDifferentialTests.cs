// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Numerics;
using System.Text;
using CobolSharp.Runtime;
using CobolSharp.Runtime.Numeric;
using Xunit;

namespace CobolSharp.Tests.Unit.Runtime.Numeric;

/// <summary>
/// The Stage-1 differential oracle for <see cref="CobolNum"/> (docs/DATA_MODEL_ARCHITECTURE.md §10, §13).
///
/// <para>Within the legacy path's <em>faithful window</em> — integer magnitudes up to ~18 significant digits
/// (the <see cref="long"/> boundary; <see cref="long.MaxValue"/> is 19 digits / 9.22e18, and the legacy
/// decode accumulators / <c>checked((long))</c> overflow casts bite above it) — <see cref="CobolNum.TryStore"/>
/// must be bit-for-bit identical to the legacy byte pipeline (<c>PicRuntime.StoreArithmeticResult</c>, reached
/// here by adding the value to a zeroed receiver via <see cref="PicRuntime.AddNumericLiteral"/>): same SIZE
/// ERROR decision, same stored value. The grid below sweeps (field × value × rounding mode) — across DISPLAY /
/// COMP / COMP-3 / COMP-5, signed and unsigned, leading-P and trailing-P scaling, and mid-range (10–18-digit)
/// magnitudes — staying inside that window, and reports every divergence.</para>
///
/// <para>Beyond the faithful window — 19–31-digit values, the 8-byte <em>unsigned</em> COMP-5 range
/// <c>(long.Max, ulong.Max]</c>, the COMP-5 signed negative extreme, and PROHIBITED inexactness — the legacy
/// decimal/long path is common-mode blind (it overflows, decodes to zero, or throws), so those branches are
/// validated against an independent <see cref="BigInteger"/> / two's-complement reference instead.</para>
/// </summary>
public sealed class CobolNumDifferentialTests
{
    private sealed record Field(string Label, string PicBody, UsageKind Usage, bool Signed)
    {
        public PicDescriptor Pic => PicDescriptorFactory.FromPicBody(
            PicBody, Usage, Signed,
            Signed && Usage == UsageKind.Display ? SignStorageKind.TrailingOverpunch : SignStorageKind.None);
    }

    private static readonly Field[] Fields =
    [
        // DISPLAY
        new("DISP 9(4)",     "9(4)",     UsageKind.Display, false),
        new("DISP 9(6)",     "9(6)",     UsageKind.Display, false),
        new("DISP 9(3)V99",  "9(3)V99",  UsageKind.Display, false),
        new("DISP 99V9",     "99V9",     UsageKind.Display, false),
        new("DISP S9(4)",    "S9(4)",    UsageKind.Display, true),
        new("DISP S9(3)V99", "S9(3)V99", UsageKind.Display, true),
        new("DISP S99V9",    "S99V9",    UsageKind.Display, true),
        new("DISP 9(15)",    "9(15)",    UsageKind.Display, false),   // mid-range (still in long window)
        new("DISP S9(13)V99","S9(13)V99",UsageKind.Display, true),    // mid-range, fractional
        new("DISP 9(3)P",    "9(3)P",    UsageKind.Display, false),   // trailing-P
        new("DISP S99P",     "S99P",     UsageKind.Display, true),    // trailing-P, signed
        new("DISP PP9",      "PP9",      UsageKind.Display, false),   // leading-P
        // COMP / BINARY (digit-count capacity)
        new("COMP 9(4)",     "9(4)",     UsageKind.Comp, false),
        new("COMP 9(3)V99",  "9(3)V99",  UsageKind.Comp, false),
        new("COMP S9(4)",    "S9(4)",    UsageKind.Comp, true),
        new("COMP S9(5)V99", "S9(5)V99", UsageKind.Comp, true),
        new("COMP 9(15)",    "9(15)",    UsageKind.Comp, false),      // mid-range (8-byte binary)
        new("COMP 9(3)P",    "9(3)P",    UsageKind.Comp, false),      // trailing-P
        // COMP-3 (packed; 2n−1 capacity)
        new("COMP3 9(5)",    "9(5)",     UsageKind.Comp3, false),
        new("COMP3 9(7)V99", "9(7)V99",  UsageKind.Comp3, false),
        new("COMP3 S9(5)V99","S9(5)V99", UsageKind.Comp3, true),
        new("COMP3 S9(9)",   "S9(9)",    UsageKind.Comp3, true),
        new("COMP3 9(15)",   "9(15)",    UsageKind.Comp3, false),     // mid-range (8-byte packed, cap 15)
        new("COMP3 S9(13)V99","S9(13)V99",UsageKind.Comp3, true),     // mid-range, fractional
        new("COMP3 9(3)PP",  "9(3)PP",   UsageKind.Comp3, false),     // trailing-P (×100)
        new("COMP3 S99P",    "S99P",     UsageKind.Comp3, true),      // trailing-P, signed
        // COMP-5 (binary capacity)
        new("COMP5 9(4)",    "9(4)",     UsageKind.Comp5, false),
        new("COMP5 S9(4)",   "S9(4)",    UsageKind.Comp5, true),
        new("COMP5 9(9)",    "9(9)",     UsageKind.Comp5, false),
        new("COMP5 S9(9)",   "S9(9)",    UsageKind.Comp5, true),
        new("COMP5 9(18)",   "9(18)",    UsageKind.Comp5, false),     // 8-byte (in-window values only)
        new("COMP5 S9(18)",  "S9(18)",   UsageKind.Comp5, true),      // 8-byte signed
        new("COMP5 S9(4)V99","S9(4)V99", UsageKind.Comp5, true),      // fractional binary
        new("COMP5 9(3)P",   "9(3)P",    UsageKind.Comp5, false),     // trailing-P
    ];

    // Values exercise sign, rounding ties/near-ties, magnitudes near capacity, overflow, P-scaling multiples,
    // and the 10–18-significant-digit mid-range. All stay inside the legacy faithful window (scaled magnitude
    // < long.MaxValue) and avoid the COMP-5 signed negative extreme (−2^(n−1)), which is validated separately.
    private static readonly string[] Values =
    [
        "0", "1", "-1", "5", "-5", "12", "-12", "12.34", "-12.34", "99.99", "-99.99",
        "123.456", "-123.456", "0.005", "-0.005", "0.05", "2.5", "-2.5", "3.5", "-3.5",
        "1.25", "-1.25", "1.35", "7.7", "-7.7", "99.5", "-99.5", "0.001", "0.999",
        "9.995", "-9.995", "100", "1000", "9999", "99999", "-99999", "12345.678", "-12345.678",
        "0.4", "0.5", "0.6", "-0.5",
        // mid-range (10–18 significant digits — past the int/decimal-9-digit grid, inside the long window)
        "123456789012345", "-123456789012345", "999999999999999", "1000000000000000",
        "999999999999999999", "1234567890123.45", "-1234567890123.45",
    ];

    private static readonly CobolRounding[] Modes =
    [
        CobolRounding.Truncation, CobolRounding.NearestAwayFromZero, CobolRounding.AwayFromZero,
        CobolRounding.NearestEven, CobolRounding.NearestTowardZero, CobolRounding.Prohibited,
        CobolRounding.TowardGreater, CobolRounding.TowardLesser,
    ];

    /// <summary>Runs the legacy byte pipeline: store <paramref name="value"/> into a zeroed receiver.</summary>
    private static (bool sizeError, decimal stored) Legacy(decimal value, PicDescriptor pic, CobolRounding mode)
    {
        var dst = new byte[pic.StorageLength];
        PicRuntime.EncodeNumeric(dst, 0, dst.Length, pic, 0m);
        var status = new ArithmeticStatus();
        PicRuntime.AddNumericLiteral(dst, 0, dst.Length, pic, value, (int)mode, ref status);
        return status.SizeError
            ? (true, 0m)
            : (false, PicRuntime.DecodeNumeric(dst, 0, dst.Length, pic));
    }

    [Fact]
    public void TryStore_MatchesLegacyBytePipeline_AcrossGrid()
    {
        var mismatches = new StringBuilder();
        int cases = 0;

        foreach (Field field in Fields)
        {
            PicDescriptor pic = field.Pic;
            var profile = NumProfile.FromDescriptor(pic);

            foreach (string literal in Values)
            {
                decimal value = decimal.Parse(literal, CultureInfo.InvariantCulture);
                foreach (CobolRounding mode in Modes)
                {
                    cases++;
                    (bool legacyErr, decimal legacyStored) = Legacy(value, pic, mode);
                    bool ok = CobolNum.TryStore(CobolDecimal.FromDecimal(value), profile, mode, out CobolDecimal stored);

                    if (legacyErr != !ok)
                    {
                        mismatches.AppendLine(
                            $"  [{field.Label}] value={literal} mode={mode}: SIZE ERROR legacy={legacyErr} cobol={!ok}");
                        continue;
                    }
                    if (!ok)
                        continue; // both signalled SIZE ERROR — no stored value to compare

                    // The legacy decoded value reflects the receiver's representation, which drops the sign for
                    // an unsigned field (the encoder's job). TryStore returns the signed rounded value, so mirror
                    // that representation here before comparing.
                    decimal cobolStored = (field.Signed ? stored : stored.Abs()).ToDecimal();
                    if (legacyStored != cobolStored)
                    {
                        mismatches.AppendLine(
                            $"  [{field.Label}] value={literal} mode={mode}: stored legacy={legacyStored} cobol={cobolStored}");
                    }
                }
            }
        }

        Assert.True(mismatches.Length == 0,
            $"CobolNum diverged from the legacy byte pipeline on {cases} cases:\n{mismatches}");
    }

    // ---------- the eight rounding modes, pinned to an independent known-answer table ----------

    // Stored into a one-fraction-digit field: each input × each mode → the expected one-decimal result.
    [Theory]
    [InlineData("0.25", CobolRounding.Truncation, "0.2")]
    [InlineData("0.25", CobolRounding.NearestAwayFromZero, "0.3")]
    [InlineData("0.25", CobolRounding.NearestEven, "0.2")]
    [InlineData("0.35", CobolRounding.NearestEven, "0.4")]
    [InlineData("0.25", CobolRounding.NearestTowardZero, "0.2")]
    [InlineData("0.26", CobolRounding.NearestTowardZero, "0.3")]
    [InlineData("0.21", CobolRounding.AwayFromZero, "0.3")]
    [InlineData("-0.21", CobolRounding.AwayFromZero, "-0.3")]
    [InlineData("0.21", CobolRounding.TowardGreater, "0.3")]
    [InlineData("-0.21", CobolRounding.TowardGreater, "-0.2")]
    [InlineData("0.29", CobolRounding.TowardLesser, "0.2")]
    [InlineData("-0.21", CobolRounding.TowardLesser, "-0.3")]
    public void RoundingModes_KnownAnswers_OneFractionDigit(string input, CobolRounding mode, string expected)
    {
        var profile = new NumProfile
        {
            Digits = 3, FractionDigits = 1, Signed = true,
            Truncation = NumericTruncation.DigitCount, StorageLength = 3,
        };
        bool ok = CobolNum.TryStore(
            CobolDecimal.FromDecimal(decimal.Parse(input, CultureInfo.InvariantCulture)),
            profile, mode, out CobolDecimal stored);
        Assert.True(ok);
        Assert.Equal(
            CobolDecimal.FromDecimal(decimal.Parse(expected, CultureInfo.InvariantCulture)),
            stored);
    }

    // ---------- >28-digit DigitCount / Packed capacity (legacy decimal path is blind here) ----------

    [Fact]
    public void TryStore_DigitCount_31Digits_WithinCapacity_BeyondDecimal()
    {
        var profile = new NumProfile
        {
            Digits = 31, FractionDigits = 0, Signed = false,
            Truncation = NumericTruncation.DigitCount, StorageLength = 31,
        };
        var thirtyOneNines = new CobolDecimal(BigInteger.Parse(new string('9', 31)), 0);
        Assert.True(CobolNum.TryStore(thirtyOneNines, profile, CobolRounding.Truncation, out CobolDecimal stored));
        Assert.Equal(thirtyOneNines, stored);

        var thirtyTwoDigits = new CobolDecimal(BigInteger.Parse("1" + new string('0', 31)), 0); // 10^31, 32 digits
        Assert.False(CobolNum.TryStore(thirtyTwoDigits, profile, CobolRounding.Truncation, out _));
    }

    [Fact]
    public void TryStore_PackedDecimal_CapacityIsTwoNMinusOne_BeyondDecimal()
    {
        // 16-byte COMP-3 → 2×16−1 = 31 digit capacity.
        var profile = new NumProfile
        {
            Digits = 31, FractionDigits = 0, Signed = true,
            Truncation = NumericTruncation.PackedDecimal, StorageLength = 16,
        };
        var thirtyOne = new CobolDecimal(-BigInteger.Parse(new string('9', 31)), 0);
        Assert.True(CobolNum.TryStore(thirtyOne, profile, CobolRounding.Truncation, out _));

        var thirtyTwo = new CobolDecimal(BigInteger.Parse("1" + new string('0', 31)), 0);
        Assert.False(CobolNum.TryStore(thirtyTwo, profile, CobolRounding.Truncation, out _));
    }

    [Fact]
    public void TryStore_RoundsCorrectly_BeyondDecimalRange()
    {
        // A 30-digit integer with two fractional places, rounded to integer (NEAREST-AWAY): independent
        // BigInteger reference. 10^30 + 0.5 → 10^30 + 1.
        var profile = new NumProfile
        {
            Digits = 31, FractionDigits = 0, Signed = false,
            Truncation = NumericTruncation.DigitCount, StorageLength = 31,
        };
        BigInteger tenTo30 = BigInteger.Pow(10, 30);
        var value = new CobolDecimal(tenTo30 * 100 + 50, 2); // (10^30).50
        Assert.True(CobolNum.TryStore(value, profile, CobolRounding.NearestAwayFromZero, out CobolDecimal stored));
        Assert.Equal(new CobolDecimal(tenTo30 + 1, 0), stored);
    }

    // ---------- COMP-5 binary capacity boundaries (independent two's-complement reference) ----------

    [Fact]
    public void TryStore_Comp5_SignedBoundaries_AreExact()
    {
        var s16 = new NumProfile
        {
            Digits = 4, FractionDigits = 0, Signed = true,
            Truncation = NumericTruncation.BinaryCapacity, StorageLength = 2,
        };
        Assert.True(Stores(s16, "32767"));
        Assert.False(Stores(s16, "32768"));
        Assert.True(Stores(s16, "-32768"));  // the negative extreme legacy mis-flags; CobolNum is correct
        Assert.False(Stores(s16, "-32769"));

        var s32 = s16 with { Digits = 9, StorageLength = 4 };
        Assert.True(Stores(s32, int.MinValue.ToString(CultureInfo.InvariantCulture)));
        Assert.False(Stores(s32, ((long)int.MinValue - 1).ToString(CultureInfo.InvariantCulture)));
        Assert.True(Stores(s32, int.MaxValue.ToString(CultureInfo.InvariantCulture)));
        Assert.False(Stores(s32, ((long)int.MaxValue + 1).ToString(CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void TryStore_Comp5_UnsignedBoundaries_StoreMagnitude()
    {
        var u16 = new NumProfile
        {
            Digits = 4, FractionDigits = 0, Signed = false,
            Truncation = NumericTruncation.BinaryCapacity, StorageLength = 2,
        };
        Assert.True(Stores(u16, "65535"));
        Assert.False(Stores(u16, "65536"));
        Assert.True(Stores(u16, "0"));

        // An unsigned receiver accepts a negative value whose magnitude fits (capacity bounds the magnitude);
        // TryStore returns the signed value — the encoder drops the sign for the unsigned representation
        // (ISO §14.9.25 GR8).
        bool ok = CobolNum.TryStore(CobolDecimal.FromInt64(-1), u16, CobolRounding.Truncation, out CobolDecimal stored);
        Assert.True(ok);
        Assert.Equal(CobolDecimal.FromInt64(-1), stored);
    }

    private static bool Stores(NumProfile profile, string literal)
        => CobolNum.TryStore(new CobolDecimal(BigInteger.Parse(literal), 0), profile, CobolRounding.Truncation, out _);

    // ---------- mid-band (19–29 digit) capacity — past the long window, legacy is blind, independent reference ----------

    [Theory]
    [InlineData(19)]
    [InlineData(25)]
    [InlineData(29)]
    public void TryStore_DigitCount_MidBand_BeyondLong_IndependentReference(int digits)
    {
        var profile = new NumProfile
        {
            Digits = digits, FractionDigits = 0, Signed = false,
            Truncation = NumericTruncation.DigitCount, StorageLength = digits,
        };
        var atCapacity = new CobolDecimal(BigInteger.Parse(new string('9', digits)), 0);
        Assert.True(CobolNum.TryStore(atCapacity, profile, CobolRounding.Truncation, out CobolDecimal stored));
        Assert.Equal(atCapacity, stored);
        var overCapacity = new CobolDecimal(BigInteger.Parse("1" + new string('0', digits)), 0); // digits+1 digits
        Assert.False(CobolNum.TryStore(overCapacity, profile, CobolRounding.Truncation, out _));
    }

    [Theory]
    [InlineData(10, 19)] // storageLength bytes → packed capacity 2n−1
    [InlineData(13, 25)]
    [InlineData(15, 29)]
    public void TryStore_PackedDecimal_MidBand_IndependentReference(int storageLength, int capacity)
    {
        var profile = new NumProfile
        {
            Digits = capacity, FractionDigits = 0, Signed = true,
            Truncation = NumericTruncation.PackedDecimal, StorageLength = storageLength,
        };
        Assert.True(Stores(profile, "-" + new string('9', capacity)));
        Assert.False(Stores(profile, "1" + new string('0', capacity)));
    }

    // ---------- 8-byte COMP-5 (the unsigned (long.Max, ulong.Max] range legacy cannot represent) ----------

    [Fact]
    public void TryStore_Comp5_EightByteBoundaries_IndependentReference()
    {
        var s64 = new NumProfile
        {
            Digits = 18, FractionDigits = 0, Signed = true,
            Truncation = NumericTruncation.BinaryCapacity, StorageLength = 8,
        };
        Assert.True(Stores(s64, long.MinValue.ToString(CultureInfo.InvariantCulture)));
        Assert.False(Stores(s64, (new BigInteger(long.MinValue) - 1).ToString(CultureInfo.InvariantCulture)));
        Assert.True(Stores(s64, long.MaxValue.ToString(CultureInfo.InvariantCulture)));
        Assert.False(Stores(s64, (new BigInteger(long.MaxValue) + 1).ToString(CultureInfo.InvariantCulture)));

        var u64 = s64 with { Signed = false };
        Assert.True(Stores(u64, "10000000000000000000"));                    // 1e19: > long.Max, < 2^64 → stored
        Assert.True(Stores(u64, ulong.MaxValue.ToString(CultureInfo.InvariantCulture))); // 2^64−1 → stored
        Assert.False(Stores(u64, (new BigInteger(ulong.MaxValue) + 1).ToString(CultureInfo.InvariantCulture))); // 2^64
    }

    // ---------- trailing-P (PIC 99PP → 2 stored digits, ×100) independent known-answers ----------

    [Theory]
    [InlineData("150", CobolRounding.NearestAwayFromZero, "200")]
    [InlineData("150", CobolRounding.Truncation, "100")]
    [InlineData("149", CobolRounding.NearestAwayFromZero, "100")]
    [InlineData("9900", CobolRounding.Truncation, "9900")] // 99 units — at capacity
    public void TryStore_TrailingP_KnownAnswers(string input, CobolRounding mode, string expected)
    {
        var profile = TrailingPP();
        Assert.True(CobolNum.TryStore(new CobolDecimal(BigInteger.Parse(input), 0), profile, mode, out CobolDecimal stored));
        Assert.Equal(new CobolDecimal(BigInteger.Parse(expected), 0), stored);
    }

    [Fact]
    public void TryStore_TrailingP_OverCapacity_SizeError()
        => Assert.False(CobolNum.TryStore(new CobolDecimal(10000, 0), TrailingPP(), CobolRounding.Truncation, out _));

    private static NumProfile TrailingPP() => new()
    {
        Digits = 2, FractionDigits = 0, TrailingScaleDigits = 2, Signed = false,
        Truncation = NumericTruncation.DigitCount, StorageLength = 2,
    };

    // ---------- PROHIBITED ----------

    [Fact]
    public void TryStore_Prohibited_InexactIsSizeError_ExactIsStored()
    {
        var profile = new NumProfile
        {
            Digits = 3, FractionDigits = 1, Signed = true,
            Truncation = NumericTruncation.DigitCount, StorageLength = 3,
        };
        // 2.25 is inexact at one fraction digit → SIZE ERROR.
        Assert.False(CobolNum.TryStore(CobolDecimal.FromDecimal(2.25m), profile, CobolRounding.Prohibited, out _));
        // 2.2 is exact → stored.
        Assert.True(CobolNum.TryStore(CobolDecimal.FromDecimal(2.2m), profile, CobolRounding.Prohibited, out CobolDecimal stored));
        Assert.Equal(CobolDecimal.FromDecimal(2.2m), stored);
    }
}
