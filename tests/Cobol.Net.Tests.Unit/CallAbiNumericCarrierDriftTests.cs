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
}
