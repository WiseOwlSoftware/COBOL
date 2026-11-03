// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;

namespace WiseOwl.Cobol.Binding.Passes;

/// <summary>
/// The CheckSignClauses pass: row 10 of the per-unit resolve pipeline (UsageResolved to UsageResolved).
/// </summary>
/// <remarks>
/// <para><b>Placement in the pipeline.</b>
/// The §13.18.52.3 SR1/SR2 SIGN clause subject screen (kb/Work PB537). Placed HERE for the ALIGNED screen's reason — SR1's group bullet asks the group KIND (a bit group can be one by §13.16.4 GR1 inheritance) and SR2 asks the elementary item's usage (§13.18.60.4 GR1 can supply it) — and BEFORE InheritSignClauses, because a refused clause is cleared and must not go on to shed onto subordinates.
/// </para>
/// </remarks>
internal sealed class CheckSignClausesPass : IBindPass
{
    /// <inheritdoc/>
    public string Name => "CheckSignClauses";

    /// <inheritdoc/>
    public PassPhase Requires => PassPhase.UsageResolved;

    /// <inheritdoc/>
    public PassPhase Produces => PassPhase.UsageResolved;

    /// <inheritdoc/>
    public PassViews Views => PassViews.None;

    /// <summary>The group kinds §13.18.52.3 SR1's third bullet admits.</summary>
    private const GroupKinds SignClauseGroupKinds =
        GroupKinds.Alphanumeric | GroupKinds.National | GroupKinds.StronglyTyped;

    /// <summary>§13.18.52.3 SR1/SR2 over every data description entry that wrote a SIGN clause. A violation
    /// CLEARS the clause, so the item binds under an already-failed compile exactly as if it had not been written
    /// (the <c>CheckAlignedClauses</c> discipline).</summary>
    public void Run(DataBinder binder)
    {
        foreach (var item in binder.ConformanceForest())
        {
            if (item.OwnSign is null || !binder.State.Clauses.SignClauseWritten.Contains(item)) continue;
            string? defect = item.IsGroup
                ? SignClauseGroupDefect(ItemCategory.GroupKindsOf(item))
                : DataBinder.SignClauseElementaryDefect(item.Pic, item.PictureText, "data description entry");
            if (defect is not null)
            {
                using var _ = binder.Edition.At(item);
                binder.Edition.Error(DiagnosticCatalog.SignClauseSubject,
                    $"data item '{item.CobolName ?? "FILLER"}': {defect}");
                item.OwnSign = null;
            }
            else if (item.IsGroup)
                GateGroupSignSubordinates(binder, item);
        }
    }

    /// <summary>⛔ THE COBOL-85 GROUP SUBJECT IS NARROWER (kb/Work PB1319). ANSI X3.23-1985 admitted the clause on "a
    /// numeric data description entry whose PICTURE contains the character S, or a group item containing at least one
    /// such numeric data description entry", and required "The numeric data description entries to which the SIGN clause
    /// applies" to be "described … as USAGE IS DISPLAY". The 2023 §13.18.52.3 keeps neither: SR1 admits any alphanumeric,
    /// national or strongly-typed group and SR2 restricts only "an elementary item for which the SIGN clause is
    /// specified". Each relaxation is a construct row introduced at 2002 (the derived edge, VCR row 7.40), so the 85
    /// compiler refuses what the 85 text refuses through the canonical funnel, and later editions take the 2023 rules.
    /// A subordinate that writes its own SIGN clause is the subject of that clause (§13.18.52.4 GR2/GR3), so the group's
    /// clause applies neither to it nor below it.</summary>
    private static void GateGroupSignSubordinates(DataBinder binder, DataItem group)
    {
        if (binder.Edition.DialectLevel >= 2002) return;
        string where = $"data item '{group.CobolName ?? "FILLER"}'";
        using var _ = binder.Edition.At(group);
        if (!DataItem.DescendantsOf(group).Any(IsSignedNumericEntry))
            ConstructRegistry.Check(binder.Edition.Edition, binder.Edition.Sink, Constructs.SignGroupUnsignedSubordinates2002, where);
        if (SignedSubordinatesTheClauseReaches(group).FirstOrDefault(d => d.Pic!.Usage is not Usage.Display) is { } other)
            ConstructRegistry.Check(binder.Edition.Edition, binder.Edition.Sink, Constructs.SignGroupNondisplaySubordinate2002,
                $"{where} over '{other.CobolName ?? "FILLER"}' (USAGE {UsageFamilies.UsageWord(other.Pic!.Usage)})");
    }

    /// <summary>A numeric data description entry whose WRITTEN picture character-string contains the symbol 'S' — the
    /// entry both editions' SIGN rules name.</summary>
    private static bool IsSignedNumericEntry(DataItem d) =>
        d.IsElementary && d.Pic is { Category: PicCategory.Numeric, Signed: true, IsRecovery: false }
        && d.PictureText is not null;

    /// <summary>The signed numeric items below <paramref name="group"/> that its own clause applies to: every one not
    /// at or under a subordinate entry that writes its own SIGN clause (§13.18.52.4 GR2/GR3).</summary>
    private static IEnumerable<DataItem> SignedSubordinatesTheClauseReaches(DataItem group)
    {
        foreach (var c in group.Children)
        {
            if (c.OwnSign is not null) continue;
            if (IsSignedNumericEntry(c)) yield return c;
            foreach (var d in SignedSubordinatesTheClauseReaches(c)) yield return d;
        }
    }

    /// <summary>SR1's group bullet — null when <paramref name="kinds"/> is one it admits.</summary>
    private static string? SignClauseGroupDefect(GroupKinds kinds) =>
        (kinds & SignClauseGroupKinds) != 0 ? null
            : "the SIGN clause may be specified for a group item only when it is an alphanumeric group item, "
              + $"national group item, or strongly-typed group item (ISO §13.18.52.3 SR1) — this entry is one of the "
              + $"{ItemCategory.Spell(kinds)}";
}
