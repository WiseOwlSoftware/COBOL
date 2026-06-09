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
