// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Frozen;
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
/// ISO/IEC 1989:2023 6.1.2, 6.1.3, 6.13, 13.5, 14.9.24.
/// </summary>
public static class CategoryCompatibility
{
    private static readonly FrozenSet<(CobolCategory Source, CobolCategory Target)> MoveLegalPairs =
        new (CobolCategory, CobolCategory)[]
        {
            // Numeric -> anything
            (CobolCategory.Numeric, CobolCategory.Numeric),
            (CobolCategory.Numeric, CobolCategory.NumericEdited),
            (CobolCategory.Numeric, CobolCategory.Alphanumeric),
            (CobolCategory.Numeric, CobolCategory.AlphanumericEdited),
            (CobolCategory.Numeric, CobolCategory.National),
            (CobolCategory.Numeric, CobolCategory.NationalEdited),
            // NumericEdited -> Numeric, NumericEdited, alpha/national families
            (CobolCategory.NumericEdited, CobolCategory.Numeric),
            (CobolCategory.NumericEdited, CobolCategory.NumericEdited),
            (CobolCategory.NumericEdited, CobolCategory.Alphanumeric),
            (CobolCategory.NumericEdited, CobolCategory.AlphanumericEdited),
            (CobolCategory.NumericEdited, CobolCategory.National),
            (CobolCategory.NumericEdited, CobolCategory.NationalEdited),
            // Alphanumeric -> alpha/national families
            (CobolCategory.Alphanumeric, CobolCategory.Alphanumeric),
            (CobolCategory.Alphanumeric, CobolCategory.AlphanumericEdited),
            (CobolCategory.Alphanumeric, CobolCategory.National),
            (CobolCategory.Alphanumeric, CobolCategory.NationalEdited),
            // AlphanumericEdited -> alpha/national families
            (CobolCategory.AlphanumericEdited, CobolCategory.Alphanumeric),
            (CobolCategory.AlphanumericEdited, CobolCategory.AlphanumericEdited),
            (CobolCategory.AlphanumericEdited, CobolCategory.National),
            (CobolCategory.AlphanumericEdited, CobolCategory.NationalEdited),
            // National -> alpha/national families
            (CobolCategory.National, CobolCategory.National),
            (CobolCategory.National, CobolCategory.NationalEdited),
            (CobolCategory.National, CobolCategory.Alphanumeric),
            (CobolCategory.National, CobolCategory.AlphanumericEdited),
            // NationalEdited -> alpha/national families
            (CobolCategory.NationalEdited, CobolCategory.National),
            (CobolCategory.NationalEdited, CobolCategory.NationalEdited),
            (CobolCategory.NationalEdited, CobolCategory.Alphanumeric),
            (CobolCategory.NationalEdited, CobolCategory.AlphanumericEdited),
        }.ToFrozenSet();

    public static bool IsMoveLegal(CobolCategory source, CobolCategory target)
        => MoveLegalPairs.Contains((source, target));

    // ISO 6.13: Only Numeric is a legal arithmetic operand.
    public static bool IsArithmeticOperand(CobolCategory category)
        => category == CobolCategory.Numeric;

    public static bool IsArithmeticResult(CobolCategory category)
        => category is CobolCategory.Numeric or CobolCategory.NumericEdited;

    public static bool IsComparisonLegal(CobolCategory left, CobolCategory right)
    {
        if (IsNumericFamily(left) && IsNumericFamily(right)) return true;
        if (IsAlphanumericFamily(left) && IsAlphanumericFamily(right)) return true;
        if (IsNationalFamily(left) && IsNationalFamily(right)) return true;
        return false;
    }

    public static bool IsNumericFamily(CobolCategory c)
        => c is CobolCategory.Numeric or CobolCategory.NumericEdited;

    public static bool IsAlphanumericFamily(CobolCategory c)
        => c is CobolCategory.Alphanumeric or CobolCategory.AlphanumericEdited;

    public static bool IsNationalFamily(CobolCategory c)
        => c is CobolCategory.National or CobolCategory.NationalEdited;
}
