// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// <see cref="ArithmeticModes"/> — the per-mode spec facts, asserted EXHAUSTIVELY over
/// <see cref="ArithmeticMode"/> rather than for the modes that happen to be reachable.
///
/// <para><b>What this replaces.</b> The NUMVAL digit cap was a two-state ternary
/// (<c>num.StandardDecimal ? 34 : ""</c>), so STANDARD-BINARY fell through the else-branch and would have taken
/// the NATIVE cap of 31 where §15.93.4 r1b sub-note 3 says 35. Unreachable — the mode is declined at bind — but
/// that is precisely the kind of entry that is never contradicted and so is never found wrong
/// (<c>feedback_a_dead_lookup_is_also_unverified</c>). The table now states all four modes and this test walks
/// the enum, so a fifth mode cannot be added and silently inherit somebody's else-branch.</para>
/// </summary>
public sealed class ArithmeticModeTableTests
{
    [Fact]
    public void NumvalDigitCap_IsTotalOverEveryArithmeticMode()
    {
        foreach (ArithmeticMode mode in Enum.GetValues<ArithmeticMode>())
        {
            int cap = ArithmeticModes.NumvalDigitCap(mode);   // throws for an unmapped mode — that IS the assertion
            Assert.InRange(cap, 31, 35);
        }
    }

    [Theory]
    // §15.93.4 r1b (and its §15.94.4 twin) states the cap in three sub-notes, one per mode:
    [InlineData(ArithmeticMode.Native, 31)]           //  2) "…greater than 31 digits is the 32nd digit…"
    [InlineData(ArithmeticMode.StandardBinary, 35)]   //  3) "…the argument has more than 35 digits…"
    [InlineData(ArithmeticMode.StandardDecimal, 34)]  //  4) "…the argument has more than 34 digits…"
    // Not named by the sub-notes: the 2002 STANDARD mode routes to the same SDIDI decimal engine, so it takes
    // STANDARD-DECIMAL's cap. Asserted so the reasoning is pinned and not re-derived from scratch each time.
    [InlineData(ArithmeticMode.Standard, 34)]
    public void NumvalDigitCap_PinnedToSpec(ArithmeticMode mode, int expected) =>
        Assert.Equal(expected, ArithmeticModes.NumvalDigitCap(mode));

    [Fact]
    public void DefaultDigitCap_IsTheNativeOne_SoOmissionMeansNative()
    {
        // The emitted call omits `digitCap:` when the mode's cap equals the runtime default. If these two ever
        // disagreed, every native compilation would silently emit — or silently omit — the wrong cap.
        Assert.Equal(ArithmeticModes.DefaultDigitCap, ArithmeticModes.NumvalDigitCap(ArithmeticMode.Native));
    }

    [Fact]
    public void StandardBinaryCap_IsUnreachable_TheOtherHalfOfTheDoubleDefence()
    {
        // The entry exists and is correct, and it must stay UNREACHABLE: the render lane is the second line of
        // defence behind the bind-time decline, never a substitute for it. If a compilation could reach the
        // renderer under StandardBinary, this cap would start deciding real output — so the assertion that
        // matters is the pair: the value is the spec's, AND the mode never survives binding
        // (ArithmeticModeScreenDriftTests proves the decline for every unit kind).
        Assert.Equal(35, ArithmeticModes.NumvalDigitCap(ArithmeticMode.StandardBinary));
        Assert.NotEqual(ArithmeticModes.DefaultDigitCap, ArithmeticModes.NumvalDigitCap(ArithmeticMode.StandardBinary));
    }
}
