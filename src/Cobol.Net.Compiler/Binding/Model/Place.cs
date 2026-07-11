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
/// <c>record struct</c> — e.g. <c>WS_N</c>, <c>WS_REC.WS_NAME</c>, <c>TBL.ROWS[i - 1].VAL</c>. The access
/// <paramref name="Path"/> is a plain C# lvalue, so reading is the path itself and writing is an assignment.
/// </summary>
public sealed record MemberPlace(string Path, DataItem MemberItem) : Place
{
    /// <inheritdoc/>
    public override PicInfo? Pic => MemberItem.Pic;

    /// <inheritdoc/>
    public override DataItem Item => MemberItem;

    /// <inheritdoc/>
    public override string Read() => Path;

    /// <inheritdoc/>
    public override string Write(string rhs) => $"{Path} = {rhs};";
}

/// <summary>
/// A subscripted OCCURS DYNAMIC element (ISO/IEC 1989:2023 §8.5.1.9.2/.9.3; data-model D9). Unlike a fixed table
/// (whose <c>CobolTable.At</c> returns a <c>ref T</c> that serves BOTH directions), a dynamic table has two distinct
/// runtime accessors: <c>RefSending(occ)</c> (a read — an out-of-range occurrence is benign scratch) and
/// <c>RefReceiving(occ)</c> (a write — an occurrence past the current capacity GROWS the table, seeding the skipped
/// intermediates). A single access-path string cannot carry that polarity, so this place holds BOTH pre-computed
/// paths: <see cref="Read"/> emits the sending path, <see cref="Write"/> the receiving path (which grows on demand).
/// A subordinate of a dynamic element (a group element's field, or a fixed OCCURS below the dynamic level) is the
/// tail appended after the accessor in each path.
/// </summary>
public sealed record DynTablePlace(string SendingPath, string ReceivingPath, DataItem ElementItem) : Place
{
    /// <inheritdoc/>
    public override PicInfo? Pic => ElementItem.Pic;

    /// <inheritdoc/>
    public override DataItem Item => ElementItem;

    /// <inheritdoc/>
    public override string Read() => SendingPath;

    /// <inheritdoc/>
    public override string Write(string rhs) => $"{ReceivingPath} = {rhs};";
}

/// <summary>
/// A Tier-B REDEFINES view (COBOLNET_DESIGN §4.2): a typed <c>(offset, width)</c> character window over the class's
/// ONE <see cref="string"/> backing field. Reading yields the window's character image (a substring); writing splices
/// a new image back into the backing, preserving its full width — so a write through any view is visible through every
/// other view of the class (one stored backing, ISO §13.18.44). The window carries the VIEW's <see cref="DataItem"/>,
/// so its category/scale/profile drive interpretation: a numeric-DISPLAY view is flagged
/// <see cref="DataItem.StoreAsImage"/>, so the numeric pipeline decodes/encodes the window via
/// <c>CobolNum.ParseDisplay</c>/<c>FormatDisplay</c> exactly as for a whole-group numeric leaf (no new emitter path).
/// </summary>
public sealed record RedefViewPlace(string Backing, string OffsetExpr, int Width, DataItem ViewItem) : Place
{
    /// <inheritdoc/>
    public override PicInfo? Pic => ViewItem.Pic;

    /// <inheritdoc/>
    public override DataItem Item => ViewItem;

    /// <summary>The character window this view occupies (1-based leftmost position; <c>CobolString.RefMod</c>).
    /// <see cref="OffsetExpr"/> is a 0-based C# <c>long</c> expression — a constant for an unsubscripted view, or
    /// the computed <c>classOffset + Σ (idx − 1) × stride</c> for a view inside an OCCURS (ISO §13.18.44 — a
    /// redefined table lays its occurrences end-to-end in the ONE backing).</summary>
    private string Window => $"CobolString.RefMod({Backing}, (int)({OffsetExpr} + 1), {Width})";

    /// <inheritdoc/>
    public override string Read() => Window;

    /// <inheritdoc/>
    public override string Write(string rhs) =>
        $"{Backing} = CobolString.SpliceInto({Backing}, (int)({OffsetExpr} + 1), {Width}, {rhs});";
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
    public override string Read() =>
        Leaves.Count == 1 ? Leaves[0].Read() : "(" + string.Join(" + ", Leaves.Select(l => l.Read())) + ")";

    /// <inheritdoc/>
    public override string Write(string rhs)
    {
        if (Leaves.Count == 1) return Leaves[0].Write(rhs);
        int width = Leaves.Sum(l => l.Item.ImageWidth);
        var sb = new System.Text.StringBuilder();
        sb.Append($"{{ string __ren = CobolString.Store({rhs}, {width});");
        int off = 0;
        foreach (var l in Leaves)
        {
            int w = l.Item.ImageWidth;
            sb.Append(' ').Append(l.Write($"__ren.Substring({off}, {w})"));
            off += w;
        }
        return sb.Append(" }").ToString();
    }
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
    /// <summary>C# <c>int</c> expression: the operand's CURRENT character extent — the fixed prefix plus
    /// data-name-1's value × the element width. The count is read at the operation site (GR8 — "at the start of
    /// the operation") through <c>CobolTable.Occ</c> (storage-form-agnostic: native <c>long</c> or a
    /// whole-group-aliased character image) and clamped benignly to [0, max]: a count outside
    /// integer-1..integer-2 at reference time makes the excess content undefined (GR7) — EC-BOUND-ODO is the
    /// 2002+ checked mode, the later EC slice (SSOT §11); COBOL-85 has no exception conditions.</summary>
    public string LengthExpr =>
        $"CobolTable.OdoExtent(CobolTable.Occ({Depending.Read()}), {MaxOccurs}, {FixedChars}, {ElemChars})";

    /// <summary>The group's SENDING character image (ISO §13.18.38 GR8 — both quadrants send the current-count
    /// part): the maximum image truncated to <see cref="LengthExpr"/> characters (a prefix, by SR22).</summary>
    public string SendingImage() => $"{Inner.Read()}.AsImage().Substring(0, {LengthExpr})";

    /// <summary>A complete receiving C# statement for the GR8a (depending-outside) quadrant: store
    /// <paramref name="imageExpr"/> over the CURRENT extent only — splice it into the live image, leaving every
    /// character position past the count unmodified (GR8a), then distribute back through the group's generated
    /// <c>FromImage</c> (the §14.4 single image facility).</summary>
    public string ReceiveInto(string imageExpr) =>
        $"{Inner.Read()}.FromImage(CobolString.SpliceInto({Inner.Read()}.AsImage(), 1, {LengthExpr}, {imageExpr}));";
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
    /// <remarks>The FormatDisplay/StoreDisplay overload sets are the storage-form BRIDGE (the
    /// <c>CobolTable.Occ</c> pattern): whether the field is a native long/Int128 or an image-stored string is
    /// decided by the post-bind whole-group analysis, AFTER this expression text is produced — C# overload
    /// resolution picks the right conversion at backend-compile time.</remarks>
    public override string Read() => $"CobolNum.FormatDisplay({Inner.Read()}, {Inner.Item.ProfileName})";

    /// <inheritdoc/>
    public override string Write(string rhs) =>
        Inner.Write($"CobolNum.StoreDisplay({rhs}, {Inner.Item.ProfileName}, {Inner.Read()})");
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
public sealed record CapacityRegisterPlace(string TablePath, DataItem RegisterItem) : Place
{
    /// <inheritdoc/>
    public override PicInfo? Pic => RegisterItem.Pic;

    /// <inheritdoc/>
    public override DataItem Item => RegisterItem;

    /// <inheritdoc/>
    public override string Read() => $"{TablePath}.Capacity";

    /// <inheritdoc/>
    public override string Write(string rhs) =>
        // Unreachable: SET Format 14 routes to BoundSetCapacity (SetCapacity/CapacityUpBy/CapacityDownBy), and any
        // other store into the register is rejected COBOLNET1523 before a receiving Place.Write is emitted (§13.18.38
        // SR30–32). This throw is the internal-error backstop if a new receiver path forgets that bind-time gate.
        throw new InvalidOperationException(
            "the CAPACITY register is set only by SET Format 14 (ISO §13.18.38 SR30-32); a direct store must be "
            + "rejected COBOLNET1523 at bind time and never reach Place.Write");
}

/// <summary>
/// A reference-modified place <c>inner(start:length)</c> (COBOLNET_DESIGN §3.3 / §7.2): reading is a substring
/// (<c>CobolString.RefMod</c>); writing splices the new slice back into the inner field (<c>CobolString.SpliceInto</c>),
/// preserving the inner's width. <paramref name="Length"/> is <see langword="null"/> for the "to the end" form.
/// </summary>
public sealed record RefModPlace(Place Inner, string Start, string? Length) : PlaceDecorator(Inner)
{
    // The start/length operands may be `long` fields, but the runtime takes `int` positions — cast at the call site.
    private string Start32 => $"(int)({Start})";
    private string Len32 => Length is null ? "-1" : $"(int)({Length})";

    /// <inheritdoc/>
    public override string Read() => $"CobolString.RefMod({Inner.Read()}, {Start32}, {Len32})";

    /// <inheritdoc/>
    public override string Write(string rhs) =>
        // A boolean receiver splices with boolean-zero fill (§14.6.8.6; §8.4.3.3 GR5a — under D-B1 a bit
        // position IS a char index); every other category keeps the space fill.
        Inner.Write($"CobolString.SpliceInto({Inner.Read()}, {Start32}, {Len32}, {rhs}"
            + $"{(Inner.Item.Pic is { Category: PicCategory.Boolean } ? ", pad: '0'" : "")})");

    /// <summary>A figurative-constant store into the slice: the figurative fills EVERY position of the
    /// reference-modified item (ISO §8.3.3.6.4 GR2 — repeated to the size of the associated fixed-length item;
    /// §8.4.3.3 GR5/GR6 — the ref-mod result is a unique elementary item of the slice length). Realized by an
    /// EMPTY slice with the fill char as the SpliceInto pad, so every targeted position takes the fill (works
    /// for a runtime-length slice too). <paramref name="fillChar"/> is a C# <c>char</c>-literal expression.</summary>
    public string WriteFill(string fillChar) =>
        Inner.Write($"CobolString.SpliceInto({Inner.Read()}, {Start32}, {Len32}, \"\", pad: {fillChar})");
}
