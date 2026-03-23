// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Semantics;

namespace CobolSharp.Compiler.FlowAnalysis;

/// <summary>
/// Validates PERFORM ... THRU ... ranges: start must come before end
/// in declaration order.
/// </summary>
public sealed class PerformRangeChecker(SymbolTable symbols, DiagnosticBag diagnostics)
{
    public void CheckRange(string startName, string? endName, int line)
    {
        if (endName == null)
            return;

        var start = symbols.Program.ProcedureDivisionScope.Resolve<ParagraphSymbol>(startName);
        var end = symbols.Program.ProcedureDivisionScope.Resolve<ParagraphSymbol>(endName);

        if (start == null)
        {
            diagnostics.ReportError(
                "FLOW",
                $"PERFORM target '{startName}' is not a paragraph or section.",
                new SourceLocation("<source>", 0, line, 0),
                TextSpan.Empty);
            return;
        }

        if (end == null)
        {
            diagnostics.ReportError(
                "FLOW",
                $"PERFORM THRU target '{endName}' is not a paragraph or section.",
                new SourceLocation("<source>", 0, line, 0),
                TextSpan.Empty);
            return;
        }

        if (start.Line > end.Line)
        {
            diagnostics.ReportWarning(
                "FLOW",
                $"PERFORM range '{startName} THRU {endName}' may be invalid: " +
                $"start (line {start.Line}) comes after end (line {end.Line}).",
                new SourceLocation("<source>", 0, line, 0),
                TextSpan.Empty);
        }
    }
}
