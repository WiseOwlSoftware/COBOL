using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding.Passes;

/// <summary>
/// The MarkFileRecordImageLeaves pass: row 29 of the per-unit resolve pipeline (FilesResolved to FilesResolved).
/// </summary>
internal sealed class MarkFileRecordImageLeavesPass : IBindPass
{
    /// <inheritdoc/>
    public string Name => "MarkFileRecordImageLeaves";

    /// <inheritdoc/>
    public PassPhase Requires => PassPhase.FilesResolved;

    /// <inheritdoc/>
    public PassPhase Produces => PassPhase.FilesResolved;

    /// <inheritdoc/>
    public PassViews Views => PassViews.Appends(FactSetKind.WholeGroupReferenced, FactSetKind.ImageForcedItems);

    /// <summary>The LAST resolve pass (<see cref="BindPipeline"/>): every FILE record area is filled WITHOUT conversion
    /// by READ/RETURN (ISO §9.1.2 — the record area is one character image), so its numeric-DISPLAY leaves store their
    /// images exactly like a whole-referenced group's — even when the PROCEDURE DIVISION never names the record as a
    /// whole (ST103A reads then tests only a child). <c>StorageFormPass</c> consumes <see cref="WholeGroupReferenced"/>
    /// after binding.</summary>
    public void Run(DataBinder binder)
    {
        foreach (var f in binder.Files)
            foreach (var rec in f.Records)
                if (rec.IsGroup)
                {
                    binder.State.WholeGroupReferenced.Add(rec);
                    // Flag the leaves NOW (not at the group-tail whole-group pass): the bind-time
                    // NumericImagePlace wrap decision consults the early image facts (a ref-mod/RENAMES base
                    // under a record area must route through its character image at resolve time; ST102A's
                    // all-DISPLAY S-RECORD must not read as a Tier-C island at bind time).
                    MarkImageLeaves(rec);
                }

        void MarkImageLeaves(DataItem item)
        {
            foreach (var child in item.Children)
            {
                if (child.IsGroup) MarkImageLeaves(child);
                // ⛔ THE CARRIAGE QUESTION IS NOT THE IMAGE-FORM QUESTION, and this arm asks the CARRIAGE one
                // (kb/Work PB646). Promotion REPLACES the leaf's native carrier with a string of its
                // ImageWidth CHARACTER positions; for usage DISPLAY that string IS the leaf's storage, so every
                // width the model holds stays one number. A national-form numeric's carrier positions and its
                // STORAGE are two numbers (D-N1 — two bytes per position), and promoting one was MEASURED to
                // corrupt a plain group-to-group MOVE between two such leaves. Its group-image contribution is
                // the D-N7 COMPOSITION instead (GroupImageCodec.CarrierImageOf + the national byte transform),
                // which needs no promotion; the price is that a group move depositing characters incompatible
                // with the leaf's description leaves it holding §14.6.13.2's value rather than those
                // characters, which is the same posture every other non-DISPLAY numeric usage already takes.
                else if (child.Pic is { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display })
                    binder.State.ImageForcedItems.Add(child);      // the collected image fact (same rule as the whole-group union, §14.9.25.4 MOVE GR4)
            }
        }
    }
}
