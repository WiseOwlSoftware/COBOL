// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// Every edition edge of the construct catalogue (<c>tests/version-matrix/constructs/</c>) is QUOTED from the held edition
/// texts, never labelled derived or provisional: no row's <c>description</c>, <c>citation</c>, <c>vcr</c> or
/// <c>rule1985</c> may say "derive"/"derived"/"derivation", "provisional", "decision 1", "ratified decision", "holds no
/// &lt;year&gt;", "not quoted" or "vendor transcription", in any case. The repository holds the 1985, 2002, 2014 and 2023
/// texts, so the reason those labels existed is gone (kb/Work PB3965: 122 rows re-cited, each citation through
/// <c>scripts/spec/cite.py --check</c>); a new row writes its edge's quotations instead.
/// </summary>
public sealed partial class ConstructEdgeProvenanceDriftTests
{
    private static readonly string[] Fields = ["description", "citation", "vcr", "rule1985"];

    [GeneratedRegex(@"deriv|provisional|decision[- ]1\b|ratified decision|holds no (1985|2002|2014)|not quoted|vendor transcription",
        RegexOptions.IgnoreCase)]
    private static partial Regex Unquoted();

    [Fact]
    public void EveryEdge_IsQuoted_NeverLabelledDerivedOrProvisional()
    {
        var found = new List<string>();
        foreach (var row in TestRepo.ConstructRows())
        {
            string id = row.GetProperty("id").GetString()!;
            foreach (var field in Fields)
                if (row.TryGetProperty(field, out var v) && v.ValueKind == JsonValueKind.String
                    && Unquoted().Match(v.GetString()!) is { Success: true } m)
                    found.Add($"{id}.{field}: \"{m.Value}\"");
        }
        Assert.True(found.Count == 0,
            "construct rows label an edge derived or provisional; quote it from the edition texts (kb/Work PB3965):\n  "
            + string.Join("\n  ", found));
    }

    [Fact]
    public void ThePattern_RefusesEachLabel_AndPassesQuotedText()
    {
        foreach (var s in new[] { "a COBOL-2002 introduction (derived: VCR row 7.27)", "PROVISIONAL 2002 edge",
                     "derive from the 2002 standard", "per decision 1", "the repo holds no 1985 text",
                     "THE EDGE IS DERIVED, NOT QUOTED", "ANSI X3.23-1985 (vendor transcriptions)" })
            Assert.Matches(Unquoted(), s);
        Assert.DoesNotMatch(Unquoted(), "THE EDGE IS QUOTED from the edition texts (kb/Work PB3965): §2002:F.2 item 63");
    }
}
