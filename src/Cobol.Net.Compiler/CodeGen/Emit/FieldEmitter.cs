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
internal sealed class FieldEmitter(EmissionContext ctx)
{
    /// <summary>Emit every WORKING-STORAGE type, profile, index field, and root field.</summary>
    public void Emit()
    {
        var w = ctx.Writer;
        foreach (var root in ctx.Data.Roots) EmitStructTypeDecls(root, w);
        foreach (var root in ctx.Data.Roots) EmitProfiles(root, w);
        foreach (var (name, field) in ctx.Data.IndexFields)
            w.Line($"private static long {field} = 1;   // INDEX-NAME {name}");
        foreach (var root in ctx.Data.Roots)
            if (root.IsGroup || root.IsElementary)
            {
                string comment = root.CobolName is { } n ? $"   // {n}{(root.Occurs is { } o ? $" OCCURS {o}" : "")}" : "";
                w.Line($"private static {root.FieldType} {root.CsName} = {FieldInit(root)};{comment}");
            }
    }

    private static void EmitStructTypeDecls(DataItem item, CodeWriter w)
    {
        if (!item.IsGroup) return;
        foreach (var child in item.Children) EmitStructTypeDecls(child, w);   // nested types first (any order is fine)
        using (w.Block($"private record struct {item.StructName}"))
        {
            foreach (var child in item.Children)
                if (child.IsGroup || child.IsElementary)
                    w.Line($"public {child.FieldType} {child.CsName};   // {child.CobolName ?? "FILLER"}");

            // A pure-character (DISPLAY-homogeneous) group gets the whole-group image facility (COBOLNET_DESIGN
            // §14.4): AsImage concatenates the leaves' character images; FromImage distributes a character image
            // back into them. Used by whole-group MOVE / DISPLAY / compare. (Mixed-usage groups are the Tier-C
            // byte island — not emitted here.)
            if (item.IsAllAlphanumeric) EmitImageMethods(item, w);
        }
    }

    private static void EmitImageMethods(DataItem group, CodeWriter w)
    {
        var members = group.Children.Where(c => c.IsGroup || c.IsElementary).ToList();
        var parts = members.Select(c => c.IsGroup ? $"{c.CsName}.AsImage()" : c.CsName);
        w.Line($"public readonly string AsImage() => {(members.Count > 0 ? string.Join(" + ", parts) : "\"\"")};");
        using (w.Block("public void FromImage(string __s)"))
        {
            w.Line($"__s = CobolString.Store(__s, {group.ImageWidth});");   // pad/truncate to the group width
            int off = 0;
            foreach (var c in members)
            {
                int width = c.ImageWidth;
                w.Line(c.IsGroup
                    ? $"{c.CsName}.FromImage(__s.Substring({off}, {width}));"
                    : $"{c.CsName} = __s.Substring({off}, {width});");
                off += width;
            }
        }
    }

    private static void EmitProfiles(DataItem item, CodeWriter w)
    {
        if (item.IsElementary && item.Pic is { Category: PicCategory.Numeric, IsFloat: false })
            w.Line($"private static readonly NumProfile {item.ProfileName} = {item.Pic.ProfileInitializer};");
        foreach (var child in item.Children) EmitProfiles(child, w);
    }

    /// <summary>The C# initializer for a field: an array literal for an OCCURS table (every element initialized so
    /// none is left at <c>default</c>), a composed object-initializer for a group, else the elementary VALUE.</summary>
    private static string FieldInit(DataItem item)
    {
        if (item.Occurs is { } n)
        {
            string element = item.IsGroup ? ComposedInit(item) : InitializerFor(item);
            return $"new {item.ElementType}[] {{ {string.Join(", ", Enumerable.Repeat(element, n))} }}";
        }
        return item.IsGroup ? ComposedInit(item) : InitializerFor(item);
    }

    private static string ComposedInit(DataItem group)
    {
        var parts = group.Children
            .Where(c => c.IsGroup || c.IsElementary)
            .Select(c => $"{c.CsName} = {FieldInit(c)}");
        return $"new {group.StructName} {{ {string.Join(", ", parts)} }}";
    }

    /// <summary>The C# initializer expression for an elementary item, from its VALUE clause or the COBOL default.</summary>
    private static string InitializerFor(DataItem item)
    {
        var pic = item.Pic!;
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
        string? fillChar = raw.ToUpperInvariant() switch
        {
            "ZERO" or "ZEROS" or "ZEROES" => "'0'",
            "SPACE" or "SPACES" => "' '",
            "HIGH-VALUE" or "HIGH-VALUES" => "'\\u00ff'",
            "LOW-VALUE" or "LOW-VALUES" or "NULL" or "NULLS" => "'\\u0000'",
            "QUOTE" or "QUOTES" => "'\\\"'",
            _ => null,
        };
        if (fillChar is null) return null;
        return pic.Category is PicCategory.Numeric ? pic.DefaultInitializer : $"new string({fillChar}, {pic.Length})";
    }

    /// <summary>A numeric VALUE literal as a C# float/double literal for a COMP-1/COMP-2 item.</summary>
    private static string RawValueAsFloat(string raw, PicInfo pic) =>
        pic.Usage == Usage.Float ? $"{raw.Trim().TrimStart('+')}f" : $"{raw.Trim().TrimStart('+')}d";
}
