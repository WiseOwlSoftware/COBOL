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
    /// <summary>A bound operand rendered as a C# <see cref="string"/> (its DISPLAY image).</summary>
    public static string AsString(BoundOperand op) => op switch
    {
        BoundStringLiteral s => EmitText.CsLiteral(s.Value),
        BoundNumericLiteral n => EmitText.CsLiteral(n.Text),
        BoundFieldOperand f => FieldAsString(f.Place),
        BoundComputedOperand => EmitText.LoudValue("string", "computed expression in a string context"),
        BoundOperandError e => EmitText.LoudValue("string", e.Feature),
        _ => EmitText.LoudValue("string", $"bound operand '{op.GetType().Name}'"),
    };

    /// <summary>True if an operand is compared as text (an alphanumeric literal or an alphanumeric/edited field).</summary>
    public static bool IsString(BoundOperand op) => op switch
    {
        BoundStringLiteral => true,
        BoundFieldOperand f => f.Place.Item.Pic?.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited,
        _ => false,
    };

    private static string FieldAsString(Place p)
    {
        if (p.Item.IsGroup) return EmitText.LoudValue("string", $"DISPLAY of group item '{p.Item.CobolName}' (whole-group image is G6)");
        return p.Item.Pic switch
        {
            { Category: PicCategory.Numeric, IsFloat: false } => $"CobolNum.FormatDisplay({p.Read()}, {p.Item.ProfileName})",
            { Category: PicCategory.Numeric } => $"{p.Read()}.ToString()",            // COMP-1/2 float — refine later
            { Category: PicCategory.Alphanumeric or PicCategory.NumericEdited } => p.Read(),
            _ => $"{p.Read()}.ToString()",
        };
    }
}
