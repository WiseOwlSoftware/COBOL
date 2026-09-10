// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;

namespace CobolNet.Binding;

/// <summary>
/// The ALIGNED clause's one syntax rule — <b>ISO §13.18.1.3 SR1</b>: "The ALIGNED clause may be specified only
/// for a bit group item or an elementary bit data item."
///
/// <para><b>Why this exists at all (kb/Work PB487).</b> ALIGNED is a clause of the §13.16.2 Format-1 general
/// format and had NO grammar rule anywhere in the frontend. It reached the parser as an <c>IDENTIFIER</c> and was
/// eaten by the vendor-extension catch-all that used to close the clause list, so
/// <c>05 B2 PIC 1(4) USAGE BIT ALIGNED.</c> compiled clean, laid out at the SAME bit offset as without the
/// clause, and <c>FUNCTION LENGTH</c> of the containing group answered 1 where §13.18.1.4 GR1 requires 2. That
/// is the silent-wrong-layout outcome the loud-guard discipline exists to prevent, and it is why the clause is
/// now a rule of its own, adjudicated here and honoured at exactly one layout site.</para>
///
/// <para><b>Why a POST-FOREST pass and not <c>BindEntry</c>.</b> SR1's subject test asks whether the entry is "a
/// bit group item or an elementary bit data item". A group can become a bit group by INHERITANCE — §13.16.4 GR1:
/// "If the subject of the entry is a group item that is subordinate to a bit group and a GROUP-USAGE BIT clause
/// is not specified, a GROUP-USAGE BIT clause is implied for the subject of the entry" — which
/// <c>UsageInheritancePass</c> settles, so a check inside <c>BindEntry</c> would reject the legal
/// <c>05 INNER ALIGNED.</c> under a <c>GROUP-USAGE BIT</c> parent. This is the identical placement argument
/// <see cref="CheckUsageDeclarations"/> records for §13.18.60.3 SR14, and the pass sits immediately after it.</para>
///
/// <para><b>The subject predicate is <see cref="BitLayout.IsBitItem"/>, not a second spelling of it.</b> That
/// method already answers §8.5.1.6.3's own question — "an elementary bit data item or bit group item" — for the
/// layout walk, and SR1 asks the same question in the same words. One predicate, so the rule that DECIDES
/// whether ALIGNED is legal and the walk that IMPLEMENTS it cannot disagree about what a bit item is.</para>
///
/// <para><b>Edition axis.</b> ALIGNED is reserved from COBOL-2002 (§8.9; <c>ReservedWords.Table.cs</c>), so at
/// <c>--std 85</c> the word is a user-defined word and never reaches this pass as a clause — the reservation
/// gate on <c>cobolWord</c>/<c>reservedGatedWord</c> routes it to the §8.9 funnel instead. The 2002 introduction
/// gate for the CLAUSE is <c>VersionConformancePass ParseArm.VisitAlignedClause</c>; this pass is a plain syntax
/// rule and is edition-agnostic, per the design's binder-is-edition-agnostic invariant.</para>
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>§13.18.1.3 SR1 — one verdict per offending WRITTEN entry. A violation CLEARS
    /// <see cref="DataItem.IsAligned"/> so the item lays out by the default §8.5.1.6.3 rules under an
    /// already-failed compile (the <c>IsBased</c> discipline: never a half-shaped item).
    /// <para>⛔ Runs over <see cref="ConformanceForest"/>, not <see cref="CompositionForest"/> — this is a
    /// property of the entry the programmer WROTE, so a TYPEDEF template is judged once at the template rather
    /// than once per <c>TYPE</c> reference site, which is the entry the programmer would have to change.</para></summary>
    internal void CheckAlignedClauses()
    {
        foreach (var item in ConformanceForest())
        {
            if (!item.IsAligned) continue;
            if (BitLayout.IsBitItem(item)) continue;

            using var _ = Edition.At(item);
            Edition.Error(DiagnosticCatalog.AlignedClauseSubject,
                $"data item '{item.CobolName ?? "FILLER"}': the ALIGNED clause may be specified only for a bit "
                + "group item or an elementary bit data item (ISO §13.18.1.3 SR1) — this entry is "
                + $"{DescribeAlignedSubject(item)}");
            item.IsAligned = false;
        }
    }

    /// <summary>What the offending subject actually IS, so the diagnostic names the mismatch rather than
    /// restating the rule. Kept next to the check because it exists only for that message.</summary>
    private static string DescribeAlignedSubject(DataItem item) =>
        !item.IsElementary
            ? item.GroupUsage is GroupUsage.National
                ? "a national group item (GROUP-USAGE NATIONAL)"
                : "an alphanumeric group item — a bit group item is one described with, or inheriting "
                  + "(§13.16.4 GR1), a GROUP-USAGE BIT clause"
        : item.Pic is { Category: PicCategory.Boolean }
            ? "an elementary boolean item whose usage is DISPLAY — an elementary BIT data item is one described "
              + "with USAGE BIT (§13.18.60.4 GR5)"
        : $"an elementary item of category {item.Pic?.Category.ToString()?.ToLowerInvariant() ?? "unknown"}";
}
