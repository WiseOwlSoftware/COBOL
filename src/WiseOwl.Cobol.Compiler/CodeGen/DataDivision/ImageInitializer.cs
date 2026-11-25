// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Common;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.CodeGen.Emit;

namespace WiseOwl.Cobol.CodeGen;

/// <summary>THE COMPILE-TIME INITIAL-IMAGE COMPOSER (DESIGN §14.4; DESIGN-architecture-review §8.3, kb/Work PB2337): the C#
/// string expression a BYTE-ADDRESSED shared area starts at (a Tier-B REDEFINES backing, an EXTERNAL run-unit cell, a
/// BASED/ADDRESS-OF cell, an OO backing) and the seeds of such a cell's variable-length half. It is the compile-time twin
/// of the run-time image <see cref="GroupImageCodec"/> emits, member for member, so a backing starts at the bytes its group
/// would image; the §2023:8.5.1.6.3 bit-group area it composes is <see cref="BitImageCodec.BitAreaOf"/>, the one placement
/// the emitted <c>AsBits()</c> face uses too. It reads VALUE clauses through <see cref="ValueInitializer"/> and emits no
/// member of its own. Wired by <see cref="DataEmitter"/>.</summary>
internal sealed class ImageInitializer(EmitContext ctx, ValueInitializer vals)
{
    /// <summary>The C# string-expression for an item's INITIAL character image (used to seed a Tier-B backing from
    /// the canonical's VALUE): a group concatenates its leaves' images; an elementary item formats its VALUE (numeric
    /// → <c>CobolNum.FormatDisplay</c>; alphanumeric/edited → the stored string; figurative/default per width). A
    /// fixed-OCCURS entry's image repeats <c>Occurs</c> times — every occurrence takes the VALUE (ISO §2023:13.18.63.4 GR9;
    /// the recursion runs through this wrapper, so nested OCCURS repeat too).
    /// <para><paramref name="useValues"/> false suppresses every VALUE and composes the CATEGORY-DEFAULT initial
    /// image instead — the §2023:13.18.63.4 GR4a shape, where "a PLAIN external item's VALUE takes effect only during
    /// INITIALIZE", so its run-unit cell must seed blank/zero rather than VALUE-composed. It is the SAME
    /// composition either way, which is the point: a byte-form leaf's default is the ZERO ENCODING of its pinned
    /// form (radix-2 / BCD / IEEE / INDEX bytes), never a run of ASCII '0' characters. The EXTERNAL path used to
    /// hand-roll that default in the BINDER (<c>CallInitialImage</c>, a second seeder with a char-fill model that
    /// predated the byte forms); one seeder, one flag (kb/Work PB164 — one-mechanism-per-job).</para></summary>
    /// <param name="subs">The OCCURRENCE CONTEXT — the subscripts of every OCCURS level entered on the way down
    /// from the record root, most inclusive first (the twin of <see cref="ValueInitializer.FieldInit"/>'s, so the
    /// two lanes key a Format 2 (table) VALUE the same way).</param>
    public string ImageInitOf(DataItem item, bool useValues = true, Subscripts subs = default,
        SeedRecipe recipe = SeedRecipe.InitialState)
    {
        recipe = ValueInitializer.RecipeFor(item, recipe);   // a CONSTANT RECORD root: §2023:13.18.15.4 GR1 (kb/Work PB1233)
        // ⛔ A FORMAT 2 (table) VALUE GIVES EACH OCCURRENCE ITS OWN IMAGE — §2023:13.18.63.4 GR12 ("A format 2 VALUE
        // clause initializes a table element to the value of literal-1"), GR13 (cyclic reuse under TO), GR14
        // (no TO = fill to the maximum), GR15 (a later FROM wins on overlap). This method read only
        // item.RawValue, which is null for a table VALUE (the two carriers are mutually exclusive on DataItem),
        // and the StrRepeat below then seeded EVERY occurrence with the same VALUE-less default: the table
        // VALUE was silently DISCARDED for every image-stored leaf — a Tier-B REDEFINES member, an EXTERNAL
        // cell, a BASED record, an OO backing. Measured on 7ed14832 with
        // `05 B PIC 9(4) COMP OCCURS 2 VALUE 12 FROM (1) TO (2).` inside a REDEFINES-aliased group: the group
        // image came back `65 65 00 00 00 00 67 67`, the seed simply absent (kb/Work PB208).
        // The per-occurrence walk is now the SAME shape the record-struct lane uses — one composition per
        // occurrence against its own subscript tuple — so it covers a SUBORDINATE-item table VALUE and a
        // multi-dimension odometer without a second rule being written here (kb/Work PB505). An occurrence
        // outside every FROM..TO range takes the same VALUE-less image it always took.
        // Composed once per occurrence RUN, never once per occurrence (kb/Work PB1722).
        return item.Occurs is { } occ and > 0
            ? OccurrenceRunEmit.Image(OccurrenceRunsOf(item, useValues, subs, occ),
                o => ImageInitOfOne(item, useValues, subs.With(o), recipe))
            : ImageInitOfOne(item, useValues, subs, recipe);
    }

    /// <summary>The runs of a fixed table's occurrences that seed alike: the table VALUE's runs where VALUEs apply
    /// (<see cref="TableValueRuns.Of"/>), else one uniform run — the twin of the record-struct lane's, so the
    /// image and bit lanes compose the same occurrences once.</summary>
    private static IReadOnlyList<OccurrenceRun> OccurrenceRunsOf(DataItem item, bool useValues, Subscripts subs, int occ) =>
        useValues ? TableValueRuns.Of(item, subs, occ) : OccurrenceRunEmit.Uniform(occ);
    /// <summary>⛔ THE SEED OF A SHARED-STORAGE BACKING ROOTED AT A MEMBER OF A GROUP THAT CARRIES A VALUE
    /// (kb/Work PB5851): the backing's storage is <paramref name="over"/> — the positions of the enclosing group's
    /// §2023:13.18.63.4 GR5 area that lie over <paramref name="canonical"/>, handed down by the one positional walk
    /// (<see cref="GroupValueSlicer.ComposedInit"/>) — and never the canonical's own description, which carries no
    /// VALUE (§2023:13.18.63.3 SR13 bars one below a group VALUE). Seeding it from that description is what lost
    /// `01 G VALUE "ABCD". 05 A PIC X(4). 05 B REDEFINES A PIC X(4).` to spaces at every edition.
    /// <para>The canonical of a REDEFINES class never carries OCCURS (§2023:13.18.44.3 SR5), so this is one
    /// occurrence: the occurrence walk of <see cref="ImageInitOf"/> is not needed and not taken.</para></summary>
    public string BackingSeedOver(DataItem canonical, GroupArea over) =>
        ImageInitOfOne(canonical, useValues: true, default, SeedRecipe.InitialState, over);

    /// <param name="subs">The subscript tuple of THIS occurrence — the twin of
    /// <see cref="ValueInitializer.InitializerFor"/>'s parameter of the same name, so the image lane and the
    /// native-field lane compose one occurrence from the same literal text (kb/Work PB208/PB505).</param>
    /// <param name="over">The enclosing group's VALUE area over this item, when one governs it
    /// (<see cref="BackingSeedOver"/>); null everywhere else.</param>
    private string ImageInitOfOne(DataItem item, bool useValues, Subscripts subs, SeedRecipe recipe, GroupArea? over = null)
    {
        // ⛔ A POINTER-CLASS LEAF CONTRIBUTES RESERVED BYTES AND NO IMAGE (kb/Work PB231 — the pointer third).
        // Its value is a managed reference held in the area's MANAGED SLOT (Place.SlotWindow / StorageCell.SlotAt),
        // whose unwritten state IS null — §2023:14.9.3.4 GR9's "data items of class object or class pointer in the
        // allocated storage are initialized to null" and §2023:13.18.63.4's "data items of class message-tag, class
        // object, and class pointer are initialized to null" both hold with no seeding here. What the BYTE image
        // owes the shared area is exactly the item's storage extent of placeholder positions, so §2023:14.9.3.4 GR3's
        // byte quantity and every FOLLOWING member's class offset are what a byte-addressed area says they are —
        // without this the pointer contributed its PICTURE-less zero-width nothing and displaced the rest.
        // Placed beside the national coding below because it is the same kind of decision: how the member's
        // value carrier relates to the bytes it occupies.
        if (SlotWindow.CarriedBySlot(item)) return SlotSeedOf(item);
        // ⛔ A DYNAMIC-LENGTH LEAF CONTRIBUTES NO BYTES AT ALL (kb/Work PB1026): its content rides the cell's
        // dynamic slot (Place.DynSlotWindow), and it occupies zero positions of the area's fixed run — its
        // ByteWidth, §2023:8.5.1.12.3's "all dynamic-length elementary items are considered to be of zero length". Its
        // VALUE, if any, is the slot's seed (CellDynSeeds). A one-character fill here displaced every following
        // member of an ADDRESS-OF-taken variable-length group by one position.
        if (DynSlotWindow.CarriedBySlot(item)) return "\"\"";
        string image = CarrierInitOfOne(item, useValues, subs, recipe, over);
        // ⛔ A NATIONAL LEAF'S SEED IS ITS BYTES, NOT ITS CARRIER (kb/Work PB231). Everything this method seeds
        // is a BYTE-ADDRESSED shared area — a Tier-B REDEFINES backing, an EXTERNAL run-unit cell, a
        // BASED/ADDRESS-OF cell, an OO backing — and a national character position occupies TWO of those bytes
        // (§2023:13.18.60.4 GR8 leaves the size to the implementor; D-N1 pins two, UTF-16BE). Applied HERE, once,
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

    /// <summary>⛔ THE ONE SEEDER OF A CELL'S VARIABLE-LENGTH HALF (kb/Work PB1026, PB1042) — the twin of
    /// <see cref="ImageInitOf"/> for the component slots <see cref="ImageInitOfOne"/> leaves out, one chained call per
    /// component of <paramref name="root"/>'s scope in ordinal order (<see cref="CellComponents.Of"/>, the order the
    /// binder numbered them in): a <c>.SeedDyn(ordinal, content)</c> for a dynamic-length item whose initial content is
    /// not empty — §2023:8.6.4 via the VALUE clause's own recipe (<see cref="ValueInitializer.InitializerFrom"/>'s
    /// dynamic-length arm: MOVE-like, truncated to the maximum size; absent a VALUE the initial length is zero, which
    /// an unwritten slot already is) — and a <c>.SeedDynTable(ordinal, table)</c> for EVERY dynamic-capacity table,
    /// which has no unwritten state: its FROM capacity of element cells, each seeded by this same method one scope
    /// down (§2023:8.5.1.9.1). Empty when the scope has no component, so every cell without one is emitted as before.</summary>
    /// <param name="useValues">False for a PLAIN external record (§2023:13.18.63.4 GR4a): no VALUE seeds the initial
    /// state — though the occurrences a statement creates under the INITIALIZED phrase are still composed as an
    /// INITIALIZE … TO VALUE composes them (§2023:8.5.1.9.5).</param>
    /// <param name="firstOrdinal">The ordinal of the root's first component; null for the root's class ordinal
    /// (<see cref="DataItem.ClassDynOrdinal"/>), and 0 for a root no storage class holds — a .NET host's published record
    /// of a group formal or RETURNING item (kb/Work PB3273), whose cell is its own scope.</param>
    public string CellDynSeeds(DataItem root, bool useValues = true, int? firstOrdinal = null) =>
        string.Concat(CellScopeSeeds(root, useValues, default, firstOrdinal));

    private IEnumerable<string> CellScopeSeeds(DataItem scope, bool useValues, Subscripts subs, int? firstOrdinal = null)
    {
        int ordinal = firstOrdinal ?? (scope.IsDynamicTable ? 0 : scope.ClassDynOrdinal);
        foreach (var (c, _, fixedSubs) in CellComponents.Of(scope))
        {
            int k = ordinal++;
            if (c.IsDynamicTable) yield return RuntimeApi.CellSeedDynTable(k, CellTable(c, useValues, subs.With(fixedSubs)));
            else if (useValues && c.RawValue is not null)
            {
                // The slot of a NATIONAL item holds its UTF-16BE image (StorageCell.DynAt; kb/Work PB3441), so the seed is that.
                string initial = vals.InitializerFrom(c, c.RawValue);
                yield return $".{nameof(WiseOwl.Cobol.Runtime.StorageCell.SeedDyn)}({k}, {(c.ImageUnit > 1 ? RuntimeApi.NatBytes(initial) : initial)})";
            }
        }
    }

    /// <summary>A cell's dynamic-capacity table, constructed by THE ONE construction from the table's own OCCURS clause
    /// (<see cref="ValueInitializer.DynTableNew"/>, shared with the record-struct lane). Each occurrence is an element
    /// cell: the element's image (<see cref="ImageInitOf"/> at that occurrence's subscript) and its own components'
    /// seeds. An occurrence a STATEMENT creates under the INITIALIZED phrase is composed with VALUEs whatever
    /// <paramref name="useValues"/> says (§2023:8.5.1.9.5: "as though … INITIALIZE … WITH FILLER ALL TO VALUE THEN TO
    /// DEFAULT"); a table VALUE (Format 2) applies only where VALUEs do.</summary>
    private string CellTable(DataItem table, bool useValues, Subscripts subs)
    {
        string Element(int o, SeedRecipe recipe)
        {
            bool values = useValues || recipe is SeedRecipe.Initialize;
            return $"new StorageCell {{ Ref = {ImageInitOf(table, values, subs.With(o), recipe)} }}"
                + string.Concat(CellScopeSeeds(table, values, subs.With(o)));
        }
        return ValueInitializer.DynTableNew(table, nameof(WiseOwl.Cobol.Runtime.StorageCell), Element, SeedRecipe.InitialState,
            useValues, subs);
    }

    /// <summary>One MEMBER's contribution to its group's compile-time image seed — the COMPILE-TIME twin of
    /// <c>GroupImageCodec.AsImageOf</c>, arm for arm, so the seed a Tier-B backing starts at and the image
    /// <c>AsImage()</c> composes at run time are the same bytes (kb/Work PB584).
    /// <para>A §2023:8.5.1.6.3 bit RUN images as its PACKED bits: the run's LEADER packs every member's INITIAL
    /// carrier concatenated (the compile-time twin of <c>BitImageCodec.BitCarrierOf</c>) and a CONTINUATION contributes
    /// nothing — which is what makes a MIXED bit/character group's seed exactly as wide as the group instead of
    /// one byte per bit member. Everything else recurses through <see cref="ImageInitOf"/>, so the national
    /// coding and the pointer-slot reservation ride <see cref="ImageInitOfOne"/> as they do for every caller.</para>
    /// <para>⚠ The walk is over <c>item.Children</c> rather than <c>PhysicalModel.PhysicalChildrenOf</c>, and the
    /// reason is measured: <c>DataBinder.AssignClassOffsets</c> marks every DESCENDANT of a redefines class
    /// <c>IsCanonical = false</c>, so the physical walk answers EMPTY for the children of a Tier-B canonical —
    /// the exact group this seed exists for. What the two composers must share is the RUN LAW, and they now do
    /// (<see cref="BitLayout.RunsOf"/>).</para></summary>
    private string InitImageOfMember(DataItem c, BitRunMap runs, bool useValues, Subscripts subs, SeedRecipe recipe) =>
        runs.RunLedBy(c) is { } run
            ? RuntimeApi.BitsPack(string.Join(" + ", run.Select(m => InitialBitCarrierOf(m, useValues, subs))),
                                  $"{BitLayout.RunBitsOf(run)}")
        : runs.IsInRun(c) ? "\"\""
        : ImageInitOf(c, useValues, subs, recipe);

    /// <summary>The item's initial image in its VALUE CARRIER's own units — see <see cref="ImageInitOfOne"/>,
    /// which applies the storage coding on top (kb/Work PB231).</summary>
    private string CarrierInitOfOne(DataItem item, bool useValues, Subscripts subs, SeedRecipe recipe, GroupArea? over)
    {
        // ⛔ AN ENCLOSING GROUP'S AREA GOVERNS THIS ITEM (kb/Work PB5851, BackingSeedOver): its positions are the
        // item's storage "without consideration for the individual elementary or group items" (§2023:13.18.63.4 GR5),
        // so neither the item's own description nor its members' are consulted. A boolean leaf stores its positions
        // through its usage (BooleanImageOf); a national leaf's characters become bytes in ImageInitOfOne, as every
        // national leaf's do.
        if (over is { } governing)
            return item.IsGroup ? AreaImageOf(item, governing)
                : item.Pic!.Category is PicCategory.Boolean ? BooleanImageOf(governing.CsExpression(), item.Pic)
                : governing.CsExpression();
        if (item.IsGroup)
        {
            // ⛔ A GROUP-LEVEL VALUE INITIALIZES THE AREA (ISO §2023:13.18.63.4 GR5 — "the group area is initialized
            // without consideration for the individual elementary or group items contained within this group"),
            // so it REPLACES the member-wise composition below rather than being composed alongside it. The one
            // area rule is GroupValueSlicer.AreaOf, shared with the record-struct lane.
            //
            // ⚠ THIS ARM WAS MISSING, and it was a live silent wrong answer on LEGAL source (kb/Work PB184's
            // sibling sweep). ImageInitOf is THE seeder for every character-image backing since the PB164
            // consolidation — Tier-B REDEFINES backings (RecordStructEmitter / PhysicalModel), EXTERNAL run-unit
            // cells (DataEmitter.ExternalCellSeed), BASED/ADDRESS-OF cells (ProgramEmitter / PtrEmitter) and the
            // OO backings — and it walked straight past the group's own RawValue into the members. Measured on
            // 99bc08c8: `01 G VALUE "ABCD". 05 A PIC X(2). 05 B PIC X(2). 01 R REDEFINES G PIC X(4).` left A, B
            // and R all SPACES, while the identical group WITHOUT the REDEFINES initialized "AB"/"CD" — the
            // whole VALUE lost to nothing but the presence of an alias. §2023:13.18.63.3 SR12 bars a VALUE in the
            // redefinING entry, never in the redefined one, so that program is conforming.
            // (The seventh instance of the two-arm-dispatch shape: the slicer's arm was written, the codec's
            // was not, and only the slicer's lane was ever tested.)
            //
            // ⛔ The UNIT is not restated here. It used to be an exclusion keyed on GROUP-USAGE BIT — a
            // predicate NARROWER than the fact it protects — and then, once the exclusion moved into the area
            // rule, a refusal. Neither is here now: GroupValueSlicer.AreaOf answers with the area AND its unit,
            // so this lane cannot disagree with the record-struct lane about what a bit group's area is
            // (kb/Work PB207).
            if (useValues && recipe is SeedRecipe.InitialState && GroupValueSlicer.AreaOf(item, ctx, subs) is { } area)
                return AreaImageOf(item, area);

            // ⛔ A BIT GROUP'S IMAGE IS ITS PACKED AREA, NOT ITS MEMBERS' IMAGES CONCATENATED. Each bit member
            // images as its own ceil(n/8) characters, and §2023:8.5.1.6.3 makes same-level bit members SHARE bytes —
            // so the concatenation is wider than the group. MEASURED at b7004e6b on
            // `01 K GROUP-USAGE BIT. 05 K1 PIC 1(2) VALUE B"11". 05 K2 PIC 1(2) VALUE B"01".
            //  01 KV REDEFINES K PIC X(1).`: K2 read back as `00` and the backing byte was 0xC0, because the
            // 2-character composition was truncated to the group's 1-character width and K2's byte fell off.
            // The composition is BitImageCodec.BitAreaOf's — the same §2023:8.5.1.6.3 placement the VALUE arm above and
            // AsBits()/FromBits use.
            if (item.GroupUsage is GroupUsage.Bit)
                return RuntimeApi.BitsPack(BitImageCodec.BitAreaOf(item, m => InitialBitCarrierOf(m, useValues, subs)),
                                           $"{BitLayout.ExtentBits(item)}");

            // ⛔ THE SEED COMPOSES OVER THE PHYSICAL FIELD LIST, NOT OVER item.Children (kb/Work PB584). A MIXED
            // bit/character group is the case that proves it: §2023:8.5.1.6.3 puts "an elementary bit data item
            // immediately following an elementary bit data item or bit group item of the same level" at the next
            // BIT position, so consecutive same-level bit members SHARE a byte, and only PhysicalModel computes
            // those runs. Walking the children instead gave each bit member its OWN ceil(n/8) characters, so the
            // composition was WIDER than the group and every member after the first run shifted. MEASURED at
            // f2bbf098b on `01 CTL. 05 H1 PIC 1(4) USAGE BIT VALUE B"0100". 05 H2 PIC 1(4) USAGE BIT VALUE
            // B"0001". 05 H3 PIC X(1) VALUE "B". 01 CV REDEFINES CTL PIC X(2).`: the members read back
            // [0100][0000][DLE] and CV's bytes were 0x40 0x10, where §2023:8.5.1.6.3's one run gives 0x41 ('A') and
            // §2023:13.18.44.4 GR1's storage association then puts "B" in CV(2:1). The identical group WITHOUT the
            // alias — the record-struct lane, which already walks PhysicalChildrenOf — gave [0100][0001][B].
            // Two composers for one image; both now ask the ONE run law, so a run cannot be resolved two ways.
            // ⚠ What they share is the LAW and not the field list: `PhysicalModel.PhysicalChildrenOf` answers
            // EMPTY for the children of a Tier-B canonical — `DataBinder.AssignClassOffsets` marks every
            // DESCENDANT of a redefines class `IsCanonical = false` — and a Tier-B group is the one this seed
            // exists for. So the walk stays over `item.Children`, and a REDEFINING child contributes nothing
            // because its target already composed that storage (the filter below, as before).
            // The run map is computed over ALL the children, redefining ones included, exactly as
            // `PhysicalModel` computes it — `BitLayout.RunsOf` excludes them itself, so the two agree by
            // construction rather than by both remembering to filter first.
            var runs = BitLayout.RunsOf(item.Children);
            var parts = item.Children.Where(c => (c.IsGroup || c.IsElementary) && c.RedefinesTargetName is null)
                .Select(c => InitImageOfMember(c, runs, useValues, subs, recipe));
            return item.Children.Count > 0 ? "(" + string.Join(" + ", parts) + ")" : "\"\"";
        }
        var pic = item.Pic!;
        // ⛔ THE ONE READER, shared with ValueInitializer.InitializerFor: the Format-1 VALUE or this occurrence's
        // Format-2 literal (DataItem.ValueAt) — one recipe, both lanes.
        string? effRaw = item.ValueAt(subs);
        if (useValues && effRaw is { } raw)
        {
            // ⛔ THE BOOLEAN ARM IS FIRST, AND THE ORDER IS THE FIX (kb/Work PB584). It used to sit BELOW the
            // generic figurative arm, so `PIC 1(4) USAGE BIT VALUE ZERO` seeded FOUR carrier characters where
            // the packed image is ONE — the figurative return knows the item's boolean POSITIONS and nothing
            // about its storage. MEASURED at f2bbf098b on `01 B PIC 1(4) USAGE BIT VALUE ZERO. 01 BV REDEFINES
            // B PIC X(1).`: the byte came back 0x30 (the character '0') where §2023:13.18.60.4 GR5's packed image of
            // four zero bits is 0x00, and B itself then read [0011]. One arm, one reader: the CARRIER is
            // ValueInitializer.BooleanCarrierOf's answer for every lane, and the USAGE decides only whether
            // those positions are packed.
            if (pic.Category is PicCategory.Boolean) return BooleanImageOf(vals.BooleanCarrierOf(raw, pic), pic);
            if (vals.FigurativeInitializer(raw, pic) is { } fig && pic.Category is not PicCategory.Numeric) return fig;
            // A NUMERIC member contributes the BYTES of its VALUE through its own pinned byte form — zoned
            // digits for DISPLAY, radix-2 / BCD for BINARY / PACKED (V59). ⛔ ONE ENCODER, so the width is the
            // item's StorageWidth BY CONSTRUCTION (kb/Work PB188): the CCVS-leniency spelling that used to sit
            // here stored `pic.Length` CHARACTERS — the PICTURE's digit count — which for a byte-form member is
            // simply a different number (`PIC 9(4) COMP` is 4 digits and 2 bytes) and displaced every following
            // member of the image. The leniency itself survives as what it always meant: an alphanumeric literal
            // on a numeric item is read AS the numeric literal §2023:13.18.63.3 SR2 asked for (NC107A's
            // `PIC 999 VALUE "000"` under a REDEFINES), which is the SAME encode — hence one arm, not two.
            if (pic.Category is PicCategory.Numeric && !pic.IsFloat && vals.FigurativeInitializer(raw, pic) is null)
                return RuntimeApi.NumFormatImage(
                    EmitText.UnscaledAtScale(raw.StartsWith('"') ? CobolLiteral.Decode(raw) : raw, pic.Scale),
                    item.ProfileName);
            // A NUMERIC literal VALUE on a numeric-edited member contributes its EDITED image (§2023:13.18.63.3 SR6) —
            // for a format-2 (LOCALE) member a RUNTIME image (no compile-time image exists, §2023:13.18.40.5 GR11;
            // the ONE producer, RuntimeApi.LocaleEditCompose — PB64 T6; the EditCompose arm's mask deref would NRE).
            if (pic.LocaleEdit is not null && !raw.StartsWith('"')
                && ValueInitializer.TryParseNumeric(raw, out var luv, out int lsc))
                return RuntimeApi.LocaleEditCompose(pic, luv, lsc, item.BlankWhenZero);
            if (pic.Category is PicCategory.NumericEdited && !raw.StartsWith('"')
                && ValueInitializer.TryParseNumeric(raw, out var uv, out int sc))
                return EmitText.CsLiteral(RuntimeApi.EditCompose(uv, sc, pic.EditMask!,
                    item.BlankWhenZero, pic.CurrencyString, pic.DecimalPointIsComma, pic.EditingRules));
            // A Format-6 `ALL literal-1` is the literal repeated to the item's size (§2023:8.3.3.6.4 GR2) — the SAME reader
            // the record-struct lane asks (ValueInitializer.AllLiteralInitializer), or the image seeds the parse text.
            if (vals.AllLiteralInitializer(raw, pic) is { } allFill) return allFill;
            if (pic.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited)
                return RuntimeApi.StrStore(EmitText.CsLiteral(CobolLiteral.Decode(raw)), $"{pic.Length}");
            if (pic.Category is PicCategory.National)
                return RuntimeApi.StrStore(EmitText.CsLiteral(CobolLiteral.Decode(raw)), $"{pic.Length}");
        }
        // A FLOAT member's image is its IEEE window bytes (the Step D arm-1 dissolution — the ' '×Length
        // fall-through seeded ZERO characters for the PICTURE-less float shapes, Length 0): the VALUE
        // literal through the ONE recipe, else the zero encoding.
        if (pic.Category is PicCategory.Numeric && pic.IsFloat && !(useValues && effRaw is not null)
            && vals.NoValueSeed(item, pic, recipe) is { } floatBackground)
            return floatBackground;   // §2023:14.6.2.3.2 action 1 over a float window (owner decision R53)
        if (pic.Category is PicCategory.Numeric && pic.IsFloat)
            return RuntimeApi.NumFormatImageFloat(
                useValues && effRaw is { } fraw && !fraw.StartsWith('"')
                    && vals.FigurativeInitializer(fraw, pic) is null
                    ? ValueInitializer.RawValueAsFloat(fraw, pic) : "0d",
                item.ProfileName, pic.IsSingle);
        // ⛔ THE TIER-B IMAGE ARM'S NO-VALUE SEED — §2023:14.6.2.3.2 action 1's BACKGROUND on the image axis
        // (kb/Work PB152). This was the arm PB151 could not reach: the ALLOCATE fix landed with its fill decoder
        // PRIVATE to PtrEmitter, so this fall-through went on hardcoding ' ' / '0' with no way to consult the
        // OPTIONS model. It now asks the SAME choke point the native-field arm asks (InitialStateBackground),
        // one call apart, and a drift test asserts the two agree for every category × usage. The baselines below
        // are unchanged and remain what a no-clause program gets.
        if (vals.NoValueSeed(item, pic, recipe) is { } bg) return bg;
        return pic.Category is PicCategory.Numeric && !pic.IsFloat
            ? RuntimeApi.NumFormatImage("0L", item.ProfileName)
            // Boolean initial state — zeros (§2023:13.18.63). A USAGE BIT item's zeros are PACKED, so the seed is
            // ceil(n/8) zero BYTES rather than n zero characters (D19/PB43); the all-zero bit pattern makes the
            // packed form '\0' repeated, which is what the AsImage side would produce for the same value.
            : pic.Category is PicCategory.Boolean
                ? pic.Usage is Usage.Bit
                    ? $"new string('\\0', {BitLayout.Characters(pic.Length)})"
                    : $"new string('0', {pic.Length})"
                : $"new string(' ', {pic.Length})";
    }

    /// <summary>A boolean item's IMAGE from its boolean CARRIER — the one place the USAGE decides how the
    /// positions are stored (ISO §2023:13.18.60.4 GR5: a usage-bit item's positions are PACKED, high-order first,
    /// into <c>ceil(n/8)</c> characters through <c>CobolBits.Pack</c>, D19/PB43; a usage-display boolean item's
    /// positions ARE its characters). Separating it from the carrier is what keeps the VALUE rules
    /// (<see cref="ValueInitializer.BooleanCarrierOf"/>) out of the storage question — the two were entangled,
    /// and a figurative VALUE therefore reached the image as carrier characters (kb/Work PB584).</summary>
    private static string BooleanImageOf(string carrier, PicInfo pic) =>
        pic.Usage is Usage.Bit ? RuntimeApi.BitsPack(carrier, $"{pic.Length}") : carrier;

    /// <summary>A GROUP's storage image when a §2023:13.18.63.4 GR5 area — its own VALUE's, or the slice of an enclosing
    /// group's (<see cref="BackingSeedOver"/>) — is its content: the area's positions in the group's storage unit.
    /// <list type="bullet">
    ///   <item>A BIT group's area is m BOOLEAN POSITIONS (§2023:13.18.29.4 GR1b); its image is those positions PACKED —
    ///     ceil(m/8) characters through CobolBits.Pack, the ONE bit-order law (D19/PB43). The count is asked of
    ///     ExtentBits rather than of the area's own width: the two are equal by AreaOf's contract, and asking the
    ///     LAYOUT keeps the image the group's full width (Pack zero-fills the rest).</item>
    ///   <item>A NATIONAL group's area is national character positions (§2023:13.18.29.4 GR2b), and every backing this
    ///     codec seeds is byte-addressed, where a national position occupies two bytes (D-N1, UTF-16BE): the area is
    ///     stored through <c>NatBytes</c>, exactly as each national LEAF's characters are in
    ///     <see cref="ImageInitOfOne"/>. Without it `01 NH GROUP-USAGE NATIONAL VALUE N"KL". 05 NHA PIC N.
    ///     05 NHB PIC N. 01 NHR REDEFINES NH PIC X(4).` stored the two characters as two bytes and read back
    ///     NH as U+4B4C U+2020 (kb/Work PB5851's sweep).</item>
    ///   <item>Every other group's area is character positions, which are its bytes: §2023:13.18.63.3 SR14 makes every
    ///     item subordinate to an alphanumeric group carrying a VALUE usage DISPLAY.</item>
    /// </list></summary>
    private static string AreaImageOf(DataItem group, GroupArea area) =>
        area.Bits ? RuntimeApi.BitsPack(area.CsExpression(), $"{BitLayout.ExtentBits(group)}")
        : group.ImageUnit > 1 ? RuntimeApi.NatBytes(area.CsExpression())
        : area.CsExpression();

    /// <summary>A slot-carried leaf's positions in a freshly seeded byte-addressed area: the NULL pointer image
    /// (<see cref="SlotWindow.CarriesTokenImage"/>; ISO 14.9.3.4 GR9 and 13.18.63.4 initialize class pointer to
    /// null) or the object's placeholder (<see cref="ImageInitOfOne"/> - D-SLOT).</summary>
    private static string SlotSeedOf(DataItem item) =>
        SlotWindow.CarriesTokenImage(item) ? RuntimeApi.PointerNullImage() : GroupImageCodec.SlotPlaceholder(item.ByteWidth);

    /// <summary>One bit member's INITIAL boolean carrier — the compile-time twin of <c>BitImageCodec.BitCarrierOf</c>,
    /// for the image seed: a nested bit group composes its own area (the same <see cref="BitImageCodec.BitAreaOf"/> walk, so
    /// §2023:8.5.1.6.3 applies "within that group" at every level), a bit leaf stores its VALUE into its declared
    /// boolean positions (§2023:14.6.8.6 — zero pad, truncate right) or takes the all-zero boolean initial state, and
    /// a fixed-OCCURS member repeats that for every occurrence (§2023:13.18.63.4 GR9).</summary>
    private string InitialBitCarrierOf(DataItem m, bool useValues, Subscripts subs = default)
    {
        // ⛔ THE BIT CARRIER IS THE THIRD STORAGE LANE, AND IT DROPPED THE FORMAT 2 (table) VALUE TOO
        // (kb/Work PB505 — the same shape kb/Work PB208 found on the character-image lane). It read `m.RawValue`,
        // which is NULL for a table VALUE, and then repeated ONE occurrence carrier `Occurs` times, so every
        // occurrence of a USAGE BIT table inside a bit group came back all-zero. MEASURED before the fix on
        // `01 BG GROUP-USAGE BIT. 05 B PIC 1(4) OCCURS 3 VALUES ARE B"1010" B"0101" FROM (1). 01 BV REDEFINES
        // BG PIC X(2).`: [0000|0000|0000], where the SAME declaration without the alias — the record-struct
        // lane — gave [1010|0101|1010]. §2023:13.18.63.4 GR12/GR13 govern this lane exactly as they govern the other
        // two, so it asks DataItem.ValueAt with the occurrence tuple like they do.
        if (m.Occurs is not { } occ || occ <= 0) return OneBitCarrierOf(m, useValues, subs);
        // ⛔ THE OCCURRENCES SIT ONE STRIDE APART (§2023:13.18.1.4 GR2, kb/Work PB3156) — the compile-time twin of
        // BitImageCodec.BitCarrierOf: every occurrence is padded to its stride and the last one's padding cut off again, which is
        // BitLayout.TableBits. A table whose layout does not depend on where in a byte it begins (no ALIGNED subject
        // or member, nothing that starts a byte, kb/Work PB3167) has stride = width and needs neither.
        int filler = BitLayout.TrailingFillerBits(m);
        if (filler == 0)
            return OccurrenceRunEmit.Image(OccurrenceRunsOf(m, useValues, subs, occ), o => OneBitCarrierOf(m, useValues, subs.With(o)));
        string padding = EmitText.CsLiteral(new string('0', filler));
        string padded = OccurrenceRunEmit.Image(OccurrenceRunsOf(m, useValues, subs, occ),
            o => $"({OneBitCarrierOf(m, useValues, subs.With(o))} + {padding})");
        return RuntimeApi.StrStoreBoolean(padded, $"{BitLayout.TableBits(m, occ)}", justifiedRight: false);
    }

    /// <summary>ONE occurrence of a bit run member’s initial carrier — see <see cref="InitialBitCarrierOf"/>,
    /// which repeats or composes it per occurrence.</summary>
    private string OneBitCarrierOf(DataItem m, bool useValues, Subscripts subs)
    {
        if (m.IsGroup) return BitImageCodec.BitAreaOf(m, c => InitialBitCarrierOf(c, useValues, subs));
        // A member the binder failed to describe still has to occupy its positions, or every member after
        // it is displaced; WidthBits is asked ONLY here, because for a group it is a whole ExtentBits walk.
        if (m.Pic is not { } pic) return EmitText.CsLiteral(new string('0', BitLayout.WidthBits(m)));
        if (!useValues || m.ValueAt(subs) is not { } raw) return EmitText.CsLiteral(new string('0', pic.Length));
        // ⛔ THE VALUE RULES ARE ASKED OF THE ONE READER, NOT RESTATED HERE (kb/Work PB584). This arm used to be
        // `FigurativeInitializer(raw, pic) ?? StrStoreBoolean(Decode(raw), …)` — the FIGURATIVE arm and the
        // PLAIN-LITERAL arm, with §2023:8.3.3.6.4 GR2's Format-6 `ALL literal-1` arm missing, which the record-struct
        // lane (ValueInitializer.InitializerFor) has always had. MEASURED at f2bbf098b on
        // `01 BG GROUP-USAGE BIT. 05 K1 PIC 1(4) VALUE ALL B"1". 05 K2 PIC 1(4) VALUE ZERO. 01 BV REDEFINES BG
        // PIC X(1).`: K1 read back [0000] through the alias and [1111] without it — the two-arm shape, one arm
        // short, in the lane nothing but an alias reaches.
        return vals.BooleanCarrierOf(raw, pic);
    }
}
