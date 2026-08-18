// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
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
    private static (double, double, double) FirstThree(long seed)
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
}
