// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The characters a numeric value with no stated representation sends to a group (kb/Work PB3570, PB3613;
/// docs/CONFORMANCE.md D-B4): its digits, in order, without the sign or the decimal separator, and the digits of a
/// floating-point text's SIGNIFICAND (never its exponent). The rule is the owner's determination for the literal
/// (2026-10-23), extended to a numeric function's value (§15.2 4)-5, §15.4.1); expected values come from that rule.
/// </summary>
public sealed class GroupDigitsTests
{
    [Theory]
    [InlineData("456", "456")]            // a bare digit run is its own image
    [InlineData("-456", "456")]           // the sign is no digit (§14.9.25.4 GR6 a)
    [InlineData("+456", "456")]
    [InlineData("-4.56", "456")]          // the decimal point is no digit
    [InlineData("-4,56", "456")]          // nor the comma of DECIMAL-POINT IS COMMA (§12.3.7.4 GR14 a)
    [InlineData("-0.05", "005")]          // leading zeros are digits
    [InlineData("0.00", "000")]
    [InlineData("1.5E+3", "15")]          // a floating-point text sends its significand
    [InlineData("-1.4142135623730951E+00", "14142135623730951")]
    [InlineData("1E-05", "1")]
    [InlineData("", "")]
    [InlineData("NaN", "")]               // no digits, nothing to send
    public void SendsTheDigitsOfTheSignificandOnly(string text, string expected) =>
        Assert.Equal(expected, CobolNum.GroupDigits(text));

    /// <summary>The digits of a returned value, through the same text image a DISPLAY of it shows.</summary>
    [Theory]
    [InlineData(-15, 0, "15")]
    [InlineData(-15, 1, "15")]            // -1.5
    [InlineData(-5, 2, "005")]            // -0.05
    [InlineData(325, 2, "325")]           // 3.25
    public void AFunctionValueSendsTheDigitsOfItsText(long unscaled, int scale, string expected) =>
        Assert.Equal(expected, CobolNum.GroupDigits(CobolNum.FormatFunctionText(unscaled, scale)));
}
