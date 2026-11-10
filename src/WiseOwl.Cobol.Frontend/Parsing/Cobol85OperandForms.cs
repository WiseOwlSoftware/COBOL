// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Frontend.Parsing;

using Core = CobolParserCore;

/// <summary>The sign an X3.23-1985 <c>integer</c> position admits (§1985:III.2: "neither signed nor zero unless explicitly
/// allowed by the rules for that format").</summary>
public enum Cobol85IntegerSign
{
    /// <summary>§1985:III.2's default: no sign.</summary>
    None,
    /// <summary>"may be signed and, if signed, it must be positive" (§1985:IV.4.3.8.2.3 SR6; §1985:VI.6.23.3 SR4's integer-1).</summary>
    Positive,
    /// <summary>"may be signed" with no condition on the value's sign (§1985:VI.6.23.3 SR4's integer-2).</summary>
    Any,
}

/// <summary>
/// ⛔ THE X3.23-1985 OPERAND FORMS, WRITTEN ONCE (kb/Work PB5074): the general-format <c>integer</c>, the literal rule "an
/// unsigned integer", and the 1985 subscript. They are readings of the parse tree, so they live beside
/// <see cref="GoToFormats"/>: the edition gate (<c>VersionConformancePass</c>) and the binders (<c>SubscriptBinder</c>,
/// <c>SetBinder</c>, <c>ControlFlowBinder</c>) ask the same function, and neither layer reaches into the other.
/// </summary>
public static class Cobol85OperandForms
{
    /// <summary>Whether a numeric literal is the X3.23-1985 <c>integer</c> of a general format: §1985:III.2 "integer must be a
    /// numeric literal which is an integer, and it must be neither signed nor zero unless explicitly allowed by the rules
    /// for that format". <paramref name="sign"/> is what the format's rules allow; no 1985 rule this reader serves allows
    /// zero, so zero is refused at every sign.</summary>
    public static bool IsInteger(Core.NumericLiteralContext literal, Cobol85IntegerSign sign) =>
        IsInteger(literal, outerSign: null, sign);

    /// <summary>The <see cref="IsInteger(Core.NumericLiteralContext, Cobol85IntegerSign)"/> question over an arithmetic
    /// operand, where the parser may read a contiguous sign as a unary operator before an unsigned literal: <c>+1</c> in a
    /// statement operand is the signed literal §1985:IV.4.2.2.2.1 describes, and a sign separated from its digits by a space
    /// is no literal at all, so only a sign token that touches the literal is taken as its sign.</summary>
    public static bool IsInteger(Core.UnaryExpressionContext unary, Cobol85IntegerSign sign)
    {
        if (unary.addOp() is not { } op) return unary.primaryExpression()?.numericLiteral() is { } bare && IsInteger(bare, sign);
        return unary.unaryExpression() is { } inner && inner.addOp() is null
            && inner.primaryExpression()?.numericLiteral() is { } literal
            && op.Stop.StopIndex + 1 == literal.Start.StartIndex
            && IsInteger(literal, op.MINUS() is null ? '+' : '-', sign);
    }

    private static bool IsInteger(Core.NumericLiteralContext literal, char? outerSign, Cobol85IntegerSign sign)
    {
        var signed = literal.signedNumericLiteral();
        var core = signed.numericLiteralCore();
        if (core.ChildCount != 1 || core.Start.Type is not (Core.INTEGERLIT or Core.SIGNED_INTEGERLIT)) return false;
        string digits = core.Start.Text;
        char written = signed.MINUS() is not null ? '-' : signed.PLUS() is not null ? '+'
            : digits[0] is '+' or '-' ? digits[0] : ' ';
        if (digits[0] is '+' or '-') digits = digits[1..];
        if (outerSign is { } outer)
        {
            if (written != ' ') return false;   // two signs: an expression, never one literal
            written = outer;
        }
        if (digits.AsSpan().Trim('0').IsEmpty) return false;
        return written switch
        {
            ' ' => true,
            '+' => sign is not Cobol85IntegerSign.None,
            _ => sign is Cobol85IntegerSign.Any,
        };
    }

    /// <summary>An unsigned integer literal: no sign written and no digit position right of the decimal point (zero is
    /// one). The "unsigned integer" of §1985:VI.6.10.3 SR2 and §1985:VI.6.24.3 SR3, a rule on a literal-1, not a general
    /// format's <c>integer</c> (<see cref="IsInteger(Core.NumericLiteralContext, Cobol85IntegerSign)"/>).</summary>
    public static bool IsUnsignedIntegerLiteral(Core.SignedNumericLiteralContext n) =>
        n.PLUS() is null && n.MINUS() is null && n.numericLiteralCore() is { ChildCount: 1 } core
        && core.Start.Type == Core.INTEGERLIT;

    /// <summary>Whether a written subscript has one of the X3.23-1985 forms (§1985:IV.4.3.8.2.2): integer-1, positive if
    /// signed (§1985:IV.4.3.8.2.3 SR6), or a data-name or index-name optionally followed by + or - an unsigned nonzero
    /// integer (§1985:III.2). The SHAPE half: <c>SubscriptBinder.ScreenCobol85SubscriptDataNames</c> asks the description
    /// half (SR5) of the subscripts this passes, so neither reports a subscript the other has.</summary>
    public static bool IsSubscript(Core.SubscriptItemContext item)
    {
        if (item.functionArgument()?.arithmeticExpression() is not { } arith) return true;   // ALL / a non-arithmetic operand: another rule's
        var terms = arith.additiveExpression().multiplicativeExpression();
        var first = SolePrimary(terms[0]);
        return terms.Length switch
        {
            1 => first?.dataReference() is not null
                || (terms[0].powerExpression() is [var power] && power.unaryExpression() is [var unary]
                    && IsInteger(unary, Cobol85IntegerSign.Positive)),
            2 => first?.dataReference() is not null
                && SolePrimary(terms[1])?.numericLiteral() is { } integer2 && IsInteger(integer2, Cobol85IntegerSign.None),
            _ => false,
        };
    }

    /// <summary>The data reference a subscript is when its first term is one, alone or before <c>{+|-} integer</c>; null
    /// otherwise.</summary>
    public static Core.DataReferenceContext? SubscriptDataName(Core.SubscriptItemContext item) =>
        item.functionArgument()?.arithmeticExpression()?.additiveExpression().multiplicativeExpression() is [var first, ..]
            ? SolePrimary(first)?.dataReference() : null;

    /// <summary>The primary a multiplicative term is when it is nothing more: no operator, no unary sign.</summary>
    private static Core.PrimaryExpressionContext? SolePrimary(Core.MultiplicativeExpressionContext term) =>
        term.powerExpression() is [var power] && power.unaryExpression() is [var unary] ? unary.primaryExpression() : null;
}
