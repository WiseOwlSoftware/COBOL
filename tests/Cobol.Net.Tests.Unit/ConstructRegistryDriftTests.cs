// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using CobolNet.Binding;
using CobolNet.Editions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The P2.5 drift check: the in-code <see cref="ConstructRegistry"/> and the canonical
/// <c>tests/version-matrix/constructs.json</c> must agree BOTH directions — every json row has a registry
/// entry with identical edition metadata, and every registry entry has its json row. This is the discipline
/// that makes a gate unable to land without its matrix row (and vice versa).
/// </summary>
public sealed class ConstructRegistryDriftTests
{
    private sealed record JsonRow(string Id, int IntroducedIn, int? RemovedIn, string? ExpectDiagnostic, int? ObsoleteIn);

    private static List<JsonRow> LoadRows()
    {
        string path = Path.Combine(RepoRoot(), "tests", "version-matrix", "constructs.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return [.. doc.RootElement.GetProperty("constructs").EnumerateArray().Select(e => new JsonRow(
            e.GetProperty("id").GetString()!,
            e.GetProperty("introducedIn").GetInt32(),
            e.GetProperty("removedIn").ValueKind == JsonValueKind.Null ? null : e.GetProperty("removedIn").GetInt32(),
            e.TryGetProperty("expectDiagnostic", out var d) ? d.GetString() : null,
            e.TryGetProperty("obsoleteIn", out var o) && o.ValueKind != JsonValueKind.Null ? o.GetInt32() : null))];
    }

    [Fact]
    public void Registry_Matches_CanonicalJson_BothDirections()
    {
        var rows = LoadRows().ToDictionary(r => r.Id, StringComparer.Ordinal);
        var reg = ConstructRegistry.Entries.ToDictionary(e => e.Id, StringComparer.Ordinal);

        var jsonOnly = rows.Keys.Where(k => !reg.ContainsKey(k)).ToList();
        var regOnly = reg.Keys.Where(k => !rows.ContainsKey(k)).ToList();
        Assert.True(jsonOnly.Count == 0 && regOnly.Count == 0,
            $"drift: json-only [{string.Join(", ", jsonOnly)}] registry-only [{string.Join(", ", regOnly)}]");

        foreach (var (id, r) in rows)
        {
            var e = reg[id];
            Assert.True(e.IntroducedIn == r.IntroducedIn && e.RemovedIn == r.RemovedIn && e.ObsoleteIn == r.ObsoleteIn,
                $"drift on {id}: registry ({e.IntroducedIn},{e.RemovedIn?.ToString() ?? "-"},{e.ObsoleteIn?.ToString() ?? "-"}) "
                + $"vs json ({r.IntroducedIn},{r.RemovedIn?.ToString() ?? "-"},{r.ObsoleteIn?.ToString() ?? "-"})");
            // Obsolete rows warn with the FIXED 0903 band code regardless of the entry's own code field.
            if (r.ExpectDiagnostic is { } code && r.ObsoleteIn is null)
                Assert.True(e.DiagnosticCode == code, $"drift on {id}: DiagnosticCode {e.DiagnosticCode} vs expectDiagnostic {code}");
        }
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
