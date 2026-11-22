// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EVERY ROUNDING LANE TAKES THE SAME NEIGHBOUR (kb/Work PB2644): the §2023:14.7.4.3 GR3 to GR10 mode table is written
/// once, in <c>RoundingRule</c>, and the lanes differ only in how they measure the dropped part. This pins the agreement
/// the old copies had lost: the fixed-point divide kernel (<see cref="CobolNum.RoundDiv{T}"/>, a remainder against its
/// divisor), the standard-decimal digit walk (<c>CobolDec.Round34Wide</c>, reached by <see cref="CobolDec.FromParsed"/>
/// on a 36-digit significand — a round digit and a sticky tail) and the exact 256-bit final transfer
/// (<see cref="CobolDec.MulAtScale"/>). Before the unification the digit walk rounded every mode but TRUNCATION and
/// NEAREST-EVEN as NEAREST-AWAY-FROM-ZERO. Expected values are the divide kernel's, which
/// <see cref="RoundDivBoundaryTests"/> pins against the rule text.
/// </summary>
public sealed class RoundingRuleLaneAgreementTests
{
    /// <summary>Every mode but PROHIBITED (GR7 stores nothing; see the raising fact below).</summary>
    public static TheoryData<CobolRounding> Modes() => new(
        CobolRounding.Truncation, CobolRounding.NearestAwayFromZero, CobolRounding.AwayFromZero, CobolRounding.NearestEven,
        CobolRounding.NearestTowardZero, CobolRounding.TowardGreater, CobolRounding.TowardLesser);

    /// <summary>36-digit significands whose last two digits are dropped: below, on and above the half, after an even and
    /// an odd kept digit, and with the sticky digit alone. None carries into a 35th digit.</summary>
    private static readonly Int128[] Magnitudes =
    [
        Int128.Parse("123456789012345678901234567890123400"),   // exact
        Int128.Parse("123456789012345678901234567890123401"),   // sticky only
        Int128.Parse("123456789012345678901234567890123449"),   // below the half
        Int128.Parse("123456789012345678901234567890123450"),   // a tie, even kept digit
        Int128.Parse("123456789012345678901234567890123350"),   // a tie, odd kept digit
        Int128.Parse("123456789012345678901234567890123451"),   // just above the half
        Int128.Parse("123456789012345678901234567890123499"),   // far above the half
    ];

    [Theory]
    [MemberData(nameof(Modes))]
    public void TheDecimalDigitWalk_TakesTheDivideKernelsNeighbour(CobolRounding mode)
    {
        foreach (Int128 magnitude in Magnitudes)
            foreach (Int128 value in new[] { magnitude, -magnitude })
            {
                CobolDec walked = CobolDec.FromParsed(value, 0, mode);
                Int128 expected = CobolNum.RoundDiv(value, 100, mode);
                Assert.Equal((expected, 2), (walked.Sig, walked.Exp));
            }
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void TheExactFinalTransfer_TakesTheDivideKernelsNeighbour(CobolRounding mode)
    {
        foreach (Int128 magnitude in Magnitudes)
            foreach (Int128 value in new[] { magnitude, -magnitude })
                Assert.Equal(CobolNum.RoundDiv(value, 100, mode),
                    CobolDec.MulAtScale(value, 2, 1, 0, 0, mode, checkedTransfer: false));
    }

    [Fact]
    public void Prohibited_RaisesTheTruncationConditionInEveryLane_AndOnlyWhenInexact()
    {
        Int128 inexact = Magnitudes[1], exact = Magnitudes[0];
        Assert.Equal("EC-SIZE-TRUNCATION",
            Assert.Throws<CobolSizeError>(() => CobolNum.RoundDiv(inexact, 100, CobolRounding.Prohibited)).EcName);
        Assert.Equal("EC-SIZE-TRUNCATION",
            Assert.Throws<CobolSizeError>(() => CobolDec.FromParsed(inexact, 0, CobolRounding.Prohibited)).EcName);
        Assert.Equal("EC-SIZE-TRUNCATION",
            Assert.Throws<CobolSizeError>(() => CobolDec.MulAtScale(inexact, 2, 1, 0, 0, CobolRounding.Prohibited, false)).EcName);
        Assert.Equal(exact / 100, CobolNum.RoundDiv(exact, 100, CobolRounding.Prohibited));
        Assert.Equal(exact / 100, CobolDec.MulAtScale(exact, 2, 1, 0, 0, CobolRounding.Prohibited, false));
    }
}
