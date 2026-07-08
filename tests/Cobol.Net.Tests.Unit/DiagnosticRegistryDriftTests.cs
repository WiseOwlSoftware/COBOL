// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using CobolNet.Editions.Diagnostics;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The P2.10 diagnostic-registry drift check (mirrors <c>ConstructRegistryDriftTests</c> /
/// <c>ReservedWordsDriftTests</c>): the first-class <see cref="DiagnosticCatalog"/> is THE source, so
/// (a) every descriptor's stable <see cref="DiagnosticDescriptor.Id"/> is unique, (b) no compiler emit site
/// still uses a BARE split-code string literal where a descriptor now exists (the <c>COBOLNET0899</c> catch-all
/// and the reused <c>COBOLNET1533</c>), and (c) <c>docs/DIAGNOSTICS.md</c> is regenerated-and-in-sync with the
/// catalogue. Editing the catalogue without regenerating the doc is a CI failure, never a silent stale doc.
/// Regenerate: <c>pwsh scripts/gen-diagnostics-doc.ps1</c> (or run this test with
/// <c>COBOLNET_WRITE_DIAGNOSTICS_DOC=1</c>).
/// </summary>
public sealed class DiagnosticRegistryDriftTests
{
    /// <summary>The split codes the 0899/1533 migration moved onto descriptors — no bare
    /// <c>.Error("&lt;code&gt;"</c> literal for these may survive in the compiler.</summary>
    private static readonly string[] SplitCodes = ["COBOLNET0899", "COBOLNET1533"];

    [Fact]
    public void EveryDescriptor_HasUniqueId()
    {
        var dupes = DiagnosticCatalog.All
            .GroupBy(d => d.Id, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        Assert.True(dupes.Count == 0, $"duplicate descriptor Id(s): [{string.Join(", ", dupes)}]");
    }

    [Fact]
    public void SplitCodes_HaveNoBareEmitLiteral_InCompiler()
    {
        string compiler = Path.Combine(RepoRoot(), "src", "Cobol.Net.Compiler");
        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(compiler, "*.cs", SearchOption.AllDirectories))
        {
            var lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
                foreach (var code in SplitCodes)
                    if (lines[i].Contains($"Error(\"{code}\"", StringComparison.Ordinal))
                        offenders.Add($"{Path.GetFileName(file)}:{i + 1}");
        }
        Assert.True(offenders.Count == 0,
            $"bare split-code emit literal(s) — route through DiagnosticCatalog.Error(descriptor, …): "
            + string.Join(", ", offenders));
    }

    [Fact]
    public void DiagnosticsDoc_IsInSync_WithTheCatalogue()
    {
        string path = Path.Combine(RepoRoot(), "docs", "DIAGNOSTICS.md");
        string expected = RenderMarkdown();

        if (Environment.GetEnvironmentVariable("COBOLNET_WRITE_DIAGNOSTICS_DOC") == "1")
        {
            File.WriteAllText(path, expected);
            return;
        }

        Assert.True(File.Exists(path), $"{path} is missing — run: pwsh scripts/gen-diagnostics-doc.ps1");
        string actual = Normalize(File.ReadAllText(path));
        Assert.True(actual == Normalize(expected),
            "docs/DIAGNOSTICS.md is out of sync with DiagnosticCatalog — regenerate: pwsh scripts/gen-diagnostics-doc.ps1");
    }

    /// <summary>Render the catalogue as the canonical <c>docs/DIAGNOSTICS.md</c> content.</summary>
    internal static string RenderMarkdown()
    {
        var sb = new StringBuilder();
        sb.Append("# COBOL.NET diagnostics\n\n");
        sb.Append("> **Generated** from `src/Cobol.Net.Editions/Diagnostics/DiagnosticCatalog.cs` — do not edit by hand.\n");
        sb.Append("> Regenerate: `pwsh scripts/gen-diagnostics-doc.ps1` (or run `DiagnosticRegistryDriftTests` with\n");
        sb.Append("> `COBOLNET_WRITE_DIAGNOSTICS_DOC=1`). `DiagnosticRegistryDriftTests` fails CI if this file drifts.\n\n");
        sb.Append("This catalogue is the first-class registry that replaced bare `COBOLNETnnnn` string literals\n");
        sb.Append("(rearch PHASE 02, P2.10). A **Code** may repeat across rows — the emitted number is byte-stable, the\n");
        sb.Append("stable **Id** is the identity. The `recognized-not-implemented` suppress key groups every\n");
        sb.Append("legal-but-deferred feature so it can be muted as a group. Scope: the edition band + the `COBOLNET0899`\n");
        sb.Append("split + the reused `COBOLNET1533`; the broader every-code→descriptor migration is the P7 follow-on.\n\n");
        sb.Append("| Code | Id | Severity | ISO § | Suppress key | Title |\n");
        sb.Append("| --- | --- | --- | --- | --- | --- |\n");
        foreach (var d in DiagnosticCatalog.All)
            sb.Append($"| {Cell(d.Code)} | {Cell(d.Id)} | {d.Severity} | {Cell(d.IsoSection)} "
                + $"| {Cell(d.ResolvedSuppressKey)} | {Cell(d.Title)} |\n");
        return sb.ToString();
    }

    /// <summary>Escape a markdown table cell (pipes + collapse any stray newlines).</summary>
    private static string Cell(string s) => s.Replace("|", "\\|").Replace("\r", "").Replace("\n", " ");

    private static string Normalize(string s) => s.Replace("\r\n", "\n").TrimEnd('\n') + "\n";

    private static string RepoRoot()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !Directory.Exists(Path.Combine(d.FullName, "tests", "version-matrix"))) d = d.Parent;
        return d?.FullName ?? throw new InvalidOperationException("repo root (with tests/version-matrix) not found");
    }
}
