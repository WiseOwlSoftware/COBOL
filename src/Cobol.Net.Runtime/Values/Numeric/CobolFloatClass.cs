// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>The questions ISO/IEC 1989:2023 §8.8.4.4.4 GR3 asks of the CONTENT of a floating-point carrier.</summary>
public enum FloatClassTest
{
    /// <summary>GR3 n) 1. b. — NUMERIC over a standard floating-point usage: "represents a finite numeric value".
    /// Also the whole of GR3 l)'s IN-ARITHMETIC-RANGE for a carrier the mode's intermediate contains.</summary>
    Finite,
    /// <summary>GR3 g) — FARTHEST-FROM-ZERO: the carrier's largest finite magnitude, either sign.</summary>
    FarthestFromZero,
    /// <summary>GR3 h) — FLOAT-INFINITY: positive or negative infinity.</summary>
    Infinity,
    /// <summary>GR3 i) — FLOAT-NOT-A-NUMBER: a quiet or a signaling NaN.</summary>
    NotANumber,
    /// <summary>GR3 j) — FLOAT-NOT-A-NUMBER-QUIET.</summary>
    QuietNaN,
    /// <summary>GR3 k) — FLOAT-NOT-A-NUMBER-SIGNALING.</summary>
    SignalingNaN,
    /// <summary>GR3 m) — NEAREST-TO-ZERO: the carrier's smallest nonzero magnitude (the least subnormal), either
    /// sign.</summary>
    NearestToZero,
}

/// <summary>
/// ⛔ THE ONE PLACE a floating-point class condition is decided (ISO §8.8.4.4.4 GR3 g)–m) and n) 1. b.; kb/Work
/// PB225), and it decides every question on the carrier's RAW ISO/IEC 60559:2020 bits — never on a
/// <c>double</c> the value was converted to. That is not a style choice: a binary32 → binary64 widening QUIETS a
/// signaling NaN, so GR3 j)/k) cannot be answered after one; and the extremes of g)/m) are exact bit patterns of
/// the carrier's own format, which a comparison against a decimal literal widened to binary64 would miss for
/// binary32 (3.4028235E+38 is not <c>(double)float.MaxValue</c>). The encodings are the ones
/// <c>CobolNet.Binding.IeeeSpecials</c> writes (SET Format 15), read back: exponent all ones with a zero
/// significand is an infinity, with a nonzero one a NaN, and the LEADING significand bit tells quiet (set) from
/// signaling (clear) — ISO/IEC 60559:2020 §6.2.1.
/// </summary>
public static class CobolFloatClass
{
    /// <summary>A binary32 carrier (USAGE FLOAT-BINARY-32 / FLOAT-SHORT / COMP-1).</summary>
    public static bool Is(float value, FloatClassTest test) => IsBits(BitConverter.SingleToUInt32Bits(value), single: true, test);

    /// <summary>A binary64 carrier (USAGE FLOAT-BINARY-64 / FLOAT-LONG / COMP-2 …).</summary>
    public static bool Is(double value, FloatClassTest test) => IsBits(BitConverter.DoubleToUInt64Bits(value), single: false, test);

    /// <summary>A float item stored as its IEEE window image (a REDEFINES view, a whole-group-aliased leaf) — the
    /// bits are taken off the image by <see cref="CobolNum.ImageFloatBits"/>, never through a decoded value.</summary>
    public static bool IsImage(string image, in NumProfile item, FloatClassTest test) =>
        IsBits(CobolNum.ImageFloatBits(image, item), single: item.ByteForm is NumericByteForm.Ieee32, test);

    /// <summary>The decision itself, over the carrier's interchange-format bits (binary32 in the low 32).</summary>
    public static bool IsBits(ulong bits, bool single, FloatClassTest test)
    {
        ulong exponent = single ? 0x7F80_0000UL : 0x7FF0_0000_0000_0000UL;
        ulong significand = single ? 0x007F_FFFFUL : 0x000F_FFFF_FFFF_FFFFUL;
        ulong quietBit = single ? 0x0040_0000UL : 0x0008_0000_0000_0000UL;
        ulong magnitude = bits & (exponent | significand);          // the sign bit dropped: "either sign"
        bool special = (bits & exponent) == exponent;
        bool nan = special && (bits & significand) != 0;
        return test switch
        {
            FloatClassTest.Finite => !special,
            FloatClassTest.Infinity => special && !nan,
            FloatClassTest.NotANumber => nan,
            FloatClassTest.QuietNaN => nan && (bits & quietBit) != 0,
            FloatClassTest.SignalingNaN => nan && (bits & quietBit) == 0,
            // The largest finite magnitude: exponent one below all-ones, significand all ones.
            FloatClassTest.FarthestFromZero => magnitude == (exponent - (single ? 0x0080_0000UL : 0x0010_0000_0000_0000UL)) + significand,
            // The least subnormal: exponent zero, significand one.
            FloatClassTest.NearestToZero => magnitude == 1UL,
            _ => throw new ArgumentOutOfRangeException(nameof(test), test, "no such floating-point class test"),
        };
    }
}
