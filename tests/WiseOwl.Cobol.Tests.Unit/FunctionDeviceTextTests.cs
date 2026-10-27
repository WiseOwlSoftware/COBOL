// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ WHAT DISPLAY WRITES FOR A NUMERIC FUNCTION'S RETURNED VALUE (kb/Work PB3514; ISO/IEC 1989:2023 §15.4 puts the value in
/// "a temporary elementary data item", §15.4.1 leaves its characteristics to the implementor under native arithmetic, and
/// §14.9.11.4 GR1 leaves the device conversion to the implementor; docs/CONFORMANCE.md DOC-A.1-92). The standard is silent on
/// the temporary's description, so CLAUDE.md rule 1's precedence adopts GnuCOBOL 3.2.0's: <c>cob_alloc_field</c> derives it from the
/// value (<c>CobolNum.DeviceFunctionText</c>), and ABS, MAX, MIN, MEDIAN and MEAN of one argument return a COPY of an argument's
/// item (<c>FunctionValueField</c>). The expected strings below are what <c>cobc -x -std=default</c> 3.2.0 printed for the same
/// values (probe matrix in the PB3514 report), EXCEPT the rows marked NOT COPIED, where GnuCOBOL shows a figure the value does not have.
/// The last section pins the same temporary's LENGTH (kb/Work PB3951).
/// </summary>
public sealed class FunctionDeviceTextTests
{
    private static CobolDec Dec(string text)
    {
        bool negative = text.StartsWith('-');
        string body = negative ? text[1..] : text;
        int point = body.IndexOf('.');
        string digits = body.Replace(".", "");
        int scale = point < 0 ? 0 : body.Length - point - 1;
        Int128 sig = Int128.Parse(digits);
        return CobolDec.From(negative ? -sig : sig, scale);
    }

    private static NumProfile Item(int digits, int scale, bool signed, NumericSign sign = NumericSign.TrailingOverpunch) => new()
    {
        Digits = digits,
        FractionDigits = scale,
        Signed = signed,
        SignKind = sign,
        Truncation = NumericTruncation.DigitCount,
        ByteForm = NumericByteForm.Zoned,
    };

    /// <summary>The value alone describes the temporary: trailing fraction zeros dropped, 9 digit positions when the magnitude fits
    /// 31 bits (32 unsigned) and the scale is under 10, 20 when it fits 63 (64) and the scale is under 19, else the value's own
    /// digits; signed exactly when negative.</summary>
    [Theory]
    [InlineData("12.50", "00000012.5")]           // FUNCTION NUMVAL("12.50")
    [InlineData("0.00", "000000000")]             // SUM(+12.5 -12.5)
    [InlineData("-0.5", "-00000000.5")]
    [InlineData("42", "000000042")]
    [InlineData("-42", "-000000042")]
    [InlineData("100", "000000100")]
    [InlineData("-13", "-000000013")]             // FUNCTION INTEGER(-12.5)
    [InlineData("4294967296", "00000000004294967296")]            // 33 bits: the 8-byte item, 20 positions
    [InlineData("-2147483648", "-00000000002147483648")]          // 32 bits negative: past the 4-byte item
    [InlineData("0.9999999999", "0000000000.9999999999")]         // scale 10: the 8-byte item
    [InlineData("0.123456789012345678", "00.123456789012345678")]  // scale 18: still the 8-byte item
    [InlineData("0.1234567890123456789", ".1234567890123456789")]  // scale 19: a DISPLAY item of exactly its digits
    [InlineData("0.000000001", ".000000001")]                      // positions = scale: no integer digit, no zero before the point
    [InlineData("12345678.123456789", "00012345678.123456789")]
    [InlineData("18446744073709551616", "18446744073709551616")]    // 65 bits: DISPLAY, exactly 20 digits
    [InlineData("123456789012345678901234567890", "123456789012345678901234567890")]
    public void TheValueDescribesTheTemporary(string value, string expected) =>
        Assert.Equal(expected, CobolNum.DeviceFunctionText(Dec(value), commaMode: false));

    /// <summary>NOT COPIED: GnuCOBOL's 4-byte item has 9 digit positions, so it shows a 10-digit value without its high-order digit
    /// (<c>NUMVAL("2147483647")</c> is <c>147483647</c> there), and a DISPLAY item's digit count there is a GMP size estimate that may exceed
    /// the true count by one (a leading zero). The digit count here is <c>max(positions, digits)</c>: no digit is cut and none invented.</summary>
    [Theory]
    [InlineData("2147483647", "2147483647")]
    [InlineData("4294967295", "4294967295")]
    [InlineData("-9223372036854775808", "-9223372036854775808")]
    [InlineData("999999999999999999999", "999999999999999999999")]
    public void NoDigitIsCutAndNoneIsInvented(string value, string expected) =>
        Assert.Equal(expected, CobolNum.DeviceFunctionText(Dec(value), commaMode: false));

    /// <summary>The decimal separator is the unit's (§12.3.7.4 GR14 a); a scaled integer carrier reaches the same text.</summary>
    [Fact]
    public void TheSeparatorIsTheUnits_AndTheScaledCarrierReachesTheSameRule()
    {
        Assert.Equal("00000012,5", CobolNum.DeviceFunctionText(Dec("12.50"), commaMode: true));
        Assert.Equal("-00000012,5", CobolNum.DeviceFunctionText(-1250, 2, commaMode: true));
        Assert.Equal("00000012.5", CobolNum.DeviceFunctionText(1250, 2, commaMode: false));
        Assert.Equal("5000000000000000000000000000000", CobolNum.DeviceFunctionText(new CobolDec(5, 30), commaMode: false));
    }

    /// <summary>A copy of an argument's item for the rows that test the TEXT: the lengths are not under test, so they are the digits.</summary>
    private static FunctionValueField Copy(string value, NumProfile item) => FunctionValueField.OfItem(Dec(value), item, item.Digits, item.Digits);

    /// <summary>ABS, MAX, MIN and the odd MEDIAN return a COPY of an argument's item: the text is what DISPLAY of that item writes, so
    /// <c>FUNCTION MAX(U S)</c> is U's <c>00012.50</c> (unsigned, no sign) and a signed item's positive value shows its <c>+</c>.</summary>
    [Fact]
    public void ASelectionIsACopyOfTheArgumentsItem()
    {
        NumProfile u = Item(7, 2, signed: false);          // PIC 9(5)V99
        NumProfile s = Item(7, 2, signed: true);           // PIC S9(5)V99
        NumProfile i = Item(3, 0, signed: false);          // PIC 9(3)
        NumProfile si = Item(3, 0, signed: true);          // PIC S9(3)
        var fu = Copy("12.50", u);
        var fs = Copy("-12.50", s);
        Assert.Equal("00012.50", FunctionValueField.Max(fu, fs).DeviceText(false));
        Assert.Equal("-00012.50", FunctionValueField.Min(fu, fs).DeviceText(false));
        Assert.Equal("+00012.50", fs.Abs().DeviceText(false));                           // ABS keeps the sign position
        Assert.Equal("00012.50", fu.Abs().DeviceText(false));
        Assert.Equal("007", FunctionValueField.Max(Copy("7", i), Copy("-14", si)).DeviceText(false));
        Assert.Equal("-014", FunctionValueField.Min(Copy("7", i), Copy("-14", si)).DeviceText(false));
    }

    /// <summary>A tie keeps the FIRST argument (§15.59.4 r1 / §15.63.4 r1 take the leftmost of equal values), so its item is the temporary.</summary>
    [Fact]
    public void ATieKeepsTheFirstArgumentsItem()
    {
        var unsigned = Copy("12.50", Item(7, 2, signed: false));
        var signed = Copy("12.50", Item(7, 2, signed: true));
        Assert.Equal("00012.50", FunctionValueField.Max(unsigned, signed).DeviceText(false));
        Assert.Equal("+00012.50", FunctionValueField.Min(signed, unsigned).DeviceText(false));
    }

    /// <summary>A literal argument is a field with a digit position for every digit written, the fraction digits written, and a sign
    /// position when a sign was written; ABS(-12.50) is a signed field holding the magnitude.</summary>
    [Fact]
    public void ALiteralArgumentIsAFieldOfItsOwnDigits()
    {
        Assert.Equal("+12.50", FunctionValueField.OfLiteral(Dec("-12.50"), 4, 2, signed: true).Abs().DeviceText(false));
        Assert.Equal("12.50", FunctionValueField.OfLiteral(Dec("12.50"), 4, 2, signed: false).Abs().DeviceText(false));
        Assert.Equal("005", FunctionValueField.OfLiteral(Dec("5"), 3, 0, signed: false).DeviceText(false));
        Assert.Equal("+5", FunctionValueField.OfLiteral(Dec("5"), 1, 0, signed: true).DeviceText(false));
        Assert.Equal("12,50", FunctionValueField.OfLiteral(Dec("12.50"), 4, 2, signed: false).DeviceText(true));
    }

    /// <summary>An odd MEDIAN is the middle argument's item (equal values keep their written order); an even one is the mean of the two
    /// middle values, which no item holds, so the value describes it.</summary>
    [Fact]
    public void MedianIsTheMiddleItemOrTheMeanOfTheMiddleTwo()
    {
        var a = Copy("12.5", Item(7, 2, signed: false));
        var b = Copy("-12.5", Item(7, 2, signed: true));
        var c = Copy("2.5", Item(2, 1, signed: false));
        Assert.Equal("2.5", FunctionValueField.Median(a, b, c).DeviceText(false));              // middle of -12.5 2.5 12.5: c's PIC 9V9
        Assert.Equal("-00012.50", FunctionValueField.Median(b).DeviceText(false));                // one argument: that argument's item
        var d = Copy("7", Item(3, 0, signed: false));
        Assert.Equal("0000004.75", FunctionValueField.Median(a, b, c, d).DeviceText(false));    // (2.5 + 7) / 2: the value's temporary
    }

    /// <summary>The temporary of an arithmetic expression or a nested function is described when it is MADE, so ABS of a negative one is a
    /// signed item holding the magnitude: <c>ABS(INTEGER(-12.5))</c> is <c>+000000013</c> and a non-negative value's stays unsigned.</summary>
    [Fact]
    public void AValueTemporaryKeepsTheSignPositionItWasMadeWith()
    {
        Assert.Equal("+000000013", FunctionValueField.OfValue(Dec("-13")).Abs().DeviceText(false));
        Assert.Equal("000000013", FunctionValueField.OfValue(Dec("13")).Abs().DeviceText(false));
        Assert.Equal("+00000011.5", FunctionValueField.OfValue(Dec("-11.5")).Abs().DeviceText(false));
    }

    /// <summary>A binary64 value keeps the image a floating-point item has, and orders by its exact value; ABS drops the sign of the image.</summary>
    [Fact]
    public void ABinary64KeepsItsImage()
    {
        Assert.Equal("2.5", FunctionValueField.OfFloat(2.5).DeviceText(false));
        Assert.Equal("0.1", FunctionValueField.OfFloat(-0.1).Abs().DeviceText(false));
        Assert.Equal("0000000005", FunctionValueField.Max(FunctionValueField.OfFloat(2.5),
            Copy("5", new NumProfile { Digits = 10, FractionDigits = 0, Signed = false, Truncation = NumericTruncation.BinaryCapacity, ByteForm = NumericByteForm.Binary, StorageLength = 4 })).DeviceText(false));
    }

    /// <summary>An ALL subscript over no occurrence leaves a selecting function with no argument (§15.3): the same EC-ARGUMENT-FUNCTION the
    /// value bodies raise, never an index fault.</summary>
    [Fact]
    public void ASelectionOverNoArgumentsIsAnArgumentError()
    {
        foreach (Func<FunctionValueField> call in new Func<FunctionValueField>[] { () => FunctionValueField.Max(), () => FunctionValueField.Min(), () => FunctionValueField.Median() })
            Assert.Contains("EMPTY argument list", Assert.ThrowsAny<Exception>(() => _ = call()).Message, StringComparison.Ordinal);
    }

    /// <summary>DATE-OF-INTEGER and DAY-OF-INTEGER return an unsigned DISPLAY integer of eight and seven digits, zero-filled.</summary>
    [Fact]
    public void AFixedDigitTemporaryIsZeroFilled()
    {
        Assert.Equal("16010101", FunctionValueField.OfDigits(Dec("16010101"), 8).DeviceText(false));
        Assert.Equal("00000000", FunctionValueField.OfDigits(Dec("0"), 8).DeviceText(false));
        Assert.Equal("0000000", FunctionValueField.OfDigits(Dec("0"), 7).DeviceText(false));
    }

    // ── The temporary's LENGTH (kb/Work PB3951): FUNCTION LENGTH / BYTE-LENGTH of a numeric function measure the item DISPLAY shows ────────
    // ISO §8.4.3.2.1 + §15.4 make the function-identifier that temporary data item, §15.50.4 r3 / §15.14.4 r1 measure it, and §15.4.1 leaves
    // its representation to the implementor, so GnuCOBOL 3.2.0's sizes are the answers (the probe matrix of the PB3951 report; the rows
    // marked NOT COPIED are the same two deliberate departures as DISPLAY's).

    private static NumProfile Packed(int digits, int scale) => new()
    {
        Digits = digits, FractionDigits = scale, Signed = true, Truncation = NumericTruncation.PackedDecimal,
        ByteForm = NumericByteForm.Packed, StorageLength = digits / 2 + 1,
    };

    /// <summary>A copy of an argument's item is that item's bytes and positions: the compiler states both from the item (the one reading
    /// FUNCTION LENGTH(field) and BYTE-LENGTH(field) use), and a copy keeps them through ABS.</summary>
    [Fact]
    public void ACopyOfAnArgumentIsTheLengthOfItsItem()
    {
        NumProfile zoned = Item(7, 2, signed: true);
        Assert.Equal(7, FunctionValueField.OfItem(Dec("-12.5"), zoned, 7, 7).Abs().ByteLength);                                              // S9(5)V99
        Assert.Equal(8, FunctionValueField.OfItem(Dec("-12.5"), Item(7, 2, signed: true, NumericSign.TrailingSeparate), 8, 8).ByteLength);    // SIGN TRAILING SEPARATE
        Assert.Equal(7, FunctionValueField.OfItem(Dec("12.5"), Item(7, 2, signed: false), 7, 7).ByteLength);                                 // unsigned: no sign position
        Assert.Equal(5, FunctionValueField.OfItem(Dec("5.25"), Packed(9, 2), 5, 5).ByteLength);                                             // S9(7)V99 COMP-3
        Assert.Equal(2, FunctionValueField.OfItem(Dec("-3"), new NumProfile { Digits = 4, FractionDigits = 0, Signed = true, Truncation = NumericTruncation.BinaryCapacity, ByteForm = NumericByteForm.Binary, StorageLength = 2 }, 2, 2).ByteLength);
        Assert.Equal(4, FunctionValueField.OfFloatItem(Dec("-1.5"), new NumProfile { Digits = 0, FractionDigits = 0, Signed = true, Truncation = NumericTruncation.BinaryCapacity, ByteForm = NumericByteForm.Ieee32, StorageLength = 4 }, "-1.5", 4).Abs().ByteLength);
    }

    /// <summary>R1-1 of the PB3951 review: a USAGE NATIONAL argument's copy is five national POSITIONS and ten BYTES (PIC S9(5) NATIONAL), a
    /// SIGN SEPARATE one six and twelve; LENGTH reads the positions (§15.50.4 r2) and BYTE-LENGTH the bytes (§15.14.4 r1), through ABS too.</summary>
    [Fact]
    public void ACopyOfANationalArgumentHasPositionsAndBytes()
    {
        NumProfile national = Item(5, 0, signed: true);
        var copy = FunctionValueField.OfItem(Dec("-12"), national, 5, 10).Abs();
        Assert.Equal(5, copy.Length);
        Assert.Equal(10, copy.ByteLength);
        var separate = FunctionValueField.OfItem(Dec("-12"), Item(5, 0, signed: true, NumericSign.LeadingSeparate), 6, 12);
        Assert.Equal(6, separate.Length);
        Assert.Equal(12, separate.ByteLength);
        // every other temporary has a position per byte
        Assert.Equal(FunctionValueField.OfValue(Dec("7")).ByteLength, FunctionValueField.OfValue(Dec("7")).Length);
        Assert.Equal(FunctionValueField.OfFloat(2.5).ByteLength, FunctionValueField.OfFloat(2.5).Length);
        Assert.Equal(FunctionValueField.OfLiteral(Dec("-5"), 1, 0, signed: true).ByteLength, FunctionValueField.OfLiteral(Dec("-5"), 1, 0, signed: true).Length);
    }

    /// <summary>The selection returns the winner's temporary, so its length is the winner's, not the loser's or the largest.</summary>
    [Fact]
    public void ASelectionIsTheWinnersBytes()
    {
        FunctionValueField packed = FunctionValueField.OfItem(Dec("5.25"), Packed(9, 2), 5, 5);
        FunctionValueField display = FunctionValueField.OfItem(Dec("-12.5"), Item(7, 2, signed: true), 7, 7);
        Assert.Equal(5, FunctionValueField.Max(packed, display).ByteLength);
        Assert.Equal(7, FunctionValueField.Min(packed, display).ByteLength);
        Assert.Equal(7, FunctionValueField.Max(display, FunctionValueField.OfItem(Dec("-12.5"), Packed(9, 2), 5, 5)).ByteLength);   // equal: the leftmost wins (§15.59.4 2)
        Assert.Equal(5, FunctionValueField.Median(display, packed, FunctionValueField.OfLiteral(Dec("9"), 1, 0, signed: false)).ByteLength);
    }

    /// <summary>A literal's field has a position for every digit written and one for a sign written; DATE-OF-INTEGER and DAY-OF-INTEGER are
    /// unsigned DISPLAY integers of eight and seven digits.</summary>
    [Fact]
    public void ALiteralAndAFixedWidthTemporaryAreTheirDigits()
    {
        Assert.Equal(5, FunctionValueField.OfLiteral(Dec("-12.5"), 4, 2, signed: true).ByteLength);    // -12.50
        Assert.Equal(4, FunctionValueField.OfLiteral(Dec("12.5"), 4, 2, signed: false).ByteLength);    // 12.50
        Assert.Equal(4, FunctionValueField.OfLiteral(Dec("7"), 3, 0, signed: true).ByteLength);        // +007
        Assert.Equal(8, FunctionValueField.OfDigits(Dec("16010101"), 8).ByteLength);
        Assert.Equal(7, FunctionValueField.OfDigits(Dec("0"), 7).ByteLength);
    }

    /// <summary>The item a value describes (<c>CobolNum.ReturnedValueProfile</c>): the 4-byte binary item under 2**31 (2**32 unsigned) with at
    /// most 9 fraction digits, the 8-byte one under 2**63 (2**64) with at most 18, otherwise the DISPLAY item of exactly its digits.</summary>
    [Theory]
    [InlineData("0", 4)]
    [InlineData("12.50", 4)]
    [InlineData("-13", 4)]
    [InlineData("2147483647", 4)]
    [InlineData("4294967295", 4)]              // 32 bits unsigned: still the 4-byte item
    [InlineData("4294967296", 8)]
    [InlineData("-2147483648", 8)]             // 32 bits negative: past the 4-byte item
    [InlineData("0.123456789", 4)]             // scale 9
    [InlineData("0.1234567891", 8)]            // scale 10
    [InlineData("0.123456789012345678", 8)]    // scale 18
    [InlineData("9223372036854775808", 8)]     // 64 bits unsigned
    [InlineData("-9223372036854775808", 19)]   // NOT COPIED: 64 bits negative is a DISPLAY item of its 19 digits (GnuCOBOL's GMP estimate says 20)
    [InlineData("18446744073709551616", 20)]
    [InlineData("0.1234567890123456789", 19)]  // scale 19: a DISPLAY item of its digits
    [InlineData("12345678901234567890123", 23)]
    [InlineData("123456789012345678901234567890123456789", 39)]   // past the 38 digits an Int128 holds: still its own digits
    public void TheValueDescribesTheBytes(string value, int bytes) =>
        Assert.Equal(bytes, FunctionValueField.OfValue(Dec(value)).ByteLength);

    /// <summary>ABS holds the magnitude in a copy of the temporary: the same item, so the same bytes (a value temporary keeps its size, the
    /// sign position is the item's).</summary>
    [Fact]
    public void AbsKeepsTheTemporarysBytes()
    {
        Assert.Equal(4, FunctionValueField.OfValue(Dec("-13")).Abs().ByteLength);
        Assert.Equal(8, FunctionValueField.OfValue(Dec("-4294967297")).Abs().ByteLength);
        Assert.Equal(8, FunctionValueField.OfFloat(-0.1).Abs().ByteLength);
    }

    /// <summary>NOT COPIED: a binary64 result is the 8 bytes of a binary64 item; GnuCOBOL gives it a DISPLAY temporary of 38 to 96 digits
    /// (kb/Work PB3505 declined that image for DISPLAY too).</summary>
    [Fact]
    public void ABinary64ResultIsEightBytes() => Assert.Equal(8, FunctionValueField.OfFloat(2.5).ByteLength);
}
