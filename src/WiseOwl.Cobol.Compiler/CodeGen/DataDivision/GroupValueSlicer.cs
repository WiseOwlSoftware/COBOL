// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Common;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.CodeGen.Emit;

namespace WiseOwl.Cobol.CodeGen;

/// <summary>⛔ THE §2023:13.18.63.4 GR5 GROUP-VALUE AREA, <b>IN THE UNIT THE GROUP'S OWN STORAGE IS MEASURED IN</b> —
/// <paramref name="Text"/> is the area's contents and <paramref name="Bits"/> says whether those are BOOLEAN
/// POSITIONS (a bit group, §2023:13.18.29.4 GR1b) or CHARACTER positions (every other group).
///
/// <para>The unit travels WITH the area rather than being re-derived by each consumer, because the two consumers
/// — the record-struct lane (<see cref="GroupValueSlicer.ComposedInit"/>) and the character-image lane
/// (<see cref="ImageInitializer.ImageInitOf"/>) — must never disagree about it. Restating "is this a bit group?"
/// at a call site is how <c>ImageInitializer.ImageInitOfOne</c> came to carry a NARROWER copy of the previous
/// exclusion (kb/Work PB207); one producer, one answer.</para>
/// <para>⛔ <b>The area is never materialized at its full width</b> (kb/Work PB1722): it is
/// <paramref name="Head"/> (the literal's own positions) followed by <paramref name="Unit"/> repeated to
/// <paramref name="Width"/> — the figurative's one character, the ALL literal, or the GR7 pad — so a group VALUE
/// over a table of a hundred million positions is a few characters at compile time, its windows are compared by
/// phase (<see cref="SameWindow"/>), and the image lane repeats the unit at run time (<see cref="CsExpression"/>).
/// <paramref name="Unit"/> is reduced to its minimal period, so two windows of the repeated part are equal exactly
/// when their phases are.</para></summary>
internal readonly record struct GroupArea(string Head, string Unit, int Width, bool Bits)
{
    /// <summary>The area whose positions are <paramref name="head"/> (truncated to <paramref name="width"/>)
    /// followed by <paramref name="unit"/> repeated.</summary>
    public static GroupArea Of(string head, string unit, int width, bool bits) =>
        new(head.Length > width ? head[..width] : head, MinimalPeriod(unit), width, bits);

    /// <summary>The position at <paramref name="i"/> (0-based).</summary>
    public char At(int i) => i < Head.Length ? Head[i] : Unit[(i - Head.Length) % Unit.Length];

    /// <summary>The <paramref name="count"/> positions from <paramref name="at"/> as an area of their own;
    /// positions past <see cref="Width"/> read <paramref name="pad"/> (a bit member's implicit filler, §2023:8.5.1.6.3).</summary>
    public GroupArea Window(int at, int count, char pad)
    {
        if (at + count > Width)
        {
            var chars = new char[count];
            for (int i = 0; i < count; i++) chars[i] = at + i < Width ? At(at + i) : pad;
            return Of(new string(chars), pad.ToString(), count, Bits);
        }
        if (at < Head.Length) return Of(Head.Substring(at, Math.Min(Head.Length - at, count)), Unit, count, Bits);
        int phase = (at - Head.Length) % Unit.Length;
        return Of("", Unit[phase..] + Unit[..phase], count, Bits);
    }

    /// <summary>Whether the windows of <paramref name="count"/> positions at <paramref name="a"/> and
    /// <paramref name="b"/> hold the same positions. Two windows wholly inside the repeated part are equal exactly
    /// when their phases agree (or, shorter than the unit, when their characters do); anything else is compared
    /// position by position, which only a window reaching into <see cref="Head"/> needs.</summary>
    public bool SameWindow(int a, int b, int count)
    {
        char pad = Bits ? '0' : ' ';
        if (a + count > Width || b + count > Width) return Window(a, count, pad).Text == Window(b, count, pad).Text;
        if (a >= Head.Length && b >= Head.Length && count >= Unit.Length)
            return (a - Head.Length) % Unit.Length == (b - Head.Length) % Unit.Length;
        for (int i = 0; i < count; i++)
            if (At(a + i) != At(b + i)) return false;
        return true;
    }

    /// <summary>How many occurrences of a <paramref name="stride"/>-position element it takes the repeated part to
    /// come back to the same phase — the period a table's windows repeat with there.</summary>
    public int PeriodFor(int stride) => Unit.Length / (int)Binding.Model.TableValuePlan.Gcd(Unit.Length, stride);

    /// <summary>The positions, materialized — for a LEAF's window, whose width its PICTURE bounds.</summary>
    public string Text
    {
        get
        {
            if (Width <= Head.Length) return Head[..Width];
            var sb = new System.Text.StringBuilder(Head, Width);
            while (sb.Length < Width) sb.Append(Unit, 0, Math.Min(Unit.Length, Width - sb.Length));
            return sb.ToString();
        }
    }

    /// <summary>The area as a C# string expression: the head, then the unit repeated at run time
    /// (<c>CobolString.Repeat</c>), then the unit's partial tail.</summary>
    public string CsExpression()
    {
        int rest = Width - Head.Length, reps = rest / Unit.Length, part = rest % Unit.Length;
        var parts = new List<string>();
        if (Head.Length > 0) parts.Add(EmitText.CsLiteral(Head));
        if (reps > 0) parts.Add(reps == 1 ? EmitText.CsLiteral(Unit) : RuntimeApi.StrRepeat(EmitText.CsLiteral(Unit), $"{reps}"));
        if (part > 0) parts.Add(EmitText.CsLiteral(Unit[..part]));
        return parts.Count switch { 0 => "\"\"", 1 => parts[0], _ => "(" + string.Join(" + ", parts) + ")" };
    }

    private static string MinimalPeriod(string unit)
    {
        for (int p = 1; p < unit.Length; p++)
        {
            if (unit.Length % p != 0) continue;
            bool periodic = true;
            for (int i = p; i < unit.Length && periodic; i++) periodic = unit[i] == unit[i - p];
            if (periodic) return unit[..p];
        }
        return unit;
    }
}

/// <summary>The group-VALUE positional distributor (P7 Step 9l; ISO §2023:13.18.63): a GROUP-level VALUE
/// initializes the whole area as ONE value of the group's own category, sliced positionally over EVERY physical
/// field of the subtree at compile time — typed members and Tier-B backings alike (kb/Work PB5851); a group with no
/// VALUE takes the member-wise composed initializer. Wired by <see cref="DataEmitter"/>.</summary>
internal sealed class GroupValueSlicer(EmitContext ctx, PhysicalModel phys)
{
    /// <param name="subs">The OCCURRENCE CONTEXT — the subscripts of every OCCURS level entered on the way down
    /// from the record root (see <see cref="ValueInitializer.FieldInit"/>). It selects WHICH occurrence's
    /// group-level VALUE this composition deposits (§2023:13.18.63.4 GR12–GR15 through <see cref="DataItem.ValueAt"/>)
    /// and travels on to the members, whose own table VALUEs are keyed by the same tuple.</param>
    /// <param name="recipe">Under <see cref="SeedRecipe.Initialize"/> a group-level VALUE is not applied: §2023:14.9.20.4
    /// GR5 c) 1. b qualifies only "a data-item format VALUE clause … specified in the data description entry of the
    /// elementary data item", so the members compose one by one (kb/Work PB1267).</param>
    public string ComposedInit(DataItem group, Subscripts subs = default, SeedRecipe recipe = SeedRecipe.InitialState)
    {
        // A GROUP-level VALUE initializes the whole AREA (ISO §2023:13.18.63.4 GR5) — the ONE area rule lives in
        // AreaOf, which also says which UNIT the area is measured in; here it distributes over the subordinate
        // items POSITIONALLY at compile time (NC104A `01 MOVE29A VALUE "$123.45". 02 MOVE30 PIC $999.99.`).
        // ⛔ WHEN AN AREA EXISTS THE DISTRIBUTION IS TOTAL (kb/Work PB5851). It used to be taken only when the
        // subtree held no shared-storage member, and every other subtree fell to the member-wise default below —
        // each member initialized from its OWN description as though the group carried no VALUE. A REDEFINES
        // anywhere under the group (`01 G VALUE "ABCD". 05 A PIC X(4). 05 B REDEFINES A PIC X(4).`, or one pair a
        // level down) lost the VALUE for EVERY member, at every edition. The comment that justified it said a
        // Tier-B backing "applies the SAME AreaOf rule to the same group" — true only when the class is rooted AT
        // the group carrying the VALUE; a class rooted at a MEMBER asked AreaOf of the member, which has none.
        // Now the walk below gives every physical field its slice: a typed member decodes it, a Tier-B backing
        // rooted at a member is seeded from it (ImageInitializer.BackingSeedOver), and a member it cannot place is
        // an internal error naming the item — never the member-wise default.
        if (recipe is SeedRecipe.InitialState && AreaOf(group, ctx, subs) is { } area)
            return area.Bits ? SliceBitInit(group, area, subs.Count) : SliceInit(group, area, subs.Count);
        var parts = phys.PhysicalChildrenOf(group, subs, recipe).Select(f => $"{f.Name} = {f.Init}");
        return $"new {group.StructName} {{ {string.Join(", ", parts)} }}";
    }

    /// <summary>⛔ THE §2023:13.18.63.4 GR5 GROUP-VALUE AREA RULE — the one place that says what a group-level VALUE
    /// puts in the group's storage, ridden by BOTH initializer lanes: this slicer (the typed-native record-struct
    /// fields) and <see cref="ImageInitializer.ImageInitOf"/> (the character-image backings — Tier-B REDEFINES,
    /// EXTERNAL cells, BASED records, OO cells). Returns the area's contents PLUS its unit
    /// (<see cref="GroupArea"/>), padded / right-truncated to the group's own width per GR7, or null when the
    /// entry carries no VALUE or an operand form that is not an area fill.
    ///
    /// <para>GR5 is "the group area is initialized without consideration for the individual elementary or group
    /// items contained within this group" — so the deposit is positional and the members are whatever their own
    /// descriptions make of the positions underneath them. For an ALPHANUMERIC group that reading needs no
    /// character/byte reconciliation, because §2023:13.18.63.3 SR14 requires every data item subordinate to an
    /// alphanumeric group item carrying a VALUE to be usage DISPLAY (screened by
    /// <c>CheckGroupValueDeclarationsPass</c>, COBOL1702): the character image IS the byte image for
    /// that whole population.</para>
    ///
    /// <para><b>⛔ TWO UNITS, because a BIT GROUP's area is not made of characters.</b> §2023:13.18.29.4 GR1a makes a
    /// GROUP-USAGE BIT group "a bit group and also a bit data item; its class and category are boolean", and GR1b
    /// describes it "as though it were an elementary data item of usage bit and class and category boolean
    /// described with PICTURE 1(m), where m is the bit length of the group" — so its area is <b>m boolean
    /// positions</b> laid out by the §2023:8.5.1.6.3 walk, NOT the <c>ceil(m/8)</c> characters
    /// <see cref="DataItem.ImageWidth"/> reports. The two are different numbers and the members do not tile the
    /// character count: <c>01 BG GROUP-USAGE BIT VALUE B"1010". 05 B1 PIC 1(2). 05 B2 PIC 1(2).</c> is 4 bits =
    /// ONE character while B1 and B2 occupy <c>ceil(2/8)</c> = one character EACH. A positional CHARACTER slice
    /// over that group ran off the end of the area (an unhandled <c>ArgumentOutOfRangeException</c> out of
    /// <see cref="SliceInit"/>) and its single-member twin stored ONE boolean position where the literal has
    /// four — kb/Work PB207, which this method now answers instead of refusing.</para>
    ///
    /// <para><b>THREE operand forms, all of them area fills</b> (kb/Work PB184's sweep — the figurative arm was
    /// MISSING and it was a live silent wrong answer: `01 GZ VALUE ZEROS. 05 Z1 PIC X(2). 05 Z2 PIC X(2).`
    /// measured as four SPACES, where GR5 requires four '0' characters, because the group fell through to the
    /// member-wise default and each member took its own category default). §2023:8.3.3.6.4 GR2 is the ONE rule for
    /// the repeat: it is the branch for the case "the length of the string is specified in the rules for the
    /// context", it names the VALUE clause explicitly, and it gives both the repeat-to-width and the
    /// truncate-from-the-right this method implements. (GR3 is the complementary branch — the length NOT
    /// specified — and its sub-rule a is scoped to a concatenation expression; neither governs here, because
    /// §2023:13.18.63.4 GR5 makes the group area the receiving field and so specifies the length.)</para>
    /// <list type="number">
    ///   <item>a FIGURATIVE constant word, with or without ALL — its one character repeated to the group's width
    ///     (§2023:8.3.3.6.4 GR2; the character itself is GR1's, with the per-format GR4–GR8 identities; SR13 admits
    ///     the operand as "a figurative constant that is permitted in a MOVE statement to a receiving item of
    ///     that category"). The fill character comes from the ONE figurative service, with the group's own
    ///     category so a national / bit group reads ITS anchor rather than the alphanumeric PCS.</item>
    ///   <item><c>ALL "literal"</c> — literal-1 repeated to the width (§2023:8.3.3.6.4 GR2, the same rule).</item>
    ///   <item>a quoted literal — its characters (or, for a bit group, its BOOLEAN POSITIONS — <c>B"1010"</c> /
    ///     <c>BX"A"</c>, §2023:8.3.3.4), decoded.</item>
    /// </list></summary>
    internal static GroupArea? AreaOf(DataItem group, EmitContext ctx, Subscripts subs = default)
    {
        // ⛔ BOTH VALUE CARRIERS, through the ONE reader (DataItem.ValueAt): a group entry may carry a FORMAT 2
        // (table) VALUE just as it may carry a format 1 — §2023:13.18.63.3 SR16 pulls SR13 onto it and §2023:13.18.63.4 GR5
        // still says the GROUP AREA is what gets initialized, per occurrence. Reading RawValue alone silently
        // discarded `05 GT OCCURS 2 VALUE "ABCD" FROM (1) TO (2).` at every edition (kb/Work PB505).
        if (group.ValueAt(subs) is not { } raw) return null;
        // ⛔ THE UNIT AND THE WIDTH, in one place. A GROUP-USAGE BIT group's area is its §2023:13.18.29.4 GR1b
        // as-if PICTURE 1(m) — m BOOLEAN POSITIONS from the §2023:8.5.1.6.3 walk; every other group's is its
        // ImageWidth CHARACTER positions.
        bool bits = group.GroupUsage is GroupUsage.Bit;
        // ⛔ A group that is NOT a bit group but carries a USAGE BIT descendant is measured by the bit walk
        // (DataItem.ImageWidth switches on HasBitDescendant, D19/PB43) while its AREA is a character run, so
        // neither unit describes it — and no such entry is conforming source: §2023:13.18.63.3 SR14 requires every
        // data item subordinate to an ALPHANUMERIC group item carrying a VALUE to be explicitly or implicitly
        // usage DISPLAY, and a USAGE BIT leaf (or a nested GROUP-USAGE BIT group) is not
        // (CheckGroupValueDeclarationsPass rejects it, COBOL1702); under a NATIONAL group
        // §2023:13.18.29.3 SR3 requires usage national of every subordinate elementary item. Returning null keeps
        // BOTH lanes total over that non-conforming residue rather than slicing a width the members do not
        // tile — never a substitute for the diagnostic, which is stated where the rule is.
        if (!bits && group.HasBitDescendant) return null;
        int width = bits ? group.AsIfPic!.Length : group.ImageWidth;
        if (width <= 0) return null;
        // THE one §2023:8.3.3.6.2 operand classifier decides which format this text is (kb/Work PB461): Formats 1-5
        // (ALL optional) fill the area, Format 6 (ALL literal-1) repeats literal-1 into it — both by §2023:8.3.3.6.4
        // GR2, and both spellings the parse tree can produce.
        // GR7 sends the literal through §2023:14.6.8, whose arm is chosen by the RECEIVING item's category: a
        // category-boolean area is filled "into the corresponding boolean positions … with ZERO fill or
        // truncation to the right" (§2023:14.6.8.6 — the boolean zero, not the space that would not be a boolean
        // position at all), every other category with space fill (§2023:14.6.8.5). GR7's own exception applies to
        // both: initialization is not affected by JUSTIFIED and no editing takes place. The repeat is the area's
        // UNIT, never a full-width string (kb/Work PB1722).
        string pad = bits ? "0" : " ";
        var op = FigurativeConstants.Classify(raw);
        if (op.Kind is { } kind)
            return GroupArea.Of("", FigurativeConstants.FillChar(kind, ctx.Data.Configuration.Collating,
                IntrinsicArgumentRules.CharacterContext(IntrinsicArgumentRules.ClassOfItem(group)), ctx.Data.Configuration.NationalCollating).ToString(), width, bits);
        if (op.AllLiteral is { } all && CobolLiteral.Decode(all) is { Length: > 0 } unit)
            return GroupArea.Of("", unit, width, bits);
        return CobolLiteral.IsStringLiteral(raw) ? GroupArea.Of(CobolLiteral.Decode(raw), pad, width, bits) : null;
    }

    /// <summary>⛔ THE ONE PLACEMENT OF A GROUP'S AREA OVER ITS PHYSICAL FIELDS (kb/Work PB5851): what a member that is
    /// NOT a nested record struct makes of the positions under it, or the internal error that names it. It is
    /// asked of every member both slice walks reach, so no member under a group VALUE is ever initialized
    /// without its slice.
    /// <list type="bullet">
    ///   <item>A member that is the canonical of a Tier-B REDEFINES class is stored as the class's ONE string
    ///     backing (<see cref="PhysicalModel"/>): the backing is seeded from the slice by the image lane
    ///     (<see cref="ImageInitializer.BackingSeedOver"/>), padded to the class width. Its redefiners are views
    ///     over that backing and take no slice of their own (the walks skip them).</item>
    ///   <item>A native CHARACTER-FORM numeric leaf decodes its zoned slice to the unscaled value through the one
    ///     <c>ParseDisplay</c> every image read uses — both usages §2023:13.18.60.4 GR7/GR8 name, through
    ///     <see cref="PicInfo.IsCharacterFormNumeric"/>: a national-form numeric's digit characters ARE its
    ///     DISPLAY twin's (design D-N7, kb/Work PB646). §2023:13.18.63.3 SR14 does not screen a NATIONAL group,
    ///     and §2023:13.18.29.3 SR3 admits numeric national leaves under one by name, so that arm is live
    ///     source.</item>
    ///   <item>A string-stored leaf (alphanumeric / edited / national / boolean / StoreAsImage) keeps the
    ///     characters (D-N4 identity) — a boolean leaf of a bit group keeps its boolean positions, its own
    ///     carrier.</item>
    ///   <item>Anything else is not a shape conforming source reaches: §2023:13.18.63.3 SR14 makes every item
    ///     under an alphanumeric group carrying a VALUE usage DISPLAY (<c>CheckGroupValueDeclarationsPass</c>,
    ///     COBOL1702), SR3 makes every leaf under a national one usage NATIONAL and SR2 every member of a bit
    ///     group a bit item. So it throws, naming the item — it does not take the member's own default.</item>
    /// </list></summary>
    /// <param name="c">The member.</param>
    /// <param name="slice">The positions of the area over <paramref name="c"/> (one occurrence of it).</param>
    /// <param name="sliceGroup">Composes a NESTED record-struct member from its slice (the caller's own walk:
    /// characters or bits).</param>
    private (string Field, string Init) MemberFromSlice(DataItem c, GroupArea slice, Func<DataItem, GroupArea, string> sliceGroup)
    {
        if (c.Class is { Tier: RedefinesTier.StringCanonical } cls)
            return (cls.BackingClrName, RuntimeApi.StrStore(phys.Images.BackingSeedOver(c, slice), $"{cls.Width}"));
        if (c.IsGroup) return (c.ClrName, sliceGroup(c, slice));
        if (!c.StoreAsImage && c.Pic is { IsCharacterFormNumeric: true })
            return (c.ClrName, $"({c.ElementType}){RuntimeApi.NumParseDisplay(EmitText.CsLiteral(slice.Text), c.ProfileName)}");
        if (c.StoreAsImage || c.Pic?.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited
                or PicCategory.National or PicCategory.Boolean)
            return (c.ClrName, EmitText.CsLiteral(slice.Text));
        throw new InvalidOperationException(
            $"the group VALUE area over '{c.CobolName ?? "FILLER"}' cannot be placed in its storage form "
            + "(§2023:13.18.63.4 GR5; §2023:13.18.63.3 SR14 should have refused the entry, COBOL1702) — kb/Work PB5851");
    }

    /// <summary>Whether <paramref name="c"/> is a member a slice walk passes over: a REDEFINING entry overlays its
    /// target's positions (§2023:13.18.44.4 GR1) and occupies none of its own, so it neither takes a slice nor
    /// advances the walk — its storage is its target's field or its class's backing, which the walk seeds from the
    /// same positions. A level-66/88 entry is no storage at all.</summary>
    private static bool TakesNoSlice(DataItem c) => c.RedefinesTargetName is not null || !(c.IsGroup || c.IsElementary);

    /// <summary>Build the composed initializer of the group <paramref name="item"/> from its positional
    /// <paramref name="slice"/> of the group VALUE text — each physical member (and each OCCURS occurrence) takes its
    /// own window, through <see cref="MemberFromSlice"/>.</summary>
    private string SliceInit(DataItem item, GroupArea slice, int depth)
    {
        var parts = new List<string>();
        int off = 0;
        foreach (var c in item.Children)
        {
            if (TakesNoSlice(c)) continue;
            int w = c.ImageWidth;
            if (c.Occurs is { } n)
            {
                // One element composed per run of windows that repeat (kb/Work PB1722): a figurative fill makes the
                // whole table one run and an ALL literal a periodic one, so the text no longer grows with the
                // element count.
                int at = off;
                var runs = OccurrenceRunEmit.RunsOfRepeats(n, slice.PeriodFor(w),
                    (a, b) => slice.SameWindow(RecordLayout.OccurrenceAt(at, a - 1, w), RecordLayout.OccurrenceAt(at, b - 1, w), w));
                parts.Add($"{c.ClrName} = " + OccurrenceRunEmit.Array(c.ElementType, n, runs,
                    o => MemberFromSlice(c, slice.Window(RecordLayout.OccurrenceAt(at, o - 1, w), w, ' '), (g, s) => SliceInit(g, s, depth + 1)).Init, depth));
                off = RecordLayout.OccurrenceAt(off, n, w);
            }
            else
            {
                var (field, init) = MemberFromSlice(c, slice.Window(off, w, ' '), (g, s) => SliceInit(g, s, depth));
                parts.Add($"{field} = {init}");
                off = RecordLayout.Advance(off, w);
            }
        }
        return $"new {item.StructName} {{ {string.Join(", ", parts)} }}";
    }

    /// <summary>Build the composed initializer of a BIT group from its §2023:13.18.63.4 GR5 area — the group's m
    /// boolean positions — by giving each member the positions §2023:8.5.1.6.3 places it at. The bit twin of
    /// <see cref="SliceInit"/>, and it is a separate walk for exactly one reason: the offsets are BIT offsets
    /// and they are not a running sum of the members' widths.
    ///
    /// <para>⛔ The placement comes from <see cref="BitLayout.StartBitWithin"/>, THE one §2023:8.5.1.6.3 walk. A
    /// private cursor here would be another copy of the placement law and would silently disagree with it
    /// wherever two members are NOT adjacent — a member whose LEVEL NUMBER differs from its predecessor's is
    /// not "immediately following … an item of the same level", so it starts at "the first bit position of the
    /// first available byte" and the bits between are implicit filler, which a sum cannot express (this is the
    /// same reason <see cref="BitLayout.ExtentBits"/> is a cursor walk and not a sum).</para>
    ///
    /// <para>A redefining member overlays its target and occupies no positions of its own (§2023:13.18.44.4 GR1), so
    /// it takes no slice (<see cref="TakesNoSlice"/>); a member that is the canonical of a Tier-B class takes its
    /// slice into the class's backing (<see cref="MemberFromSlice"/>), as it does in <see cref="SliceInit"/>.</para></summary>
    private string SliceBitInit(DataItem item, GroupArea area, int depth)
    {
        var parts = new List<string>();
        foreach (var c in item.Children)
        {
            if (TakesNoSlice(c)) continue;
            int at = BitLayout.StartBitWithin(item, c);
            // StartBitWithin's documented -1 is an overlay chain it does not model; a non-redefining member is
            // never one, so a -1 here is a member the area cannot be placed over — loud, never the member's default.
            if (at < 0)
                throw new InvalidOperationException(
                    $"the bit group VALUE area of '{item.CobolName ?? "FILLER"}' cannot be placed over '{c.CobolName ?? "FILLER"}' "
                    + "(§2023:13.18.63.4 GR5) — kb/Work PB5851");
            int per = BitLayout.WidthBits(c);
            if (c.Occurs is { } n)
            {
                // ISO §2023:13.18.63.4 GR9 gives EVERY occurrence the value at its own position; the stride is the
                // per-occurrence BIT extent, which is what makes a `PIC 1(4) OCCURS 6` table six sub-byte
                // windows rather than six bytes. ⛔ The stride and the WINDOW WIDTH were one local until
                // kb/Work PB487; §2023:13.18.1.4 GR2 splits them, because an ALIGNED occurrence still occupies only
                // its declared bits but the NEXT one starts at the following byte.
                int stride = BitLayout.StrideBits(c);
                var runs = OccurrenceRunEmit.RunsOfRepeats(n, area.PeriodFor(stride),   // kb/Work PB1722
                    (a, b) => area.SameWindow(RecordLayout.OccurrenceAt(at, a - 1, stride), RecordLayout.OccurrenceAt(at, b - 1, stride), per));
                parts.Add($"{c.ClrName} = " + OccurrenceRunEmit.Array(c.ElementType, n, runs,
                    o => MemberFromSlice(c, BitWindow(area, RecordLayout.OccurrenceAt(at, o - 1, stride), per), (g, s) => SliceBitInit(g, s, depth + 1)).Init, depth));
            }
            else
            {
                var (field, init) = MemberFromSlice(c, BitWindow(area, at, per), (g, s) => SliceBitInit(g, s, depth));
                parts.Add($"{field} = {init}");
            }
        }
        // ⛔ THE IMPLICIT FILLER IS PART OF THE AREA THE VALUE INITIALIZES (§2023:13.18.63.4 GR5 — "the group area is initialized
        // without consideration for the individual elementary or group items contained within this group", kb/Work
        // PB3564): each stored filler takes the positions the area holds where it lies, the compile-time twin of what
        // FromBits stores into it.
        foreach (var filler in BitLayout.FillersOf(item))
        {
            if (filler.Kind is BitFillerKind.RunTail)
            {
                parts.Add($"{filler.ClrName} = {EmitText.CsLiteral(BitWindow(area, filler.StartBit, filler.Bits).Text)}");
                continue;
            }
            // The pads between the occurrences of a strided table: one gap after another, composed once per run of gaps
            // that repeat so the text does not grow with the occurrence count (kb/Work PB1722).
            var table = filler.Subject;
            int n = table.Occurs!.Value, per = BitLayout.WidthBits(table), stride = BitLayout.StrideBits(table), pad = stride - per;
            int first = filler.StartBit;
            var gaps = OccurrenceRunEmit.RunsOfRepeats(n - 1, area.PeriodFor(stride),
                (a, b) => area.SameWindow(RecordLayout.OccurrenceAt(first, a - 1, stride), RecordLayout.OccurrenceAt(first, b - 1, stride), pad));
            parts.Add($"{filler.ClrName} = " + OccurrenceRunEmit.Image(gaps,
                o => EmitText.CsLiteral(BitWindow(area, RecordLayout.OccurrenceAt(first, o - 1, stride), pad).Text)));
        }
        return $"new {item.StructName} {{ {string.Join(", ", parts)} }}";
    }

    /// <summary>One member's window of the area's boolean positions, ZERO-filled past the area's end — the
    /// compile-time twin of <c>CobolBits.Slice</c>, and the same §2023:14.6.8.6 boolean-zero pad
    /// <see cref="AreaOf"/> uses. A window can run past the area only when the group's own extent exceeds the
    /// positions the walk assigns (implicit filler, §2023:8.5.1.6.3), so the fill is filler bits, which
    /// §2023:13.18.63's all-zero boolean initial state makes zeros.</summary>
    private static GroupArea BitWindow(GroupArea area, int at, int count) => area.Window(at, count, '0');
}
