// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;

namespace WiseOwl.Cobol.Tests.Shared;

/// <summary>
/// The traceability inventory, read: one JSON file per row under <c>tests/version-matrix/inventory/</c>, named by the row's
/// rule id (kb/Work PB4148).
/// </summary>
/// <remarks>
/// <para>
/// ⛔ ONE READER PER LANGUAGE, and this is the C# one; its twin is <c>load_inventory</c> in
/// <c>scripts/spec/inventory_store.py</c>. Six drift tests read the inventory, each once from a single 7.6 MB file with
/// its own <c>File.ReadAllText</c> and <c>JsonDocument.Parse</c>; the rows are now 4,214 files, so the read belongs in one
/// place that parses them ONCE per test assembly and hands every test the same elements.
/// </para>
/// <para>
/// The rows come back ordered by file name (the rule id), which is not the spec's order and is promised to no caller:
/// every test keys a row by its <c>rule-id</c>. A file that is not <c>&lt;rule-id&gt;.json</c> is not this type's to
/// judge; <c>SpecTraceabilityInventoryDriftTests.EveryRow_IsItsOwnFile_AndTheDirectoryHoldsNothingElse</c> holds it.
/// </para>
/// </remarks>
internal static class TraceabilityInventory
{
    /// <summary><c>tests/version-matrix/inventory/</c>.</summary>
    internal static string DirectoryPath => TestRepo.VersionMatrix("inventory");

    /// <summary>Every row, parsed once per test assembly. The elements outlive their documents (<see cref="JsonElement.Clone"/>).</summary>
    internal static IReadOnlyList<JsonElement> Rows => s_rows.Value;

    private static readonly Lazy<IReadOnlyList<JsonElement>> s_rows = new(Load);

    private static IReadOnlyList<JsonElement> Load()
    {
        if (!Directory.Exists(DirectoryPath))
            throw new DirectoryNotFoundException(
                $"inventory missing: {DirectoryPath} — run python scripts/spec/build_inventory.py");
        var files = Directory.EnumerateFiles(DirectoryPath, "*.json").Order(StringComparer.Ordinal).ToList();
        var rows = new List<JsonElement>(files.Count);
        foreach (string file in files)
        {
            using var doc = JsonDocument.Parse(File.ReadAllBytes(file));
            rows.Add(doc.RootElement.Clone());
        }

        return rows;
    }
}
