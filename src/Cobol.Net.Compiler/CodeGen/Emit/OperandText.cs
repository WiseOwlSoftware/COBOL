// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
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
    /// digits, not the zoned/overpunch image). DISPLAY leaves it unset (it shows the sign-aware image).</summary>
    public static string AsString(BoundOperand op, bool deSign = false) => op switch
    {
        BoundStringLiteral s => EmitText.CsLiteral(s.Value),
        BoundNumericLiteral n => EmitText.CsLiteral(n.Text),
        BoundFieldOperand f => FieldAsString(f.Place, deSign),
        BoundFigurative f => $"new string({EmitText.FigurativeFill(f.Kind)}, 1)",   // DISPLAY shows one occurrence (GR3)
        BoundAllLiteral a => EmitText.CsLiteral(a.Literal),                          // length-unspecified: the literal once (GR3c)
        // An ALPHANUMERIC-result intrinsic (ISO §15.2 type 1 — a sending item of category alphanumeric): the one
        // case that lets MOVE-to-alphanumeric, string relational comparisons, and group moves take FUNCTION
        // operands unmodified. deSign is a no-op (the result carries no operational sign). A NUMERIC intrinsic
        // in a string context falls through to the loud computed-operand case (hazard H3 — by design).
        BoundComputedOperand { Expr: BoundIntrinsicCall { ResultCategory: PicCategory.Alphanumeric } ic } =>
            IntrinsicRenderer.RenderString(ic),
        BoundComputedOperand => EmitText.LoudValue("string", "computed expression in a string context"),
        BoundOperandError e => EmitText.LoudValue("string", e.Feature),
        _ => EmitText.LoudValue("string", $"bound operand '{op.GetType().Name}'"),
    };

    /// <summary>True if an operand is compared as text (an alphanumeric literal, or an alphanumeric/edited/group
    /// field — a group compares as alphanumeric, ISO §8.8.4.1.1).</summary>
    public static bool IsString(BoundOperand op) => op switch
    {
        BoundStringLiteral => true,
        BoundAllLiteral => true,   // ALL "literal" is an alphanumeric figurative
        BoundFieldOperand f => f.Place.Item.IsGroup || f.Place.Item.Pic?.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited,
        // An intrinsic result compares by its §15.2 function type: alphanumeric functions are class/category
        // alphanumeric (IF107A's `IF FUNCTION CURRENT-DATE >= TEMP1` is a STRING comparison); numeric/integer
        // functions stay numeric operands.
        BoundComputedOperand { Expr: BoundIntrinsicCall ic } => ic.ResultCategory == PicCategory.Alphanumeric,
        _ => false,
    };

    private static string FieldAsString(Place p, bool deSign = false)
    {
        // A reference-modified result is an elementary ALPHANUMERIC item regardless of the underlying item's
        // category (ISO §8.4.2.4) — its Read() is already the character slice (a numeric inner goes through
        // NumericImagePlace), never the numeric format path.
        if (p is RefModPlace) return p.Read();
        // An occurs-depending GROUP operand SENDS only the current-count part (ISO §13.18.38 GR8 — "that part of the
        // table area specified by data-name-1 at the start of the operation"); a zero count with no fixed prefix is
        // the zero-length item of §8.5.4. This is the read side of every quadrant (MOVE/compare/INSPECT/STRING/
        // UNSTRING source); the receiving direction split lives at the store sites.
        if (p is OdoGroupPlace odo) return odo.SendingImage();
        // A Tier-B REDEFINES view's Read() is already its character-image window (a string), for a group or an
        // elementary view alike — use it directly (no .AsImage(), no FormatDisplay). EXCEPT when this signed-numeric
        // view is the de-signed source of an alphanumeric move/compare: its window holds the sign-aware image
        // (over-punch or separate sign), so decode and re-emit the magnitude digits (ISO §14.9.25.4 GR6a), exactly
        // as the StoreAsImage branch below does — the same de-sign rule, just a different storage shape.
        if (p is RedefViewPlace)
            return deSign && p.Item.Pic is { Category: PicCategory.Numeric, Signed: true } rvp
                ? PExpand($"CobolNum.FormatUnsignedDisplay(CobolNum.ParseDisplay({p.Read()}, {p.Item.ProfileName}), {rvp.Digits})", rvp)
                : p.Read();
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
                ? $"{p.Read()}.AsImage()"
                : EmitText.LoudValue("string", $"whole-group image of '{p.Item.CobolName}' with a float/COMP-5/INDEX leaf (Tier-C byte island, deferred — COBOLNET_DESIGN §4.2)");
        // A numeric-DISPLAY leaf stored as its character image is already a string holding the (sign-aware) image; when
        // it is the de-signed source of an alphanumeric move/compare, decode and re-emit the magnitude digits (GR6a).
        if (p.Item.StoreAsImage)
            return deSign && p.Item.Pic is { Category: PicCategory.Numeric, Signed: true } sip
                ? PExpand($"CobolNum.FormatUnsignedDisplay(CobolNum.ParseDisplay({p.Read()}, {p.Item.ProfileName}), {sip.Digits})", sip)
                : p.Read();
        return p.Item.Pic switch
        {
            // ISO §14.9.25.4 GR6a: a signed numeric moved to / compared as an alphanumeric item drops its operational
            // sign — the de-signed magnitude digits (FormatUnsignedDisplay), not the zoned/overpunch image. FormatDisplay
            // already yields these for an unsigned item, so deSign on an unsigned numeric is a no-op.
            { Category: PicCategory.Numeric, IsFloat: false } pic => deSign
                ? PExpand($"CobolNum.FormatUnsignedDisplay({p.Read()}, {pic.Digits})", pic)
                : $"CobolNum.FormatDisplay({p.Read()}, {p.Item.ProfileName})",
            { Category: PicCategory.Numeric } => $"{p.Read()}.ToString()",            // COMP-1/2 float — refine later
            { Category: PicCategory.Alphanumeric or PicCategory.NumericEdited } => p.Read(),
            _ => $"{p.Read()}.ToString()",
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
}
