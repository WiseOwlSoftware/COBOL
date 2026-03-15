// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Semantics;

namespace CobolSharp.Compiler.FlowAnalysis;

/// <summary>
/// Validates PERFORM ... THRU ... ranges: start must come before end
/// in declaration order.
/// </summary>
public sealed class PerformRangeChecker
{
    private readonly SymbolTable _symbols;
    private readonly DiagnosticBag _diagnostics;

    public PerformRangeChecker(SymbolTable symbols, DiagnosticBag diagnostics)
    {
        _symbols = symbols;
        _diagnostics = diagnostics;
    }

    /// <summary>
    /// Check that a PERFORM range (startName THRU endName) is valid:
    /// both names must resolve to paragraphs/sections, and start must
    /// come before end in declaration order.
    /// </summary>
    public void CheckRange(string startName, string? endName, int line)
    {
        if (endName == null)
            return;

        var start = _symbols.Program.ProcedureDivisionScope.Resolve<ParagraphSymbol>(startName);
        var end = _symbols.Program.ProcedureDivisionScope.Resolve<ParagraphSymbol>(endName);

        if (start == null)
        {
            _diagnostics.ReportError(
                "FLOW",
                $"PERFORM target '{startName}' is not a paragraph or section.",
                new Common.SourceLocation("<source>", 0, line, 0),
                new Common.TextSpan(0, 0));
            return;
        }

        if (end == null)
        {
            _diagnostics.ReportError(
                "FLOW",
                $"PERFORM THRU target '{endName}' is not a paragraph or section.",
                new Common.SourceLocation("<source>", 0, line, 0),
                new Common.TextSpan(0, 0));
            return;
        }

        if (start.Line > end.Line)
        {
            _diagnostics.ReportWarning(
                "FLOW",
                $"PERFORM range '{startName} THRU {endName}' may be invalid: " +
                $"start (line {start.Line}) comes after end (line {end.Line}).",
                new Common.SourceLocation("<source>", 0, line, 0),
                new Common.TextSpan(0, 0));
        }
    }
}
