// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.CodeGen;
using WiseOwl.Cobol.Tests.Shared;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>What the five rules of <see cref="NameTableOwnership"/> read from a compilation: which namespaces hold the
/// binder's symbol types, and the full names of the one store (<c>SymbolTable</c>), its name key (<c>CobolName</c>), the
/// declaration answer (<c>DeclarationFact</c>), the name-equality helper (<c>CobolNames</c>) and the binder types that carry
/// no symbol (<see cref="Opaque"/>: an entry's opaque number). The product's shape is
/// <see cref="Product"/>; the planted self-test names its own.</summary>
internal sealed record NameTableShape(
    IReadOnlyList<string> BinderNamespaces,
    string SymbolTable,
    string CobolName,
    string DeclarationFact,
    string CobolNames,
    IReadOnlyList<string> Opaque)
{
    /// <summary>The types only <c>SymbolTable</c> makes: the name key and the answer of each recording point (an element's
    /// <c>Identify</c> and the compilation group tier's <c>ClassNames.Lookup</c>), so a symbol reached from a written name has
    /// come through one of them.</summary>
    public IReadOnlyList<string> MadeOnlyByTable { get; init; } = [CobolName];

    public static NameTableShape Product { get; } = new(
        ["WiseOwl.Cobol.Binding", "WiseOwl.Cobol.Compiler.Oo"],
        "WiseOwl.Cobol.Binding.SymbolTable",
        "WiseOwl.Cobol.Binding.CobolName",
        "WiseOwl.Cobol.Binding.DeclarationFact",
        "WiseOwl.Cobol.Runtime.CobolNames",
        ["WiseOwl.Cobol.Binding.SymbolId"])
    {
        MadeOnlyByTable = ["WiseOwl.Cobol.Binding.CobolName", "WiseOwl.Cobol.Binding.IdentifiedName", "WiseOwl.Cobol.Compiler.Oo.OoNameResult"],
    };
}

/// <summary>One violation of a rule. <see cref="Key"/> is the line-independent identity the committed classifications
/// match (a member's signature for rule 3, a comparison site for rule 5); <see cref="Where"/> is the file and line for
/// the reader.</summary>
internal sealed record NameFinding(int Rule, string Key, string Where, string What)
{
    public override string ToString() => $"rule ({Rule}) {Where}: {What} [{Key}]";
}

/// <summary>
/// The five rules of <c>NameTableOwnershipDriftTests</c> (docs/rearchitecture/DESIGN-listing.md section 6.7.1), applied by
/// symbol through Roslyn's semantic model to a compilation of the compiler's own sources. Nothing here lists a table, a
/// lookup or a comparison: each rule is a property of a type or a call that the semantic model sees, so a new table, a new
/// lookup or a new name comparison anywhere is caught by what it IS.
/// <list type="number">
/// <item>No field or property outside <c>SymbolTable</c> is a dictionary or lookup keyed by <c>CobolName</c> or
/// <c>string</c> whose value holds a binder type (or an open type parameter).</item>
/// <item>No non-private member takes a <c>CobolName</c> and returns, or gives through an <c>out</c> parameter, a binder
/// type.</item>
/// <item>No non-private member takes a <c>string</c> and returns a binder type, unless the committed classification names
/// it with the reason its string is no COBOL name.</item>
/// <item>Every non-private member of <c>SymbolTable.Declarations</c> answers a <c>DeclarationFact</c>, a <c>bool</c> or a
/// collection of <c>DeclarationFact</c>, and has no <c>out</c> parameter of another type.</item>
/// <item>No <c>CobolNames.Same</c> call, no <c>CobolNames.Comparer</c> use and no <c>==</c>, <c>!=</c> or
/// <c>string.Equals</c> of a binder symbol's name property (or a <c>CobolNames.Fold</c> / <c>UpperFold</c> result)
/// outside <c>SymbolTable</c>, unless the committed classification names the site with a non-resolution class.</item>
/// </list>
/// "Inside <c>SymbolTable</c>" is a member of <c>SymbolTable</c> or of a type nested in it. Rules (2) and (3) ask about
/// every member whose EFFECTIVE accessibility is not private, the table's own included: its one lookup is
/// <c>Identify</c>, which takes the parser's context, so a second public lookup on the table is as red as one outside it.
/// A <c>CobolName</c> is made only inside <c>SymbolTable</c> (rule (2)'s companion, <see cref="CobolNameCreations"/>).
/// </summary>
internal static class NameTableOwnership
{
    /// <summary>The compiler's own sources (<paramref name="sources"/>: each file's path relative to the project and its
    /// text, read by the caller), compiled by Roslyn against the framework's reference set and every assembly the compiler
    /// references (resolved through the ones this host has loaded), with the implicit usings the SDK adds and the compiler's
    /// own source generator (<paramref name="generatorDll"/>) run over them, as the build runs it. The caller lists the
    /// files, so the test class names the tree it sweeps (the tests tree's walk guard, <c>TrackedTreeScopeDriftTests</c>,
    /// and the <c>Sweep</c> trait's rule, <c>scripts/spec/drift_rules.py</c>, both read it there).</summary>
    public static CSharpCompilation CompileCompiler(IEnumerable<(string Path, string Text)> sources, string generatorDll)
    {
        var options = new CSharpParseOptions(LanguageVersion.Preview);
        var trees = new List<SyntaxTree>
        {
            CSharpSyntaxTree.ParseText("""
                global using System;
                global using System.Collections.Generic;
                global using System.IO;
                global using System.Linq;
                global using System.Net.Http;
                global using System.Threading;
                global using System.Threading.Tasks;
                """, options, path: "GlobalUsings.g.cs"),
        };
        trees.AddRange(sources.Select(s => CSharpSyntaxTree.ParseText(s.Text, options, path: s.Path)));

        var compilation = CSharpCompilation.Create("WiseOwl.Cobol.Compiler", trees,
            FrameworkReferences().Concat(CompilerReferences()),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable,
                allowUnsafe: true));

        // The compiler's own source generator (the bound-tree visitors) runs as it does in the build, so the generated
        // members the sources call are bound too.
        ISourceGenerator[] generators = [.. ScratchDirectory.LoadAssembly(generatorDll).GetTypes()
            .Where(t => !t.IsAbstract && typeof(IIncrementalGenerator).IsAssignableFrom(t))
            .Select(t => ((IIncrementalGenerator)Activator.CreateInstance(t)!).AsSourceGenerator())];
        if (generators.Length == 0)
            throw new InvalidOperationException($"{generatorDll} holds no incremental generator");
        CSharpGeneratorDriver.Create(generators, parseOptions: options)
            .RunGeneratorsAndUpdateCompilation(compilation, out Compilation generated, out _);
        return (CSharpCompilation)generated;
    }

    /// <summary>The framework's reference set the compiler compiles against: the set the backend compiles a COBOL program
    /// against (<see cref="AssemblyPackager.ReferenceAssemblyPaths"/>).</summary>
    public static IEnumerable<MetadataReference> FrameworkReferences() =>
        AssemblyPackager.ReferenceAssemblyPaths.Order(StringComparer.Ordinal).Select(p => (MetadataReference)MetadataReference.CreateFromFile(p));

    /// <summary>Every assembly the compiler references, directly or through another, that is not the framework's: the
    /// product's own (front end, editions, listing, runtime), Roslyn and ANTLR, each as this host loaded it.</summary>
    private static IEnumerable<MetadataReference> CompilerReferences()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Stack<System.Reflection.Assembly>([typeof(SymbolTable).Assembly]);
        var found = new List<string>();
        while (pending.Count > 0)
        {
            foreach (var name in pending.Pop().GetReferencedAssemblies())
            {
                if (name.Name is null || IsFramework(name.Name) || !seen.Add(name.Name))
                    continue;
                var loaded = System.Reflection.Assembly.Load(name);
                found.Add(loaded.Location);
                pending.Push(loaded);
            }
        }
        return found.Order(StringComparer.Ordinal).Select(p => (MetadataReference)MetadataReference.CreateFromFile(p));

        static bool IsFramework(string name) =>
            name is "mscorlib" or "netstandard" or "Microsoft.CSharp" || name.StartsWith("System", StringComparison.Ordinal)
            || name.StartsWith("Microsoft.Win32", StringComparison.Ordinal) || name.StartsWith("Microsoft.VisualBasic", StringComparison.Ordinal);
    }

    /// <summary>Every rule's findings, in rule order and then source order.</summary>
    public static IReadOnlyList<NameFinding> Findings(Compilation compilation, NameTableShape shape)
    {
        var context = new Context(compilation, shape);
        return [.. Tables(context), .. Lookups(context), .. CobolNameCreations(context), .. Declarations(context), .. Comparisons(context)];
    }

    private sealed class Context(Compilation compilation, NameTableShape shape)
    {
        public Compilation Compilation { get; } = compilation;
        public NameTableShape Shape { get; } = shape;
        public INamedTypeSymbol? SymbolTable { get; } = compilation.GetTypeByMetadataName(shape.SymbolTable);
        public INamedTypeSymbol? CobolName { get; } = compilation.GetTypeByMetadataName(shape.CobolName);
        public INamedTypeSymbol? DeclarationFact { get; } = compilation.GetTypeByMetadataName(shape.DeclarationFact);
        public INamedTypeSymbol? CobolNames { get; } = compilation.GetTypeByMetadataName(shape.CobolNames);

        public IEnumerable<INamedTypeSymbol> SourceTypes()
        {
            var stack = new Stack<INamespaceOrTypeSymbol>();
            stack.Push(Compilation.Assembly.GlobalNamespace);
            var found = new List<INamedTypeSymbol>();
            while (stack.Count > 0)
            {
                foreach (ISymbol member in stack.Pop().GetMembers())
                {
                    if (member is INamespaceSymbol ns)
                        stack.Push(ns);
                    else if (member is INamedTypeSymbol type && type.Locations.Any(l => l.IsInSource))
                    {
                        found.Add(type);
                        stack.Push(type);
                    }
                }
            }
            return found.OrderBy(t => t.ToDisplayString(), StringComparer.Ordinal);
        }

        /// <summary>A type of the binder that can carry a symbol: declared in a binder namespace, and neither an enum
        /// (a value, never a symbol) nor the name key, the declaration fact or an opaque type of the shape.</summary>
        public bool IsBinderType(ITypeSymbol type) =>
            type is INamedTypeSymbol named && named.Locations.Any(l => l.IsInSource) && named.TypeKind != TypeKind.Enum
            && !SymbolEqualityComparer.Default.Equals(named, CobolName) && !SymbolEqualityComparer.Default.Equals(named, DeclarationFact)
            && !Shape.Opaque.Contains(named.ToDisplayString())
            && Shape.BinderNamespaces.Any(ns => NamespaceOf(named) == ns || NamespaceOf(named).StartsWith(ns + ".", StringComparison.Ordinal));

        /// <summary>The type is, or holds in a type argument, an array element or a tuple element, a binder type or an
        /// open type parameter.</summary>
        public bool HoldsBinderType(ITypeSymbol type, int depth = 0)
        {
            if (depth > 8)
                return false;
            return type switch
            {
                ITypeParameterSymbol => true,
                IArrayTypeSymbol array => HoldsBinderType(array.ElementType, depth + 1),
                INamedTypeSymbol named => IsBinderType(named)
                    || named.TypeArguments.Any(a => HoldsBinderType(a, depth + 1))
                    || (named.IsTupleType && named.TupleElements.Any(e => HoldsBinderType(e.Type, depth + 1))),
                _ => false,
            };
        }

        public bool IsNameKey(ITypeSymbol type) =>
            type.SpecialType == SpecialType.System_String || SymbolEqualityComparer.Default.Equals(type, CobolName);

        public bool InsideSymbolTable(ISymbol? symbol)
        {
            if (SymbolTable is null)
                return false;
            for (INamedTypeSymbol? t = symbol as INamedTypeSymbol ?? symbol?.ContainingType; t is not null; t = t.ContainingType)
            {
                if (SymbolEqualityComparer.Default.Equals(t.OriginalDefinition, SymbolTable))
                    return true;
            }
            return false;
        }
    }

    private static string NamespaceOf(INamedTypeSymbol type) => type.ContainingNamespace?.ToDisplayString() ?? "";

    private static string Where(ISymbol symbol)
    {
        Location? at = symbol.Locations.FirstOrDefault(l => l.IsInSource);
        return at is null ? symbol.ToDisplayString() : Where(at);
    }

    private static string Where(Location at)
    {
        FileLinePositionSpan span = at.GetLineSpan();
        return $"{span.Path}:{span.StartLinePosition.Line + 1}";
    }

    private static bool IsEffectivelyPrivate(ISymbol symbol)
    {
        for (ISymbol? s = symbol; s is not null and not INamespaceSymbol; s = s.ContainingSymbol)
        {
            if (s.DeclaredAccessibility == Accessibility.Private)
                return true;
        }
        return false;
    }

    // ---- rule (1): no name table outside SymbolTable ------------------------------------------------------------------

    private static IEnumerable<NameFinding> Tables(Context c)
    {
        foreach (INamedTypeSymbol type in c.SourceTypes())
        {
            if (c.InsideSymbolTable(type))
                continue;
            foreach (ISymbol member in type.GetMembers())
            {
                if (member.IsImplicitlyDeclared)
                    continue;
                ITypeSymbol? memberType = member switch
                {
                    IFieldSymbol { AssociatedSymbol: null } f => f.Type,
                    IPropertySymbol p => p.Type,
                    _ => null,
                };
                if (memberType is not null && NameTableValue(c, memberType) is { } value)
                {
                    yield return new NameFinding(1, member.ToDisplayString(), Where(member),
                        $"{member.ToDisplayString()} is a table keyed by a name whose value holds {value.ToDisplayString()}: it belongs in SymbolTable");
                }
            }
        }
    }

    /// <summary>The value type when <paramref name="type"/> is, or implements, a dictionary or lookup keyed by a name
    /// whose value holds a binder type; otherwise null.</summary>
    private static ITypeSymbol? NameTableValue(Context c, ITypeSymbol type)
    {
        IEnumerable<INamedTypeSymbol> shapes = type is INamedTypeSymbol self ? [self, .. type.AllInterfaces] : type.AllInterfaces;
        foreach (INamedTypeSymbol shape in shapes)
        {
            string name = shape.OriginalDefinition.ToDisplayString();
            if (name is "System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>" or "System.Collections.Generic.IDictionary<TKey, TValue>"
                    or "System.Linq.ILookup<TKey, TElement>"
                && shape.TypeArguments.Length == 2 && c.IsNameKey(shape.TypeArguments[0]) && c.HoldsBinderType(shape.TypeArguments[1]))
            {
                return shape.TypeArguments[1];
            }
        }
        return null;
    }

    // ---- rules (2) and (3): no lookup by name but Identify -----------------------------------------------------------

    private static IEnumerable<NameFinding> Lookups(Context c)
    {
        foreach (INamedTypeSymbol type in c.SourceTypes())
        {
            foreach (ISymbol member in type.GetMembers())
            {
                if (member.IsImplicitlyDeclared || IsEffectivelyPrivate(member))
                    continue;
                (IReadOnlyList<IParameterSymbol> parameters, ITypeSymbol? returns) = member switch
                {
                    IMethodSymbol m when m.MethodKind is MethodKind.Ordinary or MethodKind.UserDefinedOperator or MethodKind.Conversion
                        => ((IReadOnlyList<IParameterSymbol>)m.Parameters, m.ReturnsVoid ? null : m.ReturnType),
                    IPropertySymbol { IsIndexer: true } p => (p.Parameters, p.Type),
                    _ => ([], null),
                };
                if (parameters.Count == 0)
                    continue;
                bool givesSymbol = (returns is not null && c.HoldsBinderType(returns))
                    || parameters.Any(p => p.RefKind == RefKind.Out && c.HoldsBinderType(p.Type));
                if (!givesSymbol)
                    continue;
                string key = member.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
                if (parameters.Any(p => p.RefKind != RefKind.Out && SymbolEqualityComparer.Default.Equals(p.Type, c.CobolName)))
                {
                    yield return new NameFinding(2, key, Where(member),
                        $"{key} takes a CobolName and gives a binder symbol: a lookup by name, which only SymbolTable.Identify is");
                }
                else if (parameters.Any(p => p.RefKind != RefKind.Out && p.Type.SpecialType == SpecialType.System_String))
                {
                    yield return new NameFinding(3, key, Where(member),
                        $"{key} takes a string ({string.Join(", ", parameters.Where(p => p.Type.SpecialType == SpecialType.System_String).Select(p => p.Name))}) "
                        + "and gives a binder symbol: convert it to SymbolTable.Identify, or classify its string as no COBOL name");
                }
            }
        }
    }

    /// <summary>A <c>CobolName</c> is made only by <c>SymbolTable</c> (section 6.7.1, "The name key is a type"), and so is each
    /// lookup's answer (<c>IdentifiedName</c>, <c>OoNameResult</c>): an object creation, an implicit <c>new(...)</c>, a
    /// <c>with</c> expression (a copy with members replaced is a new answer, step 6b OO lane review lead R2-1) anywhere else is a
    /// key or an answer made without the table, and so is a <c>default</c> of the name key (a key with no word). A <c>default</c>
    /// of an answer is the empty answer, which names no symbol (the "nothing identified" a caller returns), so it makes nothing.</summary>
    private static IEnumerable<NameFinding> CobolNameCreations(Context c)
    {
        if (c.CobolName is null)
        {
            yield return new NameFinding(2, c.Shape.CobolName, c.Shape.CobolName, $"the name key {c.Shape.CobolName} does not exist");
            yield break;
        }
        INamedTypeSymbol[] tableMade = [.. c.Shape.MadeOnlyByTable.Select(c.Compilation.GetTypeByMetadataName).OfType<INamedTypeSymbol>()];
        foreach (SyntaxTree tree in c.Compilation.SyntaxTrees)
        {
            SemanticModel model = c.Compilation.GetSemanticModel(tree);
            foreach (SyntaxNode node in tree.GetRoot().DescendantNodes().Where(n => n is BaseObjectCreationExpressionSyntax
                or WithExpressionSyntax or DefaultExpressionSyntax || n.IsKind(SyntaxKind.DefaultLiteralExpression)))
            {
                TypeInfo info = model.GetTypeInfo(node);
                bool isDefault = node is DefaultExpressionSyntax || node.IsKind(SyntaxKind.DefaultLiteralExpression);
                if ((info.ConvertedType ?? info.Type) is INamedTypeSymbol made && (!isDefault || SymbolEqualityComparer.Default.Equals(made, c.CobolName))
                    && tableMade.FirstOrDefault(t => SymbolEqualityComparer.Default.Equals(made, t)) is { } kind
                    && !c.InsideSymbolTable(model.GetEnclosingSymbol(node.SpanStart)) && !InsideType(model, node, kind))
                {
                    yield return new NameFinding(2, Normalize(node.ToString()), Where(node.GetLocation()),
                        $"a {kind.Name} is made outside SymbolTable: only the table makes a name key or a lookup's answer");
                }
            }
        }
    }

    private static bool InsideType(SemanticModel model, SyntaxNode node, INamedTypeSymbol type)
    {
        for (INamedTypeSymbol? t = model.GetEnclosingSymbol(node.SpanStart)?.ContainingType; t is not null; t = t.ContainingType)
        {
            if (SymbolEqualityComparer.Default.Equals(t, type))
                return true;
        }
        return false;
    }

    // ---- rule (4): Declarations answers facts -----------------------------------------------------------------------

    private static IEnumerable<NameFinding> Declarations(Context c)
    {
        INamedTypeSymbol? declarations = c.SymbolTable?.GetTypeMembers("Declarations").FirstOrDefault();
        if (declarations is null || c.DeclarationFact is null)
        {
            yield return new NameFinding(4, c.Shape.SymbolTable + ".Declarations", c.Shape.SymbolTable,
                $"{c.Shape.SymbolTable}.Declarations or {c.Shape.DeclarationFact} does not exist: the data division's declaration questions have no fact-only home");
            yield break;
        }
        foreach (ISymbol member in declarations.GetMembers())
        {
            if (member.IsImplicitlyDeclared || IsEffectivelyPrivate(member) || member is IMethodSymbol { MethodKind: not MethodKind.Ordinary })
                continue;
            ITypeSymbol? answer = member switch
            {
                IMethodSymbol m => m.ReturnsVoid ? null : m.ReturnType,
                IPropertySymbol p => p.Type,
                IFieldSymbol f => f.Type,
                _ => null,
            };
            IEnumerable<IParameterSymbol> outs = member is IMethodSymbol method ? method.Parameters.Where(p => p.RefKind == RefKind.Out) : [];
            string key = member.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
            if (answer is null || !IsFactAnswer(c, answer))
            {
                yield return new NameFinding(4, key, Where(member),
                    $"{key} answers {answer?.ToDisplayString() ?? "nothing"}: a declaration question answers only DeclarationFact, bool or a collection of DeclarationFact");
            }
            foreach (IParameterSymbol p in outs.Where(p => !IsFactAnswer(c, p.Type)))
                yield return new NameFinding(4, key, Where(member), $"{key} gives {p.Type.ToDisplayString()} through out {p.Name}");
        }
    }

    private static bool IsFactAnswer(Context c, ITypeSymbol type) =>
        type.SpecialType == SpecialType.System_Boolean
        || SymbolEqualityComparer.Default.Equals(type, c.DeclarationFact)
        || (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable
            && SymbolEqualityComparer.Default.Equals(nullable.TypeArguments[0], c.DeclarationFact))
        || type.AllInterfaces.Concat(type is INamedTypeSymbol n ? [n] : []).Any(i =>
            i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T
            && SymbolEqualityComparer.Default.Equals(i.TypeArguments[0], c.DeclarationFact));

    // ---- rule (5): name equality is the table's ---------------------------------------------------------------------

    private static IEnumerable<NameFinding> Comparisons(Context c)
    {
        if (c.CobolNames is null)
            yield break;
        foreach (SyntaxTree tree in c.Compilation.SyntaxTrees)
        {
            SemanticModel model = c.Compilation.GetSemanticModel(tree);
            var seen = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (SyntaxNode node in tree.GetRoot().DescendantNodes())
            {
                string? kind = ComparisonKind(c, model, node);
                if (kind is null)
                    continue;
                ISymbol? enclosing = Outermost(model.GetEnclosingSymbol(node.SpanStart));
                // The name key's own equality is the one definition of name equality the table's keys use.
                if (c.InsideSymbolTable(enclosing) || SymbolEqualityComparer.Default.Equals(enclosing?.ContainingType, c.CobolName))
                    continue;
                string owner = enclosing is null ? "" : $"{enclosing.ContainingType?.ToDisplayString() ?? ""}.{enclosing.Name}";
                string baseKey = $"{kind} {tree.FilePath} {owner} {Normalize(node.ToString())}";
                int ordinal = seen[baseKey] = seen.GetValueOrDefault(baseKey) + 1;
                string key = ordinal == 1 ? baseKey : $"{baseKey} #{ordinal}";
                yield return new NameFinding(5, key, Where(node.GetLocation()),
                    $"{kind}: a comparison of COBOL names outside SymbolTable (move it into Identify or Declarations, or classify it)");
            }
        }
    }

    /// <summary>SAME for a <c>CobolNames.Same</c> call, CMP for a use of <c>CobolNames.Comparer</c>, NAMEEQ for an
    /// equality of a name operand; null for anything else.</summary>
    private static string? ComparisonKind(Context c, SemanticModel model, SyntaxNode node)
    {
        switch (node)
        {
            case InvocationExpressionSyntax call when model.GetSymbolInfo(call).Symbol is IMethodSymbol m:
                if (IsCobolNamesMember(c, m, "Same"))
                    return "SAME";
                if (m.Name == "Equals" && m.ContainingType.SpecialType == SpecialType.System_String)
                {
                    IEnumerable<ExpressionSyntax> operands = call.ArgumentList.Arguments.Select(a => a.Expression);
                    if (!m.IsStatic && call.Expression is MemberAccessExpressionSyntax access)
                        operands = operands.Prepend(access.Expression);
                    if (operands.Any(o => IsNameOperand(c, model, o)))
                        return "NAMEEQ";
                }
                return null;
            case IdentifierNameSyntax id when id.Identifier.ValueText == "Comparer"
                    && model.GetSymbolInfo(id).Symbol is IPropertySymbol p && IsCobolNamesMember(c, p, "Comparer"):
                return "CMP";
            case BinaryExpressionSyntax binary when binary.Kind() is SyntaxKind.EqualsExpression or SyntaxKind.NotEqualsExpression:
                return IsNameOperand(c, model, binary.Left) || IsNameOperand(c, model, binary.Right) ? "NAMEEQ" : null;
            default:
                return null;
        }
    }

    private static bool IsCobolNamesMember(Context c, ISymbol member, string name) =>
        member.Name == name && SymbolEqualityComparer.Default.Equals(member.ContainingType?.OriginalDefinition, c.CobolNames);

    /// <summary>A binder symbol's <c>string</c> name property or field (<c>Name</c>, or a name ending in <c>Name</c>), or
    /// a <c>CobolNames.Fold</c> / <c>UpperFold</c> result.</summary>
    private static bool IsNameOperand(Context c, SemanticModel model, ExpressionSyntax expression)
    {
        ExpressionSyntax e = expression is PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression } bang
            ? bang.Operand : expression;
        ISymbol? symbol = model.GetSymbolInfo(e).Symbol;
        if (symbol is IPropertySymbol or IFieldSymbol)
        {
            ITypeSymbol type = symbol is IPropertySymbol p ? p.Type : ((IFieldSymbol)symbol).Type;
            return type.SpecialType == SpecialType.System_String && symbol.Name.EndsWith("Name", StringComparison.Ordinal)
                && symbol.ContainingType is { } owner && c.IsBinderType(owner);
        }
        return symbol is IMethodSymbol m
            && ((IsCobolNamesMember(c, m, "Fold") && m.ReturnType.SpecialType == SpecialType.System_String) || IsCobolNamesMember(c, m, "UpperFold"));
    }

    private static ISymbol? Outermost(ISymbol? symbol)
    {
        while (symbol is IMethodSymbol { MethodKind: MethodKind.AnonymousFunction or MethodKind.LocalFunction or MethodKind.LambdaMethod })
            symbol = symbol.ContainingSymbol;
        return symbol is IMethodSymbol { AssociatedSymbol: { } property } ? property : symbol;
    }

    internal static string Normalize(string text) => Regex.Replace(text, @"\s+", " ").Trim();
}
