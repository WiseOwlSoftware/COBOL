// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using CobolSharp.Compiler.Generated;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// The single canonical mapping from a parsed rounding-mode keyword to its runtime <see cref="CobolRounding"/>
/// (ISO/IEC 1989:2023 §14.7.4.3). Shared by every consumer — the per-statement ROUNDED phrase, the OPTIONS
/// <c>DEFAULT ROUNDED</c> clause (§11.9.6), and the OPTIONS <c>INTERMEDIATE ROUNDING</c> clause (§11.9.11) — so
/// there is exactly one place that knows the keyword→mode correspondence.
/// </summary>
internal static class RoundingModes
{
    /// <summary>Map a full 8-mode <c>roundingModeName</c> (ROUNDED phrase / DEFAULT ROUNDED clause).</summary>
    public static CobolRounding Map(Core.RoundingModeNameContext m) =>
        m.AWAY_FROM_ZERO() is not null ? CobolRounding.AwayFromZero
        : m.NEAREST_AWAY_FROM_ZERO() is not null ? CobolRounding.NearestAwayFromZero
        : m.NEAREST_EVEN() is not null ? CobolRounding.NearestEven
        : m.NEAREST_TOWARD_ZERO() is not null ? CobolRounding.NearestTowardZero
        : m.TOWARD_GREATER() is not null ? CobolRounding.TowardGreater
        : m.TOWARD_LESSER() is not null ? CobolRounding.TowardLesser
        : m.PROHIBITED() is not null ? CobolRounding.Prohibited
        : CobolRounding.Truncation;   // TRUNCATION

    /// <summary>Map the restricted 4-mode <c>intermediateRoundingMode</c> (INTERMEDIATE ROUNDING clause, §11.9.11:
    /// NEAREST-AWAY-FROM-ZERO | NEAREST-EVEN | PROHIBITED | TRUNCATION).</summary>
    public static CobolRounding MapIntermediate(Core.IntermediateRoundingModeContext m) =>
        m.NEAREST_AWAY_FROM_ZERO() is not null ? CobolRounding.NearestAwayFromZero
        : m.NEAREST_EVEN() is not null ? CobolRounding.NearestEven
        : m.PROHIBITED() is not null ? CobolRounding.Prohibited
        : CobolRounding.Truncation;   // TRUNCATION
}
