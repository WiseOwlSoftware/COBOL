// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Editions;

/// <summary>
/// The SINGLE strict/permissive severity decision for edition gating (rearch PHASE 02, P2/P4). Every
/// removed / reserved / obsolete / not-yet-introduced emit site asks HERE — never a local
/// <c>if (permissive)</c>. This is exactly the decision the old <c>EditionContext.Removed</c> seam and the
/// obsolete/introduction arms of <see cref="ConstructRegistry.Check"/> made; centralizing it lets the binder,
/// the validator, and both frontend preprocessor gates share one policy (feedback_singular_pattern).
/// </summary>
public static class EditionSeverityPolicy
{
    /// <summary>The severity for a construct's availability verdict at the targeted edition:
    /// <list type="bullet">
    /// <item><see cref="ConstructAvailability.NotYetIntroduced"/> ⇒ <see cref="EditionSeverity.Error"/> on BOTH
    /// axes — the edition has no semantics for a construct newer than itself.</item>
    /// <item><see cref="ConstructAvailability.Removed"/> ⇒ error when strict, warning when
    /// <see cref="EditionInfo.Permissive"/> (the migration mode preserves the pre-removal semantics).</item>
    /// <item><see cref="ConstructAvailability.Obsolete"/> ⇒ <see cref="EditionSeverity.Warning"/> always — the
    /// element is still conforming.</item>
    /// </list></summary>
    public static EditionSeverity For(ConstructAvailability verdict, EditionInfo edition) => verdict switch
    {
        ConstructAvailability.NotYetIntroduced => EditionSeverity.Error,
        ConstructAvailability.Removed => edition.Permissive ? EditionSeverity.Warning : EditionSeverity.Error,
        ConstructAvailability.Obsolete => EditionSeverity.Warning,
        _ => EditionSeverity.Warning,
    };
}
