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

    /// <summary>This entry's OWN SIGN clause (ISO §13.18.52), or <see langword="null"/> when none — captured even on
    /// a group item: a group-level SIGN applies to every subordinate signed numeric DISPLAY item, nearest enclosing
    /// clause winning (GR1–3, applied by the binder's post-build inheritance pass).</summary>
    public SignSpec? OwnSign { get; init; }

    /// <summary>This entry's OWN USAGE keyword (ISO §13.18.60), or <see langword="null"/> when none — captured even
    /// on a group item: a group-level USAGE applies to every elementary item subordinate to it (GR1; NC107A's
    /// <c>01 U9 USAGE COMPUTATIONAL</c> makes the PICTURE-only U10 binary). Applied by the binder's post-build
    /// <c>InheritUsageClauses</c> pass.</summary>
    public Usage? OwnUsage { get; init; }

    /// <summary>The raw VALUE operand text (e.g. <c>"ABC"</c> or <c>-12.5</c>), or <see langword="null"/> if none.</summary>
    public string? RawValue { get; init; }

    /// <summary>True when this entry carries a TYPEDEF clause — it is a TYPE DECLARATION (a named template; ISO
    /// §13.18.58, data-model D17), allocating NO storage. Registered in <c>DataBinder.TypeDecls</c>, kept OFF
    /// <c>Roots</c>/<c>ByName</c>; its subordinate names are not globally referenceable (GR1).</summary>
    public bool IsTypedef { get; init; }

    /// <summary>True when the TYPEDEF carries STRONG (ISO §13.18.58.2) — the declared type is strongly typed, so its
    /// referencing items may interoperate only with the same type (the compile-time §8.5.3.3 checks).</summary>
    public bool TypedefStrong { get; init; }

    /// <summary>The type-name of a <c>TYPE IS type-name</c> reference (ISO §13.18.57), or null. The referencing entry
    /// is CLONED from that type declaration's subtree by the post-build <c>DataBinder.ExpandTypes</c> pass (D17), which
    /// clears this once expanded.</summary>
    public string? TypeRefName { get; set; }

    /// <summary>After <c>ExpandTypes</c>: the type-name this item (or its containing subtree root) was cloned from —
    /// backs the §8.5.3.3 STRONG same-type check. Null for a non-typed item.</summary>
    public string? TypeName { get; set; }

    /// <summary>After <c>ExpandTypes</c>: true when this item is the subject of a TYPE clause referencing a STRONG
    /// type declaration (an item is strongly typed if it or any ancestor has this set). Drives the §8.8.4 gates.</summary>
    public bool StrongType { get; set; }

    /// <summary>The outermost enclosing item (this item or an ancestor) whose data description is strongly typed —
    /// i.e. carries, or is subordinate to, a TYPE clause referencing a STRONG type declaration (ISO §8.5.3.1). Null
    /// when the item is not part of any strongly-typed subtree. Backs the §8.5.3.3 use-restriction gates and the
    /// §8.5.3 same-type test.</summary>
    public DataItem? StrongRoot
    {
        get
        {
            DataItem? root = null;
            for (DataItem? cur = this; cur is not null; cur = cur.Parent)
                if (cur.StrongType) root = cur;
            return root;
        }
    }

    /// <summary>True when this item is a strongly-typed GROUP — the operand form the MOVE / comparison /
    /// class-condition same-type gates restrict (ISO §8.5.3.3: only group items may be strongly typed;
    /// §14.9.25.3 SR2 / §8.8.4.2.3 SR1 / §8.8.4.4.3 SR1). An elementary leaf subordinate to a strong group is NOT
    /// strongly typed, so its individual MOVE / comparison is unrestricted (a strong record is still built up field
    /// by field).</summary>
    public bool IsStrongGroup => IsGroup && StrongRoot is not null;

    /// <summary>True when this item is part of any strongly-typed subtree (a strong group OR a leaf subordinate to
    /// one) — backs the §13.18.57.3 SR3/SR4 "in whole or in part" declaration checks (a RENAMES / REDEFINES touching
    /// any part of a strong item is prohibited).</summary>
    public bool IsStronglyTyped => StrongRoot is not null;

    /// <summary>The NEAREST enclosing item (this item or an ancestor) that directly carries a TYPE clause — the item
    /// whose <see cref="TypeName"/> it acquired. This is the item's "type" for the §8.5.3 same-type test: a nested
    /// <c>TYPE INNER-T</c> subgroup is anchored by INNER-T (itself), NOT by the outermost strong record. Null when the
    /// item is not part of any typed subtree.</summary>
    public DataItem? TypeAnchor
    {
        get
        {
            for (DataItem? cur = this; cur is not null; cur = cur.Parent)
                if (cur.TypeName is not null) return cur;
            return null;
        }
    }

    /// <summary>Two operands are of the SAME type (ISO §8.5.3 / §8.5.3.3) when their NEAREST TYPE anchors reference
    /// equivalent type declarations — within one source element, identically-named ones (cross-program EXTERNAL
    /// equivalence is a follow-up) — and each operand occupies the identical relative position within that type
    /// (the §8.5.3 "same subordinate item in equivalent type declarations" rule; both are clones of one template, so
    /// corresponding items share a member-name path from the anchor down). Uses <see cref="TypeAnchor"/>, not
    /// <see cref="StrongRoot"/>, so a nested strong subgroup is matched by ITS OWN type, not the enclosing record's.</summary>
    public static bool SameStrongType(DataItem a, DataItem b)
    {
        if (a.TypeAnchor is not { } ra || b.TypeAnchor is not { } rb) return false;
        if (!string.Equals(ra.TypeName, rb.TypeName, StringComparison.OrdinalIgnoreCase)) return false;
        return RelativeMemberPath(a, ra).SequenceEqual(RelativeMemberPath(b, rb), StringComparer.Ordinal);
    }

    /// <summary>The member-name path from <paramref name="root"/> (exclusive) down to <paramref name="item"/>
    /// (inclusive), root-first — the operand's relative position within its strong type.</summary>
    private static List<string> RelativeMemberPath(DataItem item, DataItem root)
    {
        var path = new List<string>();
        for (DataItem? cur = item; cur is not null && !ReferenceEquals(cur, root); cur = cur.Parent)
            path.Add(cur.CsName);
        path.Reverse();
        return path;
    }

    /// <summary>The ALLOCATED occurrence count — the table's physical capacity — or <see langword="null"/> if the
    /// item is not a table. For a fixed (Format 1) table this is the OCCURS count; for an occurs-depending (Format 2)
    /// table it is the MAXIMUM, integer-2 (ISO §8.5.1.8 — "the physical capacity is fixed at compile time; the
    /// logical capacity may vary"). The variable current count lives in <see cref="OccursSpec"/>.</summary>
    public int? Occurs { get; init; }

    /// <summary>The structured OCCURS DEPENDING ON / KEY description (ISO §13.18.38 Format 2 + GR3), or
    /// <see langword="null"/> for a non-table or a plain keyless fixed table (which <see cref="Occurs"/> alone
    /// describes). Carries the integer-1..integer-2 bounds, the resolved DEPENDING ON data-name-1, and the
    /// ASCENDING/DESCENDING KEY data-names.</summary>
    public OccursSpec? OccursSpec { get; init; }

    /// <summary>True for a Format-4 DYNAMIC-capacity table (ISO §13.18.38, data-model D9): capacity varies at run
    /// time; storage is the out-of-line <c>CobolDynTable&lt;T&gt;</c>, and <see cref="Occurs"/> (the fixed physical
    /// capacity) is null.</summary>
    public bool IsDynamicTable => OccursSpec is { IsDynamic: true };

    /// <summary>True for ANY table — fixed (<see cref="Occurs"/>) OR dynamic (D9). Use at table-RECOGNITION sites
    /// (subscript arity, SEARCH detection); keep <c>Occurs is not null</c> at fixed-capacity-ARITHMETIC sites
    /// (static image width, fixed-array init) where a dynamic table must NOT be treated as a fixed run.</summary>
    public bool IsTable => Occurs is not null || IsDynamicTable;

    /// <summary>The INDEXED BY index-names declared on this item's OCCURS clause (empty if none).</summary>
    public List<string> IndexNames { get; } = [];

    /// <summary>
    /// True when this numeric fixed-point elementary item is stored as its CHARACTER IMAGE (a C# <see cref="string"/>
    /// of zoned digits) rather than a native <see cref="long"/> — a numeric-DISPLAY leaf under a group used as a
    /// whole operand (ISO/IEC 1989:2023 §14.9 MOVE GR4 fills such a group "without consideration for the individual
    /// elementary items", so the leaf may receive non-numeric characters a native <c>long</c> cannot represent), a
    /// Tier-B REDEFINES window, a figurative-fill / ref-mod-store receiver, a FILE-record or report print face, or
    /// an OO crossing-form flip. Numeric <i>use</i> then goes through <c>CobolNum.ParseDisplay</c> (read) /
    /// <c>CobolNum.FormatDisplay</c> (write); the common case stays a native <c>long</c> (locked invariant #2).
    /// <para>P5.7: a READ-ONLY projection of <see cref="Storage"/> — the mutable flag and its 9 cross-layer write
    /// sites are deleted; <c>StorageFormPass</c> computes the decision ONCE from the collected facts. NULL-Storage
    /// (i.e. pre-group-tail) reads answer <c>false</c>, exactly the flag's early value for every legal read (the
    /// bind-time early consumers use <see cref="DataBinder.IsImageBackedEarly"/> instead).</para>
    /// </summary>
    public bool StoreAsImage => Storage is Model.StorageForm.CharImage { Category: PicCategory.Numeric };

    /// <summary>JUSTIFIED [RIGHT] (ISO §13.18.34): alphanumeric/alphabetic receives right-justify — space-fill on
    /// the LEFT when the sender is shorter, truncate from the LEFT when longer (§14.9.25.4 GR6c).</summary>
    public bool Justified { get; init; }

    /// <summary>BLANK [WHEN] ZERO (ISO §13.18.8): storing a ZERO value fills the item with spaces — applied at
    /// every numeric-edited store (MOVE editing and arithmetic resultants alike).</summary>
    public bool BlankWhenZero { get; init; }

    /// <summary>The entry carried a SYNCHRONIZED / SYNC clause (ISO §13.18.55). A no-op in the typed-native model
    /// (no byte alignment), but recorded so the edition validator can gate SYNCHRONIZED on a GROUP item — a
    /// COBOL-2023 introduction (Annex E.3.2 item 6) — below 2023 (P3 step 10). Not emitted.</summary>
    public bool Synchronized { get; init; }

    /// <summary>Subordinate items (group members). Empty for an elementary item.</summary>
    public List<DataItem> Children { get; } = [];

    /// <summary>The level-88 condition-names whose conditional variable is THIS item (ISO §13.18.4). Normally these
    /// also live in <c>DataBinder.Conditions</c> (the global by-name index), but a TYPEDEF template keeps them ONLY
    /// here — its condition-names are not globally referenceable (§13.18.58.4 GR1) until a <c>TYPE</c> reference
    /// clones the item, at which point the clone's copies ARE registered globally (data-model D17 inc 3).</summary>
    public List<Condition88> Own88s { get; } = [];

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

    /// <summary>True for a BASED 01/77 entry (ISO §13.18.5 — a storage TEMPLATE with an implicit data-address
    /// pointer, initially NULL; no storage of its own until SET ADDRESS OF / ALLOCATE gives it one). The
    /// post-build pass routes every reference through the pointer (Phase-4b increment 2).</summary>
    public bool IsBased { get; set; }

    /// <summary>The start of this view's window within its class's concatenated image (0 for a whole-area redefiner;
    /// &gt;0 for a partial-overlap view or a RENAMES sub-span). Meaningful only when <see cref="Class"/> is set.</summary>
    public int ClassOffset { get; set; }

    /// <summary>The canonical storage representation of this ELEMENTARY item (null for a group — a group emits as a
    /// record struct and answers its image facts recursively over children). Computed ONCE by
    /// <see cref="Passes.StorageFormPass"/> after all facts are known (rearchitecture PHASE 05; DESIGN-data-model
    /// §2.1); the pass (classification + the crossing-form harmonize) is the SOLE writer — the internal setter
    /// documents the single-writer discipline. (The design's init-only shape is not expressible with the
    /// pass-assignment pattern; recorded as a P5.10 deviation. Reading before the group tail answers null — the
    /// <see cref="StoreAsImage"/> projection then answers false, the flag's early value.)</summary>
    public Model.StorageForm? Storage { get; internal set; }

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
        // A DYNAMIC-capacity table is out-of-line (non-contiguous, variable size — §8.5.1.9.1) so it has no static
        // character image; a group CONTAINING one drops out via Children.All below (the Tier-C island, D9).
        !IsDynamicTable && (
        IsElementary
            // National and boolean leaves are string-stored (D-N1/D-B1) and contribute their CHARACTER
            // positions to a group image (ImageWidth = Length — never byte-doubled for national; a byte
            // width, if ever needed, is a NEW member, not this one).
            ? Pic?.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited
                or PicCategory.National or PicCategory.Boolean || StoreAsImage
            : IsGroup && Children.All(c => c.IsCharacterImage));

    /// <summary>
    /// True if this item participates in the generated record-image codec (<c>AsImage()</c>/<c>FromImage()</c>,
    /// COBOLNET_DESIGN §14.4): every <see cref="IsCharacterImage"/> item, PLUS any fixed-point numeric leaf —
    /// DISPLAY (native <c>long</c>/<c>Int128</c>, its image is its zoned form) and BINARY/PACKED, whose character
    /// image is the implementor-defined zoned digit image with a trailing-overpunch sign (ISO/IEC 1989:2023
    /// §13.18.60 USAGE GR4 leaves the representation, including the sign, to the implementor; see
    /// <see cref="PicInfo.ImageSignKind"/>). Excluded — kept loud (§1.4): COMP-1/COMP-2 floats (no fixed decimal
    /// width) and COMP-5 (its <c>BinaryCapacity</c> discipline stores values EXCEEDING the PICTURE digit count —
    /// a Digits-wide image cannot carry them) and INDEX items (no character image at all, §13.18.60). A group is
    /// image-capable when every child is. Width-wise the codec reuses <see cref="ImageWidth"/> unchanged: a
    /// binary/packed leaf's image is exactly <c>Pic.Digits</c> characters (the SIGN SEPARATE add is DISPLAY-only,
    /// §13.18.52 SR2 — a binary item never carries a separate sign).
    /// </summary>
    public bool IsImageCapable =>
        !IsDynamicTable && (   // out-of-line dynamic table — not in the static record codec (D9)
        IsElementary
            // P5.7: the leaf arm is defined DIRECTLY on Pic (a pure declared-shape fact, phase-stable at every
            // point of the pipeline — resolve, procedure bind, emit). Value-identical to the former
            // IsCharacterImage delegation: every image-PROMOTED leaf is a fixed-point Display/Binary/Packed
            // numeric, already true via the numeric arm — the promotion can never change this property.
            ? Pic?.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited
                or PicCategory.National or PicCategory.Boolean
                || Pic is { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display or Usage.Binary or Usage.Packed }
            : IsGroup && Children.All(c => c.IsImageCapable));

    /// <summary>The character width of this item's image — meaningful for an <see cref="IsCharacterImage"/> item. For a
    /// group it is the sum of each child's TOTAL image contribution, i.e. the child's per-occurrence image width × its
    /// fixed-OCCURS count (every OCCURS position is part of the group image, ISO §14.9). A numeric-DISPLAY leaf's image
    /// is its digit count plus a separate-sign character when SIGN IS SEPARATE (ISO §13.18.52); an over-punched sign
    /// occupies no extra position. (This is the per-occurrence width of THIS item; a parent multiplies by THIS item's
    /// own OCCURS count.)</summary>
    public int ImageWidth =>
        // A REDEFINING child occupies NO new storage (ISO §13.18.44 — it overlays its target), so a group's size
        // sums only the non-redefining subordinates (NC252A: REDEF10 is 46 chars, not 46 + its RDF3 overlay).
        IsElementary ? ElementaryImageWidth
        : Children.Where(c => c.RedefinesTargetName is null).Sum(c => c.ImageWidth * (c.Occurs ?? 1));

    /// <summary>The character-image width of an elementary item (digit count + a separate-sign position when present
    /// for a signed numeric; otherwise the PICTURE's character length). A pure DECLARED-shape fact (reads only
    /// <see cref="Pic"/>) — internal so <see cref="Model.RecordLayout"/>'s leaf arm is PHASE-FREE (P5 Step 8:
    /// callable at bind time, before <see cref="Storage"/> exists; the Storage width was built FROM this value and
    /// identity #3 proved them equal corpus-wide).</summary>
    internal int ElementaryImageWidth
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

    /// <summary>The C# type name for this item's field — <c>CobolDynTable&lt;T&gt;</c> for a DYNAMIC table (D9), an
    /// array of <see cref="ElementType"/> for a fixed OCCURS table, else the scalar <see cref="ElementType"/>.</summary>
    public string FieldType =>
        IsDynamicTable ? $"CobolDynTable<{ElementType}>"
        : Occurs is not null ? ElementType + "[]"
        : ElementType;
    // (The ClrType back-compat alias is DELETED — P5.10: zero readers, grep-proven by the topology audit.)

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
