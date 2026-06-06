// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Runtime.Numeric;

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
/// The compact, runtime-facing numeric profile of a COBOL data item: just enough to scale, round, and
/// bound-check a value, with no compile-time, byte-layout, or formatting concerns.
///
/// <para>It is the typed-path counterpart to the legacy <see cref="PicDescriptor"/> for arithmetic. Per the
/// data-model ADR (<c>docs/DATA_MODEL_ARCHITECTURE.md</c> §9) the runtime should be handed only this — a
/// small <c>readonly record struct</c> — instead of constructing a full <see cref="PicDescriptor"/> per
/// operation. During the migration it is derived from the existing descriptor via
/// <see cref="FromDescriptor"/>; this is additive and changes no behavior on its own.</para>
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

    /// <summary>
    /// Derives a <see cref="NumProfile"/> from an existing <see cref="PicDescriptor"/>. The bridge used
    /// during the data-model migration so the typed numeric pipeline can be exercised against, and later
    /// replace, the byte path while both coexist.
    /// </summary>
    public static NumProfile FromDescriptor(PicDescriptor pic) => new()
    {
        Digits = pic.TotalDigits,
        FractionDigits = pic.FractionDigits,
        LeadingScaleDigits = pic.LeadingScaleDigits,
        TrailingScaleDigits = pic.TrailingScaleDigits,
        Signed = pic.IsSigned,
        Truncation = TruncationFor(pic.Usage),
        StorageLength = pic.StorageLength,
    };

    /// <summary>Maps a <see cref="UsageKind"/> to its capacity discipline.</summary>
    private static NumericTruncation TruncationFor(UsageKind usage) => usage switch
    {
        UsageKind.Comp3 or UsageKind.PackedDecimal => NumericTruncation.PackedDecimal,
        UsageKind.Comp5 => NumericTruncation.BinaryCapacity,
        // DISPLAY, COMP/COMPUTATIONAL, BINARY → digit-count. COMP-1/COMP-2 are floating-point and bypass
        // the fixed-point CobolNum path entirely (their callers guard on usage, as StoreArithmeticResult
        // does), so their profile's discipline is immaterial; default to DigitCount.
        _ => NumericTruncation.DigitCount,
    };
}
