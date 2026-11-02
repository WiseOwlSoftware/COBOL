// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Tests.Shared;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The source of the REFERENCE-RESOLUTION subsystem, found by what a file declares and never by a list that could rot
/// (DESIGN-architecture-review §8.3 (4), kb/Work PB2299, PB2371–PB2375): the <c>ReferenceResolver</c> funnel and every part it
/// composes, each a class whose primary constructor takes <c>(ReferenceResolver refs, …)</c>. A source-scan drift test asks
/// WHERE a rule is written, and the rule's home moved from one 3,300-line class to its parts; it reads the subsystem as one
/// text, so the scan keeps measuring the rule whichever part holds it today.
/// </summary>
internal static class ResolverSources
{
    private static readonly Lazy<IReadOnlyList<string>> s_files = new(() =>
        Directory.GetFiles(TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding"), "*.cs")
            .Where(f => Path.GetFileName(f).StartsWith("ReferenceResolver", StringComparison.Ordinal)
                        || File.ReadAllText(f).Contains("(ReferenceResolver refs", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToList());

    /// <summary>Every source file of the subsystem, ordinal by path.</summary>
    public static IReadOnlyList<string> Files => s_files.Value;

    /// <summary>The subsystem's source as one text — the funnel first, then the parts.</summary>
    public static string Text { get; } = string.Concat(
        Files.OrderBy(f => Path.GetFileName(f) == "ReferenceResolver.cs" ? 0 : 1).ThenBy(f => f, StringComparer.Ordinal)
            .Select(f => File.ReadAllText(f) + "\n"));

    /// <summary>The text of one file of the subsystem by name.</summary>
    public static string Of(string fileName) =>
        File.ReadAllText(Files.Single(f => Path.GetFileName(f) == fileName));
}
