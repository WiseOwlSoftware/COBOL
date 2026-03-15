// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Text;

namespace CobolSharp.Runtime;

/// <summary>
/// PIC/USAGE-aware runtime for COBOL data movement, arithmetic, and comparison.
/// All methods use byte[] + offset + length + PicDescriptor.
/// </summary>
public static class PicRuntime
{
    // ══════════════════════════════════════
    // MOVE numeric
    // ══════════════════════════════════════

    public static void MoveNumeric(
        byte[] destArea, int destOffset, int destLength, PicDescriptor destPic,
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        int roundingMode)
    {
        decimal value = DecodeNumeric(srcArea, srcOffset, srcLength, srcPic);
        value = ApplyScalingAndRounding(value, destPic, roundingMode);
        EncodeNumeric(destArea, destOffset, destLength, destPic, value);
    }

    public static void MoveNumericLiteral(
        byte[] destArea, int destOffset, int destLength, PicDescriptor destPic,
        decimal literal, int roundingMode = 0)
    {
        decimal value = ApplyScalingAndRounding(literal, destPic, roundingMode);
        EncodeNumeric(destArea, destOffset, destLength, destPic, value);
    }

    // ══════════════════════════════════════
    // MULTIPLY
    // ══════════════════════════════════════

    public static void MultiplyNumeric(
        byte[] destArea, int destOffset, int destLength, PicDescriptor destPic,
        byte[] leftArea, int leftOffset, int leftLength, PicDescriptor leftPic,
        byte[] rightArea, int rightOffset, int rightLength, PicDescriptor rightPic,
        int roundingMode)
    {
        decimal left = DecodeNumeric(leftArea, leftOffset, leftLength, leftPic);
        decimal right = DecodeNumeric(rightArea, rightOffset, rightLength, rightPic);
        decimal value = left * right;
        value = ApplyScalingAndRounding(value, destPic, roundingMode);
        EncodeNumeric(destArea, destOffset, destLength, destPic, value);
    }

    public static void MultiplyNumericLiteral(
        byte[] destArea, int destOffset, int destLength, PicDescriptor destPic,
        decimal literal,
        byte[] otherArea, int otherOffset, int otherLength, PicDescriptor otherPic,
        int roundingMode)
    {
        decimal other = DecodeNumeric(otherArea, otherOffset, otherLength, otherPic);
        decimal value = literal * other;
        value = ApplyScalingAndRounding(value, destPic, roundingMode);
        EncodeNumeric(destArea, destOffset, destLength, destPic, value);
    }

    // ══════════════════════════════════════
    // ADD
    // ══════════════════════════════════════

    public static void AddNumeric(
        byte[] destArea, int destOffset, int destLength, PicDescriptor destPic,
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        int roundingMode)
    {
        decimal dest = DecodeNumeric(destArea, destOffset, destLength, destPic);
        decimal src = DecodeNumeric(srcArea, srcOffset, srcLength, srcPic);
        decimal value = dest + src;
        value = ApplyScalingAndRounding(value, destPic, roundingMode);
        EncodeNumeric(destArea, destOffset, destLength, destPic, value);
    }

    public static void AddNumericLiteral(
        byte[] destArea, int destOffset, int destLength, PicDescriptor destPic,
        decimal literal, int roundingMode)
    {
        decimal dest = DecodeNumeric(destArea, destOffset, destLength, destPic);
        decimal value = dest + literal;
        value = ApplyScalingAndRounding(value, destPic, roundingMode);
        EncodeNumeric(destArea, destOffset, destLength, destPic, value);
    }

    // ══════════════════════════════════════
    // COMPARE
    // ══════════════════════════════════════

    /// <summary>Returns -1, 0, or 1.</summary>
    public static int CompareNumeric(
        byte[] leftArea, int leftOffset, int leftLength, PicDescriptor leftPic,
        byte[] rightArea, int rightOffset, int rightLength, PicDescriptor rightPic)
    {
        decimal left = DecodeNumeric(leftArea, leftOffset, leftLength, leftPic);
        decimal right = DecodeNumeric(rightArea, rightOffset, rightLength, rightPic);
        return Math.Sign(left - right);
    }

    public static int CompareNumericToLiteral(
        byte[] leftArea, int leftOffset, int leftLength, PicDescriptor leftPic,
        decimal literal)
    {
        decimal left = DecodeNumeric(leftArea, leftOffset, leftLength, leftPic);
        return Math.Sign(left - literal);
    }

    // ══════════════════════════════════════
    // Alphanumeric MOVE
    // ══════════════════════════════════════

    public static void MoveAlpha(
        byte[] dest, int destOffset, int destLength,
        byte[] src, int srcOffset, int srcLength)
    {
        int copyLen = Math.Min(srcLength, destLength);
        Array.Copy(src, srcOffset, dest, destOffset, copyLen);
        for (int i = copyLen; i < destLength; i++)
            dest[destOffset + i] = (byte)' ';
    }

    // ══════════════════════════════════════
    // Decode: bytes → decimal
    // ══════════════════════════════════════

    public static decimal DecodeNumeric(
        byte[] area, int offset, int length, PicDescriptor pic)
    {
        return pic.Usage switch
        {
            UsageKind.Display => DecodeDisplay(area, offset, length),
            UsageKind.Comp3 or UsageKind.PackedDecimal => DecodeComp3(area, offset, length),
            _ => DecodeDisplay(area, offset, length)
        };
    }

    private static decimal DecodeDisplay(byte[] area, int offset, int length)
    {
        var s = Encoding.ASCII.GetString(area, offset, length).Trim();
        if (string.IsNullOrEmpty(s)) return 0m;
        if (decimal.TryParse(s,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out var value))
            return value;
        return 0m;
    }

    private static decimal DecodeComp3(byte[] area, int offset, int length)
    {
        if (length == 0) return 0m;
        int lastByte = area[offset + length - 1];
        bool negative = (lastByte & 0x0F) == 0x0D;
        long intPart = 0;
        for (int i = offset; i < offset + length - 1; i++)
        {
            intPart = intPart * 10 + ((area[i] >> 4) & 0x0F);
            intPart = intPart * 10 + (area[i] & 0x0F);
        }
        intPart = intPart * 10 + ((lastByte >> 4) & 0x0F);
        return negative ? -intPart : intPart;
    }

    // ══════════════════════════════════════
    // Encode: decimal → bytes
    // ══════════════════════════════════════

    public static void EncodeNumeric(
        byte[] area, int offset, int length, PicDescriptor pic, decimal value)
    {
        switch (pic.Usage)
        {
            case UsageKind.Comp3:
            case UsageKind.PackedDecimal:
                EncodeComp3(area, offset, length, value);
                break;
            default:
                EncodeDisplay(area, offset, length, value);
                break;
        }
    }

    private static void EncodeDisplay(byte[] area, int offset, int length, decimal value)
    {
        string s = value.ToString("G", CultureInfo.InvariantCulture);
        // Right-justify
        for (int i = 0; i < length; i++)
            area[offset + i] = (byte)' ';
        var bytes = Encoding.ASCII.GetBytes(s);
        int len = Math.Min(bytes.Length, length);
        int start = length - len;
        Array.Copy(bytes, 0, area, offset + start, len);
    }

    private static void EncodeComp3(byte[] area, int offset, int length, decimal value)
    {
        string s = Math.Abs(value).ToString("F0", CultureInfo.InvariantCulture);
        bool negative = value < 0;
        for (int i = offset; i < offset + length; i++) area[i] = 0;

        int digitCount = s.Length;
        int byteIdx = offset + length - 1;
        int digIdx = digitCount - 1;
        byte sign = (byte)(negative ? 0x0D : 0x0C);
        byte lastDigit = digIdx >= 0 ? (byte)(s[digIdx--] - '0') : (byte)0;
        area[byteIdx--] = (byte)((lastDigit << 4) | sign);
        while (byteIdx >= offset)
        {
            byte lo = digIdx >= 0 ? (byte)(s[digIdx--] - '0') : (byte)0;
            byte hi = digIdx >= 0 ? (byte)(s[digIdx--] - '0') : (byte)0;
            area[byteIdx--] = (byte)((hi << 4) | lo);
        }
    }

    // ══════════════════════════════════════
    // Scaling / rounding
    // ══════════════════════════════════════

    private static decimal ApplyScalingAndRounding(decimal value, PicDescriptor destPic, int roundingMode)
    {
        int scale = destPic.FractionDigits;
        if (scale < 0) scale = 0;
        decimal factor = 1m;
        for (int i = 0; i < scale; i++) factor *= 10m;
        return roundingMode switch
        {
            1 => Math.Round(value, scale, MidpointRounding.AwayFromZero),
            _ => decimal.Truncate(value * factor) / factor
        };
    }

    public static string FormatNumericForDisplay(decimal value, int fractionDigits)
    {
        if (fractionDigits > 0)
            return value.ToString("0." + new string('0', fractionDigits), CultureInfo.InvariantCulture);
        return ((long)value).ToString(CultureInfo.InvariantCulture);
    }
}
