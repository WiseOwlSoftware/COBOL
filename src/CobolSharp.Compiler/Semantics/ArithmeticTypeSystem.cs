// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Numeric category classification for arithmetic validation.
/// </summary>
public enum NumericCategory
{
    PureNumeric,
    NumericEdited,
    NonNumeric,
}

/// <summary>
/// Arithmetic type system: validates operands, results, ROUNDED, SIZE ERROR, REMAINDER.
/// Used by BoundTreeBuilder for semantic enforcement of arithmetic statements.
/// </summary>
public static class ArithmeticTypeSystem
{
    public static NumericCategory Classify(CobolCategory category) => category switch
    {
        CobolCategory.Numeric => NumericCategory.PureNumeric,
        CobolCategory.NumericEdited => NumericCategory.NumericEdited,
        _ => NumericCategory.NonNumeric,
    };

    public static bool IsValidArithmeticOperand(CobolCategory category)
        => category == CobolCategory.Numeric;

    public static bool IsValidArithmeticResult(CobolCategory category)
        => category is CobolCategory.Numeric or CobolCategory.NumericEdited;

    public static bool IsValidRoundedTarget(CobolCategory category)
        => category is CobolCategory.Numeric or CobolCategory.NumericEdited;

    public static bool IsValidSizeErrorContext(CobolCategory category)
        => category is CobolCategory.Numeric or CobolCategory.NumericEdited;

    public static bool IsValidRemainderTarget(ITypeSymbol? type)
    {
        if (type == null) return false;
        // COBOL-85 §6.4.5: REMAINDER must be numeric or numeric-edited
        return type.IsNumeric || type.Category == Runtime.CobolCategory.NumericEdited;
    }

    /// <summary>
    /// Validate all operands and targets of an arithmetic statement.
    /// </summary>
    public static void ValidateArithmeticStatement(
        Bound.BoundArithmeticStatement stmt,
        DiagnosticBag diagnostics,
        SourceLocation loc, TextSpan span)
    {
        // Validate source operands are numeric
        foreach (var operand in stmt.Operands)
        {
            if (operand is Bound.BoundIdentifierExpression id &&
                !IsValidArithmeticOperand(id.Category))
            {
                diagnostics.Report(DiagnosticDescriptors.CBL2601, loc, span);
            }
        }

        // Validate receiver is numeric (if identifier)
        if (stmt.Receiver is Bound.BoundIdentifierExpression recvId &&
            !IsValidArithmeticOperand(recvId.Category))
        {
            diagnostics.Report(DiagnosticDescriptors.CBL2601, loc, span);
        }

        // Validate targets are numeric/numeric-edited
        foreach (var target in stmt.Targets)
        {
            var tgtCat = target.Target.Category;
            if (!IsValidArithmeticResult(tgtCat))
            {
                diagnostics.Report(DiagnosticDescriptors.CBL2602, loc, span,
                    target.Target.Symbol.DisplayName);
            }

            // ROUNDED target must be numeric/numeric-edited
            if (target.IsRounded && !IsValidRoundedTarget(tgtCat))
            {
                diagnostics.Report(DiagnosticDescriptors.CBL2603, loc, span,
                    target.Target.Symbol.DisplayName);
            }
        }

        // DIVIDE REMAINDER must be integer numeric
        if (stmt.RemainderTarget != null)
        {
            var remSym = stmt.RemainderTarget.Symbol;
            if (!IsValidRemainderTarget(remSym.ResolvedType))
            {
                diagnostics.Report(DiagnosticDescriptors.CBL2605, loc, span,
                    remSym.DisplayName);
            }
        }
    }
}
