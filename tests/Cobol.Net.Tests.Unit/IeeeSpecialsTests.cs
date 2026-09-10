// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>ISO §14.9.39.4 GR33/GR34/GR35 — the canonical ISO/IEC 60559:2020 Clause 3 values SET Format 15
/// stores, and the Annex A.1 item 176 NaN-payload determination that rides GR34/GR35 (kb/Work PB452).
/// <para>⛔ THESE ARE A PUBLISHED CONFORMANCE CLAIM, not an incidental encoding: A.1 item 176 says the payload
/// "shall be documented in the implementor's user documentation", docs/CONFORMANCE.md §7 DOC-A.1-176 documents
/// it, and this file is what makes the documented value and the emitted value the same thing. The suite DECODES
/// the constants (is it a NaN? which side of §6.2.1's quiet bit? whose sign?) rather than string-matching them,
/// so a re-spelling that changed the VALUE would fail while a re-spelling that did not would pass.</para>
/// <para>⚠ The COBOL-observable witness is `conformance:2014/pb452_set_content_float`, which can see that a
/// value is infinite, which sign it carries, and that a NaN is unequal to itself — but NOT the quiet/signaling
/// distinction, because the class condition that would expose it (§8.8.4.4's FLOAT-NOT-A-NUMBER-QUIET /
/// -SIGNALING, kb/Work PB225) is not implemented. That gap is exactly why the distinction is pinned HERE.</para>
/// </summary>
public sealed class IeeeSpecialsTests
{
    [Theory]
    [InlineData(33, false, false)]
    [InlineData(33, false, true)]
    [InlineData(33, true, false)]
    [InlineData(33, true, true)]
    [InlineData(34, false, false)]
    [InlineData(34, false, true)]
    [InlineData(34, true, false)]
    [InlineData(34, true, true)]
    [InlineData(35, false, false)]
    [InlineData(35, false, true)]
    [InlineData(35, true, false)]
    [InlineData(35, true, true)]
    public void EveryCanonicalValue_DecodesToWhatItsGeneralRuleRequires(int generalRule, bool single, bool negative)
    {
        var which = Of(generalRule);
        ulong bits = IeeeSpecials.Bits(which, single, negative);

        // GR33/GR34/GR35's own last sentence: "If the SIGN phrase is specified, the sign of the content is set
        // according to the SIGN specification, otherwise the sign is positive."
        ulong signMask = single ? 0x8000_0000UL : 0x8000_0000_0000_0000UL;
        Assert.Equal(negative, (bits & signMask) != 0);

        // ISO/IEC 60559:2020 Clause 3: an infinity and a NaN share the all-ones biased exponent; a ZERO
        // significand is the infinity, a nonzero one a NaN.
        ulong expMask = single ? 0x7F80_0000UL : 0x7FF0_0000_0000_0000UL;
        ulong sigMask = single ? 0x007F_FFFFUL : 0x000F_FFFF_FFFF_FFFFUL;
        Assert.Equal(expMask, bits & expMask);
        Assert.Equal(which is IeeeSpecial.Infinity, (bits & sigMask) == 0);

        // §6.2.1: the LEADING significand bit is the quiet bit — set = quiet, clear = signaling; a signaling
        // NaN needs some other significand bit nonzero, which is what the documented payload of 1 supplies.
        ulong quietBit = single ? 0x0040_0000UL : 0x0008_0000_0000_0000UL;
        if (which is IeeeSpecial.QuietNaN) Assert.Equal(quietBit, bits & quietBit);
        if (which is IeeeSpecial.SignalingNaN)
        {
            Assert.Equal(0UL, bits & quietBit);
            Assert.NotEqual(0UL, bits & sigMask & ~quietBit);
        }
    }

    [Theory]
    [InlineData(33, false, false)]
    [InlineData(33, false, true)]
    [InlineData(33, true, false)]
    [InlineData(33, true, true)]
    [InlineData(34, false, false)]
    [InlineData(34, false, true)]
    [InlineData(34, true, false)]
    [InlineData(34, true, true)]
    [InlineData(35, false, false)]
    [InlineData(35, false, true)]
    [InlineData(35, true, false)]
    [InlineData(35, true, true)]
    public void TheEmittedExpression_EvaluatesToTheDocumentedBits(int generalRule, bool single, bool negative)
    {
        var which = Of(generalRule);
        // ⛔ THE TWO HALVES OF THE DETERMINATION, ASSERTED EQUAL. Bits() is what the test above decodes and what
        // CONFORMANCE.md §7 publishes; Text() is what the emitter actually writes into the generated C#. Nothing
        // else forces them to agree — an edit to one is exactly the drift this catches. The evaluation is done
        // here rather than by string comparison, so the test asks "what does the emitted source MEAN".
        string text = IeeeSpecials.Text(which, single, negative);
        ulong expected = IeeeSpecials.Bits(which, single, negative);
        Assert.Equal(expected, Evaluate(text, single));
    }

    /// <summary>The §14.9.39.4 general rule a theory row names — GR33 is the infinity, GR34 the quiet NaN,
    /// GR35 the signaling one. The rows are keyed by RULE NUMBER so the internal enum stays out of a public test
    /// signature and each row reads as the obligation it pins.</summary>
    private static IeeeSpecial Of(int generalRule) => generalRule switch
    {
        33 => IeeeSpecial.Infinity,
        34 => IeeeSpecial.QuietNaN,
        35 => IeeeSpecial.SignalingNaN,
        _ => throw new ArgumentOutOfRangeException(nameof(generalRule), generalRule, "not a §14.9.39.4 Format-15 float rule"),
    };

    /// <summary>Evaluate the small closed set of expression shapes <see cref="IeeeSpecials.Text"/> produces —
    /// a CLR infinity constant, or a BitConverter reinterpretation of a hex literal — and return the raw bits.
    /// Deliberately a tiny recogniser rather than a Roslyn script: a shape this reader does not know is a FAILED
    /// assertion, so a third expression form cannot slip through unverified.</summary>
    private static ulong Evaluate(string text, bool single)
    {
        if (text.EndsWith("PositiveInfinity", StringComparison.Ordinal))
            return single
                ? (uint)BitConverter.SingleToInt32Bits(float.PositiveInfinity)
                : (ulong)BitConverter.DoubleToInt64Bits(double.PositiveInfinity);
        if (text.EndsWith("NegativeInfinity", StringComparison.Ordinal))
            return single
                ? (uint)BitConverter.SingleToInt32Bits(float.NegativeInfinity)
                : (ulong)BitConverter.DoubleToInt64Bits(double.NegativeInfinity);
        int at = text.IndexOf("0x", StringComparison.Ordinal);
        Assert.True(at >= 0, $"unrecognised IEEE-special expression shape: {text}");
        string hex = new([.. text[(at + 2)..].TakeWhile(Uri.IsHexDigit)]);
        ulong bits = Convert.ToUInt64(hex, 16);
        // Round-trip through the very conversion the generated code performs, so the assertion covers the
        // BitConverter call the emitter names and not merely the literal beside it.
        return single
            ? (uint)BitConverter.SingleToInt32Bits(BitConverter.Int32BitsToSingle(unchecked((int)(uint)bits)))
            : (ulong)BitConverter.DoubleToInt64Bits(BitConverter.Int64BitsToDouble(unchecked((long)bits)));
    }
}
