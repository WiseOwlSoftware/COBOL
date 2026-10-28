// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ FINDS THE RECURSIVE LISTINGS OF A DIRECTORY THAT HOLDS A TEST HOST'S BUILD OUTPUT (kb/Work PB3814, review R1-1).
/// A listing that recurses from the repository root, from <c>tests/</c> or from a test project's directory descends into
/// <c>tests/&lt;project&gt;/bin/&lt;configuration&gt;/&lt;framework&gt;</c>, the working directory of the running test host,
/// where another test's file I/O creates and deletes directories while the walk runs; a delete-pending directory answers
/// the walk with access denied (the PB3677 merge-group failure). <c>TrackedTreeScopeDriftTests</c> refuses every such
/// listing in the test sources.
/// </summary>
/// <remarks>
/// The root is FOLLOWED, not pattern-matched: a root held in a local, a field, a property, an assignment, a helper
/// method's return, a lambda parameter, a <c>foreach</c> variable, a method parameter (through the calls in the same file)
/// or a theory row resolves to the <c>TestRepo</c> expression it was built from, evaluated as the path it names (a
/// <see cref="Rel"/>: the segments below the repository root). A root that resolves to nothing the file can see is
/// <see cref="Verdict.Unresolved"/>, which the drift test counts and bounds, so the guard cannot lose its reach unseen.
/// A directory is a hazard only when a test host's build output can lie under it: the repository root, <c>tests/</c>, or a
/// directory under <c>tests/</c> holding a project; a data-only directory (<c>tests/nist</c>) holds none.
/// </remarks>
internal static class TestsTreeWalks
{
    internal enum Verdict { Hazard, Safe, Unresolved }

    /// <summary>One recursive listing found in a source: where, the root as written and what it resolved to.</summary>
    internal sealed record Listing(int Line, string Root, Verdict Verdict);

    /// <summary>A path below the repository root, one segment per element; a segment the file cannot evaluate is null.</summary>
    private sealed record Rel(IReadOnlyList<string?> Segments);

    /// <summary>A string the file spells (null when it is built at run time).</summary>
    private sealed record Lit(string? Text);

    /// <summary>The method names that list a directory.</summary>
    private static readonly HashSet<string> ListingMethods = new(StringComparer.Ordinal)
    {
        "EnumerateFiles", "EnumerateDirectories", "EnumerateFileSystemEntries", "GetFiles", "GetDirectories", "GetFileSystemEntries",
    };

    /// <summary>The two ways a listing recurses. BUILT, so this file's own scan and the planted sources do not spell one.</summary>
    internal static readonly string[] Recursion = ["SearchOption" + ".AllDirectories", "Recurse" + "Subdirectories"];

    /// <summary>The <c>TestRepo</c> members that name a directory below the repository root, by their first segments.</summary>
    private static readonly Dictionary<string, string[]> RepoDirectories = new(StringComparer.Ordinal)
    {
        ["Src"] = ["src"], ["Tests"] = ["tests"], ["Docs"] = ["docs"], ["Scripts"] = ["scripts"], ["Specs"] = ["specs"],
        ["Nist"] = ["tests", "nist"], ["VersionMatrix"] = ["tests", "version-matrix"], ["Suite"] = ["conformance-suite"],
    };

    private const int MaxDepth = 14;

    /// <summary>The recursive listings in <paramref name="source"/>, each judged. <paramref name="projectDirectories"/> are
    /// the names of the directories under <c>tests/</c> that hold a project.</summary>
    internal static IReadOnlyList<Listing> Scan(string source, IReadOnlySet<string> projectDirectories)
    {
        if (!Recursion.Any(r => source.Contains(r, StringComparison.Ordinal))) return []; // no recursion is spelled, so no listing recurses
        SyntaxNode tree = CSharpSyntaxTree.ParseText(source).GetRoot();
        var resolver = new Resolver(tree);
        var found = new List<Listing>();
        foreach (InvocationExpressionSyntax call in tree.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (call.Expression is not MemberAccessExpressionSyntax access || !ListingMethods.Contains(access.Name.Identifier.ValueText))
                continue;
            if (!Recurses(call, resolver)) continue;
            ExpressionSyntax rootExpression = access.Expression is IdentifierNameSyntax { Identifier.ValueText: "Directory" }
                                              or MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Directory" }
                ? call.ArgumentList.Arguments.FirstOrDefault()?.Expression ?? access.Expression
                : access.Expression;
            IReadOnlyList<object> values = resolver.Eval(rootExpression, 0);
            Verdict verdict = Judge(values, projectDirectories);
            found.Add(new Listing(tree.SyntaxTree.GetLineSpan(call.Span).StartLinePosition.Line + 1, Squash(rootExpression.ToString()), verdict));
        }
        return found;
    }

    /// <summary>The listings in <paramref name="source"/> a test host's build output can lie under, as messages.</summary>
    internal static IReadOnlyList<string> Find(string path, string source, IReadOnlySet<string> projectDirectories) =>
        Hazards(path, Scan(source, projectDirectories));

    /// <summary>The hazards among <paramref name="listings"/> (a <see cref="Scan"/> of <paramref name="path"/>), as messages.</summary>
    internal static IReadOnlyList<string> Hazards(string path, IEnumerable<Listing> listings) =>
        [.. listings.Where(l => l.Verdict == Verdict.Hazard).Select(l => $"{path}:{l.Line}: lists {l.Root} recursively from the file system")];

    private static string Squash(string text) => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>Whether the listing's arguments name a recursion, directly or through a variable that does.</summary>
    private static bool Recurses(InvocationExpressionSyntax call, Resolver resolver)
    {
        foreach (ArgumentSyntax a in call.ArgumentList.Arguments)
        {
            if (Mentions(a.ToString())) return true;
            if (a.Expression is IdentifierNameSyntax id && resolver.Declarations(id.Identifier.ValueText).Any(d => Mentions(d.ToString())))
                return true;
        }
        return false;

        static bool Mentions(string text) => Recursion.Any(r => text.Contains(r, StringComparison.Ordinal));
    }

    private static Verdict Judge(IReadOnlyList<object> values, IReadOnlySet<string> projectDirectories)
    {
        bool resolved = false;
        foreach (Rel rel in values.OfType<Rel>())
        {
            IReadOnlyList<string?> s = rel.Segments;
            if (s.Count == 0) return Verdict.Hazard; // the repository root
            if (s[0] is null) continue; // not evaluable
            resolved = true;
            if (s[0] != "tests") continue;
            if (s.Count == 1) return Verdict.Hazard; // tests/ itself
            if (s.Count == 2 && (s[1] is null || projectDirectories.Contains(s[1]!))) return Verdict.Hazard; // a project's directory
        }
        return resolved ? Verdict.Safe : Verdict.Unresolved;
    }

    /// <summary>Evaluates the root expressions of one source.</summary>
    private sealed class Resolver
    {
        private readonly HashSet<SyntaxNode> _active = [];
        private readonly Dictionary<string, List<SyntaxNode>> _declarations = new(StringComparer.Ordinal);

        // The source indexed once, by name, so a lookup never walks the tree.
        private readonly Dictionary<string, List<ForEachStatementSyntax>> _loops = new(StringComparer.Ordinal);
        private readonly Dictionary<string, List<ParameterSyntax>> _parameters = new(StringComparer.Ordinal);
        private readonly Dictionary<string, List<InvocationExpressionSyntax>> _calls = new(StringComparer.Ordinal);
        private readonly Dictionary<string, List<MethodDeclarationSyntax>> _methods = new(StringComparer.Ordinal);

        public Resolver(SyntaxNode tree)
        {
            foreach (SyntaxNode n in tree.DescendantNodes())
            {
                switch (n)
                {
                    case ForEachStatementSyntax f: Index(_loops, f.Identifier.ValueText, f); break;
                    case ParameterSyntax p: Index(_parameters, p.Identifier.ValueText, p); break;
                    case InvocationExpressionSyntax { Expression: IdentifierNameSyntax i } c: Index(_calls, i.Identifier.ValueText, c); break;
                    case InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax m } c: Index(_calls, m.Name.Identifier.ValueText, c); break;
                    case MethodDeclarationSyntax { ParameterList.Parameters.Count: 0 } m: Index(_methods, m.Identifier.ValueText, m); break;
                    case VariableDeclaratorSyntax { Initializer: { } init } v: Add(v.Identifier.ValueText, init.Value); break;
                    case PropertyDeclarationSyntax p:
                        if (p.ExpressionBody is { } body) Add(p.Identifier.ValueText, body.Expression);
                        if (p.Initializer is { } pi) Add(p.Identifier.ValueText, pi.Value);
                        break;
                    case AssignmentExpressionSyntax { Left: var left } a when NameOf(left) is { } name: Add(name, a.Right); break;
                }
            }
        }

        private void Add(string name, SyntaxNode value) => Index(_declarations, name, value);

        private static void Index<T>(Dictionary<string, List<T>> map, string name, T node)
        {
            if (!map.TryGetValue(name, out List<T>? list)) map[name] = list = [];
            list.Add(node);
        }

        /// <summary>The expressions a name is initialized or assigned from, in this source.</summary>
        public IEnumerable<SyntaxNode> Declarations(string name) => _declarations.TryGetValue(name, out List<SyntaxNode>? l) ? l : [];

        private static string? NameOf(ExpressionSyntax e) => e switch
        {
            IdentifierNameSyntax i => i.Identifier.ValueText,
            MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name: var n } => n.Identifier.ValueText,
            _ => null,
        };

        /// <summary>The values an expression can take, as <see cref="Rel"/> (a path) or <see cref="Lit"/> (a string).</summary>
        public IReadOnlyList<object> Eval(ExpressionSyntax e, int depth)
        {
            if (depth > MaxDepth || !_active.Add(e)) return [];
            try
            {
                return EvalCore(e, depth + 1);
            }
            finally
            {
                _active.Remove(e);
            }
        }

        private IReadOnlyList<object> EvalCore(ExpressionSyntax e, int depth)
        {
            switch (e)
            {
                case LiteralExpressionSyntax l when l.IsKind(SyntaxKind.StringLiteralExpression):
                    return [new Lit(l.Token.ValueText)];
                case InterpolatedStringExpressionSyntax:
                    return [new Lit(null)];
                case ParenthesizedExpressionSyntax p: return Eval(p.Expression, depth);
                case PostfixUnaryExpressionSyntax { Operand: var o } when e.IsKind(SyntaxKind.SuppressNullableWarningExpression): return Eval(o, depth);
                case CastExpressionSyntax c: return Eval(c.Expression, depth);
                case ConditionalExpressionSyntax c: return [.. Eval(c.WhenTrue, depth), .. Eval(c.WhenFalse, depth)];
                case BinaryExpressionSyntax b when b.IsKind(SyntaxKind.CoalesceExpression): return [.. Eval(b.Left, depth), .. Eval(b.Right, depth)];
                case ObjectCreationExpressionSyntax { Type: var t, ArgumentList.Arguments: [var only] } when t.ToString() is "DirectoryInfo" or "System.IO.DirectoryInfo":
                    return Eval(only.Expression, depth);
                case MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "TestRepo" }, Name.Identifier.ValueText: "Root" }:
                    return [new Rel([])];
                case MemberAccessExpressionSyntax { Name.Identifier.ValueText: "FullName" or "Parent" } m:
                    return Eval(m.Expression, depth);
                case IdentifierNameSyntax or MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax }:
                    return Name(NameOf(e)!, depth);
                case MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax, Name: var member }:
                    return Name(member.Identifier.ValueText, depth); // a static member of a class in this source
                case InvocationExpressionSyntax call: return Invocation(call, depth);
                default: return [];
            }
        }

        private IReadOnlyList<object> Invocation(InvocationExpressionSyntax call, int depth)
        {
            var args = call.ArgumentList.Arguments;
            switch (call.Expression)
            {
                case MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "TestRepo" }, Name.Identifier.ValueText: var member }:
                    return TestRepoCall(member, args, depth);
                case MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "Path" }, Name.Identifier.ValueText: var m }:
                    switch (m)
                    {
                        case "Combine" when args.Count > 0:
                            return Eval(args[0].Expression, depth).Select(v => v is Rel r ? Append(r, args.Skip(1), depth) : v).ToList();
                        case "GetFullPath" when args.Count > 0:
                            return Eval(args[0].Expression, depth);
                        case "GetDirectoryName" when args.Count == 1:
                            return Eval(args[0].Expression, depth).Select(v => v is Rel r ? Parent(r) : v).ToList();
                    }
                    return [];
                case MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "Directory" }, Name.Identifier.ValueText: "GetParent" } when args.Count == 1:
                    return Eval(args[0].Expression, depth).Select(v => v is Rel r ? Parent(r) : v).ToList();
                case MemberAccessExpressionSyntax { Expression: var recv, Name.Identifier.ValueText: "ToString" or "ToArray" or "First" or "Single" } when args.Count == 0:
                    return Eval(recv, depth);
                case IdentifierNameSyntax { Identifier.ValueText: var method }:
                    return [.. MethodBodies(method).SelectMany(x => Eval(x, depth))];
                default: return [];
            }
        }

        private IReadOnlyList<object> TestRepoCall(string member, SeparatedSyntaxList<ArgumentSyntax> args, int depth)
        {
            if (member == "At") return [Append(new Rel([]), args, depth)];
            if (RepoDirectories.TryGetValue(member, out string[]? head)) return [Append(new Rel(head), args, depth)];
            return [new Rel(["<" + member + ">"])]; // some other TestRepo path, none of which is the repository root or tests/
        }

        /// <summary>The path <paramref name="r"/> with each argument's string appended as segments.</summary>
        private Rel Append(Rel r, IEnumerable<ArgumentSyntax> args, int depth)
        {
            var segments = new List<string?>(r.Segments);
            foreach (ArgumentSyntax a in args)
            {
                IReadOnlyList<object> values = Eval(a.Expression, depth);
                if (values.OfType<Lit>().FirstOrDefault() is { Text: { } text })
                    segments.AddRange(text.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries));
                else
                    segments.Add(null);
            }
            return new Rel(segments);
        }

        private static Rel Parent(Rel r) => new(r.Segments.Count == 0 ? [null] : [.. r.Segments.Take(r.Segments.Count - 1)]);

        /// <summary>What a name holds: its initializers and assignments, or, for a parameter, the values its callers (or its
        /// theory rows) pass; for a lambda parameter or a <c>foreach</c> variable, the elements of the sequence it ranges over.</summary>
        private IReadOnlyList<object> Name(string name, int depth)
        {
            var values = new List<object>();
            foreach (SyntaxNode d in Declarations(name))
                if (d is ExpressionSyntax x) values.AddRange(Eval(x, depth));
            foreach (ForEachStatementSyntax f in _loops.GetValueOrDefault(name) ?? [])
                values.AddRange(Elements(f.Expression, depth));
            foreach (ParameterSyntax p in _parameters.GetValueOrDefault(name) ?? [])
                values.AddRange(Parameter(p, depth));
            return values;
        }

        private IEnumerable<object> Parameter(ParameterSyntax p, int depth)
        {
            SyntaxNode? lambda = p.Parent switch
            {
                SimpleLambdaExpressionSyntax s => s,
                ParameterListSyntax { Parent: ParenthesizedLambdaExpressionSyntax pl } => pl,
                _ => null,
            };
            if (lambda is not null) return LambdaElements(lambda, depth);
            if (p.Parent is not ParameterListSyntax list) return [];
            int index = list.Parameters.IndexOf(p);
            SyntaxNode? owner = list.Parent;
            string? method = owner switch
            {
                MethodDeclarationSyntax m => m.Identifier.ValueText,
                LocalFunctionStatementSyntax lf => lf.Identifier.ValueText,
                _ => null,
            };
            if (method is null) return [];
            var values = new List<object>();
            // A theory row: the attribute's argument at this position.
            foreach (AttributeSyntax attr in (owner as MethodDeclarationSyntax)?.AttributeLists.SelectMany(l => l.Attributes) ?? [])
                if (attr.Name.ToString().StartsWith("InlineData", StringComparison.Ordinal)
                    && attr.ArgumentList?.Arguments is { } row && index < row.Count)
                    values.AddRange(Eval(row[index].Expression, 0));
            foreach (InvocationExpressionSyntax call in _calls.GetValueOrDefault(method) ?? [])
                if (index < call.ArgumentList.Arguments.Count)
                    values.AddRange(Eval(call.ArgumentList.Arguments[index].Expression, depth));
            return values;
        }

        /// <summary>The elements a lambda's parameter takes: those of the sequence the lambda is applied to.</summary>
        private IEnumerable<object> LambdaElements(SyntaxNode lambda, int depth)
        {
            if (lambda.Parent is ArgumentSyntax { Parent.Parent: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax recv } })
                return Elements(recv.Expression, depth);
            return [];
        }

        /// <summary>The values the elements of a sequence expression can take.</summary>
        private IReadOnlyList<object> Elements(ExpressionSyntax e, int depth)
        {
            if (depth > MaxDepth || !_active.Add(e)) return [];
            try
            {
                depth++;
                switch (e)
                {
                    case ParenthesizedExpressionSyntax p: return Elements(p.Expression, depth);
                    case CollectionExpressionSyntax c:
                        return [.. c.Elements.OfType<ExpressionElementSyntax>().SelectMany(x => Eval(x.Expression, depth))];
                    case ImplicitArrayCreationExpressionSyntax ia: return [.. ia.Initializer.Expressions.SelectMany(x => Eval(x, depth))];
                    case ArrayCreationExpressionSyntax { Initializer: { } ai }: return [.. ai.Expressions.SelectMany(x => Eval(x, depth))];
                    case ObjectCreationExpressionSyntax { Initializer: { } oi }: return [.. oi.Expressions.SelectMany(x => Eval(x, depth))];
                    case IdentifierNameSyntax id:
                        return [.. Declarations(id.Identifier.ValueText).OfType<ExpressionSyntax>().SelectMany(x => Elements(x, depth))];
                    case InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax m } call:
                        // A call that keeps or reorders the elements (Where, Order, Distinct, ToArray, Concat, ...).
                        return [.. Elements(m.Expression, depth), .. call.ArgumentList.Arguments.SelectMany(a => a.Expression is LambdaExpressionSyntax ? [] : Elements(a.Expression, depth))];
                    default: return EvalCore(e, depth);
                }
            }
            finally
            {
                _active.Remove(e);
            }
        }

        /// <summary>The expressions a method of this name returns (its expression body, or its <c>return</c> statements).</summary>
        private IEnumerable<ExpressionSyntax> MethodBodies(string method)
        {
            foreach (MethodDeclarationSyntax m in _methods.GetValueOrDefault(method) ?? [])
            {
                if (m.ExpressionBody is { } b) yield return b.Expression;
                foreach (ReturnStatementSyntax r in m.Body?.DescendantNodes().OfType<ReturnStatementSyntax>() ?? [])
                    if (r.Expression is { } x) yield return x;
            }
        }
    }
}
