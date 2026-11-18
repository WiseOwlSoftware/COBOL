// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>⛔ ONE DECIDER OF A MESSAGE'S WORDS BY EDITION (kb/Work PB4950 review rounds 1 to 5; docs/VERSION_TEST_MATRIX_DESIGN.md
/// P2.3a). <c>EditionInfo.Cite(y1985, later, y2002, y2014)</c> is the one choice of a citation, or of the words a message quotes,
/// by edition. A site that spells its own choice is a second decider the next edition's text has to be threaded through by hand.
/// <para>THE COMPILATIONS ARE THE BUILD'S. Each <c>src/</c> project is compiled as MSBuild compiles it: its own sources, the compile
/// items MSBuild generated for it in <c>obj/</c> (its global usings from <c>ImplicitUsings</c>, its assembly attributes), the language
/// version and nullable context of <c>Directory.Build.props</c>, its project references (resolved through
/// <see cref="TestRepo.MsBuildPath"/>, closed transitively) as compilation references, the framework and packages of the test host's
/// trusted-platform list less the WiseOwl assemblies, and the compiler's source generator. Each compilation is ASSERTED to bind with
/// no error: a guard over a compilation that does not bind could miss silently.</para>
/// <para>EDITION-DERIVED VALUES, AS A FIXED POINT OF THE SEMANTIC MODEL. The seeds are the members of <see cref="EditionInfo"/> that
/// answer which edition or a number of it (each public instance <c>int</c> property, each <c>bool</c> method of an <c>int</c>:
/// <c>Has</c>), found by reflection, plus <c>DialectLevel</c> and <c>Std</c>. A <c>bool</c> or <c>int</c> local or field whose
/// initializer, or property, method or local function whose return expression, reads a derived value (or compares a name that says
/// edition, level or year with an edition of <see cref="EditionInfo.All"/>) is derived too: a method only when every parameter
/// carries an edition (<c>Is85(EditionInfo e)</c>, <c>Printed(int edition)</c>), since one that also takes the program's data answers
/// a question about the program. The derivation repeats until nothing changes, across files and projects, so an alias
/// (<c>bool is1985 = ...</c>, <c>Prints2002</c>, a chain) is seen wherever it is read. <see cref="EditionInfo"/>'s own <c>bool</c>
/// properties are named rule facts (<c>SubscriptedClauseOperands</c>) and are neither seeds nor derived.</para>
/// <para>THE CHOICES REFUSED, outside <c>EditionInfo.cs</c>, generated code and the code generator (whose strings are emitted C#): a
/// conditional expression, a switch expression or statement, an if/else, or an if that returns with the next statement returning,
/// that chooses between two strings (written, or of type string and not a call: Cite would run both arms) on a condition that reads
/// an edition-derived value; and an interpolation hole whose own value a derived member computes, a string or a number spliced into
/// a citation (<c>§{Printed(edition)}:8.12</c>). A choice that can answer null is a verdict (whether a rule fires), and a hole that
/// prints the compile edition (<c>COBOL-{DialectLevel}</c>) names it. A program question beside the edition stays a ternary:
/// <c>x &amp;&amp; is1985 ? a : b</c> is written <c>x ? Cite(a, b) : b</c>.</para></summary>
[Trait("Sweep", "tree")]
public sealed class EditionCiteOneDeciderDriftTests
{
    /// <summary>The names of the seed members of <see cref="EditionInfo"/>.</summary>
    private static readonly HashSet<string> SeedNames = BuildSeedNames();

    /// <summary>The editions as the source compares them.</summary>
    private static readonly HashSet<int> EditionValues = [.. EditionInfo.All.ToArray()];

    private static HashSet<string> BuildSeedNames()
    {
        var t = typeof(EditionInfo);
        return t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(int))
            .Select(p => p.Name)
            .Concat(t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.ReturnType == typeof(bool) && m.GetParameters() is [{ ParameterType: var pt }] && pt == typeof(int))
                .Select(m => m.Name))
            .ToHashSet(StringComparer.Ordinal);
    }

    private static bool IsSeed(ISymbol s) =>
        (s.ContainingType?.Name == nameof(EditionInfo) && SeedNames.Contains(s.Name))
        || (s is IPropertySymbol && s.Name is "DialectLevel" or "Std");

    /// <summary>The framework and the packages, as metadata: the test host's trusted platform assemblies (the runtime resolves the
    /// framework and every package the products reference into that list), less the WiseOwl assemblies, which the compilation builds
    /// from their sources (a built copy beside them would make every member ambiguous, CS0121).</summary>
    private static IReadOnlyList<MetadataReference> FrameworkAndPackages { get; } =
        [.. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Where(f => !Path.GetFileName(f).StartsWith("WiseOwl.", StringComparison.Ordinal))
            .Select(f => MetadataReference.CreateFromFile(f))];

    /// <summary>The C# the products are built from, one compilation per <c>src/</c> project, built the way MSBuild builds it: the
    /// project's own sources, the compile items MSBuild generates for it from the same settings (its global usings, from
    /// <c>ImplicitUsings</c>, and its assembly attributes, in its <c>obj/</c>), the language version and nullable context of
    /// <c>Directory.Build.props</c>, its project references as compilation references, and the compiler's source generator run as
    /// the build runs it. Each compilation must bind with no error: a guard over a compilation that does not bind can miss silently.</summary>
    private static IReadOnlyList<CSharpCompilation> SourceCompilations => s_sourceCompilations.Value;

    private static readonly Lazy<IReadOnlyList<CSharpCompilation>> s_sourceCompilations = new(BuildSourceCompilations);

    private static readonly string[] Projects =
        ["WiseOwl.Cobol.Runtime", "WiseOwl.Cobol.Editions", "WiseOwl.Cobol.Frontend", "WiseOwl.Cobol.Compiler", "WiseOwl.Cobol.Cli"];

    private static IReadOnlyList<CSharpCompilation> BuildSourceCompilations()
    {
        // Directory.Build.props sets LangVersion 14; the Roslyn this test links (4.14) names C# 14 Preview.
        var parse = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        // Projects are keyed by their project file's full path, the identity MSBuild gives them (a ProjectReference resolves to it).
        static string ProjectFile(string project) => Path.GetFullPath(TestRepo.Src(project, project + ".csproj"));
        var built = new Dictionary<string, CSharpCompilation>(StringComparer.Ordinal);
        var closure = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (string project in Projects)   // in dependency order
        {
            string dir = TestRepo.Src(project);
            string obj = TestRepo.Src(project, "obj", TestRepo.Configuration, TestRepo.TargetFramework);
            string file = ProjectFile(project);
            var sources = Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !Path.GetRelativePath(dir, f).Split(Path.DirectorySeparatorChar).Any(seg => seg is "bin" or "obj"))
                .Concat(Directory.EnumerateFiles(obj, "*.cs", SearchOption.TopDirectoryOnly))
                .Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), parse, path: f));
            // MSBuild flows project references transitively, so a project sees every project below its own references.
            var direct = System.Xml.Linq.XDocument.Load(file)
                .Descendants("ProjectReference")
                .Where(r => (string?)r.Attribute("ReferenceOutputAssembly") != "false")
                .Select(r => TestRepo.MsBuildPath(Path.GetDirectoryName(file)!, (string)r.Attribute("Include")!))
                .ToList();
            closure[file] = [.. direct.Concat(direct.SelectMany(d => closure[d])).Distinct(StringComparer.Ordinal)];
            var references = closure[file].Select(reference => built[reference].ToMetadataReference());
            var compilation = CSharpCompilation.Create(project, sources, [.. FrameworkAndPackages, .. references],
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
            if (project == "WiseOwl.Cobol.Compiler")
                compilation = RunSourceGenerator(compilation, parse);
            var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            Assert.True(errors.Count == 0, $"{project} does not bind as the build binds it: {errors.Count} error(s); the first 20:\n"
                + string.Join("\n", errors.Take(20)));
            built[file] = compilation;
        }
        return [.. Projects.Select(p => built[ProjectFile(p)])];
    }

    /// <summary>The compiler's bound-tree visitor generator (<c>WiseOwl.Cobol.Compiler.SourceGen</c>, an analyzer the compiler project
    /// references with <c>OutputItemType="Analyzer"</c>), run over the compilation as the build runs it.</summary>
    private static CSharpCompilation RunSourceGenerator(CSharpCompilation compilation, CSharpParseOptions parse)
    {
        const string generator = "WiseOwl.Cobol.Compiler.SourceGen";
        var assembly = ScratchDirectory.LoadAssembly(
            TestRepo.Src(generator, "bin", TestRepo.Configuration, "netstandard2.0", generator + ".dll"));
        var generators = assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(IIncrementalGenerator).IsAssignableFrom(t))
            .Select(t => ((IIncrementalGenerator)Activator.CreateInstance(t)!).AsSourceGenerator())
            .ToList();
        Assert.NotEmpty(generators);
        CSharpGeneratorDriver.Create(generators, parseOptions: parse)
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
        return (CSharpCompilation)output;
    }

    /// <summary>Every ad-hoc edition choice of a string in the trees <paramref name="scan"/> accepts, as "path:line: kind".</summary>
    public static IReadOnlyList<string> Find(IReadOnlyList<CSharpCompilation> compilations, Func<SyntaxTree, bool> scan,
        Func<SyntaxTree, bool> derives)
    {
        var models = compilations.SelectMany(c => c.SyntaxTrees.Select(t => (Tree: t, Model: c.GetSemanticModel(t))))
            .ToDictionary(x => x.Tree, x => x.Model);

        // ── The edition-derived values, as a fixed point over every declaration that can carry one ──
        var reads = new List<(ISymbol Declared, IReadOnlyList<ISymbol> Reads, bool ComparesEdition)>();
        foreach (var (tree, model) in models)
        {
            if (!derives(tree)) continue;
            foreach (var node in tree.GetRoot().DescendantNodes())
            {
                (ISymbol? declared, IEnumerable<SyntaxNode> bodies) = node switch
                {
                    VariableDeclaratorSyntax { Initializer: { } init } v => (model.GetDeclaredSymbol(v), new SyntaxNode[] { init.Value }),
                    PropertyDeclarationSyntax p => (model.GetDeclaredSymbol(p), ReturnedBy(p.ExpressionBody,
                        p.AccessorList?.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)) is { } g
                            ? (SyntaxNode?)g.ExpressionBody ?? g.Body : null)),
                    // A predicate whose parameters all carry an edition is followed too: Is85(edition) answers the edition question
                    // whatever edition it is handed. One that also takes the program's data (InEdition(row), RemovedAt2023(word, level))
                    // answers a question about the program: a verdict, like a condition that reads it beside the edition.
                    MethodDeclarationSyntax m when m.ParameterList.Parameters.All(IsEditionParameter) =>
                        (model.GetDeclaredSymbol(m), ReturnedBy(m.ExpressionBody, m.Body)),
                    LocalFunctionStatementSyntax f when f.ParameterList.Parameters.All(IsEditionParameter) =>
                        (model.GetDeclaredSymbol(f), ReturnedBy(f.ExpressionBody, f.Body)),
                    _ => ((ISymbol?)null, Enumerable.Empty<SyntaxNode>()),
                };
                // An edition question answers a bool or an int, as the seeds do; a string or a record computed from an edition-derived
                // value (a picture folded by edition, a rule record) carries data, not the question, and is not followed.
                if (declared is null || TypeOf(declared) is not { SpecialType: SpecialType.System_Boolean or SpecialType.System_Int32 }) continue;
                var list = bodies.ToList();
                if (list.Count == 0) continue;
                reads.Add((declared, [.. list.SelectMany(b => SymbolsIn(b, model))], list.Any(ComparesEditionName)));
            }
        }
        var derived = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        for (bool grew = true; grew;)
        {
            grew = false;
            foreach (var (declared, symbols, compares) in reads)
                if (!derived.Contains(declared) && (compares || symbols.Any(s => IsSeed(s) || derived.Contains(s))))
                    grew |= derived.Add(declared);
        }

        // ── The choices ──
        var found = new List<string>();
        foreach (var (tree, model) in models)
        {
            if (!scan(tree)) continue;
            // An arm is a string when it is written as one or its type is string (a member, a call); a null arm is no choice of words.
            // A call is a computation, not a text (two renderings of an operand, two screens of a word): Cite would run both.
            // An arm that can answer null (a message or null) is a verdict, as round 2 ruled: whether the rule fires, not its words.
            bool Str(ExpressionSyntax e) => !YieldsNull(e) && (IsString(e)
                || (e is not InvocationExpressionSyntax && model.GetTypeInfo(e).Type?.SpecialType == SpecialType.System_String));
            bool Reads(SyntaxNode condition) =>
                ComparesEditionName(condition) || SymbolsIn(condition, model).Any(s => IsSeed(s) || derived.Contains(s));
            foreach (var node in tree.GetRoot().DescendantNodes())
            {
                string? kind = node switch
                {
                    ConditionalExpressionSyntax c when Str(c.WhenTrue) && Str(c.WhenFalse) && Reads(c.Condition) =>
                        "conditional expression",
                    SwitchExpressionSyntax s when s.Arms.Count(a => Str(a.Expression)) >= 2 && !s.Arms.Any(a => YieldsNull(a.Expression))
                        && (Reads(s.GoverningExpression) || SwitchesNamedEdition(s) || s.Arms.Any(a => a.WhenClause is { } w && Reads(w.Condition))) =>
                        "switch expression",
                    IfStatementSyntax { Else: { } e } i when YieldsString(i.Statement, Str) && YieldsString(e.Statement, Str) && Reads(i.Condition) =>
                        "if/else",
                    // An if with no else whose branch returns a string, followed by a return of another: the else is the rest of the block.
                    IfStatementSyntax { Else: null, Parent: BlockSyntax b } i when i.Statement is var t && Returns(t, Str)
                        && b.Statements.IndexOf(i) is var at && at + 1 < b.Statements.Count && Returns(b.Statements[at + 1], Str)
                        && Reads(i.Condition) =>
                        "if/return",
                    SwitchStatementSyntax w when w.Sections.Count(sec => sec.Statements.Any(st => YieldsString(st, Str))) >= 2
                        && (Reads(w.Expression) || (NamesEdition(w.Expression) && w.Sections.SelectMany(sec => sec.Labels)
                            .Any(l => l.DescendantNodes().OfType<LiteralExpressionSyntax>().Any(IsEditionValue)))) =>
                        "switch statement",
                    _ => null,
                };
                // An interpolation hole whose OWN value an edition-derived member computes (§{Printed(edition)}) chooses the text as surely
                // as a ternary does: a string, or a number spliced into a citation (right after "§" or right before its ":"). A hole
                // that prints the compile edition itself (COBOL-{DialectLevel}) names it, and one that hands the edition to a helper as an
                // argument (Format(DialectLevel, "SR1")) leaves the choice to the helper, which this test reads where it is written.
                kind ??= node is InterpolationSyntax h && !PrintsTheEdition(h.Expression) && HeadOf(h.Expression, model) is { } head
                    && derived.Contains(head)
                    && (model.GetTypeInfo(h.Expression).Type?.SpecialType == SpecialType.System_String || SplicedIntoCitation(h))
                    ? "interpolation" : null;
                if (kind is not null)
                    found.Add($"{tree.FilePath}:{node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}: {kind}");
            }
        }
        return found;
    }

    /// <summary>Whether <paramref name="e"/> can answer null: the null literal, or an arm of a conditional or switch expression that can.</summary>
    private static bool YieldsNull(ExpressionSyntax e) => e switch
    {
        LiteralExpressionSyntax l => l.IsKind(SyntaxKind.NullLiteralExpression),
        ParenthesizedExpressionSyntax p => YieldsNull(p.Expression),
        ConditionalExpressionSyntax c => YieldsNull(c.WhenTrue) || YieldsNull(c.WhenFalse),
        SwitchExpressionSyntax s => s.Arms.Any(a => YieldsNull(a.Expression)),
        _ => false,
    };

    private static ITypeSymbol? TypeOf(ISymbol s) => s switch
    {
        ILocalSymbol l => l.Type,
        IFieldSymbol f => f.Type,
        IPropertySymbol p => p.Type,
        IMethodSymbol m => m.ReturnType,
        _ => null,
    };

    /// <summary>The expressions a property or method returns: its expression body, or each return statement of its block.</summary>
    private static IEnumerable<SyntaxNode> ReturnedBy(ArrowExpressionClauseSyntax? arrow, SyntaxNode? body) =>
        arrow is not null ? [arrow.Expression]
        : body switch
        {
            ArrowExpressionClauseSyntax a => [a.Expression],
            BlockSyntax b => b.DescendantNodes().OfType<ReturnStatementSyntax>().Where(r => r.Expression is not null)
                .Select(r => (SyntaxNode)r.Expression!),
            _ => [],
        };

    private static IEnumerable<ISymbol> SymbolsIn(SyntaxNode node, SemanticModel model) =>
        node.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
            .Select(id => model.GetSymbolInfo(id) is var info && info.Symbol is { } s ? s : info.CandidateSymbols.FirstOrDefault())
            .OfType<ISymbol>();

    /// <summary>A name that says it holds an edition compared, or matched, with an edition constant.</summary>
    private static bool ComparesEditionName(SyntaxNode node) => node.DescendantNodesAndSelf().Any(n => n switch
    {
        BinaryExpressionSyntax b => (IsEditionValue(b.Right) && NamesEdition(b.Left)) || (IsEditionValue(b.Left) && NamesEdition(b.Right)),
        IsPatternExpressionSyntax p => NamesEdition(p.Expression)
            && p.Pattern.DescendantNodesAndSelf().OfType<LiteralExpressionSyntax>().Any(IsEditionValue),
        _ => false,
    });

    private static bool SwitchesNamedEdition(SwitchExpressionSyntax s) => NamesEdition(s.GoverningExpression)
        && s.Arms.Any(a => a.Pattern.DescendantNodesAndSelf().OfType<LiteralExpressionSyntax>().Any(IsEditionValue));

    private static bool IsEditionValue(ExpressionSyntax e) =>
        e is LiteralExpressionSyntax { Token.Value: int v } && EditionValues.Contains(v);

    private static bool NamesEdition(ExpressionSyntax e)
    {
        string name = e switch
        {
            IdentifierNameSyntax id => id.Identifier.ValueText,
            MemberAccessExpressionSyntax m => m.Name.Identifier.ValueText,
            _ => "",
        };
        return name.Contains("edition", StringComparison.OrdinalIgnoreCase) || name.Contains("level", StringComparison.OrdinalIgnoreCase)
            || name.Contains("year", StringComparison.OrdinalIgnoreCase) || SeedNames.Contains(name) || name is "DialectLevel" or "Std";
    }

    private static bool IsString(ExpressionSyntax e) => e switch
    {
        LiteralExpressionSyntax l => l.IsKind(SyntaxKind.StringLiteralExpression),
        InterpolatedStringExpressionSyntax => true,
        ParenthesizedExpressionSyntax p => IsString(p.Expression),
        BinaryExpressionSyntax b when b.IsKind(SyntaxKind.AddExpression) => IsString(b.Left) || IsString(b.Right),
        ConditionalExpressionSyntax c => IsString(c.WhenTrue) || IsString(c.WhenFalse),
        _ => false,
    };

    /// <summary>A parameter that carries an edition: of type EditionInfo or EditionContext, or a number named for one.</summary>
    private static bool IsEditionParameter(ParameterSyntax p) =>
        p.Type?.ToString() is "EditionInfo" or "EditionContext" or "WiseOwl.Cobol.Binding.EditionContext"
        || (p.Type?.ToString() == "int" && NamesEdition(SyntaxFactory.IdentifierName(p.Identifier.ValueText)));

    /// <summary>The symbol whose value a hole is: the method a call invokes, or the member or local it reads.</summary>
    private static ISymbol? HeadOf(ExpressionSyntax e, SemanticModel model) => e switch
    {
        ParenthesizedExpressionSyntax p => HeadOf(p.Expression, model),
        InvocationExpressionSyntax or MemberAccessExpressionSyntax or IdentifierNameSyntax =>
            model.GetSymbolInfo(e) is var info && info.Symbol is { } s ? s : info.CandidateSymbols.FirstOrDefault(),
        _ => null,
    };

    /// <summary>A hole spliced into a citation: the text before it ends in "§", or the text after it begins with ":".</summary>
    private static bool SplicedIntoCitation(InterpolationSyntax h)
    {
        var contents = ((InterpolatedStringExpressionSyntax)h.Parent!).Contents;
        int at = contents.IndexOf(h);
        return (at > 0 && contents[at - 1] is InterpolatedStringTextSyntax before && before.TextToken.ValueText.EndsWith('§'))
            || (at + 1 < contents.Count && contents[at + 1] is InterpolatedStringTextSyntax after && after.TextToken.ValueText.StartsWith(':'));
    }

    /// <summary>A hole that is the compile edition's number itself (<c>Year</c>, <c>DialectLevel</c>, an edition-named int): it names
    /// the edition, as the edition gate's "(targeting COBOL-85)" does, and chooses no text.</summary>
    private static bool PrintsTheEdition(ExpressionSyntax e) => e switch
    {
        IdentifierNameSyntax id => NamesEdition(id),
        MemberAccessExpressionSyntax m => m.Name.Identifier.ValueText is "Year" or "DialectLevel",
        _ => false,
    };

    private static bool Returns(StatementSyntax s, Func<ExpressionSyntax, bool> str) => s switch
    {
        BlockSyntax { Statements: [var only] } => Returns(only, str),
        ReturnStatementSyntax { Expression: { } r } => str(r),
        _ => false,
    };

    private static bool YieldsString(StatementSyntax s, Func<ExpressionSyntax, bool> str) => s switch
    {
        BlockSyntax { Statements: [var only] } => YieldsString(only, str),
        ReturnStatementSyntax { Expression: { } r } => str(r),
        ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax a } => str(a.Right),
        _ => false,
    };

    private static bool IsDecider(SyntaxTree t) =>
        t.FilePath.EndsWith(Path.Combine("WiseOwl.Cobol.Editions", "EditionInfo.cs"), StringComparison.Ordinal);

    /// <summary>The code generator chooses the C# it emits, which no reader of a diagnostic sees: not a message site.</summary>
    private static bool IsCodeGeneration(SyntaxTree t) => t.FilePath.Split(Path.DirectorySeparatorChar).Contains("CodeGen");

    /// <summary>A compile item MSBuild or the source generator produced (obj/, or a generator's path): no message is written there.</summary>
    private static bool IsBuildOutput(SyntaxTree t) =>
        !Path.IsPathRooted(t.FilePath) || t.FilePath.Split(Path.DirectorySeparatorChar).Contains("obj");

    private static bool IsGenerated(SyntaxTree t) =>
        t.FilePath.Split(Path.DirectorySeparatorChar).Contains("Generated") || t.FilePath.EndsWith(".g.cs", StringComparison.Ordinal);

    [Fact]
    public void NoSourceChoosesAStringByEditionOutsideCite()
    {
        string src = TestRepo.Src();
        var offences = Find(SourceCompilations, t => !IsDecider(t) && !IsGenerated(t) && !IsCodeGeneration(t) && !IsBuildOutput(t),
                t => !IsDecider(t))
            .Select(h => Path.GetRelativePath(src, h.Split(':', 2) is [var drive, var rest] && drive.Length == 1 ? $"{drive}:{rest}" : h)
                .Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
            .ToList();
        Assert.True(offences.Count == 0,
            "a site chooses a string by edition, directly or through an alias of the edition, with its own conditional, switch or "
            + "if/else; write it as EditionInfo.Cite(y1985, later[, y2002][, y2014]) (kb/Work PB4950, P2.3a):\n" + string.Join("\n", offences));
    }

    /// <summary>The detector reports what it is for, in each shape and through each kind of alias, so an empty answer above is
    /// evidence.</summary>
    [Fact]
    public void TheDetector_ReportsEachShape_AndEachAlias()
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
        var editions = SourceCompilations.Single(c => c.AssemblyName == "WiseOwl.Cobol.Editions");
        var compilation = CSharpCompilation.Create("planted",
            [CSharpSyntaxTree.ParseText(planted, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview), path: "planted.cs")],
            [.. FrameworkAndPackages, editions.ToMetadataReference()],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, "the planted source does not bind:\n" + string.Join("\n", errors.Take(20)));
        Assert.Equal([
            "planted.cs:10: conditional expression", "planted.cs:13: switch expression", "planted.cs:20: if/else",
            "planted.cs:25: conditional expression", "planted.cs:26: conditional expression", "planted.cs:27: conditional expression",
            "planted.cs:28: conditional expression", "planted.cs:33: conditional expression", "planted.cs:34: conditional expression",
            "planted.cs:36: conditional expression", "planted.cs:39: switch statement", "planted.cs:47: if/return",
            "planted.cs:51: interpolation",
        ], Find([editions, compilation], t => t.FilePath == "planted.cs", _ => true));
    }
}
