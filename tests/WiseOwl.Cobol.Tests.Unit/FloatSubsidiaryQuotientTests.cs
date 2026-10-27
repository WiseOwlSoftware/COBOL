// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE SUBSIDIARY QUOTIENT OF DIVIDE ... GIVING identifier-3 REMAINDER WHEN IDENTIFIER-3 IS FLOATING-POINT (ISO
/// §14.9.12.4 6) c), kb/Work PB2923): "a subsidiary quotient is developed that is signed and derived from the quotient by
/// truncation of digits at the least significant end and that has the same number of digits and the same decimal point
/// location as the data item referenced by identifier-3". A floating-point item has no fixed decimal point, so the
/// quotient is truncated INTO ITS SHAPE: a floating-point numeric-edited item keeps its significand's digits at the
/// quotient's own exponent (<see cref="CobolEdit.TruncateToFloatShape"/>), and a binary floating-point item the value of
/// its format nearest the quotient without passing it - the landing the item's own store performs under implied TRUNCATION
/// (<see cref="FloatResultant"/>, ISO §14.7.4.3 10)). Expected values are worked from the rule by hand, not read from the code.
/// </summary>
public sealed class FloatSubsidiaryQuotientTests
{
    private static string Show(CobolDec d) => $"{d.Sig}E{d.Exp}";

    private static CobolDec Dec(string sig, int exp) => new(Int128.Parse(sig), exp);

    // +9.99E+99: a three-digit significand with the point after its first digit (SigScale 2), exponent 0..99. The expected
    // value is S x 10^(E - 2) for the normalized three-digit S (leading digit nonzero) and the exponent E of 3.33E-1 etc.
    [Theory]
    [InlineData("3333333333333333333", -19, "333E-3")]              // 0.3333333333333333333 = 3.33E-1
    [InlineData("-3333333333333333333", -19, "-333E-3")]            // the sign survives
    [InlineData("10", 0, "100E-1")]                                 // 10 = 1.00E+1
    [InlineData("1", -25, "100E-27")]                               // 1E-25 = 1.00E-25: a fraction a fixed scale of 5 loses entirely
    [InlineData("3333333333333333333333333", -49, "333E-27")]       // 3.33E-25: the repro's quotient 1 / 3E24
    [InlineData("999999", -3, "999E0")]                             // 999.999 truncates to 9.99E+2, never rounds up to 1.00E+3
    [InlineData("0", 0, "0E0")]
    public void AFloatEditedItem_KeepsItsSignificandDigits_AtTheQuotientsOwnExponent(string sig, int exp, string expected) =>
        Assert.Equal(expected, Show(CobolEdit.TruncateToFloatShape(Dec(sig, exp), "+9.99E+99")));

    [Fact]
    public void AFloatEditedItem_HoldsAValueBeyondItsExponentAsTheSaturatedContent_AndANearerOneAsZero()
    {
        // +9.99E+9: exponent 0..9. 5E12 is farther from zero than 9.99E+9: the store's pinned all-nines image.
        Assert.Equal("999E7", Show(CobolEdit.TruncateToFloatShape(Dec("5", 12), "+9.99E+9")));
        // 3.33E-25 is nearer to zero than the item can hold (1.00E-9 is the smallest nonzero): zero.
        Assert.Equal("0E0", Show(CobolEdit.TruncateToFloatShape(Dec("333", -27), "+9.99E+9")));
    }

    [Fact]
    public void AFloatEditedItem_AgreesWithItsOwnStore_ForEveryValue()
    {
        // The subsidiary quotient and the stored one are one normalization: the image a TRUNCATING store writes de-edits
        // to the subsidiary quotient.
        foreach (var (sig, exp) in new (string, int)[] { ("1", 0), ("7", -1), ("123456789", -4), ("-2", -3), ("99999", 0), ("5", -30), ("-17", 20) })
        {
            var q = Dec(sig, exp);
            string image = CobolEdit.FormatFloatStore(q, "+9.99E+99", CobolRounding.Truncation);
            var held = CobolEdit.DeEditFloat(image, "+9.99E+99");
            Assert.Equal(0, CobolDec.Compare(held, CobolEdit.TruncateToFloatShape(q, "+9.99E+99")));
        }
    }

    // 1/10 = 0.1: the nearest binary64 (0.1000000000000000055511151231257827) is ABOVE it, so the truncation steps down.
    [Fact]
    public void ABinary64Item_HoldsTheFormatValueNearestTheQuotientWithoutPassingIt()
    {
        Assert.Equal(Math.BitDecrement(0.1), FloatResultant.FromDec(Dec("1", -1), CobolRounding.Truncation, false));
        Assert.Equal(Math.BitIncrement(-0.1), FloatResultant.FromDec(Dec("-1", -1), CobolRounding.Truncation, false));
        // 0.3333333333333333333: the nearest binary64 (0.33333333333333331483) is below it, so it is already the truncation.
        Assert.Equal(1.0 / 3.0, FloatResultant.FromDec(Dec("3333333333333333333", -19), CobolRounding.Truncation, false));
        // A quotient the format holds exactly is itself.
        Assert.Equal(0.5, FloatResultant.FromDec(Dec("5", -1), CobolRounding.Truncation, false));
        Assert.Equal(0.0, FloatResultant.FromDec(Dec("0", 0), CobolRounding.Truncation, false));
    }

    [Fact]
    public void ABinary32Item_HoldsTheBinary32ValueNearestTheQuotientWithoutPassingIt()
    {
        // 2/3: the nearest binary32 is 0.666666686534881591796875 (11184811 x 2^-24, above); the truncation is the one
        // below it, 0.66666662693023681640625 = 11184810 x 2^-24.
        Assert.Equal(11184810.0 / 16777216.0, FloatResultant.FromDec(Dec("6666666666666666666", -19), CobolRounding.Truncation, true));
        Assert.Equal(-11184810.0 / 16777216.0, FloatResultant.FromDec(Dec("-6666666666666666666", -19), CobolRounding.Truncation, true));
        // 10/3: the nearest binary32 (13981013 x 2^-22 = 3.3333332538604736328125) is already below.
        Assert.Equal(13981013.0 / 4194304.0, FloatResultant.FromDec(Dec("3333333333333333333", -18), CobolRounding.Truncation, true));
    }

    [Fact]
    public void AQuotientBeyondTheFormat_IsTheFormatsLargestFiniteValue()
    {
        Assert.Equal(double.MaxValue, FloatResultant.FromDec(Dec("1", 400), CobolRounding.Truncation, false));
        Assert.Equal(-(double)float.MaxValue, FloatResultant.FromDec(Dec("-1", 60), CobolRounding.Truncation, true));
        Assert.Equal((double)float.MaxValue, FloatResultant.FromReal(1e300, CobolRounding.Truncation, true));
    }

    [Fact]
    public void ABinary64QuotientNarrowedToBinary32_TruncatesTowardZero()
    {
        // 0.6666666666666666 (binary64) narrows to the nearest binary32 0.66666668653..., one ulp above it.
        Assert.Equal(11184810.0 / 16777216.0, FloatResultant.FromReal(2.0 / 3.0, CobolRounding.Truncation, true));
        Assert.Equal(-11184810.0 / 16777216.0, FloatResultant.FromReal(-2.0 / 3.0, CobolRounding.Truncation, true));
        Assert.Equal(0.5, FloatResultant.FromReal(0.5, CobolRounding.Truncation, true));
        Assert.Equal(0.0, FloatResultant.FromReal(0.0, CobolRounding.Truncation, true));
        Assert.True(double.IsNaN(FloatResultant.FromReal(double.NaN, CobolRounding.Truncation, true)));
    }
}
