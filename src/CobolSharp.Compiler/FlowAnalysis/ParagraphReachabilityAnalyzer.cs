using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Semantics;

namespace CobolSharp.Compiler.FlowAnalysis;

/// <summary>
/// Checks which paragraphs are reachable from the entry point.
/// Unreachable paragraphs are flagged as warnings.
/// </summary>
public sealed class ParagraphReachabilityAnalyzer
{
    private readonly IReadOnlyList<ParagraphSymbol> _paragraphs;
    private readonly Dictionary<ParagraphSymbol, List<ParagraphSymbol>> _edges;
    private readonly DiagnosticBag _diagnostics;

    public ParagraphReachabilityAnalyzer(
        IReadOnlyList<ParagraphSymbol> paragraphs,
        Dictionary<ParagraphSymbol, List<ParagraphSymbol>> edges,
        DiagnosticBag diagnostics)
    {
        _paragraphs = paragraphs;
        _edges = edges;
        _diagnostics = diagnostics;
    }

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

            if (_edges.TryGetValue(p, out var succs))
            {
                foreach (var s in succs)
                    stack.Push(s);
            }
        }

        foreach (var p in _paragraphs)
        {
            if (!visited.Contains(p))
            {
                _diagnostics.ReportWarning(
                    "FLOW",
                    $"Paragraph '{p.Name}' is unreachable.",
                    new Common.SourceLocation("<source>", 0, p.Line, 0),
                    new Common.TextSpan(0, 0));
            }
        }
    }
}
