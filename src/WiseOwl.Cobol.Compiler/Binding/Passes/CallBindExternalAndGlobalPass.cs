// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Binding.Passes;

using Core = CobolParserCore;

/// <summary>
/// The CallBindExternalAndGlobal pass: row 26 of the per-unit resolve pipeline (FilesResolved to FilesResolved), the
/// EXTERNAL and GLOBAL record bindings and the CALL cells of the unit (§13.18.27.4 GR1).
/// <para>The rule code lives with the concern it belongs to, not with the pass: the linkage rules are
/// <see cref="DataBinder.CallBindExternalAndGlobal"/> and its helpers (the <c>LinkageBinder</c> concern of
/// docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1), kb/Work PB2359), so this row only schedules it. The pass
/// forces a CALL cell's leaves to their character image, so it declares an append to
/// <see cref="FactSetKind.ImageForcedItems"/>.</para>
/// </summary>
/// <param name="program">The unit's program context, which the binder reads for its declaration-context phrases.</param>
internal sealed class CallBindExternalAndGlobalPass(Core.ProgramUnitContext program) : IBindPass
{
    /// <inheritdoc/>
    public string Name => "CallBindExternalAndGlobal";

    /// <inheritdoc/>
    public PassPhase Requires => PassPhase.FilesResolved;

    /// <inheritdoc/>
    public PassPhase Produces => PassPhase.FilesResolved;

    /// <inheritdoc/>
    public PassViews Views => PassViews.Appends(FactSetKind.ImageForcedItems);

    /// <inheritdoc/>
    public void Run(DataBinder binder) => binder.Linkage.CallBindExternalAndGlobal(program);
}
