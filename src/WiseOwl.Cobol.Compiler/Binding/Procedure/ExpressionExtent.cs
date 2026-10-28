// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Common;

namespace WiseOwl.Cobol.Binding.Procedure;

/// <summary>The exact size of an arithmetic expression's value: it is a multiple of 10<sup>−<see cref="Scale"/></sup>
/// and its magnitude is below 10<sup><see cref="IntegerDigits"/></sup>, so a fixed-point item of
/// <c>IntegerDigits + Scale</c> digit positions at <c>Scale</c> fraction digits holds it whole.</summary>
/// <param name="IntegerDigits">The digit positions left of the point (zero for a value below 1).</param>
/// <param name="Scale">The fraction digits (never negative: a scaled-integer operand reports its trailing positions
/// as integer digits).</param>
internal readonly record struct ExactExtent(int IntegerDigits, int Scale);

/// <summary>
/// ⛔ <b>HOW MANY DIGITS AN ARITHMETIC EXPRESSION'S VALUE CAN HAVE, ASKED AT BIND TIME</b> (kb/Work PB3974) — the one
/// fact the implementor's intermediate result item of an EVALUATE selection subject needs to be described FOR its
/// value. ISO §14.9.13.4 GR3 c): "Any selection subject specified by arithmetic-expression-1 is assigned a numeric
/// value according to the rules for evaluating an arithmetic expression", and the WHEN objects are compared with
/// THAT value (GR4 a) 6.), so an intermediate that holds fewer digits than the expression produces compares a
/// different number: `EVALUATE BIG * BIG WHEN BIG * BIG` over a PIC 9(18) item took WHEN OTHER through a
/// S9(21)V9(17) item that kept 21 of the product's 36 integer digits.
/// <para>The bound is a MATHEMATICAL fact of the operands, not a carrier decision, so it needs no copy of the
/// renderer's lane rules: a sum of operands below 10<sup>i</sup> and 10<sup>j</sup> is below 10<sup>max(i, j)+1</sup>, a
/// product below 10<sup>i+j</sup> with the scales added, a negation changes neither. It is stated only for the
/// operations whose value the expression evaluates EXACTLY under every arithmetic mode — <c>+</c>, <c>-</c>, <c>*</c> and
/// unary minus over numeric literals and fixed-point items (a quotient is truncated at a working scale the receiver
/// decides, an exponentiation or a function value may be a binary64, an SDIDI or an exact value of no stated width) — and
/// is <see langword="null"/> for anything else, whose intermediate keeps the value-operand description
/// (<see cref="SendingValueTemp"/>'s <c>ValueOperandPic</c>). A visitor over the generated expression interface, as
/// <c>NumericRenderer</c> and <see cref="FunctionValueLane"/>'s argument visitor are, so a bound node added
/// tomorrow is a COMPILE error here rather than a silent "unknown".</para>
/// </summary>
internal sealed class ExpressionExtent : IBoundExprVisitor<ExactExtent?>
{
    private static readonly ExpressionExtent Instance = new();

    /// <summary>The exact extent of <paramref name="expression"/>'s value, or null when the expression has an operation
    /// or operand whose value this bound does not state (see the type's remarks).</summary>
    public static ExactExtent? Of(BoundExpr expression) => expression.Accept(Instance);

    public ExactExtent? Visit(BoundNumLiteral n) =>
        // A floating-point literal is an SDIDI operand (owner decision D-B) and a folded binary float carrier is a
        // binary64 value: neither is a fixed-point extent. Every other literal states its own digits.
        n.Carrier is null && !NumericLiteral.IsFloatingPointForm(n.Text)
            && NumericLiteral.TryUnscaledParts(n.Text, out string digits, out int scale)
            ? Extent(NumericLiteral.UnscaledDigitBound(digits), scale)
            : null;

    public ExactExtent? Visit(BoundNumRef n)
    {
        // A reference-modified result and a group have no numeric description of their own to read a bound from.
        if (n.Place is RefModPlace || n.Place.DenotedItem is null) return null;
        // A floating-point item is a binary64 and a numeric-EDITED one de-edits onto the SDIDI: neither is exact.
        if (n.Place.Item.Pic is not { Category: PicCategory.Numeric, IsFloat: false } pic || pic.Usage is Usage.Index) return null;
        return Extent(pic.UnscaledDigitBound, pic.Scale);
    }

    public ExactExtent? Visit(BoundBinary n)
    {
        if (n.Left.Accept(this) is not { } left || n.Right.Accept(this) is not { } right) return null;
        return n.Op switch
        {
            '+' or '-' => new ExactExtent(Math.Max(left.IntegerDigits, right.IntegerDigits) + 1, Math.Max(left.Scale, right.Scale)),
            '*' => new ExactExtent(left.IntegerDigits + right.IntegerDigits, left.Scale + right.Scale),
            _ => null,   // a quotient: truncated at the working scale (NumericRenderer.Divide), not an exact value
        };
    }

    public ExactExtent? Visit(BoundNegate n) => n.Operand.Accept(this);

    public ExactExtent? Visit(BoundPower n) => null;
    public ExactExtent? Visit(BoundIntrinsicCall n) => null;
    public ExactExtent? Visit(BoundIndexRef n) => null;
    public ExactExtent? Visit(BoundLinageCounterRef n) => null;
    public ExactExtent? Visit(BoundReportCounterRef n) => null;
    public ExactExtent? Visit(BoundReportVaryingRef n) => null;
    public ExactExtent? Visit(BoundOdoExtent n) => null;
    public ExactExtent? Visit(BoundExprError n) => null;
    public ExactExtent? Visit(BoundUdfEvaluatedExpr n) => null;

    /// <summary>The extent of an operand whose unscaled value has at most <paramref name="unscaledDigits"/> digits at
    /// <paramref name="scale"/>: a positive scale leaves <c>unscaledDigits − scale</c> integer digits; a NEGATIVE scale
    /// (a PICTURE-P item, an exponent-form literal) means the value is a multiple of a power of ten, so the integer digits
    /// are the unscaled digits plus the exponent and the scale is zero (an over-count, as for a PICTURE-P item whose
    /// <see cref="PicInfo.UnscaledDigitBound"/> already counts the P positions, is safe; an under-count is not).</summary>
    private static ExactExtent Extent(int unscaledDigits, int scale) =>
        scale >= 0
            ? new ExactExtent(Math.Max(unscaledDigits - scale, 0), scale)
            : new ExactExtent(unscaledDigits - scale, 0);
}
