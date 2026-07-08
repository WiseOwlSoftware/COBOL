// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Validates FILE STATUS declarations: data-name exists, is alphanumeric length >= 2,
/// is not group, is not REDEFINES/RENAMES.
/// </summary>
public static class FileStatusValidator
{
    public static void Validate(SemanticModel model, DiagnosticBag diagnostics)
    {
        foreach (var fileSym in model.Symbols.Program.GlobalScope.GetAllSymbols<FileSymbol>())
        {
            if (fileSym.FileStatus == null) continue;

            var loc = new SourceLocation(model.SourceName, 0, fileSym.Line, 0);
            var span = TextSpan.Empty;

            var statusSym = model.ResolveData(fileSym.FileStatus);
            if (statusSym == null)
            {
                diagnostics.Report(DiagnosticDescriptors.CBL3201, loc, span);
                continue;
            }

            // ISO §12.4.5.8.3: the FILE STATUS item shall be a two-character data item of category
            // alphanumeric, and (rule 3) shall not be a variable-length group. A *fixed-length*
            // group of two alphanumeric characters qualifies — a group item is category alphanumeric
            // — so only variable-length groups are rejected (e.g. the CCVS suite routinely declares
            // FILE STATUS as a 2-byte group of two PIC X items).
            if (statusSym.IsGroup)
            {
                if (HasVariableLength(statusSym))
                    diagnostics.Report(DiagnosticDescriptors.CBL3203, loc, span);
                else if (statusSym.ElementSize < 2)
                    diagnostics.Report(DiagnosticDescriptors.CBL3202, loc, span);
            }
            else
            {
                // Elementary: must be alphanumeric with length >= 2.
                var type = statusSym.ResolvedType;
                if (type == null || !type.IsAlphanumeric)
                    diagnostics.Report(DiagnosticDescriptors.CBL3202, loc, span);
                else if (type.Pic != null && type.Pic.Length < 2)
                    diagnostics.Report(DiagnosticDescriptors.CBL3202, loc, span);
            }

            // Cannot be REDEFINES or RENAMES
            if (statusSym.Redefines != null || statusSym.Renames != null)
            {
                diagnostics.Report(DiagnosticDescriptors.CBL3204, loc, span);
            }
        }
    }

    /// <summary>True if the item or any subordinate is described with OCCURS … DEPENDING ON, making
    /// it a variable-length group (disallowed as a FILE STATUS item, ISO §12.4.5.8.3 rule 3).</summary>
    private static bool HasVariableLength(DataSymbol item)
    {
        if (item.Occurs?.DependingOnName != null)
            return true;
        foreach (var child in item.Children)
            if (HasVariableLength(child))
                return true;
        return false;
    }
}
