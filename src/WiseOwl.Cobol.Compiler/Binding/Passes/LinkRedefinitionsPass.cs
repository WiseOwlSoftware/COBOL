// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Compiler.Binding.Model;

namespace WiseOwl.Cobol.Compiler.Binding.Passes;

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
/// <para>⛔ <b>LINK, THEN CLEAR, ONE ITEM AT A TIME</b> (kb/Work PB93, PB5932 review round 2 R2-1). A clause whose data-name-2 names
/// no preceding entry is reported HERE (COBOL1577 for a method's level-01 entry, otherwise the unresolved-target error, COBOL1654)
/// and its written name cleared before the next item is linked: "no half-state, an unresolved redefiner is an ordinary entry".
/// <see cref="DataItem.RedefinedEntry"/> picks the nearest candidate that is no redefinition by reading each candidate's
/// <see cref="DataItem.RedefinesTargetName"/>, so a later redefiner linked while an earlier unresolved one still carried its name
/// was linked as if that entry were a redefinition (<c>05 A. 05 A REDEFINES ZZ. 05 C REDEFINES A.</c> linked C to the FIRST A)
/// and then screened as if it were ordinary (SR10's spurious "the entry 'A' between them defines new storage"). The link is the
/// resolution, so the resolution's verdict is reported with it; the entry rules over a linked pair stay in
/// <see cref="ResolveRedefinesPass"/>.</para>
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
    /// qualifier: "Data-name-2 shall not be qualified", SR6). A template's own level-01 entry has no siblings to redefine.
    /// <para>Every clone is linked in its own scope (its target feeds the layout); only a WRITTEN entry (no clone) is reported, so a
    /// type is diagnosed once, at its declaration (kb/Work PB2518).</para></summary>
    public void Run(DataBinder binder)
    {
        foreach (var item in binder.CompositionForest())
            if (item.RedefinesTargetName is { } tname && !(item.Parent is null && item.IsTypedef))
            {
                item.SetRedefinition(
                    DataItem.RedefinedEntry(binder.Symbols.RedefinitionCandidatesOf(item, RedefinitionScopeOf(binder, item))),
                    RedefinitionKind.Clause);
                if (item.RedefinesTarget is not null)
                {
                    // The written data-name-2 is a reference to the entry it is linked to (DESIGN-listing.md section 6.7.1).
                    if (item.ClonedFrom is null) binder.Symbols.RecordRedefinition(item);
                    continue;
                }
                if (item.ClonedFrom is null) ReportUnresolved(binder, item, tname);
                item.RedefinesTargetName = null;   // no half-state: an unresolved redefiner is an ordinary entry
            }
    }

    /// <summary>The one diagnostic of a REDEFINES clause whose data-name-2 names no preceding entry of its scope.</summary>
    private static void ReportUnresolved(DataBinder binder, DataItem item, string tname)
    {
        using var _ = binder.Edition.At(item);
        // A method 01 REDEFINES whose target isn't in the method's own roots is a scope error (never a silent cross-scope bind
        // to an object/program item) — §2023:11.7.4 GR5. 1577, renumbered from a bare "COBOL1518" that collided with the
        // locale-module non-support meaning (review V11 — the code comes from the catalog descriptor, never a literal).
        if (item.Parent is null && binder.Oo.RootOwner.ContainsKey(item.Root))
            binder.Edition.Error(DiagnosticCatalog.MethodRedefinesScope,
                $"REDEFINES target '{tname}' of method data item "
                + $"'{item.CobolName ?? "FILLER"}' is not a preceding item in the same method scope "
                + "(ISO §2023:13.18.44.3 — a method item may not redefine object or program data)");
        else
            // kb/Work PB93: the PROGRAM-scope miss used to be silent — the item kept RedefinesTargetName (so BitLayout /
            // ImageWidth / ByteWidth skipped it as an overlay) against a null RedefinesTarget (so the emitter gave it its own
            // field): a storage shape no edition defines. ONE diagnostic; the item then binds as an ORDINARY entry.
            binder.Edition.Error(DiagnosticCatalog.RedefinesTargetUnresolved,
                $"'{item.CobolName ?? "FILLER"}' REDEFINES '{tname}': data-name-2 does not name a preceding "
                + "entry in the same scope ("
                + binder.Edition.Edition.CiteRule($"§1985:VI.5.10.3 SR11; {binder.Edition.Edition.Citation()}",
                    "§2023:13.18.44.3 SR4/SR7/SR10; §2023:8.4.2.1") + ")");
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
