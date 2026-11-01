// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Common;

namespace WiseOwl.Cobol.Frontend.Diagnostics;

/// <summary>
/// A compiler diagnostic (error, warning, or informational message).
/// </summary>
public sealed record Diagnostic(
    string Code,
    DiagnosticSeverity Severity,
    string Message,
    SourceLocation Location,
    TextSpan Span)
{
    public bool IsError => Severity == DiagnosticSeverity.Error;

    public override string ToString() => Render(Location, Severity, Code, Message);

    /// <summary>The ONE text of a diagnostic line, for every diagnostic the compiler reports: the front end's and binder's
    /// (<see cref="Diagnostic"/>) and a code-generation backend's (<c>WiseOwl.Cobol.CodeGen.BackendDiagnostic</c>, kb/Work
    /// PB4272): <c>file(line,col): error CODE: message</c>, or <c>error CODE: message</c> for a diagnostic that names no
    /// source position (a backend that cannot write its output file).</summary>
    public static string Render(SourceLocation? location, DiagnosticSeverity severity, string code, string message)
    {
        var sev = severity switch
        {
            DiagnosticSeverity.Error => "error",
            DiagnosticSeverity.Warning => "warning",
            DiagnosticSeverity.Info => "info",
            _ => "unknown"
        };
        return location is { } at ? $"{at}: {sev} {code}: {message}" : $"{sev} {code}: {message}";
    }
}
