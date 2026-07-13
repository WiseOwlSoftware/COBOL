// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.Json;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Editions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The P2.5 drift check: <c>tests/version-matrix/constructs.json</c> is THE single source, and the two
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
        string DiagnosticCode, string Citation, string? ExpectDiagnostic);

    private static List<JsonRow> LoadRows()
    {
        string path = Path.Combine(RepoRoot(), "tests", "version-matrix", "constructs.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return [.. doc.RootElement.GetProperty("constructs").EnumerateArray().Select(e => new JsonRow(
            e.GetProperty("id").GetString()!,
            e.GetProperty("display").GetString()!,
            e.GetProperty("introducedIn").GetInt32(),
            e.GetProperty("removedIn").ValueKind == JsonValueKind.Null ? null : e.GetProperty("removedIn").GetInt32(),
            e.TryGetProperty("obsoleteIn", out var o) && o.ValueKind != JsonValueKind.Null ? o.GetInt32() : null,
            e.GetProperty("diagnosticCode").GetString()!,
            e.GetProperty("citation").GetString()!,
            e.TryGetProperty("expectDiagnostic", out var d) ? d.GetString() : null))];
    }

    /// <summary>Kebab constructs.json id → the PascalCase identifier the generator emits (must match
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
            // The generated Entries must reproduce EVERY constructs.json field verbatim (staleness guard).
            Assert.True(e.IntroducedIn == r.IntroducedIn && e.RemovedIn == r.RemovedIn && e.ObsoleteIn == r.ObsoleteIn,
                $"drift on {id}: registry ({e.IntroducedIn},{e.RemovedIn?.ToString() ?? "-"},{e.ObsoleteIn?.ToString() ?? "-"}) "
                + $"vs json ({r.IntroducedIn},{r.RemovedIn?.ToString() ?? "-"},{r.ObsoleteIn?.ToString() ?? "-"})");
            Assert.True(e.Display == r.Display, $"Display drift on {id}: '{e.Display}' vs json '{r.Display}'");
            Assert.True(e.DiagnosticCode == r.DiagnosticCode, $"DiagnosticCode drift on {id}: '{e.DiagnosticCode}' vs json '{r.DiagnosticCode}'");
            Assert.True(e.Citation == r.Citation, $"Citation drift on {id}: '{e.Citation}' vs json '{r.Citation}'");
            // Intra-json consistency: the matrix's expectDiagnostic must agree with the registry diagnosticCode
            // for a non-obsolete row (obsolete rows always warn with the fixed 0903 band code).
            if (r.ExpectDiagnostic is { } code && r.ObsoleteIn is null)
                Assert.True(e.DiagnosticCode == code, $"code mismatch on {id}: diagnosticCode {e.DiagnosticCode} vs expectDiagnostic {code}");
        }
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
            Assert.Contains(ed.Diagnostics, d => d.Contains("COBOLNET0900") && d.Contains("COBOL-2023"));
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

    private static string RepoRoot()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !Directory.Exists(Path.Combine(d.FullName, "tests", "version-matrix"))) d = d.Parent;
        return d?.FullName ?? throw new InvalidOperationException("repo root (with tests/version-matrix) not found");
    }
}
