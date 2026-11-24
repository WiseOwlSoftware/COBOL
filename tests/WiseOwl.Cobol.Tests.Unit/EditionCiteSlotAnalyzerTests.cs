// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using WiseOwl.Cobol.Analyzers;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary><see cref="EditionCiteSlotAnalyzer"/> (WOCA0004; kb/Work PB5922 review round 1, R1-1) reports a <c>Cite</c> call whose
/// COBOL-2002 or COBOL-2014 citation is not the argument the call chooses at that edition, and nothing else. The analyzer runs over
/// the real compilation of every <c>src/</c> project at build time, so a clean build is the sweep; these tests plant each shape.</summary>
public sealed class EditionCiteSlotAnalyzerTests
{
    /// <summary>The WOCA0004 diagnostics over a planted class body, each as "line: message".</summary>
    private static IReadOnlyList<string> Analyze(string members)
    {
        string source = "using WiseOwl.Cobol.Editions;\nclass C\n{\n" + members + "\n}\n";
        var compilation = CSharpCompilation.Create("planted",
            [CSharpSyntaxTree.ParseText(EditionCiteAnalyzerTests.Decider, EditionCiteAnalyzerTests.Parse, path: "EditionInfo.cs"),
             CSharpSyntaxTree.ParseText(source, EditionCiteAnalyzerTests.Parse, path: "planted.cs")],
            EditionCiteAnalyzerTests.Framework,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, "the planted source does not bind:\n" + string.Join("\n", errors.Take(20)));
        return [.. compilation.WithAnalyzers([new EditionCiteSlotAnalyzer()]).GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult()
            .OrderBy(d => d.Location.GetLineSpan().StartLinePosition)
            .Select(d => $"{d.Location.GetLineSpan().StartLinePosition.Line + 1}: {d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)}")];
    }

    /// <summary>The shape round 1 found eleven times: the COBOL-2002 rule passed only as <c>y1985</c>, so COBOL-2002 prints
    /// <c>later</c>; and its COBOL-2014 twin, a §2014 text that only <c>y2002</c> carries while <c>y2014</c> holds another.</summary>
    [Fact]
    public void AnEditionsCitation_ThatEditionNeverPrints_IsRefused() =>
        Assert.Equal([
            "4: This Cite call cites a §2002 rule that --std 2002 never prints: at COBOL-2002 Cite chooses its later argument; pass the "
                + "§2002 text as y2002 (kb/Work PB5922, docs/VERSION_TEST_MATRIX_DESIGN.md P2.3a)",
            "5: This Cite call cites a §2014 rule that --std 2014 never prints: at COBOL-2014 Cite chooses its y2014 argument; pass the "
                + "§2014 text as y2014 (kb/Work PB5922, docs/VERSION_TEST_MATRIX_DESIGN.md P2.3a)",
            "6: This Cite call cites a §2002 rule that --std 2002 never prints: at COBOL-2002 Cite chooses its later argument; pass the "
                + "§2002 text as y2002 (kb/Work PB5922, docs/VERSION_TEST_MATRIX_DESIGN.md P2.3a)",
        ], Analyze("""
                string A(EditionInfo e) => e.Cite("§2002:14.1.2 SR2", "§2023:14.2.2 SR2", y2014: "§2014:14.2.2 SR2");
                string B(EditionInfo e) => e.Cite("§1985:X.1", "§2023:9.1", y2002: "§2014:9.1", y2014: "§2023:9.1");
                string D(EditionInfo e, string r) => e.Cite($"§2002:14.1.2 {r}", $"§2023:14.2.2 {r}");
            """));

    /// <summary>Every edition's text where the call chooses it is accepted: each edition explicit; a 2002 text inherited by 2014
    /// that cites 2002 only (no §2014 is written, so nothing is asked of 2014); a first-stating-edition text in <c>y1985</c> that the
    /// edition's own argument repeats; and a call with no 2002 or 2014 citation at all.</summary>
    [Fact]
    public void EachEditionsCitation_WhereTheCallChoosesIt_IsAccepted() =>
        Assert.Empty(Analyze("""
                string A(EditionInfo e) => e.Cite("§1985:X.1", "§2023:9.1", y2002: "§2002:8.1", y2014: "§2014:8.1");
                string B(EditionInfo e) => e.Cite("§1985:X.1", "§2023:9.1", y2002: "§2002:8.1");
                string D(EditionInfo e) => e.Cite("§2014:13.1 SR9", "§2023:13.1 SR9", y2002: "§2014:13.1 SR9", y2014: "§2014:13.1 SR9");
                string F(EditionInfo e) => e.Cite("§2023:12.4 SR7", "§2023:12.4 SR7");
                string G(EditionInfo e) => e.Cite("§1985:VI.1", "§2002:8.1");
            """));

    /// <summary>The rule is an error that no configuration lowers.</summary>
    [Fact]
    public void TheRule_IsAnUnconfigurableError() =>
        Assert.All(new EditionCiteSlotAnalyzer().SupportedDiagnostics, d =>
        {
            Assert.Equal(DiagnosticSeverity.Error, d.DefaultSeverity);
            Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, d.CustomTags);
        });
}
