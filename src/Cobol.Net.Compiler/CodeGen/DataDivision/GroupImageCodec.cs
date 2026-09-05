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
    /// the recursion runs through this wrapper, so nested OCCURS repeat too).
    /// <para><paramref name="useValues"/> false suppresses every VALUE and composes the CATEGORY-DEFAULT initial
    /// image instead — the §13.18.63 GR4a shape, where "a PLAIN external item's VALUE takes effect only during
    /// INITIALIZE", so its run-unit cell must seed blank/zero rather than VALUE-composed. It is the SAME
    /// composition either way, which is the point: a byte-form leaf's default is the ZERO ENCODING of its pinned
    /// form (radix-2 / BCD / IEEE / INDEX bytes), never a run of ASCII '0' characters. The EXTERNAL path used to
    /// hand-roll that default in the BINDER (<c>CallInitialImage</c>, a second seeder with a char-fill model that
    /// predated the byte forms); one seeder, one flag (kb/Work PB164 — one-mechanism-per-job).</para></summary>
    public string ImageInitOf(DataItem item, bool useValues = true)
    {
        string one = ImageInitOfOne(item, useValues);
        return item.Occurs is { } n and > 1 ? RuntimeApi.StrRepeat(one, $"{n}") : one;
    }

    private string ImageInitOfOne(DataItem item, bool useValues)
    {
        if (item.IsGroup)
        {
            // ⛔ A GROUP-LEVEL VALUE INITIALIZES THE AREA (ISO §13.18.63.4 GR5 — "the group area is initialized
            // without consideration for the individual elementary or group items contained within this group"),
            // so it REPLACES the member-wise composition below rather than being composed alongside it. The one
            // area rule is GroupValueSlicer.AreaTextOf, shared with the record-struct lane.
            //
            // ⚠ THIS ARM WAS MISSING, and it was a live silent wrong answer on LEGAL source (kb/Work PB184's
            // sibling sweep). ImageInitOf is THE seeder for every character-image backing since the PB164
            // consolidation — Tier-B REDEFINES backings (RecordStructEmitter / PhysicalModel), EXTERNAL run-unit
            // cells (DataEmitter.ExternalCellSeed), BASED/ADDRESS-OF cells (ProgramEmitter / PtrEmitter) and the
            // OO backings — and it walked straight past the group's own RawValue into the members. Measured on
            // 8ca74a3d: `01 G VALUE "ABCD". 05 A PIC X(2). 05 B PIC X(2). 01 R REDEFINES G PIC X(4).` left A, B
            // and R all SPACES, while the identical group WITHOUT the REDEFINES initialized "AB"/"CD" — the
            // whole VALUE lost to nothing but the presence of an alias. §13.18.63.3 SR12 bars a VALUE in the
            // redefinING entry, never in the redefined one, so that program is conforming.
            // (The seventh instance of the two-arm-dispatch shape: the slicer's arm was written, the codec's
            // was not, and only the slicer's lane was ever tested.)
            //
            // ⛔ The BIT-PACKED exclusion is NOT restated here. It used to be, keyed on GROUP-USAGE BIT — a
            // predicate NARROWER than the fact it protects: what switches DataItem.ImageWidth from a character
            // sum to the §8.5.1.6.3 bit walk is HasBitDescendant (D19/PB43), which GROUP-USAGE BIT is only the
            // commonest way to acquire. The exclusion now lives once, inside AreaTextOf, so both lanes inherit
            // the same predicate and no future lane can pick up the narrow one (kb/Work PB207).
            if (useValues && GroupValueSlicer.AreaTextOf(item, ctx) is { } area)
                return EmitText.CsLiteral(area);

            // Redefining children overlay storage already composed by their targets — never part of the image.
            var parts = item.Children.Where(c => (c.IsGroup || c.IsElementary) && c.RedefinesTargetName is null)
                .Select(c => ImageInitOf(c, useValues));
            return item.Children.Count > 0 ? "(" + string.Join(" + ", parts) + ")" : "\"\"";
        }
        var pic = item.Pic!;
        if (useValues && item.RawValue is { } raw)
        {
            if (vals.FigurativeInitializer(raw, pic) is { } fig && pic.Category is not PicCategory.Numeric) return fig;
            // A NUMERIC member contributes the BYTES of its VALUE through its own pinned byte form — zoned
            // digits for DISPLAY, radix-2 / BCD for BINARY / PACKED (V59). ⛔ ONE ENCODER, so the width is the
            // item's StorageWidth BY CONSTRUCTION (kb/Work PB188): the CCVS-leniency spelling that used to sit
            // here stored `pic.Length` CHARACTERS — the PICTURE's digit count — which for a byte-form member is
            // simply a different number (`PIC 9(4) COMP` is 4 digits and 2 bytes) and displaced every following
            // member of the image. The leniency itself survives as what it always meant: an alphanumeric literal
            // on a numeric item is read AS the numeric literal §13.18.63.3 SR2 asked for (NC107A's
            // `PIC 999 VALUE "000"` under a REDEFINES), which is the SAME encode — hence one arm, not two.
            if (pic.Category is PicCategory.Numeric && !pic.IsFloat && vals.FigurativeInitializer(raw, pic) is null)
                return RuntimeApi.NumFormatImage(
                    EmitText.UnscaledAtScale(raw.StartsWith('"') ? CobolLiteral.Decode(raw) : raw, pic.Scale),
                    item.ProfileName);
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
        // A FLOAT member's image is its IEEE window bytes (the Step D arm-1 dissolution — the ' '×Length
        // fall-through seeded ZERO characters for the PICTURE-less float shapes, Length 0): the VALUE
        // literal through the ONE recipe, else the zero encoding.
        if (pic.Category is PicCategory.Numeric && pic.IsFloat)
            return RuntimeApi.NumFormatImageFloat(
                useValues && item.RawValue is { } fraw && !fraw.StartsWith('"')
                    && vals.FigurativeInitializer(fraw, pic) is null
                    ? ValueInitializer.RawValueAsFloat(fraw, pic) : "0d",
                item.ProfileName);
        // ⛔ THE TIER-B IMAGE ARM'S NO-VALUE SEED — §14.6.2.3.2 action 1's BACKGROUND on the image axis
        // (kb/Work PB152). This was the arm PB151 could not reach: the ALLOCATE fix landed with its fill decoder
        // PRIVATE to PtrEmitter, so this fall-through went on hardcoding ' ' / '0' with no way to consult the
        // OPTIONS model. It now asks the SAME choke point the native-field arm asks (InitialStateBackground),
        // one call apart, and a drift test asserts the two agree for every category × usage. The baselines below
        // are unchanged and remain what a no-clause program gets.
        if (vals.Background.Seed(item, pic) is { } bg) return bg;
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

    /// <summary>Emit a variable-length group's <c>CurrentImage()</c> — the §14.9.11.4 GR7 implementor-defined
    /// DISPLAY format, documented as CONFORMANCE.md A.1 item 57 (kb/Work PB164): the members' images in
    /// declaration order, each FIXED member contributing exactly its <see cref="AsImageOf"/> recipe (the ONE
    /// member-image law — no second copy), each dynamic-length leaf its CURRENT content, each dynamic-capacity
    /// table every occurrence at its CURRENT capacity, and each nested SCALAR variable-length group its own
    /// <c>CurrentImage()</c>. The geometry follows the §15.50.4 r7 LENGTH sum in CHARACTER POSITIONS —
    /// <c>FUNCTION LENGTH(G)</c> equals <c>CurrentImage().Length</c> except that a NATIONAL member displays
    /// one character per position while LENGTH counts its two bytes (the sanctioned D-N1/D-N3 divergence;
    /// the golden pins the relationship with a national member so it is observed, not assumed). DISPLAY-only:
    /// the static record codec (<c>AsImage</c>/<c>FromImage</c>) still excludes these groups (D9 — no fixed
    /// record window).</summary>
    public void EmitCurrentImageMethod(DataItem group, CodeWriter w)
    {
        var dyn = group.Children
            .Where(c => c.RedefinesTargetName is null && (c.IsGroup || c.IsElementary)
                && (c.IsDynamicTable || c.IsDynamicLength || (c.IsGroup && !c.IsImageCapable)))
            .ToDictionary(c => c.CsName, StringComparer.Ordinal);
        var parts = phys.PhysicalChildrenOf(group)
            .Select(f => dyn.TryGetValue(f.Name, out var d) ? CurrentMemberImage(d) : AsImageOf(f))
            .ToList();
        w.Line($"public readonly string CurrentImage() => {(parts.Count > 0 ? string.Join(" + ", parts) : "\"\"")};");
    }

    private string CurrentMemberImage(DataItem d) =>
        d.IsDynamicTable
            ? d.IsGroup ? $"{d.CsName}.CurrentImage(static __e => __e.AsImage())"
            // The element lane mirrors PhysicalModel's numLeaf rule exactly: a NATIVE numeric element goes
            // through its byte-form lane (float via the distinctly-named IEEE lane), a string-carried element
            // (alphanumeric / edited / StoreAsImage) passes through.
            : !d.StoreAsImage && d.Pic is { HasImageByteForm: true }
                ? (d.Pic.IsFloat
                    ? $"{d.CsName}.CurrentImage(__e => {RuntimeApi.NumFormatImageFloat("__e", d.ProfileName)})"
                    : $"{d.CsName}.CurrentImage(__e => {RuntimeApi.NumFormatImage("__e", d.ProfileName)})")
                : $"{d.CsName}.CurrentImage(static __e => __e)"
        : d.IsDynamicLength ? d.CsName   // §15.50.4 r7b — the current content at its current length
        : $"{d.CsName}.CurrentImage()";  // a nested variable-length group

    // ── The VARIABLE-LENGTH GROUP's ACTIVATION-BOUNDARY codec (ISO §14.8.2.2 / §14.8.3.2 via §14.9.4.3 SR25;
    //    kb/Work PB204) ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>What one member of a variable-length group contributes to its boundary carrier.</summary>
    private enum VarPartKind
    {
        /// <summary>A member with a fixed image — it lands in <c>CobolVarGroup.Fixed</c> at its own offset.</summary>
        Fixed,
        /// <summary>A dynamic-length elementary item — ONE carried component (§8.5.1.10).</summary>
        DynLeaf,
        /// <summary>A dynamic-capacity table — ONE carried component, its occurrences concatenated (§8.5.1.9).</summary>
        DynTable,
        /// <summary>A nested SCALAR variable-length group — FLATTENED: its own fixed run joins ours and its own
        /// components join ours in place, because §8.5.1.12 is stated over relative byte positions and is blind
        /// to the declaration tree.</summary>
        Nested,
    }

    private readonly record struct VarPart(
        VarPartKind Kind, PhysicalModel.Physical Field, DataItem? Item, int FixedWidth, int DynCount);

    /// <summary>A variable-length group's members classified for the boundary carrier, in PHYSICAL order — the
    /// same <see cref="PhysicalModel.PhysicalChildrenOf"/> order and the same dynamic-member selection
    /// <see cref="EmitCurrentImageMethod"/> uses, so the DISPLAY format and the crossing can never disagree
    /// about what a member is.</summary>
    private List<VarPart> VarParts(DataItem group)
    {
        var dyn = group.Children
            .Where(c => c.RedefinesTargetName is null && (c.IsGroup || c.IsElementary)
                && (c.IsDynamicTable || c.IsDynamicLength || (c.IsGroup && !c.IsImageCapable)))
            .ToDictionary(c => c.CsName, StringComparer.Ordinal);
        var parts = new List<VarPart>();
        foreach (var f in phys.PhysicalChildrenOf(group))
        {
            if (!dyn.TryGetValue(f.Name, out var d))
            {
                parts.Add(new VarPart(VarPartKind.Fixed, f, null, f.Width, 0));
                continue;
            }
            parts.Add(d.IsDynamicLength ? new VarPart(VarPartKind.DynLeaf, f, d, 0, 1)
                : d.IsDynamicTable ? new VarPart(VarPartKind.DynTable, f, d, 0, 1)
                : new VarPart(VarPartKind.Nested, f, d, VarFixedWidth(d), VarComponentCount(d)));
        }
        return parts;
    }

    /// <summary>The character width of a variable-length group's FIXED run — its image with every
    /// variable-length component collapsed to nothing. This is the §8.5.1.12.3 accounting the compatibility
    /// relation is stated in ("all dynamic-length elementary items are considered to be of zero length"),
    /// which is exactly why two COMPATIBLE groups lay this string out the same way.</summary>
    public int VarFixedWidth(DataItem group) => VarParts(group).Sum(p => p.FixedWidth);

    /// <summary>How many variable-length components a group carries, nested groups flattened in.</summary>
    public int VarComponentCount(DataItem group) => VarParts(group).Sum(p => p.DynCount);

    /// <summary>Emit a variable-length group's <c>AsVarImage()</c> / <c>FromVarImage()</c> — the boundary codec
    /// §14.8.2.2 and §14.8.3.2 need, the exact analogue of <c>AsImage</c>/<c>FromImage</c> for a group that has
    /// no fixed record window. Gated on <see cref="DataItem.CurrentExtentImageCapable"/>, THE ONE capability
    /// (kb/Work PB204): a group whose extent DISPLAY can compose is a group whose extent a crossing can carry,
    /// and giving the crossing its own gate would be the second copy this repo's two-arm rule forbids.
    /// <para>The fixed members ride exactly the <see cref="AsImageOf"/> / <see cref="EmitMemberFromImage"/>
    /// member-image law the record codec uses — the ONE recipe, no second spelling — at offsets computed with
    /// the variable-length components contributing ZERO, which is §8.5.1.12.3's own convention.</para></summary>
    public void EmitVarImageMethods(DataItem group, CodeWriter w)
    {
        var parts = VarParts(group);
        int totalFixed = parts.Sum(p => p.FixedWidth);
        using (w.Block("public readonly CobolVarGroup AsVarImage()"))
        {
            var fixedParts = new List<string>();
            var dynParts = new List<string>();
            int n = 0;
            foreach (var p in parts)
            {
                switch (p.Kind)
                {
                    case VarPartKind.Fixed:
                        if (p.Field.Width > 0 || p.Field.BitRun is not null) fixedParts.Add(AsImageOf(p.Field));
                        break;
                    case VarPartKind.DynLeaf:
                    case VarPartKind.DynTable:
                        dynParts.Add(CurrentMemberImage(p.Item!));
                        break;
                    case VarPartKind.Nested:
                        // The nested group's own carrier, spliced in place: its fixed run extends ours and its
                        // components take the next p.DynCount slots (the flattening §8.5.1.12 requires).
                        w.Line($"var __n{n} = {p.Field.Name}.AsVarImage();");
                        fixedParts.Add($"__n{n}.Fixed");
                        for (int k = 0; k < p.DynCount; k++) dynParts.Add($"__n{n}.Dyn({k})");
                        n++;
                        break;
                }
            }
            w.Line($"return new CobolVarGroup({(fixedParts.Count > 0 ? string.Join(" + ", fixedParts) : "\"\"")}, "
                + $"new string[] {{ {string.Join(", ", dynParts)} }});");
        }
        using (w.Block("public void FromVarImage(CobolVarGroup __v)"))
        {
            w.Line($"string __s = {RuntimeApi.StrStore("__v.Fixed", $"{totalFixed}")};");
            int off = 0, dynAt = 0;
            foreach (var p in parts)
            {
                switch (p.Kind)
                {
                    case VarPartKind.Fixed:
                        EmitMemberFromImage(p.Field, off, w);
                        off += p.Field.Width;
                        break;
                    case VarPartKind.DynLeaf:
                        // §8.5.1.10.4 — the receiving store for a dynamic-length item: replace, truncate on the
                        // right to the LIMIT, never pad. The component IS the item's whole current content.
                        w.Line($"{p.Field.Name} = {RuntimeApi.DynStore($"__v.Dyn({dynAt})", $"{p.Item!.DynLengthLimit}")};");
                        dynAt++;
                        break;
                    case VarPartKind.DynTable:
                        // The capacity comes from the carried length divided by OUR element width — legitimate
                        // because §8.5.1.12.3 admits corresponding tables only "when the byte length of their
                        // elements is equal", which the bind-time compatibility check enforced.
                        w.Line($"{p.Field.Name}.FromCurrentImage(__v.Dyn({dynAt}), {p.Field.Width}, "
                            + $"{TableElementFromImage(p.Item!)});");
                        dynAt++;
                        break;
                    case VarPartKind.Nested:
                        w.Line($"{p.Field.Name}.FromVarImage(__v.Slice({off}, {p.FixedWidth}, {dynAt}, {p.DynCount}));");
                        off += p.FixedWidth;
                        dynAt += p.DynCount;
                        break;
                }
            }
        }
    }

    /// <summary>The inverse of <see cref="CurrentMemberImage"/>'s dynamic-table element lane, arm for arm: a
    /// group element distributes through its own <c>FromImage</c>, a NATIVE numeric element decodes through its
    /// byte-form lane (a float through the distinctly-named IEEE lane), a string-carried element passes
    /// through. The seed occurrence is handed in so a group element keeps the arrays its initializer
    /// allocated.</summary>
    private static string TableElementFromImage(DataItem d) =>
        d.IsGroup ? "(__e, __x) => { __e.FromImage(__x); return __e; }"
        : !d.StoreAsImage && d.Pic is { HasImageByteForm: true }
            ? (d.Pic.IsFloat
                ? $"(__e, __x) => ({d.Pic.ClrType}){RuntimeApi.NumParseImageFloat("__x", d.ProfileName)}"
                : $"(__e, __x) => ({d.Pic.ClrType}){RuntimeApi.NumParseImage("__x", d.ProfileName)}")
            : "(__e, __x) => __x";

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
