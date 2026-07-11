// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.Collections.Generic;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Passes;

/// <summary>The shared spine of the whole-group middle-end passes (P6 Step 3): the parse ROOT (the terminal
/// <c>VersionConformancePass</c>'s parse-tree arm walks it — carrying it on the GROUP context puts no parse context
/// on any bound NODE), the collected units + class units, the group-bind session (turn state, class table, edition,
/// uid bands), and the <see cref="IOoBindHost"/> seam for the OO sub-steps that still live on the emitter (P9 moves
/// them).</summary>
internal sealed record GroupBindContext(
    CobolParserCore.CompilationUnitContext Tree,
    IReadOnlyList<BoundUnit> Units,
    IReadOnlyList<OoClassUnit> Classes,
    BindSession Session,
    IOoBindHost Oo);

/// <summary>
/// One declared GROUP pass of the bind pipeline (P6 Step 3): the <see cref="IPassInfo"/> contract plus the work it
/// runs over the WHOLE compilation (<see cref="GroupBindContext"/>). These are the middle-end passes that need
/// every unit's BOUND tree — procedure binding, whole-group usage collection, the storage-form computation — and,
/// terminally, the <c>VersionConformancePass</c>. The manifest is <c>BindPipeline.GroupTail</c>; the runner is
/// <c>BinderDriver.Bind</c>; the order is validated together with the per-unit prefix by
/// <c>BindPipeline.ValidateFullChainOnce</c>.
/// </summary>
internal sealed record GroupBindPass(string Name, PassPhase Requires, PassPhase Produces, Action<GroupBindContext> Body)
    : IPassInfo
{
    public void Run(GroupBindContext ctx) => Body(ctx);
}
