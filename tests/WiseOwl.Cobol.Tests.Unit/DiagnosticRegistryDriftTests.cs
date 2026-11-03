// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The P2.10 diagnostic-registry drift check (mirrors <c>ConstructRegistryDriftTests</c> /
/// <c>ReservedWordsDriftTests</c>): the first-class <see cref="DiagnosticCatalog"/> is THE source, so
/// (a) every descriptor's stable <see cref="DiagnosticDescriptor.Id"/> is unique and its code is unique outside the
/// deliberate split families, (b) no emit call anywhere in <c>src/</c> passes a <c>"COBOLnnnn"</c> literal instead of a
/// descriptor, and (c) <c>docs/DIAGNOSTICS.md</c> is a RENDERED VIEW of the catalogue
/// (kb/Work PB3026, <c>scripts/rendered_views.py</c>): gitignored and never committed, because two pull requests that each
/// add a diagnostic would both rewrite it. This test is that view's render check: the catalogue must render one row per
/// descriptor. Render it: <c>pwsh scripts/gen-diagnostics-doc.ps1</c> (or run this test with
/// <c>WISEOWL_COBOL_WRITE_DIAGNOSTICS_DOC=1</c>).
/// </summary>
[Trait("Sweep", "tree")]
public sealed class DiagnosticRegistryDriftTests
{
    /// <summary>Codes DELIBERATELY shared by multiple descriptors: the split families. Any other shared code is a
    /// collision — COBOL1535 was once listed here, its second descriptor (the staged RENAMES-in-TYPEDEF stage) having
    /// been a collision nobody had registered; the stage was implemented and the descriptor deleted (kb/Work PB1304,
    /// PB608), so the code now has one meaning like every other.</summary>
    private static readonly string[] SharedCodes = ["COBOL0899", "COBOL1533"];

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

    /// <summary>No two descriptors may share a COBOL#### Code — one user-visible code, one meaning — except the
    /// deliberate split-code families (<see cref="SplitCodes"/>: many staged-loud descriptors share 0899/1533 by
    /// design). Id-uniqueness alone cannot catch a code collision (two kebab names, one code) — the shipped
    /// COBOL1573 collision class.</summary>
    [Fact]
    public void EveryDescriptor_HasUniqueCode_OutsideSplitFamilies()
    {
        var dupes = DiagnosticCatalog.All
            .Where(d => !SharedCodes.Contains(d.Code))
            .GroupBy(d => d.Code, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key} [{string.Join(", ", g.Select(d => d.Id))}]")
            .ToList();
        Assert.True(dupes.Count == 0, $"descriptor code collision(s): {string.Join("; ", dupes)}");
    }

    /// <summary>⛔ NO EMIT CALL TAKES A CODE LITERAL (kb/Work PB4463, PB294, PB310): every call of an emit API
    /// (<c>Error</c>, <c>Warning</c>, <c>Report</c>, <c>ReportError</c>, <c>ReportWarning</c>, <c>Emit</c>), qualified or not,
    /// names a <see cref="DiagnosticCatalog"/> descriptor, never a <c>"COBOLnnnn"</c> string. A literal is invisible to the
    /// next-free allocation scan's catalog half, to <c>docs/DIAGNOSTICS.md</c>, to <c>--suppress</c> and to the code-uniqueness
    /// fact above, which is how the COBOL1573 two-meanings collision shipped and how COBOL1512 came to mean three
    /// unrelated rules. The first form of this fact had a look-behind that skipped every qualified call
    /// (<c>edition.Error(...)</c>, the way nearly every site is written), so it read 2% of its subject (PB294); this
    /// one reads the call shapes the semgrep rule <c>wiseowl-cobol-raw-diagnostic-code-literal</c> reads, whose baseline
    /// is zero. A code a program computes at run time goes through a descriptor's <see cref="DiagnosticDescriptor.Code"/>.</summary>
    [Fact]
    public void NoEmitCall_TakesACodeLiteral()
    {
        var emitRx = new System.Text.RegularExpressions.Regex(
            "(?:\\bEmit\\(\\s*[\\w.]+\\s*,\\s*|\\.(?:Error|Warning|Report|ReportError|ReportWarning)\\(\\s*|(?<![\\w.])(?:Error|Warning|ReportError|ReportWarning)\\(\\s*)\"(COBOL\\d{4})\"");
        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(TestRepo.Src(), "*.cs", SearchOption.AllDirectories))
        {
            if (file.EndsWith("DiagnosticCatalog.cs", StringComparison.Ordinal)
                || file.EndsWith(".g.cs", StringComparison.Ordinal)
                || file.Contains("Generated", StringComparison.OrdinalIgnoreCase)
                || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")) continue;
            string text = File.ReadAllText(file);
            foreach (System.Text.RegularExpressions.Match match in emitRx.Matches(text))
                offenders.Add($"{Path.GetFileName(file)}:{text.Take(match.Index).Count(c => c == '\n') + 1} {match.Groups[1].Value}");
        }
        Assert.True(offenders.Count == 0,
            "emit call(s) passing a COBOLnnnn string literal - register a DiagnosticCatalog descriptor for the rule and pass it "
            + "(one code, one rule): " + string.Join(", ", offenders));
    }

    /// <summary>⛔ THE EXCEPTION-CHECKING (FORMAT-3) PERFORM'S CODES ARE ALL CATALOG DESCRIPTORS (kb/Work PB1154). The
    /// family COBOL1597-1617 was written as bare string literals at <c>ctx.Edition.Error("COBOL16nn", …)</c>, a
    /// shape the first form of <see cref="NoEmitCall_TakesACodeLiteral"/> could not see (its look-behind skipped a dotted
    /// call), so the whole family had no catalogue row, no DIAGNOSTICS.md entry and no suppress key. This pins the
    /// family two ways: no source file outside the catalogue writes one of its codes as a quoted literal, and every
    /// code the binder emits has a descriptor (1598, 1602, 1603 and 1609 are reserved numbers no rule emits).</summary>
    [Fact]
    public void ExceptionCheckingPerformCodes_AreCatalogDescriptors()
    {
        var literal = new System.Text.RegularExpressions.Regex("\"COBOL(159[7-9]|160\\d|161[0-7])\"");
        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(TestRepo.Src(), "*.cs", SearchOption.AllDirectories))
        {
            if (file.EndsWith("DiagnosticCatalog.cs", StringComparison.Ordinal)
                || file.EndsWith(".g.cs", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")) continue;
            var lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
                if (!lines[i].TrimStart().StartsWith("//", StringComparison.Ordinal) && literal.IsMatch(lines[i]))
                    offenders.Add($"{Path.GetFileName(file)}:{i + 1}");
        }
        Assert.True(offenders.Count == 0,
            "bare COBOL1597-1617 literal(s) — reference the DiagnosticCatalog descriptor (kb/Work PB1154): "
            + string.Join(", ", offenders));

        string[] emitted =
        [
            "COBOL1597", "COBOL1599", "COBOL1600", "COBOL1601", "COBOL1604", "COBOL1605", "COBOL1606",
            "COBOL1607", "COBOL1608", "COBOL1610", "COBOL1611", "COBOL1612", "COBOL1613", "COBOL1614",
            "COBOL1615", "COBOL1616", "COBOL1617",
        ];
        var known = DiagnosticCatalog.All.Where(d => d.Severity == WiseOwl.Cobol.Editions.EditionSeverity.Error)
            .Select(d => d.Code).ToHashSet(StringComparer.Ordinal);
        Assert.All(emitted, code => Assert.True(known.Contains(code), $"{code} has no error-severity descriptor"));
    }

    [Fact]
    public void DiagnosticsDoc_RendersTheWholeCatalogue()
    {
        string rendered = RenderMarkdown();

        if (Environment.GetEnvironmentVariable("WISEOWL_COBOL_WRITE_DIAGNOSTICS_DOC") == "1")
        {
            File.WriteAllText(TestRepo.Docs("DIAGNOSTICS.md"), rendered);
            return;
        }

        // The view is not compared with a copy on disk (a copy is not promised to be current, and is gitignored):
        // what can go wrong is the renderer, so each descriptor must come out as exactly one table row.
        string[] rows = rendered.Split('\n').Where(l => l.StartsWith("| ", StringComparison.Ordinal)).Skip(2).ToArray();
        Assert.Equal(DiagnosticCatalog.All.Count, rows.Length);
        Assert.All(DiagnosticCatalog.All, d =>
            Assert.Contains(rows, r => r.Contains($"| {Cell(d.Id)} |", StringComparison.Ordinal)));
    }

    /// <summary>Render the catalogue as the canonical <c>docs/DIAGNOSTICS.md</c> content.</summary>
    internal static string RenderMarkdown()
    {
        var sb = new StringBuilder();
        sb.Append("# WiseOwl COBOL diagnostics\n\n");
        sb.Append("> **Rendered view** of `src/WiseOwl.Cobol.Editions/Diagnostics/DiagnosticCatalog.cs` — gitignored, never committed or edited.\n");
        sb.Append("> Render: `pwsh scripts/gen-diagnostics-doc.ps1` (or run `DiagnosticRegistryDriftTests` with\n");
        sb.Append("> `WISEOWL_COBOL_WRITE_DIAGNOSTICS_DOC=1`).\n\n");
        sb.Append("This catalogue is the first-class registry that replaced bare `COBOLnnnn` string literals\n");
        sb.Append("(rearch PHASE 02, P2.10). A **Code** may repeat across rows — the emitted number is byte-stable, the\n");
        sb.Append("stable **Id** is the identity. The `recognized-not-implemented` suppress key groups every\n");
        sb.Append("legal-but-deferred feature so it can be muted as a group. Scope: the edition band + the `COBOL0899`\n");
        sb.Append("split + the reused `COBOL1533`; the broader every-code→descriptor migration is the P7 follow-on.\n\n");
        sb.Append("| Code | Id | Severity | ISO § | Suppress key | Title |\n");
        sb.Append("| --- | --- | --- | --- | --- | --- |\n");
        foreach (var d in DiagnosticCatalog.All)
            sb.Append($"| {Cell(d.Code)} | {Cell(d.Id)} | {d.Severity} | {Cell(d.IsoSection)} "
                + $"| {Cell(d.ResolvedSuppressKey)} | {Cell(d.Title)} |\n");
        return sb.ToString();
    }

    /// <summary>Escape a markdown table cell (pipes + collapse any stray newlines).</summary>
    private static string Cell(string s) => s.Replace("|", "\\|").Replace("\r", "").Replace("\n", " ");
}
