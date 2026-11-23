// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol;

/// <summary>
/// ⛔ WHAT WISEOWL'S PARSE OF A PROGRAM CONTAINS, AS TOKEN STREAMS (DESIGN-conformance-suite.md section 3.4, kb/Work PB6050).
/// The conformance suite credits a coverage point (an alternative of a general format) when the parse of a test accepted at an
/// edition contains it (decision 8). This reader hands that measure the parse and nothing else: for each parse node of a
/// rule the caller names (a <em>frame</em>: a statement, a clause, an entry), the tokens of its text in order; a nested
/// statement (a <c>statement</c> context) is one placeholder token, and a context of a rule the caller names an <em>operand</em>
/// is one placeholder token when it holds more than one token (a context of one token is that token, whose type is the operand's
/// kind), so an operand of a general format (<c>identifier-1</c>, <c>literal-1</c>) is one item of the stream whatever its length.
/// It decides nothing about the program, and the caller owns which rules are frames and which are operands, so no point's rule is
/// written into the compiler.
/// </summary>
public static class ParseNodeReader
{
    /// <summary>One token of a node's text: its text in upper case and the lexer's name for its type (<c>IDENTIFIER</c>,
    /// <c>INTEGERLIT</c>, a keyword's own name); a placeholder token has the type <see cref="PlaceholderType"/> and the
    /// rule it stands for as its text.</summary>
    public sealed record Token(string Text, string Type);

    /// <summary>One parse node of a frame rule: the rule's name, the line it starts on and its tokens.</summary>
    public sealed record Node(string Rule, int Line, IReadOnlyList<Token> Tokens);

    /// <summary>The nodes of one program. <see cref="Parsed"/> is false when the front end reported an error: the program has no
    /// parse to read, and <see cref="Nodes"/> is empty.</summary>
    public sealed record Reading(bool Parsed, IReadOnlyList<Node> Nodes);

    /// <summary>What a reading asks of the parse.</summary>
    /// <param name="Frames">The rule names (lower camel case, as the grammar writes them) whose nodes are read.</param>
    /// <param name="Operands">The rule names whose contexts, inside a node, stand as one placeholder token each when they hold
    /// more than one token.</param>
    /// <param name="Flat">The frames whose nodes keep every token of their operands: a subscripted reference shows its subscripts.</param>
    public sealed record Selection(IReadOnlySet<string> Frames, IReadOnlySet<string> Operands, IReadOnlySet<string> Flat);

    /// <summary>The type name of a placeholder token.</summary>
    public const string PlaceholderType = "<context>";

    /// <summary>The text of the placeholder a nested statement becomes.</summary>
    public const string NestedStatement = "<statement>";

    /// <summary>The grammar rule every statement is wrapped in (<c>statement</c>, the alternatives of <c>statementBlock</c>): a context
    /// of it nested in a node is one statement of that node's text, whichever statement it holds.</summary>
    private const string StatementRule = "statement";

    /// <summary>Parse <paramref name="options"/>' source under its edition and read the nodes <paramref name="selection"/> names.
    /// A node whose rule and tokens repeat one already read is read once.</summary>
    public static Reading Read(CompilerDriver.Options options, Selection selection)
    {
        var diagnostics = new DiagnosticBag();
        var frontend = CompilerDriver.NewFrontend(options, new Frontend.CompilationInputs(), recordsListing: false);
        if (frontend.Parse(options.SourcePath, diagnostics) is not { } tree || diagnostics.HasErrors)
            return new Reading(false, []);
        var nodes = new List<Node>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        Walk(tree, selection, nodes, seen);
        return new Reading(true, nodes);
    }

    private static void Walk(IParseTree tree, Selection selection, List<Node> nodes, HashSet<string> seen)
    {
        if (tree is not ParserRuleContext context) return;
        string rule = RuleName(context);
        if (selection.Frames.Contains(rule) && context.Start is { } start)
        {
            var tokens = new List<Token>();
            Collect(context, selection, selection.Flat.Contains(rule), tokens, isRoot: true);
            string key = rule + "\u0001" + string.Join('\u0002', tokens.Select(t => t.Text + "\u0003" + t.Type));
            if (seen.Add(key)) nodes.Add(new Node(rule, start.Line, tokens));
        }
        for (int i = 0; i < context.ChildCount; i++) Walk(context.GetChild(i), selection, nodes, seen);
    }

    private static void Collect(IParseTree tree, Selection selection, bool flat, List<Token> tokens, bool isRoot)
    {
        switch (tree)
        {
            case ITerminalNode { Symbol: { Type: not TokenConstants.EOF } symbol }:
                tokens.Add(new Token(symbol.Text.ToUpperInvariant(), CobolLexer.DefaultVocabulary.GetSymbolicName(symbol.Type) ?? symbol.Type.ToString()));
                return;
            case ParserRuleContext context:
                string rule = RuleName(context);
                if (!isRoot && rule == StatementRule)
                {
                    tokens.Add(new Token(NestedStatement, PlaceholderType));
                    return;
                }
                if (!isRoot && !flat && selection.Operands.Contains(rule))
                {
                    var inner = new List<Token>();
                    for (int i = 0; i < context.ChildCount; i++) Collect(context.GetChild(i), selection, flat, inner, isRoot: false);
                    tokens.Add(inner.Count == 1 ? inner[0] : new Token(rule, PlaceholderType));
                    return;
                }
                for (int i = 0; i < context.ChildCount; i++) Collect(context.GetChild(i), selection, flat, tokens, isRoot: false);
                return;
        }
    }

    /// <summary>The grammar's name of a context's rule: the generated class's name without <c>Context</c>, first letter lower case.</summary>
    private static string RuleName(ParserRuleContext context)
    {
        string name = context.GetType().Name;
        if (name.EndsWith("Context", StringComparison.Ordinal)) name = name[..^"Context".Length];
        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}
