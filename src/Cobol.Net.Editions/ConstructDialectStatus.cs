// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Editions;

/// <summary>A construct's availability at a targeted edition (the registry's verdict shape).</summary>
public enum ConstructAvailability
{
    /// <summary>Valid at the edition, no flag.</summary>
    Available,
    /// <summary>Newer than the edition — introduction gating (COBOLNET0900 band; error on BOTH axes).</summary>
    NotYetIntroduced,
    /// <summary>Removed by the edition — error strict / warning permissive (the removed-construct severity seam).</summary>
    Removed,
    /// <summary>Obsolete at the edition (ISO §4.2.13 over Annex F.2) — warning always (0903).</summary>
    Obsolete,
}

/// <summary>
/// One construct's per-edition dialect status (VERSION_TEST_MATRIX_DESIGN "Phase-2 implementation plan" P2.5):
/// the in-code rendering of a <c>tests/version-matrix/constructs.json</c> row — THE canonical catalogue; the
/// drift test (<c>ConstructRegistryDriftTests</c>) asserts registry↔json equality BOTH directions, so a gate
/// cannot land without its matrix row nor a row without its registry entry.
/// </summary>
/// <param name="Id">The constructs.json row id.</param>
/// <param name="Display">Human name used in diagnostics.</param>
/// <param name="IntroducedIn">First edition that HAS the construct (85/2002/2014/2023).</param>
/// <param name="RemovedIn">First edition that REMOVED it (null = never).</param>
/// <param name="ObsoleteIn">First edition marking it obsolete/archaic (null = never; drives 0903).</param>
/// <param name="DiagnosticCode">The code its gate emits (the TARGET code where surfacing is still a raw
/// parse error today — the W1.5 upgrade wires it).</param>
/// <param name="Citation">ISO § / VCR row / roadmap-D citation.</param>
public sealed record ConstructDialectStatus(
    string Id, string Display, int IntroducedIn, int? RemovedIn, int? ObsoleteIn,
    string DiagnosticCode, string Citation)
{
    /// <summary>The availability verdict at <paramref name="edition"/>.</summary>
    public ConstructAvailability StatusAt(int edition)
    {
        if (edition < IntroducedIn) return ConstructAvailability.NotYetIntroduced;
        if (RemovedIn is { } r && edition >= r) return ConstructAvailability.Removed;
        if (ObsoleteIn is { } o && edition >= o) return ConstructAvailability.Obsolete;
        return ConstructAvailability.Available;
    }
}
