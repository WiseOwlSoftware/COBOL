// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Model;

/// <summary>
/// The ONE reason source for the Tier-C mixed-usage-group image boundary (rearchitecture PHASE-11 Step C;
/// DESIGN-data-model §2.3). A GROUP whose leaves are not uniformly character-imageable — a float / COMP-5 /
/// INDEX / BINARY-* leaf (the <see cref="DataItem.IsImageCapable"/> predicate, P1), or a COMP/binary leaf that
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
    /// reflects the caller's predicate: <c>"float/INDEX"</c> for an <see cref="DataItem.IsImageCapable"/>
    /// guard (COMP-5 and BINARY-CHAR..DOUBLE are IN the image since kb/Work PB164 wave 1 — a message naming
    /// them would blame a cause that no longer exists), <c>"COMP/binary"</c> for the stricter
    /// <see cref="DataItem.IsCharacterImage"/> guard.</summary>
    public static string Reason(DataItem item, string context, string leafKind = "float/INDEX") =>
        Reason($"{context} '{item.CobolName}' with a {leafKind} leaf");
}
