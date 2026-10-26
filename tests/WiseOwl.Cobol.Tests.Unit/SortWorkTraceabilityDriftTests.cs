// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EVERY SORT/MERGE GENERAL RULE THAT SortWork.cs CITES IS TRACED TO SortWork.cs (kb/Work PB3339, branch review
/// w1111c R1-1). When the external merge sort replaced the in-memory sort store, the ordering obligations moved out of
/// <c>CobolSort.Sort</c> / <c>CobolSort.Merge</c>, which became one-line calls into <c>SortWork.Sequence</c>. The
/// traceability inventory rows for those rules kept naming the old members, with notes about a record list and an
/// index sort that no longer existed, and no row named SortWork.cs. A verdict is re-verified by reading the code its
/// row names, so a row that names only the delegator leaves the rule's real code unchecked. The rules are the ones
/// SortWork.cs cites itself (<c>§14.9.24.4 GRn</c>, <c>§14.9.40.4 GRn</c>), so a rule the file takes on later is
/// covered without editing this test.
/// </summary>
public sealed class SortWorkTraceabilityDriftTests
{
    private const string SortWorkPath = "src/WiseOwl.Cobol.Runtime/IO/SortWork.cs";

    [Fact]
    public void EveryStatementRuleSortWorkCites_IsARowThatNamesSortWork()
    {
        string src = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Runtime", "IO", "SortWork.cs"));
        var cited = Regex.Matches(src, @"§(14\.9\.(?:24|40)\.4) GR(\d+)")
            .Select(m => $"GR-{m.Groups[1].Value}-{m.Groups[2].Value}")
            .ToHashSet(StringComparer.Ordinal);
        // Non-vacuity: the two tie-break rules the store exists to keep (MERGE GR4, SORT GR3).
        Assert.Contains("GR-14.9.24.4-4", cited);
        Assert.Contains("GR-14.9.40.4-3", cited);

        using var doc = JsonDocument.Parse(File.ReadAllText(TestRepo.VersionMatrix("traceability-inventory.json")));
        var locations = doc.RootElement.EnumerateArray().ToDictionary(
            r => r.GetProperty("rule-id").GetString()!,
            r => r.TryGetProperty("code-location", out var c) ? c.GetString() ?? "" : "",
            StringComparer.Ordinal);

        var untraced = cited.Order(StringComparer.Ordinal)
            .Where(id => !locations.TryGetValue(id, out var loc)
                || !loc.Split(';').Any(w => w.Trim().StartsWith(SortWorkPath + "#", StringComparison.Ordinal)))
            .ToList();
        Assert.True(untraced.Count == 0,
            $"SortWork.cs cites {string.Join(", ", untraced)}, but the inventory row of each names no {SortWorkPath}#<member> "
            + "in its code-location: record the member that carries the rule (scripts/spec/record_verdicts.py) and "
            + "restate the row's notes for the code it now names (kb/Work PB3339)");
    }
}
