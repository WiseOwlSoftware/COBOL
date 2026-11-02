// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding.Passes;

/// <summary>
/// The ResolveReports pass: row 25 of the per-unit resolve pipeline (FilesResolved to FilesResolved), the REPORT SECTION
/// resolution (report groups, SUM and rolled addends, column arrangement, CONTROL and PAGE limits).
/// <para>The rule code lives with the concern it belongs to, not with the pass: the report rules are
/// <see cref="DataBinder.ResolveReports"/> and its helpers (the <c>ReportSectionBinder</c> concern of
/// docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1), kb/Work PB2357), so this row only schedules it. The pass
/// forces the print faces of the report groups to their character image, so it declares an append to
/// <see cref="FactSetKind.ImageForcedItems"/>.</para>
/// </summary>
internal sealed class ResolveReportsPass : IBindPass
{
    /// <inheritdoc/>
    public string Name => "ResolveReports";

    /// <inheritdoc/>
    public PassPhase Requires => PassPhase.FilesResolved;

    /// <inheritdoc/>
    public PassPhase Produces => PassPhase.FilesResolved;

    /// <inheritdoc/>
    public PassViews Views => PassViews.Appends(FactSetKind.ImageForcedItems);

    /// <inheritdoc/>
    public void Run(DataBinder binder) => binder.ResolveReports();
}
