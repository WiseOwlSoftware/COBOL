// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.Text.RegularExpressions;
using WiseOwl.Cobol.Compiler.Binding.Bound;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ WHICH FUNCTION ARGUMENTS ARITHMETIC IS STANDARD ROUNDS TO 31 DIGITS IS READ FROM THE STANDARD, NOT REMEMBERED (kb/Work PB5575).
/// §2002:8.8.1.3.1.3 situation 2 (and §2014:8.8.1.3.1.3.1) rounds a standard intermediate data item that is the argument of a function
/// "and there is no equivalent arithmetic expression defined for the rules of the function", so
/// <see cref="IntrinsicCarrierRules.StandardArgumentKeepsAllDigits"/> must hold exactly the functions whose 2002 or 2014 definition
/// gives one, plus the stated exceptions (SQRT's "argument-1 is not rounded", and situation 1's comparison among the arguments: MAX,
/// MIN, ORD-MAX, ORD-MIN). This test reads chapter 15 of both editions' text and fails when a function gains or loses an equivalent
/// arithmetic expression without the set following it.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class StandardArgumentRoundingDriftTests
{
    private static readonly string[] Exceptions = ["SQRT", "MAX", "MIN", "ORD-MAX", "ORD-MIN"];

    /// <summary>The names of the chapter-15 functions whose section of an edition's text (<paramref name="lines"/>) states an equivalent arithmetic
    /// expression, and of those whose section says the argument "is not rounded".</summary>
    private static (HashSet<string> Eae, HashSet<string> NotRounded) Read(string[] lines)
    {
        var heading = new Regex(@"^#### 15\.(\d+) (.+) function$");
        HashSet<string> eae = new(StringComparer.Ordinal), notRounded = new(StringComparer.Ordinal);
        string? current = null;
        foreach (string line in lines)
        {
            if (line.StartsWith("#### ", StringComparison.Ordinal))
            {
                // A function's section runs to the next heading of its level; every other level-4 heading ends it.
                Match m = heading.Match(line);
                current = m.Success ? m.Groups[2].Value : null;
                continue;
            }
            if (current is null) continue;
            if (line.Contains("equivalent arithmetic expression", StringComparison.Ordinal)) eae.Add(current);
            if (line.Contains("argument-1 is not rounded", StringComparison.Ordinal)) notRounded.Add(current);
        }
        return (eae, notRounded);
    }

    [Fact]
    public void TheArgumentsThatKeepAllDigits_AreTheEquivalentExpressionFunctionsOfBothEditions_AndTheStatedExceptions()
    {
        var (eae02, notRounded02) = Read(File.ReadAllLines(TestRepo.Specs("ISO_COBOL_2002.md")));
        var (eae14, notRounded14) = Read(File.ReadAllLines(TestRepo.Specs("ISO_COBOL_2014.md")));
        Assert.True(eae02.Count > 20 && eae14.Count > 20, $"the chapter-15 reader found {eae02.Count} and {eae14.Count} functions");
        // SQRT's text names the equivalent arithmetic expression only to say it has none; it is an exception, not an EAE member.
        var expected = new SortedSet<string>(eae02.Union(eae14).Union(Exceptions), StringComparer.Ordinal);
        Assert.Equal(expected, new SortedSet<string>(IntrinsicCarrierRules.StandardArgumentKeepsAllDigits, StringComparer.Ordinal));
        // The stated "not rounded" functions: SQRT in both editions, and INTEGER in 2002 alone, the one edition edge.
        Assert.Equal(["INTEGER", "SQRT"], notRounded02.Order(StringComparer.Ordinal));
        Assert.Equal(["SQRT"], notRounded14.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void IntegerKeepsItsArgumentAt2002_AndRoundsItFrom2014()
    {
        Assert.False(IntrinsicCarrierRules.RoundsStandardArgument("INTEGER", 2002));
        Assert.True(IntrinsicCarrierRules.RoundsStandardArgument("INTEGER", 2014));
        Assert.True(DialectBehaviors.IsActive(DialectBehavior.IntegerArgumentRoundedUnderStandardArithmetic, 2014));
        Assert.True(IntrinsicCarrierRules.RoundsStandardArgument("DATE-OF-INTEGER", 2002));
        Assert.False(IntrinsicCarrierRules.RoundsStandardArgument("INTEGER-PART", 2002));
    }
}
