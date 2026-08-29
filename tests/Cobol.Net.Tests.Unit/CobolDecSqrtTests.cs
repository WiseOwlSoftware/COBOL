// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The SDIDI square root (kb/Work PB116; ISO §15.84.4 r2 — "computed to 34 digits, and the result rounded to 34
/// digits according to the rules for standard-decimal arithmetic", the ONE §15 function whose standard-mode value
/// the standard fixes exactly; r1 — the exact argument enters unrounded). Expected values are the 34-digit
/// correctly-rounded roots, hand-derived from the reference expansions — never from the implementation.
/// </summary>
public sealed class CobolDecSqrtTests
{
    private static CobolDec D(string sig, int exp) => new(Int128.Parse(sig), exp);

    [Fact]
    public void Sqrt2_Is34DigitsCorrect()
    {
        // √2 = 1.41421356237309504880168872420969807856…; the 34-digit prefix is
        // 1.414213562373095048801688724209698 and the 35th digit is 0, so every rounding mode agrees.
        var expected = D("1414213562373095048801688724209698", -33);
        Assert.Equal(expected, CobolDec.Sqrt(D("2", 0), CobolRounding.NearestAwayFromZero));
        Assert.Equal(expected, CobolDec.Sqrt(D("2", 0), CobolRounding.Truncation));
    }

    [Fact]
    public void SqrtPointOne_RoundsAtThe34thDigit()
    {
        // √0.1 = 0.31622776601683793319988935444327185337…; the 34-digit prefix is
        // 0.3162277660168379331998893544432718, the 35th digit 5 with a nonzero tail — NearestAway rounds up
        // to …2719, Truncation keeps …2718. The mode reaches the landing.
        Assert.Equal(D("3162277660168379331998893544432719", -34),
            CobolDec.Sqrt(D("1", -1), CobolRounding.NearestAwayFromZero));
        Assert.Equal(D("3162277660168379331998893544432718", -34),
            CobolDec.Sqrt(D("1", -1), CobolRounding.Truncation));
    }

    [Fact]
    public void ExactSquares_AreExact()
    {
        // An exact root has a zero remainder — sticky false — so no mode can perturb it (algebraic compare:
        // the significand/exponent normal form may differ).
        Assert.Equal(0, CobolDec.Compare(CobolDec.Sqrt(D("4", 0), CobolRounding.Truncation), D("2", 0)));
        Assert.Equal(0, CobolDec.Compare(CobolDec.Sqrt(D("121", -2), CobolRounding.Truncation), D("11", -1)));
        Assert.Equal(0, CobolDec.Compare(CobolDec.Sqrt(D("1", -30), CobolRounding.Truncation), D("1", -15)));
        Assert.Equal(0, CobolDec.Compare(CobolDec.Sqrt(D("0", 0), CobolRounding.Truncation), D("0", 0)));
    }

    [Fact]
    public void StdDevDec_RidesTheDecimalSqrt()
    {
        // §15.86.4 r1's EAE = SQRT(VARIANCE(list)); variance(1,2,3,4) = 1.25 exactly, and √1.25 =
        // 1.11803398874989484820458683436563811772…; the 34-digit prefix is
        // 1.118033988749894848204586834365638, 35th digit 1 ⇒ every mode keeps …5638. The former Math.Sqrt
        // detour carried ~16 correct digits.
        Assert.Equal(D("1118033988749894848204586834365638", -33),
            CobolIntrinsics.StdDevDec(CobolRounding.NearestAwayFromZero, D("1", 0), D("2", 0), D("3", 0), D("4", 0)));
    }
}
