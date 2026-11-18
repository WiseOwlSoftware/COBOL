// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;

namespace WiseOwl.Cobol.Listing;

/// <summary>One citation of a COBOL standard a diagnostic's message makes, in the repository's citation format
/// <c>§&lt;edition&gt;:&lt;locator&gt;[ &lt;rule&gt;]</c> (<c>§2023:13.18.40.3 SR14</c>, <c>§1985:VI.5.3.4 GR3</c>).</summary>
/// <param name="Edition">The edition's year as the format writes it: 2023, 2014, 2002 or 1985 for the standards, 1989 and 1993 for
/// the two amendments of the 1985 standard.</param>
/// <param name="Locator">The clause, or for 1985 the Roman section and paragraph (<c>VI.5.3.4</c>) or printed page (<c>p.VI-20</c>).</param>
/// <param name="Rule">The rule within the clause (<c>GR1</c>, <c>SR14</c>, <c>RV3</c>, <c>Format 10</c>), or null when none is named.</param>
public sealed record ListingCitation(int Edition, string Locator, string? Rule)
{
    /// <summary>The citation's canonical spelling.</summary>
    public override string ToString() => $"§{Edition}:{Locator}" + (Rule is null ? "" : " " + Rule);

    /// <summary>
    /// ⛔ THE ONE C# READER OF THE CITATION FORMAT (DESIGN-listing.md section 7.3): every well-formed citation of a standard in
    /// <paramref name="text"/>, left to right. It is <c>scripts/spec/citation.py</c>'s <c>scan</c> (the format's specification and
    /// its one Python parser), and the two read the same text alike: the shared vectors
    /// (<c>tests/WiseOwl.Cobol.Tests.Unit/Listing/citation-vectors.json</c>) are the expected answers of both, held by
    /// <c>ListingCitationTests</c> here and by <c>citation.py --self-test</c> there. A bare <c>§N</c> (no edition) is no citation,
    /// and a malformed one (an edition that is none, a locator of the wrong shape for its edition) is not read.
    /// </summary>
    public static IReadOnlyList<ListingCitation> Scan(string text)
    {
        if (!text.Contains('§')) return [];
        var found = new List<ListingCitation>();
        foreach (Match m in ScanPattern.Matches(text))
        {
            if (!m.Groups["edition"].Success) continue;   // a bare §N
            int edition = int.Parse(m.Groups["edition"].ValueSpan, System.Globalization.CultureInfo.InvariantCulture);
            if (!LocatorShape.TryGetValue(edition, out var shape)) continue;   // no edition of the standard
            string locator = m.Groups["locator"].Value;
            if (locator.Length == 0 || !shape.IsMatch(locator)) continue;     // no locator, or one of another edition's shape
            found.Add(new ListingCitation(edition, locator, m.Groups["rule"].Success ? CanonicalRule(m.Groups["rule"].Value) : null));
        }
        return found;
    }

    /// <summary>A rule designator in its canonical spelling: <c>GR 1</c> is <c>GR1</c>, <c>GR L2.1</c> is <c>GRL2.1</c>.</summary>
    private static string CanonicalRule(string rule)
    {
        var sublist = SublistRule.Match(rule);
        if (sublist.Success && sublist.Index == 0)
            return SublistRule.Replace(rule, "$1L$2.$3", 1);
        var numbered = NumberedRule.Match(rule);
        return numbered.Success && numbered.Index == 0 ? NumberedRule.Replace(rule, "$1$2", 1) : rule;
    }

    private const string RuleKinds = "SR|GR|AR|RV";

    /// <summary>citation.py's <c>RULE</c>.</summary>
    private const string RulePattern = $@"(?:(?:{RuleKinds}) ?(?:\d+(?:[a-z0-9.]*[a-z0-9])?|L\d+\.\d+)|Format \d+|Table \d+)";

    /// <summary>citation.py's <c>_SCAN</c>: its <c>PATTERN</c> and an optional rule after one space.</summary>
    private static readonly Regex ScanPattern = new(
        @"(?<cite>§(?:(?<edition>\d{4}):(?![<{(])(?<locator>p\.[A-Z]+-\d+|[A-Z0-9]+(?:\.[A-Z0-9]+)*(?![a-z]))?"
        + @"|\s?(?<bare>(?:\d+|[A-Z])(?:\.\d+)+)(?![a-z]|\.[A-Z])))"
        + $"(?: (?<rule>{RulePattern}))?",
        RegexOptions.CultureInvariant);

    private static readonly Regex NumberedRule = new($@"({RuleKinds}) ?(\d+)", RegexOptions.CultureInvariant);
    private static readonly Regex SublistRule = new($@"({RuleKinds}) ?L(\d+)\.(\d+)", RegexOptions.CultureInvariant);

    /// <summary>citation.py's <c>EDITIONS</c> and <c>_LOCATOR</c>: each edition's locator shape (an ISO clause; a 1985 section,
    /// page or appendix; an amendment's own paragraph or page).</summary>
    private static readonly Dictionary<int, Regex> LocatorShape = new()
    {
        [2023] = Clause, [2014] = Clause, [2002] = Clause,
        [1985] = new(@"\A(?:p\.(?:[IVX]+|[A-F])-\d+|[IVX]+(?:\.\d+)*|[A-F](?:\.\d+)*)(?![a-z])\z", RegexOptions.CultureInvariant),
        [1989] = Own, [1993] = Own,
    };

    private static Regex Clause => new(@"\A(?:\d+(?:\.\d+)*|[A-Z](?:\.\d+)*)(?![a-z])\z", RegexOptions.CultureInvariant);
    private static Regex Own => new(@"\A(?:p\.)?[A-Z0-9]+(?:[.-][A-Z0-9]+)*(?![a-z])\z", RegexOptions.CultureInvariant);
}
