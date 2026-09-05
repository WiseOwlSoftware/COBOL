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
        // ⛔ A FORMAT 2 (table) VALUE GIVES EACH OCCURRENCE ITS OWN IMAGE — §13.18.63.4 GR12 ("A format 2 VALUE
        // clause initializes a table element to the value of literal-1"), GR13 (cyclic reuse under TO), GR14
        // (no TO = fill to the maximum), GR15 (a later FROM wins on overlap). This method read only
        // item.RawValue, which is null for a table VALUE (the two carriers are mutually exclusive on DataItem),
        // and the StrRepeat below then seeded EVERY occurrence with the same VALUE-less default: the table
        // VALUE was silently DISCARDED for every image-stored leaf — a Tier-B REDEFINES member, an EXTERNAL
        // cell, a BASED record, an OO backing. Measured on 3324d794 with
        // `05 B PIC 9(4) COMP OCCURS 2 VALUE 12 FROM (1) TO (2).` inside a REDEFINES-aliased group: the group
        // image came back `65 65 00 00 00 00 67 67`, the seed simply absent (kb/Work PB208).
        // ONE map for both lanes — ValueInitializer.ResolveTableValueMap, which the record-struct lane's
        // TableValueInit already used; an occurrence outside every FROM..TO range takes the same VALUE-less
        // image it takes today (null override ⇒ the background/default seed below).
        // ⚠ ELEMENTARY ONLY, the same predicate ValueInitializer.FieldInit guards with: a GROUP entry's
        // format-2 VALUE is a group-level VALUE (§13.18.63.3 SR16 carries SR13 in) and belongs to the
        // group-area lane (GroupValueSlicer.AreaTextOf), which composes no per-occurrence text.
        if (useValues && item.HasElementaryTableValue && item.Occurs is { } occ and > 0)
        {
            var map = ValueInitializer.ResolveTableValueMap(item, occ);
            return "(" + string.Join(" + ", Enumerable.Range(1, occ)
                .Select(o => ImageInitOfOne(item, useValues, map.GetValueOrDefault(o)))) + ")";
        }
        string one = ImageInitOfOne(item, useValues);
        return item.Occurs is { } n and > 1 ? RuntimeApi.StrRepeat(one, $"{n}") : one;
    }

    /// <param name="rawOverride">The literal for THIS occurrence of a format 2 (table) VALUE — the twin of
    /// <see cref="ValueInitializer.InitializerFor"/>'s parameter of the same name, so the image lane and the
    /// native-field lane compose one occurrence from the same literal text (kb/Work PB208).</param>
    private string ImageInitOfOne(DataItem item, bool useValues, string? rawOverride = null)
    {
        // ⛔ A POINTER-CLASS LEAF CONTRIBUTES RESERVED BYTES AND NO IMAGE (kb/Work PB231 — the pointer third).
        // Its value is a managed reference held in the area's MANAGED SLOT (Place.SlotWindow / StorageCell.SlotAt),
        // whose unwritten state IS null — §14.9.3.4 GR9's "data items of class object or class pointer in the
        // allocated storage are initialized to null" and §13.18.63.4's "data items of class message-tag, class
        // object, and class pointer are initialized to null" both hold with no seeding here. What the BYTE image
        // owes the shared area is exactly the item's storage extent of placeholder positions, so §14.9.3.4 GR3's
        // byte quantity and every FOLLOWING member's class offset are what a byte-addressed area says they are —
        // without this the pointer contributed its PICTURE-less zero-width nothing and displaced the rest.
        // Placed beside the national coding below because it is the same kind of decision: how the member's
        // value carrier relates to the bytes it occupies.
        if (SlotWindow.CarriedBySlot(item)) return $"new string(' ', {item.ByteWidth})";
        string image = CarrierInitOfOne(item, useValues, rawOverride);
        // ⛔ A NATIONAL LEAF'S SEED IS ITS BYTES, NOT ITS CARRIER (kb/Work PB231). Everything this method seeds
        // is a BYTE-ADDRESSED shared area — a Tier-B REDEFINES backing, an EXTERNAL run-unit cell, a
        // BASED/ADDRESS-OF cell, an OO backing — and a national character position occupies TWO of those bytes
        // (§13.18.60.4 GR8 leaves the size to the implementor; D-N1 pins two, UTF-16BE). Applied HERE, once,
        // over whatever the carrier arms below composed, so the VALUE arm, the OPTIONS INITIALIZE fill arm and
        // the no-clause space arm cannot each get it separately right or separately wrong — the same
        // one-transform discipline the USAGE BIT arms use with CobolBits.Pack. MEASURED before the fix on
        // `01 K. 05 KT PIC N(2) OCCURS 3. 01 KX REDEFINES K PIC X(12).`: KT(1)'s initial state read back as
        // U+2020 U+2020 (two 0x20 fill BYTES paired) instead of the national spaces its struct-stored twin
        // gets — a silent wrong answer on the first read of an uninitialized national member.
        // A GROUP is never wrapped: PositionsOf answers null for it, and its national children were each
        // wrapped by their own recursion through here.
        return NationalWindow.PositionsOf(item) is not null ? RuntimeApi.NatBytes(image) : image;
    }

    /// <summary>The item's initial image in its VALUE CARRIER's own units — see <see cref="ImageInitOfOne"/>,
    /// which applies the storage coding on top (kb/Work PB231).</summary>
    private string CarrierInitOfOne(DataItem item, bool useValues, string? rawOverride = null)
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
            // ⛔ The UNIT is not restated here. It used to be an exclusion keyed on GROUP-USAGE BIT — a
            // predicate NARROWER than the fact it protects — and then, once the exclusion moved into the area
            // rule, a refusal. Neither is here now: GroupValueSlicer.AreaOf answers with the area AND its unit,
            // so this lane cannot disagree with the record-struct lane about what a bit group's area is
            // (kb/Work PB207).
            if (useValues && GroupValueSlicer.AreaOf(item, ctx) is { } area)
                // A bit group's area is m BOOLEAN POSITIONS (§13.18.29.4 GR1b); its IMAGE is those positions
                // PACKED — ceil(m/8) characters through CobolBits.Pack, the ONE bit-order law (D19/PB43), never
                // a second packer written here. The count is asked of ExtentBits, not taken from the area's own
                // Length: the two are equal by AreaOf's contract, and asking the LAYOUT keeps the image the
                // group's full width even if a future area were handed back short (Pack zero-fills the rest).
                return area.Bits
                    ? RuntimeApi.BitsPack(EmitText.CsLiteral(area.Text), $"{BitLayout.ExtentBits(item)}")
                    : EmitText.CsLiteral(area.Text);

            // ⛔ A BIT GROUP'S IMAGE IS ITS PACKED AREA, NOT ITS MEMBERS' IMAGES CONCATENATED. Each bit member
            // images as its own ceil(n/8) characters, and §8.5.1.6.3 makes same-level bit members SHARE bytes —
            // so the concatenation is wider than the group. MEASURED at 62a36759 on
            // `01 K GROUP-USAGE BIT. 05 K1 PIC 1(2) VALUE B"11". 05 K2 PIC 1(2) VALUE B"01".
            //  01 KV REDEFINES K PIC X(1).`: K2 read back as `00` and the backing byte was 0xC0, because the
            // 2-character composition was truncated to the group's 1-character width and K2's byte fell off.
            // The composition is BitAreaOf's — the same §8.5.1.6.3 placement the VALUE arm above and
            // AsBits()/FromBits use.
            if (item.GroupUsage is GroupUsage.Bit)
                return RuntimeApi.BitsPack(BitAreaOf(item, m => InitialBitCarrierOf(m, useValues)),
                                           $"{BitLayout.ExtentBits(item)}");

            // Redefining children overlay storage already composed by their targets — never part of the image.
            var parts = item.Children.Where(c => (c.IsGroup || c.IsElementary) && c.RedefinesTargetName is null)
                .Select(c => ImageInitOf(c, useValues));
            return item.Children.Count > 0 ? "(" + string.Join(" + ", parts) + ")" : "\"\"";
        }
        var pic = item.Pic!;
        // A format 2 (table) VALUE supplies THIS occurrence's literal; otherwise the item's own format 1 VALUE.
        // (Exactly ValueInitializer.InitializerFor's `effRaw = rawOverride ?? item.RawValue` — one recipe, both lanes.)
        string? effRaw = rawOverride ?? item.RawValue;
        if (useValues && effRaw is { } raw)
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
            // Boolean members of a Tier-B class contribute their zero-padded VALUE image.
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
                useValues && effRaw is { } fraw && !fraw.StartsWith('"')
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
    /// allocated. <c>sending: false</c> for the same reason the two <c>FromImage</c> sites below use it
    /// (kb/Work PB230): this is the RECEIVING side of a group decode — the program-visible sending
    /// reference was the write that produced the image, not this redistribution of it into members.</summary>
    private static string TableElementFromImage(DataItem d) =>
        d.IsGroup ? "(__e, __x) => { __e.FromImage(__x); return __e; }"
        : !d.StoreAsImage && d.Pic is { HasImageByteForm: true }
            ? (d.Pic.IsFloat
                ? $"(__e, __x) => ({d.Pic.ClrType}){RuntimeApi.NumParseImageFloat("__x", d.ProfileName)}"
                : $"(__e, __x) => ({d.Pic.ClrType}){RuntimeApi.NumParseImage("__x", d.ProfileName, sending: false)}")
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
        if (group.GroupUsage is GroupUsage.National) EmitNatMethods(group, w);
    }

    /// <summary>A NATIONAL GROUP's elementary face (ISO §13.18.29.4 GR2b — "a national group is treated as though
    /// it were an elementary data item of usage national and class and category national described with PICTURE
    /// N(m), where m is the length of the group"; D20/PB79, kb/Work PB327): <c>AsNat()</c> is the group's m
    /// national CHARACTER positions and <c>FromNat</c> stores m of them back. The BYTE form stays
    /// <c>AsImage()</c>/<c>FromImage()</c> (the record / REDEFINES / file image), exactly as it does for a bit
    /// group — two faces of ONE composition, so they cannot disagree.
    /// <para>⛔ DERIVED FROM THE IMAGE, never a second walk over the children: §13.18.29.3 SR3 requires that "all
    /// elementary items subordinate to the subject of the entry shall be explicitly or implicitly described as
    /// usage national", so every byte pair of a national group's image IS a national character position and the
    /// two faces are exactly <c>CobolBits.NatBytes</c> and its inverse. That is why this needs no <c>BitAreaOf</c>
    /// analogue: the bit face exists because §8.5.1.6.3 lets same-level bit items SHARE a byte, and national
    /// positions never share one.</para>
    /// <para>The receiving side fits in POSITIONS before serializing (<c>CobolString.Store</c> then
    /// <c>NatBytes</c>): padding the BYTES with 0x20 would manufacture U+2020 characters instead of the national
    /// spaces §14.9.25.4's national move requires — the identical trap <c>CobolBits.NatWriteWindow</c> documents.</para></summary>
    private static void EmitNatMethods(DataItem group, CodeWriter w)
    {
        // m — the as-if PICTURE N(m) length, THE ONE reader of it (DataItem.AsIfPic reads the same ImageWidth,
        // which stays the CARRIER width: national leaves contribute character positions to it, never bytes).
        int m = group.ImageWidth;
        w.Line($"public readonly string AsNat() => {RuntimeApi.NatReadWindow("AsImage()", "0", $"{m}")};");
        w.Line($"public void FromNat(string __n) => FromImage({RuntimeApi.NatBytes(RuntimeApi.StrStore("__n", $"{m}"))});");
    }

    /// <summary>A BIT GROUP's elementary face (§13.18.29.4 GR1b — "treated as though it were an elementary data item
    /// … PICTURE 1(m), where m is the bit length of the group"; D20/PB79): <c>AsBits()</c> is the group's m
    /// boolean positions and <c>FromBits</c> distributes m boolean positions back to the members. The packed byte
    /// form stays <c>AsImage()</c>/<c>FromImage()</c> (the record / REDEFINES / file image); the two agree because
    /// both compose the same area.
    ///
    /// <para>⛔ BOTH DIRECTIONS PLACE THE MEMBERS BY <see cref="BitLayout.StartBitWithin"/>, and the width is
    /// <see cref="BitLayout.ExtentBits"/> — not a sum of the members' <see cref="BitLayout.RunBits"/>, which is
    /// what both halves used to do. A sum is only right while every member is "immediately following … an item of
    /// the SAME LEVEL" (§8.5.1.6.3's one byte-sharing case); a member at a different level number starts at "the
    /// first bit position of the first available byte" and the skipped bits are implicit filler. MEASURED at
    /// 62a36759 on `01 BG GROUP-USAGE BIT. 05 B1 PIC 1(2) VALUE B"11". 03 B2 PIC 1(2) VALUE B"11".`:
    /// <c>DISPLAY BG</c> printed FOUR boolean positions while <c>FUNCTION LENGTH(BG)</c> — which reads the same
    /// walk through <c>AsIfPic</c> — answered TEN. The elementary face and the length disagreed about the same
    /// group.</para></summary>
    private static void EmitBitMethods(DataItem group, CodeWriter w)
    {
        w.Line($"public readonly string AsBits() => {BitAreaOf(group, BitCarrierOf)};");
        using (w.Block("public void FromBits(string __b)"))
        {
            // pad with boolean zeros / truncate to m (§14.6.8.6)
            w.Line($"__b = {RuntimeApi.StrStoreBoolean("__b", $"{BitLayout.ExtentBits(group)}", justifiedRight: false)};");
            foreach (var m in BitMembers(group))
            {
                int at = BitLayout.StartBitWithin(group, m);
                EmitRunMemberFromBits(m, "__b", at < 0 ? 0 : at, w);
            }
        }
    }

    /// <summary>The members of <paramref name="group"/> that OCCUPY storage — a redefining child overlays its
    /// target and adds none (§13.18.44), and a non-data child (a condition-name entry) is not a member at all.
    /// The one member list for every bit composition below, so they cannot walk different populations.</summary>
    private static IEnumerable<DataItem> BitMembers(DataItem group) =>
        group.Children.Where(c => c.RedefinesTargetName is null && (c.IsGroup || c.IsElementary));

    /// <summary>⛔ THE ONE §8.5.1.6.3 AREA COMPOSITION for a bit group: each member's boolean carrier placed at
    /// the bit position the walk gives it, with the implicit-filler zeros between and after them, for the
    /// group's whole <see cref="BitLayout.ExtentBits"/> — §13.18.29.4 GR1b's "PICTURE 1(m), where m is the bit
    /// length of the group". <paramref name="carrierOf"/> is what a member contributes: its FIELD for the
    /// emitted <c>AsBits()</c> face, its INITIAL value for the compile-time image seed, and the two therefore
    /// cannot place the same member at two different positions.
    ///
    /// <para>The filler positions are §8.5.1.6.3's — "implicit filler bit positions are generated … as needed to
    /// advance alignment to a required natural boundary for the next item within that group" and, at the end,
    /// "as needed to increase the number of bits to fill an integral number of characters". They are ZEROS
    /// because that is the boolean initial state §13.18.63 gives a position no VALUE reaches, and
    /// <c>CobolBits.Pack</c>'s own contract already zero-fills a trailing partial byte.</para></summary>
    private static string BitAreaOf(DataItem group, Func<DataItem, string> carrierOf)
    {
        var parts = new List<string>();
        int at = 0;
        foreach (var m in BitMembers(group))
        {
            int start = BitLayout.StartBitWithin(group, m);
            if (start < 0) start = at;      // an unmodelled overlay chain — keep the composition total
            if (start > at) parts.Add(EmitText.CsLiteral(new string('0', start - at)));
            parts.Add(carrierOf(m));
            at = start + BitLayout.RunBits(m);
        }
        int total = BitLayout.ExtentBits(group);
        if (total > at) parts.Add(EmitText.CsLiteral(new string('0', total - at)));
        return parts.Count > 0 ? string.Join(" + ", parts) : "\"\"";
    }

    /// <summary>One bit member's INITIAL boolean carrier — the compile-time twin of <see cref="BitCarrierOf"/>,
    /// for the image seed: a nested bit group composes its own area (the same <see cref="BitAreaOf"/> walk, so
    /// §8.5.1.6.3 applies "within that group" at every level), a bit leaf stores its VALUE into its declared
    /// boolean positions (§14.6.8.6 — zero pad, truncate right) or takes the all-zero boolean initial state, and
    /// a fixed-OCCURS member repeats that for every occurrence (§13.18.63.4 GR9).</summary>
    private string InitialBitCarrierOf(DataItem m, bool useValues)
    {
        string one =
            m.IsGroup ? BitAreaOf(m, c => InitialBitCarrierOf(c, useValues))
            // A member the binder failed to describe still has to occupy its positions, or every member after
            // it is displaced; WidthBits is asked ONLY here, because for a group it is a whole ExtentBits walk.
            : m.Pic is not { } pic ? EmitText.CsLiteral(new string('0', BitLayout.WidthBits(m)))
            : !useValues || m.RawValue is not { } raw ? EmitText.CsLiteral(new string('0', pic.Length))
            // A FIGURATIVE operand is its one character repeated to the item's boolean positions
            // (§8.3.3.6.4 GR2; GR4 makes the ZERO format "one or more of the boolean character '0'"). Asked of
            // the ONE figurative service so the bit lane cannot disagree with every other VALUE lane about it.
            : vals.FigurativeInitializer(raw, pic)
              ?? RuntimeApi.StrStoreBoolean(EmitText.CsLiteral(CobolLiteral.Decode(raw)), $"{pic.Length}",
                                            justifiedRight: false);
        return m.Occurs is { } n and > 1 ? RuntimeApi.StrRepeat(one, $"{n}") : one;
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
        // ⛔ A NATIONAL LEAF IMAGES AS ITS BYTES (kb/Work PB327): two per character position, high-order first
        // (ISO §13.18.60.4 GR8 leaves the size to the implementor — D-N1 pins two, UTF-16BE), through the ONE
        // serializer CobolBits.NatBytes that the Tier-B window, the EXTERNAL/BASED cell seed and CONVERT's
        // raw-storage channel already ride. Concatenating the occurrences BEFORE serializing is the same string
        // as serializing each and concatenating, so the OCCURS arm needs no separate spelling.
        : f.NatLeaf is not null
            ? RuntimeApi.NatBytes(f.Occurs == 0 ? f.Name : $"string.Concat({f.Name})")
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
        // ⛔ THE INVERSE OF AsImageOf's national arm (kb/Work PB327): decode the UTF-16BE byte pairs back to the
        // leaf's character positions through CobolBits.NatReadWindow — the inverse CobolBits.NatBytes names, never
        // a second decoder. A pair the image is too short to hold decodes as the NATIONAL space, which is exactly
        // §14.9.30.4 GR15's fill for a national record area ("a trailing space is defined to be the national space
        // character") — the connector's byte-level pad and this decode agree on it by construction.
        if (f.NatLeaf is { } nat)
        {
            int pos = NationalWindow.PositionsOf(nat)!.Value;
            if (f.Occurs == 0)
            {
                w.Line($"{f.Name} = {RuntimeApi.NatReadWindow("__s", $"{off}", $"{pos}")};");
                return;
            }
            using (w.Block($"for (int __i = 0; __i < {f.Occurs}; __i++)"))
                w.Line($"{f.Name}[__i] = {RuntimeApi.NatReadWindow("__s", $"{off} + __i * {RuntimeApi.BytesPerNational * pos}", $"{pos}")};");
            return;
        }
        if (f.Occurs == 0)
        {
            w.Line(f.IsGroupStruct
                ? $"{f.Name}.FromImage(__s.Substring({off}, {f.Width}));"
                : f.NumLeaf is { } leaf
                // A FLOAT leaf decodes through the IEEE bit-reinterpretation lane — the Int128 lane's cast
                // would numerically CONVERT the parsed integer (kb/Work PB164 wave 2).
                ? $"{f.Name} = ({leaf.Pic!.ClrType}){(leaf.Pic!.IsFloat
                        ? RuntimeApi.NumParseImageFloat($"__s.Substring({off}, {f.Width})", leaf.ProfileName)
                        : RuntimeApi.NumParseImage($"__s.Substring({off}, {f.Width})", leaf.ProfileName, sending: false))};"
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
                        : RuntimeApi.NumParseImage($"__s.Substring({off} + __i * {elem}, {elem})", l.ProfileName, sending: false))};"
                : $"{f.Name}[__i] = __s.Substring({off} + __i * {elem}, {elem});");
    }
}
