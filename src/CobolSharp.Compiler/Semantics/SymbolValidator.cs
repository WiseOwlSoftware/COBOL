// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;

namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Validates symbol table rules after the scope tree is fully populated:
/// - Duplicate detection via scope rejection tracking (all scopes)
/// - REDEFINES level/target constraints
/// - Linkage Section rules (no VALUE, no REDEFINES on 01)
/// Marks invalid symbols via <see cref="Symbol.IsValid"/> = false.
/// Does not modify scopes or resolution logic.
/// </summary>
public static class SymbolValidator
{
    public static void Validate(SemanticModel model, DiagnosticBag diagnostics)
    {
        ValidateScopeRejections(model, diagnostics);
        ValidateRedefines(model, diagnostics);
        ValidateLinkageSection(model, diagnostics);
        ValidateProcedureUsingReturning(model, diagnostics);
        ValidateExternalGlobal(model, diagnostics);
    }

    // ═══════════════════════════════════════════════════════════════
    // Pass 1: Scope rejection processing (all scopes)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Walk the rejection lists on every scope in the program. Each rejection
    /// represents a TryDeclare that failed because the name was already taken.
    /// Emit the appropriate CBL31xx diagnostic and mark the rejected symbol invalid.
    /// </summary>
    private static void ValidateScopeRejections(SemanticModel model, DiagnosticBag diagnostics)
    {
        var program = model.Program;

        ProcessRejections(program.GlobalScope, diagnostics);
        ProcessRejections(program.DataDivisionScope, diagnostics);
        ProcessRejections(program.ProcedureDivisionScope, diagnostics);

        // Section-level scopes (paragraph duplicates within a section)
        foreach (var section in model.SectionsInOrder)
            ProcessRejections(section.Scope, diagnostics);
    }

    private static void ProcessRejections(Scope scope, DiagnosticBag diagnostics)
    {
        foreach (var (rejected, existing) in scope.Rejections)
        {
            // Skip rejections that are legitimate in COBOL:
            // Subordinate data items (level 02-49) may share names across different
            // group items — COBOL qualified references disambiguate them.
            // Only top-level (01/77) and index names are true duplicates.
            if (rejected is DataSymbol rejData && existing is DataSymbol exData
                && rejected.Kind == SymbolKind.Data)
            {
                if (rejData.LevelNumber is not (1 or 77) || exData.LevelNumber is not (1 or 77))
                    continue;
            }

            // Condition names (88-level) under different parents are legal
            // (qualified by parent name). Only report if same parent.
            if (rejected is ConditionSymbol rejCond && existing is ConditionSymbol exCond)
            {
                if (!ReferenceEquals(rejCond.ParentDataItem, exCond.ParentDataItem))
                    continue;
            }

            // In ProcedureDivisionScope, paragraph names CAN be duplicated across sections
            // (qualified by section name). Skip rejections where the rejected paragraph
            // was already successfully declared in its section scope.
            if (scope.Kind == ScopeKind.ProcedureDivision
                && rejected.Kind == SymbolKind.Paragraph && existing.Kind == SymbolKind.Paragraph
                && rejected.DeclaringScope != null)
                continue;

            // Cross-type collisions between DataSymbol and ConditionSymbol in the
            // flat DataDivisionScope are legal — COBOL resolves by context.
            if (scope.Kind == ScopeKind.DataDivision)
            {
                bool isDataVsCond =
                    (rejected.Kind == SymbolKind.Data && existing.Kind == SymbolKind.Condition88) ||
                    (rejected.Kind == SymbolKind.Condition88 && existing.Kind == SymbolKind.Data);
                if (isDataVsCond)
                    continue;
            }

            rejected.IsValid = false;

            var loc = new SourceLocation("<source>", 0, rejected.Line, 0);
            var span = TextSpan.Empty;

            // Choose diagnostic based on symbol kinds
            if (rejected.Kind == existing.Kind)
            {
                switch (rejected.Kind)
                {
                    case SymbolKind.Data:
                    case SymbolKind.Index:
                        diagnostics.Report(DiagnosticDescriptors.CBL3101, loc, span,
                            rejected.Name);
                        break;

                    case SymbolKind.Condition88:
                        var parentName = (rejected as ConditionSymbol)?.ParentDataItem.DisplayName ?? "?";
                        diagnostics.Report(DiagnosticDescriptors.CBL3102, loc, span,
                            rejected.Name, parentName);
                        break;

                    case SymbolKind.Section:
                        diagnostics.Report(DiagnosticDescriptors.CBL3103, loc, span,
                            rejected.Name);
                        break;

                    case SymbolKind.Paragraph:
                        diagnostics.Report(DiagnosticDescriptors.CBL3104, loc, span,
                            rejected.Name);
                        break;

                    case SymbolKind.File:
                        diagnostics.Report(DiagnosticDescriptors.CBL3107, loc, span,
                            rejected.Name, "file " + existing.Name);
                        break;

                    default:
                        diagnostics.Report(DiagnosticDescriptors.CBL3107, loc, span,
                            rejected.Name, existing.Kind.ToString());
                        break;
                }
            }
            else
            {
                // Cross-type collision (e.g., paragraph name = section name)
                diagnostics.Report(DiagnosticDescriptors.CBL3107, loc, span,
                    rejected.Name, existing.Kind.ToString());
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Pass 2: REDEFINES validation
    // ═══════════════════════════════════════════════════════════════

    private static void ValidateRedefines(SemanticModel model, DiagnosticBag diagnostics)
    {
        foreach (var data in model.DataItemsInOrder)
        {
            if (data.Redefines == null) continue;

            var loc = new SourceLocation("<source>", 0, data.Line, 0);
            var span = TextSpan.Empty;
            var target = data.Redefines;

            // Level numbers must match
            if (data.LevelNumber != target.LevelNumber)
            {
                diagnostics.Report(DiagnosticDescriptors.CBL3112, loc, span,
                    data.DisplayName, data.LevelNumber,
                    target.DisplayName, target.LevelNumber);
            }

            // Cannot REDEFINES special-level items (66 or 88)
            if (target.LevelNumber is 66 or 88)
            {
                diagnostics.Report(DiagnosticDescriptors.CBL3113, loc, span,
                    target.DisplayName, target.LevelNumber);
            }

            // CBL3114: REDEFINES target must not itself have an OCCURS clause
            if (target.Occurs is { MaxOccurs: > 1 })
            {
                diagnostics.Report(DiagnosticDescriptors.CBL3114, loc, span,
                    target.DisplayName, target.DisplayName);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Pass 3: Linkage Section rules (existing)
    // ═══════════════════════════════════════════════════════════════

    private static void ValidateLinkageSection(SemanticModel model, DiagnosticBag diagnostics)
    {
        foreach (var data in model.DataItemsInOrder)
        {
            if (data.Area != StorageAreaKind.LinkageSection) continue;

            var loc = new SourceLocation("<source>", 0, data.Line, 0);
            var span = TextSpan.Empty;

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

    // ═══════════════════════════════════════════════════════════════
    // Pass 4: PROCEDURE DIVISION USING/RETURNING validation
    // ═══════════════════════════════════════════════════════════════

    private static void ValidateProcedureUsingReturning(SemanticModel model, DiagnosticBag diagnostics)
    {
        // CBL3108: USING parameters must be in LINKAGE SECTION
        foreach (var param in model.ProcedureUsingParameters)
        {
            if (param.Area != StorageAreaKind.LinkageSection)
            {
                diagnostics.Report(DiagnosticDescriptors.CBL3108,
                    new SourceLocation("<source>", 0, param.Line, 0), TextSpan.Empty,
                    param.DisplayName);
            }
        }

        // CBL3109: RETURNING item must be in LINKAGE SECTION
        if (model.ProcedureReturningItem is { } ret
            && ret.Area != StorageAreaKind.LinkageSection)
        {
            diagnostics.Report(DiagnosticDescriptors.CBL3109,
                new SourceLocation("<source>", 0, ret.Line, 0), TextSpan.Empty,
                ret.DisplayName);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Pass 5: EXTERNAL / GLOBAL clause validation (§13.18.22, §13.18.27)
    // ═══════════════════════════════════════════════════════════════

    private static void ValidateExternalGlobal(SemanticModel model, DiagnosticBag diagnostics)
    {
        foreach (var data in model.DataItemsInOrder)
        {
            var loc = new SourceLocation("<source>", 0, data.Line, 0);
            var span = TextSpan.Empty;

            if (data.IsExternal)
            {
                // §13.18.22: EXTERNAL only on level-01 in WORKING-STORAGE
                if (data.LevelNumber != 1 || data.Area != StorageAreaKind.WorkingStorage)
                {
                    diagnostics.Report(DiagnosticDescriptors.CBL3115, loc, span,
                        data.DisplayName);
                }

                // §13.18.22 rule 5: EXTERNAL shall not be combined with REDEFINES
                if (data.Redefines != null || data.RedefinesName != null)
                {
                    diagnostics.Report(DiagnosticDescriptors.CBL3117, loc, span,
                        data.DisplayName);
                }

                // Runtime warning: shared storage not yet implemented
                diagnostics.Report(DiagnosticDescriptors.CBL3118, loc, span,
                    data.DisplayName);
            }

            if (data.IsGlobal)
            {
                // §13.18.27: GLOBAL only on level-01 items
                if (data.LevelNumber != 1)
                {
                    diagnostics.Report(DiagnosticDescriptors.CBL3116, loc, span,
                        data.DisplayName);
                }

                // Runtime warning: nested program visibility not yet implemented
                diagnostics.Report(DiagnosticDescriptors.CBL3119, loc, span,
                    data.DisplayName);
            }
        }
    }
}
