// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;

namespace WiseOwl.Cobol.Tests.Shared;

/// <summary>
/// The newest held edition's traceability inventory, as a VIEW: its rule catalog joined with its store of adjudications
/// (one JSON file per adjudicated rule, named by its rule id), one row per catalog rule (DESIGN-spec-conformance-review.md
/// section 4, kb/Work PB4992).
/// </summary>
/// <remarks>
/// <para>
/// ⛔ ONE VIEW PER LANGUAGE, and this is the C# one; its twin is <c>inventory()</c> in <c>scripts/spec/inventory_view.py</c>.
/// Each fact has one home: a rule's section, kind, ordinal and subject are the catalog's; its adjudication is the store's;
/// its state is computed and stored nowhere (<c>SpecTraceabilityInventoryDriftTests.DerivedState</c> computes it on this
/// side and holds it to the Python view's). A rule nobody has adjudicated has no store file and is a row with empty
/// adjudication. The edition, its catalog, its locator, its store and the fields the store holds are the schema's
/// <c>editions.of</c> (the one home of the editions, branch review R1-1): the newest edition that names a store.
/// </para>
/// <para>
/// What the view and the store REFUSE (a store file of a rule the catalog no longer has, one holding a field no store file
/// holds, one decided against other text, a stray, misnamed or case-colliding file) is the Python view's to say, once: the
/// gate's INVENTORY VIEW audit runs it in every lane, and the drift tests' state parity runs it again. A store file this
/// side cannot join is not read. The rows come back in catalog order, which is promised to no caller: every test keys a
/// row by its rule id.
/// </para>
/// </remarks>
internal static class TraceabilityInventory
{
    /// <summary>A rule of the catalog and its adjudication (empty strings when the store holds none).</summary>
    internal sealed record Row(
        string RuleId, string Section, string Kind, int Ordinal, string Subject,
        string Verdict, string CodeLocation, string TestRef, string Derivation, string Editions, string Notes)
    {
        /// <summary>The adjudicated field <paramref name="name"/> (a name of the schema's <c>adjudicated</c> list): the one
        /// map from a field's name to this row's member, which <see cref="RowOf"/> reads and the drift tests ask.</summary>
        internal string Field(string name) => name switch
        {
            "verdict" => Verdict,
            "code-location" => CodeLocation,
            "test-ref" => TestRef,
            "derivation" => Derivation,
            "editions" => Editions,
            "notes" => Notes,
            _ => throw new InvalidOperationException(
                $"inventory-schema.json names an adjudicated field '{name}' that no inventory row has: add it to "
                + "TraceabilityInventory.Row, Row.Field and RowOf."),
        };
    }

    /// <summary>The newest held edition, as the schema's <c>editions.of</c> states it.</summary>
    internal sealed record Edition(int Year, string Catalog, string Locator, string Store, IReadOnlyList<string> Adjudicated);

    /// <summary>The newest held edition of <c>tests/version-matrix/inventory-schema.json</c>.</summary>
    internal static Edition Newest => s_newest.Value;

    private static readonly Lazy<Edition> s_newest = new(() =>
    {
        using var doc = JsonDocument.Parse(File.ReadAllBytes(TestRepo.VersionMatrix("inventory-schema.json")));
        var held = doc.RootElement.GetProperty("editions").GetProperty("of").EnumerateArray()
            .Where(e => e.TryGetProperty("store", out _)).ToList();
        if (held.Count == 0)
            throw new InvalidDataException("inventory-schema.json editions.of holds no edition with a store");
        var e = held[^1];
        return new Edition(e.GetProperty("year").GetInt32(), Str(e, "catalog"), Str(e, "locator"), Str(e, "store"),
            [.. e.GetProperty("adjudicated").EnumerateArray().Select(f => f.GetString()!)]);
    });

    /// <summary>Every row of the view, joined once per test assembly.</summary>
    internal static IReadOnlyList<Row> Rows => s_rows.Value;

    private static readonly Lazy<IReadOnlyList<Row>> s_rows = new(Load);

    private static string Str(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()!
            : "";

    private static string InTree(string rel) => Path.Combine(TestRepo.Root, rel.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>The row of catalog rule <paramref name="rule"/> with its store record <paramref name="record"/> (an
    /// undefined element when the store holds none), the rule located by <paramref name="locator"/>.</summary>
    internal static Row RowOf(JsonElement rule, JsonElement record, string locator) => new(
        Str(rule, "id"), Str(rule, locator), Str(rule, "kind"), rule.GetProperty("ordinal").GetInt32(), Str(rule, "subject"),
        Str(record, "verdict"), Str(record, "code-location"), Str(record, "test-ref"), Str(record, "derivation"),
        Str(record, "editions"), Str(record, "notes"));

    private static IReadOnlyList<Row> Load()
    {
        var edition = Newest;
        string catalogPath = InTree(edition.Catalog), storePath = InTree(edition.Store);
        if (!File.Exists(catalogPath))
            throw new FileNotFoundException($"rule catalog missing: {catalogPath} — run python scripts/spec/extract_rule_catalog.py");
        var store = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        if (Directory.Exists(storePath))
        {
            foreach (string file in Directory.EnumerateFiles(storePath, "*.json"))
            {
                using var doc = JsonDocument.Parse(File.ReadAllBytes(file));
                store[Path.GetFileNameWithoutExtension(file)] = doc.RootElement.Clone();
            }
        }

        using var catalog = JsonDocument.Parse(File.ReadAllBytes(catalogPath));
        return [.. catalog.RootElement.GetProperty("rules").EnumerateArray()
            .Select(rule => RowOf(rule, store.GetValueOrDefault(Str(rule, "id")), edition.Locator))];
    }
}
