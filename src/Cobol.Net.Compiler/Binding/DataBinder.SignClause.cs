// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;

namespace CobolNet.Binding;

/// <summary>
/// The SIGN clause's two placement rules — <b>ISO §13.18.52.3 SR1</b> ("The SIGN clause may be specified only
/// for: — a numeric data or screen description entry whose picture character-string contains the symbol 'S' —
/// a numeric report group description entry whose picture character-string contains the symbol 'S' — an
/// alphanumeric group item, national group item, or strongly-typed group item.") and <b>SR2</b> ("The usage of
/// an elementary item for which the SIGN clause is specified shall be display or national.") — screened in ONE
/// predicate that both entry kinds read (kb/Work PB537).
///
/// <para><b>What this replaced.</b> The clause was captured with no screen at all. The only SR1 shape anyone had
/// implemented was a special case beside it — a floating-point numeric-edited picture, reported under the
/// PICTURE clause's own code — so <c>PIC 9(3)</c>, <c>PIC X(3)</c>, <c>PIC N(3)</c>, <c>PIC ZZ9</c>, a
/// <c>GROUP-USAGE BIT</c> group, and <c>PIC S9(4) COMP</c> / <c>COMP-3</c> / <c>COMP-5</c> / <c>USAGE INDEX</c>
/// each compiled clean at every edition, and the report arm had no screen either. That special case is now the
/// general rule's numeric-edited arm and was deleted.</para>
///
/// <para><b>Why a POST-FOREST pass for the data description arm.</b> Both halves ask facts entry bind does not
/// yet have: SR1's third bullet asks whether the entry is a GROUP and of which kind (a group is known once its
/// subordinates are, and a group becomes a bit group by §13.16.4 GR1 inheritance), and SR2 asks the elementary
/// item's USAGE, which a group-level USAGE clause supplies by §13.18.60.4 GR1 — both settled by
/// <c>UsageInheritancePass</c>. So the pass sits right after it, beside the §13.18.1.3 SR1 ALIGNED screen for the
/// same reason.</para>
///
/// <para><b>Why the subject is the entry that WROTE the clause.</b> <see cref="DataItem.OwnSign"/> is also written
/// by the description copies — a TYPE subject assumes its template's clause (§13.18.57.4 GR1) and a SAME AS
/// subject a group-level clause of data-name-1's ancestor (§13.18.49.4 GR5) — and neither is a SIGN clause the
/// subject's entry specifies. The template is screened once where it is declared; re-screening its copy would
/// report one source entry once per reference site, and screening a GR5-assumed ancestor clause on an
/// alphanumeric subject would refuse legal source. So the capture site records its own entry
/// (<see cref="_signClauseWritten"/>) and only those entries are screened.</para>
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>The data description entries whose OWN source wrote a SIGN clause — the subjects §13.18.52.3 SR1/SR2
    /// speak about. Filled at entry bind; read once by <see cref="CheckSignClauses"/>.</summary>
    private readonly HashSet<DataItem> _signClauseWritten = [];

    /// <summary>The group kinds §13.18.52.3 SR1's third bullet admits.</summary>
    private const GroupKinds SignClauseGroupKinds =
        GroupKinds.Alphanumeric | GroupKinds.National | GroupKinds.StronglyTyped;

    /// <summary>§13.18.52.3 SR1/SR2 over every data description entry that wrote a SIGN clause. A violation
    /// CLEARS the clause, so the item binds under an already-failed compile exactly as if it had not been written
    /// (the <c>CheckAlignedClauses</c> discipline).</summary>
    internal void CheckSignClauses()
    {
        foreach (var item in ConformanceForest())
        {
            if (item.OwnSign is null || !_signClauseWritten.Contains(item)) continue;
            string? defect = item.IsGroup
                ? SignClauseGroupDefect(ItemCategory.GroupKindsOf(item))
                : SignClauseElementaryDefect(item.Pic, item.PictureText, "data description entry");
            if (defect is null) continue;
            using var _ = Edition.At(item);
            Edition.Error(DiagnosticCatalog.SignClauseSubject,
                $"data item '{item.CobolName ?? "FILLER"}': {defect}");
            item.OwnSign = null;
        }
    }

    /// <summary>SR1's group bullet — null when <paramref name="kinds"/> is one it admits.</summary>
    private static string? SignClauseGroupDefect(GroupKinds kinds) =>
        (kinds & SignClauseGroupKinds) != 0 ? null
            : "the SIGN clause may be specified for a group item only when it is an alphanumeric group item, "
              + $"national group item, or strongly-typed group item (ISO §13.18.52.3 SR1) — this entry is one of the "
              + $"{ItemCategory.Spell(kinds)}";

    /// <summary>⛔ THE ONE ELEMENTARY-SUBJECT TEST for the SIGN clause, read by the data description entry
    /// (<see cref="CheckSignClauses"/>) and the report group description entry (<c>BindReportEntry</c>) alike:
    /// SR1's first two bullets are one test — "a numeric … entry whose picture character-string contains the
    /// symbol 'S'" — and SR2 names "an elementary item" of either kind. Null when legal, or when the entry's
    /// PICTURE was itself refused (a recovery profile answers no question about the source).</summary>
    /// <param name="pic">The entry's analyzed PICTURE / usage profile, its usage already inherited.</param>
    /// <param name="pictureText">The picture character-string the entry WROTE (null when none).</param>
    /// <param name="entryKind">"data description entry" / "report group description entry", for the message.</param>
    internal static string? SignClauseElementaryDefect(PicInfo? pic, string? pictureText, string entryKind)
    {
        if (pic is null || pic.IsRecovery) return null;
        // SR1 — category numeric, with an S in a WRITTEN picture character-string (PicInfo.Signed is the S; a
        // PICTURE-less signed usage such as BINARY-LONG SIGNED or FLOAT-LONG has no character-string to hold one).
        if (pic.Category is not PicCategory.Numeric || !pic.Signed || pictureText is null)
            return $"the SIGN clause may be specified for an elementary {entryKind} only when it is numeric and its "
                + "picture character-string contains the symbol 'S' (ISO §13.18.52.3 SR1) — this entry "
                + (pictureText is null
                    ? $"has no PICTURE clause (USAGE {UsageFamilies.UsageWord(pic.Usage)})"
                    : pic.Category is PicCategory.Numeric
                        ? $"is numeric but PICTURE {pictureText} has no 'S'"
                        : $"is of category {pic.Category.ToString().ToLowerInvariant()} (PICTURE {pictureText})");
        // SR2 — the usage of the elementary subject, written or inherited (§13.18.60.4 GR1).
        if (pic.Usage is not (Usage.Display or Usage.National))
            return "the usage of an elementary item for which the SIGN clause is specified shall be display or "
                + $"national (ISO §13.18.52.3 SR2) — this entry's usage is {UsageFamilies.UsageWord(pic.Usage)}";
        return null;
    }
}
