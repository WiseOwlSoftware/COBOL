// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using WiseOwl.Cobol.Analyzers;
using WiseOwl.Cobol.Editions;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>The build's one-decider analyzer (<see cref="EditionCiteAnalyzer"/>, WOCA0001 and WOCA0002; kb/Work PB4950,
/// docs/VERSION_TEST_MATRIX_DESIGN.md P2.3a) reports what it is for. The analyzer runs over the REAL compilation of every <c>src/</c>
/// project at build time (<c>src/Directory.Build.props</c>), so a clean build of the solution is the sweep of the products; these tests
/// run it over planted source through <see cref="CompilationWithAnalyzers"/>, in each shape and through each kind of alias, so that a
/// clean build is evidence and not a silent analyzer.</summary>
public sealed class EditionCiteAnalyzerTests
{
    private static readonly CSharpParseOptions Parse = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);

    /// <summary>The framework, as metadata: the test host's trusted platform assemblies less the WiseOwl ones (the planted source declares
    /// the decider and the mark itself, so a built copy beside it would make each ambiguous).</summary>
    private static IReadOnlyList<MetadataReference> Framework { get; } =
        [.. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Where(f => !Path.GetFileName(f).StartsWith("WiseOwl.", StringComparison.Ordinal))
            .Select(f => MetadataReference.CreateFromFile(f))];

    /// <summary>The decider and the mark, as the planted compilations declare them: <c>EditionInfo</c> under its metadata name, with the
    /// shapes the analyzer reads from its symbol (an <c>int</c> property, a <c>bool</c> method of one <c>int</c>, a named rule fact), and
    /// a <c>Cite</c> whose own choice is the decider's and is not reported.</summary>
    private const string Decider = """
        namespace WiseOwl.Cobol.Editions
        {
            public readonly record struct EditionInfo(int Year, bool Permissive = false)
            {
                public static EditionInfo Of(int year) => new(year);
                public int MaxDigits => Year < 2002 ? 18 : 31;
                public bool SubscriptedClauseOperands => Year >= 2002;
                public bool Has(int introducedIn) => Year >= introducedIn;
                public string Cite(string y1985, string later, string? y2002 = null, string? y2014 = null) =>
                    Year < 2002 ? y1985 : Year < 2014 ? y2002 ?? later : Year < 2023 ? y2014 ?? y2002 ?? later : later;
            }
        }
        namespace WiseOwl.Cobol.Runtime
        {
            [System.AttributeUsage(System.AttributeTargets.All)] public sealed class EditionDerivedAttribute : System.Attribute;
        }
        """;

    /// <summary>The analyzer's diagnostics over <paramref name="trees"/>, as "file:line: id kind", after asserting the source binds.</summary>
    private static IReadOnlyList<string> Analyze(IEnumerable<MetadataReference> references, params (string Path, string Text)[] trees)
    {
        var compilation = CSharpCompilation.Create("planted",
            trees.Select(t => CSharpSyntaxTree.ParseText(t.Text, Parse, path: t.Path)),
            [.. Framework, .. references],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, "the planted source does not bind:\n" + string.Join("\n", errors.Take(20)));
        var diagnostics = compilation.WithAnalyzers([new EditionCiteAnalyzer()]).GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult();
        return [.. diagnostics
            .Select(d => (Span: d.Location.GetLineSpan(), Diagnostic: d))
            .OrderBy(x => x.Span.Path, StringComparer.Ordinal).ThenBy(x => x.Span.StartLinePosition)
            .Select(x => $"{x.Span.Path}:{x.Span.StartLinePosition.Line + 1}: {x.Diagnostic.Id} {Kind(x.Diagnostic)}")];
    }

    /// <summary>A WOCA0001 message names the shape ("This conditional expression chooses ..."); a WOCA0002 one names the member.</summary>
    private static string Kind(Diagnostic d)
    {
        string message = d.GetMessage(System.Globalization.CultureInfo.InvariantCulture);
        return d.Id == EditionCiteAnalyzer.ChoiceId
            ? message["This ".Length..message.IndexOf(" chooses", StringComparison.Ordinal)]
            : message[1..message.IndexOf('\'', 1)];
    }

    [Fact]
    public void TheAnalyzer_ReportsEachShape_AndEachAlias()
    {
        const string planted = """
            using System.Collections.Generic;
            using System.Linq;
            using WiseOwl.Cobol.Editions;
            class EditionContext { public EditionInfo Edition = EditionInfo.Of(85); public int DialectLevel => Edition.Year; public bool Permissive; }
            class C
            {
                EditionContext Edition = new();
                bool Is1985 => Edition.DialectLevel < 2002;
                bool Legacy => Is1985;
                string A() => Edition.DialectLevel < 2002
                    ? "ISO §1985:VI.1"
                    : "ISO §2023:8.1";
                string B(EditionInfo edition) => edition.Year switch
                {
                    85 => $"85 {1}",
                    _ => "later",
                };
                string D(int dialectLevel)
                {
                    if (dialectLevel is 2002 or 2014)
                        return "a";
                    else
                        return "b" + "c";
                }
                string E(EditionInfo e) => e.Has(2002) ? "x" : "y";
                string AliasLocal() { bool is1985 = Edition.DialectLevel < 2002; return is1985 ? "a" : "b"; }
                string AliasProperty() => Is1985 ? "a" : "b";
                string AliasChain() => Legacy ? "a" : "b";
                string Fine(EditionInfo edition) => edition.Cite("a", "b");
                int Count(int dialectLevel) => dialectLevel >= 2023 ? 63 : 31;
                string Mode() => Edition.Permissive ? "warning" : "error";
                string Fact(EditionInfo edition) => edition.SubscriptedClauseOperands ? "subscripted" : "plain";
                string Mixed(bool index) => index && Edition.DialectLevel < 2002 ? "refused" : "admitted";
                string ThroughACall(List<EditionContext> ds) { bool modern = ds.First().Edition.Has(2002); return modern ? "a" : "b"; }
                static bool Is85(EditionInfo e) => e.Year < 2002;
                string Predicate(EditionInfo e) => Is85(e) ? "a" : "b";
                string Statement(EditionInfo e)
                {
                    switch (e.Year)
                    {
                        case 85: return "a";
                        default: return "b";
                    }
                }
                string EarlyReturn(EditionInfo e)
                {
                    if (e.Year < 2002) return "a";
                    return "b";
                }
                static int Printed(int edition) => edition >= 2014 ? 2014 : 2002;
                string Hole(int edition) => $"§{Printed(edition)}:8.12";
                string Named(EditionInfo e) => $"targeting COBOL-{e.Year}";
            }
            """;
        Assert.Equal([
            "planted.cs:10: WOCA0001 conditional expression", "planted.cs:13: WOCA0001 switch expression", "planted.cs:20: WOCA0001 if/else",
            "planted.cs:25: WOCA0001 conditional expression", "planted.cs:26: WOCA0001 conditional expression",
            "planted.cs:27: WOCA0001 conditional expression", "planted.cs:28: WOCA0001 conditional expression",
            "planted.cs:33: WOCA0001 conditional expression", "planted.cs:34: WOCA0001 conditional expression",
            "planted.cs:36: WOCA0001 conditional expression", "planted.cs:39: WOCA0001 switch statement",
            "planted.cs:47: WOCA0001 if/return", "planted.cs:51: WOCA0001 interpolation",
        ], Analyze([], ("EditionInfo.cs", Decider), ("planted.cs", planted)));
    }

    /// <summary>Across assemblies: a derived member another assembly can read must carry the mark (WOCA0002 where it is declared, and
    /// none on one that carries it, on a seed, or on one no other assembly can read), and a marked member read from metadata is
    /// derived in the compilation that reads it, so a string chosen on it there is refused.</summary>
    [Fact]
    public void ADerivedMemberAnotherAssemblyReads_IsMarked_AndTheMarkCarriesTheDerivation()
    {
        const string library = """
            using WiseOwl.Cobol.Editions;
            using WiseOwl.Cobol.Runtime;
            public static class Rules
            {
                public static bool Unmarked(EditionInfo e) => e.Year < 2002;
                [EditionDerived] public static bool Marked(EditionInfo e) => e.Year < 2002;
                internal static bool Internal(EditionInfo e) => e.Year < 2002;
                public static bool OfTheProgram(EditionInfo e, string word) => e.Year < 2002 && word.Length > 30;
            }
            public class Context { public int DialectLevel => 85; protected bool Is85() => DialectLevel < 2002; }
            """;
        Assert.Equal(["library.cs:5: WOCA0002 Rules.Unmarked(WiseOwl.Cobol.Editions.EditionInfo)", "library.cs:10: WOCA0002 Context.Is85()"],
            Analyze([], ("EditionInfo.cs", Decider), ("library.cs", library)));

        var built = CSharpCompilation.Create("library",
            [CSharpSyntaxTree.ParseText(Decider, Parse, path: "EditionInfo.cs"), CSharpSyntaxTree.ParseText(library, Parse, path: "library.cs")],
            Framework, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        using var image = new MemoryStream();
        var emitted = built.Emit(image);
        Assert.True(emitted.Success, string.Join("\n", emitted.Diagnostics));
        const string reader = """
            using WiseOwl.Cobol.Editions;
            class Reader
            {
                string Through(EditionInfo e) => Rules.Marked(e) ? "ISO §1985:VI" : "ISO §2023:8";
                string NotMarked(EditionInfo e) => Rules.OfTheProgram(e, "w") ? "a" : "b";
            }
            """;
        Assert.Equal(["reader.cs:4: WOCA0001 conditional expression"],
            Analyze([MetadataReference.CreateFromImage(image.ToArray())], ("reader.cs", reader)));
    }

    /// <summary>A tree with an auto-generated header (the ANTLR parser, a <c>*.Table.cs</c> file) is analyzed like any other (kb/Work
    /// PB4950 round 8, R8-1): a member derived there is derived where hand-written code reads it, and a choice written in a generated
    /// file that is not a <c>Generated</c> directory or a <c>.g.cs</c> file is reported where it is written.</summary>
    [Fact]
    public void AGeneratedHeader_HidesNoDerivation_AndNoChoice()
    {
        const string generated = """
            // <auto-generated/>
            using WiseOwl.Cobol.Editions;
            static class Predicates { public static bool Old(EditionInfo e) => e.Year < 2002; }
            """;
        const string table = """
            // <auto-generated>
            //     Generated from a data file.
            // </auto-generated>
            using WiseOwl.Cobol.Editions;
            static class Words { public static string Of(EditionInfo e) => e.Year < 2002 ? "ISO §1985:VI" : "ISO §2023:8"; }
            """;
        const string reader = """
            using WiseOwl.Cobol.Editions;
            class Reader { string Through(EditionInfo e) => Predicates.Old(e) ? "ISO §1985:VI" : "ISO §2023:8"; }
            """;
        Assert.Equal(["Words.Table.cs:5: WOCA0001 conditional expression", "reader.cs:2: WOCA0001 conditional expression"],
            Analyze([], ("EditionInfo.cs", Decider), ("Predicates.cs", generated), ("Words.Table.cs", table), ("reader.cs", reader)));
    }

    /// <summary>The analyzer cannot evaluate a referenced assembly's static, so it carries the editions the source compares; they are
    /// <see cref="EditionInfo.All"/>.</summary>
    [Fact]
    public void TheAnalyzersEditions_AreEditionInfoAll() =>
        Assert.Equal(EditionInfo.All.ToArray(), EditionCiteAnalyzer.Editions);

    /// <summary>The descriptors are errors: a choice the analyzer finds fails the build.</summary>
    [Fact]
    public void EveryRule_IsAnError() =>
        Assert.All(new EditionCiteAnalyzer().SupportedDiagnostics, d => Assert.Equal(DiagnosticSeverity.Error, d.DefaultSeverity));
}
