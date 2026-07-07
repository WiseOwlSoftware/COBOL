// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;

namespace CobolNet.CodeGen.Emit;

/// <summary>
/// Emits the DATA DIVISION as typed-native C# (COBOLNET_DESIGN §3.2): a nested <c>record struct</c> type per group,
/// a static numeric <c>NumProfile</c> per numeric leaf, the INDEXED BY index fields, and one static field per
/// top-level (01/77) item — a group/table initialized with a composed initializer, an elementary item with its
/// VALUE-or-default. A COBOL record IS a .NET record; there is no byte substrate and no flattening.
/// </summary>
/// <remarks>The per-item physical-field list is <b>memoized</b> (<see cref="PhysicalChildrenOf"/>): every consumer
/// (field declarations, the AsImage/FromImage methods, a parent's width and composed initializer) reuses one computed
/// list per node, so a deeply-nested group (a CCVS test nests ~49 levels) is O(total items), not the O(2^depth) the
/// width-and-init-recompute-each-other recursion would otherwise cost.</remarks>
internal sealed class FieldEmitter(EmissionContext ctx)
{
    /// <summary>Memoized physical-field list per group item — the cache that turns the otherwise-exponential
    /// nested-group emission (width and init recursively recomputing each other) into linear time. The root forest
    /// is cached separately in <see cref="_rootPhysCache"/>.</summary>
    private readonly Dictionary<DataItem, IReadOnlyList<Physical>> _physCache = [];
    private IReadOnlyList<Physical>? _rootPhysCache;

    /// <summary>Emit every WORKING-STORAGE / FILE-SECTION type, profile, index field, and root field.</summary>
    public void Emit()
    {
        var w = ctx.Writer;
        foreach (var root in ctx.Data.Roots) EmitStructTypeDecls(root, w);
        foreach (var root in ctx.Data.Roots) EmitProfiles(root, w);
        // Programs are instantiable classes (interprogram design D3): root + index fields are INSTANCE fields —
        // a fresh instance IS the §14.6.2.3.2 initial state; the registry's cached singleton IS last-used state.
        // The suppression filter skips members another mechanism provides: a carrier-resident LINKAGE formal
        // (its field is `__lnkpN.Value` — caller storage, ISO §13.7.1) and an inherited GLOBAL table's index
        // field (a ref-bridge to the container, §13.18.27 GR2).
        foreach (var (name, field) in ctx.Data.IndexFields)
            if (!ctx.Data.CallSuppressedRootFields.Contains(field))
                w.Line($"private long {field} = 1;   // INDEX-NAME {name}");
        // A method WORKING-STORAGE table's index cell is a class STATIC (persistent across activations, §11.7;
        // M2-OO-1h step 4). LOCAL/LINKAGE table cells are per-activation method locals, emitted in OoEmitMethod.
        foreach (var cell in ctx.Data.OoStaticIndexCells)
            w.Line($"private static long {cell} = 1;   // method-WS INDEX-NAME cell (M2-OO-1h)");
        foreach (var f in RootPhysicals())
            if (!ctx.Data.CallSuppressedRootFields.Contains(f.Name))
                // A method-WS root is a STATIC field (OO deep-dive D3 — one copy per class, shared across
                // instances, persistent across activations, ISO §11.7; pre-2023 editions only, §13.5.3 SR 1).
                w.Line($"private {(ctx.Data.OoStaticRootFields.Contains(f.Name) ? "static " : "")}{f.Type} {f.Name} = {f.Init};   // {f.Comment}");
    }

    /// <summary>The C# (type, initializer) pair for declaring one root as a METHOD LOCAL (the OO slice-2
    /// LINKAGE/LOCAL-STORAGE mapping) — the same composed initializer a field declaration gets, so a group
    /// local's OCCURS arrays and VALUE seeds are identical to field semantics (§14.5.3: LOCAL-STORAGE
    /// re-initializes on every activation — a C# local declaration does exactly that).</summary>
    internal (string Type, string Init) RootDecl(DataItem item) => (item.FieldType, FieldInit(item));

    /// <summary>The (name, init) of a method-scoped Tier-B REDEFINES class's ONE string backing when
    /// <paramref name="root"/> is that class's canonical (M2-OO-1h step 3). The class-level field loop suppresses
    /// this backing (it is method-scoped), so <c>OoEmitMethod</c> emits it as a method LOCAL of type
    /// <c>string</c> — the members are windows over it (via <see cref="RedefViewPlace"/>). Null otherwise.</summary>
    internal (string Name, string Init)? MethodRedefinesBackingDecl(DataItem root) =>
        root.Class is { Tier: RedefinesTier.StringCanonical } cls && ReferenceEquals(cls.Canonical, root)
            ? (cls.BackingCsName, $"CobolString.Store({ImageInitOf(root)}, {cls.Width})")
            : null;

    /// <summary>A field that physically appears in the emitted C# — an item's own field, OR a REDEFINES class's single
    /// string backing (which replaces ALL the class's members). A REDEFINES <i>view</i> yields no physical field
    /// (ISO §13.18.44; COBOLNET_DESIGN §4.1 — never two stored fields per storage area).</summary>
    /// <summary>One emitted struct field. <paramref name="Width"/> is the field's TOTAL contribution to its group's
    /// character image — element-image-width × <paramref name="Occurs"/> for a fixed-OCCURS table, else the item's own
    /// image width. <paramref name="Occurs"/> is the fixed occurrence count (0 = not a table), so the image facility
    /// knows to concat/distribute across the array's elements. <paramref name="NumLeaf"/> is the source leaf when the
    /// field stores a NATIVE fixed-point numeric (<c>long</c>/<c>Int128</c>; DISPLAY, BINARY, or PACKED usage) — the
    /// image facility then encodes/decodes it through <c>CobolNum.FormatDisplay</c>/<c>ParseDisplay</c> with the
    /// leaf's IMAGE profile (<see cref="ImageProfileOf"/>); null for every string-shaped field (alphanumeric, edited,
    /// <see cref="DataItem.StoreAsImage"/>, a Tier-B class backing) and for nested group structs.</summary>
    private readonly record struct Physical(string Name, string Type, int Width, bool IsGroupStruct, string Init, string Comment, int Occurs = 0, DataItem? NumLeaf = null);

    /// <summary>The memoized physical fields of a group's children (the root forest under the sentinel).</summary>
    private IReadOnlyList<Physical> PhysicalChildrenOf(DataItem owner)
    {
        if (_physCache.TryGetValue(owner, out var cached)) return cached;
        var list = BuildPhysicals(owner.Children).ToList();
        _physCache[owner] = list;
        return list;
    }

    /// <summary>The memoized physical fields of the top-level (01/77) forest.</summary>
    private IReadOnlyList<Physical> RootPhysicals() => _rootPhysCache ??= BuildPhysicals(ctx.Data.Roots).ToList();

    /// <summary>The physical fields a run of sibling items emits: skip REDEFINES views; substitute a Tier-B class's ONE
    /// string backing (emitted once, at the canonical) for the whole class; a Tier-A view forwards to its canonical's
    /// field. The class's numeric <c>NumProfile</c>s are still emitted elsewhere (EmitProfiles — D9).</summary>
    private IEnumerable<Physical> BuildPhysicals(IEnumerable<DataItem> items)
    {
        foreach (var c in items)
        {
            if (!(c.IsGroup || c.IsElementary)) continue;
            if (c.Class is { Tier: RedefinesTier.StringCanonical } cls)
            {
                // The whole redefines class is ONE string backing (the canonical's VALUE seeds it, SR9); every member
                // is a window over it. A non-canonical Tier-B member yields no field.
                if (c.IsCanonical)
                    yield return new Physical(cls.BackingCsName, "string", cls.Width, false,
                        $"CobolString.Store({ImageInitOf(c)}, {cls.Width})", $"REDEFINES backing for {c.CobolName}");
                continue;
            }
            if (c.Class is { Tier: RedefinesTier.Alias } && !c.IsCanonical)
                continue;   // a Tier-A view forwards to the canonical's stored field
            string comment = c.CobolName is { } n ? $"{n}{(c.Occurs is { } o ? $" OCCURS {o}" : "")}" : "FILLER";
            // For a group child, use its PHYSICAL image width (skips its own redefines views, counts a contained
            // backing once) — the raw DataItem.ImageWidth over-counts a group that contains a redefines class. A fixed
            // OCCURS table contributes its per-occurrence image width × the count to the group image (ISO §14.9).
            int elemWidth = c.IsGroup ? PhysicalImageWidth(c) : c.ImageWidth;
            int occurs = c.Occurs ?? 0;
            int width = occurs > 0 ? elemWidth * occurs : elemWidth;
            // A NATIVE fixed-point numeric field (the ElementType test excludes the string-stored shapes —
            // StoreAsImage leaves, edited items — and floats): its slice of the group image is the zoned digit
            // form, encoded/decoded by the image methods through CobolNum (COBOLNET_DESIGN §14.4). COMP-5 and
            // INDEX never qualify (excluded by the usage filter; see DataItem.IsImageCapable).
            DataItem? numLeaf = !c.IsGroup && c.ElementType is "long" or "Int128"
                && c.Pic is { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display or Usage.Binary or Usage.Packed }
                ? c : null;
            yield return new Physical(c.CsName, c.FieldType, width, c.IsGroup, FieldInit(c), comment, occurs, numLeaf);
        }
    }

    /// <summary>The emitted character-image width of an item: a leaf's own width; a group's = the sum of its physical
    /// fields (a contained REDEFINES class contributes its single backing width once, its views nothing).</summary>
    private int PhysicalImageWidth(DataItem item) =>
        item.IsGroup ? PhysicalChildrenOf(item).Sum(f => f.Width) : item.ImageWidth;

    /// <summary>The C# string-expression for an item's INITIAL character image (used to seed a Tier-B backing from
    /// the canonical's VALUE): a group concatenates its leaves' images; an elementary item formats its VALUE (numeric
    /// → <c>CobolNum.FormatDisplay</c>; alphanumeric/edited → the stored string; figurative/default per width). A
    /// fixed-OCCURS entry's image repeats <c>Occurs</c> times — every occurrence takes the VALUE (ISO §13.18.63 GR9;
    /// the recursion runs through this wrapper, so nested OCCURS repeat too).</summary>
    internal string ImageInitOf(DataItem item)
    {
        string one = ImageInitOfOne(item);
        return item.Occurs is { } n and > 1 ? $"CobolString.Repeat({one}, {n})" : one;
    }

    private string ImageInitOfOne(DataItem item)
    {
        if (item.IsGroup)
        {
            // Redefining children overlay storage already composed by their targets — never part of the image.
            var parts = item.Children.Where(c => (c.IsGroup || c.IsElementary) && c.RedefinesTargetName is null)
                .Select(ImageInitOf);
            return item.Children.Count > 0 ? "(" + string.Join(" + ", parts) + ")" : "\"\"";
        }
        var pic = item.Pic!;
        if (item.RawValue is { } raw)
        {
            if (FigurativeInitializer(raw, pic) is { } fig && pic.Category is not PicCategory.Numeric) return fig;
            // CCVS leniency (same as InitializerFor): an ALPHANUMERIC literal VALUE on a numeric DISPLAY item
            // contributes its CHARACTERS to the image (NC107A's `PIC 999 VALUE "000"` under a REDEFINES).
            if (pic.Category is PicCategory.Numeric && !pic.IsFloat && raw.StartsWith('"'))
                return $"CobolString.Store({EmitText.CsLiteral(EmitText.DecodeCobolString(raw))}, {pic.Length})";
            if (pic.Category is PicCategory.Numeric && !pic.IsFloat && FigurativeInitializer(raw, pic) is null)
                return $"CobolNum.FormatDisplay({EmitText.UnscaledAtScale(raw, pic.Scale)}, {item.ProfileName})";
            // A NUMERIC literal VALUE on a numeric-edited member contributes its EDITED image (§13.18.63 GR6).
            if (pic.Category is PicCategory.NumericEdited && !raw.StartsWith('"')
                && TryParseNumeric(raw, out var uv, out int sc))
                return EmitText.CsLiteral(CobolNet.Runtime.CobolEdit.Format(uv, sc, pic.EditMask!,
                    item.BlankWhenZero, ctx.Data.CurrencyPicSymbol, ctx.Data.DecimalPointIsComma));
            if (pic.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited)
                return $"CobolString.Store({EmitText.CsLiteral(EmitText.DecodeCobolString(raw))}, {pic.Length})";
            // Boolean members of a Tier-B class contribute their zero-padded VALUE image (national never
            // reaches a Tier-B backing — ComputeTier rejects the class; the arm is defensive).
            if (pic.Category is PicCategory.Boolean or PicCategory.National)
                return $"CobolString.Store({EmitText.CsLiteral(EmitText.DecodeCobolString(raw))}, {pic.Length}"
                    + $"{(pic.Category is PicCategory.Boolean ? ", justifiedRight: false, pad: '0'" : "")})";
        }
        return pic.Category is PicCategory.Numeric && !pic.IsFloat
            ? $"CobolNum.FormatDisplay(0L, {item.ProfileName})"
            : pic.Category is PicCategory.Boolean
                ? $"new string('0', {pic.Length})"   // boolean initial state — zeros (§13.18.63)
                : $"new string(' ', {pic.Length})";
    }

    private void EmitStructTypeDecls(DataItem item, CodeWriter w)
    {
        if (!item.IsGroup) return;
        foreach (var child in item.Children) EmitStructTypeDecls(child, w);   // nested types first (any order is fine)
        using (w.Block($"private record struct {item.StructName}"))
        {
            foreach (var f in PhysicalChildrenOf(item))
                w.Line($"public {f.Type} {f.Name};   // {f.Comment}");

            // An image-capable group gets the whole-group image facility (COBOLNET_DESIGN §14.4): AsImage
            // concatenates the leaves' character images — a string-stored leaf its characters, a NATIVE
            // fixed-point leaf (DISPLAY/BINARY/PACKED) its zoned digit image (trailing-overpunch sign, the
            // §13.18.60 USAGE GR4 implementor representation) — and FromImage distributes a character image back
            // into them. Used by whole-group MOVE / DISPLAY / compare / WRITE / RELEASE and the READ / RETURN
            // record-area distribution; the SD/FD record codec IS this pair (§8.2). Only a group with a float /
            // COMP-5 / INDEX leaf stays the loud Tier-C island (DataItem.IsImageCapable).
            if (item.IsImageCapable) EmitImageMethods(item, w);
        }
    }

    private void EmitImageMethods(DataItem group, CodeWriter w)
    {
        var members = PhysicalChildrenOf(group);
        w.Line($"public readonly string AsImage() => {(members.Count > 0 ? string.Join(" + ", members.Select(AsImageOf)) : "\"\"")};");
        using (w.Block("public void FromImage(string __s)"))
        {
            w.Line($"__s = CobolString.Store(__s, {members.Sum(f => f.Width)});");   // pad/truncate to the image width
            int off = 0;
            foreach (var f in members)
            {
                EmitMemberFromImage(f, off, w);
                off += f.Width;
            }
        }
    }

    /// <summary>One member's AsImage sub-expression: a scalar string field directly, a nested group's
    /// <c>AsImage()</c>, a NATIVE fixed-point leaf its zoned digit image (<c>CobolNum.FormatDisplay</c> with the
    /// leaf's image profile — fixed <c>Pic.Digits</c> width, trailing-overpunch sign for binary/packed), or — for a
    /// fixed-OCCURS table — the concatenation of every occurrence's image (ISO §14.9: a group move treats the whole
    /// group, INCLUDING every OCCURS position, as one alphanumeric item).</summary>
    private static string AsImageOf(Physical f) =>
        f.Occurs == 0
            ? (f.IsGroupStruct ? $"{f.Name}.AsImage()"
               : f.NumLeaf is { } leaf ? $"CobolNum.FormatDisplay({f.Name}, {ImageProfileOf(leaf)})"
               : f.Name)
        : f.IsGroupStruct ? $"string.Concat(System.Array.ConvertAll({f.Name}, __e => __e.AsImage()))"
        : f.NumLeaf is { } l ? $"string.Concat(System.Array.ConvertAll({f.Name}, __e => CobolNum.FormatDisplay(__e, {ImageProfileOf(l)})))"
        : $"string.Concat({f.Name})";

    /// <summary>Distribute the slice of the image at <paramref name="off"/> into one member: a scalar string field
    /// gets its substring; a nested group gets <c>FromImage</c>; a NATIVE fixed-point leaf decodes its zoned slice
    /// (<c>CobolNum.ParseDisplay</c> with the image profile, cast to the leaf's CLR storage type — non-digit
    /// positions, e.g. the spaces a short record's pad legitimately deposits, decode deterministically per ISO
    /// §14.6.13.2, see CobolNum); a fixed-OCCURS table loops its occurrences, each taking its per-occurrence width
    /// in source order (the array elements are value-type structs/strings, mutated in place).</summary>
    private static void EmitMemberFromImage(Physical f, int off, CodeWriter w)
    {
        if (f.Occurs == 0)
        {
            w.Line(f.IsGroupStruct
                ? $"{f.Name}.FromImage(__s.Substring({off}, {f.Width}));"
                : f.NumLeaf is { } leaf
                ? $"{f.Name} = ({leaf.Pic!.ClrType})CobolNum.ParseDisplay(__s.Substring({off}, {f.Width}), {ImageProfileOf(leaf)});"
                : $"{f.Name} = __s.Substring({off}, {f.Width});");
            return;
        }
        int elem = f.Width / f.Occurs;   // per-occurrence width (Width = elem × Occurs, exact by construction)
        using (w.Block($"for (int __i = 0; __i < {f.Occurs}; __i++)"))
            w.Line(f.IsGroupStruct
                ? $"{f.Name}[__i].FromImage(__s.Substring({off} + __i * {elem}, {elem}));"
                : f.NumLeaf is { } l
                ? $"{f.Name}[__i] = ({l.Pic!.ClrType})CobolNum.ParseDisplay(__s.Substring({off} + __i * {elem}, {elem}), {ImageProfileOf(l)});"
                : $"{f.Name}[__i] = __s.Substring({off} + __i * {elem}, {elem});");
    }

    /// <summary>The C# <c>NumProfile</c> expression a native fixed-point leaf's IMAGE encodes/decodes with: the
    /// leaf's own <c>_P_</c> profile when its stored sign form IS its image form (every DISPLAY leaf), else the
    /// profile with the sign overridden to the image convention (a signed BINARY/PACKED leaf: its stored profile
    /// says <c>BinaryMinus</c> — a VARIABLE-width DISPLAY-statement form no fixed record window can carry — so its
    /// image carries a trailing overpunch instead, <see cref="PicInfo.ImageSignKind"/>; ISO §13.18.60 USAGE GR4
    /// makes the representation, including the sign, implementor-defined). The leaf's own profile is UNTOUCHED —
    /// DISPLAY-statement output (a leading minus, locked golden behavior) still formats through <c>_P_</c>.
    /// <c>NumProfile</c> is a readonly record struct, so the <c>with</c> copy is cheap and allocation-free.</summary>
    private static string ImageProfileOf(DataItem leaf)
    {
        var pic = leaf.Pic!;
        return pic.ImageSignKind == pic.SignKind
            ? leaf.ProfileName
            : $"({leaf.ProfileName} with {{ SignKind = NumericSign.{pic.ImageSignKind} }})";
    }

    private static void EmitProfiles(DataItem item, CodeWriter w)
    {
        if (item.IsElementary && item.Pic is { Category: PicCategory.Numeric, IsFloat: false })
            // INTERNAL (not private): an INVOKE BY CONTENT conversion composes the FORMAL's value/image at
            // the CALL SITE, qualifying the profile by its owner class ({OWNER}._P_n) — same assembly, one
            // generated file (the OO slice-2 review's cross-class-profile rule).
            w.Line($"internal static readonly NumProfile {item.ProfileName} = {item.Pic.ProfileInitializer};");
        foreach (var child in item.Children) EmitProfiles(child, w);
    }

    /// <summary>The C# initializer for a field: an array literal for an OCCURS table (every element initialized so
    /// none is left at <c>default</c>), a composed object-initializer for a group, else the elementary VALUE.</summary>
    private string FieldInit(DataItem item)
    {
        // A DYNAMIC-capacity table (§13.18.38 Format 4, D9): an out-of-line CobolDynTable seeded per occurrence with
        // the SAME one-occurrence initializer the fixed path repeats (heed DEVLOG 643 — seed EVERY occurrence). Opens
        // at FROM (min); TO is the expected capacity; INITIALIZED is carried for the (always-on) new-occurrence seed.
        if (item.IsDynamicTable)
        {
            string seed = item.IsGroup ? ComposedInit(item) : InitializerFor(item);
            var s = item.OccursSpec!;
            return $"new CobolDynTable<{item.ElementType}>(() => {seed}, {s.InitialCap ?? 0}, "
                 + $"{(s.ExpectedMax is int e ? e.ToString() : "null")}, {(s.Initialized ? "true" : "false")})";
        }
        if (item.Occurs is { } n)
        {
            string element = item.IsGroup ? ComposedInit(item) : InitializerFor(item);
            return $"new {item.ElementType}[] {{ {string.Join(", ", Enumerable.Repeat(element, n))} }}";
        }
        return item.IsGroup ? ComposedInit(item) : InitializerFor(item);
    }

    private string ComposedInit(DataItem group)
    {
        // A GROUP-level VALUE initializes the whole area as ONE alphanumeric value (ISO §13.18.63): the decoded
        // literal — space-padded / right-truncated to the group's image width — distributes over the subordinate
        // items POSITIONALLY at compile time (NC104A `01 MOVE29A VALUE "$123.45". 02 MOVE30 PIC $999.99.`).
        // Distribution requires every leaf string-stored and no shared-storage member in the subtree; anything
        // else keeps the member-wise default (the leaf's own VALUE/default).
        if (group.RawValue is { } graw && GroupValueText(graw, group) is { } text && DistributableSubtree(group))
        {
            string padded = text.Length >= group.ImageWidth ? text[..group.ImageWidth] : text.PadRight(group.ImageWidth);
            return SliceInit(group, padded);
        }
        var parts = PhysicalChildrenOf(group).Select(f => $"{f.Name} = {f.Init}");
        return $"new {group.StructName} {{ {string.Join(", ", parts)} }}";
    }

    /// <summary>The character text of a group VALUE operand: a quoted literal decoded, or <c>ALL "lit"</c>
    /// repeated to the group width (§8.3.3.6.4 GR2); null for any other operand form.</summary>
    private static string? GroupValueText(string raw, DataItem group) =>
        EmitText.AllLiteralText(raw) is { } al ? EmitText.RepeatToWidth(al, group.ImageWidth)
        : raw.Length >= 2 && raw[0] == '"' && raw[^1] == '"' ? EmitText.DecodeCobolString(raw)
        : null;

    private static bool DistributableSubtree(DataItem item) =>
        item.Class is null && (item.IsGroup
            ? item.Children.All(DistributableSubtree)
            // A string-stored leaf takes its slice verbatim; a NATIVE numeric USAGE-DISPLAY leaf decodes its
            // positional slice (the group VALUE initializes the area "without consideration for the individual
            // elementary items", ISO §13.18.63 — the slice IS the leaf's zoned image; the IF-suite shape
            // `01 ARR VALUE "40537". 02 IND OCCURS 5 PIC 9.`). Binary/packed/float leaves stay undistributable
            // (their character image is the Tier-C byte boundary) and keep the member-wise default.
            : item.StoreAsImage || item.Pic?.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited
                or PicCategory.National or PicCategory.Boolean   // string-stored — the slice is the chars (D-N4 identity)
              || item.Pic is { Category: PicCategory.Numeric, Usage: Usage.Display, IsFloat: false });

    /// <summary>Build the composed initializer of <paramref name="item"/> from its positional <paramref name="slice"/>
    /// of the group VALUE text — each subordinate (and each OCCURS occurrence) takes its own window.</summary>
    private static string SliceInit(DataItem item, string slice)
    {
        // A native (long-stored) numeric-DISPLAY leaf decodes its zoned slice to the unscaled value (sign-aware
        // overpunch/separate decode — the same ParseDisplay every image read uses). String-stored leaves
        // (alphanumeric / edited / StoreAsImage) keep the characters.
        if (!item.IsGroup && !item.StoreAsImage
            && item.Pic is { Category: PicCategory.Numeric, Usage: Usage.Display, IsFloat: false })
            return $"({item.ElementType})CobolNum.ParseDisplay({EmitText.CsLiteral(slice)}, {item.ProfileName})";
        if (!item.IsGroup) return EmitText.CsLiteral(slice);
        var parts = new List<string>();
        int off = 0;
        foreach (var c in item.Children)
        {
            int w = c.ImageWidth;
            if (c.Occurs is { } n)
            {
                var elems = new List<string>();
                for (int k = 0; k < n; k++) elems.Add(SliceInit(c, slice.Substring(off + k * w, w)));
                parts.Add($"{c.CsName} = new {c.ElementType}[] {{ {string.Join(", ", elems)} }}");
                off += w * n;
            }
            else
            {
                parts.Add($"{c.CsName} = {SliceInit(c, slice.Substring(off, w))}");
                off += w;
            }
        }
        return $"new {item.StructName} {{ {string.Join(", ", parts)} }}";
    }

    /// <summary>The C# initializer expression for an elementary item, from its VALUE clause or the COBOL default.</summary>
    private string InitializerFor(DataItem item)
    {
        var pic = item.Pic!;

        // CCVS leniency: an ALPHANUMERIC literal VALUE on a numeric DISPLAY item stores its CHARACTERS as the
        // item's content (ISO §13.18.63 SR2 wants a numeric literal; the 85 corpus writes `PIC 999 VALUE "000"`
        // — NC107A's DATA-P — and the legacy oracle accepts the character form). Strict rejection is a future
        // EditionValidator row.
        if (item.RawValue is { } q && q.StartsWith('"') && pic.Category is PicCategory.Numeric && !pic.IsFloat)
            return item.StoreAsImage
                ? $"CobolString.Store({EmitText.CsLiteral(EmitText.DecodeCobolString(q))}, {pic.Length})"
                : EmitText.UnscaledAtScale(EmitText.DecodeCobolString(q), pic.Scale);

        // A numeric-DISPLAY leaf stored as its character image (whole-group-aliased): initialize to the formatted
        // image of its unscaled VALUE (a numeric/figurative VALUE → that value; no VALUE → 0). The _P_ profile is
        // declared textually earlier (EmitProfiles runs first), so it is initialized before this use.
        if (item.StoreAsImage)
        {
            string unscaled = item.RawValue is { } rv && FigurativeInitializer(rv, pic) is null
                ? EmitText.UnscaledAtScale(rv, pic.Scale)
                : "0L";
            return $"CobolNum.FormatDisplay({unscaled}, {item.ProfileName})";
        }

        if (item.RawValue is not { } raw) return pic.DefaultInitializer;

        // Figurative constants (ZERO / SPACE / HIGH-VALUE / LOW-VALUE / QUOTE / NULL) fill the item to its width.
        if (FigurativeInitializer(raw, pic) is { } fig) return fig;

        // ALL "literal": the literal repeated to the item width (ISO §8.3.3.6.4 GR2; SR3 forbids it on a numeric item).
        if (EmitText.AllLiteralText(raw) is { } allLit && pic.Category is not PicCategory.Numeric)
            return EmitText.CsLiteral(EmitText.RepeatToWidth(allLit, pic.Length));

        return pic.Category switch
        {
            // A NUMERIC literal VALUE on a numeric-edited item converts per the MOVE editing rules
            // (ISO §13.18.63 GR6) — the edited image is a compile-time constant, baked here. (An alphanumeric
            // literal stores verbatim — NOTE 3: the programmer supplies the edited form.)
            PicCategory.NumericEdited when !raw.StartsWith('"') && TryParseNumeric(raw, out var uv, out int sc) =>
                EmitText.CsLiteral(CobolNet.Runtime.CobolEdit.Format(uv, sc, pic.EditMask!, item.BlankWhenZero,
                    ctx.Data.CurrencyPicSymbol, ctx.Data.DecimalPointIsComma)),
            // National VALUE stores like alphanumeric on the char substrate (§13.18.63 SR5 — the N"…" literal,
            // already prefix-stripped by DecodeCobolString); boolean VALUE zero-pads (SR10; §14.6.8.6).
            PicCategory.Alphanumeric or PicCategory.NumericEdited or PicCategory.National =>
                $"CobolString.Store({EmitText.CsLiteral(EmitText.DecodeCobolString(raw))}, {pic.Length})",
            PicCategory.Boolean =>
                $"CobolString.Store({EmitText.CsLiteral(EmitText.DecodeCobolString(raw))}, {pic.Length}, justifiedRight: false, pad: '0')",
            PicCategory.Numeric when pic.IsFloat => RawValueAsFloat(raw, pic),
            PicCategory.Numeric => EmitText.UnscaledAtScale(raw, pic.Scale),
            _ => pic.DefaultInitializer,
        };
    }

    /// <summary>Parse a canonical (dot-decimal) numeric VALUE text to its unscaled value + scale, for
    /// compile-time editing. False for any non-numeric shape (the caller falls back to verbatim store).</summary>
    private static bool TryParseNumeric(string text, out Int128 unscaled, out int scale)
    {
        unscaled = 0;
        scale = 0;
        string t = text.Trim();
        bool neg = t.StartsWith('-');
        if (neg || t.StartsWith('+')) t = t[1..];
        int dot = t.IndexOf('.');
        string digits = dot < 0 ? t : t.Remove(dot, 1);
        if (digits.Length == 0 || !digits.All(char.IsAsciiDigit)) return false;
        scale = dot < 0 ? 0 : t.Length - dot - 1;
        foreach (char c in digits) unscaled = unscaled * 10 + (c - '0');
        if (neg) unscaled = -unscaled;
        return true;
    }

    /// <summary>If <paramref name="raw"/> is a figurative constant, its C# initializer given the receiver's category
    /// and width; otherwise null (ISO §8.3.1.2; HIGH/LOW = U+00FF/U+0000 per COBOLNET_DESIGN §14.9).</summary>
    private string? FigurativeInitializer(string raw, PicInfo pic)
    {
        string key = raw.ToUpperInvariant();
        // ALL <figurative-word> (e.g. ALL ZEROS, ALL SPACES) is equivalent to the bare figurative (a single-character
        // figurative repeated to the width); strip the ALL prefix when the remainder is a figurative WORD. (ALL "literal"
        // — repeating a multi-character literal — is a separate form left to the literal path.)
        if (FillCharFor(key, pic.Category) is null && key.StartsWith("ALL") && key.Length > 3
            && FillCharFor(key[3..], pic.Category) is not null)
            key = key[3..];
        if (FillCharFor(key, pic.Category) is not { } fillChar) return null;
        return pic.Category is PicCategory.Numeric ? pic.DefaultInitializer : $"new string({fillChar}, {pic.Length})";
    }

    /// <summary>The C# <c>char</c>-literal a figurative-constant word fills with, or null if the text is not a
    /// figurative word (ISO §8.3.1.2; HIGH/LOW = U+00FF/U+0000 per COBOLNET_DESIGN §14.9). The alphanumeric
    /// program collating sequence governs HIGH/LOW-VALUE only for alphanumeric receivers — a national/boolean
    /// item uses the D-N3 pin (its own sequence; §8.3.3.6 GR6/GR7).</summary>
    private string? FillCharFor(string word, PicCategory cat) => word switch
    {
        "ZERO" or "ZEROS" or "ZEROES" => "'0'",
        "SPACE" or "SPACES" => "' '",
        // VALUE HIGH-/LOW-VALUE under a PROGRAM COLLATING SEQUENCE is the sequence's extreme character
        // (ISO §8.3.3.6 GR7 — compile-time figurative; NC219A's NEW-LOW PIC X VALUE LOW-VALUE = 'F').
        "HIGH-VALUE" or "HIGH-VALUES" => ctx.FigFill('H', cat),
        "LOW-VALUE" or "LOW-VALUES" or "NULL" or "NULLS" => ctx.FigFill('L', cat),
        "QUOTE" or "QUOTES" => "'\\\"'",
        _ => null,
    };

    /// <summary>A numeric VALUE literal as a C# float/double literal for a COMP-1/COMP-2 item.</summary>
    private static string RawValueAsFloat(string raw, PicInfo pic) =>
        pic.IsSingle ? $"{raw.Trim().TrimStart('+')}f" : $"{raw.Trim().TrimStart('+')}d";   // COMP-1/FLOAT-SHORT → float literal, else double
}
