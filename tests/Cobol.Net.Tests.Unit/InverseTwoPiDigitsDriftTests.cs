// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Numerics;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ <c>CobolIntrinsics.InverseTwoPiDigits</c> is the ONE wide datum behind SIN / COS / TAN of an exact argument
/// (kb/Work PB999, PB1041): the runtime reduces to a quadrant and residue on the Int128 fixed point by reading
/// limbs of this digit string (COBOLNET_DESIGN §1.2 invariant 2 keeps <c>BigInteger</c> out of the runtime), so a
/// wrong digit is a wrong sine for every argument whose reduction reads it. The constant is re-derived here
/// independently — 1/(2π) by Machin's formula over a <c>BigInteger</c> fixed point, a test-only type — and its
/// length is pinned to the SDIDI's range (ISO §8.8.1.5.2: the largest magnitude is 9.999…E+6144, so a significand
/// digit sits at 10^6144 at most and the reduction reads <c>CobolIntrinsics.ReductionPlaces</c> places past it).
/// </summary>
public sealed class InverseTwoPiDigitsDriftTests
{
    private const int TopPlace = 6144;   // §8.8.1.5.2 — 9.999…E+6144

    [Fact]
    public void TheDigitString_CoversEveryPlaceAnSdidiDigitCanOccupy()
    {
        string d = CobolIntrinsics.InverseTwoPiDigits;
        Assert.True(d.Length >= TopPlace + CobolIntrinsics.ReductionPlaces, $"{d.Length} digits cannot serve a reduction at 10^{TopPlace}");
        Assert.All(d, c => Assert.InRange(c, '0', '9'));
    }

    [Fact]
    public void TheDigitString_IsOneOverTwoPi()
    {
        string d = CobolIntrinsics.InverseTwoPiDigits;
        const int guard = 20;
        BigInteger unit = BigInteger.Pow(10, d.Length + guard);
        BigInteger pi = 16 * ArcTanInverse(5, unit) - 4 * ArcTanInverse(239, unit);   // π · 10^(n+guard)
        string want = (unit * unit / (2 * pi)).ToString().PadLeft(d.Length + guard, '0')[..d.Length];
        int first = Enumerable.Range(0, d.Length).FirstOrDefault(i => d[i] != want[i], -1);
        Assert.True(first < 0, $"InverseTwoPiDigits differs from 1/(2π) at place {first + 1}");
    }

    private static BigInteger ArcTanInverse(int n, BigInteger unit)
    {
        BigInteger power = unit / n, sum = power, n2 = n * n;
        for (int i = 1; !power.IsZero; i++)
        {
            power /= n2;
            BigInteger term = power / (2 * i + 1);
            sum += (i & 1) == 1 ? -term : term;
        }
        return sum;
    }
}
