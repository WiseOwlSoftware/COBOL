// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding.Passes;

/// <summary>
/// The LinkRedefinitions pass of the per-unit resolve pipeline (SignResolved to SignResolved): every REDEFINES clause's subject is
/// linked to its data-name-2 (<see cref="DataItem.SetRedefinition"/>) BEFORE any pass measures an item.
/// </summary>
/// <remarks>
/// <para><b>Why a pass of its own, and why here</b> (kb/Work PB5932, listing step 6b remainder slice 3, review round 1 lead R1-2). The
/// layout reads a redefiner's overlay off the resolved <see cref="DataItem.RedefinesTarget"/> (<c>BitLayout</c>'s walk: a redefining
/// child begins where its target does, §2023:13.18.44.4 GR1), never by matching the written data-name-2 again (the model reads the
/// symbol the table resolved, DESIGN-listing.md section 6.7.1). The link used to be made by <see cref="ResolveRedefinesPass"/>, which
/// runs AFTER <see cref="CheckStorageLimitsPass"/>, and that pass measures every item (<c>RecordLayout.ExceedsSizeLimit</c> through
/// <c>BitLayout.ExactExtentBits</c>, whose phase of a redefining bit group is its target's): measured before the link, a redefining
/// bit group was placed at bit 0 of a byte instead of its target's bit. The SR8 size screen and the other entry rules still run in
/// <see cref="ResolveRedefinesPass"/>, after the size limit, because measuring an over-limit item narrows a size that no admitted item
/// has (kb/Work PB5695 review R1-1); the link measures nothing, so it runs first and every measure reads a linked overlay.</para>
/// <para>A link that finds no data-name-2 leaves the item's target null and its written name in place; <see cref="ResolveRedefinesPass"/>
/// reports it (COBOL1577 or the unresolved-target error) and clears the name, so a layout read between the two passes sees an
/// unlinked redefiner only in a program already in error, and <c>BitLayout</c> answers -1 for it.</para>
/// </remarks>
internal sealed class LinkRedefinitionsPass : IBindPass
{
    /// <inheritdoc/>
    public string Name => "LinkRedefinitions";

    /// <inheritdoc/>
    public PassPhase Requires => PassPhase.SignResolved;

    /// <inheritdoc/>
    public PassPhase Produces => PassPhase.SignResolved;

    /// <inheritdoc/>
    public PassViews Views => PassViews.None;

    /// <summary>Link each redefiner of the COMPOSED forest (roots, TYPE / SAME AS clones and the TYPEDEF templates, kb/Work PB2518:
    /// every clone's target feeds the layout) to the entry its REDEFINES clause names: §2023:13.18.44.3 SR4/SR7 and NOTE 1, the
    /// original definition among the same-named entries that precede it in <see cref="RedefinitionScopeOf"/> (the placement, never a
    /// qualifier: "Data-name-2 shall not be qualified", SR6). A template's own level-01 entry has no siblings to redefine.</summary>
    public void Run(DataBinder binder)
    {
        foreach (var item in binder.CompositionForest())
            if (item.RedefinesTargetName is not null && !(item.Parent is null && item.IsTypedef))
                item.SetRedefinition(
                    DataItem.RedefinedEntry(binder.Symbols.RedefinitionCandidatesOf(item, RedefinitionScopeOf(binder, item))),
                    RedefinitionKind.Clause);
    }

    /// <summary>
    /// The entries a REDEFINES clause of <paramref name="item"/> is resolved and screened among: the ONE statement of that scope,
    /// read by this pass (the link) and by <see cref="ResolveRedefinesPass"/> (§2023:13.18.44.3 SR10's intervening-storage screen).
    /// A subordinate (02+) redefiner's siblings (correct in every scope). A top-level 01/77 METHOD redefiner's own section of the
    /// owning method (§2023:13.18.44.3 — the target is a preceding item in the SAME data description; a method may not redefine
    /// object or program data, §2023:11.7.4 GR5, and cross-section LOCAL / LINKAGE aliasing is illegal, their storage classes differ),
    /// never the cross-scope global roots (M2-OO-1h step 3). Any other top-level redefiner, the unit's roots: the candidates are
    /// narrowed to the subject's own section by <see cref="SymbolTable.RedefinitionCandidatesOf"/>.
    /// </summary>
    internal static IReadOnlyList<DataItem> RedefinitionScopeOf(DataBinder binder, DataItem item)
    {
        if (item.Parent is { } par) return par.Children;
        if (binder.Oo.RootOwner.TryGetValue(item.Root, out var mm))
            return mm.Binding!.LocalRoots.Contains(item) ? mm.Binding!.LocalRoots : mm.Binding!.LinkageRoots;
        return binder.State.Forest.Roots;
    }
}
