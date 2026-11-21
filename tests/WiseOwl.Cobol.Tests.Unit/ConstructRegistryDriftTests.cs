// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.Json;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The P2.5 drift check: <c>tests/version-matrix/constructs/</c> is THE single source, and the two
/// generated build-outputs (<c>ConstructRegistry.g.cs</c> / <c>Constructs.g.cs</c>, emitted by
/// <c>scripts/gen-constructs.ps1</c>) must be IN SYNC with it — so an edit to the json without a regen is a
/// CI failure, never a silent stale build. This mirrors the ReservedWordsDriftTests discipline. (The former
/// third output — the <c>GateId</c> intro-gate enum — was DELETED at Exec Step E: its P2.7 forward-stamping
/// consumer was abandoned, DEVLOG 679, and nothing in production consumed it.)
/// </summary>
public sealed class ConstructRegistryDriftTests
{
    private sealed record JsonRow(
        string Id, string Display, int IntroducedIn, int? RemovedIn, int? ObsoleteIn,
        string DiagnosticCode, string Citation, string? ExpectDiagnostic, string? Rule1985);

    private static List<JsonRow> LoadRows()
    {
        return [.. TestRepo.ConstructRows().Select(e => new JsonRow(
            e.GetProperty("id").GetString()!,
            e.GetProperty("display").GetString()!,
            e.GetProperty("introducedIn").GetInt32(),
            e.GetProperty("removedIn").ValueKind == JsonValueKind.Null ? null : e.GetProperty("removedIn").GetInt32(),
            e.TryGetProperty("obsoleteIn", out var o) && o.ValueKind != JsonValueKind.Null ? o.GetInt32() : null,
            e.GetProperty("diagnosticCode").GetString()!,
            e.GetProperty("citation").GetString()!,
            e.TryGetProperty("expectDiagnostic", out var d) ? d.GetString() : null,
            e.TryGetProperty("rule1985", out var r85) ? r85.GetString() : null))];
    }

    /// <summary>Kebab the construct catalogue id → the PascalCase identifier the generator emits (must match
    /// <c>To-Pascal</c> in scripts/gen-constructs.ps1).</summary>
    private static string Pascal(string id) =>
        string.Concat(id.Split('-').Select(s => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..]));

    [Fact]
    public void GeneratedEntries_Match_CanonicalJson_BothDirections()
    {
        var rows = LoadRows().ToDictionary(r => r.Id, StringComparer.Ordinal);
        var reg = ConstructRegistry.Entries.ToDictionary(e => e.Id, StringComparer.Ordinal);

        var jsonOnly = rows.Keys.Where(k => !reg.ContainsKey(k)).ToList();
        var regOnly = reg.Keys.Where(k => !rows.ContainsKey(k)).ToList();
        Assert.True(jsonOnly.Count == 0 && regOnly.Count == 0,
            $"drift: json-only [{string.Join(", ", jsonOnly)}] registry-only [{string.Join(", ", regOnly)}] — regenerate scripts/gen-constructs.ps1");

        foreach (var (id, r) in rows)
        {
            var e = reg[id];
            // The generated Entries must reproduce EVERY construct catalogue field verbatim (staleness guard).
            Assert.True(e.IntroducedIn == r.IntroducedIn && e.RemovedIn == r.RemovedIn && e.ObsoleteIn == r.ObsoleteIn,
                $"drift on {id}: registry ({e.IntroducedIn},{e.RemovedIn?.ToString() ?? "-"},{e.ObsoleteIn?.ToString() ?? "-"}) "
                + $"vs json ({r.IntroducedIn},{r.RemovedIn?.ToString() ?? "-"},{r.ObsoleteIn?.ToString() ?? "-"})");
            Assert.True(e.Display == r.Display, $"Display drift on {id}: '{e.Display}' vs json '{r.Display}'");
            Assert.True(e.DiagnosticCode == r.DiagnosticCode, $"DiagnosticCode drift on {id}: '{e.DiagnosticCode}' vs json '{r.DiagnosticCode}'");
            Assert.True(e.Citation == r.Citation, $"Citation drift on {id}: '{e.Citation}' vs json '{r.Citation}'");
            Assert.True(e.Rule1985 == r.Rule1985, $"Rule1985 drift on {id}: '{e.Rule1985}' vs json '{r.Rule1985}'");
            // Intra-json consistency: the matrix's expectDiagnostic must agree with the registry diagnosticCode
            // for a row whose code is not the fixed 0903 band code: an obsolete row that is never removed only warns with 0903,
            // while a row that is also REMOVED keeps its removal code in both fields.
            if (r.ExpectDiagnostic is { } code && (r.ObsoleteIn is null || r.RemovedIn is not null))
                Assert.True(e.DiagnosticCode == code, $"code mismatch on {id}: diagnosticCode {e.DiagnosticCode} vs expectDiagnostic {code}");
        }
    }

    /// <summary>⛔ A RELAXATION ROW'S 85 REFUSAL CITES THE 1985 RULE (kb/Work PB4950, PB6010). A row whose catalogue text says it
    /// relaxes a COBOL-85 restriction (its <c>vcr</c> or <c>description</c> names a relaxation or a restriction, or it is the areas
    /// row 7.35) is refused at 85 for breaking a rule the 1985 text HAS, so the edition gate's message there must cite that rule:
    /// through <c>rule1985</c> (<see cref="ConstructDialectStatus.Rule1985"/>) or a §1985 citation already in <c>citation</c>. The
    /// conformance net (<c>EditionCitationNet</c>) lets every edition gate through on its "(targeting COBOL-85)" words, which is how
    /// COBOL0900/3734/3735 cited only §2023 at 85; this is the check that names the row. And a <c>rule1985</c> is a 1985 citation
    /// on a row refused at 85, and from 2002 the message cites the row's own <c>citation</c> unchanged.</summary>
    [Fact]
    public void EveryRelaxationRow_CitesIts1985RuleAt85()
    {
        var relaxation = new System.Text.RegularExpressions.Regex(@"relaxation|restriction|row 7\.35\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var missing = new List<string>();
        int asked = 0;
        foreach (var row in TestRepo.ConstructRows())
        {
            string id = row.GetProperty("id").GetString()!;
            var e = ConstructRegistry.Find(id)!;
            if (e.Rule1985 is { } r)
            {
                Assert.True(r.StartsWith("§1985:", StringComparison.Ordinal), $"{id}: rule1985 '{r}' is not a §1985 citation");
                Assert.True(e.StatusAt(85) == ConstructAvailability.NotYetIntroduced, $"{id}: rule1985 on a row 85 does not refuse");
            }
            string text = (row.TryGetProperty("vcr", out var v) ? v.GetString() : "") + " "
                + (row.TryGetProperty("description", out var d) ? d.GetString() : "");
            if (e.StatusAt(85) != ConstructAvailability.NotYetIntroduced || !relaxation.IsMatch(text)) continue;
            asked++;
            var at85 = new EditionContext(85);
            ConstructRegistry.Check(at85.Edition, at85, id, "the probe");
            string message = Assert.Single(at85.Diagnostics, m => m.Contains(e.DiagnosticCode, StringComparison.Ordinal)
                || m.Contains("COBOL09", StringComparison.Ordinal));
            if (!message.Contains("§1985:", StringComparison.Ordinal)) missing.Add($"{id}: {message}");
            var at2002 = new EditionContext(2002);
            ConstructRegistry.Check(at2002.Edition, at2002, id, "the probe");
            if (e.Rule1985 is { } own && e.StatusAt(2002) == ConstructAvailability.NotYetIntroduced)
                Assert.DoesNotContain(at2002.Diagnostics, m => m.Contains(own, StringComparison.Ordinal));
        }
        Assert.True(asked > 0, "no relaxation row was asked: the classifier matched nothing");
        Assert.True(missing.Count == 0, "relaxation rows whose 85 refusal cites no 1985 rule (write the rule the 85 program breaks "
            + "as the row's rule1985, then scripts/gen-constructs.ps1):\n  " + string.Join("\n  ", missing));
    }

    [Fact]
    public void Constructs_Consts_CoverEveryEntry_NoOrphans()
    {
        var consts = typeof(Constructs).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .ToDictionary(f => f.Name, f => (string)f.GetRawConstantValue()!, StringComparer.Ordinal);
        foreach (var e in ConstructRegistry.Entries)
        {
            string p = Pascal(e.Id);
            Assert.True(consts.TryGetValue(p, out var v) && v == e.Id,
                $"Constructs.{p} missing/wrong for '{e.Id}' — regenerate scripts/gen-constructs.ps1");
        }
        Assert.Equal(ConstructRegistry.Entries.Count, consts.Count);   // no orphan consts
    }

    [Fact]
    public void Check_RoutesVerdicts_OntoTheChannels()
    {
        // NotYetIntroduced → error on BOTH axes (introduction gating is permissive-independent). The
        // EditionContext IS the IDiagnosticSink; its EditionInfo is the targeted edition (P2.4 sink-based Check).
        foreach (bool permissive in new[] { false, true })
        {
            var ed = new EditionContext(85, permissive);
            ConstructRegistry.Check(ed.Edition, ed, "delete-file-2023", "statement in paragraph M");
            Assert.True(ed.HasErrors, $"introduction gate must fail (permissive={permissive})");
            Assert.Contains(ed.Diagnostics, d => d.Contains("COBOL0900") && d.Contains("COBOL-2023"));
        }

        // Available → silent.
        var ok = new EditionContext(2023);
        ConstructRegistry.Check(ok.Edition, ok, "allocate-2002", "statement");
        Assert.False(ok.HasErrors);
        Assert.Empty(ok.Warnings);

        // Unregistered id → programming error, loud.
        var bad = new EditionContext(2023);
        Assert.Throws<ArgumentException>(() => ConstructRegistry.Check(bad.Edition, bad, "no-such-id", "x"));
    }
}
