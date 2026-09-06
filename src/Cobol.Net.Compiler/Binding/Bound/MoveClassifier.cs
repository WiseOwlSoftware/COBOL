// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;

namespace CobolNet.Binding.Bound;

/// <summary>The per-TARGET dispatch class of one MOVE store (P7 Step 7; ISO §14.9.25.4 GR4 decides
/// elementary-vs-group FIRST, sender side included). Computed ONCE by <see cref="MoveClassifier"/> when the
/// <see cref="BoundMove"/> is constructed — the emitter renders by kind and re-derives NOTHING. Deviation from
/// the phase doc's sketch (recorded in the ledger, audit-mandated): the kind is PER TARGET, not scalar (one MOVE
/// can have a ref-mod slice, a group, and an elementary receiver simultaneously), and the STORAGE FORM does not
/// travel on the node — <c>DataItem.Storage</c> is null while the binder runs (StorageFormPass is a group-tail
/// pass, and the MOVE's own bind records the facts that decide it); the emitter reads the by-then-settled
/// <c>item.Storage</c> projection instead.</summary>
public enum MoveKind
{
    /// <summary>A reference-modified receiver: the slice takes the source characters (or a figurative fills
    /// every slice position, §8.3.3.6.4 GR2 / §8.4.3.3 GR5-6).</summary>
    RefModSlice,
    /// <summary>A GROUP receiver — the §14.9.25.4 GR4 group move (no conversion; filled without consideration
    /// for subordinate items).</summary>
    Group,
    /// <summary>A GROUP SENDER into an elementary receiver — still a group move by GR4 (the sender side of the
    /// elementary-vs-group test; never receiver-category conversion/editing).</summary>
    GroupToElementary,
    /// <summary>An alphanumeric figurative (S/Q/H/L) or non-digit ALL "literal" into an ELEMENTARY NUMERIC
    /// receiver — the pre-2023 removed construct's fill semantics (§14.9.25.3 SR5; Annex E.2 item 1). Whether
    /// the fill can be stored (image-backed) or is the narrow loud residue is an EMIT-time storage question.</summary>
    FigurativeToNumericImage,
    /// <summary>The ordinary elementary move — rendered by the receiver-category conversion/editing table
    /// (§14.9.25.3 Table 16 / GR5).</summary>
    Convert,
}

/// <summary>THE MOVE-dispatch classifier (P7 Step 7): one pure function over bind-time facts, the single
/// authority for <see cref="MoveKind"/> — called by <c>BindMove</c> AND by the emitter's synthetic implicit-MOVE
/// constructions (READ INTO / WRITE FROM / RETURN INTO / INITIALIZE / CORRESPONDING / function RETURNING — the
/// same classification, never a second table; feedback_one_mechanism_per_job).</summary>
public static class MoveClassifier
{
    /// <summary>The dispatch kind of storing <paramref name="source"/> into <paramref name="target"/> —
    /// EXACTLY the pre-P7.7 <c>EmitMove</c> dispatch order: ref-mod receiver → group receiver → group sender →
    /// figurative-into-numeric → elementary conversion.</summary>
    public static MoveKind Kind(BoundOperand source, Place target)
    {
        if (target is RefModPlace) return MoveKind.RefModSlice;
        // A bit / national group RECEIVER is an elementary boolean / national receiver (§13.18.29.4 GR1b/GR2b —
        // D20/PB79): the Convert path over its as-if picture, stored through PlaceRenderer.Write's as-if arm.
        if (target.Item.IsGroup && !target.Item.IsAsIfElementary) return MoveKind.Group;
        if (IsGroupSender(source)) return MoveKind.GroupToElementary;
        if (target.Item.Pic is { Category: PicCategory.Numeric }
            && source is BoundFigurative { Kind: 'S' or 'Q' or 'H' or 'L' } or BoundAllLiteral { IsDigitOnly: false })
            return MoveKind.FigurativeToNumericImage;
        return MoveKind.Convert;
    }

    /// <summary>Classify every target of one MOVE (the <see cref="BoundMove.Kinds"/> parallel list).</summary>
    public static IReadOnlyList<MoveKind> Classify(BoundOperand source, IReadOnlyList<Place> targets)
    {
        var kinds = new MoveKind[targets.Count];
        for (int i = 0; i < targets.Count; i++) kinds[i] = Kind(source, targets[i]);
        return kinds;
    }

    /// <summary>True when a MOVE source operand is a GROUP data item (ISO §14.9.25.4 GR4 sender-side test). A
    /// reference-modified sender is excluded — its unique result is an elementary alphanumeric item whatever the
    /// underlying item (§8.4.3.3.4 GR6) — and so is a RENAMES alias (the level-66 view is composed as ONE elementary
    /// alphanumeric item, §13.18.45). A Tier-B REDEFINES group VIEW counts: GR4 classifies by the data item,
    /// not its storage shape. ⛔ The test is NOT purely structural: §14.9.30.4 GR4 b) / §14.9.34.4 GR5 b)
    /// DESIGNATE the READ/RETURN INTO implicit move an alphanumeric group move whenever the file description
    /// entry carries a RECORD IS VARYING clause, so <see cref="BoundCurrentRecord"/> can answer true over an
    /// ELEMENTARY record area (kb/Work PB339).</summary>
    public static bool IsGroupSender(BoundOperand source) => source switch
    {
        // THE CURRENT RECORD of a RECORD IS VARYING file is a group sender BY DESIGNATION, not by structure:
        // §14.9.30.4 GR4 b) / §14.9.34.4 GR5 b) — "If the file description entry contains a RECORD IS VARYING
        // clause, the implied move is an alphanumeric group move" — so `READ F INTO an-edited-item` deposits
        // characters and does NOT edit, even when the FD's 01 is an elementary PIC X(n) (kb/Work PB339). Without
        // that designation (a FORMAT 3 `RECORD CONTAINS m TO n` file) the ordinary structural test decides, so
        // the arm falls through to it rather than answering false.
        BoundCurrentRecord cr => cr.AlphanumericGroupMove || IsGroupPlace(cr.Area),
        BoundFieldOperand f => IsGroupPlace(f.Place),
        _ => false,
    };

    /// <summary>The STRUCTURAL half of the §14.9.25.4 GR4 sender-side test — the sending PLACE is a group item.
    /// Written once so the plain field operand and the current-record operand cannot drift apart.</summary>
    private static bool IsGroupPlace(Place p) =>
        p is not (RefModPlace or RenamesPlace)
        && p.Item.IsGroup && !p.Item.IsAsIfElementary;   // a bit / national group SENDS as elementary (D20/PB79)
}
