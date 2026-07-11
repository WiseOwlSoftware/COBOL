// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Linq;
using CobolNet.Binding;

namespace CobolNet.Binding.Model;

/// <summary>
/// The ONE physical offset/width authority over a record tree (rearchitecture PHASE 05; DESIGN-data-model §2.6).
/// Consolidates the duplicated width geometry so it single-sources here (prove-then-delete: the Step-4 corpus assert
/// proves equality BEFORE Step 9 deletes the copies): <c>DataItem.ImageWidth</c>'s recursion and
/// <c>OdoModel.PhysicalWidth</c> (the ODO/GR8 physical extent). Leaf widths are read from the Step-2
/// <see cref="StorageForm"/> (already proven equal to the legacy per-leaf image width, exit criterion 3).
/// <para><b>Scope note (PHASE-05 Step 4):</b> this step delivers the WIDTH authority — the named drift hazard
/// (DESIGN §5.4). The two OFFSET copies (<c>StatementBinder.Sort</c>'s <c>SortOffsetInRecord</c>/<c>SortPlainOffset</c>
/// and <c>StatementBinder.KeyedIo</c>'s <c>KeyedAreaOffset</c>/<c>KeyedKeyIndex</c>) are NOT folded in here: they use
/// DIFFERENT width bases for the running increment — Sort advances by <see cref="PhysicalWidth"/> (class-max, matching
/// the emitted codec) while Keyed advances by <see cref="ImageWidth"/> (the redefined item's own width). They agree
/// only where no key/sort-key follows a redefines whose redefiner is wider than its target, so they cannot be proven
/// byte-equal as a pure port. Their unification (choosing the codec-correct <see cref="PhysicalWidth"/> basis) folds
/// into <c>RecordLayout.OffsetOf</c>/<c>KeyIndexByPosition</c> at Step 8, under the protection of the Sort/Keyed
/// output goldens.</para>
/// </summary>
internal static class RecordLayout
{
    /// <summary>The character-image width of an item, per THIS item's occurrence (a parent multiplies by this item's
    /// own OCCURS count): a leaf's <see cref="StorageForm.ImageWidth"/>; a group's sum over its NON-redefining children
    /// of (child image width × the child's own fixed-OCCURS count) — every OCCURS position is part of the group image
    /// (ISO §14.9), and a REDEFINING child overlays its target and adds no storage (§13.18.44). Mirrors
    /// <c>DataItem.ImageWidth</c>; reads the Step-2 <see cref="StorageForm"/> (the single source of leaf widths).</summary>
    public static int ImageWidth(DataItem item) =>
        item.IsElementary ? item.Storage!.ImageWidth
        : item.Children.Where(c => c.RedefinesTargetName is null).Sum(c => ImageWidth(c) * (c.Occurs ?? 1));

    /// <summary>The tier-aware PHYSICAL image width of an item — the extent the emitted <c>AsImage()</c>/<c>FromImage()</c>
    /// codec spans (COBOLNET_DESIGN §4.2): a Tier-B <see cref="RedefinesTier.StringCanonical"/> class contributes its
    /// ONE backing (class-max <see cref="RedefinesClass.Width"/>) once at the canonical member, its views nothing; a
    /// Tier-A <see cref="RedefinesTier.Alias"/> non-canonical view forwards (adds nothing). For the class-free common
    /// subtree this equals <see cref="ImageWidth"/>. Mirrors <c>OdoModel.PhysicalWidth</c> / <c>SortPhysicalWidth</c>
    /// (the ODO §13.18.38 GR8 extent) — the raw <see cref="ImageWidth"/> would over-count a group containing a
    /// redefines class whose redefiner is wider than the redefined item.</summary>
    public static int PhysicalWidth(DataItem item)
    {
        if (!item.IsGroup) return ImageWidth(item);
        int w = 0;
        foreach (var c in item.Children)
        {
            if (c.Class is { Tier: RedefinesTier.StringCanonical } cls)
            {
                if (c.IsCanonical) w += cls.Width;   // the ONE backing, counted once at the canonical
                continue;
            }
            if (c.Class is { Tier: RedefinesTier.Alias } && !c.IsCanonical) continue;   // a forwarded view
            w += (c.IsGroup ? PhysicalWidth(c) : ImageWidth(c)) * (c.Occurs ?? 1);
        }
        return w;
    }
}
