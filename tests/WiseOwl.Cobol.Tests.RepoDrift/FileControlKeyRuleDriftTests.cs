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
/// The REPOSITORY half of the FILE-CONTROL key-rule guard (kb/Work PB5230): the table of screened rules
/// (<c>FileControlKeyRules.Catalog</c>) against the traceability inventory, a record that changes with every records
/// batch, so this method reads outside the compiler unit tests' declared inputs and runs here. The compiler half (the
/// table against the standard's text) is Unit's <c>FileControlKeyRuleDriftTests</c>.
/// </summary>
public sealed class FileControlKeyRuleDriftTests
{
    /// <summary>⭐ THE "NEXT RULE IS A ROW" CLAMP, in both directions.
    /// <list type="bullet">
    /// <item>A rule the inventory says is screened HERE must have a row — otherwise it is enforced somewhere else,
    /// which is the shape PB699 was.</item>
    /// <item>A row's rule-id must be a REAL inventory rule-id, and if that row has already been credited to a
    /// code-location, that location must be this file — a row claiming an id the inventory files against a
    /// different site is a claim on the burn-down the code does not back.</item>
    /// </list>
    /// A table row whose inventory row carries NO code-location yet is legal and deliberate: a rule may be
    /// screened before it has earned a verdict (the two SR2s are screened only in their within-a-record arm, so
    /// neither CONFORMS nor a non-resolving promise would be true of them — see the class's "NOT HERE"
    /// paragraph). Demanding a verdict here would push the register into recording one it has not earned.</summary>
    [Fact]
    public void EveryScreenedRuleInTheInventory_IsARowInTheTable()
    {
        var claimed = new SortedSet<string>(StringComparer.Ordinal);       // credited to THIS file
        var locatedElsewhere = new Dictionary<string, string>(StringComparer.Ordinal);
        var allIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in TraceabilityInventory.Rows)
        {
            string id = row.RuleId;
            allIds.Add(id);
            string where = row.CodeLocation;
            if (where.Contains("FileControlKeyRules", StringComparison.Ordinal)) claimed.Add(id);
            else if (where.Length > 0) locatedElsewhere[id] = where;
        }
        var inTable = new SortedSet<string>(FileControlKeyRules.Catalog.Select(r => r.RuleId), StringComparer.Ordinal)
        {
            // The RequiredClauses table is the implementation of the general format's unbracketed members, which no
            // numbered rule states: its inventory row is the format row (kb/Work PB4236).
            "FMT-12.4.5.1",
        };

        Assert.True(claimed.Count > 0,
            "no inventory row points at FileControlKeyRules — either the batch was never applied or the screen moved.");
        Assert.Empty(claimed.Except(inTable));                             // screened here per the register, no row
        foreach (string id in inTable)
        {
            Assert.True(allIds.Contains(id), $"row '{id}' is not a rule-id the traceability inventory knows.");
            Assert.False(locatedElsewhere.TryGetValue(id, out string? where),
                $"row '{id}' is screened here, but the inventory credits it to '{where}'.");
        }
    }
}
