// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
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

            var loc = new SourceLocation("<source>", 0, fileSym.Line, 0);
            var span = new TextSpan(0, 0);

            var statusSym = model.ResolveData(fileSym.FileStatus);
            if (statusSym == null)
            {
                diagnostics.Report(DiagnosticDescriptors.CBL3201, loc, span);
                continue;
            }

            // Must be elementary alphanumeric
            if (statusSym.IsGroup)
            {
                diagnostics.Report(DiagnosticDescriptors.CBL3203, loc, span);
                continue;
            }

            // Must be alphanumeric with length >= 2
            var type = statusSym.ResolvedType;
            if (type == null || !type.IsAlphanumeric)
            {
                diagnostics.Report(DiagnosticDescriptors.CBL3202, loc, span);
            }
            else if (type.Pic != null && type.Pic.Length < 2)
            {
                diagnostics.Report(DiagnosticDescriptors.CBL3202, loc, span);
            }

            // Cannot be REDEFINES or RENAMES
            if (statusSym.Redefines != null || statusSym.Renames != null)
            {
                diagnostics.Report(DiagnosticDescriptors.CBL3204, loc, span);
            }
        }
    }
}
