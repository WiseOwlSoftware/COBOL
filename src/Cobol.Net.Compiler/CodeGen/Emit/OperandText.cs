// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.Runtime;

namespace CobolNet.CodeGen.Emit;

/// <summary>Renders a bound operand to its C# DISPLAY-image string (used by DISPLAY, MOVE-to-alphanumeric, and
/// string comparisons). A numeric field's image goes through <c>CobolNum.FormatDisplay</c> (sign-aware); an
/// alphanumeric field is its <see cref="Place"/> value.</summary>
internal static class OperandText
{
    /// <summary>A bound operand rendered as a C# <see cref="string"/> (its character image). When
    /// <paramref name="deSign"/> is set, a SIGNED numeric operand drops its operational sign (ISO §14.9.25.4 GR6a /
    /// §8.8.4.2.5 — a signed numeric used as / compared against an alphanumeric operand moves the de-signed magnitude
    /// digits, not the zoned/overpunch image). DISPLAY leaves it unset (it shows the sign-aware image).
    /// <paramref name="num"/> is the caller's per-unit expression renderer: an ALPHANUMERIC/NATIONAL-result
    /// intrinsic operand (ISO §15.2 type 1 — the one case that lets MOVE-to-alphanumeric, string comparisons,
    /// and group moves take FUNCTION operands) renders through its INSTANCE intrinsic channel (P7 Step 12 —
    /// the context-free static channel is deleted); deSign is a no-op for it (no operational sign).</summary>
    // Two cached operand-visitor instances (deSign on/off) render the image with zero per-call allocation; a single
    // cached IsString visitor answers the text-comparison predicate. The generated IBoundOperandVisitor makes both
    // exhaustive (PHASE-07 Step 6f) — a new BoundOperand leaf is a COMPILE error, the loud `_ =>` defaults are gone.
    // The intrinsic-operand case is intercepted at the entry (it needs the PER-UNIT renderer, which the cached
    // static visitors cannot hold); the visitor's computed arm keeps the loud non-intrinsic case.
    // Four cached instances = deSign × floatCheck (both flags carried on the instance so every call is
    // allocation-free). floatCheck on (the default) wraps a float sending read in CobolFloat.Sending
    // (EC-DATA-NOT-FINITE, §14.6.13.2 item 3); the exempt callers (class condition, future VALIDATE) pass false.
    private static readonly AsStringVisitor _asStringPlain = new(deSign: false, floatCheck: true);
    private static readonly AsStringVisitor _asStringDeSign = new(deSign: true, floatCheck: true);
    private static readonly AsStringVisitor _asStringPlainNoCheck = new(deSign: false, floatCheck: false);
    private static readonly AsStringVisitor _asStringDeSignNoCheck = new(deSign: true, floatCheck: false);
    private static readonly IsStringVisitor _isString = new();

    private static AsStringVisitor Visitor(bool deSign, bool floatCheck) =>
        floatCheck ? (deSign ? _asStringDeSign : _asStringPlain)
                   : (deSign ? _asStringDeSignNoCheck : _asStringPlainNoCheck);

    public static string AsString(BoundOperand op, NumericRenderer num, bool deSign = false, bool floatCheck = true) =>
        op is BoundComputedOperand { Expr: BoundIntrinsicCall { ResultCategory: PicCategory.Alphanumeric or PicCategory.National or PicCategory.Boolean } ic }
            ? num.Intrinsics.RenderString(ic)
        // A NUMERIC-result intrinsic in a string context — DISPLAY FUNCTION ORD(C), MOVE FUNCTION MAX(…) TO a
        // PIC X item. §8.4.3.1.2 Format 1 makes a function-identifier an IDENTIFIER, so every "identifier-1"
        // position admits one unless a syntax rule excludes it (§14.9.11.3 SR1 excludes only message-tag, object
        // and pointer), and §15.4 puts the returned value in a temporary elementary data item. Intercepted at the
        // ENTRY beside the string channel because it needs the PER-UNIT renderer, which the cached static
        // visitors cannot hold. Before this, ONLY the compile-time-FOLDED cases worked — a fold turns the call
        // into a numeric literal, which is why FUNCTION LENGTH printed and FUNCTION ORD threw.
        : op is BoundComputedOperand { Expr: BoundIntrinsicCall { ResultCategory: PicCategory.Numeric } nic }
            ? NumericIntrinsicText(num, nic, deSign)
        // A function-identifier the binder FOLDED into an arithmetic sum — FUNCTION LENGTH / BYTE-LENGTH of a
        // variable-length group is §15.50.4 r7's fixed-plus-dynamic-leaves sum (kb/Work PB61): still a numeric
        // function's returned value in a string position, rendered in the same item-92 literal form. No general
        // format admits a bare arithmetic expression here, so nothing else can reach this arm.
        : op is BoundComputedOperand { Expr: BoundBinary } sum
            ? NumericExprText(num, sum.Expr, deSign)
        // A bare figurative (HIGH-VALUE/LOW-VALUE/SPACE/…) in a DISPLAY/STRING/STOP value position is an
        // alphanumeric value (§8.3.3.6.4 GR1) of one character (GR3b). Materialize it through the declared collating
        // tables so HIGH-/LOW-VALUE is the runtime-collating extreme (§8.3.3.6.4 GR6/GR7), matching the MOVE and
        // relation paths — not the native pin. Intercepted at the ENTRY (like the intrinsic channel) because the
        // AsStringVisitor has no access to the renderer's collating context; cat=null ⇒ the alphanumeric PCS applies.
        : op is BoundFigurative fig
            ? $"new string({FigurativeConstants.Fill(fig.Kind, num.Collating, null, num.NationalCollating)}, 1)"
        // A boolean EXPRESSION operand's string is its '0'/'1' image through the ONE boolean renderer (kb/Work PB65
        // — a legal intrinsic argument, §8.4.3.2.3 SR8, and any other string position a boolean expression reaches);
        // intercepted at the ENTRY because the renderer needs the per-unit NumericRenderer (shift counts).
        : op is BoundBoolOperand bo
            ? BooleanRenderer.Render(bo.Expr, num)
            : op.Accept(Visitor(deSign, floatCheck));

    /// <summary>The text image of a NUMERIC-result intrinsic (DA2), across all three shapes a numeric
    /// intermediate can take. A FLOAT-valued function renders through the same <c>CobolFloat.Display</c> a float
    /// ITEM does (shortest round-trip, §14.9.11.4 GR1 implementor-defined) — the two must agree, since
    /// <c>FUNCTION SQRT(2)</c> and a COMP-2 item holding that value are the same value; an SDIDI intermediate and
    /// an ordinary scaled value share <c>FormatFunctionText</c>. No float SENDING guard here: the value is a
    /// freshly computed temporary, not a stored item that could hold a NaN deposited earlier.
    /// <para>⛔ <see cref="ReceiverContext.None"/>, NEVER the ambient receiver — the same convention
    /// <c>IntrinsicRenderer.ArgNum</c> uses for the string channel. A text context HAS no numeric receiver, and an
    /// intrinsic's working scale is <c>max(Receiver.Scale, 9)</c>, so reading the ambient one made a function's
    /// printed text depend on whatever arithmetic statement happened to run BEFORE it.</para>
    /// <para>⛔ <paramref name="deSign"/> is honoured, because §14.9.25.4 GR6a is a GENERAL rule carrying no
    /// implementor latitude: "If the sending operand is described as being signed numeric, the operational sign is
    /// not moved". The §15.4.1 / §14.9.11.4 GR1 latitude covers the FORM of the text (padding, radix), never
    /// whether the sign travels. So a de-signing caller — MOVE to an alphanumeric/national/edited receiver, a text
    /// relation (§8.8.4.2.5 routes it through the MOVE rules), INSPECT — gets the MAGNITUDE, exactly as a signed
    /// FIELD operand does via <see cref="FieldAsString"/>. Without this, a signed item and a function returning
    /// the same value rendered differently in identical statements.</para></summary>
    private static string NumericIntrinsicText(NumericRenderer num, BoundIntrinsicCall ic, bool deSign) =>
        NumericExprText(num, ic, deSign);

    /// <summary>The item-92 text form of a numeric function-identifier's value, whatever bound shape the binder
    /// left it in — the intrinsic call itself, or a fold's arithmetic sum (<see cref="AsString"/>).</summary>
    private static string NumericExprText(NumericRenderer num, BoundExpr expr, bool deSign)
    {
        NumX x = num.Render(expr, ReceiverContext.None);
        return x.Real ? RuntimeApi.FloatDisplay(x.Expr)
             : x.Dec ? RuntimeApi.DecFunctionText(x.Expr, deSign)
             : RuntimeApi.NumFormatFunctionText(x.Expr, x.Scale, deSign);
    }

    /// <summary>A data item's character image directly from its <see cref="Place"/> — the num-free entry for
    /// callers that hold a Place (a FIELD can never be an intrinsic operand, so no per-unit renderer is
    /// needed). Same rendering as <see cref="AsString"/> over a field operand.</summary>
    public static string FieldImage(Place p, bool deSign = false, bool floatCheck = true) => FieldAsString(p, deSign, floatCheck);

    /// <summary>The RAW-STORAGE byte image of a field — CONVERT's ANY source channel (§15.19.3 r7: "argument-1
    /// shall be of any usage … It is not necessary for the contents to be valid according to the usage" — the
    /// item's STORAGE BITS, never its display image; fix-queue PB59 family 5b / AR-15.19.3-7 leg b). The
    /// convention is char==byte (the <c>CobolBits.Pack</c> image convention), one representation per leaf shape,
    /// each THE mechanism that already defines that shape's storage (<c>GroupImageCodec.AsImageOf</c>'s recipes —
    /// never a second codec): a group is its generated <c>AsImage()</c>; a national leaf its UTF-16BE bytes
    /// (<c>CobolBits.NatBytes</c>, D-N1); a USAGE BIT leaf its packed bits (<c>CobolBits.Pack</c> — which also
    /// materializes §15.19.4 r2's trailing zero-bit pad); a zoned/character leaf its carrier (the sign-AWARE
    /// image — storage, so no GR6a de-sign); a BINARY/PACKED/COMP-5 leaf its radix-2/BCD bytes
    /// (<c>CobolNum.FormatImage</c>, V59/§14.4); a float leaf its IEEE interchange bytes
    /// (<c>CobolNum.FormatImageFloat</c> — kb/Work PB164 wave 2); an INDEX leaf its 8-byte occurrence-number
    /// bytes (R40). Only a VARIABLE-LENGTH group stages LOUD rather than returning a plausible wrong image;
    /// pointer/object shapes are unreachable — the §15.19.3 r7 bind screen rejected them.</summary>
    public static string AsStorageImage(Place p)
    {
        // §8.4.3.3.4 GR6 — a ref-mod view is an elementary item over the underlying item's characters; its
        // storage is the slice's characters (a NATIONAL slice keeps category national, so its bytes are UTF-16BE).
        // ⛔ A BIT-USAGE SLICE IS THE THIRD CASE, AND IT WAS MISSING (kb/Work PB173). §8.4.3.3.4 GR5a gives a
        // usage-bit operand BIT positions, and GR6 preserves its class, category and usage — so the slice's
        // '0'/'1' carrier is NOT its storage, exactly as it is not for the whole item three arms below. §15.19.3
        // r7 asks for the item's storage BITS and §15.19.4 r2 then pads: "If the number of bits in argument-1 is
        // not a multiple of those needed for a single alphanumeric character, the trailing portion needed to make
        // up a complete multiple is padded with zero bits". Measured before the fix: `CONVERT(XM(1:3) ANY ANUM
        // HEX)` returned 313130 — the three CHARACTERS '1','1','0' — where the unsliced `CONVERT(XM ANY ANUM
        // HEX)` correctly returned CA. Both the bit-GROUP slice (new with this place) and the ELEMENTARY USAGE
        // BIT slice (which predates it) take this arm, and both were wrong; one arm fixes both.
        // ⚠ A DISPLAY-FORM boolean slice must NOT pack: §13.18.60.4 GR7 makes usage DISPLAY "an alphanumeric
        // coded character set", one character per boolean position, so its carrier IS its storage. That is why
        // the test reads the USAGE and not the category (measured: `PIC 1(8)` without USAGE BIT correctly
        // returns 3131303031303130 sliced and unsliced alike, and must keep doing so).
        if (p is RefModPlace rmp)
            return rmp.Category is PicCategory.National ? RuntimeApi.NatBytes(PlaceRenderer.Read(p))
                : rmp.Inner.Item.OperandPic is { Category: PicCategory.Boolean, Usage: Usage.Bit }
                    ? RuntimeApi.BitsPackAll(PlaceRenderer.Read(p))
                    : PlaceRenderer.Read(p);
        // A GROUP's raw storage IS its character image — through THE ONE SENDING READER, never a self-spelled
        // `.AsImage()`. That routing is the fix for kb/Work PB178: a Tier-B REDEFINES group VIEW is fully
        // image-CAPABLE and its Read() is ALREADY the (offset, width) window over the class backing
        // (§13.18.44.4 GR1 — one storage area, so the view's raw storage IS that window, never a second
        // encoding), so the capability guard this arm already had was the WRONG AXIS and
        // `CobolStr.RefMod(...).AsImage()` was a backend CS1061. `SendingGroupImage` owns all four arms —
        // the RedefViewPlace window, the OdoGroupPlace §13.18.38 GR8 current-extent slice (the arm this site
        // used to spell itself, one line above), the capability guard, and the plain struct image.
        if (p.Item.IsGroup) return PlaceRenderer.SendingGroupImage(p, "raw-storage image (CONVERT ANY) of");
        return p.Item.Pic switch
        {
            { Category: PicCategory.National } => RuntimeApi.NatBytes(PlaceRenderer.Read(p)),
            // A USAGE BIT leaf packs its own carrier (an elementary operand is a run of one — §8.5.1.6.3's
            // shared-byte runs exist only INSIDE a group image, which the group arm above owns).
            { Category: PicCategory.Boolean, Usage: Usage.Bit } pic =>
                RuntimeApi.BitsPack(PlaceRenderer.Read(p), pic.Length.ToString()),
            // Display-form boolean, alphanumeric, edited: the carrier IS the storage, one char per byte.
            { Category: PicCategory.Alphanumeric or PicCategory.NumericEdited or PicCategory.Boolean } =>
                PlaceRenderer.Read(p),
            // A numeric leaf stored as its image: the carrier ALREADY HOLDS the storage bytes, whatever byte form
            // they are — zoned digits, radix-2, BCD, or the IEEE interchange bytes of a windowed float. §15.19.3 r7
            // asks for the item's STORAGE, and a windowed item's window IS its storage, so it passes through
            // untouched. ⛔ THIS ARM MUST PRECEDE the native byte-form arms below (the same ordering pin
            // NumericRenderer's windowed-float arm carries): with an `IsFloat: false` conjunct here a windowed
            // float fell through to FormatImageFloat(<string window>) — a backend CS1503, the ONE shape
            // §15.19.3 r7's ANY source format can reach.
            { Category: PicCategory.Numeric } when p.Item.StoreAsImage => PlaceRenderer.Read(p),
            // A native numeric leaf: the bytes it occupies at a byte boundary (zoned digits, radix-2, BCD,
            // or the wave-2 IEEE forms — PicInfo.HasImageByteForm, THE ONE image predicate; kb/Work PB164) —
            // the same recipes the group codec uses, the float family through its distinctly-named lane.
            { HasImageByteForm: true, IsFloat: true } =>
                RuntimeApi.NumFormatImageFloat(PlaceRenderer.Read(p), p.Item.ProfileName),
            { HasImageByteForm: true } =>
                RuntimeApi.NumFormatImage(PlaceRenderer.Read(p), p.Item.ProfileName),
            _ => EmitText.LoudValue("string", TierCIsland.Reason(p.Item, "raw-storage image (CONVERT ANY) of")),
        };
    }

    /// <summary>True if an operand is compared as text (an alphanumeric literal, or an alphanumeric/edited/group
    /// field — an alphanumeric group item "has class and category alphanumeric", ISO §8.5.2.1, and §8.8.4.2.3 SR2
    /// admits it as a relation operand). ⚠ §8.8.4.1.1 DOES NOT EXIST (kb/Work PB182 — the repo-wide sweep).</summary>
    public static bool IsString(BoundOperand op) => op.Accept(_isString);

    /// <summary>⛔ BYTES ARE NOT TEXT (V59). An image-STORED BINARY/PACKED leaf holds its radix-2 / BCD bytes, and
    /// those bytes are not the item's alphanumeric text: an ELEMENTARY numeric operand used as text is "treated as
    /// though it were moved to an alphanumeric data item" (ISO §14.9.25.4 GR6, and §8.8.4.2.2 for a numeric ↔
    /// nonnumeric comparison), which yields its DIGITS. So decode the stored bytes and re-render the DISPLAY image.
    /// Returns null for every ZONED item — there the stored image IS the text, and passing the window through
    /// verbatim also preserves the incompatible content a group MOVE can legitimately deposit (spaces in a numeric
    /// leaf), which a decode-and-reformat round trip would silently turn into zeros.
    /// <para>A GROUP operand is the opposite case and is handled before this: §8.5.2.1 makes it class and category
    /// alphanumeric over the items' REPRESENTATION, so its text IS the record image, bytes and all.</para>
    /// <para>⛔ EVERY byte form the window can hold decodes here, keyed on the item's own carrier lane — the
    /// decode is the exact inverse of the encode the group codec / REDEFINES backing used, so a WINDOWED leaf
    /// and its NATIVE twin print identical text. §13.18.44.4 GR1 associates the two descriptions with ONE
    /// storage area, so "same value, two descriptions, two different printed images" is never a defensible
    /// answer, whatever latitude §14.9.11.4 GR1 leaves the implementor over the FORM of that text (COBOL.NET's
    /// determination: docs/CONFORMANCE.md Annex A.1 items 56/92). The three lanes below are the three carriers
    /// a byte-form numeric leaf can have, and they mirror <c>NumericRenderer.FieldNumCore</c>'s windowed arms
    /// one for one — the float lane FIRST (its <c>IsFloat</c> exclusion is what printed raw IEEE bytes), then
    /// the UInt128 lane (a signed decode of a 16-byte unsigned window is a silent wrong answer above
    /// <c>Int128.MaxValue</c>'s bit pattern), then the signed Int128 lane every other form rides. An 8-byte
    /// UNSIGNED window needs no twin: <c>ParseImage</c> already returns its full [0, 2^64) range as an
    /// Int128.</para></summary>
    private static string? NonTextBytes(Place p, bool deSign, bool floatCheck)
    {
        if (p.Item.Pic is not { Category: PicCategory.Numeric } pic
            || pic.ByteForm is NumericByteForm.Zoned or NumericByteForm.None) return null;
        // A FLOAT window holds the item's IEEE interchange bytes (§13.18.60.4 GR13–GR15, the wave-2 pin): decode
        // through the distinctly-named float lane and render through the SAME CobolFloat.Display the native float
        // arm below uses, including the CobolFloat.Sending guard (EC-DATA-NOT-FINITE, §14.6.13.2 item 3) unless
        // the caller is an exempt context. deSign is a no-op: §14.9.25.4 GR6a speaks of an item "described as
        // being signed numeric" — a float item has no PICTURE and no operational sign to drop.
        if (pic.IsFloat)
        {
            string dec = RuntimeApi.NumParseImageFloat(PlaceRenderer.Read(p), p.Item.ProfileName);
            return RuntimeApi.FloatDisplay(floatCheck ? RuntimeApi.FloatSending(dec) : dec);
        }
        // A 16-byte UNSIGNED BinaryCapacity window (UInt128 carrier, kb/Work R10): the unsigned parse twin
        // reinterprets the signed lane's Int128 bit-identically, and the U-named format lane keeps the full
        // [0, 2^128) range — picked by NAME, never by overload (see CobolNum.FormatDisplayU).
        if (pic.IsUnsignedWideBinary)
        {
            string uv = RuntimeApi.NumParseImageU128(PlaceRenderer.Read(p), p.Item.ProfileName);
            return deSign
                ? PExpand($"CobolNum.FormatUnsignedDisplayU({uv}, {pic.Digits})", pic)
                : RuntimeApi.NumFormatDisplay(uv, p.Item.ProfileName, u: true);
        }
        string value = RuntimeApi.NumParseImage(PlaceRenderer.Read(p), p.Item.ProfileName);
        // GR6a: an alphanumeric move/compare drops the operational sign — the magnitude digits, never the
        // BinaryMinus form (which is VARIABLE width and would shift a fixed receiver).
        return deSign
            ? PExpand(RuntimeApi.NumFormatUnsignedDisplay(value, pic.Digits), pic)
            : RuntimeApi.NumFormatDisplay(value, p.Item.ProfileName);
    }

    private static string FieldAsString(Place p, bool deSign = false, bool floatCheck = true)
    {
        // A reference-modified result is an elementary ALPHANUMERIC item regardless of the underlying item's
        // category (ISO §8.4.3.3.4 GR6) — its Read() is already the character slice (a numeric inner goes through
        // NumericImagePlace), never the numeric format path.
        if (p is RefModPlace) return PlaceRenderer.Read(p);
        // A Tier-B REDEFINES view's Read() is already its character-image window (a string), for a group or an
        // elementary view alike — use it directly (no .AsImage(), no FormatDisplay). EXCEPT for a NUMERIC view
        // whose bytes are not text: every non-zoned byte form (binary / packed / COMP-5 / IEEE float) decodes
        // through NonTextBytes and re-renders its DISPLAY image, so the view prints what its NATIVE twin prints
        // (§13.18.44.4 GR1 — one storage, two descriptions). The zoned case falls through to the window verbatim,
        // and a signed zoned view that is the de-signed source of an alphanumeric move/compare re-emits its
        // magnitude digits (ISO §14.9.25.4 GR6a) exactly as the StoreAsImage branch below does.
        if (p is RedefViewPlace)
            return NonTextBytes(p, deSign, floatCheck) is { } rvBytes ? rvBytes
                : deSign && p.Item.Pic is { Category: PicCategory.Numeric, Signed: true } rvp
                ? PExpand(RuntimeApi.NumFormatUnsignedDisplay(RuntimeApi.NumParseImage(PlaceRenderer.Read(p), p.Item.ProfileName), rvp.Digits), rvp)
                : PlaceRenderer.Read(p);
        // A group operand's character image is the generated AsImage(): each string-stored leaf contributes its
        // characters; a DISPLAY leaf its zoned digits; and a BINARY/PACKED leaf ⛔ ITS TRUE BYTES — radix-2
        // two's complement of StorageWidth, or BCD with a trailing sign nibble (V59). Implementor-defined
        // territory (§8.8.4.2.3 SR2 + §8.8.4.2.7: a group operand is class alphanumeric compared over its
        // REPRESENTATION, and §13.18.60.4 GR4/GR11 leave a binary/packed item's representation, including its
        // sign, to the implementor — COBOLNET_DESIGN §14.4 and docs/CONFORMANCE.md items 205–215 are the ONE
        // total definition).
        // ⚠ THIS COMMENT USED TO SAY "its zoned decimal digit image … with a trailing-overpunch sign", which was
        // true of the PRE-V59 image and is now false — the bytes are not digits. Corrected rather than left, since
        // a stale comment describing the old representation is exactly how the two-predicate residue below spread.
        // Only a VARIABLE-LENGTH group or a group with a pointer/object-class leaf has no image and stays loud
        // (kb/Work PB164 + R40 — there is no "Tier-C island" left for a numeric leaf: every numeric usage has a
        // pinned byte form). This is the WRITE / RELEASE / DISPLAY / compare sender path.
        // A BIT GROUP operates as an elementary boolean item of PICTURE 1(m) (§13.18.29.4 GR1b; D20/PB79): its
        // operand value is its BIT STRING (the subordinates' boolean positions concatenated), never the packed
        // byte image AsImage yields; a NATIONAL group's operand value IS its character image (GR2b).
        // ⛔ THIS TEST PRECEDES THE OdoGroupPlace ARM, AND THE ORDER IS THE FIX (kb/Work PB173): the ODO early
        // return used to sit above it and steal every bit group with an occurs-depending table into the CHARACTER
        // reader — two different alphabets on one operand (`IF G = B"1100"` compared packed bytes against a boolean
        // literal), and the character-unit extent it then computed was NEGATIVE for a sub-byte element, so the
        // operand rendered as the EMPTY string. `SendingBits` is the ONE bit reader and carries the GR8a
        // current-extent arm itself, so the ODO shape is served here rather than routed past.
        if (p.Item.IsAsIfElementary && p.Item.GroupUsage is GroupUsage.Bit) return PlaceRenderer.SendingBits(p);
        // An occurs-depending GROUP operand SENDS only the current-count part (ISO §13.18.38 GR8 — "that part of the
        // table area specified by data-name-1 at the start of the operation"); a zero count with no fixed prefix is
        // the zero-length item of §8.5.4. This is the read side of every quadrant (MOVE/compare/INSPECT/STRING/
        // UNSTRING source); the receiving direction split lives at the store sites.
        if (p is OdoGroupPlace odo) return PlaceRenderer.SendingGroupImage(odo);
        // THE ONE READER (kb/Work PB178). Reaching here p is neither a RefModPlace, an OdoGroupPlace nor a
        // RedefViewPlace — the three early returns above own those shapes — so this is byte-identical to the
        // `Read(p).AsImage()` it replaces; routing it anyway is what removes the third self-spelled copy and
        // lets the source-level drift test (BoundaryImageChannelTests) hold the law.
        if (p.Item.IsGroup) return PlaceRenderer.GroupImage(p);
        // A numeric-DISPLAY leaf stored as its character image is already a string holding the (sign-aware) image; when
        // it is the de-signed source of an alphanumeric move/compare, decode and re-emit the magnitude digits (GR6a).
        if (p.Item.StoreAsImage)
            return NonTextBytes(p, deSign, floatCheck) is { } siBytes ? siBytes
                : deSign && p.Item.Pic is { Category: PicCategory.Numeric, Signed: true } sip
                ? PExpand(RuntimeApi.NumFormatUnsignedDisplay(RuntimeApi.NumParseImage(PlaceRenderer.Read(p), p.Item.ProfileName), sip.Digits), sip)
                : PlaceRenderer.Read(p);
        return p.Item.Pic switch
        {
            // ISO §14.9.25.4 GR6a: a signed numeric moved to / compared as an alphanumeric item drops its operational
            // sign — the de-signed magnitude digits (FormatUnsignedDisplay), not the zoned/overpunch image. FormatDisplay
            // already yields these for an unsigned item, so deSign on an unsigned numeric is a no-op.
            // A UInt128-carrier item (kb/Work R10) renders through the U-named lane — picked by name, never by
            // overload (an int constant converts implicitly to both Int128 and UInt128; see CobolNum.FormatDisplayU).
            { IsUnsignedWideBinary: true } pic => deSign
                ? PExpand($"CobolNum.FormatUnsignedDisplayU({PlaceRenderer.Read(p)}, {pic.Digits})", pic)
                : RuntimeApi.NumFormatDisplay(PlaceRenderer.Read(p), p.Item.ProfileName, u: true),
            { Category: PicCategory.Numeric, IsFloat: false } pic => deSign
                ? PExpand($"CobolNum.FormatUnsignedDisplay({PlaceRenderer.Read(p)}, {pic.Digits})", pic)
                : $"CobolNum.FormatDisplay({PlaceRenderer.Read(p)}, {p.Item.ProfileName})",
            // A float item (COMP-1/2/FLOAT-*, D16): DISPLAY renders the algebraic value via CobolFloat.Display
            // (invariant-culture shortest round-trip, §14.9.11 GR1 implementor-defined) — never a bare .ToString().
            // The sending read is wrapped in CobolFloat.Sending (raises EC-DATA-NOT-FINITE for NaN/±Inf under checking,
            // §14.6.13.2 item 3) UNLESS this is an exempt context (class condition / VALIDATE — floatCheck false).
            { Category: PicCategory.Numeric } => floatCheck
                ? RuntimeApi.FloatDisplay(RuntimeApi.FloatSending(PlaceRenderer.Read(p)))
                : RuntimeApi.FloatDisplay(PlaceRenderer.Read(p)),
            // National and boolean items are string-stored (D-N1/D-B1) — the value IS the character image.
            { Category: PicCategory.Alphanumeric or PicCategory.NumericEdited
                or PicCategory.National or PicCategory.Boolean } => PlaceRenderer.Read(p),
            _ => $"{PlaceRenderer.Read(p)}.ToString()",
        };
    }

    /// <summary>The sending character image of a numeric item whose PICTURE has <c>P</c> scaling positions: the Ps
    /// are counted in the sending size and are ZEROS (ISO §13.18.40.3 symbol-P operations item b; §14.9.25.4 GR6a)
    /// — appended for trailing P (a negative scale), prepended for leading P (scale &gt; digit count). The zero
    /// runs are compile-time constants, so they concatenate as literals.</summary>
    private static string PExpand(string digitsExpr, PicInfo pic)
    {
        int trailing = pic.Scale < 0 ? -pic.Scale : 0;
        int leading = pic.Scale > pic.Digits ? pic.Scale - pic.Digits : 0;
        if (trailing > 0) return $"({digitsExpr} + \"{new string('0', trailing)}\")";
        if (leading > 0) return $"(\"{new string('0', leading)}\" + {digitsExpr})";
        return digitsExpr;
    }

    /// <summary>The operand→DISPLAY-image dispatch (PHASE-07 Step 6f). <paramref name="deSign"/> carried on the
    /// instance so the two cached instances need no per-call allocation. Each Visit is the former <c>AsString</c>
    /// switch arm verbatim.</summary>
    private sealed class AsStringVisitor(bool deSign, bool floatCheck) : IBoundOperandVisitor<string>
    {
        public string Visit(BoundStringLiteral n) => EmitText.CsLiteral(n.Value);
        public string Visit(BoundNumericLiteral n) => EmitText.CsLiteral(n.Text);
        public string Visit(BoundFieldOperand n) => FieldAsString(n.Place, deSign, floatCheck);
        // A bare figurative is intercepted PCS-aware at AsString's ENTRY (the collating context lives on the
        // renderer, not this visitor); this arm is the unreachable native-pin fallback the visitor interface requires.
        public string Visit(BoundFigurative n) => $"new string({FigurativeConstants.Fill(n.Kind, null)}, 1)";   // DISPLAY shows one occurrence (GR3)
        public string Visit(BoundAllLiteral n) => EmitText.CsLiteral(n.Literal);                          // length-unspecified: the literal once (GR3c)
        // EVERY intrinsic-result operand is intercepted at AsString's ENTRY (it needs the per-unit INSTANCE
        // renderer — P7 Step 12): alphanumeric/national/boolean through the string channel, numeric through
        // NumericIntrinsicText (DA2). What reaches this arm is a computed operand that is NOT an intrinsic — an
        // arithmetic expression in a string position, which no general format admits — so it stays loud.
        public string Visit(BoundComputedOperand n) =>
            EmitText.LoudValue("string", "computed expression in a string context");
        public string Visit(BoundOperandError n) => EmitText.LoudValue("string", n.Feature);
        // A boolean EXPRESSION operand is intercepted at AsString's ENTRY (it needs the per-unit renderer); this
        // cached visitor arm is the unreachable backstop.
        public string Visit(BoundBoolOperand n) => EmitText.LoudValue("string", $"bound operand '{nameof(BoundBoolOperand)}'");
    }

    /// <summary>The "compared as text?" predicate (PHASE-07 Step 6f) — the former <c>IsString</c> switch, with the
    /// four <c>_ => false</c> leaves now explicit.</summary>
    private sealed class IsStringVisitor : IBoundOperandVisitor<bool>
    {
        public bool Visit(BoundStringLiteral n) => true;
        public bool Visit(BoundAllLiteral n) => true;   // ALL "literal" is an alphanumeric figurative
        public bool Visit(BoundFieldOperand n) => n.Place.Item.IsGroup || n.Place.Item.Pic?.Category
            is PicCategory.Alphanumeric or PicCategory.NumericEdited
            or PicCategory.National or PicCategory.Boolean;
        // An intrinsic result compares by its §15.2 function type: alphanumeric functions are class/category
        // alphanumeric (IF107A's `IF FUNCTION CURRENT-DATE >= TEMP1` is a STRING comparison); a boolean
        // function's '0'/'1' image compares as text likewise (§8.8.4.3 over the D-B1 substrate);
        // numeric/integer functions stay numeric operands. A computed operand that is NOT an intrinsic is not text.
        public bool Visit(BoundComputedOperand n) =>
            n.Expr is BoundIntrinsicCall ic
                && ic.ResultCategory is PicCategory.Alphanumeric or PicCategory.National or PicCategory.Boolean;
        public bool Visit(BoundFigurative n) => false;
        public bool Visit(BoundNumericLiteral n) => false;
        public bool Visit(BoundOperandError n) => false;
        public bool Visit(BoundBoolOperand n) => false;
    }
}
