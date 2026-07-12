// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.CodeGen.Emit;

namespace CobolNet.CodeGen;

/// <summary>The group-VALUE positional distributor (P7 Step 9l; ISO §13.18.63): a GROUP-level VALUE
/// initializes the whole area as ONE alphanumeric value, sliced positionally over the (distributable)
/// subtree at compile time; otherwise the member-wise composed initializer. Wired by <see cref="DataEmitter"/>.</summary>
internal sealed class GroupValueSlicer(PhysicalModel phys)
{
    public string ComposedInit(DataItem group)
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
        var parts = phys.PhysicalChildrenOf(group).Select(f => $"{f.Name} = {f.Init}");
        return $"new {group.StructName} {{ {string.Join(", ", parts)} }}";
    }

    /// <summary>The character text of a group VALUE operand: a quoted literal decoded, or <c>ALL "lit"</c>
    /// repeated to the group width (§8.3.3.6.4 GR2); null for any other operand form.</summary>
    private static string? GroupValueText(string raw, DataItem group) =>
        EmitText.AllLiteralText(raw) is { } al ? EmitText.RepeatToWidth(al, group.ImageWidth)
        : CobolLiteral.IsStringLiteral(raw) ? CobolLiteral.Decode(raw)
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

}
