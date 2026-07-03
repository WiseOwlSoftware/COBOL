// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Validation;

/// <summary>
/// The edition-gating diagnostics band COBOLNET0900–0999 (VERSION_TEST_MATRIX_DESIGN "Phase-2 implementation
/// plan" P2.3; verified unused before allocation). Pre-band codes are KEPT where tests/history pin them:
/// 0801/0802 (digit capacity), 0873 (DATA RECORDS FD+SD), 0810/0811 (ALTER / bare GO TO), 0882 (CALL ON
/// OVERFLOW) — those sites migrate their SEVERITY policy onto <see cref="Binding.EditionContext.Removed"/>
/// without renumbering.
/// </summary>
public static class EditionCodes
{
    /// <summary>Construct requires COBOL-YYYY — introduction gating, validator-visible. Error on both axes
    /// (there is nothing to run: the targeted edition has no semantics for the construct).</summary>
    public const string Introduction = "COBOLNET0900";

    /// <summary>Word reserved in COBOL-YYYY used as a user-defined word (ISO §8.9). Error strict / warning
    /// permissive (via <see cref="Binding.EditionContext.Removed"/> — the reserved-word check is an
    /// interval-encoded removal of the SPELLING from the user-word space).</summary>
    public const string ReservedWord = "COBOLNET0901";

    /// <summary>Construct removed in COBOL-YYYY. Error strict / warning permissive (via
    /// <see cref="Binding.EditionContext.Removed"/>; permissive preserves the pre-removal semantics).</summary>
    public const string RemovedConstruct = "COBOLNET0902";

    /// <summary>Obsolete/archaic-element flag (ISO §4.2.12 archaic / §4.2.13 obsolete). Warning, always —
    /// the element is still conforming in the targeted edition.</summary>
    public const string ObsoleteFlag = "COBOLNET0903";
}
