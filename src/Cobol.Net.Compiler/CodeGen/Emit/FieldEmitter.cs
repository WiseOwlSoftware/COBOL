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
        foreach (var (name, field) in ctx.Data.IndexFields)
            w.Line($"private static long {field} = 1;   // INDEX-NAME {name}");
        foreach (var f in RootPhysicals())
            w.Line($"private static {f.Type} {f.Name} = {f.Init};   // {f.Comment}");
    }

    /// <summary>A field that physically appears in the emitted C# — an item's own field, OR a REDEFINES class's single
    /// string backing (which replaces ALL the class's members). A REDEFINES <i>view</i> yields no physical field
    /// (ISO §13.18.44; COBOLNET_DESIGN §4.1 — never two stored fields per storage area).</summary>
    /// <summary>One emitted struct field. <paramref name="Width"/> is the field's TOTAL contribution to its group's
    /// character image — element-image-width × <paramref name="Occurs"/> for a fixed-OCCURS table, else the item's own
    /// image width. <paramref name="Occurs"/> is the fixed occurrence count (0 = not a table), so the image facility
    /// knows to concat/distribute across the array's elements.</summary>
    private readonly record struct Physical(string Name, string Type, int Width, bool IsGroupStruct, string Init, string Comment, int Occurs = 0);

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
            yield return new Physical(c.CsName, c.FieldType, width, c.IsGroup, FieldInit(c), comment, occurs);
        }
    }

    /// <summary>The emitted character-image width of an item: a leaf's own width; a group's = the sum of its physical
    /// fields (a contained REDEFINES class contributes its single backing width once, its views nothing).</summary>
    private int PhysicalImageWidth(DataItem item) =>
        item.IsGroup ? PhysicalChildrenOf(item).Sum(f => f.Width) : item.ImageWidth;

    /// <summary>The C# string-expression for an item's INITIAL character image (used to seed a Tier-B backing from
    /// the canonical's VALUE): a group concatenates its leaves' images; an elementary item formats its VALUE (numeric
    /// → <c>CobolNum.FormatDisplay</c>; alphanumeric/edited → the stored string; figurative/default per width).</summary>
    private string ImageInitOf(DataItem item)
    {
        if (item.IsGroup)
        {
            var parts = item.Children.Where(c => c.IsGroup || c.IsElementary).Select(ImageInitOf);
            return item.Children.Count > 0 ? "(" + string.Join(" + ", parts) + ")" : "\"\"";
        }
        var pic = item.Pic!;
        if (item.RawValue is { } raw)
        {
            if (FigurativeInitializer(raw, pic) is { } fig && pic.Category is not PicCategory.Numeric) return fig;
            if (pic.Category is PicCategory.Numeric && !pic.IsFloat && FigurativeInitializer(raw, pic) is null)
                return $"CobolNum.FormatDisplay({EmitText.UnscaledAtScale(raw, pic.Scale)}, {item.ProfileName})";
            if (pic.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited)
                return $"CobolString.Store({EmitText.CsLiteral(EmitText.DecodeCobolString(raw))}, {pic.Length})";
        }
        return pic.Category is PicCategory.Numeric && !pic.IsFloat
            ? $"CobolNum.FormatDisplay(0L, {item.ProfileName})"
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

            // A pure-character (DISPLAY-homogeneous) group gets the whole-group image facility (COBOLNET_DESIGN
            // §14.4): AsImage concatenates the leaves' character images; FromImage distributes a character image
            // back into them. Used by whole-group MOVE / DISPLAY / compare. (Mixed-usage groups are the Tier-C
            // byte island — not emitted here.)
            if (item.IsCharacterImage) EmitImageMethods(item, w);
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
    /// <c>AsImage()</c>, or — for a fixed-OCCURS table — the concatenation of every occurrence's image (ISO §14.9: a
    /// group move treats the whole group, INCLUDING every OCCURS position, as one alphanumeric item).</summary>
    private static string AsImageOf(Physical f) =>
        f.Occurs == 0 ? (f.IsGroupStruct ? $"{f.Name}.AsImage()" : f.Name)
        : f.IsGroupStruct ? $"string.Concat(System.Array.ConvertAll({f.Name}, __e => __e.AsImage()))"
        : $"string.Concat({f.Name})";

    /// <summary>Distribute the slice of the image at <paramref name="off"/> into one member: a scalar field gets its
    /// substring; a nested group gets <c>FromImage</c>; a fixed-OCCURS table loops its occurrences, each taking its
    /// per-occurrence width in source order (the array elements are value-type structs/strings, mutated in place).</summary>
    private static void EmitMemberFromImage(Physical f, int off, CodeWriter w)
    {
        if (f.Occurs == 0)
        {
            w.Line(f.IsGroupStruct
                ? $"{f.Name}.FromImage(__s.Substring({off}, {f.Width}));"
                : $"{f.Name} = __s.Substring({off}, {f.Width});");
            return;
        }
        int elem = f.Width / f.Occurs;   // per-occurrence width (Width = elem × Occurs, exact by construction)
        using (w.Block($"for (int __i = 0; __i < {f.Occurs}; __i++)"))
            w.Line(f.IsGroupStruct
                ? $"{f.Name}[__i].FromImage(__s.Substring({off} + __i * {elem}, {elem}));"
                : $"{f.Name}[__i] = __s.Substring({off} + __i * {elem}, {elem});");
    }

    private static void EmitProfiles(DataItem item, CodeWriter w)
    {
        if (item.IsElementary && item.Pic is { Category: PicCategory.Numeric, IsFloat: false })
            w.Line($"private static readonly NumProfile {item.ProfileName} = {item.Pic.ProfileInitializer};");
        foreach (var child in item.Children) EmitProfiles(child, w);
    }

    /// <summary>The C# initializer for a field: an array literal for an OCCURS table (every element initialized so
    /// none is left at <c>default</c>), a composed object-initializer for a group, else the elementary VALUE.</summary>
    private string FieldInit(DataItem item)
    {
        if (item.Occurs is { } n)
        {
            string element = item.IsGroup ? ComposedInit(item) : InitializerFor(item);
            return $"new {item.ElementType}[] {{ {string.Join(", ", Enumerable.Repeat(element, n))} }}";
        }
        return item.IsGroup ? ComposedInit(item) : InitializerFor(item);
    }

    private string ComposedInit(DataItem group)
    {
        var parts = PhysicalChildrenOf(group).Select(f => $"{f.Name} = {f.Init}");
        return $"new {group.StructName} {{ {string.Join(", ", parts)} }}";
    }

    /// <summary>The C# initializer expression for an elementary item, from its VALUE clause or the COBOL default.</summary>
    private static string InitializerFor(DataItem item)
    {
        var pic = item.Pic!;

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

        return pic.Category switch
        {
            PicCategory.Alphanumeric or PicCategory.NumericEdited =>
                $"CobolString.Store({EmitText.CsLiteral(EmitText.DecodeCobolString(raw))}, {pic.Length})",
            PicCategory.Numeric when pic.IsFloat => RawValueAsFloat(raw, pic),
            PicCategory.Numeric => EmitText.UnscaledAtScale(raw, pic.Scale),
            _ => pic.DefaultInitializer,
        };
    }

    /// <summary>If <paramref name="raw"/> is a figurative constant, its C# initializer given the receiver's category
    /// and width; otherwise null (ISO §8.3.1.2; HIGH/LOW = U+00FF/U+0000 per COBOLNET_DESIGN §14.9).</summary>
    private static string? FigurativeInitializer(string raw, PicInfo pic)
    {
        string key = raw.ToUpperInvariant();
        // ALL <figurative-word> (e.g. ALL ZEROS, ALL SPACES) is equivalent to the bare figurative (a single-character
        // figurative repeated to the width); strip the ALL prefix when the remainder is a figurative WORD. (ALL "literal"
        // — repeating a multi-character literal — is a separate form left to the literal path.)
        if (FillCharFor(key) is null && key.StartsWith("ALL") && key.Length > 3 && FillCharFor(key[3..]) is not null)
            key = key[3..];
        if (FillCharFor(key) is not { } fillChar) return null;
        return pic.Category is PicCategory.Numeric ? pic.DefaultInitializer : $"new string({fillChar}, {pic.Length})";
    }

    /// <summary>The C# <c>char</c>-literal a figurative-constant word fills with, or null if the text is not a
    /// figurative word (ISO §8.3.1.2; HIGH/LOW = U+00FF/U+0000 per COBOLNET_DESIGN §14.9).</summary>
    private static string? FillCharFor(string word) => word switch
    {
        "ZERO" or "ZEROS" or "ZEROES" => "'0'",
        "SPACE" or "SPACES" => "' '",
        "HIGH-VALUE" or "HIGH-VALUES" => "'\\u00ff'",
        "LOW-VALUE" or "LOW-VALUES" or "NULL" or "NULLS" => "'\\u0000'",
        "QUOTE" or "QUOTES" => "'\\\"'",
        _ => null,
    };

    /// <summary>A numeric VALUE literal as a C# float/double literal for a COMP-1/COMP-2 item.</summary>
    private static string RawValueAsFloat(string raw, PicInfo pic) =>
        pic.Usage == Usage.Float ? $"{raw.Trim().TrimStart('+')}f" : $"{raw.Trim().TrimStart('+')}d";
}
