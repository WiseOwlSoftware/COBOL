// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The eight ISO/IEC 1989:2023 ROUNDED MODE rounding methods (§14.9.4 / §11.9.6).
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
    /// (EC-SIZE-TRUNCATION) and leaves the receiver unchanged (ISO §14.9.4).</summary>
    Prohibited = 5,
    /// <summary>TOWARD-GREATER — round toward positive infinity (ceiling).</summary>
    TowardGreater = 6,
    /// <summary>TOWARD-LESSER — round toward negative infinity (floor).</summary>
    TowardLesser = 7,
}
