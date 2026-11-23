// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// NUMVAL-F's binary64 projection (<c>CobolIntrinsics.NumvalFDouble</c>, the ACCEPT device transfer into a binary64
/// receiver): ONE correctly-rounded conversion of the scan's canonical <c>[-]digits E exp</c>, the same
/// <c>NvfScan</c> and reject projection as the SDIDI twin <c>NumvalFDec</c> that FUNCTION NUMVAL-F returns in every
/// arithmetic mode and channel (kb/Work PB3839).
/// </summary>
public sealed class NumvalFDoubleTests
{
    [Theory]
    [InlineData("5E+30", 5e30)]
    [InlineData("9E+30", 9e30)]
    [InlineData("1.5E-12", 1.5e-12)]
    [InlineData(" 1.5 ", 1.5)]
    [InlineData("0.1", 0.1)]                    // the decimal-clean identity, one rounding
    [InlineData(" - 35E+0 ", -35d)]             // §2023:15.69.3 AR5's legal spaces
    [InlineData("12345678901234567890E-20", 0.12345678901234567890)]   // 20 digits ⇒ the nearest binary64
    public void NumvalFDouble_IsTheNearestBinary64(string text, double expected) =>
        Assert.Equal(expected, CobolIntrinsics.NumvalFDouble(text));

    [Fact]
    public void NumvalFDouble_CommaMode_And_Rejects()
    {
        Assert.Equal(1.5, CobolIntrinsics.NumvalFDouble("1,5", commaMode: true));    // §2023:15.69.3 AR4
        Assert.Equal(0d, CobolIntrinsics.NumvalFDouble("1 2"));                        // r5's except-clause — the shared reject, checking off
        Assert.Equal(0d, CobolIntrinsics.NumvalFDouble("0E+9999"));                    // zero significand
        Assert.True(double.IsPositiveInfinity(CobolIntrinsics.NumvalFDouble("1E+9999")));   // past binary64 is +Infinity
    }

    /// <summary>kb/Work PB2658: the conversion is <c>CobolFloat.ScaledToDouble</c>, whose exact-operand path (a
    /// significand within 2^53 and a scale within ±22) and whose integer cast at scale 0 must each give the SAME
    /// correctly rounded binary64 as one parse of the text. A seeded sweep of significands of 1–31 digits at exponents
    /// −39..+39 (0 among them) crosses all three paths, the 2^53 bound and the 10^22 bound, with both signs.</summary>
    [Fact]
    public void NumvalFDouble_IsTheParseOfItsText_OnEveryPathOfTheConversion()
    {
        var rng = new Random(2658);
        int examined = 0;
        for (int digits = 1; digits <= 31; digits++)
        {
            for (int trial = 0; trial < 8; trial++)
            {
                var significand = new System.Text.StringBuilder().Append((char)('1' + rng.Next(9)));
                for (int d = 1; d < digits; d++) significand.Append((char)('0' + rng.Next(10)));
                for (int exponent = -39; exponent <= 39; exponent += 3)
                {
                    string text = $"{(trial % 2 == 0 ? "" : "-")}{significand}E{(exponent < 0 ? "-" : "+")}{Math.Abs(exponent)}";
                    double expected = double.Parse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
                    Assert.True(BitConverter.DoubleToInt64Bits(expected) == BitConverter.DoubleToInt64Bits(CobolIntrinsics.NumvalFDouble(text)), text);
                    examined++;
                }
            }
        }
        Assert.Equal(31 * 8 * 27, examined);
    }

    /// <summary>The binary64 carries the argument's DECIMAL IDENTITY: converted back through the shortest
    /// round-trip decimal (the <c>CobolDec.FromApproximation</c> conversion of an implementor-defined approximation) it
    /// lands at scale 9 exactly where the exact SDIDI value (<c>NumvalFDec</c>, what the function returns) is.
    /// (Deliberately NOT through <c>CobolFloat.ToScaled</c>'s multiply-then-truncate, which lands 1.5E-8 at
    /// scale 9 as 14.)</summary>
    [Theory]
    [InlineData("1.5E-8")]
    [InlineData("1234.5")]
    [InlineData("-2.5E+1")]
    public void NumvalFDouble_CarriesTheDecimalIdentity_OfTheSdidiValue(string text) =>
        Assert.Equal(CobolIntrinsics.NumvalFDec(CobolRounding.NearestAwayFromZero, text).ToUnscaled(9, CobolRounding.Truncation),
            CobolDec.FromApproximation(CobolIntrinsics.NumvalFDouble(text)).ToUnscaled(9, CobolRounding.Truncation));
}
