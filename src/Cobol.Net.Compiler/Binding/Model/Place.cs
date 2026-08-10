// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Model;

/// <summary>
/// The ONE typed-lvalue model (COBOLNET_DESIGN §3.3 / §14.1): a resolved reference to a storage location, built once
/// by <see cref="ReferenceResolver"/> and consumed identically by every verb — MOVE, arithmetic,
/// INSPECT/STRING/UNSTRING, file READ INTO / WRITE FROM, and CALL-by-reference all go through this contract. A
/// <c>Place</c> is a BACKEND-NEUTRAL structural value (the G4 invariant): it carries an <see cref="AccessPath"/>,
/// resolved <see cref="DataItem"/>s, and (until PHASE 15's D10) transitional index/offset strings — never C# render
/// text. The C# read/write text is produced by <c>CodeGen.PlaceRenderer</c> (P7 Step 11); the binder never renders.
/// </summary>
public abstract record Place
{
    /// <summary>The analyzed PICTURE of the location (<see langword="null"/> for a group item).</summary>
    public abstract PicInfo? Pic { get; }

    /// <summary>The underlying bound data item this place refers to (carries category, scale, and the profile name).</summary>
    public abstract DataItem Item { get; }
}

/// <summary>
/// The common base of the WRAPPING places (DESIGN-data-model §2.2 item 1): a decoration over one
/// <see cref="Inner"/> place that keeps the inner item's identity (<see cref="Pic"/>/<see cref="Item"/> forward).
/// Leaf places (<see cref="MemberPlace"/>, <see cref="DynTablePlace"/>, <see cref="RedefViewPlace"/>,
/// <see cref="CapacityRegisterPlace"/>) derive from <see cref="Place"/> directly. <see cref="RenamesPlace"/> stays
/// direct too — it composes N spanned leaves (no single inner) and its Pic/Item are the level-66 ALIAS's own, never a
/// forward (the DESIGN §2.2 item-1 derive list was over-inclusive on that member). The backend
/// <c>CodeGen.PlaceRenderer</c> renders each decorator's transformed access (a plain decorator renders as its inner).
/// </summary>
public abstract record PlaceDecorator(Place Inner) : Place
{
    /// <inheritdoc/>
    public override PicInfo? Pic => Inner.Pic;

    /// <inheritdoc/>
    public override DataItem Item => Inner.Item;
}

/// <summary>
/// A direct member-access place: a static field or a (possibly nested, possibly subscripted) member of a
/// <c>record struct</c> — e.g. <c>WS_N</c>, <c>WS_REC.WS_NAME</c>, <c>CobolTable.At(TBL.ROWS, i).VAL</c>. The access
/// <paramref name="Path"/> is a structural <see cref="AccessPath"/> (a field chain + fixed-table accessors + the D10
/// transitional subscript strings), rendered by <c>CodeGen.PlaceRenderer</c>. A fixed-table member is a plain lvalue,
/// so its read and write render the same path (a subscripted OCCURS DYNAMIC element is the sibling
/// <see cref="DynTablePlace"/>, whose accessor is direction-specific).
/// </summary>
public sealed record MemberPlace(AccessPath Path, DataItem MemberItem) : Place
{
    /// <inheritdoc/>
    public override PicInfo? Pic => MemberItem.Pic;

    /// <inheritdoc/>
    public override DataItem Item => MemberItem;
}

/// <summary>
/// A subscripted OCCURS DYNAMIC element (ISO/IEC 1989:2023 §8.5.1.9.2/.9.3; data-model D9). Unlike a fixed table
/// (whose <c>CobolTable.At</c> <c>ref T</c> serves BOTH directions), a dynamic table has direction-specific accessors:
/// <c>RefSending(occ)</c> on a read (an out-of-range occurrence is benign scratch) and <c>RefReceiving(occ)</c> on a
/// write (an occurrence past the current capacity GROWS the table). The <see cref="Path"/>'s trailing
/// <see cref="DynTableSegment"/> renders that polarity at emit time (SENDING for a read, RECEIVING for a write) — one
/// structural path replaces the former two precomputed strings. Rendered by <c>CodeGen.PlaceRenderer</c>. (Kept a
/// distinct subtype rather than folded into <see cref="MemberPlace"/> to bound the Step 11 blast radius —
/// <c>UsageCollectionPass</c>/<c>MoveEmitter</c> discriminate it by type.)
/// </summary>
public sealed record DynTablePlace(AccessPath Path, DataItem ElementItem) : Place
{
    /// <inheritdoc/>
    public override PicInfo? Pic => ElementItem.Pic;

    /// <inheritdoc/>
    public override DataItem Item => ElementItem;
}

/// <summary>
/// A Tier-B REDEFINES view (COBOLNET_DESIGN §4.2): a typed <c>(offset, width)</c> character window over the class's
/// ONE <see cref="string"/> backing field. Reading yields the window's character image (a substring); writing splices
/// a new image back into the backing, preserving its full width — so a write through any view is visible through every
/// other view of the class (one stored backing, ISO §13.18.44). The window carries the VIEW's <see cref="DataItem"/>,
/// so its category/scale/profile drive interpretation: a numeric-DISPLAY view is flagged
/// <see cref="DataItem.StoreAsImage"/>, so the numeric pipeline decodes/encodes the window exactly as for a
/// whole-group numeric leaf. <paramref name="Backing"/> is the structural path to the class's stored backing field;
/// <paramref name="OffsetExpr"/> is the 0-based window offset — the D10 transitional string (a constant, or the
/// <c>classOffset + Σ (idx − 1) × stride</c> arithmetic for a view inside an OCCURS, ISO §13.18.44). Rendered by
/// <c>CodeGen.PlaceRenderer</c>.
/// </summary>
public sealed record RedefViewPlace(AccessPath Backing, string OffsetExpr, int Width, DataItem ViewItem) : Place
{
    /// <inheritdoc/>
    public override PicInfo? Pic => ViewItem.Pic;

    /// <inheritdoc/>
    public override DataItem Item => ViewItem;
}

/// <summary>
/// A level-66 RENAMES place (ISO §13.18.45): ONE elementary-alphanumeric view composed over the spanned record
/// leaves. Reading concatenates the leaves' character images (each leaf field invariantly holds exactly its image
/// width); writing stores the value at the span's width and distributes the slices back into the leaves left to
/// right — so a write through the alias is visible through every renamed item and vice versa (no second storage,
/// SR/GR — RENAMES adds no data item). Rendered by <c>CodeGen.PlaceRenderer</c>.
/// </summary>
public sealed record RenamesPlace(IReadOnlyList<Place> Leaves, DataItem AliasItem) : Place
{
    /// <inheritdoc/>
    public override PicInfo? Pic => AliasItem.Pic;

    /// <inheritdoc/>
    public override DataItem Item => AliasItem;
}

/// <summary>
/// The <see cref="Place"/> of a GROUP operand whose subtree contains an occurs-depending table (ISO/IEC 1989:2023
/// §13.18.38 Format 2). It decorates the plain member place — read/write render as the struct lvalue unchanged (the
/// inherited inner access), so every consumer that does not know about ODO behaves exactly as before — and the GR8
/// operand seams (rendered by <c>CodeGen.PlaceRenderer</c> over this record's structure) consult the decoration:
/// <list type="bullet">
///   <item><b>Sending</b> (the sending side of BOTH GR8 quadrants): "only that part of the table area that is
///     specified by the value of [data-name-1] at the start of the operation will be used" — the group image
///     truncated to the current extent. SR22 (the subject may be followed within its record only by entries
///     subordinate to it) guarantees the table is the TRAILING storage, so the current extent is a character PREFIX
///     of the maximum image; a zero count with no preceding fixed part is the zero-length item of §8.5.4 item 1.</item>
///   <item><b>Receiving, data-name-1 outside the group</b> (GR8a): the same current extent — character positions
///     past it are NOT modified; the stored prefix is spliced over the live image.</item>
///   <item><b>Receiving, data-name-1 inside the group</b> (GR8b): "the maximum length of the group will be used"
///     — <see cref="DependingInside"/> lets each receiver keep the plain full-width <c>FromImage</c> store.</item>
/// </list>
/// The legacy engine proved exactly this direction split over the NIST-85 corpus (NC247A; its
/// <c>LocationResolver.ResolveWholeItem(receiving)</c>); the greenfield twin computes the CHARACTER extent at the
/// operand site — no runtime table state (COBOLNET_DESIGN §3.6 / §14.4 — ONE image facility, the GR8 slice is a
/// view over it). The legacy's LINKAGE max-length shortcut is deliberately NOT ported: GR8 applies in any section.
/// </summary>
public sealed record OdoGroupPlace(
    Place Inner, Place Depending, int FixedChars, int ElemChars, int MinOccurs, int MaxOccurs,
    bool DependingInside)
    : PlaceDecorator(Inner);

/// <summary>
/// A NUMERIC-DISPLAY item viewed as its CHARACTER IMAGE for reference modification (ISO §8.4.3.3.4 GR6 — the unique
/// result is an elementary alphanumeric item over the operand's standard data format): reading formats the stored
/// value's display image; writing decodes the spliced image back into the typed field (sign-aware both ways via
/// the FormatDisplay/StoreDisplay pair). Rendered by <c>CodeGen.PlaceRenderer</c>.
/// </summary>
public sealed record NumericImagePlace(Place Inner) : PlaceDecorator(Inner);

/// <summary>
/// The CAPACITY register of an OCCURS DYNAMIC table (ISO/IEC 1989:2023 §13.18.38 GR15 / §8.5.1.9.1; data-model D9):
/// a VIEW over the table's current capacity, NOT its own storage. Reading renders <c>{table-path}.Capacity</c> — the
/// runtime <see cref="CobolNet.Runtime.CobolDynTable{T}.Capacity"/> (a native <c>long</c>, an unsigned integer per
/// SR31), so <see cref="RegisterItem"/> carries a native-binary <see cref="PicInfo"/> (scale 0) and the numeric
/// pipeline reads it as a scale-0 integer with no profile. The register is set ONLY by SET Format 14 (which emits
/// <c>SetCapacity</c>/<c>CapacityUpBy</c>/<c>CapacityDownBy</c> directly); an ordinary store receiver is rejected
/// COBOLNET1523 at bind time (SR30–32), so <c>PlaceRenderer.Write</c> of this place is an internal-error backstop.
/// </summary>
public sealed record CapacityRegisterPlace(AccessPath Table, DataItem RegisterItem) : Place
{
    /// <inheritdoc/>
    public override PicInfo? Pic => RegisterItem.Pic;

    /// <inheritdoc/>
    public override DataItem Item => RegisterItem;
}

/// <summary>The member of the X3.23-1985 <c>DEBUG-ITEM</c> register a <see cref="DebugRegisterPlace"/> refers to
/// (the whole group, or one elementary member). A STRUCTURAL selector — the backend
/// (<c>CodeGen.PlaceRenderer</c>) maps it to the C# read expression, so no C# text lives in the bound tree
/// (the G4 backend-neutrality invariant; the <see cref="CapacityRegisterPlace"/> precedent).</summary>
public enum DebugRegisterMember { Item, Line, Name, Sub1, Sub2, Sub3, Contents }

/// <summary>
/// The X3.23-1985 <c>DEBUG-ITEM</c> special register (and its members DEBUG-LINE / DEBUG-NAME / DEBUG-SUB-1/2/3 /
/// DEBUG-CONTENTS) — the '85 debug module, deleted 2002 and absent ISO 2023, so modeled only at <c>--std 85</c>
/// (VCR Table 7 row 7.17). It is IMPLICITLY described (no DATA DIVISION entry) and referenced only inside debugging
/// declaratives, so it is a VIEW, never its own storage: reading is the read-only program-instance
/// <c>__dbgItem</c> member selected by <paramref name="Member"/> (<see cref="CobolNet.Runtime.DebugItem"/>), which
/// the injected debug trigger populates — the C# text is produced by <c>CodeGen.PlaceRenderer</c>, NOT stored here
/// (backend-neutral, like <see cref="CapacityRegisterPlace"/>). A COBOL program never assigns to a DEBUG-* register
/// (the runtime sets it), so <c>PlaceRenderer.Write</c> of this place is an internal-error backstop.
/// <see cref="RegisterItem"/> carries the member's alphanumeric <see cref="PicInfo"/> (its fixed width) so
/// MOVE / DISPLAY interpret it as an X-item.
/// </summary>
public sealed record DebugRegisterPlace(DataItem RegisterItem, DebugRegisterMember Member) : Place
{
    /// <inheritdoc/>
    public override PicInfo? Pic => RegisterItem.Pic;

    /// <inheritdoc/>
    public override DataItem Item => RegisterItem;
}

/// <summary>
/// One source reference-modification <c>(start : [length])</c>, reduced to its RENDERED index expressions — the
/// ISO §8.4.3.3.2 general format read off the source, independent of WHAT is being modified. Produced by the ONE
/// reader (<c>ReferenceResolver.ReadRefMod</c>, which accepts both source carriers: the DEFAULT-mode parsed
/// <c>refModPart</c> and the SUBSCRIPT-mode captured token group) and consumed by the two things a ref-mod can
/// attach to:
/// <list type="bullet">
///   <item>a storage <b>place</b> — <see cref="RefModPlace"/>, readable AND writable (the splice);</item>
///   <item>a <b>value</b> with no place — the result of a function-identifier (ISO §8.4.3.3.3 SR2), carried on
///         <c>BoundIntrinsicCall.RefMod</c>. §8.4.3.2.3 SR1 makes a function-identifier a non-receiving operand,
///         so the value form is read-only by construction and needs no splice counterpart.</item>
/// </list>
/// <paramref name="Start"/>/<paramref name="Length"/> are rendered index strings — the D10 TRANSITIONAL carrier,
/// deliberately the SAME one <see cref="RefModPlace"/> uses so PHASE 15 migrates both to <c>BoundExpr</c> in one
/// move rather than leaving a second, differently-shaped ref-mod behind.
/// </summary>
/// <param name="Start">The rendered leftmost-position expression (§8.4.3.3.4 item 5b).</param>
/// <param name="Length">The rendered length expression, or <see langword="null"/> for the omitted
/// "to the end" form (§8.4.3.3.4 item 5c).</param>
/// <param name="AllowZeroLength">The REF-MOD-ZERO-LENGTH directive (ISO §7.3.23) is ON at this ref-mod's source
/// line, so a zero-length result is allowed instead of raising EC-BOUND-REF-MOD.</param>
public readonly record struct RefModSpec(string Start, string? Length, bool AllowZeroLength);

/// <summary>
/// A reference-modified place <c>inner(start:length)</c> (COBOLNET_DESIGN §3.3 / §7.2): reading is a substring
/// (<c>CobolString.RefMod</c>); writing splices the new slice back into the inner field (<c>CobolString.SpliceInto</c>),
/// preserving the inner's width. <paramref name="Length"/> is <see langword="null"/> for the "to the end" form.
/// <paramref name="Start"/>/<paramref name="Length"/> stay the rendered index string (the D10 TRANSITIONAL carrier —
/// they become <c>BoundExpr</c> when PHASE 15 removes the SUBSCRIPT lexer mode). Rendered by
/// <c>CodeGen.PlaceRenderer</c>.
/// </summary>
public sealed record RefModPlace(Place Inner, string Start, string? Length) : PlaceDecorator(Inner)
{
    /// <summary>The REF-MOD-ZERO-LENGTH directive (ISO §7.3.23) is ON at this ref-mod's source line — a zero-length
    /// result is ALLOWED (no EC-BOUND-REF-MOD raise, §8.4.3.3.4 item 5c). The directive's GR default is OFF, so this
    /// is <see langword="false"/> for every ref-mod outside a <c>&gt;&gt;REF-MOD-ZERO-LENGTH ON</c> region — an
    /// init-only property (not a positional member) so existing deconstructions/constructions stay untouched.</summary>
    public bool AllowZeroLength { get; init; }

    /// <summary>
    /// The CATEGORY of the unique data item reference modification creates — <b>ISO §8.4.3.3.4 GR6, verbatim</b>:
    /// "The unique data item has the same class, category, and usage as that defined for identifier-1, except
    /// that: a) the category alphanumeric-edited is considered class and category alphanumeric, b) the category
    /// national-edited is considered class and category national, c) the categories numeric and numeric-edited
    /// are considered class and category national if the usage is national; otherwise they are considered class
    /// and category alphanumeric."
    /// <para>
    /// ⛔ <b>THE ONE PLACE THIS RULE IS WRITTEN (fix-queue PB20).</b> It was previously written three times and
    /// none of the three was right: <c>IntrinsicArgumentRules.ClassOfPlace</c> returned class ALPHANUMERIC
    /// unconditionally, <c>ExpressionBinder</c> said the same in prose, and <c>MoveBinder</c> carried a partial
    /// map that got national and boolean right but still flattened national-edited and national-usage numeric.
    /// The base case is the whole point: <b>GR6 PRESERVES the category</b>, and only the three lettered
    /// exceptions rewrite it — so a ref-modified BOOLEAN item stays boolean and a ref-modified NATIONAL item
    /// stays national.
    /// </para>
    /// <para>
    /// ⚠ <b>All three copies justified themselves with "ISO 8.4.2.4", A CLAUSE THAT DOES NOT EXIST</b>
    /// (<c>cite.py --check 8.4.2.4</c> → "there is no clause 8.4.2.4 in the transcription"; §8.4.2 has only
    /// .1/.2/.3, and reference modification is §8.4.3.3). It was inherited into 21 sites across 14 files — the
    /// failure mode CLAUDE.md rule 1 names, at scale. A fabricated citation is how a wrong rule survives review:
    /// every reader saw a § and stopped.
    /// </para>
    /// <para>
    /// NOTE this compiler's <see cref="PicCategory.Alphanumeric"/> covers ISO's alphanumeric AND alphabetic
    /// categories (the alphabetic-ness rides on <c>PicInfo.IsAlphabetic</c>) — and ⛔ ALPHABETIC-NESS DOES NOT
    /// SURVIVE REFERENCE MODIFICATION (corrected 2026-08-09, fix-queue PB72): GR2 operates on a usage-DISPLAY
    /// non-alphanumeric item "as if it were redefined as a data item of class and category alphanumeric", and
    /// GR1/SR5's closed class lists (boolean, alphanumeric, national) leave no alphabetic result anywhere in
    /// the ref-mod scheme — an earlier revision of this NOTE claimed the base case "preserves both", and
    /// reading the INNER item's IsAlphabetic through a view refused legal Table-16 moves. Consumers that need
    /// the finer Table-16 row build through <c>Table16Operand.Of(Place)</c>, which erases it. No ref-mod result
    /// is ever category NUMERIC — GR6c rewrites numeric away — which is why the §8.8.1.1 arithmetic-operand
    /// bar on a ref-modified operand is correct as it stands. (Whether GR2 likewise makes a DISPLAY-FORM
    /// boolean view alphanumeric — this method keeps it boolean per GR6's base case — is an OPEN adjudication
    /// registered on kb/Work PB72; the usage-BIT view is boolean under every reading, GR5a.)
    /// </para></summary>
    /// <remarks>
    /// ⚠ <b>GR6 a AND b HAVE NO ARM HERE, AND THAT IS A PROPERTY OF THE MODEL, NOT OF THE RULE.</b>
    /// <see cref="PicCategory"/> has no <c>AlphanumericEdited</c> member — an alphanumeric-edited item already
    /// carries <see cref="PicCategory.Alphanumeric"/> with an <c>EditMask</c> — so GR6a is satisfied by the base
    /// case rather than skipped. GR6b is the same shape for a different reason: national-edited is a
    /// recognized-but-unimplemented SKELETON that <c>PictureAnalyzer</c> recovers to
    /// <see cref="PicCategory.Alphanumeric"/> (<c>SkeletonGate = NationalEdited2002</c>), so this cannot see it
    /// and must not pretend to — the gap belongs to that skeleton, not to this rule. **If either category ever
    /// gains its own member, both arms must appear here**, which is why they are named rather than silently
    /// absent.
    /// </remarks>
    public static PicCategory CategoryOf(PicInfo inner) => inner.Category switch
    {
        // GR6 c — numeric and numeric-edited become national under a national usage, alphanumeric otherwise.
        PicCategory.Numeric or PicCategory.NumericEdited =>
            inner.Usage == Usage.National ? PicCategory.National : PicCategory.Alphanumeric,
        // GR6 base — the category is PRESERVED. This is the arm the three old copies did not have: it is what
        // keeps a ref-modified BOOLEAN boolean and a ref-modified NATIONAL national.
        _ => inner.Category,
    };
}
