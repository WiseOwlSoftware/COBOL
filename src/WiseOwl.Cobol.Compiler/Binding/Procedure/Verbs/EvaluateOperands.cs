// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler.Binding.Bound;
using WiseOwl.Cobol.Frontend.Parsing;

namespace WiseOwl.Cobol.Compiler.Binding.Procedure.Verbs;

using Core = CobolParserCore;

/// <summary>The selection-operand facts EVALUATE's two sides share (kb/Work PB2338): the ONE bare-operand
/// classification on Table 15's axes (§2023:14.9.13.3 SR10), which <see cref="EvaluateSubjectBinder"/> maps to a
/// COLUMN and <see cref="WhenPhraseBinder"/> reads as a ROW, and the ONE binding of a <c>valueOperand</c> subject or
/// object operand. One home, so the subject and the object cannot classify or bind the same shape two ways
/// (kb/Work PB400).</summary>
internal static class EvaluateOperands
{
    /// <summary>⛔ THE ONE bare-operand classification, named on Table 15's ROW axis and mapped to the COLUMN
    /// axis for a selection subject. A bare word that RESOLVES to a level-88 (§2023:8.8.4.2.7 GR2) or a switch-status
    /// (§2023:8.8.4.6) condition-name IS a condition, not an identifier; a boolean operand (§2023:8.8.2) is a boolean
    /// expression until SR6 says otherwise; everything else is <see cref="OperandKindOf"/>'s syntactic
    /// question.</summary>
    internal static EvaluateObjectOperand? BareOperandKind(Core.ValueOperandContext vo, in BareOperandAnalysis bare) =>
        bare.Form switch
        {
            BareOperandForm.ConditionName or BareOperandForm.SwitchStatus => EvaluateObjectOperand.Condition,
            BareOperandForm.Boolean => EvaluateObjectOperand.BooleanExpression,
            // §2023:14.9.13.3 SR5 — "a class condition without the identifier": a bare class-name / alphabet-name is a
            // partial-expression by its resolved SYMBOL, the spelling the grammar hands to valueOperand (PB843).
            BareOperandForm.ClassName => EvaluateObjectOperand.PartialExpression,
            _ => OperandKindOf(vo),
        };

    /// <summary>Classify a bare value operand as identifier / literal / arithmetic-expression, or null when the
    /// shape is not one of those with certainty — §2023:14.9.13.4 GR1's question: "If an operand of the EVALUATE
    /// statement consists of a single literal, that operand is treated as a literal, not as an expression."
    /// <c>ConditionBinder.SoleNumLiteral</c> answers the "consists of a single literal" half, sign included
    /// (§2023:8.3.3.3.2 SR2), which is what makes <c>-5</c> a literal and the SEPARATED <c>- 5</c> an arithmetic
    /// expression.</summary>
    private static EvaluateObjectOperand? OperandKindOf(Core.ValueOperandContext vo)
    {
        // NULL is the grammar's `predefinedNull` arm of nonNumericLiteral only as a parse CARRIER (kb/Work PB1427); it is an
        // IDENTIFIER (ISO §2023:8.4.3.1.2 identifier Format 8, predefined-address), so Table 15 reads it in the Identifier
        // row/column, never as a literal (kb/Work PB1682).
        if (vo.nonNumericLiteral() is { } nn)
            return nn.predefinedNull() is not null ? EvaluateObjectOperand.Identifier : EvaluateObjectOperand.Literal;     // GR1
        if (vo.arithmeticExpression() is not { } expr) return null;
        if (ConditionBinder.SoleDataRef(expr) is not null) return EvaluateObjectOperand.Identifier;
        if (ConditionBinder.SoleNumLiteral(expr) is not null) return EvaluateObjectOperand.Literal;   // GR1
        return EvaluateObjectOperand.ArithmeticExpression;
    }

    /// <summary>Bind a <c>valueOperand</c> (an arithmetic expression or a non-numeric literal) as an EVALUATE subject
    /// or object operand. §2023:14.9.13.4 GR4a.6 says the pair "is considered to be a conditional expression" of the
    /// form <c>selection-subject = selection-object</c>, so the binding IS the relation operand's, <see cref="ConditionBinder.ComparisonOperandOf"/>. This
    /// method used to be an arm-for-arm copy of that dispatch (kb/Work PB172 added its missing HEXLIT arm, PB224
    /// aligned its order by hand), and both copies mapped a parenthesized operand to the item it encloses (kb/Work
    /// PB1935); one body now answers both. NULL is admitted as on the relation side: §2023:14.9.13.3 SR7a makes a
    /// selection object "valid operands for comparison … in accordance with 8.8.4.2", a §2023:8.4.3.10.3 SR1 context.</summary>
    internal static BoundOperand BindValueOperand(StatementBinder host, Core.ValueOperandContext vo) =>
        host.Cond.ComparisonOperandOf(vo, "EVALUATE operand");
}
