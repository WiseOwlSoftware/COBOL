// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding.Passes;

/// <summary>
/// The CheckGlobalNameUniqueness pass (TypesExpanded to TypesExpanded): the GLOBAL data-name uniqueness rule of
/// §2023:8.4.2.1 asked of the COMPOSED forest, immediately after ExpandTypes. A TYPE or SAME AS clause brings its description
/// in "as though ... coded in place" (§2023:13.18.57.4 GR1), so a copied member is a data item of the source element and
/// shares no name with a GLOBAL item (kb/Work PB5073, review finding R1-1).
/// </summary>
internal sealed class CheckGlobalNameUniquenessPass : IBindPass
{
    /// <inheritdoc/>
    public string Name => "CheckGlobalNameUniqueness";

    /// <inheritdoc/>
    public PassPhase Requires => PassPhase.TypesExpanded;

    /// <inheritdoc/>
    public PassPhase Produces => PassPhase.TypesExpanded;

    /// <inheritdoc/>
    public PassViews Views => PassViews.None;

    /// <inheritdoc/>
    public void Run(DataBinder binder) => binder.ScreenGlobalNameUniqueness();
}
