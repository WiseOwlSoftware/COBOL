// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// NUMVAL-F's NATIVE binary64 projection (<c>CobolIntrinsics.NumvalFDouble</c>, kb/Work PB60 / RV-15.69.4-2):
/// §15.69.4 r2's approximation carried as the float family's binary64 in the receiver-less and float-receiver
/// channels — ONE correctly-rounded conversion of the scan's canonical <c>[-]digits E exp</c>, the same
/// <c>NvfScan</c> and reject projection as the Int128 and SDIDI twins.
/// </summary>
public sealed class NumvalFDoubleTests
{
    [Theory]
    [InlineData("5E+30", 5e30)]                 // the value that saturated the Int128 receiver-less channel
    [InlineData("9E+30", 9e30)]                 // …and compared EQUAL to it there
    [InlineData("1.5E-12", 1.5e-12)]            // …and was 0 there
    [InlineData(" 1.5 ", 1.5)]
    [InlineData("0.1", 0.1)]                    // the decimal-clean identity, one rounding
    [InlineData(" - 35E+0 ", -35d)]             // §15.69.3 r5's legal spaces
    [InlineData("12345678901234567890E-20", 0.12345678901234567890)]   // 20 digits ⇒ the nearest binary64
    public void NumvalFDouble_IsTheNearestBinary64(string text, double expected) =>
        Assert.Equal(expected, CobolIntrinsics.NumvalFDouble(text));

    [Fact]
    public void NumvalFDouble_CommaMode_And_Rejects()
    {
        Assert.Equal(1.5, CobolIntrinsics.NumvalFDouble("1,5", commaMode: true));    // §15.69.3 r4
        Assert.Equal(0d, CobolIntrinsics.NumvalFDouble("1 2"));                        // r5's except-clause — the shared reject, checking off
        Assert.Equal(0d, CobolIntrinsics.NumvalFDouble("0E+9999"));                    // zero significand
        Assert.True(double.IsPositiveInfinity(CobolIntrinsics.NumvalFDouble("1E+9999")));   // past binary64 — the float family's disposition
    }

    /// <summary>The binary64 carries the argument's DECIMAL IDENTITY: converted back through the shortest
    /// round-trip decimal (the §8.8.1.5.1 <c>CobolDec.FromDouble</c> conversion) it lands at scale 9 exactly
    /// where the Int128 projection (a fixed arithmetic receiver's / a MOVE sender's route) already is.
    /// (Deliberately NOT through <c>CobolFloat.ToScaled</c>'s multiply-then-truncate, which lands 1.5E-8 at
    /// scale 9 as 14 — the very artifact the MOVE sender's exact route exists to avoid.)</summary>
    [Theory]
    [InlineData("1.5E-8")]
    [InlineData("1234.5")]
    [InlineData("-2.5E+1")]
    public void NumvalFDouble_CarriesTheDecimalIdentity_OfTheInt128Projection(string text) =>
        Assert.Equal(CobolIntrinsics.NumvalF(text, 9),
            CobolDec.FromDouble(CobolIntrinsics.NumvalFDouble(text)).ToUnscaled(9, CobolRounding.Truncation));
}
