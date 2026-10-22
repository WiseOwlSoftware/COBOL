// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE DECIMAL DEVICE FORM OF A NUMERIC ITEM (kb/Work PB3505, owner decision PB3467; ISO §14.9.11.4 GR1, Annex A.1 item
/// 56 — docs/CONFORMANCE.md DOC-A.1-56). Every expected string is the form the owner chose, GnuCOBOL 3.2.0's
/// default configuration, as its <c>cobc -x -std=default</c> printed it for the same declaration (the survey is in the
/// DOC-A.1-56 determination), never read back from <c>CobolNum.DisplayDecimal</c>. One composition serves the native
/// carriers and the character image, so each row is asserted on both.
/// </summary>
public sealed class NumericDisplayDecimalTests
{
    private static NumProfile Zoned(int digits, int scale, bool signed = false, NumericSign sign = NumericSign.TrailingOverpunch) => new()
    {
        Digits = digits, FractionDigits = scale, Signed = signed, SignKind = sign,
        Truncation = NumericTruncation.DigitCount, ByteForm = NumericByteForm.Zoned,
    };

    private static NumProfile Binary(int digits, int scale, bool signed, NumericTruncation truncation, int bytes) => new()
    {
        Digits = digits, FractionDigits = scale, Signed = signed, SignKind = NumericSign.BinaryMinus,
        Truncation = truncation, ByteForm = NumericByteForm.Binary, StorageLength = bytes,
    };

    private static NumProfile Packed(int digits, int scale, bool signed) => new()
    {
        Digits = digits, FractionDigits = scale, Signed = signed, SignKind = NumericSign.BinaryMinus,
        Truncation = NumericTruncation.PackedDecimal, ByteForm = NumericByteForm.Packed, StorageLength = digits / 2 + 1,
    };

    /// <summary>The native carrier of a value and the character image the same item would store, asserted equal.</summary>
    private static void Shows(string expected, Int128 unscaled, NumProfile p, bool comma = false)
    {
        Assert.Equal(expected, CobolNum.DisplayDecimal(unscaled, p, comma));
        // A BINARY-CAPACITY item is never carried as a digit image of its PICTURE (a window decodes to the native value).
        if (p.Truncation != NumericTruncation.BinaryCapacity)
            Assert.Equal(expected, CobolNum.DisplayDecimal(CobolNum.FormatDisplay(unscaled, p), p, comma));
    }

    [Fact]
    public void Unsigned_scaled_item_shows_the_decimal_point_at_the_scale()
    {
        Shows("00012.50", 1250, Zoned(7, 2));               // PIC 9(5)V99 = 12.5
        Shows(".500", 500, Zoned(3, 3));                    // PIC V9(3) = .5
        Shows("0.500", 500, Zoned(4, 3));                   // PIC 9V9(3) = .5
        Shows("0.00000001", 1, Zoned(9, 8));                // PIC 9V9(8)
        Shows("007", 7, Zoned(3, 0));                       // PIC 9(3): no point, no sign
    }

    [Fact]
    public void Signed_item_shows_a_leading_sign_character_always_and_zero_is_positive()
    {
        Shows("+00012.50", 1250, Zoned(7, 2, true));        // PIC S9(5)V99 = +12.5
        Shows("-00012.50", -1250, Zoned(7, 2, true));
        Shows("+005", 5, Zoned(3, 0, true));                // PIC S9(3) = +5
        Shows("-005", -5, Zoned(3, 0, true));
        Shows("+000", 0, Zoned(3, 0, true));
        Shows("+000.00", 0, Zoned(5, 2, true));
        Shows("-001.50", -150, Zoned(5, 2, true, NumericSign.LeadingOverpunch));   // SIGN LEADING (overpunch): leading sign
        Shows("-001.50", -150, Zoned(5, 2, true, NumericSign.LeadingSeparate));
        Shows("+001.50", 150, Zoned(5, 2, true, NumericSign.LeadingSeparate));
    }

    [Fact]
    public void Sign_trailing_separate_shows_its_sign_where_the_program_put_it()
    {
        Shows("001.50-", -150, Zoned(5, 2, true, NumericSign.TrailingSeparate));
        Shows("001.50+", 150, Zoned(5, 2, true, NumericSign.TrailingSeparate));
    }

    [Fact]
    public void P_scaling_writes_the_implied_zeros()
    {
        Shows("01200", 12, Zoned(3, -2));                   // PIC 9(3)PP = 1200 stores 012
        Shows("-01200", -12, Zoned(3, -2, true));           // PIC S9(3)PP
        Shows(".00123", 123, Zoned(3, 5));                  // PIC PP9(3) = .00123
        Shows("-.00123", -123, Zoned(3, 5, true));          // PIC SPP9(3)
    }

    [Fact]
    public void Decimal_point_is_comma_uses_the_programs_separator()
    {
        Shows("-001,50", -150, Zoned(5, 2, true), comma: true);
        Shows("01,50", 150, Binary(4, 2, false, NumericTruncation.DigitCount, 2), comma: true);
        Shows(",500", 500, Zoned(3, 3), comma: true);
    }

    [Fact]
    public void Binary_and_packed_items_show_the_same_form()
    {
        Shows("001.50", 150, Binary(5, 2, false, NumericTruncation.DigitCount, 4));        // PIC 9(3)V99 COMP
        Shows("-001.50", -150, Binary(5, 2, true, NumericTruncation.DigitCount, 4));       // PIC S9(3)V99 COMP
        Shows("+001.50", 150, Binary(5, 2, true, NumericTruncation.DigitCount, 4));
        Shows("001.50", 150, Packed(5, 2, false));                                         // PIC 9(3)V99 COMP-3
        Shows("-001.50", -150, Packed(5, 2, true));
        Shows("-123456789012345678.123456789", Int128.Parse("-123456789012345678123456789"), Packed(27, 9, true));
    }

    /// <summary>A BINARY-CAPACITY item holds any value of its container (kb/Work R10), so it shows the container's
    /// digits - 3, 5, 10 and 20 for 1, 2, 4 and 8 bytes, GnuCOBOL 3.2.0's widths - and never a value cut to its PICTURE.
    /// The scaled COMP-5 shows its point (GnuCOBOL prints the unscaled integer there, a non-ISO anomaly DOC-A.1-56 records).</summary>
    [Fact]
    public void Binary_capacity_item_shows_its_container_not_its_picture()
    {
        Shows("007", 7, Binary(2, 0, false, NumericTruncation.BinaryCapacity, 1));                 // BINARY-CHAR UNSIGNED
        Shows("-005", -5, Binary(3, 0, true, NumericTruncation.BinaryCapacity, 1));                // BINARY-CHAR
        Shows("00007", 7, Binary(3, 0, false, NumericTruncation.BinaryCapacity, 2));               // PIC 9(3) COMP-5
        Shows("12345", 12345, Binary(3, 0, false, NumericTruncation.BinaryCapacity, 2));           // beyond its PICTURE
        Shows("-00300", -300, Binary(5, 0, true, NumericTruncation.BinaryCapacity, 2));            // BINARY-SHORT
        Shows("-0000070000", -70000, Binary(10, 0, true, NumericTruncation.BinaryCapacity, 4));    // BINARY-LONG
        Shows("-00000000005000000000", -5000000000L, Binary(18, 0, true, NumericTruncation.BinaryCapacity, 8));   // BINARY-DOUBLE
        Shows("+0300.0", 3000, Binary(3, 1, true, NumericTruncation.BinaryCapacity, 2));           // PIC S9(2)V9 COMP-5 = 300
        Assert.Equal("18446744073709551615",
            CobolNum.DisplayDecimalU(18446744073709551615UL, Binary(18, 0, false, NumericTruncation.BinaryCapacity, 8), false));
    }

    /// <summary>Content a group MOVE deposits in a numeric item's image is carried through as stored (§14.6.13.2: undefined,
    /// and a decode-and-reformat round trip would turn it into zeros); only the sign position is read.</summary>
    [Fact]
    public void An_image_carries_its_digit_positions_as_stored_and_decodes_only_the_sign()
    {
        NumProfile unsigned = Zoned(5, 2);
        Assert.Equal("AB1,CD", CobolNum.DisplayDecimal("AB1CD", unsigned, true));
        Assert.Equal("  1.  ", CobolNum.DisplayDecimal("  1  ", unsigned, false));
        // Overpunched signs: IBM '}' is a negative zero, '{' a positive zero; SIGN LEADING reads the first position.
        Assert.Equal("-0123.0", CobolNum.DisplayDecimal("0123}", Zoned(5, 1, true), false));
        Assert.Equal("+0123.0", CobolNum.DisplayDecimal("0123{", Zoned(5, 1, true), false));
        Assert.Equal("-0123.5", CobolNum.DisplayDecimal("}1235", Zoned(5, 1, true, NumericSign.LeadingOverpunch), false));
        Assert.Equal("-0123.5", CobolNum.DisplayDecimal("-01235", Zoned(5, 1, true, NumericSign.LeadingSeparate), false));
        Assert.Equal("0123.5-", CobolNum.DisplayDecimal("01235-", Zoned(5, 1, true, NumericSign.TrailingSeparate), false));
        Assert.Equal("-123.45", CobolNum.DisplayDecimal("-12345", Binary(5, 2, true, NumericTruncation.DigitCount, 4), false));
        Assert.Equal("+123.45", CobolNum.DisplayDecimal("12345", Binary(5, 2, true, NumericTruncation.DigitCount, 4), false));
        // The ASCII convention: a negative digit is digit + 0x40, a positive one is the digit.
        NumProfile ascii = Zoned(4, 0, true) with { SignEncoding = SignEncoding.Ascii };
        Assert.Equal("-1234", CobolNum.DisplayDecimal("123t", ascii, false));
        Assert.Equal("+1234", CobolNum.DisplayDecimal("1234", ascii, false));
    }
}
