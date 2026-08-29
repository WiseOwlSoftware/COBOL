// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// FACTORIAL on the SDIDI lane (kb/Work PB125; ISO §15.36.4 r1c — the equivalent arithmetic expression
/// n × (n−1) × … × 1 in standard-decimal arithmetic). Expected values hand-derived: 34! =
/// 295232799039604140847618609643520000000 — 39 digits with 7 trailing zeros, so every intermediate product
/// fits decimal128 exactly (the dropped digits are always trailing zeros) and the result is EXACT, where the
/// Int128 native lane's documented 33 cap answered the §15.3 default 0 for a conforming argument.
/// </summary>
public sealed class FactorialDecTests
{
    private static CobolDec D(string sig, int exp) => new(Int128.Parse(sig), exp);

    [Fact]
    public void Factorial34_IsExactOnTheSdidi()
    {
        // 34! = 295232799039604140847618609643520000000 (39 digits, 7 trailing zeros): the per-multiply
        // Round34 only ever drops trailing zeros, landing the 34-digit significand at 10^5 — exact in any mode.
        var expected = D("2952327990396041408476186096435200", 5);
        Assert.Equal(expected, CobolIntrinsics.FactorialDec(CobolRounding.NearestAwayFromZero, 34));
        Assert.Equal(expected, CobolIntrinsics.FactorialDec(CobolRounding.Truncation, 34));
    }

    [Fact]
    public void Factorial_ZeroAndOne_AreOne()
    {
        Assert.Equal(D("1", 0), CobolIntrinsics.FactorialDec(CobolRounding.NearestAwayFromZero, 0));
        Assert.Equal(D("1", 0), CobolIntrinsics.FactorialDec(CobolRounding.NearestAwayFromZero, 1));
    }

    [Fact]
    public void Factorial_Negative_IsTheArgumentErrorDefault()
    {
        // §15.36.3 r1 via EC-ARGUMENT-FUNCTION → the §15.3 default 0 (checking disabled in-process).
        Assert.Equal(D("0", 0), CobolIntrinsics.FactorialDec(CobolRounding.NearestAwayFromZero, -1));
    }

    [Fact]
    public void Factorial_PastDecimal128_RaisesSizeOverflow()
    {
        // The product exceeds 9.999…E+6144 near n ≈ 1755; the raise comes from CobolDec.Mul's range check
        // (§8.8.1.5.2 r2), never from an iteration cap.
        Assert.Throws<CobolSizeError>(() =>
            CobolIntrinsics.FactorialDec(CobolRounding.NearestAwayFromZero, 9999));
    }

    [Fact]
    public void NativeFactorial_PastInt128_RaisesSizeOverflow_NotArgumentError()
    {
        // The NATIVE half of PB125: 34 CONFORMS to §15.36.3 r1, so the old EC-ARGUMENT-FUNCTION default 0
        // was a silent wrong answer — the value exceeds the native Int128 intermediate, which is the SIZE
        // error class (CONFORMANCE.md items 70/179). 33! stays exact on the carrier.
        Assert.Throws<CobolSizeError>(() => CobolIntrinsics.Factorial(34));
        Assert.Equal(Int128.Parse("8683317618811886495518194401280000000"), CobolIntrinsics.Factorial(33));
    }
}
