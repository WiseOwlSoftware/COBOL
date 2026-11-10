// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.Text.Json;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ NO MERGE DRIVER, AND ONE FILE PER ENTRY (kb/Work PB5110; owner, 2026-11-10: "No custom merge drivers are required").
/// Two branches that each add an entry to a shared registry must add two FILES, so plain git merges them locally and in
/// GitHub's merge queue alike. This class holds that shape: <c>.gitattributes</c> names no <c>merge=</c> driver, and each
/// registry that is a directory of files (the construct catalogue, the traceability inventory of every held edition, WiseOwl's
/// refusals and compile options) holds exactly one entry per file, named by the entry's own key, so no file is shared by two
/// entries. The work notes' own shape (a note is one file, its addenda are files beside it) is checked by
/// <c>scripts/spec/work.py check</c>.
/// </summary>
public sealed class RegistryShapeDriftTests
{
    [Fact]
    public void GitAttributes_NamesNoMergeDriver()
    {
        var drivers = File.ReadAllLines(TestRepo.At(".gitattributes"))
            .Select((line, n) => (line: line.Trim(), n: n + 1))
            .Where(l => l.line.Length > 0 && !l.line.StartsWith('#') && Regex.IsMatch(l.line, @"(^|\s)merge="))
            .Select(l => $".gitattributes:{l.n}: {l.line}")
            .ToList();
        Assert.True(drivers.Count == 0,
            "a merge= attribute names a custom merge driver, which GitHub's merge queue cannot run (kb/Work PB5110): "
            + string.Join("; ", drivers) + " — make the registry one file per entry instead");
    }

    [Fact]
    public void EveryConstructFile_IsOneConstruct_NamedByItsId()
    {
        var bad = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.ConstructsDir, "*", SearchOption.AllDirectories))
        {
            string stem = Path.GetFileNameWithoutExtension(file);
            if (Path.GetExtension(file) != ".json" || Path.GetDirectoryName(file) != Path.GetFullPath(TestRepo.ConstructsDir)) { bad.Add($"{Path.GetFileName(file)}: not a <id>.json file at the top of the catalogue"); continue; }
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            if (doc.RootElement.ValueKind != JsonValueKind.Object || !doc.RootElement.TryGetProperty("id", out var id) || id.GetString() != stem)
                bad.Add($"{stem}.json: not one construct object whose id is the file's name");
        }
        Assert.True(bad.Count == 0, "the construct catalogue is one file per construct: " + string.Join("; ", bad));
        Assert.True(TestRepo.ConstructRows().Count >= 300, "the catalogue lost its rows");
    }

    [Theory]
    [InlineData("inventory")]
    [InlineData("inventory-1985")]
    public void EveryInventoryFile_IsOneRule_NamedByItsRuleId(string directory)
    {
        string root = TestRepo.VersionMatrix(directory);
        if (!Directory.Exists(root)) return;   // an edition's store starts with no directory at all (inventory_store.py)
        var bad = new List<string>();
        foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            string stem = Path.GetFileNameWithoutExtension(file);
            if (Path.GetExtension(file) != ".json" || Path.GetDirectoryName(file) != Path.GetFullPath(root)) { bad.Add($"{Path.GetFileName(file)}: not a <rule-id>.json file"); continue; }
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            if (doc.RootElement.ValueKind != JsonValueKind.Object || !doc.RootElement.TryGetProperty("rule-id", out var id) || id.GetString() != stem)
                bad.Add($"{stem}.json: not one rule object whose rule-id is the file's name");
        }
        Assert.True(bad.Count == 0, $"{directory}/ is one file per rule: " + string.Join("; ", bad));
    }

    [Theory]
    [InlineData("diagnostics")]
    [InlineData("options")]
    public void EverySuiteSidecar_IsOneObject_AndNoTwoNamesDifferOnlyByCase(string directory)
    {
        string root = TestRepo.Suite("wiseowl", directory);
        var bad = new List<string>();
        var seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.TopDirectoryOnly))
        {
            string name = Path.GetFileName(file);
            if (!name.EndsWith(".json", StringComparison.Ordinal)) { bad.Add($"{name}: not a .json file"); continue; }
            if (seen.TryGetValue(name, out var other)) bad.Add($"{name} and {other} differ only by case (one file on a case-insensitive checkout)");
            else seen[name] = name;
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            if (doc.RootElement.ValueKind != JsonValueKind.Object) bad.Add($"{name}: not one object (a sidecar is one entry)");
        }
        Assert.True(bad.Count == 0, $"wiseowl/{directory}/ is one file per entry: " + string.Join("; ", bad));
    }
}
