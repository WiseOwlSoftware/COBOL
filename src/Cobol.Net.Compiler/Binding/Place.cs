// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding;

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
/// A NUMERIC-DISPLAY item viewed as its CHARACTER IMAGE for reference modification (ISO §8.4.2.4 — the unique
/// result is an elementary alphanumeric item over the operand's standard data format): reading formats the stored
/// value's display image; writing decodes the spliced image back into the typed field (sign-aware both ways via
/// the FormatDisplay/ParseDisplay pair).
/// </summary>
public sealed record NumericImagePlace(Place Inner) : Place
{
    /// <inheritdoc/>
    public override PicInfo? Pic => Inner.Pic;

    /// <inheritdoc/>
    public override DataItem Item => Inner.Item;

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
/// A reference-modified place <c>inner(start:length)</c> (COBOLNET_DESIGN §3.3 / §7.2): reading is a substring
/// (<c>CobolString.RefMod</c>); writing splices the new slice back into the inner field (<c>CobolString.SpliceInto</c>),
/// preserving the inner's width. <paramref name="Length"/> is <see langword="null"/> for the "to the end" form.
/// </summary>
public sealed record RefModPlace(Place Inner, string Start, string? Length) : Place
{
    // The start/length operands may be `long` fields, but the runtime takes `int` positions — cast at the call site.
    private string Start32 => $"(int)({Start})";
    private string Len32 => Length is null ? "-1" : $"(int)({Length})";

    /// <inheritdoc/>
    public override PicInfo? Pic => Inner.Pic;

    /// <inheritdoc/>
    public override DataItem Item => Inner.Item;

    /// <inheritdoc/>
    public override string Read() => $"CobolString.RefMod({Inner.Read()}, {Start32}, {Len32})";

    /// <inheritdoc/>
    public override string Write(string rhs) =>
        Inner.Write($"CobolString.SpliceInto({Inner.Read()}, {Start32}, {Len32}, {rhs})");
}
