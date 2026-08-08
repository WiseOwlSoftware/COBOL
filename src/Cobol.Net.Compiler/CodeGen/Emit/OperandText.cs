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
        // A bare figurative (HIGH-VALUE/LOW-VALUE/SPACE/…) in a DISPLAY/STRING/STOP value position is an
        // alphanumeric value (§8.3.3.6.4 GR1) of one character (GR3b). Materialize it through the declared collating
        // tables so HIGH-/LOW-VALUE is the runtime-collating extreme (§8.3.3.6.4 GR6/GR7), matching the MOVE and
        // relation paths — not the native pin. Intercepted at the ENTRY (like the intrinsic channel) because the
        // AsStringVisitor has no access to the renderer's collating context; cat=null ⇒ the alphanumeric PCS applies.
        : op is BoundFigurative fig
            ? $"new string({FigurativeConstants.Fill(fig.Kind, num.Collating, null, num.NationalCollating)}, 1)"
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
    private static string NumericIntrinsicText(NumericRenderer num, BoundIntrinsicCall ic, bool deSign)
    {
        NumX x = num.Render(ic, ReceiverContext.None);
        return x.Real ? RuntimeApi.FloatDisplay(x.Expr)
             : x.Dec ? RuntimeApi.DecFunctionText(x.Expr, deSign)
             : RuntimeApi.NumFormatFunctionText(x.Expr, x.Scale, deSign);
    }

    /// <summary>A data item's character image directly from its <see cref="Place"/> — the num-free entry for
    /// callers that hold a Place (a FIELD can never be an intrinsic operand, so no per-unit renderer is
    /// needed). Same rendering as <see cref="AsString"/> over a field operand.</summary>
    public static string FieldImage(Place p, bool deSign = false, bool floatCheck = true) => FieldAsString(p, deSign, floatCheck);

    /// <summary>True if an operand is compared as text (an alphanumeric literal, or an alphanumeric/edited/group
    /// field — a group compares as alphanumeric, ISO §8.8.4.1.1).</summary>
    public static bool IsString(BoundOperand op) => op.Accept(_isString);

    /// <summary>⛔ BYTES ARE NOT TEXT (V59). An image-STORED BINARY/PACKED leaf holds its radix-2 / BCD bytes, and
    /// those bytes are not the item's alphanumeric text: an ELEMENTARY numeric operand used as text is "treated as
    /// though it were moved to an alphanumeric data item" (ISO §14.9.25.4 GR6, and §8.8.4.2.2 for a numeric ↔
    /// nonnumeric comparison), which yields its DIGITS. So decode the stored bytes and re-render the DISPLAY image.
    /// Returns null for every ZONED item — there the stored image IS the text, and passing the window through
    /// verbatim also preserves the incompatible content a group MOVE can legitimately deposit (spaces in a numeric
    /// leaf), which a decode-and-reformat round trip would silently turn into zeros.
    /// <para>A GROUP operand is the opposite case and is handled before this: §8.8.4.1.1 makes it alphanumeric over
    /// the items' REPRESENTATION, so its text IS the record image, bytes and all.</para></summary>
    private static string? NonTextBytes(Place p, bool deSign)
    {
        if (p.Item.Pic is not { Category: PicCategory.Numeric, IsFloat: false } pic
            || pic.ByteForm is NumericByteForm.Zoned or NumericByteForm.None) return null;
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
        // An occurs-depending GROUP operand SENDS only the current-count part (ISO §13.18.38 GR8 — "that part of the
        // table area specified by data-name-1 at the start of the operation"); a zero count with no fixed prefix is
        // the zero-length item of §8.5.4. This is the read side of every quadrant (MOVE/compare/INSPECT/STRING/
        // UNSTRING source); the receiving direction split lives at the store sites.
        if (p is OdoGroupPlace odo) return PlaceRenderer.SendingImage(odo);
        // A Tier-B REDEFINES view's Read() is already its character-image window (a string), for a group or an
        // elementary view alike — use it directly (no .AsImage(), no FormatDisplay). EXCEPT when this signed-numeric
        // view is the de-signed source of an alphanumeric move/compare: its window holds the sign-aware image
        // (over-punch or separate sign), so decode and re-emit the magnitude digits (ISO §14.9.25.4 GR6a), exactly
        // as the StoreAsImage branch below does — the same de-sign rule, just a different storage shape.
        if (p is RedefViewPlace)
            return NonTextBytes(p, deSign) is { } rvBytes ? rvBytes
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
        // Only a group with a float / COMP-5 / INDEX leaf stays the loud Tier-C island. This is the
        // WRITE / RELEASE / DISPLAY / compare sender path.
        if (p.Item.IsGroup)
            return p.Item.IsImageCapable
                ? $"{PlaceRenderer.Read(p)}.AsImage()"
                : EmitText.LoudValue("string", TierCIsland.Reason(p.Item, "whole-group image of"));
        // A numeric-DISPLAY leaf stored as its character image is already a string holding the (sign-aware) image; when
        // it is the de-signed source of an alphanumeric move/compare, decode and re-emit the magnitude digits (GR6a).
        if (p.Item.StoreAsImage)
            return NonTextBytes(p, deSign) is { } siBytes ? siBytes
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
        // A class-boolean operand has no alphanumeric image (the former loud `_ =>` default; byte-identical value).
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
