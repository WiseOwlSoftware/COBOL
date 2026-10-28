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
/// method's call (its parameters bound to what that call passes, a <c>params</c> parameter to the arguments it holds), a
/// lambda parameter, a <c>foreach</c> variable, a method parameter (through the calls in the same file) or a theory row resolves to the <c>TestRepo</c> expression it was built from, evaluated as the path it names (a
/// <see cref="Rel"/>: the segments below the repository root). A root that resolves to nothing the file can see is
/// <see cref="Verdict.Unresolved"/>, which the drift test counts and bounds, so the guard cannot lose its reach unseen.
/// A directory is a hazard only when a test host's build output can lie under it: the repository root, <c>tests/</c>, a
/// directory under <c>tests/</c> holding a project, or that project's <c>bin/</c> or <c>obj/</c> and anything below; a
/// data-only directory (<c>tests/nist</c>) holds none. A <c>TestRepo</c> or <c>Path</c> written with its namespace is the same.
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

    /// <summary>The arguments a <c>params</c> parameter holds at one call: one evaluation per argument, so the helper's body
    /// sees exactly the segments its caller passed (none, for <c>Unit()</c>).</summary>
    private sealed record Params(IReadOnlyList<IReadOnlyList<object>> Args);

    /// <summary>A segment standing for zero or more unknown segments (a spread the file does not let the guard expand); the
    /// path is judged by the segments before it.</summary>
    private const string Open = "*";

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
            ExpressionSyntax rootExpression = IsNamed(access.Expression, "Directory")
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

    /// <summary>Whether <paramref name="e"/> names <paramref name="name"/>, bare or qualified by a namespace or a class
    /// (<c>TestRepo</c>, <c>Shared.TestRepo</c>, <c>System.IO.Path</c>).</summary>
    private static bool IsNamed(ExpressionSyntax e, string name) => e switch
    {
        IdentifierNameSyntax i => i.Identifier.ValueText == name,
        MemberAccessExpressionSyntax m => m.Name.Identifier.ValueText == name,
        _ => false,
    };

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
            // What stands after an unexpandable spread may be nothing, so the path is judged by the segments before it.
            int open = rel.Segments.ToList().IndexOf(Open);
            IReadOnlyList<string?> s = open < 0 ? rel.Segments : [.. rel.Segments.Take(open)];
            if (s.Count == 0) return Verdict.Hazard; // the repository root
            if (s[0] is null) continue; // not evaluable
            resolved = true;
            if (s[0] != "tests") continue;
            if (s.Count == 1) return Verdict.Hazard; // tests/ itself
            bool project = s[1] is null || projectDirectories.Contains(s[1]!);
            if (s.Count == 2 && project) return Verdict.Hazard; // a project's directory
            // The test host's build output itself, or anything below it (the working directory of the running host).
            if (s.Count >= 3 && project && s[2] is { } sub
                && (sub.Equals("bin", StringComparison.OrdinalIgnoreCase) || sub.Equals("obj", StringComparison.OrdinalIgnoreCase)))
                return Verdict.Hazard;
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
        private readonly Dictionary<string, List<Callable>> _callables = new(StringComparer.Ordinal);

        // The parameters of the helper being evaluated at one call, bound to what that call passes (null outside a helper).
        private Dictionary<string, IReadOnlyList<object>>? _bound;

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
                    case MethodDeclarationSyntax m:
                        Index(_callables, m.Identifier.ValueText, new Callable(m.ParameterList, m.ExpressionBody?.Expression, m.Body));
                        break;
                    case LocalFunctionStatementSyntax lf:
                        Index(_callables, lf.Identifier.ValueText, new Callable(lf.ParameterList, lf.ExpressionBody?.Expression, lf.Body));
                        break;
                    case VariableDeclaratorSyntax { Initializer: { } init } v: Add(v.Identifier.ValueText, init.Value); break;
                    case PropertyDeclarationSyntax p:
                        if (p.ExpressionBody is { } body) Add(p.Identifier.ValueText, body.Expression);
                        if (p.Initializer is { } pi) Add(p.Identifier.ValueText, pi.Value);
                        break;
                    case AssignmentExpressionSyntax { Left: var left } a when NameOf(left) is { } name: Add(name, a.Right); break;
                }
            }
        }

        /// <summary>A method or local function of this source: its parameters and the expressions it returns.</summary>
        private sealed record Callable(ParameterListSyntax Parameters, ExpressionSyntax? ExpressionBody, BlockSyntax? Body)
        {
            public IEnumerable<ExpressionSyntax> Returns()
            {
                if (ExpressionBody is not null) yield return ExpressionBody;
                foreach (ReturnStatementSyntax r in Body?.DescendantNodes().OfType<ReturnStatementSyntax>() ?? [])
                    if (r.Expression is { } x) yield return x;
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
                case MemberAccessExpressionSyntax { Expression: var repo, Name.Identifier.ValueText: "Root" } when IsNamed(repo, "TestRepo"):
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
                case MemberAccessExpressionSyntax { Expression: var repo, Name.Identifier.ValueText: var member } when IsNamed(repo, "TestRepo"):
                    return TestRepoCall(member, args, depth);
                case MemberAccessExpressionSyntax { Expression: var path, Name.Identifier.ValueText: var m } when IsNamed(path, "Path"):
                    switch (m)
                    {
                        case "Combine" when args.Count == 1 && Items(args[0].Expression) is { Count: > 0 } items && !items[0].Spread:
                            return Eval(items[0].Expression, depth).Select(v => v is Rel r ? AppendItems(r, items.Skip(1), depth) : v).ToList();
                        case "Combine" when args.Count > 0:
                            return Eval(args[0].Expression, depth).Select(v => v is Rel r ? Append(r, args.Skip(1), depth) : v).ToList();
                        case "GetFullPath" when args.Count > 0:
                            return Eval(args[0].Expression, depth);
                        case "GetDirectoryName" when args.Count == 1:
                            return Eval(args[0].Expression, depth).Select(v => v is Rel r ? Parent(r) : v).ToList();
                    }
                    return [];
                case MemberAccessExpressionSyntax { Expression: var dir, Name.Identifier.ValueText: "GetParent" } when args.Count == 1 && IsNamed(dir, "Directory"):
                    return Eval(args[0].Expression, depth).Select(v => v is Rel r ? Parent(r) : v).ToList();
                case MemberAccessExpressionSyntax { Expression: var text, Name.Identifier.ValueText: "Split" }:
                    // "a/b".Split('/'): the parts of a string the file spells, so a spread of it expands to its segments
                    return [.. Eval(text, depth).OfType<Lit>().Select(l => l.Text is { } t
                        ? new Params([.. SegmentsOf(t).Select(part => (IReadOnlyList<object>)[new Lit(part)])])
                        : (object)new Lit(null))];
                case MemberAccessExpressionSyntax { Expression: var recv, Name.Identifier.ValueText: "ToString" or "ToArray" or "First" or "Single" } when args.Count == 0:
                    return Eval(recv, depth);
                case IdentifierNameSyntax { Identifier.ValueText: var method }:
                    return Inline(method, args, depth);
                case MemberAccessExpressionSyntax { Name.Identifier.ValueText: var member } when _callables.ContainsKey(member):
                    return Inline(member, args, depth); // a method of this source, called through this or its class
                default: return [];
            }
        }

        /// <summary>What a call of a method or local function of this source returns: each of its <c>return</c> expressions
        /// evaluated with the parameters bound to what THIS call passes, never to what another call of it passes.</summary>
        private IReadOnlyList<object> Inline(string method, SeparatedSyntaxList<ArgumentSyntax> args, int depth)
        {
            var values = new List<object>();
            foreach (Callable callee in _callables.GetValueOrDefault(method) ?? [])
            {
                var parameters = callee.Parameters.Parameters;
                bool hasParams = parameters.Any(p => p.Modifiers.Any(SyntaxKind.ParamsKeyword));
                if (args.Count > parameters.Count && !hasParams) continue; // another overload
                var bound = new Dictionary<string, IReadOnlyList<object>>(StringComparer.Ordinal);
                for (int i = 0; i < parameters.Count; i++)
                {
                    ParameterSyntax p = parameters[i];
                    string name = p.Identifier.ValueText;
                    if (p.Modifiers.Any(SyntaxKind.ParamsKeyword))
                        bound[name] = [new Params([.. args.Skip(i).Select(a => Eval(a.Expression, depth))])];
                    else if (args.FirstOrDefault(a => a.NameColon?.Name.Identifier.ValueText == name) is { } named)
                        bound[name] = Eval(named.Expression, depth);
                    else if (i < args.Count && args[i].NameColon is null)
                        bound[name] = Eval(args[i].Expression, depth);
                    else
                        bound[name] = p.Default is { } d ? Eval(d.Value, depth) : [];
                }
                var saved = _bound;
                _bound = bound;
                try
                {
                    foreach (ExpressionSyntax x in callee.Returns()) values.AddRange(Eval(x, depth));
                }
                finally
                {
                    _bound = saved;
                }
            }
            return values;
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
            foreach (ArgumentSyntax a in args) AppendArgument(segments, a.Expression, depth);
            return new Rel(segments);
        }

        /// <summary>The elements, in order, of a collection expression or an array creation, each with whether it is a spread;
        /// null when <paramref name="e"/> spells no collection.</summary>
        private static IReadOnlyList<(ExpressionSyntax Expression, bool Spread)>? Items(ExpressionSyntax e) => e switch
        {
            CollectionExpressionSyntax c =>
                [.. c.Elements.Select(x => x switch
                {
                    SpreadElementSyntax sp => (sp.Expression, true),
                    ExpressionElementSyntax ex => (ex.Expression, false),
                    _ => (null!, false), // a key-value element: no path is built from one
                }).Where(x => x.Expression is not null)],
            ArrayCreationExpressionSyntax { Initializer: { } ai } => [.. ai.Expressions.Select(x => (x, false))],
            ImplicitArrayCreationExpressionSyntax ia => [.. ia.Initializer.Expressions.Select(x => (x, false))],
            _ => null,
        };

        private Rel AppendItems(Rel r, IEnumerable<(ExpressionSyntax Expression, bool Spread)> items, int depth)
        {
            var segments = new List<string?>(r.Segments);
            foreach ((ExpressionSyntax x, bool spread) in items)
                if (spread) AppendValues(segments, Eval(x, depth), spread: true);
                else AppendArgument(segments, x, depth);
            return new Rel(segments);
        }

        /// <summary>One argument's segments: a string's parts, the arguments a <c>params</c> parameter holds, or the elements of
        /// a collection expression or array (a spread stands for the unknown, <see cref="Open"/>).</summary>
        private void AppendArgument(List<string?> segments, ExpressionSyntax e, int depth)
        {
            if (Items(e) is { } items) segments.AddRange(AppendItems(new Rel([]), items, depth).Segments);
            else AppendValues(segments, Eval(e, depth), spread: false);
        }

        private static string[] SegmentsOf(string path) => path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);

        private static void AppendValues(List<string?> segments, IReadOnlyList<object> values, bool spread)
        {
            if (values.OfType<Params>().FirstOrDefault() is { } held)
            {
                foreach (IReadOnlyList<object> arg in held.Args) AppendValues(segments, arg, spread: false);
            }
            else if (values.OfType<Lit>().FirstOrDefault() is { Text: { } text })
            {
                segments.AddRange(SegmentsOf(text));
            }
            else
            {
                segments.Add(spread ? Open : null);
            }
        }

        /// <summary>The directory above <paramref name="r"/>; an <see cref="Open"/> segment may be empty, so the parent is taken
        /// from the segments before it.</summary>
        private static Rel Parent(Rel r)
        {
            int open = r.Segments.ToList().IndexOf(Open);
            IReadOnlyList<string?> known = open < 0 ? r.Segments : [.. r.Segments.Take(open)];
            return new Rel(known.Count == 0 ? [null] : [.. known.Take(known.Count - 1)]);
        }

        /// <summary>What a name holds: its initializers and assignments, or, for a parameter, the values its callers (or its
        /// theory rows) pass; for a lambda parameter or a <c>foreach</c> variable, the elements of the sequence it ranges over.</summary>
        private IReadOnlyList<object> Name(string name, int depth)
        {
            if (_bound is not null && _bound.TryGetValue(name, out IReadOnlyList<object>? held)) return held;
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
    }
}
