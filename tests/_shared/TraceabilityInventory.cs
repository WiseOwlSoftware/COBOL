// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;

namespace CobolNet.Tests.Shared;

/// <summary>
/// The P14 traceability inventory and the verdict vocabulary that reads it — for the gates that need a row's
/// VERDICT and nothing else.
/// </summary>
/// <remarks>
/// <para>
/// ⛔ "DEFECTIVE" AND "RESOLVING" ARE DERIVED, NEVER LISTED. A verdict resolves when
/// <c>tests/version-matrix/inventory-schema.json</c> says its <c>resolves</c> flag is true, so a verdict added to
/// the schema extends every gate built on this with no edit here — the one-rule-one-place discipline the schema
/// itself is built on (<c>feedback_one_rule_one_place</c>). A hand-written list of verdict names in a test would
/// be a second copy of the vocabulary, and the first one to go stale would do so silently, in the direction of
/// passing.
/// </para>
/// <para>
/// ⚠ SCOPE: rule-id and verdict. The gates that judge a row's <c>code-location</c>, <c>test-ref</c>,
/// <c>derivation</c> or computed <c>state</c> read the file themselves, because they need the whole row and its
/// schema — see <c>SpecTraceabilityInventoryDriftTests</c>. This is the shared half, not a general model.
/// </para>
/// </remarks>
internal static class TraceabilityInventory
{
    /// <summary>
    /// One inventory row, reduced to the facts a coverage gate reads. <paramref name="State"/> is the
    /// schema's COMPUTED disposition — <c>OK</c> or <c>GAP</c> — and it is what the burn-down counts.
    /// It is strictly stronger than the verdict: a CONFORMS row whose covering evidence is missing still
    /// reads GAP, so "the verdict resolves" and "the row is closed" are not the same question.
    /// </summary>
    internal sealed record Row(string RuleId, string Verdict, string State)
    {
        /// <summary>The row no longer counts toward the GAP.</summary>
        public bool IsClosed => State == "OK";
    }

    /// <summary>Every row of <c>tests/version-matrix/traceability-inventory.json</c>.</summary>
    public static List<Row> Rows()
    {
        string path = TestRepo.VersionMatrix("traceability-inventory.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"inventory missing: {path} — run python scripts/spec/build_inventory.py", path);
        }
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return [.. doc.RootElement.EnumerateArray().Select(e => new Row(
            e.GetProperty("rule-id").GetString()!,
            e.TryGetProperty("verdict", out var v) ? v.GetString() ?? "" : "",
            e.TryGetProperty("state", out var s) ? s.GetString() ?? "" : ""))];
    }

    /// <summary>
    /// The verdicts the schema marks as NOT resolving — the ones that leave a rule outstanding. ⚠ An EMPTY
    /// verdict is not among them and is not settled either: a row nobody has adjudicated is neither, and
    /// treating the two as one is how a GAP row can be read as closed. The complement is deliberately NOT
    /// offered here — "this row is closed" is <see cref="Row.IsClosed"/>, the schema's own computed state,
    /// because a resolving verdict whose required evidence is missing still leaves the row in the GAP.
    /// </summary>
    public static HashSet<string> DefectiveVerdicts() => VerdictsWhere(resolves: false);

    private static HashSet<string> VerdictsWhere(bool resolves)
    {
        string path = TestRepo.VersionMatrix("inventory-schema.json");
        if (!File.Exists(path)) throw new FileNotFoundException($"inventory schema missing: {path}", path);
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var set = doc.RootElement.GetProperty("verdicts").EnumerateObject()
            .Where(p => p.Value.GetProperty("resolves").GetBoolean() == resolves)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.Ordinal);
        if (set.Count == 0)
        {
            throw new InvalidOperationException(
                $"inventory-schema.json defines no verdict with resolves={resolves.ToString().ToLowerInvariant()}"
                + " — a gate built on it would then be vacuous, so the schema, not the gate, is what changed.");
        }
        return set;
    }
}
