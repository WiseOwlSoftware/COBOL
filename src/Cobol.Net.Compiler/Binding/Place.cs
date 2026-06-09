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
public sealed record RedefViewPlace(string Backing, int Offset, int Width, DataItem ViewItem) : Place
{
    /// <inheritdoc/>
    public override PicInfo? Pic => ViewItem.Pic;

    /// <inheritdoc/>
    public override DataItem Item => ViewItem;

    /// <summary>The character window this view occupies (1-based leftmost position; <c>CobolString.RefMod</c>).</summary>
    private string Window => $"CobolString.RefMod({Backing}, {Offset + 1}, {Width})";

    /// <inheritdoc/>
    public override string Read() => Window;

    /// <inheritdoc/>
    public override string Write(string rhs) =>
        $"{Backing} = CobolString.SpliceInto({Backing}, {Offset + 1}, {Width}, {rhs});";
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
