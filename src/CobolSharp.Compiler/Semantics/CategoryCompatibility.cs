// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Operation kinds for category compatibility checking.
/// </summary>
public enum OperationKind
{
    Move,
    Add,
    Subtract,
    Multiply,
    Divide,
    Compare,
}

/// <summary>
/// Central authority for COBOL category compatibility rules.
/// ISO/IEC 1989:2023 §6.1.2, §6.1.3, §6.13, §13.5, §14.9.24.
/// See docs/CATEGORY-RULES.md for full truth tables and rationale.
///
/// One place, one lattice — binder uses this for diagnostics, LoweringTable uses it
/// to select the correct PicRuntime helper.
///
/// MOVE truth table (source × target):
///                  Target →
/// Source ↓     Num   NumEd   Alpha   AlphaEd   Nat   NatEd
/// ---------------------------------------------------------
/// Numeric        T      T       T        T       T      T
/// NumericEdited  F      T       T        T       T      T
/// Alphanumeric   F      F       T        T       T      T
/// AlphaEdited    F      F       T        T       T      T
/// National       F      F       T        T       T      T
/// NatEdited      F      F       T        T       T      T
/// </summary>
public static class CategoryCompatibility
{
    // ── MOVE ──

    private static readonly HashSet<(CobolCategory Source, CobolCategory Target)> s_moveLegal = new()
    {
        // Numeric → anything
        (CobolCategory.Numeric, CobolCategory.Numeric),
        (CobolCategory.Numeric, CobolCategory.NumericEdited),
        (CobolCategory.Numeric, CobolCategory.Alphanumeric),
        (CobolCategory.Numeric, CobolCategory.AlphanumericEdited),
        (CobolCategory.Numeric, CobolCategory.National),
        (CobolCategory.Numeric, CobolCategory.NationalEdited),

        // NumericEdited → NumericEdited, alpha/national families
        (CobolCategory.NumericEdited, CobolCategory.NumericEdited),
        (CobolCategory.NumericEdited, CobolCategory.Alphanumeric),
        (CobolCategory.NumericEdited, CobolCategory.AlphanumericEdited),
        (CobolCategory.NumericEdited, CobolCategory.National),
        (CobolCategory.NumericEdited, CobolCategory.NationalEdited),

        // Alphanumeric → alpha/national families
        (CobolCategory.Alphanumeric, CobolCategory.Alphanumeric),
        (CobolCategory.Alphanumeric, CobolCategory.AlphanumericEdited),
        (CobolCategory.Alphanumeric, CobolCategory.National),
        (CobolCategory.Alphanumeric, CobolCategory.NationalEdited),

        // AlphanumericEdited → alpha/national families
        (CobolCategory.AlphanumericEdited, CobolCategory.Alphanumeric),
        (CobolCategory.AlphanumericEdited, CobolCategory.AlphanumericEdited),
        (CobolCategory.AlphanumericEdited, CobolCategory.National),
        (CobolCategory.AlphanumericEdited, CobolCategory.NationalEdited),

        // National → alpha/national families
        (CobolCategory.National, CobolCategory.National),
        (CobolCategory.National, CobolCategory.NationalEdited),
        (CobolCategory.National, CobolCategory.Alphanumeric),
        (CobolCategory.National, CobolCategory.AlphanumericEdited),

        // NationalEdited → alpha/national families
        (CobolCategory.NationalEdited, CobolCategory.National),
        (CobolCategory.NationalEdited, CobolCategory.NationalEdited),
        (CobolCategory.NationalEdited, CobolCategory.Alphanumeric),
        (CobolCategory.NationalEdited, CobolCategory.AlphanumericEdited),
    };

    public static bool IsMoveLegal(CobolCategory source, CobolCategory target)
        => s_moveLegal.Contains((source, target));

    // ── Arithmetic ──

    // ISO §6.13: Only Numeric is a legal arithmetic operand.
    // NumericEdited is a display/editing category, not numeric for arithmetic.
    private static readonly HashSet<CobolCategory> s_arithmeticOperand = new()
    {
        CobolCategory.Numeric,
    };

    private static readonly HashSet<CobolCategory> s_arithmeticResult = new()
    {
        CobolCategory.Numeric,
        CobolCategory.NumericEdited,
    };

    public static bool IsArithmeticOperand(CobolCategory category)
        => s_arithmeticOperand.Contains(category);

    public static bool IsArithmeticResult(CobolCategory category)
        => s_arithmeticResult.Contains(category);

    // ── Comparison ──

    public static bool IsComparisonLegal(CobolCategory left, CobolCategory right)
    {
        if (IsNumericFamily(left) && IsNumericFamily(right))
            return true;
        if (IsAlphanumericFamily(left) && IsAlphanumericFamily(right))
            return true;
        if (IsNationalFamily(left) && IsNationalFamily(right))
            return true;
        return false;
    }

    // ── Family helpers ──

    public static bool IsNumericFamily(CobolCategory c)
        => c is CobolCategory.Numeric or CobolCategory.NumericEdited;

    public static bool IsAlphanumericFamily(CobolCategory c)
        => c is CobolCategory.Alphanumeric or CobolCategory.AlphanumericEdited;

    public static bool IsNationalFamily(CobolCategory c)
        => c is CobolCategory.National or CobolCategory.NationalEdited;
}
