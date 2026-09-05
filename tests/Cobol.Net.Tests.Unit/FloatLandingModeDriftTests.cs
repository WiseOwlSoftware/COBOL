// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.CodeGen;
using CobolNet.Runtime;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE FLOAT→FIXED QUANTIZER TAKES THE STATEMENT'S ROUNDING DECISION, AND TAKES IT ONCE (kb/Work PB647).
/// <para>
/// The arithmetic channel's quantizer (<c>CobolIntrinsics.FromDouble</c>) hard-coded
/// <see cref="CobolRounding.NearestAwayFromZero"/> at a working scale while the MOVE channel
/// (<c>CobolFloat.ToScaledUnchecked</c>) had always taken the receiver's mode at the receiver's scale, so ONE
/// returned value landed TWO ways in ONE receiver — <c>MOVE FUNCTION SQRT(3) TO S</c> gave 1.732050807 into
/// <c>PIC 9V9(9)</c> and <c>COMPUTE S = FUNCTION SQRT(3)</c> gave 1.732050808 — and the ROUNDED phrase was a
/// no-op on the whole §15.4.1 float family. ISO §15.4.1 forbids the split outright; §14.7.4.1 puts the
/// truncation "relative to the size provided for the resultant identifier"; §14.7.4.3 rule 2 makes a no-phrase
/// store ROUNDED MODE IS TRUNCATION.
/// </para>
/// <para>
/// <see cref="ReceiverContext.FloatLanding"/> is the fix, and this class is its proof. The corpus goldens
/// <c>pb647_float_landing_mode</c> (2002/2014/2023) pin the BEHAVIOUR at a handful of receivers;
/// these pin the RULE over every legal picture shape and every one of the eight modes — which is what stops a
/// second mode from being written down somewhere else again.
/// </para>
/// </summary>
public sealed class FloatLandingModeDriftTests
{
    /// <summary>Every (integer-digits, scale) pair a legal PICTURE can present (ISO §13.18.40.3 SR14 caps the
    /// total digit positions at 31) crossed with every rounding mode §14.7.4.2's format admits.</summary>
    public static TheoryData<int, int, CobolRounding> LegalShapesAndModes()
    {
        var data = new TheoryData<int, int, CobolRounding>();
        for (int intDigits = 0; intDigits <= 31; intDigits += 1)
            for (int scale = 0; scale + intDigits <= 31; scale += 1)
                foreach (CobolRounding mode in Enum.GetValues<CobolRounding>())
                    data.Add(intDigits, scale, mode);
        return data;
    }

    private static ReceiverContext Rcv(int intDigits, int scale, CobolRounding mode) =>
        new(scale, Real: false, mode, InSizeError: false, IntegerDigits: intDigits);

    /// <summary>
    /// INVARIANT 1 — THE FINAL TRANSFER LANDS AT THE RESULTANT IDENTIFIER'S OWN SCALE, WITH ITS OWN MODE.
    /// That is §14.7.4.1 ("truncation is relative to the size provided for the resultant identifier") plus
    /// §14.7.4.3's per-mode rules, each of which names "the resultant identifier". It is also what makes the
    /// arithmetic channel's landing IDENTICAL to the MOVE channel's, which lands at the receiver's scale with
    /// the receiver's mode — the §15.4.1 identity the defect broke.
    /// </summary>
    [Theory]
    [MemberData(nameof(LegalShapesAndModes))]
    public void FinalTransfer_LandsAtTheReceiversOwnScaleAndMode(int intDigits, int scale, CobolRounding mode)
    {
        var rcv = Rcv(intDigits, scale, mode);
        Assert.Equal((scale, mode), rcv.FloatLanding(finalTransfer: true));
    }

    /// <summary>
    /// INVARIANT 2 — AND SO THE STORE'S RESCALE IS THE IDENTITY, WHICH IS WHY THERE CAN BE NO SECOND ROUNDING.
    /// A landing at a WORKING scale above the receiver's is followed by a rescale down that rounds AGAIN, and two
    /// roundings of one value do not compose (round-then-round differs from rounding once for every tie-breaking
    /// mode). Landing AT the receiver's scale removes the second rounding by construction rather than by
    /// argument. Stated separately from invariant 1 because it is the CONSEQUENCE that matters at the store, and
    /// a future change that keeps "the receiver's mode" but restores a working scale would still be a defect.
    /// </summary>
    [Theory]
    [MemberData(nameof(LegalShapesAndModes))]
    public void FinalTransfer_MakesTheStoresRescaleTheIdentity(int intDigits, int scale, CobolRounding mode) =>
        Assert.Equal(scale, Rcv(intDigits, scale, mode).FloatLanding(finalTransfer: true).Scale);

    /// <summary>
    /// INVARIANT 3 — A NESTED INTERMEDIATE NEVER INHERITS THE RECEIVER'S MODE. The ROUNDED phrase binds to the
    /// transfer into the resultant identifier (§14.7.4.3 rules 3–10 each say "the resultant identifier"), and the
    /// single receiver store performs it; an operand feeding a larger expression must therefore land TRUNCATED,
    /// which is the rule <c>NumericRenderer.Align</c> and <c>Divide</c>'s nested arm already state for every
    /// other float→fixed intermediate. Without it, <c>COMPUTE R ROUNDED = FUNCTION SQRT(3) * 1</c> would round
    /// at the intermediate AND at the receiver.
    /// </summary>
    [Theory]
    [MemberData(nameof(LegalShapesAndModes))]
    public void NestedIntermediate_NeverInheritsTheReceiversMode(int intDigits, int scale, CobolRounding mode)
    {
        var rcv = Rcv(intDigits, scale, mode);
        Assert.Equal((rcv.FloatWorkingScale, CobolRounding.Truncation), rcv.FloatLanding(finalTransfer: false));
    }

    /// <summary>
    /// INVARIANT 4 — A FLOATING-POINT NUMERIC-EDITED RESULTANT IS NEVER A FINAL TRANSFER for this purpose. It has
    /// no fixed fraction scale to round at — the result normalizes into the mask and the significand is truncated
    /// to the mask's digits (data-model design D21 / kb/Work PB66) — so <c>ReceiverContext.Scale</c> carries the
    /// mask's significand scale as a WORKING-scale hint only. Landing a final transfer at it would truncate the
    /// value before the mask's own normalization ever saw the small magnitudes it exists to express.
    /// </summary>
    [Theory]
    [MemberData(nameof(LegalShapesAndModes))]
    public void AFloatEditedResultant_KeepsTheWorkingScaleAndTruncates(int intDigits, int scale, CobolRounding mode)
    {
        var rcv = Rcv(intDigits, scale, mode) with { FloatEdited = true };
        Assert.Equal((rcv.FloatWorkingScale, CobolRounding.Truncation), rcv.FloatLanding(finalTransfer: true));
    }

    /// <summary>
    /// INVARIANT 5 — AND THE RUNTIME LANDING NAMES NO MODE OF ITS OWN. This is the drift that re-opens the
    /// defect: the mode became a parameter, but a body that still writes <c>CobolRounding.Something</c> into the
    /// quantization silently overrides it, and no golden would say so — the value would simply be wrong in one
    /// channel again. The ONE mode constant this file may name is <see cref="CobolRounding.Prohibited"/>, which
    /// it names to TEST for (the §14.7.4.3 rule 7 raise), never to quantize with.
    /// </summary>
    [Fact]
    public void TheRuntimeQuantizer_HardCodesNoRoundingMode()
    {
        string src = File.ReadAllText(TestRepo.Src("Cobol.Net.Runtime", "Intrinsics", "CobolIntrinsics.cs"));
        // Strip doc comments and line comments: the prose explains the retired hard-coding by name, and the
        // scan is about CODE. (A block comment would be stripped the same way; the file uses neither.)
        string code = string.Join('\n', src.Split('\n').Where(l => !l.TrimStart().StartsWith("//")));
        var named = Regex.Matches(code, @"CobolRounding\.(\w+)").Select(m => m.Groups[1].Value).Distinct().ToList();
        Assert.Equal(["Prohibited"], named);
    }
}
