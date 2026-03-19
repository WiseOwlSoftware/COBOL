// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Common;

namespace CobolSharp.Compiler.Diagnostics;

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
