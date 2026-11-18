// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>⛔ ONE DECIDER OF A MESSAGE'S WORDS BY EDITION (kb/Work PB4950 review R1-1; docs/VERSION_TEST_MATRIX_DESIGN.md P2.3a).
/// <c>EditionInfo.Cite(y1985, y2002, y2023, y2014)</c> is the one choice of the citation, or of the words a message quotes, by
/// edition. A message site that spells its own <c>DialectLevel &lt; 2002 ? "…" : "…"</c> is a second decider that the next
/// edition's text has to be threaded through by hand, so this test refuses, anywhere under <c>src/</c> except the decider itself
/// and the generated parser, an edition comparison whose ternary arm is a string literal.</summary>
public sealed class EditionCiteOneDeciderDriftTests
{
    /// <summary>An edition comparison (<c>DialectLevel</c>, <c>Year</c>, <c>dialectLevel</c> against 85, 2002, 2014 or 2023) whose
    /// ternary's first arm is a string literal or an interpolated string.</summary>
    private static readonly Regex AdHocEditionText = new(
        @"\b(?:DialectLevel|Year|dialectLevel)\s*(?:==|!=|<=|>=|<|>)\s*(?:85|2002|2014|2023)\s*\?\s*\$?@?""", RegexOptions.Compiled);

    [Fact]
    public void NoMessageSite_ChoosesItsTextByEditionOutsideCite()
    {
        string src = TestRepo.Src();
        string decider = Path.Combine("WiseOwl.Cobol.Editions", "EditionInfo.cs");
        var offences = Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Select(f => (Full: f, Rel: Path.GetRelativePath(src, f)))
            .Where(f => !f.Rel.Split(Path.DirectorySeparatorChar).Any(seg => seg is "bin" or "obj" or "Generated"))
            .Where(f => f.Rel != decider)
            .SelectMany(f => File.ReadLines(f.Full).Select((line, i) => (f.Rel, Line: i + 1, Text: line)))
            .Where(l => AdHocEditionText.IsMatch(l.Text))
            .Select(l => $"{l.Rel.Replace('\\', '/')}:{l.Line}: {l.Text.Trim()}")
            .Order(StringComparer.Ordinal)
            .ToList();
        Assert.True(offences.Count == 0,
            "a message site chooses its words by edition with its own ternary; write it as EditionInfo.Cite(y1985, y2002, y2023[, y2014]) "
            + "(kb/Work PB4950, P2.3a):\n" + string.Join("\n", offences));
    }

    /// <summary>The pattern finds what it is for, so an empty answer above is evidence.</summary>
    [Theory]
    [InlineData("x = Edition.DialectLevel < 2002 ? \"a\" : \"b\";", true)]
    [InlineData("x = edition.Year == 2014 ? $\"a{y}\" : \"b\";", true)]
    [InlineData("x = edition.Cite(\"a\", \"b\", \"b\");", false)]
    [InlineData("int n = dialectLevel >= 2023 ? 63 : 31;", false)]
    public void ThePatternSeesAnAdHocTernary(string line, bool offends) =>
        Assert.Equal(offends, AdHocEditionText.IsMatch(line));
}
