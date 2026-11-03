// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding.Passes;

/// <summary>
/// The OoRouteMethodRedefinesBackings pass: row 16 of the per-unit resolve pipeline (RedefinesClassified to
/// StrongTypeChecked), which routes the string backing of a METHOD's Tier-B REDEFINES class to the class's field channel.
/// <para>The rule code lives with the concern it belongs to, not with the pass: the OO data rules are
/// <see cref="DataBinder.OoRouteMethodRedefinesBackings"/> and its neighbours (the <c>OoDataBinder</c> concern of
/// docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1), kb/Work PB2360), so this row only schedules it.</para>
/// </summary>
internal sealed class OoRouteMethodRedefinesBackingsPass : IBindPass
{
    /// <inheritdoc/>
    public string Name => "OoRouteMethodRedefinesBackings";

    /// <inheritdoc/>
    public PassPhase Requires => PassPhase.RedefinesClassified;

    /// <inheritdoc/>
    public PassPhase Produces => PassPhase.StrongTypeChecked;

    /// <inheritdoc/>
    public PassViews Views => PassViews.None;

    /// <inheritdoc/>
    public void Run(DataBinder binder) => binder.Oo.RouteMethodRedefinesBackings();
}
