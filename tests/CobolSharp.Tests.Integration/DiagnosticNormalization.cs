// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;

namespace CobolSharp.Tests.Integration;

/// <summary>
/// Normalizes diagnostic messages to a stable "shape" for testing.
/// Allows wording tweaks without test churn.
/// </summary>
public static partial class DiagnosticNormalization
{
    public static string Normalize(string? message)
    {
        if (message == null) return string.Empty;

        var normalized = message.Trim();
        normalized = CollapseWhitespace().Replace(normalized, " ");
        normalized = normalized.ToLowerInvariant();

        // Normalize quotes
        normalized = normalized.Replace("\u201c", "\"").Replace("\u201d", "\"")
                               .Replace("\u2018", "'").Replace("\u2019", "'");

        // Strip variable identifiers (keep structure)
        normalized = QuotedId().Replace(normalized, "'<id>'");

        // Strip variable numbers
        normalized = WholeNumber().Replace(normalized, "<n>");

        // Normalize common phrasings to canonical forms
        normalized = normalized
            .Replace("missing a space before the string literal", "missing space before string literal")
            .Replace("missing a space after the string literal", "missing space after string literal")
            .Replace("it looks like you're missing a space before the string literal.", "missing space before string literal")
            .Replace("it looks like you're missing a space after the string literal.", "missing space after string literal")
            .Replace("you may be missing the to keyword", "missing to keyword")
            .Replace("you may be missing then", "missing then keyword")
            .Replace("you may be missing a period", "missing period")
            .Replace("did you forget to before the target?", "missing to keyword");

        return normalized;
    }

    public static bool ContainsNormalized(string? message, string expectedFragment)
    {
        var normMessage = Normalize(message);
        var normFragment = Normalize(expectedFragment);
        return normMessage.Contains(normFragment, StringComparison.Ordinal);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex CollapseWhitespace();

    [GeneratedRegex(@"'[^']+'")]
    private static partial Regex QuotedId();

    [GeneratedRegex(@"\b\d+\b")]
    private static partial Regex WholeNumber();
}
