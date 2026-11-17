// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>The 1985 citation ratchet's own invariants (kb/Work PB4950 review R1-1): no entry outlives its refusal, and the
/// arithmetic of <see cref="EditionCitationNet.RatchetFindings"/> fails a grown, a shrunk and a zeroed count.</summary>
public sealed class EditionCitationNetDriftTests
{
    /// <summary>Every entry names a refusal the wiseowl manifest compiles at 85; any other entry is never asked, so it would
    /// keep a count nobody checks.</summary>
    [Fact]
    public void EveryRatchetEntry_NamesARefusalCompiledAt85()
    {
        var at85 = SuiteManifest.Manifest.Refusals.Values.Where(r => r.RefuseAt.Contains("85"))
            .Select(r => r.Id).ToHashSet(StringComparer.Ordinal);
        Assert.NotEmpty(at85);
        var stale = EditionCitationNet.StaleEntries(at85);
        Assert.True(stale.Count == 0, "ratchet entries whose refusal is not compiled at 85; remove them:\n" + string.Join("\n", stale));
    }

    /// <summary>The ratchet against planted diagnostics: one listed refusal and code, its count taken from the file.</summary>
    [Fact]
    public void Ratchet_FailsAGrownAShrunkAndAZeroedCount()
    {
        // The first entry (in code, then refusal order) whose count is at least 2, so the shrunk arm is asked too, and whose
        // refusal the ratchet lists under no other code, so each planted list is that one code's whole story.
        var entries = EditionCitationNet.Pending.OrderBy(p => p.Key, StringComparer.Ordinal)
            .SelectMany(p => p.Value.OrderBy(c => c.Key, StringComparer.Ordinal).Select(c => (Code: p.Key, Id: c.Key, Count: c.Value)))
            .ToList();
        var (code, id, counted) = entries.First(e => e.Count >= 2 && entries.Count(o => o.Id == e.Id) == 1);
        string line = $"prog.cob(1,8): error {code}: planted (ISO §2023:14.9.37.3 SR7)";
        string[] Lines(int n) => [.. Enumerable.Repeat(line, n)];

        Assert.Empty(EditionCitationNet.RatchetFindings(Lines(counted), id));
        Assert.Contains("a NEW site", Assert.Single(EditionCitationNet.RatchetFindings(Lines(counted + 1), id)));
        Assert.Contains("lower the count", Assert.Single(EditionCitationNet.RatchetFindings(Lines(counted - 1), id)));
        Assert.Contains("remove the entry", Assert.Single(EditionCitationNet.RatchetFindings([], id)));
        // The same code in a refusal the ratchet does not list for it is a new site too.
        Assert.Contains("a NEW site", Assert.Single(EditionCitationNet.RatchetFindings(Lines(1), "no-such-refusal")));
        // A 1985 citation beside it, or an edition gate's words, is no offence.
        Assert.Empty(EditionCitationNet.RatchetFindings(
            [$"prog.cob(1,8): error {code}: planted (ISO §2023:14.9.37.3 SR7; §1985:VI.6.22.3 SR1)"], "no-such-refusal"));
    }
}
