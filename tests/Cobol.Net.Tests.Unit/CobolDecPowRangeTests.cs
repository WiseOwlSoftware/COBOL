// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// <c>CobolDec.Pow</c>'s escape and range dispositions (kb/Work PB145; ISO §8.8.1.5.4 r2/r3 + §8.8.1.5.2 r2).
/// The old code raised a SPURIOUS size error for a near-unit base past the loop bound (the guard comment's
/// |n·log10|b|| ≥ |n|/34 is false within 10⁻³³ of 1), resolved a saturated out-of-long integer exponent's sign
/// BY THE PARITY OF long.MaxValue (odd — so (−1) ** 10²⁰ answered −1), collapsed SDIDI-range bases through
/// binary64 on the non-integer arm (1.0E−400 ** 0.5 → silent 0; 2.0E+400 ** 0.5 → spurious overflow), and
/// named EC-SIZE-OVERFLOW for BOTH out-of-range directions (0.5 ** 600000 is owed UNDERFLOW) plus
/// EC-SIZE-TRUNCATION for a below-range exponent under INTERMEDIATE ROUNDING IS PROHIBITED. Expected values
/// derived from the cited rules; the COBOL-level traces ride the pb145_* goldens.
/// </summary>
public sealed class CobolDecPowRangeTests
{
    private static readonly CobolDec TenTo20 = new(Int128.Parse("100000000000000000000"), 0);   // 10²⁰, EVEN
    private static readonly CobolDec MinusOne = new(-1, 0);
    private const CobolRounding Mode = CobolRounding.NearestAwayFromZero;

    // §8.8.1.5.4 r2: (−1) ** 10²⁰ = +1 — the exponent's exact parity, never the saturated long's.
    [Fact]
    public void MinusOne_ToAnEvenExponentPastLongRange_IsPlusOne()
        => Assert.Equal(new CobolDec(1, 0), CobolDec.Pow(MinusOne, TenTo20, Mode));

    // §8.8.1.5.4 r3: (−1) ** (−10²⁰) = 1 / ((−1) ** 10²⁰) = +1.
    // ⛔ COMPARED BY VALUE, NOT BY (Sig, Exp) — for the reason the TinyBase case below already carries.
    // r3's reciprocal is now spelled once, at the top of Pow, so this answer arrives through a real
    // Div(1, 1) whose pre-scaled quotient normalizes to (10³³, −33). That is the same VALUE as (1, 0), and
    // §8.8.1.5.2 leaves the internal representation to the implementor; the structural assertion that used to
    // stand here was pinning a representation the standard does not fix, and it was only ever satisfied
    // because r3 was NOT being evaluated on this path.
    [Fact]
    public void MinusOne_ToAnEvenNegativeExponentPastLongRange_IsPlusOne()
        => Assert.Equal(0, CobolDec.Compare(new CobolDec(1, 0),
            CobolDec.Pow(MinusOne, new CobolDec(-TenTo20.Sig, 0), Mode)));

    // §8.8.1.5.2 r2 sets the size error only when the value is out of range: 1.00001 ** 1000000 = e^9.99995…
    // ≈ 22025.36 is comfortably inside decimal128 (the old escape raised a spurious EC-SIZE-OVERFLOW).
    [Fact]
    public void NearUnitBase_PastTheLoopBound_ComputesInsteadOfRaising()
    {
        var r = CobolDec.Pow(CobolDec.From(100001, 5), new CobolDec(1_000_000, 0), Mode);
        double expected = Math.Pow(1.00001, 1_000_000);
        Assert.True(Math.Abs(r.ToDouble() / expected - 1) < 1e-9,
            $"got {r.ToDouble()}, expected ≈{expected}");
    }

    // §8.8.1.5.2 r2's TWO names: 0.5 ** 600000 ≈ 10^−180618 is too SMALL (UNDERFLOW — the old escape said
    // OVERFLOW), 2 ** 600000 too LARGE.
    [Fact]
    public void HalfPastTheLoopBound_IsUnderflow_NotOverflow()
    {
        var ex = Assert.Throws<CobolSizeError>(() => CobolDec.Pow(CobolDec.From(5, 1), new CobolDec(600_000, 0), Mode));
        Assert.Equal("EC-SIZE-UNDERFLOW", ex.EcName);
    }

    [Fact]
    public void TwoPastTheLoopBound_IsOverflow()
    {
        var ex = Assert.Throws<CobolSizeError>(() => CobolDec.Pow(new CobolDec(2, 0), new CobolDec(600_000, 0), Mode));
        Assert.Equal("EC-SIZE-OVERFLOW", ex.EcName);
    }

    // §8.8.1.5.4 r2e over a base OUTSIDE binary64's range: 1.0E−400 ** 0.5 = 1.0E−200 EXACTLY (the old
    // ToDouble underflowed the base to 0 and answered a silent 0).
    // ⛔ COMPARED BY VALUE, NOT BY (Sig, Exp). §8.8.1.5.2 leaves the SDIDI's internal representation to the
    // implementor — 1E−200 is 1×10⁻²⁰⁰ and 10³³×10⁻²³³ alike, and the two arms that can produce it legitimately
    // produce different pairs (since owner decision D-C the ½ exponent routes through Sqrt, whose exact-integer
    // root carries its guard digits into the significand). Structural equality here was pinning a representation
    // the standard does not fix; CobolDec.Compare is the relation the language's own `=` uses.
    [Fact]
    public void TinyBase_NonIntegerExponent_DoesNotCollapseToZero()
        => Assert.Equal(0, CobolDec.Compare(new CobolDec(1, -200),
            CobolDec.Pow(CobolDec.From(1, 400), CobolDec.From(5, 1), Mode)));

    // …and 2.0E+400 ** 0.5 ≈ 1.4142…E+200 is inside decimal128 (the old path raised a spurious overflow).
    [Fact]
    public void HugeBase_NonIntegerExponent_DoesNotSpuriouslyOverflow()
    {
        var r = CobolDec.Pow(new CobolDec(2, 400), CobolDec.From(5, 1), Mode);
        var scaled = CobolDec.Mul(r, CobolDec.From(1, 200), Mode);   // ÷10²⁰⁰ → √2
        Assert.True(Math.Abs(scaled.ToDouble() / Math.Sqrt(2) - 1) < 1e-9,
            $"got {scaled.ToDouble()}, expected ≈√2");
    }

    // §8.8.1.5.2 r2: a below-range exponent is the TOO-SMALL condition under EVERY intermediate rounding
    // mode — INTERMEDIATE ROUNDING IS PROHIBITED included (the old Clamp path threw §14.7.4.3 r7's
    // EC-SIZE-TRUNCATION there: two names for one physical condition).
    [Theory]
    [InlineData(CobolRounding.Prohibited)]
    [InlineData(CobolRounding.NearestAwayFromZero)]
    [InlineData(CobolRounding.NearestEven)]
    [InlineData(CobolRounding.Truncation)]
    public void BelowRangeExponent_IsUnderflow_UnderEveryIntermediateMode(CobolRounding mode)
    {
        var ex = Assert.Throws<CobolSizeError>(() => CobolDec.Mul(CobolDec.From(1, 6000), CobolDec.From(1, 200), mode));
        Assert.Equal("EC-SIZE-UNDERFLOW", ex.EcName);
    }

    // §8.8.1.5.2 r2's gradual underflow: 1.5E−6176 lands ON the 10⁻⁶¹⁷⁶ quantum by mode — 2 under the
    // NEAREST modes (tie away / to-even-2), 1 under TRUNCATION; PROHIBITED raises the too-small name.
    [Theory]
    [InlineData(CobolRounding.NearestAwayFromZero, 2)]
    [InlineData(CobolRounding.NearestEven, 2)]
    [InlineData(CobolRounding.Truncation, 1)]
    public void SubnormalTie_LandsOnTheQuantumByMode(CobolRounding mode, int quantum)
        => Assert.Equal(new CobolDec(quantum, -6176), CobolDec.Mul(CobolDec.From(15, 1), CobolDec.From(1, 6176), mode));

    [Fact]
    public void SubnormalTie_UnderProhibited_IsUnderflowByName()
    {
        var ex = Assert.Throws<CobolSizeError>(() => CobolDec.Mul(CobolDec.From(15, 1), CobolDec.From(1, 6176), CobolRounding.Prohibited));
        Assert.Equal("EC-SIZE-UNDERFLOW", ex.EcName);
    }
}
