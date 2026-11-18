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

/// <summary>⛔ ONE DECIDER OF A MESSAGE'S WORDS BY EDITION (kb/Work PB4950 review rounds 1 to 3; docs/VERSION_TEST_MATRIX_DESIGN.md
/// P2.3a). <c>EditionInfo.Cite(y1985, later, y2002, y2014)</c> is the one choice of a citation, or of the words a message quotes,
/// by edition. A site that spells its own choice is a second decider the next edition's text has to be threaded through by hand.
/// <para>This test compiles every <c>src/**/*.cs</c> as ONE compilation and asks its SEMANTIC MODEL which values are
/// EDITION-DERIVED, as a fixed point. The seeds are the members of <see cref="EditionInfo"/> that answer which edition or a number
/// of it (each public instance <c>int</c> property, each <c>bool</c> method of an <c>int</c>: <c>Has</c>), found by reflection, plus
/// the compiler's mirror <c>DialectLevel</c> and the CLI's <c>Std</c>. A local or field whose initializer, or a parameter-free
/// property or method whose return expression, reads an edition-derived symbol (or compares a name that says edition, level or
/// year with an edition of <see cref="EditionInfo.All"/>) is edition-derived too, when it is a <c>bool</c> or an <c>int</c> as the seeds
/// are (a string or record computed from one carries data, not the edition question); the derivation repeats until nothing changes, across
/// files and projects. So an alias (<c>bool is1985 = Edition.DialectLevel &lt; 2002</c>, <c>Prints2002</c>, a chain of them) is
/// seen wherever it is read. <see cref="EditionInfo"/>'s own <c>bool</c> properties are named rule facts
/// (<c>SubscriptedClauseOperands</c>) and are neither seeds nor derived: a choice on one is a verdict about the program.</para>
/// <para>It then refuses, outside <c>EditionInfo.cs</c>, generated code and the code generator (whose strings are emitted C#, not
/// messages), every conditional expression, switch expression or
/// if/else that chooses between two strings (a literal, an interpolated string, a concatenation holding one, or any expression of
/// type string that is not a call: a call computes, and Cite would run both arms) on a condition
/// that reads an edition-derived value. A program question beside it stays a ternary: <c>x &amp;&amp; is1985 ? a : b</c> is
/// written <c>x ? Cite(a, b) : b</c>.</para></summary>
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

    /// <summary>The metadata a compilation of the sources needs: the framework and every non-WiseOwl assembly beside this test.</summary>
    private static IEnumerable<MetadataReference> FrameworkAndThirdParty() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Concat(Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll")
                .Where(f => !Path.GetFileName(f).StartsWith("WiseOwl.", StringComparison.Ordinal)))
            .DistinctBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .Select(f => MetadataReference.CreateFromFile(f));

    /// <summary>Every ad-hoc edition choice of a string in the trees <paramref name="scan"/> accepts, as "path:line: kind".</summary>
    public static IReadOnlyList<string> Find(CSharpCompilation compilation, Func<SyntaxTree, bool> scan, Func<SyntaxTree, bool> derives)
    {
        var models = compilation.SyntaxTrees.ToDictionary(t => t, t => compilation.GetSemanticModel(t));

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
                    MethodDeclarationSyntax { ParameterList.Parameters.Count: 0 } m => (model.GetDeclaredSymbol(m), ReturnedBy(m.ExpressionBody, m.Body)),
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
                    _ => null,
                };
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

    private static bool IsGenerated(SyntaxTree t) =>
        t.FilePath.Split(Path.DirectorySeparatorChar).Contains("Generated") || t.FilePath.EndsWith(".g.cs", StringComparison.Ordinal);

    [Fact]
    public void NoSourceChoosesAStringByEditionOutsideCite()
    {
        string src = TestRepo.Src();
        var trees = Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(f => !Path.GetRelativePath(src, f).Split(Path.DirectorySeparatorChar).Any(seg => seg is "bin" or "obj"))
            .Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview),
                path: f))
            .ToList();
        var compilation = CSharpCompilation.Create("src", trees, FrameworkAndThirdParty(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));
        var offences = Find(compilation, t => !IsDecider(t) && !IsGenerated(t) && !IsCodeGeneration(t), t => !IsDecider(t))
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
            }
            """;
        var compilation = CSharpCompilation.Create("planted", [CSharpSyntaxTree.ParseText(planted, path: "planted.cs")],
            [.. FrameworkAndThirdParty(), MetadataReference.CreateFromFile(typeof(EditionInfo).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Assert.Equal([
            "planted.cs:8: conditional expression", "planted.cs:11: switch expression", "planted.cs:18: if/else",
            "planted.cs:23: conditional expression", "planted.cs:24: conditional expression", "planted.cs:25: conditional expression",
            "planted.cs:26: conditional expression", "planted.cs:31: conditional expression",
        ], Find(compilation, _ => true, _ => true));
    }
}
