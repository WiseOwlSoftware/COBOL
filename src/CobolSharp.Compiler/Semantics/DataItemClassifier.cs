// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Validates data item consistency after type resolution and storage layout.
/// Checks OCCURS placement, BLANK WHEN ZERO, JUSTIFIED, category consistency.
/// </summary>
public static class DataItemClassifier
{
    public static void Validate(SemanticModel model, DiagnosticBag diagnostics)
    {
        foreach (var data in model.DataItemsInOrder)
        {
            var loc = new SourceLocation("<source>", 0, data.Line, 0);
            var span = TextSpan.Empty;

            ValidateOccurs(data, loc, span, diagnostics, model);
            ValidateBlankWhenZero(data, loc, span, diagnostics);
            ValidateJustified(data, loc, span, diagnostics);
            ValidateValueClause(data, loc, span, diagnostics);
        }
    }

    private static void ValidateOccurs(DataSymbol data, SourceLocation loc, TextSpan span,
        DiagnosticBag diagnostics, SemanticModel model)
    {
        if (data.Occurs == null) return;

        // OCCURS not allowed on level 01, 77, or 66 (RENAMES)
        if (data.LevelNumber is 1 or 77)
        {
            diagnostics.Report(DiagnosticDescriptors.CBL0801, loc, span, data.DisplayName);
            return;
        }

        // Check 5: OCCURS not allowed on level-66 (RENAMES)
        if (data.LevelNumber == 66)
        {
            diagnostics.Report(DiagnosticDescriptors.CBL0805, loc, span, data.DisplayName);
            return;
        }

        // DEPENDING ON must be integer numeric
        if (data.Occurs.DependingOnName != null)
        {
            var depSym = model.ResolveData(data.Occurs.DependingOnName);
            if (depSym != null)
            {
                data.Occurs.DependingOnSymbol = depSym;
                if (depSym.ResolvedType?.IsNumeric != true ||
                    (depSym.ResolvedType.Pic?.FractionDigits ?? 0) > 0)
                {
                    diagnostics.Report(DiagnosticDescriptors.CBL1101, loc, span,
                        data.Occurs.DependingOnName);
                }
            }
        }

        // ASCENDING/DESCENDING KEY validation
        ValidateOccursKeys(data, data.Occurs.AscendingKeys, loc, span, diagnostics, model);
        ValidateOccursKeys(data, data.Occurs.DescendingKeys, loc, span, diagnostics, model);
    }

    private static void ValidateOccursKeys(DataSymbol table, IReadOnlyList<string> keyNames,
        SourceLocation loc, TextSpan span, DiagnosticBag diagnostics, SemanticModel model)
    {
        foreach (var keyName in keyNames)
        {
            var keySym = model.ResolveData(keyName);
            if (keySym == null) continue;

            // Key must be subordinate to table
            if (!IsSubordinateTo(keySym, table))
            {
                diagnostics.Report(DiagnosticDescriptors.CBL1103, loc, span,
                    keyName, table.DisplayName);
            }

            // COBOL-85 allows group items as OCCURS keys (comparison uses
            // the group's alphanumeric representation). CBL1104 reserved for
            // future use if a stricter mode is needed.
        }
    }

    private static bool IsSubordinateTo(DataSymbol child, DataSymbol ancestor)
    {
        // The key is the table item itself — valid (self-referencing key)
        if (child == ancestor) return true;
        for (var cur = child.Parent; cur != null; cur = cur.Parent)
        {
            if (cur == ancestor) return true;
        }
        return false;
    }

    /// <summary>
    /// Returns true if the item has a parent with an OCCURS clause.
    /// Items subordinate to OCCURS should not have VALUE clauses (ISO §6.13.2).
    /// </summary>
    private static bool IsSubordinateToOccurs(DataSymbol data)
    {
        for (var cur = data.Parent; cur != null; cur = cur.Parent)
        {
            if (cur.Occurs != null) return true;
        }
        return false;
    }

    private static void ValidateBlankWhenZero(DataSymbol data, SourceLocation loc, TextSpan span,
        DiagnosticBag diagnostics)
    {
        if (data.ResolvedType?.Pic?.BlankWhenZero != true) return;

        // BLANK WHEN ZERO only on numeric DISPLAY
        bool isNumericDisplay = data.ResolvedType.IsNumeric && data.Usage == UsageKind.Display;
        // Also allowed on numeric-edited DISPLAY
        bool isNumericEditedDisplay = data.ResolvedType.Category == CobolCategory.NumericEdited
            && data.Usage == UsageKind.Display;

        if (!isNumericDisplay && !isNumericEditedDisplay)
        {
            diagnostics.Report(DiagnosticDescriptors.CBL0802, loc, span, data.DisplayName);
        }

        // Check 4: BLANK WHEN ZERO with JUSTIFIED is not allowed (ISO §13.18.5)
        if (data.IsJustifiedRight)
        {
            diagnostics.Report(DiagnosticDescriptors.CBL0804, loc, span, data.DisplayName);
        }
    }

    private static void ValidateJustified(DataSymbol data, SourceLocation loc, TextSpan span,
        DiagnosticBag diagnostics)
    {
        if (!data.IsJustifiedRight) return;

        // JUSTIFIED only on alphanumeric elementary items
        if (!data.IsElementary || data.ResolvedType?.IsAlphanumeric != true)
        {
            diagnostics.Report(DiagnosticDescriptors.CBL0803, loc, span, data.DisplayName);
        }
    }

    private static void ValidateValueClause(DataSymbol data, SourceLocation loc, TextSpan span,
        DiagnosticBag diagnostics)
    {
        if (data.InitialValue == null && data.FigurativeInit == null) return;
        if (data.LevelNumber == 88) return; // Condition values validated separately

        // Check 6: VALUE on REDEFINES items (except level-88)
        if (data.Redefines != null)
        {
            diagnostics.Report(DiagnosticDescriptors.CBL0806, loc, span, data.DisplayName);
        }

        // Check 7: VALUE on items subordinate to OCCURS
        if (IsSubordinateToOccurs(data))
        {
            diagnostics.Report(DiagnosticDescriptors.CBL0807, loc, span, data.DisplayName);
        }

        // VALUE on group item → warning (ISO allows it but it's an extension)
        if (data.IsGroup)
        {
            diagnostics.Report(DiagnosticDescriptors.CBL1001, loc, span, data.DisplayName);
            return;
        }

        // VALUE type vs data category compatibility
        if (data.InitialValue != null && data.ResolvedType != null)
        {
            bool isNumericValue = decimal.TryParse(data.InitialValue,
                System.Globalization.NumberStyles.AllowLeadingSign | System.Globalization.NumberStyles.AllowDecimalPoint,
                System.Globalization.CultureInfo.InvariantCulture, out _);
            bool isNumericItem = data.ResolvedType.IsNumeric;

            // String value on numeric item (not figurative zero)
            if (!isNumericValue && isNumericItem && data.FigurativeInit == null)
            {
                diagnostics.Report(DiagnosticDescriptors.CBL1002, loc, span, data.DisplayName);
            }
        }
    }
}
