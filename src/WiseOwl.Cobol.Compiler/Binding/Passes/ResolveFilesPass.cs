// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding.Passes;

/// <summary>
/// The ResolveFiles pass: row 23 of the per-unit resolve pipeline (OccursResolved to FilesResolved), the FILE-CONTROL and
/// I-O-CONTROL resolution (FILE STATUS items, ASSIGN, collating sequences, keys, LINAGE, APPLY COMMIT).
/// <para>The rule code lives with the concern it belongs to, not with the pass: the file-control rules are
/// <see cref="DataBinder.ResolveFiles"/> and its helpers (the <c>FileControlBinder</c> concern of
/// docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1), kb/Work PB2356), so this row only schedules it.</para>
/// </summary>
internal sealed class ResolveFilesPass : IBindPass
{
    /// <inheritdoc/>
    public string Name => "ResolveFiles";

    /// <inheritdoc/>
    public PassPhase Requires => PassPhase.OccursResolved;

    /// <inheritdoc/>
    public PassPhase Produces => PassPhase.FilesResolved;

    /// <inheritdoc/>
    public PassViews Views => PassViews.None;

    /// <inheritdoc/>
    public void Run(DataBinder binder) => binder.ResolveFiles();
}
