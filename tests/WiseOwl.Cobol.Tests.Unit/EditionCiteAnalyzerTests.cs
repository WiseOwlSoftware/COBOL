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

    /// <summary>A diagnostic option for every tree, as a <c>.editorconfig</c> (per tree) or a <c>.globalconfig</c> (global) sets one.</summary>
    private sealed class ConfiguredSeverity(string id, ReportDiagnostic severity, bool global) : SyntaxTreeOptionsProvider
    {
        public override GeneratedKind IsGenerated(SyntaxTree tree, CancellationToken cancellationToken) => GeneratedKind.Unknown;

        public override bool TryGetDiagnosticValue(SyntaxTree tree, string diagnosticId, CancellationToken cancellationToken,
            out ReportDiagnostic severity1) => Configured(diagnosticId, !global, out severity1);

        public override bool TryGetGlobalDiagnosticValue(string diagnosticId, CancellationToken cancellationToken,
            out ReportDiagnostic severity1) => Configured(diagnosticId, global, out severity1);

        private bool Configured(string diagnosticId, bool applies, out ReportDiagnostic configured)
        {
            configured = severity;
            return applies && diagnosticId == id;
        }
    }

    /// <summary>The analyzer's diagnostics that survive every filter the compiler applies (compilation options, configured severities,
    /// <c>#pragma</c>, <c>[SuppressMessage]</c>), each as "id severity".</summary>
    private static IReadOnlyList<string> Surviving(string source, Func<CSharpCompilationOptions, CSharpCompilationOptions> configure)
    {
        var options = configure(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
            nullableContextOptions: NullableContextOptions.Enable));
        var compilation = CSharpCompilation.Create("planted",
            [CSharpSyntaxTree.ParseText(Decider, Parse, path: "EditionInfo.cs"), CSharpSyntaxTree.ParseText(source, Parse, path: "planted.cs")],
            Framework, options);
        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, "the planted source does not bind:\n" + string.Join("\n", errors.Take(20)));
        return [.. compilation.WithAnalyzers([new EditionCiteAnalyzer()]).GetAllDiagnosticsAsync().GetAwaiter().GetResult()
            .Where(d => d.Id.StartsWith("WOCA", StringComparison.Ordinal) && !d.IsSuppressed)
            .Select(d => $"{d.Id} {d.Severity}")];
    }

    /// <summary>⛔ THE RULES CANNOT BE SILENCED (kb/Work PB4950 round 10). Both carry <see cref="WellKnownDiagnosticTags.NotConfigurable"/>,
    /// so NoWarn, a severity lowered to warning (the only way WarningsNotAsErrors could reach one), a <c>.editorconfig</c> or
    /// <c>.globalconfig</c> severity and <c>#pragma warning disable</c> neither turn one off nor lower it below an error, just as the
    /// test the analyzer replaced could not be silenced. Each form is planted here and the error survives it. Without the tag every
    /// one of them silenced the rule.</summary>
    [Theory]
    [InlineData("none")]
    [InlineData("NoWarn")]
    [InlineData("warning")]
    [InlineData("editorconfig")]
    [InlineData("globalconfig")]
    [InlineData("pragma")]
    public void NoFormOfSilencing_SilencesARule(string form)
    {
        const string choice = """string A(EditionInfo e) => e.Year < 2002 ? "ISO §1985:VI" : "ISO §2023:8";""";
        string source = form switch
        {
            "pragma" => "using WiseOwl.Cobol.Editions;\nclass C\n{\n#pragma warning disable WOCA0001\n    " + choice + "\n#pragma warning restore WOCA0001\n}\n",
            _ => "using WiseOwl.Cobol.Editions;\nclass C\n{\n    " + choice + "\n}\n",
        };
        Func<CSharpCompilationOptions, CSharpCompilationOptions> configure = form switch
        {
            "NoWarn" => o => o.WithSpecificDiagnosticOptions([new("WOCA0001", ReportDiagnostic.Suppress)]),
            "warning" => o => o.WithSpecificDiagnosticOptions([new("WOCA0001", ReportDiagnostic.Warn)]),
            "editorconfig" => o => o.WithSyntaxTreeOptionsProvider(new ConfiguredSeverity("WOCA0001", ReportDiagnostic.Suppress, global: false)),
            "globalconfig" => o => o.WithSyntaxTreeOptionsProvider(new ConfiguredSeverity("WOCA0001", ReportDiagnostic.Suppress, global: true)),
            _ => o => o,
        };
        Assert.Equal(["WOCA0001 Error"], Surviving(source, configure));
    }

    /// <summary>⛔ A SUPPRESSION ATTRIBUTE THAT NAMES A RULE FAILS THE BUILD (WOCA0003; kb/Work PB4950 round 11). <c>[SuppressMessage]</c>
    /// and <c>[UnconditionalSuppressMessage]</c> are applied by the analyzer driver after <see cref="WellKnownDiagnosticTags.NotConfigurable"/>
    /// is read, so they silence the rule they name, and any analyzer diagnostic refusing them could be silenced the same way. So the
    /// refusal is <see cref="SuppressionRefusal"/>, a generator that writes a <c>#error</c> (a compiler error, CS1029) for each such
    /// attribute, whatever its spelling: the full name, a <c>using</c> alias, the short name, an id held in a constant, the unconditional
    /// form.</summary>
    [Theory]
    [InlineData("""[System.Diagnostics.CodeAnalysis.SuppressMessage("WiseOwl.Editions", "WOCA0001")]""", "", "SuppressMessageAttribute")]
    [InlineData("""[Quiet("WiseOwl.Editions", "WOCA0001:A string chosen by edition")]""",
        "using Quiet = System.Diagnostics.CodeAnalysis.SuppressMessageAttribute;", "SuppressMessageAttribute")]
    [InlineData("""[SuppressMessage("WiseOwl.Editions", Ids.Rule)]""", "using System.Diagnostics.CodeAnalysis;", "SuppressMessageAttribute")]
    [InlineData("""[UnconditionalSuppressMessage("WiseOwl.Editions", "WOCA" + "0001")]""", "using System.Diagnostics.CodeAnalysis;",
        "UnconditionalSuppressMessageAttribute")]
    public void ASuppressionThatNamesARule_FailsTheBuild(string attribute, string usings, string kind)
    {
        string source = usings + "\nusing WiseOwl.Cobol.Editions;\nstatic class Ids { public const string Rule = \"WOCA\" + \"0001\"; }\nclass C\n{\n    "
            + attribute + "\n    string A(EditionInfo e) => e.Year < 2002 ? \"ISO §1985:VI\" : \"ISO §2023:8\";\n}\n";
        Assert.Empty(Surviving(source, o => o));   // the premise: the attribute silences the rule it names
        var refusals = Refusals(source);
        Assert.Equal($"WOCA0003: the {kind} at planted.cs:6 names 'WOCA0001', a rule of the one-decider analyzer", Assert.Single(refusals)
            [..$"WOCA0003: the {kind} at planted.cs:6 names 'WOCA0001', a rule of the one-decider analyzer".Length]);
    }

    /// <summary>A suppression that names WOCA0003 itself, on the member or as a target-less assembly-level suppression (each silenced a
    /// refusal reported as an analyzer diagnostic), reaches no compiler error: every attribute is refused.</summary>
    [Fact]
    public void ASuppressionOfTheRefusalItself_IsRefusedToo()
    {
        const string source = "using System.Diagnostics.CodeAnalysis;\nusing WiseOwl.Cobol.Editions;\n"
            + "[assembly: SuppressMessage(\"x\", \"WOCA0003\")]\n[assembly: SuppressMessage(\"x\", \"CS1029\")]\nclass C\n{\n"
            + "    [SuppressMessage(\"WiseOwl.Editions\", \"WOCA0001\")]\n    [SuppressMessage(\"WiseOwl.Editions\", \"WOCA0003\")]\n"
            + "    string A(EditionInfo e) => e.Year < 2002 ? \"ISO §1985:VI\" : \"ISO §2023:8\";\n}\n";
        Assert.Equal(["planted.cs:3", "planted.cs:7", "planted.cs:8"],
            Refusals(source).Select(r => r.Split(" names ")[0].Split(" at ")[1]).Order(StringComparer.Ordinal));
    }

    /// <summary>Only the checkId names what a suppression silences (kb/Work PB4950 round 12, R11-2): a WOCA id that a Justification, a
    /// MessageId, a Scope or a Target mentions, on a suppression of another rule, is not refused.</summary>
    [Fact]
    public void AnIdOutsideTheCheckId_IsNotRefused()
    {
        Assert.Empty(Refusals("using System.Diagnostics.CodeAnalysis;\nclass C\n{\n"
            + "    [SuppressMessage(\"Style\", \"IDE0001\", Justification = \"not WOCA0001, which still applies\", MessageId = \"WOCA0002\")]\n"
            + "    int A() => 1;\n}\n"));
        Assert.Single(Refusals("using System.Diagnostics.CodeAnalysis;\nclass C\n{\n"
            + "    [SuppressMessage(checkId: \"WOCA0001\", category: \"WiseOwl.Editions\")]\n    int A() => 1;\n}\n"));
    }

    /// <summary>A suppression that names no rule of the analyzer is no concern of it.</summary>
    [Fact]
    public void ASuppressionOfAnotherRule_IsNotRefused() =>
        Assert.Empty(Refusals("using System.Diagnostics.CodeAnalysis;\nclass C\n{\n    [SuppressMessage(\"Style\", \"IDE0001\")]\n    int A() => 1;\n}\n"));

    /// <summary>The compiler errors <see cref="SuppressionRefusal"/> puts into a planted compilation (CS1029, a <c>#error</c>), each as its
    /// message, after asserting the planted source binds without them.</summary>
    private static IReadOnlyList<string> Refusals(string source)
    {
        var compilation = CSharpCompilation.Create("planted",
            [CSharpSyntaxTree.ParseText(Decider, Parse, path: "EditionInfo.cs"), CSharpSyntaxTree.ParseText(source, Parse, path: "planted.cs")],
            Framework, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, "the planted source does not bind:\n" + string.Join("\n", errors.Take(20)));
        CSharpGeneratorDriver.Create([new SuppressionRefusal().AsSourceGenerator()], parseOptions: Parse)
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
        return [.. output.GetDiagnostics().Where(d => d.Id == "CS1029" && d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture))
            .Select(m => m.StartsWith("#error: '", StringComparison.Ordinal) ? m["#error: '".Length..^1] : m)];
    }

    /// <summary>⛔ AN ALIAS THROUGH ANOTHER SYMBOL OF THE SAME MEMBER (kb/Work PB4950 round 11, R10-2): an extension method read as an
    /// extension (a reduced symbol) and as a static call, and a member of a constructed generic type, are each the declared member.</summary>
    [Fact]
    public void AReducedOrConstructedSymbol_IsTheMemberItStandsFor()
    {
        const string planted = """
            using WiseOwl.Cobol.Editions;
            static class Ext { public static bool IsOld(this EditionInfo e) => e.Year < 2002; }
            static class Rules<T> { public static bool Old(EditionInfo e) => e.Year < 2002; public static bool Generic<U>(EditionInfo e) => e.Year < 2002; }
            class C
            {
                string AsExtension(EditionInfo e) => e.IsOld() ? "ISO §1985:VI" : "ISO §2023:8";
                string AsStatic(EditionInfo e) => Ext.IsOld(e) ? "ISO §1985:VI" : "ISO §2023:8";
                string ThroughAConstructedType(EditionInfo e) => Rules<int>.Old(e) ? "ISO §1985:VI" : "ISO §2023:8";
                string ThroughAGenericMethod(EditionInfo e) => Rules<string>.Generic<int>(e) ? "ISO §1985:VI" : "ISO §2023:8";
            }
            """;
        Assert.Equal([
            "planted.cs:6: WOCA0001 conditional expression", "planted.cs:7: WOCA0001 conditional expression",
            "planted.cs:8: WOCA0001 conditional expression", "planted.cs:9: WOCA0001 conditional expression",
        ], Analyze([], ("EditionInfo.cs", Decider), ("planted.cs", planted)));
    }

    /// <summary>⛔ NEVER UP INTO THE FRAMEWORK (kb/Work PB4950 round 12, R11-1): an edition-derived override of <c>object.GetHashCode</c>
    /// and an edition-derived implementation of <c>IReadOnlyCollection&lt;T&gt;.Count</c> do not make those framework members edition
    /// questions, so an unrelated read of them chooses nothing by edition.</summary>
    [Fact]
    public void ADerivedOverrideOfAFrameworkMember_LeavesTheFrameworkMemberAlone()
    {
        const string planted = """
            using System.Collections;
            using System.Collections.Generic;
            using WiseOwl.Cobol.Editions;
            sealed class Keyed { EditionInfo E = EditionInfo.Of(85); public override int GetHashCode() => E.Year < 2002 ? 1 : 2; public override bool Equals(object? o) => false; }
            sealed class Rows : IReadOnlyCollection<string>
            {
                EditionInfo E = EditionInfo.Of(85);
                public int Count => E.Year < 2002 ? 1 : 2;
                public IEnumerator<string> GetEnumerator() => new List<string>().GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }
            class C
            {
                string Names(IReadOnlyCollection<string> names) => names.Count == 1 ? "name" : "names";
                string List(List<string> names) => names.Count == 1 ? "name" : "names";
                string Hash(object o) => o.GetHashCode() == 1 ? "one" : "other";
            }
            """;
        Assert.Empty(Analyze([], ("EditionInfo.cs", Decider), ("planted.cs", planted)));
    }

    /// <summary>⛔ A READ THROUGH A BASE MEMBER (kb/Work PB4950 round 11, R10-4): an interface member, or an abstract or virtual one, is
    /// derived when a member that implements or overrides it is, and one another assembly can read carries the mark.</summary>
    [Fact]
    public void AnInterfaceOrAbstractMember_IsDerived_WhenItsImplementationIs()
    {
        const string planted = """
            using WiseOwl.Cobol.Editions;
            public interface IRule { bool Old(EditionInfo e); }
            public abstract class Rule { public abstract bool Old(EditionInfo e); public virtual bool Older(EditionInfo e) => false; }
            sealed class Impl : Rule, IRule
            {
                public override bool Old(EditionInfo e) => e.Year < 2002;
                public override bool Older(EditionInfo e) => e.Year < 2002;
                bool IRule.Old(EditionInfo e) => e.Year < 2002;
            }
            class C
            {
                string ThroughTheInterface(IRule r, EditionInfo e) => r.Old(e) ? "ISO §1985:VI" : "ISO §2023:8";
                string ThroughTheAbstract(Rule r, EditionInfo e) => r.Old(e) ? "ISO §1985:VI" : "ISO §2023:8";
                string ThroughTheVirtual(Rule r, EditionInfo e) => r.Older(e) ? "ISO §1985:VI" : "ISO §2023:8";
            }
            """;
        Assert.Equal([
            "planted.cs:2: WOCA0002 IRule.Old(WiseOwl.Cobol.Editions.EditionInfo)",
            "planted.cs:3: WOCA0002 Rule.Old(WiseOwl.Cobol.Editions.EditionInfo)",
            "planted.cs:3: WOCA0002 Rule.Older(WiseOwl.Cobol.Editions.EditionInfo)",
            "planted.cs:12: WOCA0001 conditional expression", "planted.cs:13: WOCA0001 conditional expression",
            "planted.cs:14: WOCA0001 conditional expression",
        ], Analyze([], ("EditionInfo.cs", Decider), ("planted.cs", planted)));
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

    /// <summary>An internal derived member is visible to another assembly when the compilation grants <c>InternalsVisibleTo</c> to a
    /// product assembly (kb/Work PB4950 round 10), read from the compilation's own attributes: it must carry the mark. A grant to a
    /// test assembly alone does not make it so.</summary>
    [Theory]
    [InlineData("WiseOwl.Cobol.Compiler", true)]
    [InlineData("WiseOwl.Cobol.Compiler, PublicKey=0024000004800000", true)]
    [InlineData("WiseOwl.Cobol.Tests.Unit", false)]
    [InlineData("Other.IntegrationTests", false)]
    public void AnInternalDerivedMember_IsMarked_WhenAProductIsGrantedItsInternals(string grantee, bool marked)
    {
        string library = $$"""
            [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("{{grantee}}")]
            namespace Lib;
            using WiseOwl.Cobol.Editions;
            internal static class Rules
            {
                internal static bool Old(EditionInfo e) => e.Year < 2002;
                private static bool Hidden(EditionInfo e) => e.Year < 2002;
            }
            """;
        Assert.Equal(marked ? ["library.cs:6: WOCA0002 Lib.Rules.Old(WiseOwl.Cobol.Editions.EditionInfo)"] : [],
            Analyze([], ("EditionInfo.cs", Decider), ("library.cs", library)));
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
