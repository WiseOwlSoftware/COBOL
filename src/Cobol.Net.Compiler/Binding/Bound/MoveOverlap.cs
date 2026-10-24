// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;

namespace CobolNet.Binding.Bound;

/// <summary>What a MOVE does with a STATICALLY overlapping sender and receiver (kb/Work PB1907, PB1910;
/// docs/CONFORMANCE.md §3 D-OVL2).</summary>
public enum OverlapStoreKind
{
    /// <summary>Nothing — the ordinary store: the sender is read in full, then the receiver is written (the
    /// snapshot every other MOVE, overlapping or not, performs).</summary>
    None,
    /// <summary>A JUSTIFIED RIGHT receiver's leading <see cref="OverlapStore.Positions"/> positions are
    /// space-filled before the sender is read (the padding of §14.9.25.4 GR6's right-justified alignment, written
    /// first), and the ordinary store follows.</summary>
    LeadingSpaces,
    /// <summary>The whole store is <c>OverlappingMove.DisplayToDisplay</c>: a numeric DISPLAY sender converted into a
    /// numeric DISPLAY receiver of a different description, in place.</summary>
    DisplayToDisplay,
    /// <summary>The whole store is <c>OverlappingMove.PackedToDisplay</c>: a PACKED-DECIMAL sender converted into a
    /// numeric DISPLAY receiver; its digits are a snapshot and its sign is read from the sender's last byte AFTER the
    /// receiver is stored.</summary>
    PackedToDisplay,
    /// <summary>The whole store is <c>OverlappingMove.AlphanumericToDisplay</c>: an alphanumeric sender (item,
    /// group-free slice) converted into a numeric DISPLAY receiver, in place.</summary>
    AlphanumericToDisplay,
    /// <summary>The whole store is <c>OverlappingMove.DisplayToPacked</c>: a numeric DISPLAY sender converted into a
    /// PACKED-DECIMAL receiver, in place.</summary>
    DisplayToPacked,
}

/// <summary>The overlap treatment of one MOVE store: its kind, for <see cref="OverlapStoreKind.LeadingSpaces"/> how
/// many leading positions, and for the in-place kinds the receiver's first byte minus the sender's (both static).</summary>
public readonly record struct OverlapStore(OverlapStoreKind Kind, int Positions = 0, int ReceiverAt = 0)
{
    /// <summary>The ordinary store.</summary>
    public static OverlapStore None => default;

    /// <summary>True for the kinds whose emission replaces the whole store (the runtime's in-place algorithm).</summary>
    public bool IsInPlace => Kind is OverlapStoreKind.DisplayToDisplay or OverlapStoreKind.PackedToDisplay
        or OverlapStoreKind.AlphanumericToDisplay or OverlapStoreKind.DisplayToPacked;
}

/// <summary>⛔ <b>ISO §14.6.10 1) — "operands … not described by the same data description entry … overlap: the
/// result is undefined" (Annex A.2 item 36; §4.4 makes a run unit that allows it a conforming one) — WHAT
/// WISEOWL COBOL DOES, WRITTEN ONCE</b> (kb/Work PB1907, PB1910; docs/CONFORMANCE.md §3 D-OVL1/D-OVL2).
/// <para>The standard defines no result, so the choice is the implementor's and CLAUDE.md rule 1 settles it: where
/// GnuCOBOL measurably differs from the plain snapshot (the sender is read in full before the receiver is
/// written), follow GnuCOBOL. GnuCOBOL 3.2's <c>libcob</c> differs in five families, all because the library writes
/// the RECEIVER while it is still reading the SENDER:</para>
/// <list type="bullet">
///   <item><b>JUSTIFIED RIGHT receiver, shorter sender</b> (<c>cob_move_alphanum_to_alphanum</c> and
///     <c>cob_move_display_to_alphanum</c>): the <c>size2 - size1</c> leading positions are space-filled, THEN the
///     sender is moved — so a sender overlapping those positions reads spaces (<see cref="OverlapStoreKind.LeadingSpaces"/>).</item>
///   <item><b>Numeric DISPLAY receiver from a numeric DISPLAY sender</b> (<c>cob_move_display_to_display</c>,
///     <c>store_common_region</c>): the receiver is set to zero, then its digits are copied one at a time from the
///     sender AS THE STORAGE THEN HOLDS IT (<see cref="OverlapStoreKind.DisplayToDisplay"/>).</item>
///   <item><b>Numeric DISPLAY receiver from a PACKED-DECIMAL sender</b> (<c>cob_move_packed_to_display</c>): the digits
///     are a snapshot but the sign is read from the sender's last byte after the receiver is stored
///     (<see cref="OverlapStoreKind.PackedToDisplay"/>).</item>
///   <item><b>Numeric DISPLAY receiver from an alphanumeric sender</b> (<c>cob_move_alphanum_to_display</c>): the same,
///     after a scan of the sender for a sign, digits and a decimal point (<see cref="OverlapStoreKind.AlphanumericToDisplay"/>).</item>
///   <item><b>PACKED-DECIMAL receiver from a numeric DISPLAY sender</b> (<c>cob_move_display_to_packed</c>): the
///     receiver is cleared, then written one digit pair at a time (<see cref="OverlapStoreKind.DisplayToPacked"/>).</item>
/// </list>
/// <para>The three in-place families are ONE runtime algorithm each (<c>OverlappingMove</c>) because their result
/// depends on the ORDER the library writes and reads in, not on a rule that can be stated ("receiver zero, then
/// convert" is right only where the receiver lies at or left of the sender's digits; a receiver to the right smears
/// the sender's first digit across it). The classification below chooses the algorithm; the emitter calls it.</para>
/// <para>GnuCOBOL's compiler (<c>cb_build_move_field</c>) copies with <c>memcpy</c>/<c>memmove</c> — a snapshot —
/// when the two numeric descriptions are identical, so this rule asks the same question: identical descriptions
/// keep the snapshot.</para>
/// <para>⛔ It applies ONLY where the overlap is a COMPILE-TIME FACT (<see cref="StorageExtent"/>), so the
/// ordinary MOVE — which cannot overlap — carries <see cref="OverlapStore.None"/> and emits exactly what it always
/// did. Where the overlap is not provable (a run-time reference-modification bound, an OCCURS element) the
/// snapshot stands. ISO §14.6.10 2) makes operands described by the SAME entry behave as if disjoint, but Annex A.2
/// item 36 b) — "When one or more of the operands is reference-modified" — makes a reference-modified operand's
/// overlap undefined whatever its entry, so a move from a slice of the receiver itself is overlap like any other
/// (GnuCOBOL measured: <c>MOVE JR(1:2) TO JR</c>, JR PIC X(4) JUSTIFIED RIGHT, leaves four spaces). An
/// un-sliced operand moved to itself has equal descriptions and no pad, so no corner can fire.</para>
/// <para>NOT adopted: a numeric-edited receiver, and a PACKED-DECIMAL receiver of a PACKED-DECIMAL sender
/// (docs/CONFORMANCE.md D-OVL2).</para></summary>
public static class MoveOverlap
{
    /// <summary>The overlap treatment for storing <paramref name="sender"/> into <paramref name="target"/> as a
    /// store of <paramref name="kind"/> — <see cref="OverlapStore.None"/> unless every condition holds.</summary>
    public static OverlapStore Classify(BoundOperand sender, Place target, MoveKind kind)
    {
        if (kind is not (MoveKind.Convert or MoveKind.GroupToElementary)
            || sender is not BoundFieldOperand { Place: var from }
            || target is not (MemberPlace or RedefViewPlace { Coding: null })
            || !target.Item.IsElementary) return OverlapStore.None;

        if (StorageExtent.Of(target) is not { } to || StorageExtent.Of(from) is not { } fromExtent
            || !fromExtent.Overlaps(to)) return OverlapStore.None;

        // The sender's CHARACTER positions must be its bytes (no national or bit leaf inside a group sender), so
        // the positions this rule counts are the bytes the extent measured.
        if (!IsCharacterPositioned(from, fromExtent)) return OverlapStore.None;

        var pic = target.Item.Pic!;
        if (target.Item.IsDynamicLength || target.Item.IsAnyLength) return OverlapStore.None;

        // PACKED-DECIMAL receiver — an elementary (Convert) move from a numeric DISPLAY item. (An alphanumeric
        // sender reaches a packed receiver through a temporary, and a PACKED or BINARY sender through a value: both
        // are snapshots in libcob, so they keep ours.) The receiver is stored as its byte image, never a native field.
        if (pic.Usage is Usage.Packed)
            return kind is MoveKind.Convert && pic.Category is PicCategory.Numeric && !pic.IsFloat
                   && (target is RedefViewPlace || target.Item.StoreAsImage) && IsNumericDisplayItem(from)
                ? new OverlapStore(OverlapStoreKind.DisplayToPacked, ReceiverAt: to.Start - fromExtent.Start)
                : OverlapStore.None;

        if (pic.Usage is not Usage.Display) return OverlapStore.None;

        // JUSTIFIED RIGHT receiver — alphanumeric or alphabetic, a character-image sender of fewer positions.
        if (pic.Category is PicCategory.Alphanumeric && !pic.IsCharacterEdited && target.Item.Justified)
        {
            int pad = to.Length - fromExtent.Length;
            return pad > 0 && IsCharacterSender(from)
                ? new OverlapStore(OverlapStoreKind.LeadingSpaces, pad)
                : OverlapStore.None;
        }

        // Numeric DISPLAY receiver — an elementary (Convert) move from a differently described numeric DISPLAY
        // item or from an alphanumeric item. A GROUP sender is a group move (GR4): no conversion, no in-place store.
        if (kind is MoveKind.Convert && pic.Category is PicCategory.Numeric && !pic.IsFloat)
        {
            int at = to.Start - fromExtent.Start;
            if (IsCharacterSender(from)) return new OverlapStore(OverlapStoreKind.AlphanumericToDisplay, ReceiverAt: at);
            if (IsOtherNumericDisplay(from, pic)) return new OverlapStore(OverlapStoreKind.DisplayToDisplay, ReceiverAt: at);
            if (IsPackedItem(from)) return new OverlapStore(OverlapStoreKind.PackedToDisplay, ReceiverAt: at);
        }

        return OverlapStore.None;
    }

    private static bool IsCharacterPositioned(Place p, StorageExtent e) =>
        p is RefModPlace r ? IsCharacterPositioned(r.Inner, StorageExtent.Of(r.Inner) ?? e)
                           : p.Item.ImageWidth == e.Length;

    /// <summary>An alphanumeric-family sender: a reference-modified slice (always alphanumeric, §8.4.3.3.4 GR6), an
    /// unedited alphanumeric or alphabetic DISPLAY item, or a group.</summary>
    private static bool IsCharacterSender(Place from) =>
        from is RefModPlace { Inner.Item: var inner } r
            ? r.Category is PicCategory.Alphanumeric && inner.OperandPic is null or { Usage: Usage.Display }
            : from.Item.IsGroup && !from.Item.IsAsIfElementary
              || from.Item.Pic is { Category: PicCategory.Alphanumeric, Usage: Usage.Display, IsCharacterEdited: false };

    /// <summary>A whole numeric PACKED-DECIMAL item (not a slice).</summary>
    private static bool IsPackedItem(Place from) =>
        from.DenotedItem is not null && from.Item.Pic is { Category: PicCategory.Numeric, Usage: Usage.Packed, IsFloat: false };

    /// <summary>A whole unedited numeric DISPLAY item (not a slice: a slice is alphanumeric, §8.4.3.3.4 GR6).</summary>
    private static bool IsNumericDisplayItem(Place from) =>
        from.DenotedItem is not null && from.Item.Pic is { Category: PicCategory.Numeric, Usage: Usage.Display };

    /// <summary>An unedited numeric DISPLAY sender whose description (usage, digits, scale, sign and sign form)
    /// differs from the receiver's — GnuCOBOL's compiler copies identical descriptions byte for byte.</summary>
    private static bool IsOtherNumericDisplay(Place from, PicInfo receiver) =>
        IsNumericDisplayItem(from) && from.Item.Pic is { } s
        && !(s.Length == receiver.Length && s.Digits == receiver.Digits && s.Scale == receiver.Scale
             && s.Signed == receiver.Signed && s.SignKind == receiver.SignKind);
}
