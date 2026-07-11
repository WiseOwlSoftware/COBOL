// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;

namespace CobolNet.CodeGen;

/// <summary>
/// The RECEIVER a numeric render is computed FOR (P7 Step 3; DESIGN-codegen-backend §2.5): the receiving item's
/// scale (the division/power working scale, ISO §8.8.1/§14.7.4), whether it is a floating-point receiver (the
/// whole RHS then evaluates in IEEE binary64 — D16), its ROUNDED mode, and whether the statement runs under ON
/// SIZE ERROR / EC-SIZE checking (a division renders the checked <c>DivideOrThrow</c>, a product
/// <c>MulChecked</c>; ISO §14.7.5). Passed BY PARAMETER into every public numeric-render entry — replacing the
/// four mutable <c>EmitContext.Target*</c>/<c>InSizeErrorContext</c> fields whose manual set/reset discipline
/// was the H1 staleness hazard (a receiver-less render inheriting the PREVIOUS statement's receiver). A site with
/// no receiver in scope passes <see cref="None"/> (scale 0, fixed-point, truncation, unchecked).
/// </summary>
internal readonly record struct ReceiverContext(int Scale, bool Real, CobolRounding Rounding, bool InSizeError)
{
    /// <summary>The receiver-less context: scale 0, not floating, TRUNCATION, no size-error checking.</summary>
    public static readonly ReceiverContext None = new(0, false, CobolRounding.Truncation, false);
}
