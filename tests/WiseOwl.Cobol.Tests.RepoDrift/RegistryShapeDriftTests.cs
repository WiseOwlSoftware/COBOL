// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.Text.Json;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.RepoDrift;

/// <summary>
/// The traceability inventory's store shape: one file per rule, named by its rule-id (kb/Work PB5110). The REPOSITORY
/// half of the registry-shape guard (kb/Work PB5230): the inventory is a record, outside the compiler unit tests'
/// declared inputs; the construct catalogue's and the suite sidecars' shape stay in Unit's <c>RegistryShapeDriftTests</c>.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class RegistryShapeDriftTests
{
    [Theory]
    [InlineData("inventory")]
    [InlineData("inventory-1985")]
    public void EveryInventoryFile_IsOneRule_NamedByItsRuleId(string directory)
    {
        string root = TestRepo.VersionMatrix(directory);
        if (!Directory.Exists(root)) return;   // an edition's store starts with no directory at all (inventory_store.py)
        var bad = new List<string>();
        foreach (string dir in Directory.EnumerateDirectories(root)) bad.Add($"{Path.GetFileName(dir)}/: a directory in the store");
        foreach (string file in Directory.EnumerateFiles(root))
        {
            string stem = Path.GetFileNameWithoutExtension(file);
            if (Path.GetExtension(file) != ".json") { bad.Add($"{Path.GetFileName(file)}: not a <rule-id>.json file"); continue; }
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            if (doc.RootElement.ValueKind != JsonValueKind.Object || !doc.RootElement.TryGetProperty("rule-id", out var id) || id.GetString() != stem)
                bad.Add($"{stem}.json: not one rule object whose rule-id is the file's name");
        }
        Assert.True(bad.Count == 0, $"{directory}/ is one file per rule: " + string.Join("; ", bad));
    }
}
