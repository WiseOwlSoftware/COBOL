// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The P3.6 VERSION_CHANGE_REFERENCE (VCR) audit gate — the Tier-1 STRUCTURAL spine that makes the ledger's status
/// DERIVED (never hand-ticked): each change row carries a machine anchor in its gating cell
/// (<c>&lt;!-- gate:construct-id --&gt;</c> / <c>ref-only</c> / <c>pin-to-spec</c> / <c>todo</c>), and the
/// generated "Gating status index" block (between <c>&lt;!-- GEN:VCR-STATUS START/END --&gt;</c>) is rendered from
/// those anchors + <c>constructs.json</c>. This test both RENDERS the block (write mode, via
/// <c>scripts/gen-vcr.ps1</c> → <c>COBOLNET_WRITE_VCR=1</c>) and, in normal CI, ASSERTS it is in sync, that every
/// <c>gate:</c> anchor resolves to a real construct (forward coverage), and that every appendix specLines reference
/// resolves inside the spec. Mirrors <c>DiagnosticRegistryDriftTests</c> (ONE renderer, in the test).
/// </summary>
public sealed class VcrDriftTests
{
    private const string Start = "<!-- GEN:VCR-STATUS START -->";
    private const string End = "<!-- GEN:VCR-STATUS END -->";

    private static string VcrPath => Path.Combine(EditionHarness.RepoRoot(), "docs", "VERSION_CHANGE_REFERENCE.md");

    private sealed record Con(string Id, int IntroducedIn, int? RemovedIn, int? ObsoleteIn, string DiagnosticCode, string Status);

    private static Dictionary<string, Con> LoadConstructs()
    {
        string p = Path.Combine(EditionHarness.RepoRoot(), "tests", "version-matrix", "constructs.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(p));
        var d = new Dictionary<string, Con>(StringComparer.Ordinal);
        foreach (var e in doc.RootElement.GetProperty("constructs").EnumerateArray())
        {
            string id = e.GetProperty("id").GetString()!;
            d[id] = new Con(id,
                e.GetProperty("introducedIn").GetInt32(),
                e.TryGetProperty("removedIn", out var r) && r.ValueKind != JsonValueKind.Null ? r.GetInt32() : null,
                e.TryGetProperty("obsoleteIn", out var o) && o.ValueKind != JsonValueKind.Null ? o.GetInt32() : null,
                e.GetProperty("diagnosticCode").GetString()!,
                e.TryGetProperty("status", out var s) && s.ValueKind != JsonValueKind.Null ? s.GetString()! : "active");
        }
        return d;
    }

    private static readonly Regex AnchorRx = new(@"<!--\s*((?:gate:[^\s>]+\s*)+)-->", RegexOptions.Compiled);
    private static readonly Regex RowNumRx = new(@"^\|\s*([0-9]+[a-z]?)\s*\|", RegexOptions.Compiled);

    /// <summary>Every VCR <c>gate:</c> anchor with the change row's number that carries it.</summary>
    private static List<(string Row, string[] Ids)> GateAnchors(string[] lines)
    {
        var res = new List<(string, string[])>();
        foreach (var ln in lines)
        {
            // ONLY real change-table rows carry anchors — a line must start "| <rownum> |". This excludes the
            // legend prose (which shows `<!-- gate:CONSTRUCT-ID -->` etc. as EXAMPLES) from being parsed as gates.
            var rm = RowNumRx.Match(ln);
            if (!rm.Success) continue;
            var m = AnchorRx.Match(ln);
            if (!m.Success) continue;
            var ids = m.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.StartsWith("gate:", StringComparison.Ordinal))
                .Select(t => t["gate:".Length..]).ToArray();
            res.Add((rm.Groups[1].Value, ids));
        }
        return res;
    }

    private static int RowSortKey(string row)
    {
        var digits = new string(row.TakeWhile(char.IsDigit).ToArray());
        return digits.Length > 0 ? int.Parse(digits) : int.MaxValue;
    }

    /// <summary>Render the generated status-index block body from the VCR anchors + the catalogue.</summary>
    private static string RenderIndex(string[] lines, Dictionary<string, Con> cons)
    {
        var byId = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        foreach (var (row, ids) in GateAnchors(lines))
            foreach (var id in ids)
            {
                if (!byId.TryGetValue(id, out var set)) byId[id] = set = new SortedSet<string>();
                set.Add(row);
            }
        var sb = new StringBuilder();
        sb.Append("| Construct | Introduced | Removed/obsolete | Diagnostic | Status | VCR row(s) |\n");
        sb.Append("| --- | --- | --- | --- | --- | --- |\n");
        foreach (var (id, rows) in byId)
        {
            var c = cons[id];   // forward-coverage test guarantees existence; KeyNotFound here = a real drift
            string window = c.RemovedIn is { } r ? $"removed {r}"
                : c.ObsoleteIn is { } ob ? $"obsolete {ob}" : "—";
            string status = c.Status == "active" ? "done" : "pending";
            string rowList = string.Join(", ", rows.OrderBy(RowSortKey).ThenBy(x => x, StringComparer.Ordinal));
            sb.Append($"| {id} | {c.IntroducedIn} | {window} | {c.DiagnosticCode} | {status} | {rowList} |\n");
        }
        return sb.ToString().TrimEnd('\n');
    }

    [Fact]
    public void EveryGateAnchor_ResolvesToARealConstruct()
    {
        var cons = LoadConstructs();
        var bad = new List<string>();
        foreach (var (row, ids) in GateAnchors(File.ReadAllLines(VcrPath)))
            foreach (var id in ids)
                if (!cons.ContainsKey(id)) bad.Add($"row {row}: gate:{id}");
        Assert.True(bad.Count == 0,
            "VCR gate anchor(s) naming a construct absent from constructs.json (forward-coverage): " + string.Join(", ", bad));
    }

    [Fact]
    public void EverySpecLineRef_IsWithinTheSpec()
    {
        string specPath = Path.Combine(EditionHarness.RepoRoot(), "specs", "ISO_COBOL.md");
        if (!File.Exists(specPath)) return;   // private submodule not initialized in this checkout — skip
        int specLines = File.ReadAllLines(specPath).Length;
        var lines = File.ReadAllLines(VcrPath);
        int appIdx = Array.FindIndex(lines, l => l.Contains("Appendix", StringComparison.Ordinal) && l.Contains("spec line", StringComparison.OrdinalIgnoreCase));
        Assert.True(appIdx >= 0, "VCR appendix (spec line references) not found");
        var numRx = new Regex(@"\d+");
        var bad = new List<string>();
        for (int i = appIdx; i < lines.Length; i++)
        {
            var ln = lines[i];
            if (!ln.TrimStart().StartsWith("|", StringComparison.Ordinal)) continue;
            var cells = ln.Split('|');
            if (cells.Length < 4) continue;
            string rownum = cells[1].Trim();
            if (rownum is "#" || rownum.All(c => c is '-' or ':' or ' ')) continue;   // header / separator
            foreach (Match mm in numRx.Matches(cells[2]))
            {
                int v = int.Parse(mm.Value);
                if (v < 1 || v > specLines) bad.Add($"row {rownum}: spec line {v} (spec has {specLines} lines)");
            }
        }
        Assert.True(bad.Count == 0, "VCR appendix specLines outside the spec (dangling citation): " + string.Join(", ", bad));
    }

    [Fact]
    public void GeneratedStatusIndex_IsInSync()
    {
        var lines = File.ReadAllLines(VcrPath);
        string rendered = RenderIndex(lines, LoadConstructs());
        string full = File.ReadAllText(VcrPath).Replace("\r\n", "\n");
        int s = full.IndexOf(Start, StringComparison.Ordinal);
        int e = full.IndexOf(End, StringComparison.Ordinal);
        Assert.True(s >= 0 && e > s, $"VCR generated-block markers not found ({Start} … {End})");

        if (Environment.GetEnvironmentVariable("COBOLNET_WRITE_VCR") == "1")
        {
            string updated = full[..(s + Start.Length)] + "\n" + rendered + "\n" + full[e..];
            File.WriteAllText(VcrPath, updated);
            return;
        }

        string committed = full[(s + Start.Length)..e].Trim('\n');
        Assert.True(committed == rendered,
            "docs/VERSION_CHANGE_REFERENCE.md gating status index is out of sync — regenerate: pwsh scripts/gen-vcr.ps1\n"
            + $"--- expected ---\n{rendered}\n\n--- committed ---\n{committed}");
    }
}
