// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The currency STRING in <c>CobolEdit</c> (ISO §12.3.7.4 GR13 / §13.18.40.4 GR14; kb/Work PB60 / AR-15.68.3-3):
/// the mask's currency symbol is the canonical <c>$</c> and <c>currencyString</c> is what it renders as — a
/// one-character string other than '$', or a multi-character one that widens the item by its extra length. The
/// core edits the LOGICAL image; the physical image is its expansion, and de-editing collapses it back.
/// </summary>
public sealed class CobolEditCurrencyStringTests
{
    [Theory]
    [InlineData("$9.99", 525, 2, "USD", "USD5.25")]                 // fixed insertion (§13.18.40.5 r5): the string where the symbol is
    [InlineData("$$$9", 12, 0, "USD", " USD12")]                     // floating (r6a): before the first nonzero digit; 6 wide (GR14)
    [InlineData("$$$9", 123, 0, "USD", "USD123")]
    [InlineData("$$$9", 0, 0, "USD", "  USD0")]                      // zero with a '9': before the first non-floating position
    [InlineData("$$$$", 0, 0, "USD", "      ")]                      // all-floating zero (r6b): all spaces at the physical width
    [InlineData("$$$$", 7, 0, "USD", "  USD7")]
    [InlineData("-$$$9", -12, 0, "USD", "- USD12")]                  // a fixed sign before a floating string
    [InlineData("$$,$$9.99", 123450, 2, "#", "#1,234.50")]          // a one-character string other than '$' (bare CURRENCY SIGN "#")
    public void Format_ExpandsTheCurrencyString(string mask, long value, int scale, string cur, string expected) =>
        Assert.Equal(expected, CobolEdit.Format(value, scale, mask, currencyString: cur));

    [Theory]
    [InlineData("$9.99", "USD5.25", "USD", 525)]
    [InlineData("$$$9", " USD12", "USD", 12)]
    [InlineData("$$$9", "USD123", "USD", 123)]
    [InlineData("$$$9", "  USD0", "USD", 0)]
    [InlineData("$$$$", "      ", "USD", 0)]
    [InlineData("-$$$9", "- USD12", "USD", -12)]
    [InlineData("$$,$$9.99", "#1,234.50", "#", 123450)]
    public void DeEdit_CollapsesTheCurrencyString(string mask, string image, string cur, long expected) =>
        Assert.Equal((Int128)expected, CobolEdit.DeEdit(image, mask, currencyString: cur));

    [Fact]
    public void Format_DefaultDollar_IsUnchanged()
    {
        Assert.Equal(" $1.50", CobolEdit.Format(150, 2, "$$9.99"));
        Assert.Equal(" $1.50", CobolEdit.Format(150, 2, "$$9.99", currencyString: "$"));
    }

    [Fact]
    public void Format_CommaMode_ExpandsAfterTheSeparatorSwap() =>
        Assert.Equal(" USD1.234,50", CobolEdit.Format(123450, 2, "$$$.$$9,99", commaMode: true, currencyString: "USD"));

    [Fact]
    public void TryFormat_CapacityIsTheLogicalMasks() =>
        Assert.False(CobolEdit.TryFormat(12345, 0, "$$$9", out _, currencyString: "USD"));   // 3 digit positions
}
