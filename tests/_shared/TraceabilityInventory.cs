// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;

namespace WiseOwl.Cobol.Tests.Shared;

/// <summary>
/// The 2023 traceability inventory, as a VIEW: the rule catalog joined with the store of adjudications
/// (<c>tests/version-matrix/inventory/</c>, one JSON file per adjudicated rule, named by its rule id), one row per catalog
/// rule (DESIGN-spec-conformance-review.md section 4, kb/Work PB4992).
/// </summary>
/// <remarks>
/// <para>
/// ⛔ ONE VIEW PER LANGUAGE, and this is the C# one; its twin is <c>inventory()</c> in <c>scripts/spec/inventory_view.py</c>.
/// Each fact has one home: a rule's section, kind, ordinal and subject are the catalog's; its adjudication is the store's;
/// its state is computed and stored nowhere (<c>SpecTraceabilityInventoryDriftTests.DerivedState</c> computes it on this
/// side and holds it to the Python view's). A rule nobody has adjudicated has no store file and is a row with empty
/// adjudication.
/// </para>
/// <para>
/// What the view REFUSES (a store file of a rule the catalog no longer has, one holding a field no store file holds, one
/// decided against other text) is the Python view's to say, once: the gate's INVENTORY VIEW audit runs it in every lane,
/// and the drift tests' state parity runs it again. A store file this side cannot join is not read. The rows come back in
/// catalog order, which is promised to no caller: every test keys a row by its rule id.
/// </para>
/// </remarks>
internal static class TraceabilityInventory
{
    /// <summary>A rule of the 2023 catalog and its adjudication (empty strings when the store holds none).</summary>
    internal sealed record Row(
        string RuleId, string Section, string Kind, int Ordinal, string Subject,
        string Verdict, string CodeLocation, string TestRef, string Derivation, string Editions, string Notes);

    /// <summary><c>tests/version-matrix/inventory/</c>: the 2023 store.</summary>
    internal static string StorePath => TestRepo.VersionMatrix("inventory");

    /// <summary><c>docs/rearchitecture/spec-rule-catalog.json</c>: the 2023 rule catalog.</summary>
    internal static string CatalogPath => TestRepo.Docs("rearchitecture", "spec-rule-catalog.json");

    /// <summary>Every row of the view, joined once per test assembly.</summary>
    internal static IReadOnlyList<Row> Rows => s_rows.Value;

    private static readonly Lazy<IReadOnlyList<Row>> s_rows = new(Load);

    private static string Str(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()!
            : "";

    private static IReadOnlyList<Row> Load()
    {
        if (!File.Exists(CatalogPath))
            throw new FileNotFoundException($"rule catalog missing: {CatalogPath} — run python scripts/spec/extract_rule_catalog.py");
        var store = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        if (Directory.Exists(StorePath))
        {
            foreach (string file in Directory.EnumerateFiles(StorePath, "*.json"))
            {
                using var doc = JsonDocument.Parse(File.ReadAllBytes(file));
                store[Path.GetFileNameWithoutExtension(file)] = doc.RootElement.Clone();
            }
        }

        using var catalog = JsonDocument.Parse(File.ReadAllBytes(CatalogPath));
        var rows = new List<Row>();
        foreach (var rule in catalog.RootElement.GetProperty("rules").EnumerateArray())
        {
            string id = Str(rule, "id");
            var a = store.GetValueOrDefault(id);
            rows.Add(new Row(
                id, Str(rule, "section"), Str(rule, "kind"), rule.GetProperty("ordinal").GetInt32(), Str(rule, "subject"),
                Str(a, "verdict"), Str(a, "code-location"), Str(a, "test-ref"), Str(a, "derivation"), Str(a, "editions"),
                Str(a, "notes")));
        }

        return rows;
    }
}
