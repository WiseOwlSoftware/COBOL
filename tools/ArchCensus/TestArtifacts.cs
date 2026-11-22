// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WiseOwl.Cobol.Tools.ArchCensus;

/// <summary>
/// The test-tree half of the dead-artifact census, as compiler facts: scaffold types (no test method of their own or
/// inherited, no use from outside themselves in ANY assembly that compiles them — a linked <c>tests/_shared</c> file
/// is compiled into every test assembly), and the strings drift tests name, which <c>scripts/arch/census.py</c>
/// resolves against the tree.
/// </summary>
internal static class TestArtifacts
{
    private const string DriftSuffix = "DriftTests.cs";

    /// <summary>
    /// The metadata name of the repository-path locator the drift tests call (<c>tests/_shared/TestRepo.cs</c>): the
    /// locator is the SYMBOL a call binds to, never an identifier's text, so an alias or a <c>using static</c> call is
    /// read and a same-named type elsewhere is not (kb/Work PB3201). RepoDrift's <c>ArchCensusDriftTests</c> pins this
    /// string to <c>typeof(TestRepo).FullName</c>, so a rename of the locator is red at the gate, and
    /// <see cref="DriftLiterals"/> refuses a solution whose drift tests compile no such type instead of recording every
    /// path they build as an unchecked literal.
    /// </summary>
    internal const string LocatorMetadataName = "WiseOwl.Cobol.Tests.Shared.TestRepo";

    public static IReadOnlyList<RawScaffold> Scaffolds(LoadedSolution solution, ReferenceIndex index)
    {
        var byDeclaration = new Dictionary<(string File, string Id), (List<string> Projects, bool Used, int Line, int Lines)>();
        foreach (LoadedProject project in solution.Projects.Where(p => !p.IsCensus))
        {
            var authored = project.Documents.Where(d => !d.Generated).Select(d => d.Tree).ToHashSet();
            foreach (INamedTypeSymbol type in TypeInventory.AllTypes(project.Compilation.Assembly.GlobalNamespace))
            {
                SyntaxReference? first = type.DeclaringSyntaxReferences.FirstOrDefault(r => authored.Contains(r.SyntaxTree));
                if (first is null || IsTestOrDiscovered(type, index) || SymbolKeys.Key(type) is not { } key)
                {
                    continue;
                }

                bool used = index.OutsideUsersOf(key).Count > 0;
                FileLinePositionSpan span = first.SyntaxTree.GetLineSpan(first.Span);
                var at = (solution.Paths.Relative(first.SyntaxTree.FilePath), SymbolKeys.MetadataName(type));
                if (!byDeclaration.TryGetValue(at, out var entry))
                {
                    entry = ([], false, span.StartLinePosition.Line + 1,
                        span.EndLinePosition.Line - span.StartLinePosition.Line + 1);
                }

                entry.Projects.Add(project.Name);
                byDeclaration[at] = entry with { Used = entry.Used || used };
            }
        }

        return [.. byDeclaration.Where(e => !e.Value.Used)
            .Select(e => new RawScaffold(e.Key.Id, e.Value.Projects, e.Key.File, e.Value.Line, e.Value.Lines))
            .OrderBy(s => s.File, StringComparer.Ordinal).ThenBy(s => s.Line)];
    }

    /// <summary>A test class (it or a base declares an xunit-attributed method), a type a framework discovers (an
    /// xunit or BenchmarkDotNet attribute on it), or a type named in a string (xunit resolves its framework by name).</summary>
    private static bool IsTestOrDiscovered(INamedTypeSymbol type, ReferenceIndex index)
    {
        for (INamedTypeSymbol? t = type; t is not null; t = t.BaseType)
        {
            if (t.GetMembers().Any(m => m.GetAttributes().Any(a => IsFrameworkAttribute(a.AttributeClass))))
            {
                return true;
            }
        }

        return type.GetAttributes().Any(a => IsFrameworkAttribute(a.AttributeClass))
               || type.GetMembers(WellKnownMemberNames.EntryPointMethodName).Any(m => m.IsStatic)
               || index.StringLiterals.Contains(type.ToDisplayString())
               || index.StringLiterals.Contains(SymbolKeys.MetadataName(type));
    }

    private static bool IsFrameworkAttribute(INamedTypeSymbol? attribute)
    {
        string name = attribute?.ToDisplayString() ?? "";
        return name.StartsWith("Xunit.", StringComparison.Ordinal) || name.StartsWith("BenchmarkDotNet.", StringComparison.Ordinal);
    }

    /// <summary>Every string literal in a drift test, classified (<see cref="RawLiteral"/>), and every repository
    /// path a call of the locator (<see cref="LocatorMetadataName"/>) with only literal arguments builds. The locator's
    /// prefixes are READ from its own expression bodies (<c>Src(..)</c> is <c>At(["src", .. segments])</c>), never
    /// restated here. Refuses (<see cref="CensusRefusal"/>) a solution with drift tests in which no project compiles the
    /// locator, and a locator none of whose methods has a shape the census reads: either would leave every path a drift
    /// test builds unchecked, with no sign.</summary>
    public static IReadOnlyList<RawLiteral> DriftLiterals(LoadedSolution solution)
    {
        var literals = new SortedSet<RawLiteral>(Comparer<RawLiteral>.Create((a, b) =>
            string.CompareOrdinal($"{a.File}\0{a.Line:D6}\0{a.Kind}\0{a.Text}", $"{b.File}\0{b.Line:D6}\0{b.Kind}\0{b.Text}")));
        bool driftTests = false, located = false;
        foreach (LoadedProject project in solution.Projects.Where(p => !p.IsCensus))
        {
            INamedTypeSymbol? locator = project.Compilation.Assembly.GetTypeByMetadataName(LocatorMetadataName);
            IReadOnlyDictionary<string, string> prefixes = locator is null ? new Dictionary<string, string>() : LocatorPrefixes(locator);
            located |= locator is not null;
            foreach (LoadedDocument document in project.Documents.Where(d => !d.Generated
                         && d.RepoPath.EndsWith(DriftSuffix, StringComparison.Ordinal)))
            {
                driftTests = true;
                SemanticModel? model = null;
                foreach (SyntaxNode node in document.Tree.GetRoot().DescendantNodes())
                {
                    int line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    if (node is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
                    {
                        literals.Add(new RawLiteral(document.RepoPath, line, literal.Token.ValueText,
                            InStaticList(literal) ? "list" : "other"));
                    }
                    else if (locator is not null
                             && node is InvocationExpressionSyntax invocation
                             && invocation.ArgumentList.Arguments.Count > 0
                             && invocation.ArgumentList.Arguments.All(a => a.Expression.IsKind(SyntaxKind.StringLiteralExpression))
                             && (model ??= project.Compilation.GetSemanticModel(document.Tree)).GetSymbolInfo(invocation).Symbol
                                 is IMethodSymbol method
                             && SymbolEqualityComparer.Default.Equals(method.ContainingType, locator)
                             && prefixes.TryGetValue(method.Name, out string? prefix))
                    {
                        IEnumerable<string> parts = invocation.ArgumentList.Arguments
                            .Select(a => ((LiteralExpressionSyntax)a.Expression).Token.ValueText);
                        literals.Add(new RawLiteral(document.RepoPath, line, prefix + string.Join("/", parts), "path"));
                    }
                }
            }
        }

        if (driftTests && !located)
        {
            throw new CensusRefusal($"no project of the solution compiles the drift tests' path locator {LocatorMetadataName} "
                                    + "(renamed or moved? re-point TestArtifacts.LocatorMetadataName): every path a drift "
                                    + "test builds would be recorded unchecked");
        }

        return [.. literals];
    }

    /// <summary>An element of the collection that initializes a static field — an exemption or allow list.</summary>
    private static bool InStaticList(LiteralExpressionSyntax literal) =>
        literal.Parent is ExpressionElementSyntax or InitializerExpressionSyntax
        && literal.FirstAncestorOrSelf<FieldDeclarationSyntax>() is { } field
        && field.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword) || m.IsKind(SyntaxKind.ConstKeyword))
        && literal.Ancestors().Any(a => a is CollectionExpressionSyntax or InitializerExpressionSyntax);

    /// <summary>
    /// The path prefix of each locator method whose body is <c>At(...)</c> itself (<c>Path.Combine([Root, ..
    /// segments])</c>, the empty prefix) or a call to another such method with literal segments before the spread —
    /// resolved from the locator's own source (every partial declaration of the symbol). A method of any other shape
    /// is left out, so its calls are reported only as their literal arguments; a locator with NO method of these shapes
    /// is a refusal, never an empty map.
    /// </summary>
    private static IReadOnlyDictionary<string, string> LocatorPrefixes(INamedTypeSymbol locator)
    {
        var bodies = new Dictionary<string, ExpressionSyntax>(StringComparer.Ordinal);
        foreach (TypeDeclarationSyntax declaration in locator.DeclaringSyntaxReferences.Select(r => r.GetSyntax())
                     .OfType<TypeDeclarationSyntax>())
        {
            foreach (MethodDeclarationSyntax method in declaration.Members.OfType<MethodDeclarationSyntax>())
            {
                if (method.ExpressionBody?.Expression is { } body)
                {
                    bodies[method.Identifier.Text] = body;
                }
            }
        }

        var prefixes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string name in bodies.Keys)
        {
            if (Resolve(name, bodies, depth: 0) is { } prefix)
            {
                prefixes[name] = prefix;
            }
        }

        return prefixes.Count > 0
            ? prefixes
            : throw new CensusRefusal($"the path locator {LocatorMetadataName} has no method of a shape the census reads "
                                      + "(Path.Combine([Root, .. segments]), or a call of one with literal segments "
                                      + "before the spread): TestArtifacts.Resolve must learn its new shape");
    }

    private static string? Resolve(string method, Dictionary<string, ExpressionSyntax> bodies, int depth)
    {
        if (depth > 8 || !bodies.TryGetValue(method, out ExpressionSyntax? body)
            || body is not InvocationExpressionSyntax { ArgumentList.Arguments: [{ Expression: CollectionExpressionSyntax segments }] } call)
        {
            return null;
        }

        string callee = call.Expression switch
        {
            IdentifierNameSyntax id => id.Identifier.Text,
            MemberAccessExpressionSyntax m => m.Name.Identifier.Text,
            _ => "",
        };
        if (segments.Elements.Count == 0 || segments.Elements[^1] is not SpreadElementSyntax)
        {
            return null;
        }

        if (callee == "Combine")
        {
            // Path.Combine([Root, .. segments]): the root itself, with no literal segment.
            return segments.Elements is [ExpressionElementSyntax { Expression: IdentifierNameSyntax }, _] ? "" : null;
        }

        var literal = segments.Elements.SkipLast(1).ToList();
        if (!literal.All(e => e is ExpressionElementSyntax { Expression: LiteralExpressionSyntax }))
        {
            return null;
        }

        string own = string.Concat(literal.Select(e =>
            ((LiteralExpressionSyntax)((ExpressionElementSyntax)e).Expression).Token.ValueText + "/"));
        return Resolve(callee, bodies, depth + 1) is { } outer ? outer + own : null;
    }
}
