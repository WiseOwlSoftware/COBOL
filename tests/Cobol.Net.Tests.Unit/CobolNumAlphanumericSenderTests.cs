// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The two value-level decodes of a character image (numeric design D25): <see cref="CobolNum.FromAlphanumeric"/>
/// carries ISO §14.9.25.4 GR6 d) 3's size rule for an ALPHANUMERIC SENDING OPERAND — "the rightmost 31 character
/// positions" — and <see cref="CobolNum.DigitMagnitude"/> carries no size rule at all, for a caller whose image
/// size its own data description already fixes. The boundary is tested HERE rather than only through COBOL
/// programs because it is an arithmetic property of the window (31 vs 32 characters, and CHARACTER positions vs
/// DIGIT characters) that a program-level golden can only reach one value at a time.
/// <para>The checked twin <c>FromAlphanumericSending</c> is NOT tested here on purpose: it reads the process-wide
/// <c>ExceptionState.DataIncompatibleChecking</c> flag, which the generated program sets, and toggling a global
/// from a parallel test assembly would make this file's verdict depend on execution order.
/// <c>MoveAlphanumericSenderTests</c> measures it the way a program does.</para>
/// </summary>
public sealed class CobolNumAlphanumericSenderTests
{
    private const string Digits40 = "1234567890123456789012345678901234567890";

    [Fact]
    public void FromAlphanumeric_OverThirtyOneCharacters_KeepsOnlyTheRightmostThirtyOne()
    {
        // The rightmost 31 characters of Digits40 are "0123456789012345678901234567890".
        Assert.Equal(Int128.Parse("123456789012345678901234567890"), CobolNum.FromAlphanumeric(Digits40));
        // …and the decode never reaches the carrier again: 31 digits is at most 10^31 - 1.
        Assert.True(CobolNum.FromAlphanumeric(new string('9', 40)) < Int128.MaxValue);
        Assert.Equal(Int128.Parse(new string('9', 31)), CobolNum.FromAlphanumeric(new string('9', 40)));
    }

    [Fact]
    public void FromAlphanumeric_AtExactlyThirtyOneCharacters_IsNotWindowed()
    {
        string at31 = Digits40[..31];
        Assert.Equal(Int128.Parse(at31), CobolNum.FromAlphanumeric(at31));
        // 32 characters: the leading one is dropped, and it is the ONE character that distinguishes the two.
        Assert.Equal(Int128.Parse(Digits40[1..32]), CobolNum.FromAlphanumeric(Digits40[..32]));
    }

    // ⛔ CHARACTER POSITIONS, NOT DIGIT CHARACTERS. §14.6.13.2 lets a non-digit position contribute no digit, but
    // it still OCCUPIES a position, so the window must be taken over the image and only then scanned. These two
    // images have the same digits in the same order and different answers.
    [Fact]
    public void FromAlphanumeric_CountsCharacterPositions_NotDigitCharacters()
    {
        Assert.Equal(Int128.Parse("456789012345678901234567890"),
            CobolNum.FromAlphanumeric("12X456789012345678901234567890ABCD"));
        Assert.Equal(Int128.Parse("12456789012345678901234567890"),
            CobolNum.FromAlphanumeric("12456789012345678901234567890"));
    }

    [Fact]
    public void FromAlphanumeric_KeepsTheDeterministicDecodeOfIncompatibleContent()
    {
        Assert.Equal(Int128.Zero, CobolNum.FromAlphanumeric(""));
        Assert.Equal(Int128.Zero, CobolNum.FromAlphanumeric("Q"));
        Assert.Equal(Int128.Zero, CobolNum.FromAlphanumeric("   "));
        Assert.Equal((Int128)1234, CobolNum.FromAlphanumeric("1A2B3C4D"));
    }

    // The rule-free decode: the same digit scan with no window, for an image whose size its own PICTURE fixes.
    // (Measured at 35 characters, not 40: a 40-digit value does not fit `Int128` at all — which is exactly the
    // carrier the missing size rule used to let an alphanumeric operand reach.)
    [Fact]
    public void DigitMagnitude_AppliesNoSizeRule()
    {
        string digits35 = Digits40[..35];
        Assert.Equal(Int128.Parse(digits35), CobolNum.DigitMagnitude(digits35));
        Assert.NotEqual(CobolNum.DigitMagnitude(digits35), CobolNum.FromAlphanumeric(digits35));
        Assert.Equal(Int128.Parse("1111111111111111111111111111111"),
            CobolNum.DigitMagnitude("1111111111111111111111111111111+"));   // 31 digits + a separate sign
        Assert.Equal(Int128.Zero, CobolNum.DigitMagnitude(""));
    }

    // The two agree wherever the size rule cannot fire — the property that makes the split safe for every caller
    // holding an image of 31 characters or fewer, which is every numeric item's own image.
    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("000000000000000000000000000003")]
    [InlineData("1234567890123456789012345678901")]
    [InlineData("12X45")]
    public void TheTwoDecodesAgreeAtOrBelowTheCap(string image)
        => Assert.Equal(CobolNum.DigitMagnitude(image), CobolNum.FromAlphanumeric(image));

    [Fact]
    public void TheCapIsTheStandardsThirtyOne()
        => Assert.Equal(31, CobolNum.MaxSendingCharacterPositions);
}
