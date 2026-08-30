// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Model;

/// <summary>
/// The ONE reason source for the Tier-C mixed-usage-group image boundary (rearchitecture PHASE-11 Step C;
/// DESIGN-data-model §2.3). A GROUP whose leaves are not uniformly character-imageable — a USAGE INDEX leaf
/// (the <see cref="DataItem.IsImageCapable"/> predicate, P1; floats and COMP-5/BINARY-* joined the image in
/// kb/Work PB164 waves 1–2), or a COMP/binary leaf that
/// was not whole-group image-promoted (<see cref="DataItem.IsCharacterImage"/>, P2) — has no whole-group
/// character image, so the verbs that need one (MOVE/STRING/UNSTRING/INSPECT/ACCEPT/DISPLAY/CALL/SORT-key/
/// record-area distribution/FILE-STATUS) stage LOUD by name (COBOLNET_DESIGN §1.4). This is the "Tier-C byte
/// island": the one sanctioned <c>byte[]</c> boundary of hard-invariant #1, whose confined codec is a scheduled
/// increment (DESIGN-data-model §2.3, Step D — deferred).
/// <para>Before Step C the same explanation was copy-pasted across ~12 emit guards with drift ("byte island"
/// vs "byte path", with/without the §4.2 reference). Step C routes each guard's message through the ONE
/// <see cref="Reason(string)"/> tail here, PRESERVING each site's own predicate (P1 vs P2) and its
/// operation-specific lead + offending-leaf descriptor — a message-only collapse, behavior-neutral. The
/// REDEFINES-CLASS Tier-C rejection is a SEPARATE, already-single-sourced verdict (the ONE
/// <see cref="RedefinesClass.Classify"/> mutator; <see cref="ComputeTier"/>'s reason strings carry the ISO
/// citations threaded to references by <c>ExpressionBinder.RefFailure</c>).</para>
/// </summary>
internal static class TierCIsland
{
    /// <summary>Append the ONE canonical Tier-C-island explanation to an operation-specific
    /// <paramref name="lead"/> (e.g. <c>"STRING INTO group 'REC' with a COMP/binary leaf"</c>).</summary>
    public static string Reason(string lead) =>
        $"{lead} — the Tier-C byte island (no whole-group character image), deferred; COBOLNET_DESIGN §4.2";

    /// <summary>The uniform lead for the common shape (<paramref name="context"/> verb + the group name +
    /// its offending-leaf <paramref name="leafKind"/>), then the canonical tail. <paramref name="leafKind"/>
    /// reflects the caller's predicate: <c>"COMP/binary"</c> for the stricter
    /// <see cref="DataItem.IsCharacterImage"/> guard; for an <see cref="DataItem.IsImageCapable"/> guard the
    /// default is DERIVED FROM THE ITEM (kb/Work PB164 — a fixed string blamed "USAGE INDEX" even when the
    /// group was imageless for a different reason): a VARIABLE-LENGTH group (§8.5.1.12) is named as such —
    /// with the pointer that DISPLAY, uniquely, renders its documented current-extent format (A.1 item 57) —
    /// and only a genuinely INDEX-leafed group blames USAGE INDEX (COMP-5/BINARY-* landed with wave 1, the
    /// FLOAT family with wave 2 — naming them would blame a cause that no longer exists).</summary>
    public static string Reason(DataItem item, string context, string? leafKind = null)
    {
        // Derivation order and wording per the PB164 review fleet: an IMAGELESS LEAF is tested FIRST (a
        // group with both an INDEX leaf and a dynamic member is imageless because of the leaf — DISPLAY's
        // current-extent composer refuses it too, so blaming the dynamic member handed the user a false
        // remedy); an item that IS a dynamic table / dynamic-length item names the dynamic mechanism itself
        // (HasVariableLengthSubordinate walks children only and answered false for it); and the message
        // PROMISES NOTHING about DISPLAY — some variable-length shapes (an in-element runtime length, an
        // OCCURS DEPENDING member) are loud under DISPLAY as well; CONFORMANCE.md row 57 states which
        // shapes render.
        string offender = leafKind is not null ? $"a {leafKind} leaf"
            : HasImagelessLeaf(item) ? "a USAGE INDEX leaf"
            : item.IsDynamicTable || item.IsDynamicLength
                || CobolNet.Binding.ReferenceResolver.HasVariableLengthSubordinate(item)
                ? "a dynamic-length / dynamic-capacity member (a variable-length group has no fixed record window)"
            : "a USAGE INDEX leaf";
        return Reason($"{context} '{item.CobolName}' with {offender}");
    }

    /// <summary>Any elementary descendant whose SHAPE is imageless — the leaf half of
    /// <see cref="DataItem.ElementImageCapable"/> answered false (today exactly a USAGE INDEX leaf: category
    /// numeric, <c>ByteForm</c> None). Tested on the item itself when elementary.</summary>
    private static bool HasImagelessLeaf(DataItem item) =>
        item.IsElementary ? !item.ElementImageCapable
        : item.Children.Any(c => c.RedefinesTargetName is null && HasImagelessLeaf(c));
}
