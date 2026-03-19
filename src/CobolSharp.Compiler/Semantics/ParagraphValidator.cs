// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Frozen;
using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;

namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Detects phantom paragraphs -- paragraphs whose names match COBOL keywords
/// that commonly appear as unconsumed clause tokens (e.g., WRITE ... ADVANCING
/// where ADVANCING was not consumed by the parser and became a paragraph name).
/// Emits a warning diagnostic for each match so grammar bugs surface early.
/// </summary>
public static class ParagraphValidator
{
    /// <summary>
    /// Keywords that should never appear as paragraph names in well-parsed programs.
    /// Each entry corresponds to a clause keyword that is sometimes left unconsumed
    /// by the parser when a statement rule is incomplete.
    /// </summary>
    private static readonly FrozenSet<string> SuspiciousNames =
        new[] { "LINE", "LINES", "PAGE", "ADVANCING", "GIVING", "ROUNDED",
                "TALLYING", "REPLACING", "UNTIL", "VARYING", "TIMES" }
        .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Checks all paragraphs in <paramref name="model"/> against the suspicious-name
    /// list and adds a warning to <paramref name="diagnostics"/> for each match.
    /// </summary>
    public static void Validate(SemanticModel model, DiagnosticBag diagnostics)
    {
        foreach (var para in model.ParagraphsInOrder)
        {
            if (SuspiciousNames.Contains(para.Name))
            {
                diagnostics.Add(new Diagnostic(
                    "SEM",
                    DiagnosticSeverity.Warning,
                    $"Paragraph '{para.Name}' has a name that matches a COBOL keyword — " +
                    "this may indicate a parsing error (e.g., unconsumed keyword from a statement clause).",
                    new SourceLocation("<source>", 0, para.Line, 0),
                    new TextSpan(0, 0)));
            }
        }
    }
}
