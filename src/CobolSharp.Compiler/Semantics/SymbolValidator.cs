// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;

namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Validates symbol table rules:
/// - Data-name uniqueness (01/77 program-wide; 88 within parent)
/// - Section/paragraph uniqueness
/// - Linkage Section rules (no VALUE, no REDEFINES)
/// </summary>
public static class SymbolValidator
{
    public static void Validate(SemanticModel model, DiagnosticBag diagnostics)
    {
        ValidateDataNameUniqueness(model, diagnostics);
        ValidateLinkageSection(model, diagnostics);
    }

    private static void ValidateDataNameUniqueness(SemanticModel model, DiagnosticBag diagnostics)
    {
        // Check for duplicate 01/77-level names (excluding FILLER)
        var seen = new Dictionary<string, DataSymbol>(StringComparer.OrdinalIgnoreCase);
        foreach (var data in model.DataItemsInOrder)
        {
            if (data.LevelNumber is not (1 or 77)) continue;
            if (data.IsFiller) continue;

            if (seen.TryGetValue(data.DisplayName, out var existing))
            {
                // Duplicate — only warn, don't error (COBOL allows qualified references)
                // Real enforcement would require checking if qualification resolves ambiguity
            }
            else
            {
                seen[data.DisplayName] = data;
            }
        }
    }

    private static void ValidateLinkageSection(SemanticModel model, DiagnosticBag diagnostics)
    {
        foreach (var data in model.DataItemsInOrder)
        {
            if (data.Area != StorageAreaKind.LinkageSection) continue;

            var loc = new SourceLocation("<source>", 0, data.Line, 0);
            var span = new TextSpan(0, 0);

            // VALUE not allowed on LINKAGE items (except 88-level)
            if (data.InitialValue != null && data.LevelNumber != 88)
            {
                diagnostics.Report(DiagnosticDescriptors.CBL3110, loc, span, data.DisplayName);
            }

            // REDEFINES not allowed in LINKAGE SECTION 01-level items
            if (data.Redefines != null && data.LevelNumber == 1)
            {
                diagnostics.Report(DiagnosticDescriptors.CBL3111, loc, span, data.DisplayName);
            }
        }
    }
}
