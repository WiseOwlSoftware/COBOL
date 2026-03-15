// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Common;

namespace CobolSharp.Compiler.Diagnostics;

/// <summary>
/// A compiler diagnostic (error, warning, or informational message).
/// </summary>
public sealed class Diagnostic
{
    public string Code { get; }
    public DiagnosticSeverity Severity { get; }
    public string Message { get; }
    public SourceLocation Location { get; }
    public TextSpan Span { get; }

    public Diagnostic(string code, DiagnosticSeverity severity, string message,
        SourceLocation location, TextSpan span)
    {
        Code = code;
        Severity = severity;
        Message = message;
        Location = location;
        Span = span;
    }

    public bool IsError => Severity == DiagnosticSeverity.Error;

    public override string ToString()
    {
        var sev = Severity switch
        {
            DiagnosticSeverity.Error => "error",
            DiagnosticSeverity.Warning => "warning",
            DiagnosticSeverity.Info => "info",
            _ => "unknown"
        };
        return $"{Location}: {sev} {Code}: {Message}";
    }
}
