// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding.Model;

/// <summary>The classification of a COBOL-85 segment-number (kb/Work PB5084): §1985:XVI.3.2.4 GR2 "Segments with segment-number 0
/// through 49 belong to the fixed portion of the object program", GR3 "Segments with segment-number 50 through 99 are independent
/// segments." A model fact, so the binder's rules (<c>Binding.Procedure.Segmentation</c>) and the emitter's initial-state
/// machinery (<c>AlterSwitchEmitter.EmitSegmentState</c>) ask the same question of the same number.</summary>
internal static class SegmentNumber
{
    /// <summary>The lowest independent segment-number (§1985:XVI.3.2.4 GR3).</summary>
    public const int FirstIndependent = 50;

    /// <summary>The highest segment-number a section may carry (§1985:XVI.3.2.3 SR1: "The segment-number must be an integer
    /// ranging in value from 0 through 99.").</summary>
    public const int Last = 99;

    /// <summary>Is <paramref name="segment"/> a number the module gives a meaning (0 through <see cref="Last"/>,
    /// §1985:XVI.3.2.3 SR1)? A number outside it is refused by the range screen (COBOL6819), so a rule that asks about a
    /// segment's place in the program (fixed or independent) skips it rather than report a cascade about a number that
    /// classifies nothing (kb/Work PB5223).</summary>
    public static bool HasMeaning(int segment) => segment is >= 0 and <= Last;

    /// <summary>Is <paramref name="segment"/> an independent segment (§1985:XVI.3.2.4 GR3)?</summary>
    public static bool IsIndependent(int segment) => segment >= FirstIndependent;
}
