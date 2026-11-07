// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Cst;

namespace WiseOwl.Cobol.Binding.Passes;

/// <summary>
/// The CheckPictureRequired pass: row 8 of the per-unit resolve pipeline (UsageResolved to UsageResolved).
/// </summary>
/// <remarks>
/// <para><b>Placement in the pipeline.</b>
/// The §2023:13.16.3 SR8 closing guard — every elementary item has a PICTURE or a synthesized profile (kb/Work PB487). Placed immediately after UsageInheritancePass because that is the LAST pass which can legitimately fill a null Pic (a group header shedding its usage to leaves, §2023:13.18.60.4 GR1; a PICTURE-less INDEX / OBJECT REFERENCE leaf taking the inherited profile), and BEFORE everything that reads a Pic. Without it `01 M.` — plain COBOL, no exotic clause — reached MoveEmitter with a null Pic and crashed the compiler with an unhandled NullReferenceException and no diagnostic at all.
/// </para>
/// </remarks>
internal sealed class CheckPictureRequiredPass : IBindPass
{
    /// <inheritdoc/>
    public string Name => "CheckPictureRequired";

    /// <inheritdoc/>
    public PassPhase Requires => PassPhase.UsageResolved;

    /// <inheritdoc/>
    public PassPhase Produces => PassPhase.UsageResolved;

    /// <inheritdoc/>
    public PassViews Views => PassViews.None;

    /// <summary>The <see cref="ClausePlacementKind.ElementaryOnly"/> rows over the finished forest — the
    /// "has this clause ⇒ is elementary" direction for PICTURE (§2023:13.18.40.3 SR1), JUSTIFIED and BLANK WHEN ZERO
    /// (§2023:13.16.3 SR11). §2023:8.5.1.3.1: "The most basic subdivisions of a record, that is, those not further
    /// subdivided, are called elementary items", so the test is <see cref="DataBinder.HasNoSubordinates"/>, never
    /// <see cref="DataItem.IsElementary"/> (which is DEFINED as <c>Pic is not null</c> and would make the PICTURE
    /// row circular).
    /// <para>Reported over <see cref="DataBinder.ConformanceForest"/> (a defective TYPEDEF template is named once, at the
    /// entry the programmer must change) and RECOVERED over <see cref="DataBinder.CompositionForest"/> (every clone
    /// <c>ExpandTypes</c> made is repaired too). The recovery clears the clause, never the subordinates: for
    /// PICTURE that makes the entry the group its hierarchy declares (kb/Work PB527 — the model cannot hold an
    /// entry that is both), and for JUSTIFIED / BLANK WHEN ZERO it drops a flag no group consumer may read.</para></summary>
    private static void CheckElementaryOnlyClauses(DataBinder binder)
    {
        foreach (var item in binder.ConformanceForest())
        {
            if (DataBinder.HasNoSubordinates(item)) continue;
            foreach (var rule in ClausePlacementRules.Rules)
            {
                if (rule.Kind is not ClausePlacementKind.ElementaryOnly) continue;
                var present = ElementaryOnlyClausesOn(item) & rule.Clauses;
                if (present == DataClauseKind.None) continue;
                int n = item.Children.Count;
                using var _ = binder.Edition.At(item);
                binder.Edition.Error(rule.Code, $"data item '{item.CobolName ?? "FILLER"}': the {DataClauseKinds.Name(present)} "
                    + $"clause may be specified only at the elementary level (ISO {rule.Rule}) — this entry has {n} "
                    + $"subordinate {(n == 1 ? "entry" : "entries")}, so it is not an elementary item (§2023:8.5.1.3.1: the "
                    + "elementary items are the subdivisions \"not further subdivided\"). Remove "
                    + (present == DataClauseKind.Picture && item.PictureText is { } pt
                        ? $"PICTURE {pt}" : $"the {DataClauseKinds.Name(present)} clause")
                    + " from this entry, or remove the subordinate entries");
            }
        }

        foreach (var item in binder.CompositionForest())
        {
            if (DataBinder.HasNoSubordinates(item)) continue;
            item.Pic = null;              // the entry becomes the group its hierarchy declares
            item.Justified = false;
            item.BlankWhenZero = false;
            item.Property = null;         // a refused PROPERTY defines no accessor (§2023:13.18.42.3 SR3)
        }
    }

    /// <summary>The <see cref="ClausePlacementKind.ElementaryOnly"/> subject clauses <paramref name="item"/>
    /// carries, read from the bound facts each clause leaves. ⛔ A clause joining an ElementaryOnly row adds its
    /// arm HERE and its recovery in <see cref="CheckElementaryOnlyClauses"/>; <c>DataClausePlacementDriftTests</c>
    /// is red for an ElementaryOnly subject this method does not report.</summary>
    private static DataClauseKind ElementaryOnlyClausesOn(DataItem item) =>
        (item.Pic is not null ? DataClauseKind.Picture : DataClauseKind.None)
        | (item.Justified ? DataClauseKind.Justified : DataClauseKind.None)
        | (item.BlankWhenZero ? DataClauseKind.BlankWhenZero : DataClauseKind.None)
        | (item.Property is not null ? DataClauseKind.Property : DataClauseKind.None);

    /// <summary>§2023:13.16.3 SR8 — every elementary item has a PICTURE or a synthesized profile by now.
    /// <para>Runs immediately after <c>UsageInheritancePass</c>, which is the last pass that can legitimately
    /// FILL a null <c>Pic</c> (a group header shedding its usage to leaves, §2023:13.18.60.4 GR1; a PICTURE-less
    /// INDEX or OBJECT REFERENCE leaf taking the inherited profile). Anything still null at this point is a
    /// declaration defect, not a pending adjudication.</para></summary>
    public void Run(DataBinder binder)
    {
        CheckElementaryOnlyClauses(binder);   // §2023:13.18.40.3 SR1 + §2023:13.16.3 SR11 — the table's ElementaryOnly rows
        foreach (var item in binder.ConformanceForest())
        {
            if (!DataBinder.IsPictureLessLeaf(item)) continue;
            // ⛔ NO CARVE-OUT, AND THE ABSENCE IS THE POINT (kb/Work PB504/PB831). SR8 defers to SR9 in its own
            // words — "except as indicated in Syntax rule 9" — and SR9 is APPLIED, not excused: the
            // SynthesizeImpliedPictures pass gives every entry inside SR9's grant the PICTURE the rule implies
            // (DataBinder.ImpliedPicture.cs), so such an entry is no longer a picture-less leaf and this guard
            // never sees it. The FIGURATIVE case rides the same synthesis (§2023:8.3.3.1 makes a figurative constant a
            // literal; §2023:8.3.3.6.4 GR3 b/c supply the length SR9 takes "as specified in 8.3.3"), so the former
            // exemption — which let `01 G VALUE ALL "AB".` fall to the ONE-CHARACTER recovery item and store "A",
            // a silent wrong answer — is gone rather than widened. What remains here is exactly SR8's own
            // population: a numeric literal VALUE (`01 F VALUE 42.`), the class-pointer figurative NULL, a
            // zero-length literal (SR9 excludes it by name), a Format-2 table VALUE (SR9 grants only the
            // data-item format), and an entry with no VALUE clause at all.
            using var _ = binder.Edition.At(item);
            binder.Edition.Error(DiagnosticCatalog.UsageClauseCompatibility,
                $"data item '{item.CobolName ?? "FILLER"}': a PICTURE clause shall be specified for an elementary "
                + "item (ISO §2023:13.16.3 SR8) — it may be omitted only for an item whose usage is "
                + "binary-char, binary-short, binary-long, binary-double, float-short, float-long, "
                + "float-extended, index, message-tag, object reference, pointer, function-pointer or "
                + "program-pointer, or when a VALUE clause supplies an alphanumeric, boolean or national "
                + "literal (SR9)");
        }

        // The recovery shape, over the COMPOSED forest so a TYPE/SAME AS clone of a defective template cannot
        // carry a null Pic into the emitter behind an already-reported diagnostic. Errors have been raised, so
        // the compile HALTS before codegen either way — this is the belt to that braces, and it is what makes
        // "no elementary item reaches the emitter without a Pic" an INVARIANT rather than a hope.
        foreach (var item in binder.CompositionForest())
            if (DataBinder.IsPictureLessLeaf(item))
                item.Pic = PicInfo.Recovery();
    }
}
