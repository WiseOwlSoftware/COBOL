// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// How a numeric item's capacity (the SIZE ERROR boundary) is defined. COBOL distinguishes three
/// overflow disciplines that <see cref="UsageKind"/> alone conflates (ISO §8.5.1.2 / §13.18.60).
/// </summary>
public enum NumericTruncation
{
    /// <summary>Overflow when the stored-digit count exceeds the PICTURE digit count. DISPLAY, COMP/COMPUTATIONAL,
    /// and BINARY all truncate by digit count: <c>PIC 99 COMP</c> holds 0–99, not 0–32767.</summary>
    DigitCount = 0,
    /// <summary>Overflow when the stored-digit count exceeds the packed-decimal capacity
    /// <c>2 × byteLength − 1</c>. COMP-3 / PACKED-DECIMAL.</summary>
    PackedDecimal = 1,
    /// <summary>Overflow when the value leaves the native two's-complement range of the byte width.
    /// COMP-5 / COMPUTATIONAL-5 (and the COBOL-2002 BINARY-* usages, which lower to COMP-5):
    /// <c>PIC 9(4) COMP-5</c> holds 0–65535, <c>PIC S9(4) COMP-5</c> holds −32768–32767.</summary>
    BinaryCapacity = 2,
}

/// <summary>
/// How a signed numeric item presents its sign in its DISPLAY image (ISO §13.18.45 SIGN / §8.5.1.2). For USAGE
/// DISPLAY this is the operational-sign convention; for binary/packed usages the DISPLAY image carries a leading
/// minus only when negative.
/// </summary>
public enum NumericSign
{
    /// <summary>USAGE DISPLAY default: the sign is over-punched onto the last digit (IBM-ASCII <c>{A-I</c> / <c>}J-R</c>).</summary>
    TrailingOverpunch = 0,
    /// <summary>USAGE DISPLAY, SIGN LEADING (no SEPARATE): over-punched onto the first digit.</summary>
    LeadingOverpunch = 1,
    /// <summary>USAGE DISPLAY, SIGN LEADING SEPARATE: a leading <c>+</c>/<c>-</c> character (always present).</summary>
    LeadingSeparate = 2,
    /// <summary>USAGE DISPLAY, SIGN TRAILING SEPARATE: a trailing <c>+</c>/<c>-</c> character (always present).</summary>
    TrailingSeparate = 3,
    /// <summary>Binary/packed (COMP/COMP-3/COMP-5): a leading <c>-</c> only when negative; positive/zero is bare.</summary>
    BinaryMinus = 4,
}

/// <summary>
/// The compact, runtime-facing numeric profile of a COBOL data item: just enough to scale, round, and
/// bound-check a value, with no byte-layout or formatting concerns. The COBOL.NET compiler builds it directly
/// from a <c>PicInfo</c> (digits, scale, sign, usage→capacity discipline) and threads it into every numeric
/// store so arithmetic obeys the receiver's PICTURE+USAGE (truncation / ROUNDED / SIZE ERROR).
/// </summary>
public readonly record struct NumProfile
{
    /// <summary>Count of digit positions in the PICTURE (the '9' count; integer + fraction).</summary>
    public required int Digits { get; init; }

    /// <summary>Digits after the implied decimal point (the V position).</summary>
    public required int FractionDigits { get; init; }

    /// <summary>Leading-P scaling positions — shift the implied point left of the stored digits.</summary>
    public int LeadingScaleDigits { get; init; }

    /// <summary>Trailing-P scaling positions — the stored digits are multiples of 10^this.</summary>
    public int TrailingScaleDigits { get; init; }

    /// <summary>Whether the item carries an operational sign (PIC S or a SIGN clause).</summary>
    public required bool Signed { get; init; }

    /// <summary>How the sign is represented in the DISPLAY image (only consulted when <see cref="Signed"/>).</summary>
    public NumericSign SignKind { get; init; }

    /// <summary>Which capacity discipline bounds the value (the SIZE ERROR boundary).</summary>
    public required NumericTruncation Truncation { get; init; }

    /// <summary>Storage width in bytes — used for <see cref="NumericTruncation.PackedDecimal"/> capacity
    /// (2n−1 digits) and <see cref="NumericTruncation.BinaryCapacity"/> two's-complement range.</summary>
    public int StorageLength { get; init; }

    /// <summary>The number of fractional positions the value is scaled/rounded to: <c>FractionDigits +
    /// LeadingScaleDigits</c>, never negative (ISO §14.9.4 — leading P adds to the fraction scale).</summary>
    public int FractionScale
    {
        get
        {
            int s = FractionDigits + LeadingScaleDigits;
            return s < 0 ? 0 : s;
        }
    }
}
