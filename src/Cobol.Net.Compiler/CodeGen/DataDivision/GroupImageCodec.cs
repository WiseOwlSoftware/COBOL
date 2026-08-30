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
        return item.Occurs is { } n and > 1 ? RuntimeApi.StrRepeat(one, $"{n}") : one;
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
                return RuntimeApi.StrStore(EmitText.CsLiteral(CobolLiteral.Decode(raw)), $"{pic.Length}");
            if (pic.Category is PicCategory.Numeric && !pic.IsFloat && vals.FigurativeInitializer(raw, pic) is null)
                return RuntimeApi.NumFormatImage(EmitText.UnscaledAtScale(raw, pic.Scale), item.ProfileName);
            // A NUMERIC literal VALUE on a numeric-edited member contributes its EDITED image (§13.18.63 GR6) —
            // for a format-2 (LOCALE) member a RUNTIME image (no compile-time image exists, §13.18.40.5 r11;
            // the ONE producer, RuntimeApi.LocaleEditCompose — PB64 T6; the EditCompose arm's mask deref would NRE).
            if (pic.LocaleEdit is not null && !raw.StartsWith('"')
                && ValueInitializer.TryParseNumeric(raw, out var luv, out int lsc))
                return RuntimeApi.LocaleEditCompose(pic, luv, lsc, item.BlankWhenZero);
            if (pic.Category is PicCategory.NumericEdited && !raw.StartsWith('"')
                && ValueInitializer.TryParseNumeric(raw, out var uv, out int sc))
                return EmitText.CsLiteral(RuntimeApi.EditCompose(uv, sc, pic.EditMask!,
                    item.BlankWhenZero, pic.CurrencyString, ctx.Data.DecimalPointIsComma, pic.EditingRules));
            if (pic.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited)
                return RuntimeApi.StrStore(EmitText.CsLiteral(CobolLiteral.Decode(raw)), $"{pic.Length}");
            // Boolean members of a Tier-B class contribute their zero-padded VALUE image (national never
            // reaches a Tier-B backing — ComputeTier rejects the class; the arm is defensive).
            // ⛔ A USAGE BIT member contributes its PACKED image, not its carrier (D19/PB43): the backing is sized
            // from ImageWidth, which is now ceil(n/8), so seeding it with n carrier characters would silently
            // truncate against the backing width. Found by sweeping the OTHER image path after the AsImage/
            // FromImage pair was packed — the two compose the same bytes and must agree (rule 4).
            if (pic.Category is PicCategory.Boolean)
                return pic.Usage is Usage.Bit
                    ? RuntimeApi.BitsPack(
                        RuntimeApi.StrStoreBoolean(EmitText.CsLiteral(CobolLiteral.Decode(raw)), $"{pic.Length}",
                                                   justifiedRight: false), $"{pic.Length}")
                    : RuntimeApi.StrStoreBoolean(EmitText.CsLiteral(CobolLiteral.Decode(raw)), $"{pic.Length}", justifiedRight: false);
            if (pic.Category is PicCategory.National)
                return RuntimeApi.StrStore(EmitText.CsLiteral(CobolLiteral.Decode(raw)), $"{pic.Length}");
        }
        return pic.Category is PicCategory.Numeric && !pic.IsFloat
            ? RuntimeApi.NumFormatImage("0L", item.ProfileName)
            // Boolean initial state — zeros (§13.18.63). A USAGE BIT item's zeros are PACKED, so the seed is
            // ceil(n/8) zero BYTES rather than n zero characters (D19/PB43); the all-zero bit pattern makes the
            // packed form '\0' repeated, which is what the AsImage side would produce for the same value.
            : pic.Category is PicCategory.Boolean
                ? pic.Usage is Usage.Bit
                    ? $"new string('\\0', {BitLayout.Characters(pic.Length)})"
                    : $"new string('0', {pic.Length})"
                : $"new string(' ', {pic.Length})";
    }

    public void EmitImageMethods(DataItem group, CodeWriter w)
    {
        var members = phys.PhysicalChildrenOf(group);
        w.Line($"public readonly string AsImage() => {(members.Count > 0 ? string.Join(" + ", members.Select(AsImageOf)) : "\"\"")};");
        using (w.Block("public void FromImage(string __s)"))
        {
            w.Line($"__s = {RuntimeApi.StrStore("__s", $"{members.Sum(f => f.Width)}")};");   // pad/truncate to the image width
            int off = 0;
            foreach (var f in members)
            {
                EmitMemberFromImage(f, off, w);
                off += f.Width;
            }
        }
        if (group.GroupUsage is GroupUsage.Bit) EmitBitMethods(group, w);
    }

    /// <summary>A BIT GROUP's elementary face (§13.18.29.4 GR1b — "treated as though it were an elementary data item
    /// … PICTURE 1(m)"; D20/PB79): <c>AsBits()</c> is the group's boolean-position string — its members' bit
    /// carriers concatenated in declaration order (every member is a bit leaf or a bit group, SR2, and same-level
    /// bit items sit at successive positions with no filler, §8.5.1.6.3) — and <c>FromBits</c> distributes a
    /// boolean string back to the members. The packed byte form stays <c>AsImage()</c>/<c>FromImage()</c> (the
    /// record / REDEFINES / file image); the two agree because both pack the same run.</summary>
    private static void EmitBitMethods(DataItem group, CodeWriter w)
    {
        var members = group.Children.Where(c => c.RedefinesTargetName is null && (c.IsGroup || c.IsElementary)).ToList();
        w.Line($"public readonly string AsBits() => {(members.Count > 0 ? string.Join(" + ", members.Select(BitCarrierOf)) : "\"\"")};");
        using (w.Block("public void FromBits(string __b)"))
        {
            int total = members.Sum(BitLayout.RunBits);
            w.Line($"__b = {RuntimeApi.StrStoreBoolean("__b", $"{total}", justifiedRight: false)};");   // pad with boolean zeros / truncate to m
            int at = 0;
            foreach (var m in members)
            {
                EmitRunMemberFromBits(m, "__b", at, w);
                at += BitLayout.RunBits(m);
            }
        }
    }

    /// <summary>Distribute one run member's slice of an unpacked bit carrier — THE one distributor, ridden by
    /// both the bit-group <c>FromBits</c> face and the record <c>FromImage</c> run loop (kb/Work PB161: the
    /// FromImage copy lacked the OCCURS loop its sibling here had, so a <c>PIC 1(8) USAGE BIT OCCURS 3</c>
    /// member made the generated record fail backend compilation with CS0029 — the sixth instance of the
    /// two-arm-dispatch defect shape).</summary>
    private static void EmitRunMemberFromBits(DataItem m, string carrier, int at, CodeWriter w)
    {
        int per = m.IsGroup ? m.AsIfPic!.Length : m.Pic!.Length;
        if (m.Occurs is { } o)
        {
            using (w.Block($"for (int __i = 0; __i < {o}; __i++)"))
                w.Line(m.IsGroup
                    ? $"{m.CsName}[__i].FromBits({RuntimeApi.BitsSlice(carrier, $"{at} + __i * {per}", $"{per}")});"
                    : $"{m.CsName}[__i] = {RuntimeApi.BitsSlice(carrier, $"{at} + __i * {per}", $"{per}")};");
        }
        else
        {
            w.Line(m.IsGroup
                ? $"{m.CsName}.FromBits({RuntimeApi.BitsSlice(carrier, $"{at}", $"{per}")});"
                : $"{m.CsName} = {RuntimeApi.BitsSlice(carrier, $"{at}", $"{per}")};");
        }
    }

    /// <summary>One member's AsImage sub-expression: a scalar string field directly, a nested group's
    /// <c>AsImage()</c>, a NATIVE fixed-point leaf the BYTES it occupies (<c>CobolNum.FormatImage</c> with the
    /// leaf's own profile — its zoned digits for USAGE DISPLAY, its radix-2 / BCD bytes for BINARY / PACKED,
    /// §13.18.60.4 GR4/GR11), or — for a fixed-OCCURS table — the concatenation of every occurrence's image
    /// (ISO §14.9: a group move treats the whole group, INCLUDING every OCCURS position, as one alphanumeric
    /// item).</summary>
    /// <summary>A §8.5.1.6.3 run member's BIT CARRIER expression — THE one carrier law (kb/Work PB161): a bit
    /// leaf's '0'/'1' string field; a bit GROUP's <c>AsBits()</c>; a fixed-OCCURS member the concatenation of
    /// every occurrence's carrier (the AsImage run packer read the raw <c>string[]</c> field before, CS1503).</summary>
    private static string BitCarrierOf(DataItem m) =>
        m.Occurs is not null
            ? (m.IsGroup ? $"string.Concat(System.Array.ConvertAll({m.CsName}, __e => __e.AsBits()))"
                         : $"string.Concat({m.CsName})")
            : m.IsGroup ? $"{m.CsName}.AsBits()" : m.CsName;

    private static string AsImageOf(PhysicalModel.Physical f) =>
        // D19/PB43 — a USAGE BIT run images as its PACKED bits (§13.18.60.4 GR5), high-order first, and a run may
        // span several FIELDS because §8.5.1.6.3 puts same-level bit items at successive bit positions. The run's
        // leader packs every member's carrier concatenated; a continuation contributed Width 0 and images as "".
        f.BitRun is { } run
            ? RuntimeApi.BitsPack(string.Join(" + ", run.Select(BitCarrierOf)),
                                  $"{run.Sum(BitLayout.RunBits)}")
        : f.Width == 0 ? "\"\""
        : f.Occurs == 0
            ? (f.IsGroupStruct ? $"{f.Name}.AsImage()"
               // A FLOAT leaf encodes through the IEEE lane (kb/Work PB164 wave 2 — distinctly named so
               // integer call sites stay unambiguous).
               : f.NumLeaf is { } leaf ? (leaf.Pic!.IsFloat
                    ? RuntimeApi.NumFormatImageFloat(f.Name, leaf.ProfileName)
                    : RuntimeApi.NumFormatImage(f.Name, leaf.ProfileName))
               : f.Name)
        : f.IsGroupStruct ? $"string.Concat(System.Array.ConvertAll({f.Name}, __e => __e.AsImage()))"
        : f.NumLeaf is { } l ? $"string.Concat(System.Array.ConvertAll({f.Name}, __e => {(l.Pic!.IsFloat
                ? RuntimeApi.NumFormatImageFloat("__e", l.ProfileName)
                : RuntimeApi.NumFormatImage("__e", l.ProfileName))}))"
        : $"string.Concat({f.Name})";

    /// <summary>Distribute the slice of the image at <paramref name="off"/> into one member: a scalar string field
    /// gets its substring; a nested group gets <c>FromImage</c>; a NATIVE fixed-point leaf decodes its byte slice
    /// (<c>CobolNum.ParseImage</c> with the leaf's own profile, cast to its CLR storage type — an incompatible
    /// position, e.g. the spaces a short record's pad legitimately deposits, decodes deterministically per ISO
    /// §14.6.13.2, see CobolNum); a fixed-OCCURS table loops its occurrences, each taking its per-occurrence width
    /// in source order (the array elements are value-type structs/strings, mutated in place).
    /// <para>ONE profile, never an image-specific override: since the image IS the item's bytes, a BINARY/PACKED
    /// leaf's sign lives in those bytes (two's complement / the sign nibble) and its <c>SignKind</c> — a DISPLAY
    /// concern — is not consulted at all. The former <c>ImageProfileOf</c> sign rewrite existed only because the
    /// image was a zoned digit run that had to carry a fixed-width sign.</para></summary>
    private static void EmitMemberFromImage(PhysicalModel.Physical f, int off, CodeWriter w)
    {
        // D19/PB43 — the run's leader unpacks the shared byte(s) ONCE and distributes the boolean positions back
        // to each member in declaration order; the continuations have Width 0 and consume no slice.
        if (f.BitRun is { } run)
        {
            int runBits = run.Sum(BitLayout.RunBits);
            // ⚠ The carrier local is named for the run's OFFSET, not a bare `__bits`: a group may hold SEVERAL
            // runs (two bit items separated by a character item are two runs, §8.5.1.6.3 — the character item
            // breaks the same-level adjacency), and they all land in this one method scope. The first version
            // emitted `var __bits` per run and a two-run group failed to compile with CS0128.
            string carrier = $"__bits{off}";
            w.Line($"var {carrier} = {RuntimeApi.BitsUnpack($"__s.Substring({off}, {f.Width})", $"{runBits}")};");
            int at = 0;
            foreach (var m in run)
            {
                // THE one distributor (kb/Work PB161) — a bit GROUP member spreads to its subordinates
                // (FromBits, D20/PB79), a fixed-OCCURS member loops its occurrences.
                EmitRunMemberFromBits(m, carrier, at, w);
                at += BitLayout.RunBits(m);
            }
            return;
        }
        if (f.Width == 0) return;   // a run continuation — its value came from the leader's unpack
        if (f.Occurs == 0)
        {
            w.Line(f.IsGroupStruct
                ? $"{f.Name}.FromImage(__s.Substring({off}, {f.Width}));"
                : f.NumLeaf is { } leaf
                // A FLOAT leaf decodes through the IEEE bit-reinterpretation lane — the Int128 lane's cast
                // would numerically CONVERT the parsed integer (kb/Work PB164 wave 2).
                ? $"{f.Name} = ({leaf.Pic!.ClrType}){(leaf.Pic!.IsFloat
                        ? RuntimeApi.NumParseImageFloat($"__s.Substring({off}, {f.Width})", leaf.ProfileName)
                        : RuntimeApi.NumParseImage($"__s.Substring({off}, {f.Width})", leaf.ProfileName))};"
                : $"{f.Name} = __s.Substring({off}, {f.Width});");
            return;
        }
        int elem = f.Width / f.Occurs;   // per-occurrence width (Width = elem × Occurs, exact by construction)
        using (w.Block($"for (int __i = 0; __i < {f.Occurs}; __i++)"))
            w.Line(f.IsGroupStruct
                ? $"{f.Name}[__i].FromImage(__s.Substring({off} + __i * {elem}, {elem}));"
                : f.NumLeaf is { } l
                ? $"{f.Name}[__i] = ({l.Pic!.ClrType}){(l.Pic!.IsFloat
                        ? RuntimeApi.NumParseImageFloat($"__s.Substring({off} + __i * {elem}, {elem})", l.ProfileName)
                        : RuntimeApi.NumParseImage($"__s.Substring({off} + __i * {elem}, {elem})", l.ProfileName))};"
                : $"{f.Name}[__i] = __s.Substring({off} + __i * {elem}, {elem});");
    }
}
