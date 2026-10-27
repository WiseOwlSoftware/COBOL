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

    /// <summary>ABS, MAX, MIN and the odd MEDIAN return a COPY of an argument's item: the text is what DISPLAY of that item writes, so
    /// <c>FUNCTION MAX(U S)</c> is U's <c>00012.50</c> (unsigned, no sign) and a signed item's positive value shows its <c>+</c>.</summary>
    [Fact]
    public void ASelectionIsACopyOfTheArgumentsItem()
    {
        NumProfile u = Item(7, 2, signed: false);          // PIC 9(5)V99
        NumProfile s = Item(7, 2, signed: true);           // PIC S9(5)V99
        NumProfile i = Item(3, 0, signed: false);          // PIC 9(3)
        NumProfile si = Item(3, 0, signed: true);          // PIC S9(3)
        var fu = FunctionValueField.OfItem(Dec("12.50"), u);
        var fs = FunctionValueField.OfItem(Dec("-12.50"), s);
        Assert.Equal("00012.50", FunctionValueField.Max(fu, fs).DeviceText(false));
        Assert.Equal("-00012.50", FunctionValueField.Min(fu, fs).DeviceText(false));
        Assert.Equal("+00012.50", fs.Abs().DeviceText(false));                           // ABS keeps the sign position
        Assert.Equal("00012.50", fu.Abs().DeviceText(false));
        Assert.Equal("007", FunctionValueField.Max(FunctionValueField.OfItem(Dec("7"), i), FunctionValueField.OfItem(Dec("-14"), si)).DeviceText(false));
        Assert.Equal("-014", FunctionValueField.Min(FunctionValueField.OfItem(Dec("7"), i), FunctionValueField.OfItem(Dec("-14"), si)).DeviceText(false));
    }

    /// <summary>A tie keeps the FIRST argument (§15.59.4 r1 / §15.63.4 r1 take the leftmost of equal values), so its item is the temporary.</summary>
    [Fact]
    public void ATieKeepsTheFirstArgumentsItem()
    {
        var unsigned = FunctionValueField.OfItem(Dec("12.50"), Item(7, 2, signed: false));
        var signed = FunctionValueField.OfItem(Dec("12.50"), Item(7, 2, signed: true));
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
        var a = FunctionValueField.OfItem(Dec("12.5"), Item(7, 2, signed: false));
        var b = FunctionValueField.OfItem(Dec("-12.5"), Item(7, 2, signed: true));
        var c = FunctionValueField.OfItem(Dec("2.5"), Item(2, 1, signed: false));
        Assert.Equal("2.5", FunctionValueField.Median(a, b, c).DeviceText(false));              // middle of -12.5 2.5 12.5: c's PIC 9V9
        Assert.Equal("-00012.50", FunctionValueField.Median(b).DeviceText(false));                // one argument: that argument's item
        var d = FunctionValueField.OfItem(Dec("7"), Item(3, 0, signed: false));
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
            FunctionValueField.OfItem(Dec("5"), new NumProfile { Digits = 10, FractionDigits = 0, Signed = false, Truncation = NumericTruncation.BinaryCapacity, ByteForm = NumericByteForm.Binary, StorageLength = 4 })).DeviceText(false));
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
}
