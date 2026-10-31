// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Common;

namespace WiseOwl.Cobol.Frontend.Diagnostics;

/// <summary>
/// Collects diagnostics during compilation. Passed through all compiler phases.
/// </summary>
public sealed class DiagnosticBag
{
    private readonly List<Diagnostic> _diagnostics = [];

    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;
    public bool HasErrors => _diagnostics.Exists(d => d.IsError);

    private readonly List<CompileOutputLine> _compileOutput = [];

    /// <summary>What the compilation itself TRANSFERS to its operator, in order: the lines the DISPLAY directive writes at
    /// compile time (ISO §7.3.12.4 GR1, kb/Work PB1538). Not a diagnostic — a diagnostic names a defect at a position,
    /// and a transferred line is the program's own output — so it travels beside them on the sink every phase already
    /// holds, and the driver hands it to the CLI, which writes each line to the stream it names.</summary>
    public IReadOnlyList<CompileOutputLine> CompileOutput => _compileOutput;

    /// <summary>Transfer one line to <paramref name="stream"/> when the compilation's output is presented.</summary>
    public void Transfer(CompileOutputStream stream, string text) => _compileOutput.Add(new CompileOutputLine(stream, text));

    /// <summary>Carry <paramref name="lines"/> over from another bag (a speculative front-end pass whose result is the
    /// compilation's).</summary>
    public void AddCompileOutput(IEnumerable<CompileOutputLine> lines) => _compileOutput.AddRange(lines);

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

    public void Add(Diagnostic diagnostic) => _diagnostics.Add(diagnostic);

    /// <summary>
    /// Add <paramref name="causes"/> (in the order given) so that each reads BEFORE the first effect it can explain: the first
    /// diagnostic at index <paramref name="from"/> or later that sits in the same file at or after the cause's own position.
    /// A cause with no such effect is appended. Reports are otherwise kept in the order they were made; this is the one place a
    /// diagnosis found AFTER the stage whose failure it explains (a word-character screen that needs the partial parse tree to
    /// know which tokens are words, kb/Work PB4131) is put where a reader meets it first. A tie in position puts the cause
    /// first: the cause outranks its effect. Diagnostics of another file are never compared (the positions of two files do not
    /// order), and those before <paramref name="from"/> are never moved past.
    /// </summary>
    public void AddBeforeEffects(IEnumerable<Diagnostic> causes, int from)
    {
        foreach (var cause in causes)
        {
            int at = _diagnostics.Count;
            for (int i = from; i < _diagnostics.Count; i++)
            {
                var effect = _diagnostics[i].Location;
                if (effect.FileName == cause.Location.FileName
                    && (effect.Line, effect.Column).CompareTo((cause.Location.Line, cause.Location.Column)) >= 0)
                {
                    at = i;
                    break;
                }
            }
            _diagnostics.Insert(at, cause);
        }
    }

    /// <summary>
    /// Report a diagnostic using a descriptor and format arguments.
    /// </summary>
    public void Report(DiagnosticDescriptor descriptor, SourceLocation location, TextSpan span,
        params object[] args)
    {
        string message = args.Length > 0
            ? string.Format(descriptor.MessageTemplate, args)
            : descriptor.MessageTemplate;
        _diagnostics.Add(new Diagnostic(descriptor.Code, descriptor.DefaultSeverity, message, location, span));
    }
}
