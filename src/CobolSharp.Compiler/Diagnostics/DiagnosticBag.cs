using CobolSharp.Compiler.Common;

namespace CobolSharp.Compiler.Diagnostics;

/// <summary>
/// Collects diagnostics during compilation. Passed through all compiler phases.
/// </summary>
public sealed class DiagnosticBag
{
    private readonly List<Diagnostic> _diagnostics = new();

    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;
    public bool HasErrors => _diagnostics.Any(d => d.IsError);

    public void Report(string code, DiagnosticSeverity severity, string message,
        SourceLocation location, TextSpan span)
    {
        _diagnostics.Add(new Diagnostic(code, severity, message, location, span));
    }

    public void ReportError(string code, string message, SourceLocation location, TextSpan span)
    {
        Report(code, DiagnosticSeverity.Error, message, location, span);
    }

    public void ReportWarning(string code, string message, SourceLocation location, TextSpan span)
    {
        Report(code, DiagnosticSeverity.Warning, message, location, span);
    }
}
