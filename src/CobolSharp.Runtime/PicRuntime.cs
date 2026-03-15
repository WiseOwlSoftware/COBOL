// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Text;

namespace CobolSharp.Runtime;

/// <summary>
/// Status returned by MOVE operations.
/// </summary>
public struct MoveStatus
{
    public bool Truncated { get; set; }
}

/// <summary>
/// Status returned by arithmetic operations (for ON SIZE ERROR).
/// </summary>
public struct ArithmeticStatus
{
    public bool SizeError { get; set; }
}

/// <summary>
/// PIC/USAGE-aware runtime for COBOL data movement, arithmetic, and comparison.
/// Public surface organized by (OperationKind × source CobolCategory × target CobolCategory).
/// All methods use byte[] + offset + length + PicDescriptor.
/// </summary>
public static class PicRuntime
{
    // ══════════════════════════════════════════════════════════
    // MOVE: Numeric → …
    // ══════════════════════════════════════════════════════════

    public static void MoveNumericToNumeric(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        decimal value = DecodeNumeric(srcArea, srcOffset, srcLength, srcPic);
        value = ApplyScalingAndRounding(value, dstPic, roundingMode);
        EncodeNumeric(dstArea, dstOffset, dstLength, dstPic, value);
    }

    public static void MoveNumericToNumericEdited(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        // TODO: numeric editing formatting (Z, *, $, etc.)
        // For now: decode, scale, encode as display
        decimal value = DecodeNumeric(srcArea, srcOffset, srcLength, srcPic);
        value = ApplyScalingAndRounding(value, dstPic, roundingMode);
        EncodeNumeric(dstArea, dstOffset, dstLength, dstPic, value);
    }

    public static void MoveNumericToAlphanumeric(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        decimal value = DecodeNumeric(srcArea, srcOffset, srcLength, srcPic);
        string formatted = FormatNumericForDisplay(value, srcPic.FractionDigits);
        MoveStringToBytes(dstArea, dstOffset, dstLength, formatted);
    }

    public static void MoveNumericToAlphanumericEdited(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveNumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    // ══════════════════════════════════════════════════════════
    // MOVE: Alphanumeric → …
    // ══════════════════════════════════════════════════════════

    public static void MoveAlphanumericToAlphanumeric(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        // Left-justified, space-padded
        int copyLen = Math.Min(srcLength, dstLength);
        Array.Copy(srcArea, srcOffset, dstArea, dstOffset, copyLen);
        for (int i = copyLen; i < dstLength; i++)
            dstArea[dstOffset + i] = (byte)' ';
    }

    public static void MoveAlphanumericToAlphanumericEdited(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    // ══════════════════════════════════════════════════════════
    // MOVE: NumericEdited → …
    // ══════════════════════════════════════════════════════════

    public static void MoveNumericEditedToAlphanumeric(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        // Treat numeric-edited as alphanumeric for MOVE to alpha targets
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    public static void MoveNumericEditedToAlphanumericEdited(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    // ══════════════════════════════════════════════════════════
    // MOVE: AlphanumericEdited → …
    // ══════════════════════════════════════════════════════════

    public static void MoveAlphanumericEditedToAlphanumeric(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    public static void MoveAlphanumericEditedToAlphanumericEdited(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    // ══════════════════════════════════════════════════════════
    // MOVE: Literal helpers (called by emitter for MOVE "lit" TO field)
    // ══════════════════════════════════════════════════════════

    public static void MoveNumericLiteral(
        byte[] destArea, int destOffset, int destLength, PicDescriptor destPic,
        decimal literal, int roundingMode = 0)
    {
        decimal value = ApplyScalingAndRounding(literal, destPic, roundingMode);
        EncodeNumeric(destArea, destOffset, destLength, destPic, value);
    }

    // ══════════════════════════════════════════════════════════
    // MOVE: Legacy aliases (keep CIL emitter working during transition)
    // ══════════════════════════════════════════════════════════

    public static void MoveNumeric(
        byte[] destArea, int destOffset, int destLength, PicDescriptor destPic,
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        int roundingMode)
    {
        MoveNumericToNumeric(srcArea, srcOffset, srcLength, srcPic,
            destArea, destOffset, destLength, destPic, roundingMode);
    }

    // ══════════════════════════════════════════════════════════
    // MULTIPLY
    // ══════════════════════════════════════════════════════════

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

    // ══════════════════════════════════════════════════════════
    // ADD
    // ══════════════════════════════════════════════════════════

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

    // ══════════════════════════════════════════════════════════
    // SUBTRACT
    // ══════════════════════════════════════════════════════════

    public static void SubtractNumeric(
        byte[] destArea, int destOffset, int destLength, PicDescriptor destPic,
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        int roundingMode)
    {
        decimal dest = DecodeNumeric(destArea, destOffset, destLength, destPic);
        decimal src = DecodeNumeric(srcArea, srcOffset, srcLength, srcPic);
        decimal value = dest - src;
        value = ApplyScalingAndRounding(value, destPic, roundingMode);
        EncodeNumeric(destArea, destOffset, destLength, destPic, value);
    }

    public static void SubtractNumericLiteral(
        byte[] destArea, int destOffset, int destLength, PicDescriptor destPic,
        decimal literal, int roundingMode)
    {
        decimal dest = DecodeNumeric(destArea, destOffset, destLength, destPic);
        decimal value = dest - literal;
        value = ApplyScalingAndRounding(value, destPic, roundingMode);
        EncodeNumeric(destArea, destOffset, destLength, destPic, value);
    }

    // ══════════════════════════════════════════════════════════
    // DIVIDE
    // ══════════════════════════════════════════════════════════

    public static void DivideNumeric(
        byte[] destArea, int destOffset, int destLength, PicDescriptor destPic,
        byte[] leftArea, int leftOffset, int leftLength, PicDescriptor leftPic,
        byte[] rightArea, int rightOffset, int rightLength, PicDescriptor rightPic,
        int roundingMode)
    {
        decimal left = DecodeNumeric(leftArea, leftOffset, leftLength, leftPic);
        decimal right = DecodeNumeric(rightArea, rightOffset, rightLength, rightPic);
        if (right == 0m) return; // SIZE ERROR handling deferred
        decimal value = left / right;
        value = ApplyScalingAndRounding(value, destPic, roundingMode);
        EncodeNumeric(destArea, destOffset, destLength, destPic, value);
    }

    public static void DivideNumericLiteral(
        byte[] destArea, int destOffset, int destLength, PicDescriptor destPic,
        decimal literal,
        byte[] otherArea, int otherOffset, int otherLength, PicDescriptor otherPic,
        int roundingMode)
    {
        decimal other = DecodeNumeric(otherArea, otherOffset, otherLength, otherPic);
        if (literal == 0m) return; // SIZE ERROR handling deferred
        decimal value = other / literal;
        value = ApplyScalingAndRounding(value, destPic, roundingMode);
        EncodeNumeric(destArea, destOffset, destLength, destPic, value);
    }

    // ══════════════════════════════════════════════════════════
    // COMPARE: Numeric
    // ══════════════════════════════════════════════════════════

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

    // ══════════════════════════════════════════════════════════
    // COMPARE: Alphanumeric
    // ══════════════════════════════════════════════════════════

    /// <summary>Alphanumeric comparison using collating sequence. Returns -1, 0, or 1.</summary>
    public static int CompareAlphanumeric(
        byte[] leftArea, int leftOffset, int leftLength,
        byte[] rightArea, int rightOffset, int rightLength)
    {
        int maxLen = Math.Max(leftLength, rightLength);
        for (int i = 0; i < maxLen; i++)
        {
            byte lb = i < leftLength ? leftArea[leftOffset + i] : (byte)' ';
            byte rb = i < rightLength ? rightArea[rightOffset + i] : (byte)' ';
            if (lb < rb) return -1;
            if (lb > rb) return 1;
        }
        return 0;
    }

    /// <summary>National comparison. Returns -1, 0, or 1.</summary>
    public static int CompareNational(
        byte[] leftArea, int leftOffset, int leftLength,
        byte[] rightArea, int rightOffset, int rightLength)
    {
        // National uses 2-byte characters; for now treat same as alphanumeric
        return CompareAlphanumeric(leftArea, leftOffset, leftLength,
            rightArea, rightOffset, rightLength);
    }

    // ══════════════════════════════════════════════════════════
    // MOVE: NumericEdited → NumericEdited
    // ══════════════════════════════════════════════════════════

    public static void MoveNumericEditedToNumericEdited(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        // Edited-to-edited: raw byte copy (same as alpha-to-alpha)
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    // ══════════════════════════════════════════════════════════
    // MOVE: National stubs
    // ══════════════════════════════════════════════════════════

    public static void MoveNationalToNational(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    public static void MoveNationalToNationalEdited(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    public static void MoveNationalEditedToNationalEdited(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    public static void MoveNationalToAlphanumeric(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    public static void MoveNationalToAlphanumericEdited(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    public static void MoveNationalEditedToNational(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    public static void MoveNationalEditedToAlphanumeric(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    public static void MoveNationalEditedToAlphanumericEdited(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    // Also add missing cross-category MOVE stubs for Numeric → National
    public static void MoveNumericToNational(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveNumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    public static void MoveNumericToNationalEdited(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveNumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    public static void MoveNumericEditedToNational(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    public static void MoveNumericEditedToNationalEdited(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    public static void MoveAlphanumericToNational(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    public static void MoveAlphanumericToNationalEdited(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    public static void MoveAlphanumericEditedToNational(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    public static void MoveAlphanumericEditedToNationalEdited(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    // ══════════════════════════════════════════════════════════
    // Decode: bytes → decimal
    // ══════════════════════════════════════════════════════════

    public static decimal DecodeNumeric(
        byte[] area, int offset, int length, PicDescriptor pic)
    {
        return pic.Usage switch
        {
            UsageKind.Display => DecodeDisplay(area, offset, length, pic),
            UsageKind.Comp3 or UsageKind.PackedDecimal => DecodeComp3(area, offset, length),
            _ => DecodeDisplay(area, offset, length, pic)
        };
    }

    /// <summary>
    /// DISPLAY numeric decoding:
    /// - Field contains digits only (no decimal point stored)
    /// - Uses PicDescriptor.FractionDigits to restore the implied decimal
    /// - Handles leading '-' for signed fields
    /// </summary>
    private static decimal DecodeDisplay(byte[] area, int offset, int length, PicDescriptor pic)
    {
        var s = Encoding.ASCII.GetString(area, offset, length).Trim();
        if (string.IsNullOrEmpty(s)) return 0m;

        // Check for sign
        bool negative = false;
        if (s.Length > 0 && s[0] == '-')
        {
            negative = true;
            s = s[1..].Trim();
        }
        else if (s.Length > 0 && s[0] == '+')
        {
            s = s[1..].Trim();
        }

        if (string.IsNullOrEmpty(s)) return 0m;

        // Try to parse as integer (digits-only, no decimal point)
        if (long.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out long intVal))
        {
            // Apply implied decimal from FractionDigits
            decimal result = intVal;
            int scale = pic.FractionDigits;
            if (scale > 0)
                result /= Pow10(scale);
            return negative ? -result : result;
        }

        // Fallback: try decimal parse (handles legacy data with embedded decimal)
        if (decimal.TryParse(s,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out var fallback))
            return negative ? -fallback : fallback;

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

    // ══════════════════════════════════════════════════════════
    // Encode: decimal → bytes
    // ══════════════════════════════════════════════════════════

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
                EncodeDisplay(area, offset, length, pic, value);
                break;
        }
    }

    /// <summary>
    /// DISPLAY numeric encoding:
    /// - Implied decimal: no '.' stored; digits only
    /// - Uses PicDescriptor.FractionDigits to scale
    /// - Right-justified, zero-padded in field
    /// - Sign rendered as leading '-' when IsSigned and value &lt; 0
    /// </summary>
    private static void EncodeDisplay(
        byte[] area, int offset, int length, PicDescriptor pic, decimal value)
    {
        // Clear field to spaces
        for (int i = 0; i < length; i++)
            area[offset + i] = (byte)' ';

        bool isNegative = value < 0m;
        decimal absValue = Math.Abs(value);

        int scale = pic.FractionDigits;
        if (scale < 0) scale = 0;

        // Scale to integer: 320.48 with scale=2 → 32048
        decimal scaled = absValue * Pow10(scale);
        long intValue = (long)scaled;

        // Digits-only string
        string digits = intValue.ToString(CultureInfo.InvariantCulture);

        // Truncate from left if too long for field (SIZE ERROR should be handled separately)
        int availableLength = pic.IsSigned && isNegative ? length - 1 : length;
        if (digits.Length > availableLength)
            digits = digits.Substring(digits.Length - availableLength);

        // Right-justify digits in field
        int digitStart = pic.IsSigned && isNegative ? 1 : 0;
        int start = digitStart + (availableLength - digits.Length);
        for (int i = 0; i < digits.Length; i++)
            area[offset + start + i] = (byte)digits[i];

        // Zero-fill leading positions (COBOL numeric fields are zero-filled)
        for (int i = digitStart; i < start; i++)
            area[offset + i] = (byte)'0';

        // Sign handling
        if (pic.IsSigned && isNegative)
            area[offset] = (byte)'-';
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

    // ══════════════════════════════════════════════════════════
    // Scaling / rounding
    // ══════════════════════════════════════════════════════════

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

    // ══════════════════════════════════════════════════════════
    // Helpers
    // ══════════════════════════════════════════════════════════

    public static string FormatNumericForDisplay(decimal value, int fractionDigits)
    {
        if (fractionDigits > 0)
            return value.ToString("0." + new string('0', fractionDigits), CultureInfo.InvariantCulture);
        return ((long)value).ToString(CultureInfo.InvariantCulture);
    }

    private static decimal Pow10(int scale)
    {
        decimal result = 1m;
        for (int i = 0; i < scale; i++)
            result *= 10m;
        return result;
    }

    private static void MoveStringToBytes(byte[] area, int offset, int length, string value)
    {
        int copyLen = Math.Min(value.Length, length);
        for (int i = 0; i < length; i++)
            area[offset + i] = i < copyLen ? (byte)value[i] : (byte)' ';
    }
}
