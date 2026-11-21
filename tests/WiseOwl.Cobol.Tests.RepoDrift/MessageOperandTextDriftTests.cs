// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.RepoDrift;

/// <summary>
/// ⛔ A MESSAGE NAMES A WRITTEN OPERAND AS THE PROGRAMMER WROTE IT (kb/Work PB5979). <c>ParserRuleContext.GetText()</c> joins a
/// node's tokens with no separators, so <c>X OF Q</c> printed as <c>XOFQ</c> and <c>KY OF IREC</c> as <c>KYOFIREC</c>: a name the
/// program does not contain. A diagnostic's text takes <c>WrittenSource.Of</c> (<c>DataBinder.WrittenText</c>), the one reader of
/// a node's source extent. The sweep reads the compiler's sources and refuses a <c>GetText()</c> of a multi-token parse context
/// that reaches a message by one of three shapes: it sits in an interpolation hole; it is an operand of a <c>+</c> that also holds
/// a string literal; or it initializes a local that the same member then uses in one of those two places. A <c>GetText()</c> used
/// as a lookup key, a comparison or a value is none of the three; a one-token context (a word) and a terminal node cannot run
/// tokens together. Red on its planted counterexamples (<see cref="TheSweep_SeesEachShape"/>).
/// </summary>
[Trait("Sweep", "tree")]
public sealed class MessageOperandTextDriftTests
{
    private static readonly TreeScope CompilerSources = new(
        Under: ["src/WiseOwl.Cobol.Compiler/", "src/WiseOwl.Cobol.Frontend/"],
        Except: ["src/WiseOwl.Cobol.Frontend/Generated/"],
        Kinds: [".cs"]);

    private static readonly TreeScope GrammarSources = new(Under: ["src/WiseOwl.Cobol.Frontend/Grammar/"], Kinds: [".g4"]);

    [Fact]
    public void NoMessageIsBuiltFromGetText()
    {
        var files = TrackedTree.Files(CompilerSources)
            .Select(rel => (Path: rel, Text: File.ReadAllText(TestRepo.At(rel.Split('/')))))
            .Where(f => f.Text.Contains("GetText()", StringComparison.Ordinal))
            .ToList();
        var offences = Offences(files);
        Assert.True(offences.Count == 0,
            "A diagnostic names an operand with ParserRuleContext.GetText(), which runs its tokens together ('X OF Q' prints "
            + "'XOFQ'); use DataBinder.WrittenText (WrittenSource.Of) for the text a message quotes (kb/Work PB5979):\n"
            + string.Join("\n", offences));
    }

    [Fact]
    public void TheSweep_SeesEachShape()
    {
        const string source = """
            class C
            {
                string Hole(Antlr4.Runtime.ParserRuleContext c) => $"'{c.GetText()}' is not defined";
                string Plus(Antlr4.Runtime.ParserRuleContext c) => "'" + c.GetText() + "' is not defined";
                string Local(Antlr4.Runtime.ParserRuleContext c) { string t = c.GetText(); return $"'{t}' is not defined"; }
                string LocalPlus(Antlr4.Runtime.ParserRuleContext c) { var t = c.GetText().ToUpperInvariant(); return "'" + t + "'"; }
                bool Key(Antlr4.Runtime.ParserRuleContext c) { string t = c.GetText(); return t.Length == 3 && c.GetText() == "X"; }
                string Word(WiseOwl.Cobol.Frontend.Generated.CobolParserCore.CobolWordContext w) => $"'{w.GetText()}'";
                string Terminal(Antlr4.Runtime.Tree.ITerminalNode w) => $"'{w.GetText()}'";
            }
            """;
        var found = Offences([("planted.cs", source)]);
        Assert.Equal(4, found.Count);
        Assert.All(["planted.cs:3:", "planted.cs:4:", "planted.cs:5:", "planted.cs:6:"],
            line => Assert.Contains(found, f => f.StartsWith(line, StringComparison.Ordinal)));
    }

    /// <summary>Each <c>GetText()</c> of a multi-token parse context that reaches a message, as <c>path:line: text</c>. The files
    /// are compiled against the built product, as the test assembly the Compiler grants its internals to, so a receiver's type is the
    /// compiler's own; a receiver whose type does not bind is reported (an unbound name could be a context).</summary>
    private static List<string> Offences(IReadOnlyList<(string Path, string Text)> files)
    {
        var options = new CSharpParseOptions(LanguageVersion.Preview);
        var trees = files.Select(f => CSharpSyntaxTree.ParseText(f.Text, options, path: f.Path)).ToList();
        string globalUsings = Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler", "obj"), "WiseOwl.Cobol.Compiler.GlobalUsings.g.cs",
            SearchOption.AllDirectories).OrderByDescending(File.GetLastWriteTimeUtc).First();
        var usings = CSharpSyntaxTree.ParseText(File.ReadAllText(globalUsings), options, path: globalUsings);
        string platform = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        var references = platform.Split(System.IO.Path.PathSeparator)
            .Concat(Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll"))
            .GroupBy(System.IO.Path.GetFileName, StringComparer.OrdinalIgnoreCase).Select(g => g.First())
            .Where(IsManagedAssembly)
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p)).ToList();
        var compilation = CSharpCompilation.Create(typeof(MessageOperandTextDriftTests).Assembly.GetName().Name, [.. trees, usings], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable,
                metadataImportOptions: MetadataImportOptions.All));
        var singleToken = SingleTokenContexts();
        var offences = new List<string>();
        foreach (var tree in trees)
        {
            var model = compilation.GetSemanticModel(tree);
            string path = tree.FilePath;
            foreach (var body in tree.GetRoot().DescendantNodes().Where(n => n is BaseMethodDeclarationSyntax or AccessorDeclarationSyntax
                         or ArrowExpressionClauseSyntax))
            {
                // Only the outermost body of each member is scanned, so a nested one is not read twice.
                if (body.Ancestors().Any(a => a is BaseMethodDeclarationSyntax or AccessorDeclarationSyntax or ArrowExpressionClauseSyntax)) continue;
                var locals = new HashSet<string>(StringComparer.Ordinal);
                foreach (var declarator in body.DescendantNodes().OfType<VariableDeclaratorSyntax>())
                    if (declarator.Initializer is { Value: var init } && CarriesGetText(init, model, singleToken))
                        locals.Add(declarator.Identifier.ValueText);
                foreach (var node in body.DescendantNodes())
                {
                    if (IsReferenceGetText(node, model))
                        offences.Add($"{path}:{node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}: {node.ToString().Trim()}");
                    IEnumerable<ExpressionSyntax> message = node switch
                    {
                        InterpolationSyntax hole => [hole.Expression],
                        BinaryExpressionSyntax plus when plus.IsKind(SyntaxKind.AddExpression)
                            && plus.Parent is not BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AddExpression }
                            && plus.DescendantNodesAndSelf().Any(n => n is LiteralExpressionSyntax l && l.IsKind(SyntaxKind.StringLiteralExpression))
                            => Operands(plus),
                        _ => [],
                    };
                    foreach (var operand in message)
                        // A context's GetText() anywhere in the message operand; a flagged local only where its TEXT is the operand's.
                        foreach (var use in operand.DescendantNodesAndSelf().Where(n => IsMultiTokenGetText(n, model, singleToken))
                                     .Concat(Spine(operand).OfType<IdentifierNameSyntax>().Where(id => locals.Contains(id.Identifier.ValueText) && IsRead(id))))
                            offences.Add($"{path}:{use.GetLocation().GetLineSpan().StartLinePosition.Line + 1}: {use.ToString().Trim()}");
                }
            }
        }
        return [.. offences.Distinct()];
    }

    /// <summary>The generated context classes whose rule matches exactly ONE token, read from the grammar (so the next
    /// word-shaped rule is exempt with no list to edit): an alternative is one token when it is a lone token reference, a lone
    /// rule that is itself one token, or one parenthesized group of such. A one-token context's <c>GetText()</c> is the word as
    /// written, and a word is no qualified reference.</summary>
    private static HashSet<string> SingleTokenContexts()
    {
        var rules = new Dictionary<string, List<List<string>>>(StringComparer.Ordinal);
        foreach (string g4 in TrackedTree.Files(GrammarSources))
            foreach (var (name, alternatives) in GrammarRules(GrammarSource.Read(TestRepo.At(g4.Split('/')))))
                rules[name] = alternatives;
        var single = new HashSet<string>(StringComparer.Ordinal);
        for (bool grew = true; grew;)
        {
            grew = false;
            foreach (var (name, alternatives) in rules)
                if (!single.Contains(name) && alternatives.All(alt => alt is [var only] && (IsToken(only) || single.Contains(only))))
                    grew = single.Add(name) || grew;
        }
        return [.. single.Select(r => char.ToUpperInvariant(r[0]) + r[1..] + "Context")];
    }

    private static bool IsToken(string element) => element.Length > 0 && char.IsUpper(element[0]);

    /// <summary>Each parser rule of grammar text as its alternatives, each a list of its elements (a quantifier or a parenthesis is an
    /// element of its own, and a literal is two, so an alternative holding either is never one token).</summary>
    private static IEnumerable<(string Name, List<List<string>> Alternatives)> GrammarRules(string g4)
    {
        string text = Regex.Replace(g4, @"//[^\n]*|/\*.*?\*/", " ", RegexOptions.Singleline);
        text = Regex.Replace(text, @"'(?:[^'\\]|\\.)*'", " LITERAL LITERAL ");
        text = StripActions(text);
        foreach (Match match in Regex.Matches(text, @"(?m)^(?<name>[a-z]\w*)\s*(?:returns\s*\[[^\]]*\])?\s*:(?<body>[^;]*);"))
        {
            // One parenthesized group around the whole body: its alternatives are the rule's.
            string body = Regex.Replace(match.Groups["body"].Value.Trim(), @"^\(([^()]*)\)$", "$1");
            var alternatives = body.Split('|')
                .Select(a => Regex.Matches(a, @"(?:\w+\s*=\s*)?[A-Za-z_]\w*|[()?*+]").Select(m => Regex.Replace(m.Value, @"^\w+\s*=\s*", "")).ToList())
                .ToList();
            yield return (match.Groups["name"].Value, alternatives);
        }
    }

    /// <summary>Grammar text with every <c>{ action }</c> block removed (nesting respected).</summary>
    private static string StripActions(string text)
    {
        var sb = new System.Text.StringBuilder(text.Length);
        int depth = 0;
        bool predicate = false;
        foreach (char c in text)
        {
            if (c == '{') depth++;
            else if (c == '}') { depth--; sb.Append(' '); predicate = depth == 0; }
            else if (depth == 0)
            {
                if (predicate && c == '?') { predicate = false; continue; }
                if (!char.IsWhiteSpace(c)) predicate = false;
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    /// <summary>A <c>GetText()</c> of a context that writes a QUALIFIED, SUBSCRIPTED or reference-modified reference (a data reference, a receiving
    /// identifier, an object reference). Whatever it is passed to, its text is not what the programmer wrote (<c>X OF Q</c> is <c>XOFQ</c>),
    /// so no argument it is handed to — a message parameter or a lookup key — receives a name: this shape is refused anywhere.</summary>
    private static bool IsReferenceGetText(SyntaxNode n, SemanticModel model) =>
        IsGetTextCall(n) && ReceiverType(n, model) is { Name: "DataReferenceContext" or "ReceivingIdentifierContext" or "ObjectReferenceContext" };

    private static IEnumerable<ExpressionSyntax> Operands(BinaryExpressionSyntax plus)
    {
        if (plus.Left is BinaryExpressionSyntax l && l.IsKind(SyntaxKind.AddExpression))
            foreach (var o in Operands(l)) yield return o;
        else yield return plus.Left;
        yield return plus.Right;
    }

    /// <summary>Whether <paramref name="e"/> IS a context's <c>GetText()</c> text: the call itself, a string method chained on it
    /// (<c>.Trim()</c>), or a <c>??</c>, <c>+</c>, conditional, parenthesis or null-forgiving operator over one. A <c>GetText()</c> that is an
    /// ARGUMENT of another call (<c>NumericLiteral.Normalize(num.GetText())</c>) is parsed into a value, so it is not carried.</summary>
    private static bool CarriesGetText(ExpressionSyntax e, SemanticModel model, HashSet<string> singleToken) =>
        Spine(e).Any(n => IsMultiTokenGetText(n, model, singleToken));

    /// <summary>The nodes of <paramref name="e"/> whose TEXT is the expression's text: itself, the receiver a string method is chained on
    /// (<c>x.Trim()</c> is <c>x</c>), and the operands of <c>??</c>, <c>+</c>, a conditional, a parenthesis, an indexer or a null-forgiving
    /// operator. The arguments of a call are not on it: <c>Table.Find(name)</c> uses <c>name</c> as a key, and <c>Normalize(text)</c> as a value.</summary>
    private static IEnumerable<SyntaxNode> Spine(ExpressionSyntax e)
    {
        yield return e;
        IEnumerable<ExpressionSyntax> parts = e switch
        {
            InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax m } => [m.Expression],
            ConditionalAccessExpressionSyntax c => [c.Expression, c.WhenNotNull],
            BinaryExpressionSyntax b when b.IsKind(SyntaxKind.CoalesceExpression) || b.IsKind(SyntaxKind.AddExpression) => [b.Left, b.Right],
            ConditionalExpressionSyntax q => [q.WhenTrue, q.WhenFalse],
            ParenthesizedExpressionSyntax p => [p.Expression],
            PostfixUnaryExpressionSyntax u => [u.Operand],
            ElementAccessExpressionSyntax x => [x.Expression],
            _ => [],
        };
        foreach (var part in parts)
            foreach (var node in Spine(part))
                yield return node;
    }

    /// <summary>A <c>GetText()</c> call (also <c>?.GetText()</c>) whose receiver is a parse context other than a one-token word or a terminal.</summary>
    private static bool IsMultiTokenGetText(SyntaxNode n, SemanticModel model, HashSet<string> singleToken)
    {
        if (!IsGetTextCall(n)) return false;
        var type = ReceiverType(n, model);
        if (type is null or IErrorTypeSymbol) return true;
        for (var t = type; t is not null; t = t.BaseType)
            if (t is { Name: "ParserRuleContext", ContainingNamespace.Name: "Runtime" })
                return !singleToken.Contains(type.Name);
        return false;
    }

    private static bool IsGetTextCall(SyntaxNode n) =>
        n is InvocationExpressionSyntax { ArgumentList.Arguments.Count: 0, Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "GetText" } or MemberBindingExpressionSyntax { Name.Identifier.ValueText: "GetText" } };

    /// <summary>The static type of the receiver of the <c>GetText()</c> call <paramref name="n"/> (also <c>x?.GetText()</c>).</summary>
    private static ITypeSymbol? ReceiverType(SyntaxNode n, SemanticModel model)
    {
        var call = (InvocationExpressionSyntax)n;
        ExpressionSyntax receiver = call.Expression is MemberAccessExpressionSyntax m
            ? m.Expression
            : call.Ancestors().OfType<ConditionalAccessExpressionSyntax>().First().Expression;
        return model.GetTypeInfo(receiver).Type;
    }

    private static bool IsRead(IdentifierNameSyntax id) => id.Parent is not MemberAccessExpressionSyntax { Name: var n } || n != id;

    private static bool IsManagedAssembly(string path)
    {
        try
        {
            using var pe = new PEReader(File.OpenRead(path));
            return pe.HasMetadata;
        }
        catch (BadImageFormatException) { return false; }
    }
}
