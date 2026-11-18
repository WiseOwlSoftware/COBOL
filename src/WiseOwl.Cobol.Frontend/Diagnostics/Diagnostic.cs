// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Common;

namespace WiseOwl.Cobol.Frontend.Diagnostics;

/// <summary>
/// A compiler diagnostic (error, warning, or informational message): THE ONE RECORD of a diagnostic, for every phase that
/// reports one — the front end's (<see cref="DiagnosticBag"/>), the binder's (<c>EditionContext</c>, DESIGN-listing.md section
/// 6.3) and a code-generation backend's (the seam's <c>BackendArtifact.Diagnostics</c>, kb/Work PB4272).
/// </summary>
/// <param name="Code">The diagnostic's code (<c>COBOL0001</c>; a backend's own identifier, such as Roslyn's <c>CS0103</c>).</param>
/// <param name="Severity">Error, warning or information.</param>
/// <param name="Message">The diagnostic's text.</param>
/// <param name="Location">Where it lies: the file, physical line and column the user edits, or <see langword="null"/> for a
/// diagnostic about the compilation unit as a whole (a bind diagnostic reported with no cursor, an output file a backend could
/// not write).</param>
/// <param name="Span">The span of the offending text in the text that was read, where the reporter knows it.</param>
public sealed record Diagnostic(
    string Code,
    DiagnosticSeverity Severity,
    string Message,
    SourceLocation? Location,
    TextSpan Span)
{
    public bool IsError => Severity == DiagnosticSeverity.Error;

    public override string ToString() => Render(Location, Severity, Code, Message);

    /// <summary>The ONE text of a diagnostic line: <c>file(line,col): error CODE: message</c>, or <c>error CODE: message</c>
    /// for a diagnostic that names no source position.</summary>
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
