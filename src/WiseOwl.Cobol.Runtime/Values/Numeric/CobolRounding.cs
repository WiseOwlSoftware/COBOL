// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// The eight ISO/IEC 1989:2023 ROUNDED MODE rounding methods (§2023:14.9.4 / §2023:11.9.6).
///
/// The numeric values are deliberately identical to the legacy <c>PicRuntime.Round*</c> integer
/// constants so that the data-model migration can cast between the two without a lookup table while
/// both numeric pipelines coexist (the byte path keyed off the int constant, the typed path keyed off
/// this enum). The mapping is therefore the identity cast; see <c>PicRuntime.RoundTruncation</c> et al.
/// </summary>
public enum CobolRounding
{
    /// <summary>TRUNCATION — drop the excess fraction toward zero (no rounding). COBOL default when no
    /// ROUNDED phrase is present.</summary>
    Truncation = 0,
    /// <summary>NEAREST-AWAY-FROM-ZERO — round to nearest; a tie rounds away from zero. The behavior of a
    /// bare <c>ROUNDED</c> phrase.</summary>
    NearestAwayFromZero = 1,
    /// <summary>AWAY-FROM-ZERO — always round up in magnitude when any nonzero fraction is dropped.</summary>
    AwayFromZero = 2,
    /// <summary>NEAREST-EVEN — round to nearest; a tie rounds to the nearest even digit (banker's rounding).</summary>
    NearestEven = 3,
    /// <summary>NEAREST-TOWARD-ZERO — round to nearest; a tie rounds toward zero.</summary>
    NearestTowardZero = 4,
    /// <summary>PROHIBITED — rounding is not permitted; an inexact result raises the SIZE ERROR condition
    /// (EC-SIZE-TRUNCATION) and leaves the receiver unchanged (ISO §2023:14.9.4).</summary>
    Prohibited = 5,
    /// <summary>TOWARD-GREATER — round toward positive infinity (ceiling).</summary>
    TowardGreater = 6,
    /// <summary>TOWARD-LESSER — round toward negative infinity (floor).</summary>
    TowardLesser = 7,
}

/// <summary>A §2023:14.7.4.3 mode restated on the MAGNITUDE of an inexact value: which of the two representable
/// magnitudes bracketing it the mode takes (the truncated one, <see cref="Down"/>, or the one a unit farther from zero,
/// <see cref="Up"/>), and for a NEAREST-* mode, which one a tie takes.</summary>
internal enum MagnitudeStep
{
    /// <summary>Always the magnitude nearer to zero (GR10 TRUNCATION; GR8 TOWARD-GREATER on a negative value, GR9
    /// TOWARD-LESSER on a positive one).</summary>
    Down,
    /// <summary>Always the magnitude farther from zero (GR3 AWAY-FROM-ZERO; GR8 on a positive value, GR9 on a negative one).</summary>
    Up,
    /// <summary>The nearer magnitude; a tie goes farther from zero (GR4 NEAREST-AWAY-FROM-ZERO).</summary>
    NearestTieUp,
    /// <summary>The nearer magnitude; a tie goes to the even one (GR5 NEAREST-EVEN).</summary>
    NearestTieEven,
    /// <summary>The nearer magnitude; a tie goes nearer to zero (GR6 NEAREST-TOWARD-ZERO).</summary>
    NearestTieDown,
}

/// <summary>⛔ THE ONE ROUNDING DECISION (ISO §2023:14.7.4.3 GR3 to GR10; kb/Work PB2644): which neighbour each of the eight
/// modes takes for a value that "cannot be exactly represented in the resultant identifier". Every lane asks it — the
/// fixed-point divide kernels over any integer carrier (<c>CobolNum.RoundDiv</c>, <c>CobolNum.LandQuotient</c>,
/// <c>CobolExact</c>'s quotient), the standard-decimal rounding of a dropped digit run (<c>CobolDec.Round34Wide</c>,
/// <c>CobolDec.TransferWide</c>, <c>CobolDec.RoundFromRemainder</c>) and the floating-point resultant's choice between two
/// binary32 or binary64 neighbours (<c>FloatResultant</c>) — and the lanes differ only in how they measure where the value
/// lies against the half-way point between the two neighbours. Before it the table was written four times and the copies
/// had drifted: the standard-decimal digit walk (<c>Round34Wide</c>) rounded every mode but TRUNCATION and NEAREST-EVEN as
/// NEAREST-AWAY-FROM-ZERO.</summary>
internal static class RoundingRule
{
    /// <summary>The <paramref name="mode"/> restated on the magnitude of a value of sign <paramref name="negative"/>.
    /// ⛔ PROHIBITED is no direction (GR7: a value that cannot be represented exactly is never stored, and the receiver is
    /// unchanged): asking for its step raises <see cref="CobolSizeError.ProhibitedInexact"/>, the right answer for every
    /// lane that asks only once it knows the value is inexact. A lane that measures first and decides afterwards whether
    /// to store (the floating-point resultant's <c>Admits</c>) asks with <see cref="CobolRounding.Truncation"/>.</summary>
    internal static MagnitudeStep StepOf(CobolRounding mode, bool negative) => mode switch
    {
        CobolRounding.AwayFromZero => MagnitudeStep.Up,                                    // GR3
        CobolRounding.NearestAwayFromZero => MagnitudeStep.NearestTieUp,                   // GR4
        CobolRounding.NearestEven => MagnitudeStep.NearestTieEven,                         // GR5
        CobolRounding.NearestTowardZero => MagnitudeStep.NearestTieDown,                   // GR6
        CobolRounding.Prohibited => throw CobolSizeError.ProhibitedInexact(),               // GR7
        CobolRounding.TowardGreater => negative ? MagnitudeStep.Down : MagnitudeStep.Up,   // GR8: the nearest larger value
        CobolRounding.TowardLesser => negative ? MagnitudeStep.Up : MagnitudeStep.Down,    // GR9: the nearest smaller value
        CobolRounding.Truncation => MagnitudeStep.Down,                                    // GR10
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "not a §2023:14.7.4.3 rounding mode"),
    };

    /// <summary>Whether <paramref name="step"/> takes the magnitude a unit farther from zero than the truncated one.
    /// <paramref name="againstHalf"/> is the sign of (the dropped part − one half unit): positive above the half-way
    /// point, zero exactly on it, negative below. <paramref name="truncatedIsOdd"/> says whether the truncated magnitude's
    /// rightmost digit (or significand bit) is odd — NEAREST-EVEN's tie-break.</summary>
    internal static bool Bumps(MagnitudeStep step, bool truncatedIsOdd, int againstHalf) => step switch
    {
        MagnitudeStep.Down => false,
        MagnitudeStep.Up => true,
        MagnitudeStep.NearestTieUp => againstHalf >= 0,
        MagnitudeStep.NearestTieDown => againstHalf > 0,
        _ => againstHalf > 0 || (againstHalf == 0 && truncatedIsOdd),                       // NearestTieEven
    };

    /// <summary>The decision for a NONZERO remainder <paramref name="absRem"/> of a division by the positive
    /// <paramref name="divisor"/>, over any integer carrier. The half-way test compares the remainder with its COMPLEMENT
    /// <c>divisor − |rem|</c>, never <c>2·|rem|</c> with the divisor (kb/Work PB2640): <c>|rem| &lt; divisor</c>, so the
    /// complement cannot overflow on any carrier, where doubling the remainder wrapped NEGATIVE on <see cref="Int128"/>
    /// once the divisor passed 2^126.</summary>
    internal static bool Bumps<T>(CobolRounding mode, bool negative, bool truncatedIsOdd, T absRem, T divisor)
        where T : System.Numerics.INumber<T> =>
        Bumps(StepOf(mode, negative), truncatedIsOdd, absRem.CompareTo(divisor - absRem));

    /// <summary>The decision for a dropped DECIMAL digit run, read as its leftmost digit <paramref name="roundDigit"/> and
    /// whether any digit after it is nonzero (<paramref name="sticky"/>); the run is nonzero.</summary>
    internal static bool BumpsOnDigit(CobolRounding mode, bool negative, bool truncatedIsOdd, int roundDigit, bool sticky) =>
        Bumps(StepOf(mode, negative), truncatedIsOdd, roundDigit != 5 ? roundDigit.CompareTo(5) : sticky ? 1 : 0);
}
