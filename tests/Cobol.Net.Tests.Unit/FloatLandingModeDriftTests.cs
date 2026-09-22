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
        Assert.Equal(FloatLandingDecision.At(scale, mode), rcv.FloatLanding(finalTransfer: true));
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
    /// INVARIANT 3 — A NESTED INTERMEDIATE IS NOT QUANTIZED AT ALL; IT KEEPS ITS BINARY64 (kb/Work PB653).
    /// It never inherits the receiver's MODE, because the ROUNDED phrase binds to the transfer into the resultant
    /// identifier (§14.7.4.3 rules 3–10 each say "the resultant identifier") and the single receiver store
    /// performs it — but it must not inherit a receiver-derived SCALE either. A ≥9 working scale truncates the
    /// returned value one digit too early and every operation above it propagates the loss:
    /// <c>COMPUTE R = FUNCTION SQRT(3) * 2</c> into <c>PIC 9V9(9)</c> gave 3.464101614 where a <c>COMP-2</c> item
    /// holding the IDENTICAL binary64 gave 3.464101615, which §15.4.1 forbids ("the returned value is the same
    /// for all instances of a given function within a single execution of the runtime element so long as the
    /// value and order of the arguments, the collating sequence, and the locale are unchanged").
    /// <para>The alternative — guard digits, the way <c>Divide</c>'s nested arm carries them — is what this
    /// invariant exists to REFUSE: a landing has no bound on how many multiplications sit above it, so a guarded
    /// scale accumulates until the Int128 carrier wraps (two scale-23 operands multiply to scale 46). Keeping the
    /// binary64 is the shape that has no tuning constant in it at all.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(LegalShapesAndModes))]
    public void NestedIntermediate_KeepsItsBinary64AndQuantizesNothing(int intDigits, int scale, CobolRounding mode) =>
        Assert.Equal(FloatLandingDecision.Binary64, Rcv(intDigits, scale, mode).FloatLanding(finalTransfer: false));

    /// <summary>
    /// INVARIANT 3b — AND SO DOES A FLOAT RECEIVER AND A RECEIVER-LESS RENDER, THROUGH THIS SAME DECISION.
    /// Both consumers used to spell the receiver-shape test out for themselves —
    /// <c>if (Receiver.Real || Receiver.Receiverless)</c> in <c>IntrinsicRenderer.RenderFloatNative</c>,
    /// <c>if (b.Real || e.Real || _rcv.Real || _rcv.Receiverless)</c> in <c>NumericRenderer.Power</c> — which is
    /// precisely why neither of them learned about the nested case when PB647 added it (kb/Work PB653,
    /// feedback_two_arm_dispatch). Folding the shape test INTO the decision is what makes the next consumer
    /// inherit the whole rule; this invariant is what keeps it folded in.
    /// </summary>
    [Theory]
    [MemberData(nameof(LegalShapesAndModes))]
    public void AFloatOrReceiverlessReceiver_QuantizesNothing_EvenOnTheFinalTransfer(int intDigits, int scale, CobolRounding mode)
    {
        Assert.Equal(FloatLandingDecision.Binary64,
            (Rcv(intDigits, scale, mode) with { Real = true }).FloatLanding(finalTransfer: true));
        Assert.Equal(FloatLandingDecision.Binary64,
            (Rcv(intDigits, scale, mode) with { Receiverless = true }).FloatLanding(finalTransfer: true));
        Assert.Equal(FloatLandingDecision.Binary64, ReceiverContext.None.FloatLanding(finalTransfer: true));
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
        Assert.Equal(FloatLandingDecision.At(rcv.FloatWorkingScale, CobolRounding.Truncation),
            rcv.FloatLanding(finalTransfer: true));
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

    /// <summary>
    /// INVARIANT 6 — AND NEITHER CONSUMER RE-DERIVES THE DECISION'S RECEIVER-SHAPE HALF (kb/Work PB653).
    /// This is the drift that produced the defect in the first place: <c>FloatLanding</c> returned a bare
    /// <c>(scale, mode)</c> pair, so "do not quantize at all" had nowhere to live and each consumer tested
    /// <c>Receiver.Real</c> / <c>Receiver.Receiverless</c> for itself — two private copies of half a rule, and
    /// when PB647 added the nested case to <c>FloatLanding</c>, neither copy inherited it. A consumer that names
    /// those receiver flags again in its float-landing body has started a THIRD copy, and the next rule added to
    /// the decision will miss it exactly the same way. The flags are legitimate elsewhere in both files (argument
    /// intake, NUMVAL-F's own §15.69.4 r2 determination), so the scan is anchored to the landing bodies.
    /// </summary>
    [Theory]
    [InlineData("IntrinsicRenderer.cs", "private NumX RenderFloatNative(")]
    [InlineData("NumericRenderer.cs", "private NumX Power(")]
    public void NeitherLandingConsumer_RederivesTheReceiverShapeTest(string file, string member)
    {
        string src = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Emit", file));
        int at = src.IndexOf(member, StringComparison.Ordinal);
        Assert.True(at >= 0, $"{file} no longer declares {member} — re-anchor this drift test");
        int stop = src.IndexOf("\n    }", at, StringComparison.Ordinal);
        Assert.True(stop > at, $"{file}: could not find the end of {member}");
        // CODE only: the doc comments and the forensic prose name the retired copies deliberately.
        string body = string.Join('\n', src[at..stop].Split('\n').Where(l => !l.TrimStart().StartsWith("//")));
        Assert.DoesNotContain("Receiverless", body);
        Assert.DoesNotContain("Receiver.Real", body);
        Assert.DoesNotContain("_rcv.Real", body);
        Assert.Contains("FloatLanding(", body);
    }
}
