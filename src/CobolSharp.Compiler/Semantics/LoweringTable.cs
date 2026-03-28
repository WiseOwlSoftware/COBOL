// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Frozen;
using System.Reflection;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Central dispatch table: (OperationKind, CobolCategory source, CobolCategory target) → PicRuntime helper.
/// See docs/CATEGORY-RULES.md for the full compatibility matrices.
/// Both the binder (for validation) and the emitter (for lowering) consult this single table.
/// If ResolveHelper returns null, the combination is illegal and the binder should emit a diagnostic.
/// </summary>
public static class LoweringTable
{
    public static MethodInfo? ResolveHelper(OperationKind op,
        CobolCategory source, CobolCategory target)
    {
        return op switch
        {
            OperationKind.Move => ResolveMoveHelper(source, target),
            OperationKind.Add => ResolveArithmeticHelper(nameof(PicRuntime.AddNumeric), source, target),
            OperationKind.Subtract => ResolveArithmeticHelper(nameof(PicRuntime.SubtractNumeric), source, target),
            OperationKind.Multiply => ResolveArithmeticHelper(nameof(PicRuntime.MultiplyNumeric), source, target),
            OperationKind.Divide => ResolveArithmeticHelper(nameof(PicRuntime.DivideNumeric), source, target),
            OperationKind.Compare => ResolveCompareHelper(source, target),
            _ => null
        };
    }

    private static MethodInfo? ResolveMoveHelper(CobolCategory source, CobolCategory target)
    {
        if (!CategoryCompatibility.IsMoveLegal(source, target))
            return null;

        return (source, target) switch
        {
            (CobolCategory.Numeric, CobolCategory.Numeric) =>
                Get(nameof(PicRuntime.MoveNumericToNumeric)),
            (CobolCategory.Numeric, CobolCategory.NumericEdited) =>
                Get(nameof(PicRuntime.MoveNumericToNumericEdited)),
            (CobolCategory.Numeric, CobolCategory.Alphanumeric) =>
                Get(nameof(PicRuntime.MoveNumericToAlphanumeric)),
            (CobolCategory.Numeric, CobolCategory.AlphanumericEdited) =>
                Get(nameof(PicRuntime.MoveNumericToAlphanumericEdited)),
            (CobolCategory.Numeric, CobolCategory.National) =>
                Get(nameof(PicRuntime.MoveNumericToNational)),
            (CobolCategory.Numeric, CobolCategory.NationalEdited) =>
                Get(nameof(PicRuntime.MoveNumericToNationalEdited)),

            (CobolCategory.NumericEdited, CobolCategory.Numeric) =>
                Get(nameof(PicRuntime.MoveNumericEditedToNumeric)),
            (CobolCategory.NumericEdited, CobolCategory.NumericEdited) =>
                Get(nameof(PicRuntime.MoveNumericEditedToNumericEdited)),
            (CobolCategory.NumericEdited, CobolCategory.Alphanumeric) =>
                Get(nameof(PicRuntime.MoveNumericEditedToAlphanumeric)),
            (CobolCategory.NumericEdited, CobolCategory.AlphanumericEdited) =>
                Get(nameof(PicRuntime.MoveNumericEditedToAlphanumericEdited)),
            (CobolCategory.NumericEdited, CobolCategory.National) =>
                Get(nameof(PicRuntime.MoveNumericEditedToNational)),
            (CobolCategory.NumericEdited, CobolCategory.NationalEdited) =>
                Get(nameof(PicRuntime.MoveNumericEditedToNationalEdited)),

            // Alphabetic → treated as Alphanumeric for runtime dispatch
            (CobolCategory.Alphabetic, CobolCategory.Alphabetic) =>
                Get(nameof(PicRuntime.MoveAlphanumericToAlphanumeric)),
            (CobolCategory.Alphabetic, CobolCategory.Alphanumeric) =>
                Get(nameof(PicRuntime.MoveAlphanumericToAlphanumeric)),
            (CobolCategory.Alphabetic, CobolCategory.AlphanumericEdited) =>
                Get(nameof(PicRuntime.MoveAlphanumericToAlphanumericEdited)),

            (CobolCategory.Alphanumeric, CobolCategory.Alphabetic) =>
                Get(nameof(PicRuntime.MoveAlphanumericToAlphanumeric)),
            (CobolCategory.Alphanumeric, CobolCategory.Numeric) =>
                Get(nameof(PicRuntime.MoveAlphanumericToNumeric)),
            (CobolCategory.Alphanumeric, CobolCategory.NumericEdited) =>
                Get(nameof(PicRuntime.MoveAlphanumericToNumericEdited)),
            (CobolCategory.Alphanumeric, CobolCategory.Alphanumeric) =>
                Get(nameof(PicRuntime.MoveAlphanumericToAlphanumeric)),
            (CobolCategory.Alphanumeric, CobolCategory.AlphanumericEdited) =>
                Get(nameof(PicRuntime.MoveAlphanumericToAlphanumericEdited)),
            (CobolCategory.Alphanumeric, CobolCategory.National) =>
                Get(nameof(PicRuntime.MoveAlphanumericToNational)),
            (CobolCategory.Alphanumeric, CobolCategory.NationalEdited) =>
                Get(nameof(PicRuntime.MoveAlphanumericToNationalEdited)),

            (CobolCategory.AlphanumericEdited, CobolCategory.Alphabetic) =>
                Get(nameof(PicRuntime.MoveAlphanumericEditedToAlphanumeric)),
            (CobolCategory.AlphanumericEdited, CobolCategory.Alphanumeric) =>
                Get(nameof(PicRuntime.MoveAlphanumericEditedToAlphanumeric)),
            (CobolCategory.AlphanumericEdited, CobolCategory.AlphanumericEdited) =>
                Get(nameof(PicRuntime.MoveAlphanumericEditedToAlphanumericEdited)),
            (CobolCategory.AlphanumericEdited, CobolCategory.National) =>
                Get(nameof(PicRuntime.MoveAlphanumericEditedToNational)),
            (CobolCategory.AlphanumericEdited, CobolCategory.NationalEdited) =>
                Get(nameof(PicRuntime.MoveAlphanumericEditedToAlphanumericEdited)),

            (CobolCategory.National, CobolCategory.National) =>
                Get(nameof(PicRuntime.MoveNationalToNational)),
            (CobolCategory.National, CobolCategory.NationalEdited) =>
                Get(nameof(PicRuntime.MoveNationalToNationalEdited)),
            (CobolCategory.National, CobolCategory.Alphanumeric) =>
                Get(nameof(PicRuntime.MoveNationalToAlphanumeric)),
            (CobolCategory.National, CobolCategory.AlphanumericEdited) =>
                Get(nameof(PicRuntime.MoveNationalToAlphanumericEdited)),

            (CobolCategory.NationalEdited, CobolCategory.National) =>
                Get(nameof(PicRuntime.MoveNationalEditedToNational)),
            (CobolCategory.NationalEdited, CobolCategory.NationalEdited) =>
                Get(nameof(PicRuntime.MoveNationalEditedToNationalEdited)),
            (CobolCategory.NationalEdited, CobolCategory.Alphanumeric) =>
                Get(nameof(PicRuntime.MoveNationalEditedToAlphanumeric)),
            (CobolCategory.NationalEdited, CobolCategory.AlphanumericEdited) =>
                Get(nameof(PicRuntime.MoveNationalEditedToAlphanumericEdited)),

            _ => null
        };
    }

    private static MethodInfo? ResolveArithmeticHelper(string methodName,
        CobolCategory source, CobolCategory target)
    {
        if (!CategoryCompatibility.IsArithmeticOperand(source) ||
            !CategoryCompatibility.IsArithmeticResult(target))
            return null;

        return Get(methodName);
    }

    private static MethodInfo? ResolveCompareHelper(CobolCategory left, CobolCategory right)
    {
        if (!CategoryCompatibility.IsComparisonLegal(left, right))
            return null;

        if (CategoryCompatibility.IsNumericFamily(left))
            return Get(nameof(PicRuntime.CompareNumeric));

        if (CategoryCompatibility.IsAlphanumericFamily(left))
            return Get(nameof(PicRuntime.CompareAlphanumeric));

        if (CategoryCompatibility.IsNationalFamily(left))
            return Get(nameof(PicRuntime.CompareNational));

        return null;
    }

    private static readonly FrozenDictionary<string, MethodInfo> MethodCache =
        typeof(PicRuntime)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .ToFrozenDictionary(m => m.Name);

    private static MethodInfo Get(string name) => MethodCache[name];
}
