// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Model;

/// <summary>
/// The ONE typed-lvalue model (COBOLNET_DESIGN §3.3 / §14.1): a resolved reference to a storage location, built
/// once by <see cref="ReferenceResolver"/> and consumed identically by every verb. <see cref="Read"/> yields a C#
/// rvalue expression for the location's current value; <see cref="Write"/> yields a C# statement that stores into
/// it. There is no second lvalue type — MOVE, arithmetic, INSPECT/STRING/UNSTRING, file READ INTO / WRITE FROM, and
/// CALL-by-reference all go through this contract. (Reference-modification and level-88 add <c>Place</c> subtypes
/// in later slices; this slice has the member-access form.)
/// </summary>
public abstract record Place
{
    /// <summary>The analyzed PICTURE of the location (<see langword="null"/> for a group item).</summary>
    public abstract PicInfo? Pic { get; }

    /// <summary>The underlying bound data item this place refers to (carries category, scale, and the profile name).</summary>
    public abstract DataItem Item { get; }

    /// <summary>A C# expression that reads the location's current value.</summary>
    public abstract string Read();

    /// <summary>A C# statement (with trailing <c>;</c>) that stores <paramref name="rhs"/> into the location.</summary>
    public abstract string Write(string rhs);

    /// <summary>The tripwire a subtype whose rendering has moved to the backend's <c>CodeGen.PlaceRenderer</c>
    /// (P7 Step 11 — structural <see cref="Place"/>) uses for its now-unreachable <see cref="Read"/>/<see cref="Write"/>:
    /// every consumer renders through <c>PlaceRenderer</c>, so reaching a migrated subtype's own render method is an
    /// internal error. These methods disappear entirely when the last subtype migrates (with the R5 neutrality test).</summary>
    private protected static string RenderedElsewhere() =>
        throw new InvalidOperationException(
            "a structural Place is rendered by CodeGen.PlaceRenderer (P7 Step 11) — never Place.Read()/Write()");
}

/// <summary>
/// The common base of the WRAPPING places (DESIGN-data-model §2.2 item 1): a decoration over one
/// <see cref="Inner"/> place that keeps the inner item's identity (<see cref="Pic"/>/<see cref="Item"/> forward)
/// and, by default, its plain access (<see cref="Read"/>/<see cref="Write"/> forward — <see cref="OdoGroupPlace"/>
/// keeps them and adds the GR8 seams beside; the view wrappers <see cref="NumericImagePlace"/> and
/// <see cref="RefModPlace"/> override them with their transformed access). Leaf places (<see cref="MemberPlace"/>,
/// <see cref="DynTablePlace"/>, <see cref="RedefViewPlace"/>, <see cref="CapacityRegisterPlace"/>) derive from
/// <see cref="Place"/> directly. <see cref="RenamesPlace"/> stays direct too — it composes N spanned leaves (no
/// single inner) and its Pic/Item are the level-66 ALIAS's own, never a forward, so nothing here applies to it
/// (the DESIGN §2.2 item-1 derive list was over-inclusive on that member).
/// </summary>
public abstract record PlaceDecorator(Place Inner) : Place
{
    /// <inheritdoc/>
    public override PicInfo? Pic => Inner.Pic;

    /// <inheritdoc/>
    public override DataItem Item => Inner.Item;

    /// <inheritdoc/>
    public override string Read() => Inner.Read();

    /// <inheritdoc/>
    public override string Write(string rhs) => Inner.Write(rhs);
}

/// <summary>
/// A direct member-access place: a static field or a (possibly nested, possibly subscripted) member of a
/// <c>record struct</c> — e.g. <c>WS_N</c>, <c>WS_REC.WS_NAME</c>, <c>CobolTable.At(TBL.ROWS, i).VAL</c>. The access
/// <paramref name="Path"/> is a structural <see cref="AccessPath"/> (a field chain + fixed-table accessors + the D10
/// transitional subscript strings), rendered by <c>CodeGen.PlaceRenderer</c>. A fixed-table member is a plain lvalue,
/// so its read and write render the same path (a subscripted OCCURS DYNAMIC element is the sibling
/// <see cref="DynTablePlace"/>, whose accessor is direction-specific).
/// </summary>
public sealed record MemberPlace(AccessPath Path, DataItem MemberItem) : Place
{
    /// <inheritdoc/>
    public override PicInfo? Pic => MemberItem.Pic;

    /// <inheritdoc/>
    public override DataItem Item => MemberItem;

    /// <inheritdoc/>
    public override string Read() => RenderedElsewhere();

    /// <inheritdoc/>
    public override string Write(string rhs) => RenderedElsewhere();
}

/// <summary>
/// A subscripted OCCURS DYNAMIC element (ISO/IEC 1989:2023 §8.5.1.9.2/.9.3; data-model D9). Unlike a fixed table
/// (whose <c>CobolTable.At</c> <c>ref T</c> serves BOTH directions), a dynamic table has direction-specific accessors:
/// <c>RefSending(occ)</c> on a read (an out-of-range occurrence is benign scratch) and <c>RefReceiving(occ)</c> on a
/// write (an occurrence past the current capacity GROWS the table). The <see cref="Path"/>'s trailing
/// <see cref="DynTableSegment"/> renders that polarity at emit time (SENDING for <c>Read</c>, RECEIVING for
/// <c>Write</c>) — one structural path replaces the former two precomputed strings. Rendered by
/// <c>CodeGen.PlaceRenderer</c>. (Kept a distinct subtype rather than folded into <see cref="MemberPlace"/> to bound
/// the Step 11 blast radius — <c>UsageCollectionPass</c>/<c>MoveEmitter</c> discriminate it by type.)
/// </summary>
public sealed record DynTablePlace(AccessPath Path, DataItem ElementItem) : Place
{
    /// <inheritdoc/>
    public override PicInfo? Pic => ElementItem.Pic;

    /// <inheritdoc/>
    public override DataItem Item => ElementItem;

    /// <inheritdoc/>
    public override string Read() => RenderedElsewhere();

    /// <inheritdoc/>
    public override string Write(string rhs) => RenderedElsewhere();
}

/// <summary>
/// A Tier-B REDEFINES view (COBOLNET_DESIGN §4.2): a typed <c>(offset, width)</c> character window over the class's
/// ONE <see cref="string"/> backing field. Reading yields the window's character image (a substring); writing splices
/// a new image back into the backing, preserving its full width — so a write through any view is visible through every
/// other view of the class (one stored backing, ISO §13.18.44). The window carries the VIEW's <see cref="DataItem"/>,
/// so its category/scale/profile drive interpretation: a numeric-DISPLAY view is flagged
/// <see cref="DataItem.StoreAsImage"/>, so the numeric pipeline decodes/encodes the window exactly as for a
/// whole-group numeric leaf. <paramref name="Backing"/> is the structural path to the class's stored backing field;
/// <paramref name="OffsetExpr"/> is the 0-based window offset — the D10 transitional string (a constant, or the
/// <c>classOffset + Σ (idx − 1) × stride</c> arithmetic for a view inside an OCCURS, ISO §13.18.44). Rendered by
/// <c>CodeGen.PlaceRenderer</c>.
/// </summary>
public sealed record RedefViewPlace(AccessPath Backing, string OffsetExpr, int Width, DataItem ViewItem) : Place
{
    /// <inheritdoc/>
    public override PicInfo? Pic => ViewItem.Pic;

    /// <inheritdoc/>
    public override DataItem Item => ViewItem;

    /// <inheritdoc/>
    public override string Read() => RenderedElsewhere();

    /// <inheritdoc/>
    public override string Write(string rhs) => RenderedElsewhere();
}

/// <summary>
/// A level-66 RENAMES place (ISO §13.18.45): ONE elementary-alphanumeric view composed over the spanned record
/// leaves. Reading concatenates the leaves' character images (each leaf field invariantly holds exactly its image
/// width); writing stores the value at the span's width and distributes the slices back into the leaves left to
/// right — so a write through the alias is visible through every renamed item and vice versa (no second storage,
/// SR/GR — RENAMES adds no data item).
/// </summary>
public sealed record RenamesPlace(IReadOnlyList<Place> Leaves, DataItem AliasItem) : Place
{
    /// <inheritdoc/>
    public override PicInfo? Pic => AliasItem.Pic;

    /// <inheritdoc/>
    public override DataItem Item => AliasItem;

    /// <inheritdoc/>
    public override string Read() => RenderedElsewhere();

    /// <inheritdoc/>
    public override string Write(string rhs) => RenderedElsewhere();
}

/// <summary>
/// The <see cref="Place"/> of a GROUP operand whose subtree contains an occurs-depending table (ISO/IEC 1989:2023
/// §13.18.38 Format 2). It decorates the plain member place — <see cref="PlaceDecorator.Read"/>/<see
/// cref="PlaceDecorator.Write"/> are the struct lvalue unchanged (the inherited forwards), so every consumer that
/// does not know about ODO behaves exactly as before — and the GR8 operand seams consult the decoration:
/// <list type="bullet">
///   <item><b>Sending</b> (the sending side of BOTH GR8 quadrants): "only that part of the table area that is
///     specified by the value of [data-name-1] at the start of the operation will be used" — <see
///     cref="SendingImage"/> is the group image truncated to the current extent. SR22 (the subject may be
///     followed within its record only by entries subordinate to it) guarantees the table is the TRAILING
///     storage, so the current extent is a character PREFIX of the maximum image; a zero count with no preceding
///     fixed part is the zero-length item of §8.5.4 item 1.</item>
///   <item><b>Receiving, data-name-1 outside the group</b> (GR8a): the same current extent — character positions
///     past it are NOT modified; <see cref="ReceiveInto"/> splices the stored prefix over the live image.</item>
///   <item><b>Receiving, data-name-1 inside the group</b> (GR8b): "the maximum length of the group will be used"
///     — <see cref="DependingInside"/> lets each receiver keep the plain full-width <c>FromImage</c> store.</item>
/// </list>
/// The legacy engine proved exactly this direction split over the NIST-85 corpus (NC247A; its
/// <c>LocationResolver.ResolveWholeItem(receiving)</c>); the greenfield twin computes the CHARACTER extent at the
/// operand site — no runtime table state (COBOLNET_DESIGN §3.6 / §14.4 — ONE image facility, the GR8 slice is a
/// view over it). The legacy's LINKAGE max-length shortcut is deliberately NOT ported: GR8 applies in any section.
/// </summary>
public sealed record OdoGroupPlace(
    Place Inner, Place Depending, int FixedChars, int ElemChars, int MaxOccurs, bool DependingInside)
    : PlaceDecorator(Inner)
{
    // The GR8 seams — the current character extent (LengthExpr), the sending-side prefix image (SendingImage), and
    // the GR8a receiving splice (ReceiveInto) — are rendered by CodeGen.PlaceRenderer over this record's structure
    // (Inner/Depending places + FixedChars/ElemChars/MaxOccurs). These stubs are unreachable (every consumer renders
    // through PlaceRenderer) and disappear when structural Place is complete.
    public string LengthExpr => RenderedElsewhere();
    public string SendingImage() => RenderedElsewhere();
    public string ReceiveInto(string imageExpr) => RenderedElsewhere();
}

/// <summary>
/// A NUMERIC-DISPLAY item viewed as its CHARACTER IMAGE for reference modification (ISO §8.4.2.4 — the unique
/// result is an elementary alphanumeric item over the operand's standard data format): reading formats the stored
/// value's display image; writing decodes the spliced image back into the typed field (sign-aware both ways via
/// the FormatDisplay/ParseDisplay pair).
/// </summary>
public sealed record NumericImagePlace(Place Inner) : PlaceDecorator(Inner)
{
    /// <inheritdoc/>
    public override string Read() => RenderedElsewhere();

    /// <inheritdoc/>
    public override string Write(string rhs) => RenderedElsewhere();
}

/// <summary>
/// The CAPACITY register of an OCCURS DYNAMIC table (ISO/IEC 1989:2023 §13.18.38 GR15 / §8.5.1.9.1; data-model D9):
/// a VIEW over the table's current capacity, NOT its own storage. Reading emits <c>{TablePath}.Capacity</c> — the
/// runtime <see cref="CobolNet.Runtime.CobolDynTable{T}.Capacity"/> (a native <c>long</c>, an unsigned integer per
/// SR31), so <see cref="RegisterItem"/> carries a native-binary <see cref="PicInfo"/> (scale 0) and the numeric
/// pipeline reads it as a scale-0 integer with no profile. The register is set ONLY by SET Format 14 (which emits
/// <c>SetCapacity</c>/<c>CapacityUpBy</c>/<c>CapacityDownBy</c> directly, never through <see cref="Write"/>); an
/// ordinary store receiver is rejected COBOLNET1523 at bind time (SR30–32), so <see cref="Write"/> is unreachable.
/// </summary>
public sealed record CapacityRegisterPlace(AccessPath Table, DataItem RegisterItem) : Place
{
    /// <inheritdoc/>
    public override PicInfo? Pic => RegisterItem.Pic;

    /// <inheritdoc/>
    public override DataItem Item => RegisterItem;

    /// <inheritdoc/>
    public override string Read() => RenderedElsewhere();

    /// <inheritdoc/>
    public override string Write(string rhs) => RenderedElsewhere();
}

/// <summary>
/// A reference-modified place <c>inner(start:length)</c> (COBOLNET_DESIGN §3.3 / §7.2): reading is a substring
/// (<c>CobolString.RefMod</c>); writing splices the new slice back into the inner field (<c>CobolString.SpliceInto</c>),
/// preserving the inner's width. <paramref name="Length"/> is <see langword="null"/> for the "to the end" form.
/// </summary>
public sealed record RefModPlace(Place Inner, string Start, string? Length) : PlaceDecorator(Inner)
{
    // Read/Write/WriteFill are rendered by CodeGen.PlaceRenderer over Inner + Start/Length. Start/Length stay the
    // rendered index string (the D10 TRANSITIONAL carrier — they become BoundExpr when PHASE 15 removes the
    // SUBSCRIPT lexer mode; see the PHASE-07 Step 11 plan). These stubs are unreachable (consumers route through
    // PlaceRenderer) and disappear at the structural-Place delete.
    public override string Read() => RenderedElsewhere();
    public override string Write(string rhs) => RenderedElsewhere();
    public string WriteFill(string fillChar) => RenderedElsewhere();
}
