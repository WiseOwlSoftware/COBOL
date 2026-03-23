// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Semantics;

namespace CobolSharp.Compiler.FlowAnalysis;

/// <summary>
/// Checks which paragraphs are reachable from the entry point.
/// Unreachable paragraphs are flagged as warnings.
/// </summary>
public sealed class ParagraphReachabilityAnalyzer(
    IReadOnlyList<ParagraphSymbol> paragraphs,
    Dictionary<ParagraphSymbol, List<ParagraphSymbol>> edges,
    DiagnosticBag diagnostics)
{
    public void Analyze(ParagraphSymbol entry)
    {
        var visited = new HashSet<ParagraphSymbol>();
        var stack = new Stack<ParagraphSymbol>();
        stack.Push(entry);

        while (stack.Count > 0)
        {
            var p = stack.Pop();
            if (!visited.Add(p))
                continue;

            if (edges.TryGetValue(p, out var succs))
            {
                foreach (var s in succs)
                    stack.Push(s);
            }
        }

        foreach (var p in paragraphs)
        {
            if (!visited.Contains(p))
            {
                diagnostics.ReportWarning(
                    "FLOW",
                    $"Paragraph '{p.Name}' is unreachable.",
                    new SourceLocation("<source>", 0, p.Line, 0),
                    TextSpan.Empty);
            }
        }
    }
}
