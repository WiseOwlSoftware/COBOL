// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE CALLER NAMES A CARRIER TYPE FROM <c>DataItem.ElementType</c> AND THE CALLEE READS IT BY A TYPE SWITCH —
/// SO THE TWO SETS ARE ONE SET, AND NOTHING TELLS THEM APART AT COMPILE TIME (kb/Work PB238).
/// </summary>
/// <remarks>
/// <para>
/// <c>CallEmitter.ArgText</c> emits <c>ManagedPointer&lt;{CallNumCarrier(p)}&gt;.Cell(…)</c> for every place the
/// character route (<c>CallPlaceIsString</c>) declines, and <c>CallNumCarrier</c> is <c>DataItem.ElementType</c>
/// — the item's own field type. The callee side is <c>CobolArgAdapt</c>'s private <c>ReadNumericCell</c> /
/// <c>ReadRealCell</c> type switches. A carrier the caller can name and the callee cannot read does not fail to
/// compile and does not throw at the boundary: the adapter falls to <c>Omitted&lt;T&gt;</c>, the §14.9.4.4 GR12
/// carrier, whose read RAISES EC-PROGRAM-ARG-OMITTED and — when checking for it is not enabled, which is the
/// default — returns the benign <c>default</c>, i.e. ZERO. So an unreadable carrier is a SILENT zero, not a
/// crash (MEASURED: the last assertion below was first written as <c>ThrowsAny</c> and was red). That is the
/// hole PB238 found — <c>double</c> was on neither side, so a float leaf was routed onto the CHARACTER image
/// and decoded with <c>CobolNum.ParseDisplay</c> through the receiver's zoned profile instead.
/// </para>
/// <para>
/// ⚠ This is the drift guard the fix is paired with, not a restatement of it. The SIX carriers below are the
/// whole numeric vocabulary of the boundary: kb/Work R12's four integer tiers (<c>long</c>, <c>ulong</c>,
/// <c>Int128</c>, <c>UInt128</c> — the >18-digit and unsigned-binary containers) plus PB238's float lane
/// (<c>double</c>, <c>float</c>). Adding a seventh to <c>DataItem.ElementType</c> without an arm in
/// <c>CobolArgAdapt</c> is silent today and is what the last assertion pins as a BOUNDARY rather than an
/// oversight.
/// </para>
/// <para>
/// PROVEN TO FAIL before being trusted: deleting the <c>ManagedPointer&lt;float&gt;</c> arm of
/// <c>ReadRealCell</c> makes the <c>float</c> theory case red (it reads 0 instead of 150), and deleting the
/// <c>ManagedPointer&lt;ulong&gt;</c> arm of <c>ReadNumericCell</c> makes the <c>ulong</c> case red.
/// </para>
/// </remarks>
public sealed class CallAbiNumericCarrierDriftTests
{
    /// <summary>The callee's profile for a <c>PIC S9(5)V99</c> BY VALUE formal — 7 digits, scale 2.</summary>
    private static NumProfile Formal => new()
    {
        Digits = 7,
        FractionDigits = 2,
        Signed = true,
        Truncation = NumericTruncation.DigitCount,
        ByteForm = NumericByteForm.Binary,
    };

    /// <summary>Every carrier the CALL boundary may name for a numeric argument reaches the callee as its VALUE.
    /// <para>Each case is the same COBOL value, 1.50, in that carrier's own terms — the four integer tiers carry
    /// the UNSCALED 150 with <c>CobolArg.Scale</c> 2 (ISO §14.2.3 GR10's "COMPUTE statement without the ROUNDED
    /// phrase" then rescales to the formal's own scale, which here is the identity), and the two float carriers
    /// carry the binary64/32 value 1.5 with no scale meta at all, because a binary floating-point item has no
    /// PICTURE digits and the quantization is the RECEIVER's.</para></summary>
    [Theory]
    [InlineData("long")]
    [InlineData("ulong")]
    [InlineData("Int128")]
    [InlineData("UInt128")]
    [InlineData("double")]
    [InlineData("float")]
    public void EveryNamedCarrier_ReachesTheCalleeAsItsValue(string carrier)
    {
        (ManagedPointer Carrier, int Scale) arg = carrier switch
        {
            "long" => (ManagedPointer<long>.Cell(150L), 2),
            "ulong" => (ManagedPointer<ulong>.Cell(150UL), 2),
            "Int128" => (ManagedPointer<Int128>.Cell((Int128)150), 2),
            "UInt128" => (ManagedPointer<UInt128>.Cell((UInt128)150), 2),
            "double" => (ManagedPointer<double>.Cell(1.5d), 0),
            "float" => (ManagedPointer<float>.Cell(1.5f), 0),
            _ => throw new ArgumentOutOfRangeException(nameof(carrier)),
        };
        var args = new[] { new CobolArg(CobolPassMode.Value, arg.Carrier, 7, arg.Scale) };
        Assert.Equal(150L, CobolArgAdapt.NumValue<long>(args, 0, Formal, 2).Value);
    }

    /// <summary>A carrier OUTSIDE the six degrades to the §14.9.4.4 GR12 OMITTED carrier — value ZERO, with
    /// EC-PROGRAM-ARG-OMITTED raised but not fatal unless checking is enabled. The value is asserted so the
    /// test FAILS the day a seventh carrier is added to <c>DataItem.ElementType</c> without an arm in
    /// <c>CobolArgAdapt</c>: the argument would start reading as its real value and this expectation would go
    /// red, which is the whole point. Asserting the silence rather than a throw is what the measurement said —
    /// see the remarks; the softness of that fallback is itself worth knowing at this boundary.</summary>
    [Fact]
    public void ACarrierOutsideTheSix_DegradesToTheOmittedCarrier()
    {
        var args = new[] { new CobolArg(CobolPassMode.Value, ManagedPointer<decimal>.Cell(1.5m), 7, 2) };
        Assert.Equal(0L, CobolArgAdapt.NumValue<long>(args, 0, Formal, 2).Value);
    }

    /// <summary>The callee's profile for a <c>PIC S9(9)V9(9)</c> formal — 18 digits, scale 9 (kb/Work PB288).</summary>
    private static NumProfile Wide => new()
    {
        Digits = 18,
        FractionDigits = 9,
        Signed = true,
        Truncation = NumericTruncation.DigitCount,
        ByteForm = NumericByteForm.Binary,
    };

    /// <summary>⛔ THE TWO ARMS ARE ONE RULE (kb/Work PB288). ISO §14.2.3 GR9 and GR10 describe the SAME
    /// conversion — "if the formal parameter is numeric, a COMPUTE statement without the ROUNDED phrase" into a
    /// record of the formal's description — and GR11 resolves every reference to the formal through that same
    /// description, so <see cref="CobolArgAdapt.Num"/>'s GR8 aliasing view and
    /// <see cref="CobolArgAdapt.NumValue"/>'s GR10 detached copy owe the IDENTICAL value for the identical
    /// argument. They did not: <c>Num</c> rescaled with the unchecked <c>CobolNum.Rescale</c> straight into
    /// <c>T.CreateTruncating</c> — two binary wraps in series, no digit-capacity landing — while <c>NumValue</c>
    /// composed with <c>CobolNum.Store</c>. 10^30 crossing to a <c>PIC S9(9)V9(9)</c> formal arrived as
    /// 873995514006732800 through one arm and −123822295304634368 through the other; the §14.7.5 case-3
    /// no-phrase disposition (CONFORMANCE.md DOC-A.1-70) is the result's LOW-ORDER digits, 0.
    /// <para>PROVEN TO FAIL: reverting either <c>CobolArgAdapt.Land</c> call site to the bare
    /// <c>CobolNum.Rescale</c> / <c>CobolFloat.ToScaledUnchecked</c> makes the matching row red.</para></summary>
    [Theory]
    // The 31-digit fixed-point argument (§8.3.3.3.2 admits 1 through 31 digits) at scale 0. Aligned at the
    // formal's scale 9 the result is 10^39; the formal's 18 digit positions keep its low-order 18 — all zero.
    [InlineData("Int128-10e30", 0L)]
    // The binary64 nearest 10^30 is exactly 1000000000000000019884624838656; at scale 9 that is
    // 1000000000000000019884624838656000000000, whose low-order 18 digits are 624838656000000000.
    [InlineData("double-1e30", 624838656000000000L)]
    // An 18-digit argument at scale 0: aligned at scale 9 it is 123456789012345678000000000 (27 digits),
    // whose low-order 18 are 012345678000000000 — i.e. the formal shows 12345678.000000000.
    [InlineData("Int128-18digit", 12345678000000000L)]
    // A value the formal HOLDS — the control that keeps the landing from degenerating to a blanket zero.
    [InlineData("Int128-inrange", 123456000000L)]
    public void TheGr8ViewAndTheGr10Copy_LandIdentically(string shape, long expected)
    {
        (ManagedPointer Carrier, int Scale) arg = shape switch
        {
            "Int128-10e30" => (ManagedPointer<Int128>.Cell(Int128.Parse("1000000000000000000000000000000")), 0),
            "double-1e30" => (ManagedPointer<double>.Cell(1.0e30d), 0),
            "Int128-18digit" => (ManagedPointer<Int128>.Cell((Int128)123456789012345678L), 0),
            "Int128-inrange" => (ManagedPointer<Int128>.Cell((Int128)123456), 3),
            _ => throw new ArgumentOutOfRangeException(nameof(shape)),
        };
        var byRef = new[] { new CobolArg(CobolPassMode.Content, arg.Carrier, 38, arg.Scale) };
        var byValue = new[] { new CobolArg(CobolPassMode.Value, arg.Carrier, 38, arg.Scale) };
        long view = CobolArgAdapt.Num<long>(byRef, 0, Wide, 9).Value;
        long copy = CobolArgAdapt.NumValue<long>(byValue, 0, Wide, 9).Value;
        Assert.Equal(expected, view);
        Assert.Equal(expected, copy);
    }
}
