// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Microsoft.CodeAnalysis.CSharp;

namespace CobolNet.Binding;

/// <summary>
/// A bound DATA DIVISION item: a node in the record tree. Elementary items (<see cref="Pic"/> non-null) become
/// native C# fields; group items (children, no PIC) become nested <c>record struct</c> types. There is no byte
/// offset or storage length — the .NET type IS the storage.
/// </summary>
public sealed class DataItem
{
    /// <summary>The COBOL level number (01, 05, 77, …). Levels 66/88 are not modeled in this slice.</summary>
    public required int Level { get; init; }

    /// <summary>
    /// A globally-unique id assigned at bind time. It backs the collision-free names for an item's
    /// <c>record struct</c> type (<see cref="StructName"/>) and its numeric profile field (<see cref="ProfileName"/>)
    /// — two distinct groups both named <c>REC</c>, or two leaves both named <c>NUM</c> in different groups, would
    /// otherwise collide. (The member/field name, <see cref="CsName"/>, only needs to be unique within its struct.)
    /// </summary>
    public int Uid { get; set; }

    /// <summary>The original COBOL data-name, or <see langword="null"/> for <c>FILLER</c>.</summary>
    public string? CobolName { get; init; }

    /// <summary>The C#-safe member/field identifier for this item (unique within its containing struct scope).</summary>
    public required string CsName { get; set; }

    /// <summary>The analyzed PICTURE/USAGE for an elementary item; <see langword="null"/> for a group.</summary>
    public PicInfo? Pic { get; set; }

    /// <summary>The raw VALUE operand text (e.g. <c>"ABC"</c> or <c>-12.5</c>), or <see langword="null"/> if none.</summary>
    public string? RawValue { get; init; }

    /// <summary>The fixed OCCURS count, or <see langword="null"/> if the item is not a table. (ODO is a later slice.)</summary>
    public int? Occurs { get; init; }

    /// <summary>The INDEXED BY index-names declared on this item's OCCURS clause (empty if none).</summary>
    public List<string> IndexNames { get; } = [];

    /// <summary>
    /// When true, this numeric USAGE-DISPLAY elementary item is stored as its CHARACTER IMAGE (a C# <see cref="string"/>
    /// of zoned digits) rather than a native <see cref="long"/>. Set by the bind-time whole-group analysis for a
    /// numeric-DISPLAY leaf that lives under a group used as a whole (non-elementary) operand: ISO/IEC 1989:2023 §14.9
    /// MOVE GR4 fills such a group "without consideration for the individual elementary items", so the leaf may receive
    /// non-numeric characters (e.g. spaces) that a native <c>long</c> cannot represent. Numeric <i>use</i> of the leaf
    /// then goes through <c>CobolNum.ParseDisplay</c> (read) / <c>CobolNum.FormatDisplay</c> (write); the common case
    /// (a numeric leaf never referenced as part of a whole group) stays a native <c>long</c> (locked invariant #2).
    /// </summary>
    public bool StoreAsImage { get; set; }

    /// <summary>Subordinate items (group members). Empty for an elementary item.</summary>
    public List<DataItem> Children { get; } = [];

    /// <summary>The raw REDEFINES target data-name as written (ISO §13.18.44), resolved post-build; null if none.</summary>
    public string? RedefinesTargetName { get; init; }

    /// <summary>The resolved REDEFINES target item (the immediately-redefined entry, which may itself be a
    /// redefiner — SR11). Set by the post-build pass; null for a non-redefining entry.</summary>
    public DataItem? RedefinesTarget { get; set; }

    /// <summary>The level-66 RENAMES descriptor (ISO §13.18.45), or null unless this is a level-66 entry.</summary>
    public RenamesInfo? Renames { get; set; }

    /// <summary>The redefines class (shared-storage equivalence class) this item belongs to, or null if it stands
    /// alone. Every member of a class — the original + every redefiner — points to the SAME instance.</summary>
    public RedefinesClass? Class { get; set; }

    /// <summary>True for the ONE stored member of a redefines class (the non-redefining anchor — SR7); every other
    /// member is a computed view. Defaults true so a standalone item (the whole existing corpus) emits normally.</summary>
    public bool IsCanonical { get; set; } = true;

    /// <summary>The start of this view's window within its class's concatenated image (0 for a whole-area redefiner;
    /// &gt;0 for a partial-overlap view or a RENAMES sub-span). Meaningful only when <see cref="Class"/> is set.</summary>
    public int ClassOffset { get; set; }

    /// <summary>The level-66 RENAMES entries attached to this record (a 01/FD/SD owner). They are NOT storage
    /// children (they add no storage, ISO §13.18.45) — kept here so layout / struct emission ignores them while
    /// reference resolution can still find them.</summary>
    public List<DataItem> Renames66 { get; } = [];

    /// <summary>The containing group, or <see langword="null"/> for a top-level (01/77) item.</summary>
    public DataItem? Parent { get; set; }

    /// <summary>True for a group item (has children, no PICTURE).</summary>
    public bool IsGroup => Pic is null && Children.Count > 0;

    /// <summary>True for an elementary item (has a PICTURE).</summary>
    public bool IsElementary => Pic is not null;

    /// <summary>The generated <c>record struct</c> type name for a group item (unique via <see cref="Uid"/>).</summary>
    public string StructName => "_T_" + Uid;

    /// <summary>The generated runtime <c>NumProfile</c> field name for a numeric item (unique via <see cref="Uid"/>).</summary>
    public string ProfileName => "_P_" + Uid;

    /// <summary>
    /// True if this item's storage is a pure character image (a C# <see cref="string"/> at every leaf) — so the group
    /// has a clean <c>AsImage()</c>/<c>FromImage()</c> (COBOLNET_DESIGN §14.4 / Tier-B §4.2). A leaf qualifies when it
    /// is alphanumeric / numeric-edited (string-stored), OR a numeric-DISPLAY leaf flagged <see cref="StoreAsImage"/>
    /// (also string-stored, its zoned image). An OCCURS table of a character-image element qualifies too — its image
    /// is every occurrence's image concatenated (ISO §14.9 group move includes every OCCURS position; the count is the
    /// fixed/max occurrence count, the only OCCURS form the data model tracks today). A group with a
    /// COMP/COMP-3/COMP-5/float leaf (native non-character storage) is the genuine mixed-usage byte-island (Tier-C),
    /// deferred.
    /// </summary>
    public bool IsCharacterImage =>
        IsElementary
            ? Pic?.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited || StoreAsImage
            : IsGroup && Children.All(c => c.IsCharacterImage);

    /// <summary>The character width of this item's image — meaningful for an <see cref="IsCharacterImage"/> item. For a
    /// group it is the sum of each child's TOTAL image contribution, i.e. the child's per-occurrence image width × its
    /// fixed-OCCURS count (every OCCURS position is part of the group image, ISO §14.9). A numeric-DISPLAY leaf's image
    /// is its digit count plus a separate-sign character when SIGN IS SEPARATE (ISO §13.18.52); an over-punched sign
    /// occupies no extra position. (This is the per-occurrence width of THIS item; a parent multiplies by THIS item's
    /// own OCCURS count.)</summary>
    public int ImageWidth =>
        IsElementary ? ElementaryImageWidth : Children.Sum(c => c.ImageWidth * (c.Occurs ?? 1));

    /// <summary>The character-image width of an elementary item (digit count + a separate-sign position when present
    /// for a signed numeric; otherwise the PICTURE's character length).</summary>
    private int ElementaryImageWidth
    {
        get
        {
            if (Pic is not { } pic) return 0;
            if (pic.Category is PicCategory.Numeric)
                return pic.Digits + (pic.Signed && pic.SignKind is "LeadingSeparate" or "TrailingSeparate" ? 1 : 0);
            return pic.Length;
        }
    }

    /// <summary>The C# type of a single occurrence (a record-struct type name for a group, a <see cref="string"/> for
    /// a <see cref="StoreAsImage"/> numeric leaf, else the PIC's CLR type).</summary>
    public string ElementType => IsGroup ? StructName : StoreAsImage ? "string" : Pic?.ClrType ?? "object";

    /// <summary>The C# type name for this item's field — an array of <see cref="ElementType"/> for an OCCURS table.</summary>
    public string FieldType => Occurs is not null ? ElementType + "[]" : ElementType;

    /// <summary>Back-compat alias of <see cref="ElementType"/> (the per-occurrence type).</summary>
    public string ClrType => ElementType;

    /// <summary>
    /// Convert a COBOL data-name to a valid, collision-safe C# identifier: hyphens → underscores, a leading digit
    /// gets an underscore prefix, and C# keywords are escaped with <c>@</c>.
    /// </summary>
    public static string Sanitize(string cobolName)
    {
        string s = cobolName.Replace('-', '_');
        if (s.Length == 0 || char.IsDigit(s[0])) s = "_" + s;
        return SyntaxFacts.GetKeywordKind(s) != SyntaxKind.None ? "@" + s : s;
    }
}
