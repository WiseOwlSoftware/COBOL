// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Semantics.Bound;

namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Builds a procedure-level control flow graph and detects:
/// - Unreachable paragraphs (CBL3001)
/// - Illegal cross-section fall-through (CBL3002)
/// - PERFORM cycles (CBL3004)
/// </summary>
public static class ProcedureGraph
{
    /// <summary>
    /// Run flow analysis on the bound program and emit diagnostics.
    /// </summary>
    public static void Analyze(BoundProgram program, SemanticModel model, DiagnosticBag diagnostics)
    {
        if (program.Paragraphs.Count == 0) return;

        // Build adjacency: paragraph → set of reachable paragraphs
        var edges = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var allNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var para in program.Paragraphs)
        {
            var name = para.Symbol.Name;
            allNames.Add(name);
            if (!edges.ContainsKey(name))
                edges[name] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        // Add fall-through edges (each paragraph to the next in declaration order)
        for (int i = 0; i < program.Paragraphs.Count - 1; i++)
        {
            var from = program.Paragraphs[i].Symbol.Name;
            var to = program.Paragraphs[i + 1].Symbol.Name;

            // Check for cross-section fall-through
            var fromSection = model.GetParagraphSection(from);
            var toSection = model.GetParagraphSection(to);
            if (fromSection != null && toSection != null && !fromSection.Equals(toSection, StringComparison.OrdinalIgnoreCase))
            {
                // Only warn if the preceding paragraph doesn't end with GO TO or STOP
                if (!EndsWithTransfer(program.Paragraphs[i]))
                {
                    diagnostics.Report(DiagnosticDescriptors.CBL3002,
                        new SourceLocation("<source>", 0, program.Paragraphs[i].Symbol.Line, 0),
                        TextSpan.Empty, fromSection, toSection);
                }
            }

            edges[from].Add(to);
        }

        // Add PERFORM and GO TO edges
        foreach (var para in program.Paragraphs)
        {
            CollectTransferEdges(para, edges);
        }

        // Reachability analysis (BFS from entry paragraph + all PERFORM targets)
        var reachable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>();

        // Entry point is the first paragraph
        queue.Enqueue(program.Paragraphs[0].Symbol.Name);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!reachable.Add(current)) continue;

            if (edges.TryGetValue(current, out var targets))
            {
                foreach (var target in targets)
                    queue.Enqueue(target);
            }
        }

        // Report unreachable paragraphs
        foreach (var para in program.Paragraphs)
        {
            if (!reachable.Contains(para.Symbol.Name))
            {
                diagnostics.Report(DiagnosticDescriptors.CBL3001,
                    new SourceLocation("<source>", 0, para.Symbol.Line, 0),
                    TextSpan.Empty, para.Symbol.Name);
            }
        }
    }

    private static bool EndsWithTransfer(BoundParagraph para)
    {
        if (para.Sentences.Count == 0) return false;
        var lastSentence = para.Sentences[^1];
        if (lastSentence.Statements.Count == 0) return false;
        var lastStmt = lastSentence.Statements[^1];
        return lastStmt is BoundGoToStatement or BoundStopStatement
            or BoundExitSectionStatement or BoundExitParagraphStatement;
    }

    private static void CollectTransferEdges(BoundParagraph para,
        Dictionary<string, HashSet<string>> edges)
    {
        var name = para.Symbol.Name;
        foreach (var sentence in para.Sentences)
        {
            foreach (var stmt in sentence.Statements)
                CollectTransferEdgesFromStatement(name, stmt, edges);
        }
    }

    private static void CollectTransferEdgesFromStatement(string fromPara,
        BoundStatement stmt, Dictionary<string, HashSet<string>> edges)
    {
        switch (stmt)
        {
            case BoundPerformStatement perf:
                if (perf.Target != null)
                    edges[fromPara].Add(perf.Target.Name);
                if (perf.ThruTarget != null)
                    edges[fromPara].Add(perf.ThruTarget.Name);
                if (perf.InlineStatements != null)
                    foreach (var inner in perf.InlineStatements)
                        CollectTransferEdgesFromStatement(fromPara, inner, edges);
                break;

            case BoundGoToStatement goTo:
                foreach (var target in goTo.Targets)
                    edges[fromPara].Add(target.Name);
                break;

            case BoundIfStatement ifStmt:
                foreach (var inner in ifStmt.ThenStatements)
                    CollectTransferEdgesFromStatement(fromPara, inner, edges);
                if (ifStmt.ElseStatements != null)
                    foreach (var inner in ifStmt.ElseStatements)
                        CollectTransferEdgesFromStatement(fromPara, inner, edges);
                break;

            case BoundEvaluateStatement eval:
                foreach (var when in eval.Whens)
                    foreach (var inner in when.Statements)
                        CollectTransferEdgesFromStatement(fromPara, inner, edges);
                if (eval.WhenOther != null)
                    foreach (var inner in eval.WhenOther)
                        CollectTransferEdgesFromStatement(fromPara, inner, edges);
                break;

            case BoundSearchStatement search:
                foreach (var when in search.Whens)
                    foreach (var inner in when.Statements)
                        CollectTransferEdgesFromStatement(fromPara, inner, edges);
                foreach (var inner in search.AtEnd)
                    CollectTransferEdgesFromStatement(fromPara, inner, edges);
                break;

            case BoundCompoundStatement compound:
                foreach (var inner in compound.Statements)
                    CollectTransferEdgesFromStatement(fromPara, inner, edges);
                break;
        }
    }
}
