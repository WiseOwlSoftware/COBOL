// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding;

/// <summary>The three non-finite values ISO §14.9.39.4 GR33 / GR34 / GR35 name — "a canonical representation of
/// infinity" / "of a quiet NaN" / "of a signaling NaN … as described in ISO/IEC 60559:2020, Clause 3, for the
/// basic interchange format corresponding to the usage of identifier-14".</summary>
public enum IeeeSpecial
{
    /// <summary>GR33 — an infinity.</summary>
    Infinity,
    /// <summary>GR34 — a QUIET NaN, "with the payload set to an implementor-defined value".</summary>
    QuietNaN,
    /// <summary>GR35 — a SIGNALING NaN, likewise with an implementor-defined payload.</summary>
    SignalingNaN,
}

/// <summary>⛔ THE ONE PLACE the ISO/IEC 60559:2020 Clause 3 canonical encodings are written down, and the
/// implementing site of the Annex A.1 item 176 determination published in <c>docs/CONFORMANCE.md</c> §7 under
/// <c>DOC-A.1-176</c> (kb/Work PB452). ISO §14.9.39.4 GR34/GR35 leave the NaN PAYLOAD to the implementor and
/// A.1 item 176 makes documenting the choice mandatory, so the values below are a published conformance claim,
/// not an incidental encoding — which is why they live in one testable function rather than inline in the
/// emitter, and why <c>IeeeSpecialsTests</c> asserts what each one decodes to.
/// <para>The receiver of a Format-15 float word is confined by §14.9.39.3 SR32 to a STANDARD floating-point
/// usage; of those, the two COBOL.NET provides map EXACTLY onto the CLR's IEEE types — FLOAT-BINARY-32 →
/// <c>float</c> = binary32, FLOAT-BINARY-64 → <c>double</c> = binary64 — so the interchange format GR33–GR35
/// name IS the carrier and nothing is approximated here. FLOAT-BINARY-128 and the two FLOAT-DECIMAL usages are
/// Annex A.3 items 17/19 documented non-support, refused at declaration (COBOLNET1564), so no receiver of those
/// usages reaches this code.</para>
/// <para>⛔ EVERY NaN IS SPELLED AS A BIT PATTERN, never as a CLR constant or a negation. <c>double.NaN</c> is
/// 0xFFF8000000000000 — its sign bit is SET — so GR34's "otherwise the sign is positive" would have to be
/// written <c>-double.NaN</c> and would then depend on how a constant negation folds; and the CLR has no
/// signaling-NaN constant at all, because every managed arithmetic path quiets one. A
/// <c>BitConverter.Int64BitsToDouble</c> / <c>Int32BitsToSingle</c> call is a reinterpretation, performs no
/// arithmetic, and so cannot quiet the value.</para></summary>
internal static class IeeeSpecials
{
    /// <summary>The C# expression for <paramref name="which"/> in the interchange format of the receiver's
    /// carrier.</summary>
    /// <param name="single">True for binary32 (<c>float</c> — USAGE FLOAT-BINARY-32), false for binary64.</param>
    /// <param name="negative">GR33/GR34/GR35's own last sentence: the SIGN phrase's sign, positive when the
    /// phrase is absent.</param>
    internal static string Text(IeeeSpecial which, bool single, bool negative)
    {
        if (which is IeeeSpecial.Infinity)
            return $"{(single ? "float" : "double")}.{(negative ? "Negative" : "Positive")}Infinity";
        // ISO/IEC 60559:2020 Clause 3 / §6.2.1: a NaN has the biased exponent all ones and a nonzero significand;
        // the LEADING significand bit distinguishes them — SET is quiet, CLEAR is signaling (and a signaling NaN
        // needs some other significand bit nonzero, so the payload is the minimal 1).
        //   binary32  quiet +  0x7FC00000   signaling +  0x7F800001
        //   binary64  quiet +  0x7FF8000000000000   signaling +  0x7FF0000000000001
        // The payload is A.1 item 176's implementor-defined value: ZERO for the quiet form (the canonical NaN
        // every IEEE implementation produces) and ONE for the signaling form (the smallest payload that keeps
        // the significand nonzero). Both are published in docs/CONFORMANCE.md §7, DOC-A.1-176.
        bool quiet = which is IeeeSpecial.QuietNaN;
        char signNibble = negative ? 'F' : '7';
        return single
            ? $"System.BitConverter.Int32BitsToSingle(unchecked((int)0x{signNibble}{(quiet ? "FC00000" : "F800001")}))"
            : $"System.BitConverter.Int64BitsToDouble(unchecked((long)0x{signNibble}{(quiet ? "FF8000000000000" : "FF0000000000001")}))";
    }

    /// <summary>The raw bits the <see cref="Text"/> expression evaluates to — the same table, as a value rather
    /// than as source. It exists so a test can DECODE the determination (is it a NaN? is the quiet bit where
    /// GR34/GR35 require? is the sign GR3x's?) instead of string-matching the emitted text, and so the two can
    /// be asserted equal to each other.</summary>
    internal static ulong Bits(IeeeSpecial which, bool single, bool negative)
    {
        ulong signBit = negative ? (single ? 0x8000_0000UL : 0x8000_0000_0000_0000UL) : 0UL;
        ulong magnitude = which switch
        {
            IeeeSpecial.Infinity => single ? 0x7F80_0000UL : 0x7FF0_0000_0000_0000UL,
            IeeeSpecial.QuietNaN => single ? 0x7FC0_0000UL : 0x7FF8_0000_0000_0000UL,
            _ => single ? 0x7F80_0001UL : 0x7FF0_0000_0000_0001UL,
        };
        return signBit | magnitude;
    }
}
