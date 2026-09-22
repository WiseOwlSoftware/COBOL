// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using CobolNet.Runtime.Exceptions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ISO §15.75.4 r3 — "The implementor shall specify the subset of the domain of argument-1 values that will yield
/// distinct sequences of pseudo-random numbers. This subset shall include the values from 0 through at least
/// 32767." Annex A.1 item 145 requires that subset to be DOCUMENTED (docs/CONFORMANCE.md §7 item 145), and a
/// documented determination is worth exactly what was measured (kb/Work PB65, RV-15.75.4-3): this test IS the
/// measurement the row cites — the first three draws of every seed 0..65,535 are pairwise distinct across the
/// whole set (so the required 0..32,767 floor holds with a margin), and a seed at or above 2³¹ selects the same
/// sequence as its masked value (<c>seed &amp; 0x7FFFFFFF</c> — the documented wide-seed mapping).
/// </summary>
public sealed class RandomSeedSubsetTests
{
    private static (double, double, double) FirstThree(Int128 seed)
    {
        double a = CobolIntrinsics.Random(seed);
        double b = CobolIntrinsics.Random();
        double c = CobolIntrinsics.Random();
        return (a, b, c);
    }

    [Fact]
    public void Seeds_0_Through_65535_YieldPairwiseDistinctSequences()
    {
        var seen = new Dictionary<(double, double, double), long>(65536);
        for (long seed = 0; seed <= 65535; seed++)
        {
            var key = FirstThree(seed);
            if (seen.TryGetValue(key, out long other))
                Assert.Fail($"seeds {other} and {seed} yield the same first three draws — the §15.75.4 r3 subset must not contain both");
            seen[key] = seed;
        }
        Assert.Equal(65536, seen.Count);
    }

    [Fact]
    public void SeedsAtOrAbove2Pow31_AliasToTheirMaskedValue()
    {
        // The documented wide-seed mapping (A.1 item 145): the generator's int seed is `seed & 0x7FFFFFFF`.
        Assert.Equal(FirstThree(0), FirstThree(2147483648L));
        Assert.Equal(FirstThree(1), FirstThree(2147483649L));
        Assert.Equal(FirstThree(32767), FirstThree(2147483648L + 32767));
    }

    /// <summary>
    /// kb/Work PB636 — the seed is a TOTAL argument: §15.75.3 r2 constrains the SIGN alone and §15.75.4 r3
    /// makes the distinct-sequence subset a FLOOR, not a domain, so §15.3's closing paragraph has no incorrect
    /// value to raise on. A seed past <c>long</c> therefore has to ANSWER — in the §15.75.4 r1 range and
    /// reproducibly (r2) — with EC-ARGUMENT-FUNCTION checking ARMED, which is the leg that used to terminate
    /// the run unit. The mapping is the SAME documented one: the wide seed aliases to its low 31 bits.
    /// </summary>
    [Fact]
    public void SeedsPastLong_AnswerInRange_AndRaiseNothingUnderChecking()
    {
        Int128 big19 = Int128.Parse("9999999999999999999");                    // > long.MaxValue
        Int128 b31 = Int128.Parse("1234567890123456789012345678901");          // §13.18.40.3 r14's widest numeric item
        bool saved = ExceptionState.ArgumentFunctionChecking;
        ExceptionState.ArgumentFunctionChecking = true;
        try
        {
            foreach (Int128 seed in new[] { big19, b31, Int128.MaxValue })
            {
                double v = CobolIntrinsics.Random(seed);                        // §15.75.4 r1 — [0, 1)
                Assert.True(v >= 0 && v < 1, $"RANDOM({seed}) returned {v}, outside §15.75.4 r1's [0, 1)");
                Assert.Equal(v, CobolIntrinsics.Random(seed));                  // §15.75.4 r2 — same seed, same sequence
                // The documented reduction (A.1 item 145), asserted on the WIDE carrier too.
                Assert.Equal(FirstThree(seed & 0x7FFFFFFF), FirstThree(seed));
            }
        }
        finally { ExceptionState.ArgumentFunctionChecking = saved; }
    }

    /// <summary>⛔ THE COMPLEMENT: widening the intake must not widen the one rule that DOES constrain this
    /// argument. §15.75.3 r2's sign is still an incorrect value, so a negative seed — of any width — still sets
    /// EC-ARGUMENT-FUNCTION (fatal under checking; kb/Work PB65's raise site, PB636's guard against losing it).</summary>
    [Fact]
    public void ANegativeSeed_StillRaises_AtEveryWidth()
    {
        bool saved = ExceptionState.ArgumentFunctionChecking;
        ExceptionState.ArgumentFunctionChecking = true;
        try
        {
            foreach (Int128 seed in new[] { (Int128)(-5), Int128.Parse("-9999999999999999999"), Int128.MinValue })
            {
                var e = Assert.Throws<CobolFatalException>(() => CobolIntrinsics.Random(seed));
                Assert.Equal("EC-ARGUMENT-FUNCTION", e.EcName);
                Assert.Contains("15.75.3 r2", e.Message);
            }
        }
        finally { ExceptionState.ArgumentFunctionChecking = saved; }
    }
}
