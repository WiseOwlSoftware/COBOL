// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>
/// The one C# reader of the citation format (DESIGN-listing.md section 7.3), <see cref="ListingCitation.Scan"/>, against the
/// vectors it shares with <c>scripts/spec/citation.py</c>'s <c>scan</c> (whose <c>--self-test</c> holds the Python reader to the
/// same file): each vector's citations are written by hand from the format's specification, never captured from either reader.
/// </summary>
public sealed class ListingCitationTests
{
    private sealed record Vector(string Text, IReadOnlyList<ListingCitation> Citations);

    private static IReadOnlyList<Vector> Shared()
    {
        using var json = JsonDocument.Parse(File.ReadAllText(TestRepo.At("tests", "WiseOwl.Cobol.Tests.Unit", "Listing", "citation-vectors.json")));
        return [.. json.RootElement.GetProperty("vectors").EnumerateArray().Select(v => new Vector(
            v.GetProperty("text").GetString()!,
            [.. v.GetProperty("citations").EnumerateArray().Select(c => new ListingCitation(c.GetProperty("edition").GetInt32(),
                c.GetProperty("locator").GetString()!, c.GetProperty("rule").GetString()))]))];
    }

    /// <summary>The vectors, in the file's order (a stable order: the gate lists the case names before the run).</summary>
    public static TheoryData<int> VectorIndexes() => [.. Enumerable.Range(0, Shared().Count)];

    [Theory]
    [MemberData(nameof(VectorIndexes))]
    public void EachSharedVector_ReadsAsWritten(int index)
    {
        var vector = Shared()[index];
        Assert.Equal(vector.Citations, ListingCitation.Scan(vector.Text));
    }

    /// <summary>The file holds vectors of every edition token, a rule of every kind, a bare and a malformed citation: a vector
    /// file emptied or narrowed would hold nothing.</summary>
    [Fact]
    public void TheSharedVectors_CoverEveryEditionAndRuleShape()
    {
        var vectors = Shared();
        var all = vectors.SelectMany(v => v.Citations).ToList();
        Assert.Equal([1985, 1989, 1993, 2002, 2014, 2023], all.Select(c => c.Edition).Distinct().Order());
        foreach (string kind in new[] { "SR", "GR", "AR", "RV", "Format ", "Table ", "GRL" })
            Assert.Contains(all, c => c.Rule?.StartsWith(kind, StringComparison.Ordinal) == true);
        Assert.Contains(vectors, v => v.Citations.Count == 0 && v.Text.Contains('§'));
    }

    /// <summary>A citation prints in its canonical spelling.</summary>
    [Fact]
    public void ACitation_PrintsCanonically()
    {
        Assert.Equal("§2023:14.9.8.4 GR1", new ListingCitation(2023, "14.9.8.4", "GR1").ToString());
        Assert.Equal("§1985:p.VI-20", new ListingCitation(1985, "p.VI-20", null).ToString());
    }
}
