// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.RepoDrift;

/// <summary>
/// The REPOSITORY half of the RECORD-clause rule guard (kb/Work PB5230): the table of screened rules against the
/// traceability inventory, a record that changes with every records batch, so this method reads outside the compiler
/// unit tests' declared inputs and runs here. The compiler half (the table against the standard's text) is Unit's
/// <c>RecordClauseRuleDriftTests</c>.
/// </summary>
public sealed class RecordClauseRuleDriftTests
{
    /// <summary>⭐ THE "NEXT RULE IS A ROW" CLAMP, in both directions: a rule the traceability inventory credits
    /// to this file must have a row, and a row's rule-id must be a real inventory id that the inventory does not
    /// credit somewhere else. A row claiming an id filed against a different site is a claim on the burn-down
    /// that the code does not back.</summary>
    [Fact]
    public void EveryScreenedRuleInTheInventory_IsARowInTheTable()
    {
        var claimed = new SortedSet<string>(StringComparer.Ordinal);
        var locatedElsewhere = new Dictionary<string, string>(StringComparer.Ordinal);
        var allIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in TraceabilityInventory.Rows)
        {
            string id = row.RuleId;
            allIds.Add(id);
            string where = row.CodeLocation;
            if (where.Contains("RecordClauseRules", StringComparison.Ordinal)) claimed.Add(id);
            else if (where.Length > 0) locatedElsewhere[id] = where;
        }
        var inTable = new SortedSet<string>(
            RecordClauseRules.Catalog.Select(r => r.RuleId), StringComparer.Ordinal);

        Assert.True(claimed.Count > 0,
            "no inventory row points at RecordClauseRules — either the batch was never applied or the screen moved.");
        Assert.Empty(claimed.Except(inTable));
        foreach (string id in inTable)
        {
            Assert.True(allIds.Contains(id), $"row '{id}' is not a rule-id the traceability inventory knows.");
            Assert.False(locatedElsewhere.TryGetValue(id, out string? where),
                $"row '{id}' is screened here, but the inventory credits it to '{where}'.");
        }
    }
}
