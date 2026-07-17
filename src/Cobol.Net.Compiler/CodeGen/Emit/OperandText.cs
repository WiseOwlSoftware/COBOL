// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;

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
    private static readonly AsStringVisitor _asStringPlain = new(deSign: false);
    private static readonly AsStringVisitor _asStringDeSign = new(deSign: true);
    private static readonly IsStringVisitor _isString = new();

    public static string AsString(BoundOperand op, NumericRenderer num, bool deSign = false) =>
        op is BoundComputedOperand { Expr: BoundIntrinsicCall { ResultCategory: PicCategory.Alphanumeric or PicCategory.National or PicCategory.Boolean } ic }
            ? num.Intrinsics.RenderString(ic)
            : op.Accept(deSign ? _asStringDeSign : _asStringPlain);

    /// <summary>A data item's character image directly from its <see cref="Place"/> — the num-free entry for
    /// callers that hold a Place (a FIELD can never be an intrinsic operand, so no per-unit renderer is
    /// needed). Same rendering as <see cref="AsString"/> over a field operand.</summary>
    public static string FieldImage(Place p, bool deSign = false) => FieldAsString(p, deSign);

    /// <summary>True if an operand is compared as text (an alphanumeric literal, or an alphanumeric/edited/group
    /// field — a group compares as alphanumeric, ISO §8.8.4.1.1).</summary>
    public static bool IsString(BoundOperand op) => op.Accept(_isString);

    private static string FieldAsString(Place p, bool deSign = false)
    {
        // A reference-modified result is an elementary ALPHANUMERIC item regardless of the underlying item's
        // category (ISO §8.4.2.4) — its Read() is already the character slice (a numeric inner goes through
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
            return deSign && p.Item.Pic is { Category: PicCategory.Numeric, Signed: true } rvp
                ? PExpand($"CobolNum.FormatUnsignedDisplay(CobolNum.ParseDisplay({PlaceRenderer.Read(p)}, {p.Item.ProfileName}), {rvp.Digits})", rvp)
                : PlaceRenderer.Read(p);
        // A group operand's character image is the generated AsImage(): each string-stored leaf contributes its
        // characters, each NATIVE fixed-point leaf (DISPLAY/BINARY/PACKED) its zoned decimal digit image —
        // implementor-defined territory (ISO §8.8.4.1.1: a group operand is alphanumeric over the item's
        // representation, and §13.18.60 USAGE GR4 leaves a binary item's representation, including its sign, to
        // the implementor; the legacy byte engine used hardware bytes, the greenfield defines the digit image
        // with a trailing-overpunch sign — COBOLNET_DESIGN §14.4, the ONE total definition; the inline
        // MixedGroupImage concat it supersedes mis-imaged a signed negative COMP leaf variable-width and bailed
        // on fixed-OCCURS children). Only a group with a float / COMP-5 / INDEX leaf stays the loud Tier-C
        // island. This is the WRITE / RELEASE / DISPLAY / compare sender path.
        if (p.Item.IsGroup)
            return p.Item.IsImageCapable
                ? $"{PlaceRenderer.Read(p)}.AsImage()"
                : EmitText.LoudValue("string", TierCIsland.Reason(p.Item, "whole-group image of"));
        // A numeric-DISPLAY leaf stored as its character image is already a string holding the (sign-aware) image; when
        // it is the de-signed source of an alphanumeric move/compare, decode and re-emit the magnitude digits (GR6a).
        if (p.Item.StoreAsImage)
            return deSign && p.Item.Pic is { Category: PicCategory.Numeric, Signed: true } sip
                ? PExpand($"CobolNum.FormatUnsignedDisplay(CobolNum.ParseDisplay({PlaceRenderer.Read(p)}, {p.Item.ProfileName}), {sip.Digits})", sip)
                : PlaceRenderer.Read(p);
        return p.Item.Pic switch
        {
            // ISO §14.9.25.4 GR6a: a signed numeric moved to / compared as an alphanumeric item drops its operational
            // sign — the de-signed magnitude digits (FormatUnsignedDisplay), not the zoned/overpunch image. FormatDisplay
            // already yields these for an unsigned item, so deSign on an unsigned numeric is a no-op.
            { Category: PicCategory.Numeric, IsFloat: false } pic => deSign
                ? PExpand($"CobolNum.FormatUnsignedDisplay({PlaceRenderer.Read(p)}, {pic.Digits})", pic)
                : $"CobolNum.FormatDisplay({PlaceRenderer.Read(p)}, {p.Item.ProfileName})",
            // A float item (COMP-1/2/FLOAT-*, D16): DISPLAY renders the algebraic value via CobolFloat.Display
            // (invariant-culture shortest round-trip, §14.9.11 GR1 implementor-defined) — never a bare .ToString().
            { Category: PicCategory.Numeric } => $"CobolFloat.Display({PlaceRenderer.Read(p)})",
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
    private sealed class AsStringVisitor(bool deSign) : IBoundOperandVisitor<string>
    {
        public string Visit(BoundStringLiteral n) => EmitText.CsLiteral(n.Value);
        public string Visit(BoundNumericLiteral n) => EmitText.CsLiteral(n.Text);
        public string Visit(BoundFieldOperand n) => FieldAsString(n.Place, deSign);
        public string Visit(BoundFigurative n) => $"new string({FigurativeConstants.Fill(n.Kind, null)}, 1)";   // DISPLAY shows one occurrence (GR3)
        public string Visit(BoundAllLiteral n) => EmitText.CsLiteral(n.Literal);                          // length-unspecified: the literal once (GR3c)
        // An ALPHANUMERIC/NATIONAL-result intrinsic operand is intercepted at AsString's ENTRY (it renders
        // through the per-unit INSTANCE intrinsic channel — P7 Step 12); what reaches this arm is a NUMERIC
        // computed operand in a string context, which stays the loud named channel by design.
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
