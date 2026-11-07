// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding.Passes;
using WiseOwl.Cobol.Runtime;

/// <summary>
/// The ExpandTypes pass: row 2 of the per-unit resolve pipeline (None to TypesExpanded).
/// </summary>
internal sealed class ExpandTypesPass : IBindPass
{
    /// <inheritdoc/>
    public string Name => "ExpandTypes";

    /// <inheritdoc/>
    public PassPhase Requires => PassPhase.None;

    /// <inheritdoc/>
    public PassPhase Produces => PassPhase.TypesExpanded;

    /// <inheritdoc/>
    public PassViews Views => PassViews.None;

    // ── TYPEDEF / the TYPE clause (ISO §2023:13.18.58 / §2023:13.18.57; data-model D17) ──────────────────────────────────

    /// <summary>Post-build TYPE-expansion pass: every <c>TYPE IS type-name</c> reference (a real item in the forest)
    /// gets the referenced type declaration's subtree cloned in. Runs at the top of <see cref="DataBinder.BindResolve"/> — AFTER
    /// all <see cref="DataBinder.BindEntries"/> (so forward references resolve; every TYPEDEF is in <see cref="ForestSlice.TypeDecls"/>),
    /// AFTER <see cref="SynthesizeImpliedPicturesPass.Run"/> (so a template's §2023:13.16.3 SR9 VALUE-implied PICTURE is part of
    /// the description it hands over — §2023:13.18.57.4 GR3; kb/Work PB1300) and BEFORE the resolution passes (so the
    /// clone is a normal part of the forest they walk).</summary>
    public void Run(DataBinder binder)
    {
        // ⛔ THE TYPE DECLARATIONS FIRST, EACH COMPLETED AS A DECLARATION (kb/Work PB1302). Every TYPE and SAME AS
        // reference among a template's members is expanded IN the template, once, before any storage item copies
        // it, so the rules whose subject is the declaration are asked of the declaration — §2023:13.18.58.3 SR2's
        // recursion ("The description of the subject of the entry, including its subordinate items, shall not
        // contain a TYPE clause that directly or indirectly references this type definition") and §2023:13.18.57.3
        // SR6's placement of a member typed by a STRONG declaration — whether or not anything references it.
        // They used to be side effects of per-REFERENCE expansion, so a recursive type declaration that no
        // storage item expanded compiled clean, and the answer for one that was depended on declaration order.
        // §2023:8.4.2.2.1 rule 4's antecedent, recorded BEFORE anything is expanded or resolved (kb/Work PB1476): "a type
        // declaration entry for which the type-name is not referenced in any TYPE clause in the source unit". EVERY
        // written TYPE clause counts — a storage item's, a linkage item's, and a member's of another type
        // declaration, which nothing may ever copy — so the exemption's lapse is a property of the source, never
        // of which clones happen to reach the name index. The WRITTEN clause is read (DataItem.WrittenTypeName), never
        // the pending mark: a constant's length phrase may have expanded an entry already (kb/Work PB2465).
        foreach (var written in binder.DeclaredForest())
            if (written.WrittenTypeName is { } referenced && binder.TryFindTypeDecl(referenced, binder.ScopeOf(written.Root), out var decl))
                decl.ReferencedByTypeClause = true;
        foreach (var template in binder.AllTypeDecls().ToList())
            binder.ExpandTemplate(template);
        foreach (var item in binder.AllItems().Where(i => i.TypeRefName is not null).ToList())
            binder.ExpandType(item);
        // SAME AS (ISO §2023:13.18.49; P10 Step 16) — expanded AFTER every TYPE reference, so a data-name-1 that
        // was itself declared with a TYPE clause copies its fully-EXPANDED description (GR1 — "as though the
        // data description identified by data-name-1 had been coded in place"). Chains (a target with its own
        // pending SAME AS) recurse inside ExpandSameAs; cycles are the §2023:13.18.49.3 SR3 rejection.
        foreach (var item in binder.AllItems().Where(i => i.SameAsName is not null).ToList())
            binder.ExpandSameAs(item, []);
        // §2023:13.18.55.4 GR1 over the composed storage forest — a TYPE subject or SAME AS subject that writes the clause
        // reaches the leaves it copied (DataBinder.SynchronizedClause.cs; the templates took it in ExpandTemplate).
        foreach (var root in binder.State.Forest.Roots) binder.InheritSynchronized(root);
        ResolveRestrictedTypes(binder);
        IndexLapsedTypeMembers(binder);
    }

    /// <summary>Attach the TYPE DECLARATION a restricted data-pointer's <c>USAGE POINTER TO type-name-1</c> names
    /// (ISO §2023:13.18.60.4 GR23) to its <see cref="PicInfo.RestrictedTypeDecl"/>. Runs at the TAIL of
    /// <see cref="Run"/>, so every template's own nested TYPE / SAME AS references are already expanded
    /// and the declaration it attaches is the complete one.
    /// <para>The restriction's carried identity used to be the NAME alone, which decides correctly WITHIN a
    /// source element (§2023:8.3.2.2 lets a user-defined word be only one type of user-defined word there) and wrongly ACROSS them: two elements may each
    /// declare a non-equivalent type under one name, and §2023:8.5.3.1 makes equivalence — not the spelling — the
    /// test §2023:14.9.39.3 SR19, §2023:14.9.3.3 SR4/SR5 and §2023:14.8.2.3.2 spend (kb/Work PB427). No diagnostic is
    /// raised for an unresolved name: §2023:13.18.60.3 SR18 already requires such an entry to be a TYPEDEF, and a
    /// restriction whose declaration is not in hand falls back to the name comparison rather than rejecting.</para>
    /// <para>⛔ It walks <see cref="DataBinder.ClauseResolutionForest"/>, the TYPEDEF templates too (kb/Work PB3120): a template is
    /// itself a form of the type declaration the model compares and keys (<c>StrongTypeModel.EquivalentTypeDeclarations</c>,
    /// <c>TypeIdentityKey</c>), so a pointer member restricted to a type must resolve in the template exactly as in the
    /// TYPE subject cloned from it, or the two forms of one declaration key apart.</para></summary>
    private static void ResolveRestrictedTypes(DataBinder binder)
    {
        foreach (var item in binder.ClauseResolutionForest())
            if (item.Pic is { Category: PicCategory.Pointer, RestrictedTypeName: { } tn, RestrictedTypeDecl: null }
                && binder.TryFindTypeDecl(tn, binder.ScopeOf(item.Root), out var decl))
                item.Pic = item.Pic with { RestrictedTypeDecl = decl };
    }

    /// <summary>Build <see cref="ClauseFacts.LapsedTypeMembers"/> — the tail of <see cref="Run"/>.</summary>
    private static void IndexLapsedTypeMembers(DataBinder binder)
    {
        var index = new Dictionary<string, List<DataItem>>(CobolNames.Comparer);
        foreach (var decl in binder.State.Forest.TypeDecls.Values.Where(d => d.ReferencedByTypeClause))
            foreach (var member in DataItem.DescendantsOf(decl))
                if (member.CobolName is { } n)
                {
                    if (!index.TryGetValue(n, out var list)) index[n] = list = [];
                    list.Add(member);
                }
        binder.State.Clauses.LapsedTypeMembers = index;
    }
}
