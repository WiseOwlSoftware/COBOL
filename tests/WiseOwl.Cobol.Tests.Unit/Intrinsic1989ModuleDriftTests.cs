// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE PARAGRAPH A FUNCTION HAS IN THE 1989 INTRINSIC FUNCTION MODULE IS DERIVED, AND THIS READS IT BACK FROM THE STANDARD (kb/Work
/// PB4950). A diagnostic about a function's argument cites, at <c>--std 85</c>, <c>§1989:A.2.n.3</c>
/// (<see cref="IntrinsicCitations.Clause"/>), where <c>n</c> is <see cref="IntrinsicCatalog.Paragraph1989"/>: the catalog's
/// COBOL-85 functions in ordinal name order, numbered from 5. The transcription prints one <c>## 2.n THE x FUNCTION</c> heading per
/// function; the guard compares the two in both directions, so a function the catalog introduces at 85 and the module lacks (or the
/// reverse) fails here, and the number can never be a hand-kept list that drifts.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class Intrinsic1989ModuleDriftTests
{
    /// <summary>The module's headings, from the transcription of ANSI X3.23-1985 and its amendment: paragraph number → function.</summary>
    private static IReadOnlyDictionary<int, string> Headings()
    {
        var headings = new Dictionary<int, string>();
        string[] lines = File.ReadAllLines(TestRepo.Specs("ANSI_COBOL_1985.md"));
        // The module's paragraphs are the ones after "A.2" — the nucleus has no heading of this form.
        var heading = new Regex(@"^#{2,3} 2\.(?<n>\d+) THE (?<name>[A-Z0-9-]+) FUNCTION\s*$", RegexOptions.Compiled);
        foreach (string line in lines)
            if (heading.Match(line) is { Success: true } m)
                headings[int.Parse(m.Groups["n"].Value)] = m.Groups["name"].Value;
        return headings;
    }

    [Fact]
    public void EveryFunctionOfTheModule_HasTheParagraphTheTranscriptionPrintsForIt()
    {
        var headings = Headings();
        Assert.True(headings.Count >= 40, $"the transcription yields {headings.Count} function headings; the module has 42");
        var wrong = new List<string>();
        foreach (var (paragraph, name) in headings)
            if (IntrinsicCatalog.Paragraph1989(name) != paragraph)
                wrong.Add($"2.{paragraph} THE {name} FUNCTION: the catalog says {IntrinsicCatalog.Paragraph1989(name)?.ToString() ?? "no paragraph"}");
        Assert.True(wrong.Count == 0, "IntrinsicCatalog.Paragraph1989 disagrees with the 1989 module's headings:\n" + string.Join("\n", wrong));
    }

    [Fact]
    public void EveryFunctionTheCatalogIntroducesAt85_IsInTheModule()
    {
        var names = Headings().Values.ToHashSet(StringComparer.Ordinal);
        foreach (string name in names)
            Assert.True(IntrinsicCatalog.TryGet(name, out var sig) && sig.IntroducedIn == 85,
                $"{name} has a paragraph in the 1989 module, so the catalog shall introduce it at COBOL-85");
        // …and the other way: a function introduced at 85 has a paragraph, a later one has none.
        foreach (string probe in new[] { "ACOS", "WHEN-COMPILED", "STANDARD-DEVIATION" })
            Assert.NotNull(IntrinsicCatalog.Paragraph1989(probe));
        foreach (string probe in new[] { "ABS", "CONCAT", "FIND-STRING" })
            Assert.Null(IntrinsicCatalog.Paragraph1989(probe));
    }

    [Fact]
    public void AFunctionTheEditionLacks_SaysSoWithTheEditionGatesPhrase()
    {
        EditionInfo at85 = EditionInfo.Of(85);
        Assert.Equal("§1989:A.2.40.3", IntrinsicCitations.Clause(at85, "SQRT", "§2023:15.84.3 r1"));
        string later = IntrinsicCitations.Clause(at85, "ABS", "§2023:15.7.3 r1");
        Assert.Contains("(targeting COBOL-85)", later);
        Assert.StartsWith("§2023:15.7.3 r1", later);
        Assert.Equal("§2023:15.84.3 r1", IntrinsicCitations.Clause(EditionInfo.Of(2002), "SQRT", "§2023:15.84.3 r1"));
    }
}
