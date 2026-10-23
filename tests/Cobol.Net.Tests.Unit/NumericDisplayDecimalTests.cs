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

    // ── ACCEPT reads the form back (kb/Work PB3513; ISO §14.9.1.4 GR1; DOC-A.1-1) ─────────────────────────────────────
    // Every expected value is GnuCOBOL 3.2.0's own result for the same declaration and the same typed text
    // (cobc -x -std=default, measured 2026-10-23 over 1,300 receiver and text pairs; the survey is in DOC-A.1-1), as
    // an UNSCALED value: the PICTURE's digits with the scale implied. A character-form item is handed its own
    // window (its digit count plus a SIGN SEPARATE position), a binary or packed one the whole record.

    private static void Reads(Int128 unscaled, string text, NumProfile p, bool comma = false) =>
        Assert.Equal(unscaled, CobolNum.ParseDeviceDecimal(text, p, comma));

    [Fact]
    public void Accept_reads_the_decimal_point_and_the_leading_sign()
    {
        NumProfile pic52 = Zoned(7, 2), spic52 = Zoned(7, 2, true);   // PIC 9(5)V99 and PIC S9(5)V99: window 7
        Reads(1250, "00012.5", pic52);                  // what DISPLAY wrote ("00012.50"), cut to the 7-character window
        Reads(1250, "12.5   ", pic52);
        Reads(500, "5.     ", pic52);
        Reads(50, ".5     ", pic52);
        Reads(12500, "12 5   ", pic52);                 // a space inside the number is skipped: 125, then the scale
        Reads(1256, "12.567 ", pic52);                  // the fraction keeps its first two digits: truncated, never rounded
        Reads(3456700, "1234567", pic52);               // no point: the HIGH-order digits past the five integer positions are lost
        Reads(1250, "-12.5  ", pic52);                  // an unsigned item takes the magnitude
        Reads(-1250, "-12.5  ", spic52);
        Reads(-1250, "  -12.5", spic52);
        Reads(-1250, "- 12.5 ", spic52);                // spaces after the sign are skipped
        Reads(500, "+ 5    ", spic52);
        Reads(-1200, "-00012.", spic52);                // DISPLAY's "-00012.50" cut to its 7-character window: the .50 is lost
        Reads(0, "-0     ", spic52);                    // there is no negative zero in a value
    }

    [Fact]
    public void Accept_converts_a_text_that_is_not_a_number_to_zero_and_sets_nothing()
    {
        NumProfile spic52 = Zoned(7, 2, true);
        Reads(0, "12.5-  ", spic52);                    // a TRAILING sign is not part of the form, not even for SIGN TRAILING SEPARATE
        Reads(0, "12.5-  ", Zoned(6, 2, true, NumericSign.TrailingSeparate));
        Reads(0, "--5    ", spic52);
        Reads(0, "+-5    ", spic52);
        Reads(0, "1.2.3  ", spic52);                    // a second decimal point
        Reads(0, "12x    ", spic52);                    // a letter before the last digit position is filled
        Reads(0, "12.5CR ", spic52);                    // ... and the "CR" of 12.5CR is such a letter: one fraction digit still wanted
        Reads(0, "abc    ", spic52);
        Reads(0, "       ", spic52);
        Reads(0, "1e2    ", spic52);
        Reads(0, ".      ", spic52);
    }

    /// <summary>A character-form item stops reading when its last digit position is filled, so what follows is never
    /// examined; a binary or packed item reads the whole text, so the same trailing text is invalid there.</summary>
    [Fact]
    public void Accept_stops_a_character_item_at_its_last_position_and_reads_a_binary_item_whole()
    {
        NumProfile pic52 = Zoned(7, 2);
        Reads(1250, "12.50CR", pic52);                  // both fraction positions filled: "CR" is never read
        Reads(1256, "12.567CR", pic52);
        Reads(12, "12x", Zoned(3, 0, true));            // PIC S9(3) window 3: "12x" fills two digits and the zero fill is the third
        Reads(0, "1x2", Zoned(3, 0, true));
        Reads(125, "12.5CR", Zoned(7, 1));              // PIC 9(6)V9: one fraction position, filled by the 5
        Reads(0, "12.50CR", Binary(5, 2, false, NumericTruncation.DigitCount, 4));   // PIC 9(3)V99 COMP reads the whole record
        Reads(0, "12x", Packed(7, 2, true));
        Reads(1250, "12.50", Binary(5, 2, false, NumericTruncation.DigitCount, 4));
        Reads(-1250, "-00012.50", Binary(5, 2, true, NumericTruncation.DigitCount, 4));   // not cut at 5 characters
    }

    [Fact]
    public void Accept_keeps_the_low_order_digits_an_item_can_hold()
    {
        Reads(89000, "1234567890", Binary(5, 2, true, NumericTruncation.DigitCount, 4));    // PIC S9(3)V99 COMP: 890.00
        Reads(6789000, "1234567890", Packed(7, 2, true));                                    // PIC S9(5)V99 COMP-3: 67890.00
        Reads(123, "123", Zoned(3, 0));
        Reads(1, "123", Zoned(3, -2));                  // PIC 9(3)PP given 123: its digits are the hundreds, so 100 holds 1
    }

    [Fact]
    public void Accept_reads_a_p_scaled_item_by_value()
    {
        NumProfile leadingP = Packed(3, 5, false);       // PIC PP9(3) COMP-3
        Reads(123, ".00123", leadingP);
        Reads(123, "0.001239", leadingP);               // truncated at the fifth fraction digit
        Reads(0, "12", leadingP);                       // the integer part is above every position
        Reads(1, ".0000123", leadingP);                 // .00001: only the digits that fall inside the three positions
        Reads(0, ".00", Zoned(3, 5));                   // a DISPLAY one is cut to its 3-character window first
        Reads(-123, "-.00123", Packed(3, 5, true));
    }

    [Fact]
    public void Accept_reads_the_separator_the_program_declares()
    {
        NumProfile pic52 = Zoned(7, 2);
        Reads(1250, "12,5   ", pic52, comma: true);      // DECIMAL-POINT IS COMMA: the comma is the point ...
        Reads(12500, "12.5   ", pic52, comma: true);     // ... and the period is the grouping separator, skipped
        Reads(0, "1,5,7  ", pic52, comma: true);
        Reads(12300, "1.2.3  ", pic52, comma: true);
        Reads(1500, "1,5    ", pic52);                   // period mode: the comma is skipped, so 15
    }

    [Fact]
    public void Accept_wraps_a_binary_capacity_item_to_its_container()
    {
        NumProfile comp5 = Binary(4, 0, true, NumericTruncation.BinaryCapacity, 2);          // PIC S9(4) COMP-5
        Reads(722, "1234567890", comp5);                 // 1234567890 mod 65536: the container keeps every digit, then wraps
        Reads(-32768, "32768", comp5);
        Reads(-1, "-1", comp5);
        Reads(0, "4294967296", Binary(9, 0, false, NumericTruncation.BinaryCapacity, 4));     // PIC 9(9) COMP-5
        Reads(3728274254, "12345678901234567890123456789012345678", Binary(9, 0, false, NumericTruncation.BinaryCapacity, 4));
        Reads(30000, "300", Binary(3, 2, true, NumericTruncation.BinaryCapacity, 2));        // PIC S9V99 COMP-5: its two-byte container holds 300.00
    }

    /// <summary>What DISPLAY writes, a whole-text read gives back: the inverse the option's name promises, for every item whose
    /// decimal form carries its sign leading (SIGN TRAILING SEPARATE's trailing sign is not part of the readable form, and a
    /// SIGNED ACCEPT window cuts DISPLAY's text at the item's own size, both documented in DOC-A.1-1).</summary>
    [Fact]
    public void Accept_inverts_the_display_form_over_the_whole_text()
    {
        (NumProfile p, Int128 v)[] rows =
        [
            (Zoned(7, 2), 1250), (Zoned(7, 2, true), 1250), (Zoned(7, 2, true), -1250), (Zoned(3, 0, true), -5),
            (Zoned(5, 2, true, NumericSign.LeadingSeparate), -150), (Zoned(3, -2), 12), (Zoned(3, 5, true), -123),
            (Binary(5, 2, true, NumericTruncation.DigitCount, 4), -150), (Packed(7, 2, false), 1250),
            (Binary(4, 0, true, NumericTruncation.BinaryCapacity, 2), -300),
        ];
        foreach (var (p, v) in rows)
            foreach (bool comma in new[] { false, true })
                Assert.Equal(v, CobolNum.ParseDeviceDecimal(CobolNum.DisplayDecimal(v, p, comma), p, comma));
    }
}
