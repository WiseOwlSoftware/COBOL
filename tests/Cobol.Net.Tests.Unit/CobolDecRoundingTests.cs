// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The SDIDI final transfer's rounding decision (<c>CobolDec.ToUnscaled</c>, ISO §14.7.4.3) and the §8.8.1.5.2 r2
/// range check (<c>CobolDec.FromParsed</c> → <c>Clamp</c>) for values FAR BELOW the target precision — the
/// remainder-marker shape (kb/Work PB76): past the Int128 carrier the true remainder is unavailable, so
/// <c>DivRemPow10</c> returns a marker, and that marker must read as a signed, below-half, inexact remainder —
/// never as an exact half-tie.
/// </summary>
/// <remarks>The defect this pins out: the marker was <c>(0, 1, 2)</c> — exactly half — so every NEAREST mode
/// lifted a sub-precision value to one unit (<c>COMPUTE R9 ROUNDED = 10 ** -20</c> stored 0.000000001 into
/// <c>V9(9)</c>), and the unsigned remainder sent AWAY-FROM-ZERO / TOWARD-GREATER of a NEGATIVE value toward +∞.
/// The 34-digit significand makes the shape common: 1/10²⁰ is 10³³×10⁻⁵³, 44 places below scale 9. Expected
/// values are §14.7.4.3's own: r4 nearest (tie ⇒ away), r5 nearest-even, r3 away-from-zero, r8 toward-greater,
/// r9 toward-lesser, r10 truncation, r6 nearest-toward-zero, r7 prohibited ⇒ size error.</remarks>
public sealed class CobolDecRoundingTests
{
    // 10⁻²⁰ in the representation SDIDI division produces (34-digit significand, exponent −53): 44 places below scale 9.
    private static readonly CobolDec TinyPos = new(Int128.Parse("1000000000000000000000000000000000"), -53);
    private static readonly CobolDec TinyNeg = new(-Int128.Parse("1000000000000000000000000000000000"), -53);

    [Theory]
    [InlineData(CobolRounding.NearestAwayFromZero, 0)]     // r4 — nowhere near a tie ⇒ 0
    [InlineData(CobolRounding.NearestEven, 0)]             // r5
    [InlineData(CobolRounding.NearestTowardZero, 0)]       // r6
    [InlineData(CobolRounding.Truncation, 0)]              // r10
    [InlineData(CobolRounding.AwayFromZero, 1)]            // r3 — the nearest value farther from zero
    [InlineData(CobolRounding.TowardGreater, 1)]           // r8
    [InlineData(CobolRounding.TowardLesser, 0)]            // r9
    public void SubPrecisionPositive_RoundsPerMode(CobolRounding mode, long expectedUnits) =>
        Assert.Equal((Int128)expectedUnits, TinyPos.ToUnscaled(9, mode));

    [Theory]
    [InlineData(CobolRounding.NearestAwayFromZero, 0)]
    [InlineData(CobolRounding.NearestEven, 0)]
    [InlineData(CobolRounding.Truncation, 0)]
    [InlineData(CobolRounding.AwayFromZero, -1)]           // r3 — farther from zero, in the VALUE's direction
    [InlineData(CobolRounding.TowardGreater, 0)]           // r8 — 0 is the nearest greater value
    [InlineData(CobolRounding.TowardLesser, -1)]           // r9
    public void SubPrecisionNegative_KeepsTheValuesSign(CobolRounding mode, long expectedUnits) =>
        Assert.Equal((Int128)expectedUnits, TinyNeg.ToUnscaled(9, mode));

    [Fact]
    public void SubPrecision_Prohibited_IsInexact() =>
        Assert.Throws<CobolSizeError>(() => TinyPos.ToUnscaled(9, CobolRounding.Prohibited));   // r7

    /// <summary>The same marker feeds the decimal128 subnormal re-round (§8.8.1.5.2 r2): a value far below the
    /// 10⁻⁶¹⁷⁶ quantum rounds to zero under a NEAREST mode and is EC-SIZE-UNDERFLOW — the half-tie marker had
    /// returned it as one quantum instead.</summary>
    [Fact]
    public void FarBelowSubnormal_IsUnderflow_NotOneQuantum()
    {
        var ex = Assert.Throws<CobolSizeError>(() => CobolDec.FromParsed(1, -9999, CobolRounding.NearestAwayFromZero));
        Assert.Equal("EC-SIZE-UNDERFLOW", ex.EcName);
        // AWAY-FROM-ZERO keeps the one-quantum step (r3) — the smallest positive nonzero value.
        var q = CobolDec.FromParsed(1, -9999, CobolRounding.AwayFromZero);
        Assert.Equal((Int128)1, q.Sig);
        Assert.Equal(-6176, q.Exp);
    }

    /// <summary>The at-scale controls the sub-precision case must agree with: an exact tie rounds away (r4), a
    /// below-half remainder rounds down.</summary>
    [Fact]
    public void AtScale_TieAndBelowHalf_Controls()
    {
        Assert.Equal((Int128)1, CobolDec.From(5, 10).ToUnscaled(9, CobolRounding.NearestAwayFromZero));   // 5E-10 — tie ⇒ away
        Assert.Equal((Int128)0, CobolDec.From(4, 10).ToUnscaled(9, CobolRounding.NearestAwayFromZero));   // 4E-10 ⇒ 0
        Assert.Equal((Int128)0, CobolDec.From(5, 10).ToUnscaled(9, CobolRounding.NearestEven));           // tie ⇒ even (0)
    }
}
