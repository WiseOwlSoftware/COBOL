// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions;

namespace WiseOwl.Cobol.Binding.Passes;

/// <summary>
/// The SynthesizeImpliedPictures pass: row 1 of the per-unit resolve pipeline (None to None).
/// </summary>
/// <remarks>
/// <para><b>Placement in the pipeline.</b>
/// The §13.16.3 SR9 VALUE-implied PICTURE (kb/Work PB504/PB831/PB1300; DataBinder.ImpliedPicture.cs). FIRST, and the placement is the design. SR9 implies the clause for the entry the programmer WROTE, and §13.18.57.4 GR3 then says "when the description of type-name-1 includes an implicit PICTURE clause derived from a VALUE clause, that implicit PICTURE clause becomes part of the description of the subject of the entry" — so the template must OWN its implied PICTURE before ExpandTypes copies its description, exactly as it owns a written one. Run after the copy (the former order), the template had no PICTURE to hand over and the subject's OWN VALUE literal implied one instead: `01 U TYPEDEF VALUE "ABCD". 01 W TYPE U VALUE "Q".` bound W as PIC X(1) where GR3 makes it PIC X(4) (kb/Work PB1300). An entry whose description is still a pending TYPE / SAME AS reference is not picture-less in SR9's sense (IsPictureLessLeaf) — its PICTURE is the referenced description's — so it waits for the copy. BEFORE UsageInheritancePass as well, so that from this point on the entry is INDISTINGUISHABLE from one whose source wrote `PICTURE X(n)` — §13.18.60.4 GR1 inheritance, the §13.18.60.3 SR3/SR5/SR12/SR20 screens, SIGN inheritance and every later pass apply to it through their existing single sites.
/// </para>
/// </remarks>
internal sealed class SynthesizeImpliedPicturesPass : IBindPass
{
    /// <inheritdoc/>
    public string Name => "SynthesizeImpliedPictures";

    /// <inheritdoc/>
    public PassPhase Requires => PassPhase.None;

    /// <inheritdoc/>
    public PassPhase Produces => PassPhase.None;

    /// <inheritdoc/>
    public PassViews Views => PassViews.None;

    /// <summary>§13.16.3 SR9 — give every picture-less elementary item whose VALUE clause supplies an
    /// alphanumeric, boolean or national literal the PICTURE the rule implies.</summary>
    public void Run(DataBinder binder)
    {
        // The WRITTEN entries — the pass runs before ExpandTypes, so there are no clones to exclude yet; the forest
        // is named for what the rule is about (see the class remarks).
        foreach (var item in binder.ConformanceForest()) SynthesizeImpliedPicture(binder, item);
    }

    /// <summary>The same synthesis over one subtree's WRITTEN entries — what <see cref="DataBinder.CompleteDescriptionAhead"/>
    /// runs for an operand a constant's length phrase measures before the pipeline reaches it (kb/Work PB2465). The
    /// pass is idempotent (an entry with a PICTURE fails <see cref="DataBinder.IsPictureLessLeaf"/>), so the pipeline's own run
    /// later passes over these entries.</summary>
    internal static void SynthesizeImpliedPicturesUnder(DataBinder binder, DataItem root)
    {
        foreach (var item in DataItem.SelfAndDescendantsOf(root))
            if (DataBinder.IsWrittenEntry(item)) SynthesizeImpliedPicture(binder, item);
    }

    /// <summary>§13.16.3 SR9 for one entry: the body of <see cref="Run"/>.</summary>
    private static void SynthesizeImpliedPicture(DataBinder binder, DataItem item)
    {
        // ⛔ THE SAME PREDICATE THE SR8 GUARD USES, and deliberately so: SR9's subject is precisely the
        // population SR8 would otherwise reject, so the two rules must never disagree about which entries
        // they are looking at. It also excludes an entry whose description is a still-pending TYPE / SAME AS
        // reference (its PICTURE is the referenced description's — §13.18.57.4 GR3, §13.18.49.4 GR1), and it
        // is what makes this pass IDEMPOTENT: an entry that already has a PICTURE (written or
        // usage-synthesized) fails it.
        if (!DataBinder.IsPictureLessLeaf(item)) return;
        // ⛔ FORMAT 1 ONLY. SR9's grant is worded "specified in the DATA-ITEM FORMAT of the VALUE clause",
        // which is §13.18.63.2 Format 1 (`VALUE IS literal-1`) — DataItem.RawValue. The Format-2 (table)
        // carrier, DataItem.TableValues, is a different format and SR9 does not reach it, so a picture-less
        // `OCCURS … VALUES ARE "AB" FROM (1)` stays SR8's rejection.
        if (item.RawValue is not { } raw) return;
        if (DataBinder.Sr9ImpliedFor(raw, Sr9EffectiveUsage(item)) is not { } implied) return;

        using var _ = binder.Edition.At(item);
        string where = $"data item '{item.CobolName ?? "FILLER"}'";
        // The edition gate, through the ONE canonical funnel (ConstructRegistry.Check), once per SOURCE entry
        // (every entry visited here is one — no clone exists yet): COBOL-85 required a PICTURE for every
        // elementary item bar an index data item and the subject of a RENAMES clause, and had no
        // VALUE-implied PICTURE at all. Its boolean and national arms are gated a second time and
        // independently by `boolean-data-2002` / `national-data-2002`, which is why this row carries the
        // alphanumeric arm's introduction.
        ConstructRegistry.Check(binder.Edition.Edition, binder.Edition.Sink, Constructs.ValueImpliedPicture2002, where);

        // ⛔ THE SAME CALL BindEntry MAKES FOR A WRITTEN PICTURE, argument for argument — the entry's OWN
        // usage and its own SIGN clause, `explicitUsage` = "this entry wrote a USAGE clause". A usage the
        // entry acquires from a GROUP is applied ONCE, by UsageInheritancePass's ApplyEffectiveUsage, exactly
        // as it is for a written PICTURE; passing the inherited usage here as well would screen it twice and
        // report the §13.18.60.3 SR3/SR5/SR12/SR20 violation twice.
        item.PictureText = implied.Text;
        item.PicIsValueImplied = true;   // an IMPLICIT clause: no SR4/SR5/SR10 size bound (see the property)
        item.Pic = PictureAnalyzer.Analyze(implied.Text, item.OwnUsage ?? Usage.Display, binder.Edition, where,
            item.OwnSign, currencies: binder.SpecialNames.CurrencySigns, blankWhenZero: item.BlankWhenZero,
            explicitUsage: item.OwnUsage is not null, decimalPointIsComma: binder.SpecialNames.DecimalPointIsComma);
        // The entry is no longer picture-less, so the deferred §13.18.60.3 SR5/SR12 adjudication a
        // PICTURE-less USAGE NATIONAL / BIT entry carries is ANSWERED — by the picture SR9 supplies. The
        // screen above is the one that judges it (`01 A USAGE NATIONAL VALUE "AB".` is SR12's alphanumeric
        // refusal; `01 A USAGE NATIONAL VALUE N"AB".` conforms).
        item.Pending = PicPending.None;

        // ⚠ The VALUE literal is NOT re-screened through ScreenValueLiteral, and that is the rule's own
        // doing: §13.18.63.3 SR4/SR5/SR10's size sentences bound the literal by "the size indicated by an
        // EXPLICIT PICTURE clause", and this one is implied; their class sentences cannot bite because the
        // implied picture's category IS the literal's class by construction; and SR2's numeric-range arm is
        // unreachable, SR9 admitting only the three character categories.
    }

    /// <summary>The usage that applies to <paramref name="item"/> by §13.18.60.4 GR1 — its own clause (or the one
    /// a GROUP-USAGE clause implies), else the NEAREST ENCLOSING entry's, else §13.18.60.3 SR13 b)'s implied
    /// DISPLAY. It is the CONTEXT §8.3.3.6.4 GR1 and GR4 ask about, and nothing else: a written literal carries
    /// its own class, so only a figurative constant consults it.
    /// <para><see cref="DataBinder.DeclaredUsageOf"/> is the "own or implied" half, shared verbatim with
    /// <c>UsageInheritanceWalk</c>, which PUSHES the same derivation down the forest; the loop is GR1's "nearest
    /// enclosing" half asked PULL-style, because this pass runs before that walk and the walk's own result does
    /// not yet exist. The two agreeing is pinned end-to-end by
    /// <c>ImpliedPictureTests.Sr9_FigurativeContext_TravelsTheWholeAncestorChain</c>, which nests the usage two
    /// levels above the entry so a one-level or self-only reading fails.</para></summary>
    private static Usage Sr9EffectiveUsage(DataItem item)
    {
        for (DataItem? d = item; d is not null; d = d.Parent)
            if (DataBinder.DeclaredUsageOf(d) is { } u) return u;
        return Usage.Display;
    }
}
