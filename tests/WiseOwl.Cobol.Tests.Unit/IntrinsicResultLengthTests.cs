// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The returned-value LENGTH the catalog states for the alphanumeric functions of the 1989 intrinsic function module (kb/Work
/// PB5425; <see cref="IntrinsicResultLength"/>). "The number of character positions in this data item is specified in the
/// function definition" (§1989:A.2.3 GR1), and the module's eight alphanumeric functions are a closed set: CHAR returns "a
/// one-character alphanumeric value" (§1989:A.2.9.1), CURRENT-DATE "a 21-character alphanumeric value" (§1989:A.2.11.1),
/// WHEN-COMPILED the 21 positions of its table (§1989:A.2.46.3), LOWER-CASE, UPPER-CASE and REVERSE a string "the same length as
/// argument-1" (§1989:A.2.22.1, §1989:A.2.44.1, §1989:A.2.38.1), and MAX and MIN the value of the argument they select, whose
/// length is the run time's. A row that lost its length would let a mis-sized INSPECT operand through the 1985 size rules
/// (§1985:VI.6.18.3 SR7 to SR9) silently, so each is pinned.
/// </summary>
public sealed class IntrinsicResultLengthTests
{
    [Theory]
    [InlineData("CHAR", 1)]
    [InlineData("CURRENT-DATE", 21)]
    [InlineData("WHEN-COMPILED", 21)]
    public void A1989FunctionOfFixedLength_StatesItsPositions(string name, int positions)
    {
        Assert.True(IntrinsicCatalog.TryGet(name, out var sig));
        Assert.Equal(IntrinsicResultLength.Fixed(positions), sig.Length);
    }

    [Theory]
    [InlineData("LOWER-CASE")]
    [InlineData("UPPER-CASE")]
    [InlineData("REVERSE")]
    public void A1989CaseOrOrderFunction_IsAsLongAsArgument1(string name)
    {
        Assert.True(IntrinsicCatalog.TryGet(name, out var sig));
        Assert.Equal(IntrinsicResultLength.SameAsArgument1, sig.Length);
    }

    [Theory]
    [InlineData("MAX")]
    [InlineData("MIN")]
    public void A1989SelectingFunction_StatesNoLength(string name)
    {
        Assert.True(IntrinsicCatalog.TryGet(name, out var sig));
        Assert.Equal(default, sig.Length);
    }
}
