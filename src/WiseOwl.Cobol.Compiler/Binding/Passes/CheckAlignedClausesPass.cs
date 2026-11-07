// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions.Diagnostics;

namespace WiseOwl.Cobol.Binding.Passes;

/// <summary>
/// The CheckAlignedClauses pass: row 6 of the per-unit resolve pipeline (UsageResolved to UsageResolved).
/// </summary>
/// <remarks>
/// <para><b>Placement in the pipeline.</b>
/// The §2023:13.18.1.3 SR1 ALIGNED subject screen (kb/Work PB487). Placed HERE for the SAME reason as the pass above: SR1 asks whether the entry is "a bit group item or an elementary bit data item", and a group becomes a bit group by §2023:13.16.4 GR1 INHERITANCE, which UsageInheritancePass settles. Nothing later than that — it reads only declared shape, and BitLayout must not lay out an item whose ALIGNED clause was never adjudicated.
/// </para>
/// <para>
/// The ALIGNED clause's one syntax rule — <b>ISO §2023:13.18.1.3 SR1</b>: "The ALIGNED clause may be specified only
/// for a bit group item or an elementary bit data item."
///
/// <para><b>Why this exists at all (kb/Work PB487).</b> ALIGNED is a clause of the §2023:13.16.2 Format-1 general
/// format and had NO grammar rule anywhere in the frontend. It reached the parser as an <c>IDENTIFIER</c> and was
/// eaten by the vendor-extension catch-all that used to close the clause list, so
/// <c>05 B2 PIC 1(4) USAGE BIT ALIGNED.</c> compiled clean, laid out at the SAME bit offset as without the
/// clause, and <c>FUNCTION LENGTH</c> of the containing group answered 1 where §2023:13.18.1.4 GR1 requires 2. That
/// is the silent-wrong-layout outcome the loud-guard discipline exists to prevent, and it is why the clause is
/// now a rule of its own, adjudicated here and honoured at exactly one layout site.</para>
///
/// <para><b>Why a POST-FOREST pass and not <c>BindEntry</c>.</b> SR1's subject test asks whether the entry is "a
/// bit group item or an elementary bit data item". A group can become a bit group by INHERITANCE — §2023:13.16.4 GR1:
/// "If the subject of the entry is a group item that is subordinate to a bit group and a GROUP-USAGE BIT clause
/// is not specified, a GROUP-USAGE BIT clause is implied for the subject of the entry" — which
/// <c>UsageInheritancePass</c> settles, so a check inside <c>BindEntry</c> would reject the legal
/// <c>05 INNER ALIGNED.</c> under a <c>GROUP-USAGE BIT</c> parent. This is the identical placement argument
/// <see cref="CheckUsageDeclarationsPass.Run"/> records for §2023:13.18.60.3 SR14, and the pass sits immediately after it.</para>
///
/// <para><b>The subject predicate is <see cref="BitLayout.IsBitItem"/>, not a second spelling of it.</b> That
/// method already answers §2023:8.5.1.6.3's own question — "an elementary bit data item or bit group item" — for the
/// layout walk, and SR1 asks the same question in the same words. One predicate, so the rule that DECIDES
/// whether ALIGNED is legal and the walk that IMPLEMENTS it cannot disagree about what a bit item is.</para>
///
/// <para><b>Edition axis.</b> ALIGNED is reserved from COBOL-2002 (§2023:8.9; <c>ReservedWords.Table.cs</c>), so at
/// <c>--std 85</c> the word is a user-defined word and never reaches this pass as a clause — the reservation
/// gate on <c>cobolWord</c>/<c>reservedGatedWord</c> routes it to the §2023:8.9 funnel instead. The 2002 introduction
/// gate for the CLAUSE is <c>VersionConformancePass ParseArm.VisitAlignedClause</c>; this pass is a plain syntax
/// rule and is edition-agnostic, per the design's binder-is-edition-agnostic invariant.</para>
/// </para>
/// </remarks>
internal sealed class CheckAlignedClausesPass : IBindPass
{
    /// <inheritdoc/>
    public string Name => "CheckAlignedClauses";

    /// <inheritdoc/>
    public PassPhase Requires => PassPhase.UsageResolved;

    /// <inheritdoc/>
    public PassPhase Produces => PassPhase.UsageResolved;

    /// <inheritdoc/>
    public PassViews Views => PassViews.None;

    /// <summary>§2023:13.18.1.3 SR1 — one verdict per offending WRITTEN entry. A violation CLEARS
    /// <see cref="DataItem.IsAligned"/> so the item lays out by the default §2023:8.5.1.6.3 rules under an
    /// already-failed compile (the <c>IsBased</c> discipline: never a half-shaped item).
    /// <para>⛔ Runs over <see cref="DataBinder.ConformanceForest"/>, not <see cref="DataBinder.CompositionForest"/> — this is a
    /// property of the entry the programmer WROTE, so a TYPEDEF template is judged once at the template rather
    /// than once per <c>TYPE</c> reference site, which is the entry the programmer would have to change.</para></summary>
    public void Run(DataBinder binder)
    {
        foreach (var item in binder.ConformanceForest())
        {
            if (!item.IsAligned) continue;
            if (BitLayout.IsBitItem(item)) continue;

            using var _ = binder.Edition.At(item);
            binder.Edition.Error(DiagnosticCatalog.AlignedClauseSubject,
                $"data item '{item.CobolName ?? "FILLER"}': the ALIGNED clause may be specified only for a bit "
                + "group item or an elementary bit data item (ISO §2023:13.18.1.3 SR1) — this entry is "
                + $"{DescribeAlignedSubject(item)}");
            item.IsAligned = false;
        }
    }

    /// <summary>What the offending subject actually IS, so the diagnostic names the mismatch rather than
    /// restating the rule. The words are <see cref="ItemCategory.Face(DataItem)"/>'s — the one reading of a
    /// group's kind (<see cref="ItemCategory.GroupKindsOf"/>: national, strongly-typed, variable-length,
    /// alphanumeric) and of an elementary item's Table 2 category — so this pass keeps no classification of its
    /// own (kb/Work PB1654). Three hints ride on top because they exist only for this rule: what a plain group
    /// would have to carry to be a bit group item (§2023:13.16.4 GR1), what makes an elementary boolean item a BIT
    /// data item (§2023:13.18.60.4 GR5) — said after the item's category face and never of a usage, because a boolean
    /// item may be described with USAGE DISPLAY or USAGE NATIONAL (§2023:13.18.60.3 SR12) as well as BIT (kb/Work
    /// PB4603) — and — because this pass judges the TEMPLATE once, where no item is yet
    /// strongly typed (§2023:8.5.3.1) — that a group inside a STRONG type declaration will be one in every item
    /// described with the type, which no GROUP-USAGE BIT clause can turn into a bit group (§2023:13.18.29.3 SR1), so
    /// the "write GROUP-USAGE BIT" hint would send the programmer to a second refusal.</summary>
    private static string DescribeAlignedSubject(DataItem item) =>
        item.Pic is { Category: PicCategory.Boolean }
            ? ItemCategory.Face(item) + " — an elementary BIT data item is one described with USAGE BIT "
              + "(§2023:13.18.60.4 GR5)"
        : ItemCategory.GroupKindsOf(item) is not GroupKinds.Alphanumeric
            ? ItemCategory.Face(item)
        : item.DeclaresStrongType || DataBinder.UnderStrongTypeDeclaration(item)
            ? "a group item of a type declaration with the STRONG phrase — every item described with the type is "
              + "a strongly-typed group item (ISO §2023:8.5.3.1), which cannot be a bit group item (§2023:13.18.29.3 SR1)"
        : ItemCategory.Face(item) + " — a bit group item is one described with, or inheriting "
              + "(§2023:13.16.4 GR1), a GROUP-USAGE BIT clause";
}
