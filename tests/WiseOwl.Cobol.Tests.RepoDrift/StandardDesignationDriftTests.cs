// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.RepoDrift;

/// <summary>⛔ A 1985 CITATION NAMES NO ISO STANDARD (kb/Work PB6013, PB6010). The 1985 text is American National Standard
/// X3.23-1985; no ISO standard has a <c>§1985</c> clause, so a message saying <c>ISO §1985:VII.2.7.3 SR1</c> sends the user to a
/// document that does not exist. A message's citation takes its designation from ONE function, <c>EditionInfo.Designated</c>
/// (<c>ISO </c> before a rule of ISO/IEC 1989, nothing before the edition form of a 1985 or 1989 rule, <c>§1985:VII.2.7.3 SR1</c>), or from
/// <c>EditionInfo.CiteRule</c>, which chooses the rule by edition and designates it through that function. So no source line
/// types <c>ISO §1985</c>, and no message types <c>ISO</c> in front of a COMPUTED citation (an interpolation <c>ISO {…}</c>, or a
/// literal ending <c>ISO "</c> joined by <c>+</c> to anything but a literal later-edition citation such as <c>"§2023:13.10.3 SR11)"</c>), which renders
/// <c>ISO §1985</c> whenever it computes a 1985 rule. Comments are not messages and are not counted. The runtime is outside the
/// computed-citation half: it has no edition and cites the rule its own text names; a literal <c>ISO §1985</c> there is still
/// refused.</summary>
[Trait("Sweep", "tree")]
public sealed class StandardDesignationDriftTests
{
    /// <summary>The C# sources the messages are built in (declared, kb/Work PB3531).</summary>
    private static readonly TreeScope Sources = new(Under: ["src/"], Kinds: [".cs"]);

    /// <summary>The one function that writes the designation (it is the one place <c>"ISO " + citation</c> may stand).</summary>
    private const string DesignationHome = "src/WiseOwl.Cobol.Editions/EditionInfo.cs";

    /// <summary>The runtime: no edition reaches it, so no citation it prints is chosen by edition.</summary>
    private const string Runtime = "src/WiseOwl.Cobol.Runtime/";

    private static readonly Regex Literal1985 = new(@"ISO §1985", RegexOptions.Compiled);

    private static readonly Regex ComputedCitation =
        new(@"ISO \{|ISO ""\s*\+(?!\s*\$?""§(2002|2014|2023):)", RegexOptions.Compiled);

    [Fact]
    public void NoMessageTypesAnIsoDesignationBeforeACitationThatCanBe1985s()
    {
        var hits = new List<string>();
        foreach (string path in TrackedTree.Files(Sources))
        {
            string[] lines = File.ReadAllLines(Path.Combine(TestRepo.Root, path));
            string text = string.Join("\n", lines.Select(l => l.TrimStart().StartsWith("//", StringComparison.Ordinal) ? "" : l));
            bool computedAsked = path != DesignationHome && !path.StartsWith(Runtime, StringComparison.Ordinal);
            foreach (Match m in Literal1985.Matches(text)) hits.Add(Hit(path, lines, text, m));
            if (computedAsked)
                foreach (Match m in ComputedCitation.Matches(text)) hits.Add(Hit(path, lines, text, m));
        }
        Assert.True(hits.Count == 0,
            $"{hits.Count} message sites type a standard's designation before a citation that can be 1985's: write the citation "
            + "through EditionInfo.Designated (or EditionInfo.CiteRule for a rule chosen by edition), never \"ISO \" by hand:\n  "
            + string.Join("\n  ", hits));
    }

    [Theory]
    [InlineData("    Report($\"x (ISO {edition.Cite(\"§1985:VI.5.3.4 GR1\", \"§2023:13.16.3 SR11\")})\");", true)]
    [InlineData("    Report(\"x (ISO \"\n        + Rule(edition) + \")\");", true)]
    [InlineData("    Report(\"x (ISO §1985:VII.2.7.3 SR1)\");", true)]
    [InlineData("    Report(\"x (ISO \"\n        + \"§2023:13.10.3 SR11)\");", false)]
    [InlineData("    Report($\"x ({edition.CiteRule(\"§1985:VI.5.3.4 GR1\", \"§2023:13.16.3 SR11\")})\");", false)]
    [InlineData("    // a comment saying ISO §1985 is no message", false)]
    public void TheRule_RefusesEachHandTypedForm_AndPassesTheDesignatedOnes(string source, bool refused)
    {
        string text = string.Join("\n", source.Split('\n').Select(l => l.TrimStart().StartsWith("//", StringComparison.Ordinal) ? "" : l));
        Assert.Equal(refused, Literal1985.IsMatch(text) || ComputedCitation.IsMatch(text));
    }

    private static string Hit(string path, string[] lines, string text, Match m)
    {
        int line = text.AsSpan(0, m.Index).Count('\n');
        return $"{path}:{line + 1}: {lines[line].Trim()}";
    }
}
