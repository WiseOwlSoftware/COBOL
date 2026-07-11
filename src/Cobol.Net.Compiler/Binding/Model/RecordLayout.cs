// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Generic;
using System.Linq;
using CobolNet.Binding;

namespace CobolNet.Binding.Model;

/// <summary>
/// The ONE physical offset/width authority over a record tree (rearchitecture PHASE 05; DESIGN-data-model §2.6).
/// PHASE-FREE since P5 Step 8: leaf widths come from the DECLARED shape (<c>DataItem.ElementaryImageWidth</c>, a
/// pure Pic fact proven equal to the <see cref="StorageForm"/> width by the Step-2 corpus identity #3), so every
/// member here is callable at bind time AND emit time. Consolidates the formerly-duplicated geometry
/// (prove-then-delete: the Step-4/8 corpus asserts + the Sort/Keyed goldens gate each fold before Step 9 deletes
/// the copies): <c>DataItem.ImageWidth</c>'s recursion, <c>OdoModel.PhysicalWidth</c> (the ODO/GR8 physical
/// extent), the Sort geometry (<c>SortPhysicalWidth</c>/<c>SortOffsetInRecord</c>/<c>SortPlainOffset</c>), and the
/// Keyed geometry (<c>KeyedAreaOffset</c>/<c>KeyedKeyIndex</c> + the emitter's <c>KeyedImageOffset</c> twin).
/// <para><b>The Step-8 basis unification (a REAL fix, gated by the failing-first <c>KeyedOffsetSpecTests</c>):</b>
/// the legacy Keyed copies advanced by the redefined item's OWN <see cref="ImageWidth"/>, so a key sitting AFTER a
/// REDEFINES whose redefiner is WIDER than its target got a byte offset that disagreed with the emitted record
/// codec's physical layout — the runtime extracted the WRONG bytes and a same-byte-position operand from a sibling
/// record description failed to match its key. <see cref="OffsetOf"/> sits on the codec-correct
/// <see cref="PhysicalWidth"/> basis (ISO §12.4.5.12.4 GR4 — "the identical BYTE POSITIONS ... in any one record
/// description entry are implicitly referenced as keys for all other record description entries";
/// §14.9.41.3 SR6b — leftmost character position correspondence).</para>
/// </summary>
internal static class RecordLayout
{
    /// <summary>The character-image width of an item, per THIS item's occurrence (a parent multiplies by this item's
    /// own OCCURS count): a leaf's declared image width (digits + separate-sign, or PIC length); a group's sum over
    /// its NON-redefining children of (child image width × the child's own fixed-OCCURS count) — every OCCURS
    /// position is part of the group image (ISO §14.9), and a REDEFINING child overlays its target and adds no
    /// storage (§13.18.44). Mirrors <c>DataItem.ImageWidth</c>.</summary>
    public static int ImageWidth(DataItem item) =>
        item.IsElementary ? item.ElementaryImageWidth
        : item.Children.Where(c => c.RedefinesTargetName is null).Sum(c => ImageWidth(c) * (c.Occurs ?? 1));

    /// <summary>The tier-aware PHYSICAL image width of an item — the extent the emitted <c>AsImage()</c>/<c>FromImage()</c>
    /// codec spans (COBOLNET_DESIGN §4.2): a REDEFINING child overlays its target and contributes nothing; a Tier-B
    /// <see cref="RedefinesTier.StringCanonical"/> class contributes its ONE backing (class-max
    /// <see cref="RedefinesClass.Width"/>) once at the canonical member. For the class-free common subtree this
    /// equals <see cref="ImageWidth"/>. Mirrors <c>OdoModel.PhysicalWidth</c> / <c>SortPhysicalWidth</c> (the ODO
    /// §13.18.38 GR8 extent) — the raw <see cref="ImageWidth"/> would over- or under-count a group containing a
    /// redefines class whose redefiner is wider than the redefined item.</summary>
    public static int PhysicalWidth(DataItem item)
    {
        if (!item.IsGroup) return ImageWidth(item);
        int w = 0;
        foreach (var c in item.Children)
        {
            // The class-backing substitution applies ONLY where the child is a top-level class MEMBER (the
            // overlay root) — a SUBORDINATE of a member carries an inherited .Class link but occupies its own
            // positions inside the member's window (the P5.8 area-class find: skipping subordinates of a
            // multi-record FD's synthesized AREA class collapsed the record width to 0). The CANONICAL member —
            // which may itself be a REDEFINER (the classifier picks the class's storage owner, not necessarily
            // the target) — contributes the ONE backing at class-max width; other members contribute nothing.
            if (c.Class is { Tier: RedefinesTier.StringCanonical } cls && cls.Members.Contains(c))
            {
                if (c.IsCanonical) w += cls.Width;
                continue;
            }
            if (c.Class is { Tier: RedefinesTier.Alias } clsA && clsA.Members.Contains(c) && !c.IsCanonical)
                continue;   // a forwarded view
            w += (c.IsGroup ? PhysicalWidth(c) : ImageWidth(c)) * (c.Occurs ?? 1);
        }
        return w;
    }

    /// <summary>The item's character offset within its record AREA on the PHYSICAL (codec) basis: the offset inside
    /// its own 01 root, which IS the area offset because every secondary 01 under an FD is a synthesized REDEFINES
    /// of the first, starting at position 0 (ISO §13.18.44 GR1). A REDEFINING child takes its TARGET's offset and
    /// contributes no width; the running position advances by each preceding sibling's PHYSICAL contribution — the
    /// class-max backing width at a Tier-B canonical (matching the emitted record layout), the plain image extent
    /// elsewhere. Null when the item or any ancestor carries OCCURS (no single fixed position — §12.4.5.12 SR1 /
    /// §14.9.40.3 SR6b ban OCCURS subjects; mirrors the Sort walk's bail).</summary>
    public static int? OffsetOf(DataItem item)
    {
        for (var p = item; p is not null; p = p.Parent)
            if (p.Occurs is not null) return null;   // subject to OCCURS ⇒ no single fixed position
        DataItem root = item;
        while (root.Parent is { } parent) root = parent;

        int? found = null;
        var offsets = new Dictionary<DataItem, int>(ReferenceEqualityComparer.Instance);
        Walk(root, 0);
        return found;

        void Walk(DataItem node, int off)
        {
            if (found is not null) return;
            offsets[node] = off;
            if (ReferenceEquals(node, item)) { found = off; return; }
            int running = off;
            foreach (var c in node.Children)
            {
                int cOff = c.RedefinesTarget is { } t && offsets.TryGetValue(t, out int tOff) ? tOff : running;
                Walk(c, cOff);
                if (found is not null) return;
                // The advance mirrors PhysicalWidth's per-child contribution exactly (the codec layout):
                // a StringCanonical class MEMBER contributes the ONE backing (class-max) at the CANONICAL —
                // which may itself be a redefiner — and nothing at the other members; any other redefining
                // child overlays its target (no advance); everything else its physical extent.
                if (c.Class is { Tier: RedefinesTier.StringCanonical } cls && cls.Members.Contains(c))
                {
                    if (c.IsCanonical) running += cls.Width;
                    continue;
                }
                if (c.Class is { Tier: RedefinesTier.Alias } clsA && clsA.Members.Contains(c) && !c.IsCanonical)
                    continue;                                             // a forwarded view
                if (c.RedefinesTargetName is not null) continue;          // overlays its target — no advance
                running += (c.IsGroup ? PhysicalWidth(c) : ImageWidth(c)) * (c.Occurs ?? 1);
            }
        }
    }

    /// <summary>The PHYSICAL width of a RECORD as its frame/area extent: a record that is itself a member of a
    /// REDEFINES class (a secondary 01 of a multi-record SD/FD — the synthesized area class) spans the ONE shared
    /// backing (class-max width, §9.1.2 records leftmost-aligned in one area); otherwise its own
    /// <see cref="PhysicalWidth"/>. (← <c>SortPhysicalWidth</c>'s item-level rule.)</summary>
    public static int AreaWidth(DataItem record) =>
        record.Class is { Tier: RedefinesTier.StringCanonical } cls && record.IsCanonical
            ? cls.Width
            : PhysicalWidth(record);

    /// <summary>The character offset of <paramref name="target"/> within <paramref name="root"/>'s PHYSICAL
    /// record image — the compile-time key window (ISO §14.9.40.3 SR6e: the same byte positions are the key in
    /// every record). An item inside a REDEFINES class sits at the class anchor's offset plus its
    /// <see cref="DataItem.ClassOffset"/> (a redefinition begins at the redefined item's first position,
    /// §13.18.44 GR1; covers keys under a redefining group and keys in a secondary 01 of a multi-01 SD/FD, whose
    /// synthesized class anchors at the first record). Null when the target does not live in
    /// <paramref name="root"/>'s area, or sits under an OCCURS (not a legal key, §14.9.40.3 SR6b/SR6f).
    /// (← <c>SortOffsetInRecord</c>/<c>SortPlainOffset</c>, on the ONE <see cref="OffsetOf"/> walk.)</summary>
    public static int? OffsetInRecord(DataItem root, DataItem target)
    {
        if (target.Class is { } cls)
        {
            if (ReferenceEquals(cls.Canonical, root)) return target.ClassOffset;
            if (ReferenceEquals(root.Class, cls)) return target.ClassOffset - root.ClassOffset;
            // The class anchors at its CANONICAL's plain position (the canonical itself carries the class link,
            // so it must resolve through the PLAIN walk — recursing the class branch would never terminate).
            return Plain(root, cls.Canonical) is { } a ? a + target.ClassOffset : null;
        }
        return Plain(root, target);

        static int? Plain(DataItem root, DataItem target)
        {
            for (DataItem? n = target; n is not null; n = n.Parent)
                if (ReferenceEquals(n, root))
                    return OffsetOf(target);   // under root (an 01 ⇒ area offset 0): the area offset IS the window
            return null;
        }
    }

    /// <summary>Resolve a key-of-reference operand to its key index by BYTE POSITION (−1 = prime, i = the i-th
    /// alternate, null = no match): the operand qualifies when its leftmost character position within the record
    /// area coincides with the key's leftmost position and it is no longer than the key (ISO §14.9.41.3 SR6 /
    /// §14.9.30 SR11; §12.4.5.12.4 GR4 — the identical byte positions in ANY record description entry are
    /// implicitly keys for all record descriptions of the file, so a REDEFINES of the key or a same-position item
    /// in another 01 matches by position, never by name). Positions are the PHYSICAL codec layout via
    /// <see cref="OffsetOf"/>. (SR6b.2's same-class/category/usage check is a pre-existing looseness kept as-is;
    /// the width-≤ rule is SR6b.3.)</summary>
    public static int? KeyIndexByPosition(FileModel file, DataItem operand)
    {
        if (OffsetOf(operand) is not { } off) return null;
        if (file.RecordKeyItem is { } pk && OffsetOf(pk) == off && operand.ImageWidth <= pk.ImageWidth)
            return -1;
        for (int i = 0; i < file.AlternateKeys.Count; i++)
        {
            var alt = file.AlternateKeys[i].Item;
            if (OffsetOf(alt) == off && operand.ImageWidth <= alt.ImageWidth) return i;
        }
        return null;
    }
}
