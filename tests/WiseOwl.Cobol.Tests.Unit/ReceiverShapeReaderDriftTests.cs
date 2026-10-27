// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A NUMERIC FUNCTION'S VALUE MAY NOT DEPEND ON THE SHAPE OF ITS RECEIVER, AND THE PLACES THAT READ THE RECEIVER ARE A
/// CLOSED, JUSTIFIED SET (kb/Work PB3839).
/// <para>
/// The standard states the rule for the standard modes only: §15.4.1 "the returned value is the same for all instances of a
/// given function within a single execution of the runtime element so long as the value and order of the arguments, the
/// collating sequence, and the locale are unchanged" opens "When standard-decimal arithmetic or standard-binary arithmetic is
/// in effect", and for native arithmetic it leaves "the characteristics and representation of the returned value" to the
/// implementor. The receiver is none of the listed inputs, and this compiler's determination for NATIVE (owner, kb/Work
/// PB3839; CLAUDE.md "consistent with ourselves") is the same: one reference, one value, whatever receives it.
/// NUMVAL-F broke it for months because its arm tested the receiver
/// (<c>Receiver.Real || Receiver.Receiverless</c>) to choose between a binary64 and an <c>Int128</c> parse truncated at the
/// receiver-capped working scale: <c>DISPLAY</c> and a COMP-2 got the full value, a fixed-point receiver got nine places and
/// then rounded them again, and ROUNDED MODE AWAY-FROM-ZERO of NUMVAL-F("0.12340000000001") into a <c>PIC 9V9(4)</c> stored
/// 0.1234 where NUMVAL stored 0.1235. MAX and MIN had the sibling: a selected value truncated at a coarser receiver's scale
/// before the receiver's ROUNDED phrase saw it. No runtime test can see a function that reads its receiver and happens to
/// agree for ordinary pictures; this reads the renderer.
/// </para>
/// <para>
/// ⚠ THE GUARD NAMES NO FUNCTION. It collects every member of <c>ReceiverContext</c> that <c>IntrinsicRenderer</c> reads
/// through <c>num.Receiver.</c> and requires each to be in <see cref="Readers"/> with the reason that read cannot change a
/// value, in both directions (a reader nobody justified fails; a justification nobody uses fails). A new reader is therefore
/// a red test until its author writes down why it is a landing decision and not a different value.
/// </para>
/// </summary>
public sealed class ReceiverShapeReaderDriftTests
{
    /// <summary>The <c>ReceiverContext</c> members <c>IntrinsicRenderer</c> reads, each with why that read decides a LANDING
    /// (how the one value reaches the receiver) and never the value.</summary>
    private static readonly Dictionary<string, string> Readers = new(StringComparer.Ordinal)
    {
        ["FloatLanding"] =
            "the float family's one decision (kb/Work PB647, PB653, PB3829): the value IS a binary64; the final transfer into a "
            + "fixed-point resultant quantizes it ONCE at that resultant's scale and mode, anything else keeps the binary64",
        ["RoundsAtItsScale"] =
            "MAX / MIN: whether a receiver-bound result may be landed (store-cap) or must stay loud on the escape check",
        ["WorkingScale"] =
            "MAX / MIN: the receiver-bound result lands at the common argument scale widened to the receiver's and capped at its "
            + "Int128 headroom, and SelectionPastReceiverHeadroom asks whether that cap would cut the selection - then the SDIDI "
            + "carries it - so the selected value reaches a ROUNDED receiver with every digit it has (kb/Work PB3839, review R1-1)",
        ["Scale"] =
            "MEAN's quotient scale, max(receiver scale, sum scale + 1, 6): OPEN. The quotient's digits past the receiver scale are "
            + "truncated at that working scale before the receiver's rounding mode sees them, so MEAN is the one function whose "
            + "value still depends on its receiver (filed from the PB3839 report; remove this entry and the next when it lands)",
        ["Rounding"] =
            "MEAN's quotient mode, the other half of the OPEN entry above",
    };

    private static string RendererCode()
    {
        string src = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen", "Emit", "IntrinsicRenderer.cs"));
        // Comments mention the members all the time; only code reads count.
        return string.Join("\n", src.Split('\n').Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal)));
    }

    [Fact]
    public void TheReceiverMembersTheIntrinsicRendererReads_AreAClosedJustifiedSet()
    {
        var read = Regex.Matches(RendererCode(), @"\bnum\.Receiver\.(?<m>\w+)")
            .Select(m => m.Groups["m"].Value).ToHashSet(StringComparer.Ordinal);
        Assert.True(read.Count > 0, "the scan found no num.Receiver reads - the renderer's shape changed, re-point this guard");
        var unjustified = read.Except(Readers.Keys).OrderBy(x => x, StringComparer.Ordinal).ToList();
        Assert.True(unjustified.Count == 0,
            "IntrinsicRenderer now reads ReceiverContext member(s) [" + string.Join(", ", unjustified) + "] with no entry in "
            + "ReceiverShapeReaderDriftTests.Readers. A function's value must not depend on its receiver (the PB3839 owner "
            + "determination, self-consistency: §15.4.1 leaves the NATIVE returned value implementor-defined, and the "
            + "implementor's choice is one value per reference); write down why this read decides a LANDING and not a "
            + "value, or remove it (kb/Work PB3839).");
        var stale = Readers.Keys.Except(read).OrderBy(x => x, StringComparer.Ordinal).ToList();
        Assert.True(stale.Count == 0,
            "ReceiverShapeReaderDriftTests.Readers justifies [" + string.Join(", ", stale) + "], which IntrinsicRenderer no longer "
            + "reads - delete the entry (a justification nobody uses is how the next reader hides).");
    }

    /// <summary>The functions whose value is the exact parse of their argument
    /// (<see cref="IntrinsicCarrierRules.ExactParseFunctions"/>) have no <c>RenderNum</c> arm of their own - an arm is where a
    /// receiver test lives - and an SDIDI arm in <c>RenderDec</c>, the one body every mode, channel and receiver shares.</summary>
    [Fact]
    public void TheExactParseFunctions_HaveNoArmOfTheirOwn_AndOneSdidiBody()
    {
        string code = RendererCode();
        Assert.True(IntrinsicCarrierRules.ExactParseFunctions.Count >= 3);
        foreach (string method in IntrinsicCarrierRules.ExactParseFunctions)
        {
            Assert.DoesNotContain($"case \"{method}\"", code, StringComparison.Ordinal);
            Assert.True(Regex.IsMatch(code, $"\"{method}\"\\s*(=>|when)"),
                $"RenderDec has no SDIDI arm for {method}, which IntrinsicCarrierRules.ReturnsExactParse routes to it");
        }
    }
}
