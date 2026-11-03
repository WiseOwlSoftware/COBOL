// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The drift guard that bolts the BEHAVIOUR register (<see cref="DialectBehaviors"/>, the runtime's table of
/// named per-edition behaviour changes) to the ledger that owns the obligation
/// (<c>docs/VERSION_CHANGE_REFERENCE.md</c> Table 1, whose gating action for such a row is
/// <c>gate-behavior-by-dialect</c>) — kb/Work PB344, PB4453.
///
/// <para>⛔ WHY A SECOND ANCHOR KIND EXISTS. A VCR row's <c>&lt;!-- gate:id --&gt;</c> anchor resolves to a
/// <c>tests/version-matrix/constructs.json</c> row, and that register answers "does this edition HAVE this
/// construct?" — every row of it carries a DIAGNOSTIC code, because its answer is a rejection. A behaviour
/// change has no diagnostic and cannot have one: the source is legal at every edition and only the ANSWER
/// differs, so it can never be a <c>constructs.json</c> row. Those rows sat at <c>&lt;!-- todo --&gt;</c> —
/// indistinguishable from "nobody has done the work" — while the compiler shipped the 2023 rule to
/// <c>--std 85/2002/2014</c>. The <c>&lt;!-- behavior:id --&gt;</c> anchor resolves HERE instead.</para>
///
/// <para>⛔ THE TWO HALVES (kb/Work PB4453: no compiler test opens a Markdown file). THIS CLASS holds the registry's own shape
/// and holds <c>tests/doc-facts/compiler-facts.json</c>'s <c>dialectBehaviors</c> equal to it; the VCR's anchors are held to
/// that file in both directions by <c>scripts/docchecks/facts.py</c> (<c>behavior_findings</c>): a member with no row, a row
/// whose anchor names no member, or a landed gate still marked todo fails the DOC AUDITS gate audit.</para>
/// </summary>
public sealed class DialectBehaviorRegistryDriftTests
{
    [Fact]
    public void EveryBehavior_HasItsOwnRow_WithACompleteShape()
    {
        var behaviors = Enum.GetValues<DialectBehavior>();
        Assert.Equal(behaviors.Length, DialectBehaviors.All.Length);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var b in behaviors)
        {
            var row = DialectBehaviors.Of(b);
            Assert.Equal(b, row.Behavior);
            Assert.True(ids.Add(row.Id), $"duplicate DialectBehaviors id '{row.Id}'");
            Assert.Matches("^[a-z0-9]+(-[a-z0-9]+)*$", row.Id);
            Assert.Contains(row.ChangedIn, new[] { 2002, 2014, 2023 });
            Assert.Matches("^[0-9]+([.][0-9]+)?$", row.VcrRow);
            Assert.False(string.IsNullOrWhiteSpace(row.SpecRef), $"{b}: no normative clause");
            Assert.False(string.IsNullOrWhiteSpace(row.AnnexRef), $"{b}: no Annex E item or Annex A.4 module");
            Assert.False(string.IsNullOrWhiteSpace(row.Title), $"{b}: no title");
        }
    }

    /// <summary>The registry's id and VCR row of every behavior are what the facts file holds, which the audit holds the
    /// VCR's <c>&lt;!-- behavior:id --&gt;</c> anchors to (kb/Work PB4453).</summary>
    [Fact]
    public void EveryBehavior_IsAnchoredOnItsVersionChangeReferenceRow()
    {
        var facts = DocFacts.Load("compiler-facts.json").GetProperty("dialectBehaviors").EnumerateArray()
            .ToDictionary(e => e.GetProperty("id").GetString()!, e => e.GetProperty("vcrRow").GetString()!, StringComparer.Ordinal);
        var registry = DialectBehaviors.All.ToArray().ToDictionary(r => r.Id, r => r.VcrRow, StringComparer.Ordinal);
        Assert.Equal(registry.OrderBy(kv => kv.Key, StringComparer.Ordinal), facts.OrderBy(kv => kv.Key, StringComparer.Ordinal));
    }

    [Fact]
    public void IsActive_IsTheEditionWindowTheRowDeclares()
    {
        foreach (var row in DialectBehaviors.All.ToArray())
            foreach (int edition in new[] { 85, 2002, 2014, 2023 })
                Assert.Equal(edition >= row.ChangedIn, DialectBehaviors.IsActive(row.Behavior, edition));
    }
}
