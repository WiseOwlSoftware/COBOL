// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.CodeGen.Emit;

namespace CobolNet.CodeGen;

/// <summary>The whole-group character-image codec (P7 Step 9l; COBOLNET_DESIGN §14.4): the generated
/// <c>AsImage</c>/<c>FromImage</c> pair every image-capable group carries — string leaves verbatim, NATIVE
/// fixed-point leaves through their zoned image profile — plus the compile-time INITIAL-image composer that
/// seeds Tier-B REDEFINES backings. Wired by <see cref="DataEmitter"/>.</summary>
internal sealed class GroupImageCodec(EmitContext ctx, PhysicalModel phys, ValueInitializer vals)
{
    /// <summary>The C# string-expression for an item's INITIAL character image (used to seed a Tier-B backing from
    /// the canonical's VALUE): a group concatenates its leaves' images; an elementary item formats its VALUE (numeric
    /// → <c>CobolNum.FormatDisplay</c>; alphanumeric/edited → the stored string; figurative/default per width). A
    /// fixed-OCCURS entry's image repeats <c>Occurs</c> times — every occurrence takes the VALUE (ISO §13.18.63 GR9;
    /// the recursion runs through this wrapper, so nested OCCURS repeat too).</summary>
    public string ImageInitOf(DataItem item)
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
            if (vals.FigurativeInitializer(raw, pic) is { } fig && pic.Category is not PicCategory.Numeric) return fig;
            // CCVS leniency (same as InitializerFor): an ALPHANUMERIC literal VALUE on a numeric DISPLAY item
            // contributes its CHARACTERS to the image (NC107A's `PIC 999 VALUE "000"` under a REDEFINES).
            if (pic.Category is PicCategory.Numeric && !pic.IsFloat && raw.StartsWith('"'))
                return $"CobolString.Store({EmitText.CsLiteral(CobolLiteral.Decode(raw))}, {pic.Length})";
            if (pic.Category is PicCategory.Numeric && !pic.IsFloat && vals.FigurativeInitializer(raw, pic) is null)
                return $"CobolNum.FormatDisplay({EmitText.UnscaledAtScale(raw, pic.Scale)}, {item.ProfileName})";
            // A NUMERIC literal VALUE on a numeric-edited member contributes its EDITED image (§13.18.63 GR6).
            if (pic.Category is PicCategory.NumericEdited && !raw.StartsWith('"')
                && ValueInitializer.TryParseNumeric(raw, out var uv, out int sc))
                return EmitText.CsLiteral(CobolNet.Runtime.CobolEdit.Format(uv, sc, pic.EditMask!,
                    item.BlankWhenZero, ctx.Data.CurrencyPicSymbol, ctx.Data.DecimalPointIsComma));
            if (pic.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited)
                return $"CobolString.Store({EmitText.CsLiteral(CobolLiteral.Decode(raw))}, {pic.Length})";
            // Boolean members of a Tier-B class contribute their zero-padded VALUE image (national never
            // reaches a Tier-B backing — ComputeTier rejects the class; the arm is defensive).
            if (pic.Category is PicCategory.Boolean or PicCategory.National)
                return $"CobolString.Store({EmitText.CsLiteral(CobolLiteral.Decode(raw))}, {pic.Length}"
                    + $"{(pic.Category is PicCategory.Boolean ? ", justifiedRight: false, pad: '0'" : "")})";
        }
        return pic.Category is PicCategory.Numeric && !pic.IsFloat
            ? $"CobolNum.FormatDisplay(0L, {item.ProfileName})"
            : pic.Category is PicCategory.Boolean
                ? $"new string('0', {pic.Length})"   // boolean initial state — zeros (§13.18.63)
                : $"new string(' ', {pic.Length})";
    }

    public void EmitImageMethods(DataItem group, CodeWriter w)
    {
        var members = phys.PhysicalChildrenOf(group);
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
    private static string AsImageOf(PhysicalModel.Physical f) =>
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
    private static void EmitMemberFromImage(PhysicalModel.Physical f, int off, CodeWriter w)
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

}
