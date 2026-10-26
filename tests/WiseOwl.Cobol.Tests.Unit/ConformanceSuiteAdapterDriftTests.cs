// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Common;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;
using FrontendDiagnostic = WiseOwl.Cobol.Frontend.Diagnostics.Diagnostic;
using FrontendDescriptors = WiseOwl.Cobol.Frontend.Diagnostics.DiagnosticDescriptors;
using FrontendDescriptor = WiseOwl.Cobol.Frontend.Diagnostics.DiagnosticDescriptor;
using FrontendSeverity = WiseOwl.Cobol.Frontend.Diagnostics.DiagnosticSeverity;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The drift guard for WiseOwl COBOL's conformance-suite adapter, <c>conformance-suite/adapters/wiseowl.json</c>
/// (DESIGN-conformance-suite.md §5): its <c>diagnostic</c> pattern, which <c>run.py</c> reads a flagged test's
/// diagnostics with, matches every diagnostic the compiler can report, as the compiler's own formatters render it —
/// the parse layer's <see cref="FrontendDiagnostic"/> (the COBOL<i>n</i> parser and CBL<i>n</i> COPY codes, and the
/// parse-layer COBOL#### twins) and the binder's <see cref="EditionContext"/> (every <see cref="DiagnosticCatalog"/>
/// code) — and reads the file and the line from it. A pattern that missed a code would judge a flagged test FAIL
/// ("no diagnostic reported") although the compiler flagged the marked line: the pattern once required the catalog's
/// former prefix and so never saw a parse error (kb/Work PB3175, review R1-2).
/// </summary>
public sealed class ConformanceSuiteAdapterDriftTests
{
    /// <summary>The adapter's pattern, a Python regular expression, read as .NET's: the two agree on everything the
    /// pattern uses but the named group, <c>(?P&lt;name&gt;</c> in Python and <c>(?&lt;name&gt;</c> in .NET.</summary>
    private static Regex AdapterPattern()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(TestRepo.At("conformance-suite", "adapters", "wiseowl.json")));
        string python = doc.RootElement.GetProperty("diagnostic").GetString()!;
        return new Regex(python.Replace("(?P<", "(?<", StringComparison.Ordinal), RegexOptions.CultureInvariant);
    }

    /// <summary>The series a code belongs to (kb/Work PB3575): <c>CBL####</c> is the COPY preprocessor's ("copy"), a
    /// <c>COBOL####</c> below 0400 the parser's ("parser") and one from 0400 up every other compiler diagnostic's
    /// ("compiler"); any other shape is "malformed", which no series check accepts.</summary>
    private static string SeriesOf(string code) =>
        Regex.Match(code, @"^(CBL|COBOL)(\d{4})$") is not { Success: true } m ? "malformed"
        : m.Groups[1].Value == "CBL" ? "copy"
        : int.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture) < 400 ? "parser" : "compiler";

    /// <summary>⛔ THE OBLIGATION, with its population: both registries enumerate to their known size and span the three
    /// code families, and every code, at every severity it can be reported at, rendered by its own formatter, is
    /// matched with its file and line.</summary>
    [Fact]
    public void TheWiseOwlAdapterPattern_MatchesEveryDiagnosticTheCompilerReports()
    {
        var pattern = AdapterPattern();
        var frontend = typeof(FrontendDescriptors).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(FrontendDescriptor))
            .Select(f => ((FrontendDescriptor)f.GetValue(null)!).Code).Distinct(StringComparer.Ordinal).ToArray();
        var catalog = DiagnosticCatalog.All.Select(d => d.Code).Distinct(StringComparer.Ordinal).ToArray();
        Assert.True(frontend.Length >= 20, $"DiagnosticDescriptors enumerated to {frontend.Length} code(s); the reflection is broken");
        Assert.True(catalog.Length >= 500, $"DiagnosticCatalog.All enumerated to {catalog.Length} code(s)");
        // the three series of the two prefixes (kb/Work PB3575): the parser's COBOL0001-COBOL0313, the COPY
        // preprocessor's CBL####, and every other compiler diagnostic, COBOL0400 and up
        Assert.Contains(frontend.Concat(catalog), c => SeriesOf(c) == "parser");
        Assert.Contains(frontend.Concat(catalog), c => SeriesOf(c) == "copy");
        Assert.Contains(frontend.Concat(catalog), c => SeriesOf(c) == "compiler");

        var problems = new List<string>();
        void Expect(string rendered, string code, string file, int? line)
        {
            var m = pattern.Match("  " + rendered);   // the CLI indents a warning line by two spaces
            if (!m.Success)
                problems.Add($"{code}: no match for {rendered}");
            else if (Path.GetFileName(m.Groups["file"].Value.Trim()) != file)
                problems.Add($"{code}: file {m.Groups["file"].Value} read from {rendered}");
            else if (line is { } want && m.Groups["line"].Value != want.ToString(System.Globalization.CultureInfo.InvariantCulture))
                problems.Add($"{code}: line {m.Groups["line"].Value} read from {rendered}, not {want}");
        }

        var location = new SourceLocation("PROG.cob", 0, 3, 11);   // 0-based line 3 renders as line 4
        foreach (string code in frontend.Concat(catalog))
            foreach (var severity in new[] { FrontendSeverity.Error, FrontendSeverity.Warning, FrontendSeverity.Info })
                Expect(new FrontendDiagnostic(code, severity, "a message", location, default).ToString(), code, "PROG.cob", 4);

        foreach (string code in catalog)
        {
            var edition = new EditionContext(2023) { SourceFile = "PROG.cob", Cursor = new DiagnosticCursor(4, 12) };
            edition.Error(code, "a message");
            edition.Warning(code, "a message");
            foreach (string rendered in edition.Diagnostics.Concat(edition.Warnings))
                Expect(rendered, code, "PROG.cob", null);
            if (edition.Diagnostics.Count + edition.Warnings.Count != 2)
                problems.Add($"{code}: EditionContext recorded {edition.Diagnostics.Count + edition.Warnings.Count} line(s), not 2");
        }

        Assert.True(problems.Count == 0,
            "conformance-suite/adapters/wiseowl.json's diagnostic pattern misses diagnostics the compiler reports "
            + $"({problems.Count}):\n  " + string.Join("\n  ", problems.Take(20)));
    }
}
