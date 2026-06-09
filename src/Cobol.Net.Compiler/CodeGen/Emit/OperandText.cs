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
        _ => false,
    };

    private static string FieldAsString(Place p, bool deSign = false)
    {
        // A Tier-B REDEFINES view's Read() is already its character-image window (a string), for a group or an
        // elementary view alike — use it directly (no .AsImage(), no FormatDisplay). EXCEPT when this signed-numeric
        // view is the de-signed source of an alphanumeric move/compare: its window holds the sign-aware image
        // (over-punch or separate sign), so decode and re-emit the magnitude digits (ISO §14.9.25.4 GR6a), exactly
        // as the StoreAsImage branch below does — the same de-sign rule, just a different storage shape.
        if (p is RedefViewPlace)
            return deSign && p.Item.Pic is { Category: PicCategory.Numeric, Signed: true } rvp
                ? $"CobolNum.FormatUnsignedDisplay(CobolNum.ParseDisplay({p.Read()}, {p.Item.ProfileName}), {rvp.Digits})"
                : p.Read();
        if (p.Item.IsGroup)
            return p.Item.IsCharacterImage
                ? $"{p.Read()}.AsImage()"
                : EmitText.LoudValue("string", $"whole-group image of mixed-usage '{p.Item.CobolName}' with a COMP/binary leaf (Tier-C byte path, deferred)");
        // A numeric-DISPLAY leaf stored as its character image is already a string holding the (sign-aware) image; when
        // it is the de-signed source of an alphanumeric move/compare, decode and re-emit the magnitude digits (GR6a).
        if (p.Item.StoreAsImage)
            return deSign && p.Item.Pic is { Category: PicCategory.Numeric, Signed: true } sip
                ? $"CobolNum.FormatUnsignedDisplay(CobolNum.ParseDisplay({p.Read()}, {p.Item.ProfileName}), {sip.Digits})"
                : p.Read();
        return p.Item.Pic switch
        {
            // ISO §14.9.25.4 GR6a: a signed numeric moved to / compared as an alphanumeric item drops its operational
            // sign — the de-signed magnitude digits (FormatUnsignedDisplay), not the zoned/overpunch image. FormatDisplay
            // already yields these for an unsigned item, so deSign on an unsigned numeric is a no-op.
            { Category: PicCategory.Numeric, IsFloat: false } pic => deSign
                ? $"CobolNum.FormatUnsignedDisplay({p.Read()}, {pic.Digits})"
                : $"CobolNum.FormatDisplay({p.Read()}, {p.Item.ProfileName})",
            { Category: PicCategory.Numeric } => $"{p.Read()}.ToString()",            // COMP-1/2 float — refine later
            { Category: PicCategory.Alphanumeric or PicCategory.NumericEdited } => p.Read(),
            _ => $"{p.Read()}.ToString()",
        };
    }
}
