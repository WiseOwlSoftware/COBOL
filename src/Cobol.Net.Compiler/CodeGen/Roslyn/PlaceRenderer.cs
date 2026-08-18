// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Linq;
using CobolNet.Binding.Model;
using CobolNet.CodeGen.Emit;

namespace CobolNet.CodeGen;

/// <summary>
/// The Roslyn backend's sole renderer of a <see cref="Place"/> lvalue to C# read/write text
/// (DESIGN-codegen-backend §2.3). Keeping ALL C# text on the backend side of the bind→emit seam is what makes the
/// bound tree backend-neutral (the G4 invariant): a <see cref="Place"/> carries STRUCTURE (an access path, subscript
/// <c>BoundExpr</c>s, a ref-mod span, resolved <see cref="DataItem"/>s), never call text, so a future CIL backend can
/// consume the same tree. Every runtime call this renderer emits routes through <see cref="RuntimeApi"/>.
/// <para>Rendering is context-free (it reads only the <see cref="Place"/>'s own structure + <see cref="RuntimeApi"/>),
/// so this is a <c>static</c> class like <see cref="RuntimeApi"/> — no <c>EmitContext</c> is threaded. It lives in
/// <c>CodeGen</c>, never <c>Binding</c>: the binder produces the structural <see cref="Place"/>, the backend renders
/// it (the binder never references this type — that would invert the layering the seam exists to enforce).</para>
/// <para><b>Migration (P7 Step 11, subtype-at-a-time).</b> Consumers are routed through <see cref="Read"/>/
/// <see cref="Write"/> FIRST (each routing is byte-identical — the shim below forwards to the legacy
/// <see cref="Place.Read"/>/<see cref="Place.Write"/>); THEN each <see cref="Place"/> subtype is converted to
/// structure one at a time, its rendering moving into an explicit arm here while the still-string subtypes keep
/// delegating. When every subtype and consumer is migrated, <see cref="Place.Read"/>/<see cref="Place.Write"/> are
/// deleted and the neutrality reflection test (DESIGN-codegen-backend §6 R5) locks the invariant.</para>
/// </summary>
internal static class PlaceRenderer
{
    /// <summary>A C# expression that reads <paramref name="p"/>'s current value.</summary>
    public static string Read(Place p) => p switch
    {
        // A reference-modified slice inner(start:length) (ISO §8.4.3.3): a substring of the inner field's image.
        RefModPlace r => RuntimeApi.StrRefMod(Read(r.Inner), RmStart(r), RmLen(r), r.AllowZeroLength),
        // A NUMERIC item viewed as its character image (ISO §8.4.3.3.4 GR6 ref-mod; §13.18.45 a RENAMES span leaf):
        // the BYTES it occupies — its zoned digits for USAGE DISPLAY, its radix-2 / BCD bytes for BINARY / PACKED
        // (V59), which is what a span over it renames.
        NumericImagePlace n => RuntimeApi.NumFormatImage(Read(n.Inner), n.Inner.Item.ProfileName),
        // A GROUP viewed as its character image for reference modification (ISO §8.4.3.3.3 SR1 / §8.4.3.3.4 GR6 —
        // kb/Work PB70): the generated AsImage(); an occurs-depending group with data-name-1 outside sends its
        // CURRENT-count part (§13.18.38 GR8, so a position past the count is EC-BOUND-REF-MOD, not a read of the
        // unused area); a Tier-C group (a float / COMP-5 / INDEX leaf) has no image and stays the loud island.
        GroupImagePlace g => !g.Inner.Item.IsImageCapable
            ? EmitText.LoudValue("string", TierCIsland.Reason(g.Inner.Item, "reference modification of group"))
            : g.Inner is OdoGroupPlace { DependingInside: false } odo ? SendingImage(odo)
            : GroupImage(g.Inner),
        // A level-66 RENAMES alias (ISO §13.18.45): concatenate the spanned leaves' character images.
        RenamesPlace n => n.Leaves.Count == 1
            ? Read(n.Leaves[0])
            : "(" + string.Join(" + ", n.Leaves.Select(Read)) + ")",
        // An ODO group operand read plainly (not as a GR8 slice) is the struct lvalue — the inner member place.
        OdoGroupPlace o => Read(o.Inner),
        // A direct member/fixed-table access — the structural path rendered as a C# lvalue.
        MemberPlace m => RenderPath(m.Path, AccessDir.Sending),
        // A subscripted OCCURS DYNAMIC element read — the trailing DynTableSegment renders RefSending (§8.5.1.9.2).
        DynTablePlace d => RenderPath(d.Path, AccessDir.Sending),
        // A Tier-B REDEFINES view (§13.18.44): the (offset, width) character window over the class's ONE backing.
        RedefViewPlace v => RuntimeApi.StrRefMod(RenderPath(v.Backing, AccessDir.Sending), RvOffset(v), v.Width.ToString()),
        // The OCCURS DYNAMIC CAPACITY register (§13.18.38 GR15): a read-only view over the table's current capacity.
        CapacityRegisterPlace c => $"{RenderPath(c.Table, AccessDir.Sending)}.Capacity",
        // The X3.23-1985 DEBUG-ITEM register / member (VCR 7.17): a read-only view over the program's __dbgItem.
        DebugRegisterPlace d => DebugRead(d.Member),
        // A table(ALL) intrinsic argument (ISO §15.3; kb/Work PB62) is an ENUMERATION, never a single value — the
        // intrinsic argument-list renderers expand it (IntrinsicRenderer.ArgArray); reaching a read here is a
        // renderer that forgot to, and it must fail at compile time rather than emit an unbound index variable.
        TableAllPlace a => throw new System.InvalidOperationException(
            $"a table(ALL) argument over '{a.Element.Item.CobolName ?? a.Element.Item.CsName}' reached a single-value read — the argument-list renderer must enumerate it"),
        _ => throw Unhandled(p),
    };

    /// <summary>The C# read expression for a DEBUG-ITEM register member (X3.23-1985, VCR 7.17) — the C# text the
    /// structural <see cref="DebugRegisterMember"/> selector maps to (kept on the RENDERER, never in the Place).</summary>
    private static string DebugRead(DebugRegisterMember m) => m switch
    {
        DebugRegisterMember.Item => "__dbgItem.Image",
        DebugRegisterMember.Line => "__dbgItem.DebugLine",
        DebugRegisterMember.Name => "__dbgItem.DebugName",
        DebugRegisterMember.Sub1 => "__dbgItem.DebugSub1",
        DebugRegisterMember.Sub2 => "__dbgItem.DebugSub2",
        DebugRegisterMember.Sub3 => "__dbgItem.DebugSub3",
        DebugRegisterMember.Contents => "__dbgItem.DebugContents",
        _ => throw new System.InvalidOperationException($"unknown DEBUG-ITEM member '{m}'"),
    };

    /// <summary>A C# statement (with trailing <c>;</c>) that stores <paramref name="rhs"/> into <paramref name="p"/>.</summary>
    public static string Write(Place p, string rhs) => p switch
    {
        // Splice the new slice back into the inner field, preserving its width. A BOOLEAN receiver pads with
        // boolean-zero (§14.6.8.6; §8.4.3.3 GR5a); every other category keeps the space fill.
        RefModPlace r => Write(r.Inner, RuntimeApi.StrSpliceInto(Read(r.Inner), RmStart(r), RmLen(r), rhs,
            r.Inner.Item.Pic is { Category: PicCategory.Boolean } ? "'0'" : null, allowZeroLength: r.AllowZeroLength)),
        // Decode the spliced image back into the typed field (via the FormatImage/StoreImage pair — the same
        // bytes the read produced, so a splice round-trips whatever the item's byte form is).
        NumericImagePlace n => Write(n.Inner, RuntimeApi.NumStoreImage(rhs, n.Inner.Item.ProfileName, Read(n.Inner))),
        // The spliced group image goes back through the ONE group-image store (kb/Work PB70).
        GroupImagePlace g => WriteGroupImage(g.Inner, rhs, "reference modification into group"),
        RenamesPlace n => WriteRenames(n, rhs),
        OdoGroupPlace o => Write(o.Inner, rhs),
        // A member/fixed-table store — the structural path as an assignment target.
        MemberPlace m => $"{RenderPath(m.Path, AccessDir.Sending)} = {rhs};",
        // A dynamic-table store uses RefReceiving, which grows-and-seeds past the current capacity (§8.5.1.9.3).
        DynTablePlace d => $"{RenderPath(d.Path, AccessDir.Receiving)} = {rhs};",
        // Splice the new image back into the class's ONE backing, preserving its full width (§13.18.44).
        RedefViewPlace v => $"{RenderPath(v.Backing, AccessDir.Sending)} = " +
            $"{RuntimeApi.StrSpliceInto(RenderPath(v.Backing, AccessDir.Sending), RvOffset(v), v.Width.ToString(), rhs)};",
        // Unreachable: SET Format 14 routes to BoundSetCapacity, and any other store into the CAPACITY register is
        // rejected COBOLNET1523 at bind time (§13.18.38 SR30–32). The backstop for a receiver path that forgot the gate.
        CapacityRegisterPlace => throw new System.InvalidOperationException(
            "the CAPACITY register is set only by SET Format 14 (ISO §13.18.38 SR30-32); a direct store must be "
            + "rejected COBOLNET1523 at bind time and never reach PlaceRenderer.Write"),
        // Unreachable: a COBOL program never assigns to a DEBUG-* register (X3.23-1985 — the runtime populates it via
        // the injected debug trigger); a receiving-position use is rejected at bind time. The backstop for a
        // receiver path that forgot the gate.
        DebugRegisterPlace => throw new System.InvalidOperationException(
            "the X3.23-1985 DEBUG-ITEM register is read-only (the debug facility populates it); a store must be "
            + "rejected at bind time and never reach PlaceRenderer.Write"),
        _ => throw Unhandled(p),
    };

    // Unreachable: every concrete Place subtype has an explicit arm above (there is no Place.Read()/Write() to fall
    // back to since the structural-Place migration completed). A new subtype without an arm trips this at run time.
    private static System.InvalidOperationException Unhandled(Place p) =>
        new($"CodeGen.PlaceRenderer has no arm for Place subtype '{p.GetType().Name}'");

    /// <summary>Render an <see cref="AccessPath"/> to a C# lvalue expression — a static/instance root field, then
    /// <c>.Member</c> access, <c>CobolTable.At(path, index)</c> for a fixed OCCURS, and <c>RefSending</c>/
    /// <c>RefReceiving</c> for an OCCURS DYNAMIC level (the accessor chosen from <paramref name="dir"/>: a read
    /// sends, a write receives). A subscript INDEX is the D10 transitional string carried on the table segment.</summary>
    public static string RenderPath(AccessPath ap, AccessDir dir)
    {
        string path = "";
        foreach (var seg in ap.Segments)
            path = seg switch
            {
                RootFieldSegment r => r.CsField,
                MemberSegment m => path + "." + m.CsMember,
                FixedTableSegment f => RuntimeApi.TableAt(path, f.OneBasedIndex),
                DynTableSegment d => $"{path}.{(dir == AccessDir.Sending ? "RefSending" : "RefReceiving")}({d.OneBasedIndex})",
                _ => path,
            };
        return path;
    }

    // A Tier-B view's 1-based window start = the 0-based offset expression + 1 (OffsetExpr is the D10 transitional string).
    private static string RvOffset(RedefViewPlace v) => $"(int)({v.OffsetExpr} + 1)";

    // The reference-modification start/length are `long`-valued expressions but the runtime takes `int` positions —
    // cast at the call site. Start/Length are the P5.11/D10 TRANSITIONAL string carrier (a rendered index expression);
    // they become BoundExpr when D10 removes the SUBSCRIPT lexer mode (PHASE 15) — see the PHASE-07 Step 11 plan.
    // The cast and the OMITTED-length sentinel are RuntimeApi's (the ONE definition, shared with the ref-modified
    // FUNCTION RESULT channel in IntrinsicRenderer — the two must render identical positions).
    private static string RmStart(RefModPlace r) => RuntimeApi.RefModStart(r.Start);
    private static string RmLen(RefModPlace r) => RuntimeApi.RefModLength(r.Length);

    /// <summary>Store into a multi-leaf RENAMES alias (ISO §13.18.45): store the value at the span width, then
    /// distribute the slices back into the leaves left to right (a write through the alias shows through every
    /// renamed item and vice versa — no second storage).</summary>
    private static string WriteRenames(RenamesPlace n, string rhs)
    {
        if (n.Leaves.Count == 1) return Write(n.Leaves[0], rhs);
        int width = n.Leaves.Sum(l => l.Item.ImageWidth);
        var sb = new System.Text.StringBuilder();
        sb.Append($"{{ string __ren = {RuntimeApi.StrStore(rhs, width.ToString())};");
        int off = 0;
        foreach (var l in n.Leaves)
        {
            int w = l.Item.ImageWidth;
            sb.Append(' ').Append(Write(l, $"__ren.Substring({off}, {w})"));
            off += w;
        }
        return sb.Append(" }").ToString();
    }

    /// <summary>A figurative-constant store into a reference-modified slice: an EMPTY slice with the fill char as the
    /// SpliceInto pad, so every targeted position takes the fill (ISO §8.3.3.6.4 GR2 / §8.4.3.3 GR5/GR6). Threads
    /// <c>AllowZeroLength</c> exactly like the <see cref="Write"/> RefModPlace arm (review V31: omitting it made a
    /// figurative MOVE into a zero-length slice spuriously raise fatal EC-BOUND-REF-MOD under
    /// <c>&gt;&gt;REF-MOD-ZERO-LENGTH ON</c> — §8.4.3.3.4 GR5c allows the zero-length result, and §14.9.25.4 GR1
    /// makes the zero-length MOVE receiver a no-op, never a raise).</summary>
    public static string WriteFill(RefModPlace p, string fillChar) =>
        Write(p.Inner, RuntimeApi.StrSpliceInto(Read(p.Inner), RmStart(p), RmLen(p), "\"\"", pad: fillChar,
            allowZeroLength: p.AllowZeroLength));

    /// <summary>
    /// ⛔ THE ONE STORE OF A CHARACTER IMAGE INTO A GROUP RECEIVER (ISO §14.9.25.4 GR4 — a group receiver is
    /// "filled without consideration for the individual elementary or group items", no conversion): the generated
    /// <c>FromImage</c> distributes the image into the leaves; an occurs-depending group with data-name-1 OUTSIDE
    /// takes only its CURRENT-count part (§13.18.38 GR8a — the GR8a splice, positions past the count unmodified;
    /// data-name-1 INSIDE uses the maximum length, GR8b — the plain FromImage); a Tier-B REDEFINES view's image IS
    /// its character window; a group nested under an OCCURS DYNAMIC level distributes through the RECEIVING accessor
    /// (RefReceiving grows-and-seeds past the current capacity, §8.5.1.9.3 — never RefSending, which drops an
    /// out-of-capacity write into scratch). A group with a float / COMP-5 / INDEX leaf has no image
    /// (<see cref="DataItem.IsImageCapable"/>) and stays the loud Tier-C island (COBOLNET_DESIGN §4.2) —
    /// <paramref name="context"/> names the verb in that message.
    /// <para>V59 residue (DA5), written ONCE: this is a POSITIONAL character transfer INTO the group's storage — the
    /// same job a group MOVE does — so a COMP / PACKED group is admitted (its leaves decode their image slices) and
    /// "BYTES ARE NOT TEXT" does not apply (that rule governs RENDERING a COMP leaf's value as text, not writing
    /// characters positionally over its bytes). §14.9.43.4 GR3a makes STRING's transfer the alphanumeric MOVE
    /// rules, §14.9.22.3 SR1 names "an alphanumeric or national group item" a valid INSPECT identifier-1, ACCEPT
    /// and UNSTRING store by the same MOVE rules — so every verb that deposits an image into a group calls THIS
    /// (kb/Work PB70 removed five copies of the rule, each carrying this paragraph).</para>
    /// </summary>
    /// <summary>The FULL-image store into a group place — a STORAGE-BOUNDARY write (a file record area receiving a
    /// READ, a FILE STATUS group, the CALL formal's copy-in / copy-out), never the §13.18.38 GR8 sending/receiving
    /// slice: an occurs-depending wrapper is unwrapped and its inner takes the whole image (kb/Work PB80 — an FD
    /// record holding an ODO table is now an <see cref="OdoGroupPlace"/> over a class-tier window, and a
    /// <c>Read(record).FromImage(…)</c> spelled at the call site was CS1061 on the string).</summary>
    public static string WriteFullGroupImage(Place group, string image, string context) =>
        group is OdoGroupPlace o ? WriteFullGroupImage(o.Inner, image, context) : WriteGroupImage(group, image, context);

    public static string WriteGroupImage(Place group, string image, string context) => group switch
    {
        RedefViewPlace => Write(group, image),
        _ when !group.Item.IsImageCapable => EmitText.LoudStmt(TierCIsland.Reason(group.Item, context)),
        OdoGroupPlace { DependingInside: false } odo => ReceiveInto(odo, image),
        OdoGroupPlace odo => WriteGroupImage(odo.Inner, image, context),   // GR8b — the maximum length, whatever the inner's storage shape
        DynTablePlace dyn => $"{RenderPath(dyn.Path, AccessDir.Receiving)}.FromImage({image});",
        _ => $"{Read(group)}.FromImage({image});",
    };

    /// <summary>The character IMAGE of a group place, whatever its storage shape (kb/Work PB80): a record-struct
    /// group's generated <c>AsImage()</c>; a Tier-B / BASED class-tier window's <c>Read</c> (already the string
    /// window); an occurs-depending wrapper's inner image (the wrapper's own <see cref="SendingImage"/> is the GR8
    /// slice of this). THE ONE reader — a consumer that spells <c>.AsImage()</c> itself is wrong for the window shape.</summary>
    public static string GroupImage(Place group) => group switch
    {
        RedefViewPlace => Read(group),
        OdoGroupPlace o => GroupImage(o.Inner),
        _ => $"{Read(group)}.AsImage()",
    };

    /// <summary>The SENDING character image of an occurs-depending GROUP operand (ISO §13.18.38 GR8 — only the
    /// current-count part: the maximum image truncated to the current extent, a prefix by SR22).</summary>
    public static string SendingImage(OdoGroupPlace p) => $"{GroupImage(p.Inner)}.Substring(0, {LengthExpr(p)})";

    /// <summary>A receiving store over an occurs-depending GROUP operand's CURRENT extent (GR8a — depending-outside):
    /// splice the stored prefix over the live image, leaving positions past the count unmodified. <c>allowZeroLength</c>
    /// because a zero current extent (OCCURS 0 TO n DEPENDING at count 0, §13.18.38 GR8a) is a no-op store, NOT a
    /// reference-modification violation — this internal splice is not a user ref-mod, so it must not raise
    /// EC-BOUND-REF-MOD under checking (review V48).</summary>
    public static string ReceiveInto(OdoGroupPlace p, string imageExpr) =>
        WriteGroupImage(p.Inner, RuntimeApi.StrSpliceInto(GroupImage(p.Inner), "1", LengthExpr(p), imageExpr, allowZeroLength: true),
            "occurs-depending group receive");

    /// <summary>The C# <c>int</c> expression for an occurs-depending group operand's current character extent (GR8):
    /// the fixed prefix plus data-name-1's clamped value × the element width, read at the operation site.</summary>
    public static string LengthExpr(OdoGroupPlace p) =>
        RuntimeApi.TableOdoExtent(RuntimeApi.TableOcc(Read(p.Depending)), p.MinOccurs, p.MaxOccurs, p.FixedChars, p.ElemChars);
}
