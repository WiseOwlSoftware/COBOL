// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Common;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Frontend.Parsing;

/// <summary>
/// ⭐ THE CHARACTER HALF OF ISO §8.3.2.1, ASKED OF EVERY WORD THE LEXER PRODUCED — before, and whether or not, the parse
/// succeeds (kb/Work PB3845). §8.3.2.1: "Each character of a COBOL word … shall be selected from the set of basic
/// letters, basic digits, extended letters, and the basic special characters hyphen and underscore"; §8.1.3.2 GR4 makes
/// the extended letters Annex B less the basic characters, and Annex B.3 places each one (anywhere, not first, medial).
/// <list type="bullet">
/// <item>the underscore is a COBOL-2002 introduction (<see cref="Constructs.UserWordUnderscore2002"/>; the '85 set is
/// hyphen-only);</item>
/// <item>below COBOL-2002 any extended letter is the construct <see cref="Constructs.UserWordExtendedLetter2002"/>
/// (COBOL0900) — but a character that no edition's Annex B holds (U+00A0, the euro sign) is no extended letter, so it is
/// COBOL2773 at every target (<see cref="CobolWordRule.CharacterViolation"/>, kb/Work PB4130);</item>
/// <item>at every edition a hyphen or underscore as the FIRST or LAST character of a word is COBOL2773
/// (<see cref="CobolCharacterRepertoire.SeparatorPositionViolation"/>): the lexer takes <c>_A</c> and <c>A_</c> whole as
/// ill-formed words (kb/Work PB4234), and the screen joins a hyphen that touches a word into the word it belongs to
/// (<see cref="WordScanner"/>, kb/Work PB4278); at COBOL-85 the underscore itself is also the construct above;</item>
/// <item>from 2002 a character where the edition's Annex B does not permit it is COBOL2773
/// (<see cref="CobolWordRule.CharacterViolation"/>), or — when the 2002/2014 Annex B permitted it and 2023's does not
/// (Annex E.2 item 4) — the removed construct <see cref="Constructs.UserWordCharacterRemoved2023"/> (COBOL0902; a
/// warning under <c>--permissive</c>).</item>
/// </list>
/// <para>⛔ WHY A TOKEN WALK AND NOT THE TREE FUNNEL. The test used to live in <c>VersionConformancePass.VisitCobolWord</c>,
/// which walks the parse TREE and so never runs on a source that does not parse. The U+00A0-only line of golden
/// <c>pb1660-nbsp-only-line-is-not-blank</c> is a word the lexer reads (<c>NAME_BODY</c> admits every non-basic
/// character, so the diagnostic can NAME the character instead of ending the word with "unexpected"): the word is
/// not where the grammar can use it, the parse fails at the NEXT token, and the user was told "cannot parse near
/// 'STOP'" without the cause. The lexer's word class is the single <see cref="CobolLexer.IDENTIFIER"/> token (only
/// <c>IDENTIFIER</c> is built from <c>NAME_BODY</c>; a reserved word has only basic characters), so one walk of the
/// words asks the rule of every word in every position — a name slot, a phrase word, a word the parser
/// rejects — and a new grammar position needs no arm. The one text that is not words is a comment-entry, which the
/// tree names (<see cref="CobolParserCore.CommentEntryContext"/>). The directive stages ask the same rule of the
/// words they scan themselves (<see cref="CobolWordRule.DirectiveWordViolations"/>): a directive is removed from the
/// text before the lexer runs.</para>
/// <para>⛔ A HYPHEN AT A WORD'S EDGE IS THE ONE CHARACTER THE LEXER CANNOT GIVE ITS WORD (kb/Work PB4278). §8.3.1: "A
/// character-string is delimited by separators", and the hyphen is no separator (§8.3.5), so <c>A-</c> and <c>-A</c> are
/// each ONE character-string, a COBOL word whose first or last character is a hyphen. But <c>-</c> is also the special
/// character word of the arithmetic minus, and the lexer cannot tell <c>A -B</c>, a subtraction written without its second
/// space, from <c>DISPLAY A -B</c>, a word <c>-B</c>: only the parse knows whether that hyphen is an operator. So the lexer
/// splits it (IDENTIFIER, MINUS) and this screen joins it back: a run of ADJACENT tokens (no separator between them) that
/// are all word characters and hold a MINUS is one word, the run's text, judged by the same rule as every other word.
/// Two such runs are not words: one holding a MINUS the tree made an arithmetic operator (<see cref="CobolParserCore.AddOpContext"/>:
/// that boundary is §8.7.1's — "shall be preceded by a space and followed by a space" — and
/// <see cref="SeparatorRule.ArithmeticOperatorViolations"/> reports it, one report per boundary), and a hyphen followed by
/// digits alone, a numeric literal's sign (§8.3.3.3.2 2), which the lexer cut from its digits outside a list region and the
/// literal rules own. A word token inside a joined run is judged as part of that run, never again alone.</para>
/// <para>⛔ TWO WORD SOURCES, ONE RULE, chosen by whether the binder will run. When the frontend reported ANY error (a
/// parse that FAILED, or one that parsed beside a separator, PICTURE or preprocessor error: the compiler driver stops
/// before the binder on any) the frontend asks it of the lexer's token stream (<see cref="RunOnTokens"/>; the tree may be
/// partial, so it only names the comment-entries and the arithmetic operators) and reports as frontend errors beside that
/// error. Otherwise the binder driver asks it of the tree's own words (<see cref="RunOnTree"/>), so the reports ride the
/// compiler's channel beside the binder's own diagnostics instead of stopping the compile before it (a source whose only
/// fault is a word character still draws every bind-time error). Both feed one <see cref="WordScanner"/> and run
/// <see cref="Check"/>.</para>
/// <para>One report per DISTINCT word per compilation (the Annex C fold), the posture the length ceiling has.</para>
/// </summary>
public static class WordCharacterScreen
{
    /// <summary>The frontend reported an error, so the binder will not run (the parse failed, or parsed beside another
    /// frontend error): screen every word of <paramref name="tokens"/> (the lexer's whole stream, in order).
    /// <paramref name="tree"/> is the unit's parse tree, complete or not: it names only the tokens that are NOT words,
    /// those of a comment-entry (<c>AUTHOR. JOSÉ</c> is a legal '85 comment), and the hyphens that are arithmetic operators.</summary>
    public static void RunOnTokens(IList<IToken> tokens, IParseTree tree, EditionInfo edition, IDiagnosticSink sink)
    {
        if (!HasCandidate(tokens)) return;   // the ordinary program: no word holds a character beyond letters, digits and hyphen
        var (commentEntries, operators) = TreeFacts(tree);
        var scanner = new WordScanner();
        foreach (var token in tokens)
        {
            if (token.Channel != Lexer.DefaultTokenChannel || token.Type == TokenConstants.EOF
                || commentEntries.Contains(token.TokenIndex))
                scanner.Break();
            else
                scanner.Add(token, operators.Contains(token.TokenIndex));
        }
        if (scanner.Finish() is { } words) Check(words, edition, sink);
    }

    /// <summary>The frontend reported no error, so the binder runs on the complete tree: screen every word the tree holds,
    /// outside its comment-entries, its terminals read in source order.</summary>
    public static void RunOnTree(IParseTree tree, EditionInfo edition, IDiagnosticSink sink)
    {
        var scanner = new WordScanner();
        var pending = new Stack<IParseTree>();
        pending.Push(tree);
        while (pending.Count > 0)
        {
            var node = pending.Pop();
            if (node is CobolParserCore.CommentEntryContext)
            {
                scanner.Break();
                continue;
            }
            if (node is ITerminalNode t)
            {
                if (t.Symbol.Type == TokenConstants.EOF) scanner.Break();
                else scanner.Add(t.Symbol, IsArithmeticOperator(t));
                continue;
            }
            for (int i = node.ChildCount - 1; i >= 0; i--) pending.Push(node.GetChild(i));
        }
        if (scanner.Finish() is { } words) Check(words, edition, sink);
    }

    /// <summary>A word the screen judges: its text (a word token's, or a joined run's) and where its first character is.</summary>
    private readonly record struct ScreenedWord(string Text, int Line, int Column);

    /// <summary>⭐ The words of a token sequence in source order (see the class remarks). Tokens are grouped into CHAINS of
    /// adjacent tokens — each one starting where the previous one stopped, so no separator stands between them — and a chain
    /// is read once it closes: a chain without a MINUS contributes its <see cref="IsCandidate"/> word tokens, as the screen
    /// always has; a chain with one is split into its runs of word-character tokens, and each run that holds a MINUS and
    /// another token is ONE word, unless it is an arithmetic operator's (§8.7.1 reports it) or a literal's sign.
    /// The only allocation on the ordinary path is the one reused chain list; a token's text is read only for the
    /// candidates and for the tokens of a chain that holds a MINUS.</summary>
    private sealed class WordScanner
    {
        private readonly List<(IToken Token, bool Operator)> _chain = [];
        private List<ScreenedWord>? _words;

        /// <summary>The next token of the sequence; <paramref name="isOperator"/> says the tree made it an arithmetic
        /// operator.</summary>
        public void Add(IToken token, bool isOperator)
        {
            if (_chain.Count > 0 && (token.StartIndex < 0 || token.StartIndex != _chain[^1].Token.StopIndex + 1)) Close();
            _chain.Add((token, isOperator));
        }

        /// <summary>A token that is no word of the program (a comment-entry's, a hidden one, the end of the stream): it
        /// ends the chain, and no run continues across it.</summary>
        public void Break() => Close();

        /// <summary>The words found, in source order; null when there are none.</summary>
        public List<ScreenedWord>? Finish()
        {
            Close();
            return _words;
        }

        private void Close()
        {
            bool hasMinus = false;
            foreach (var (token, _) in _chain) hasMinus |= token.Type == CobolLexer.MINUS;
            if (!hasMinus)
            {
                foreach (var (token, _) in _chain) AddCandidate(token);
            }
            else
            {
                int start = 0;
                for (int i = 0; i <= _chain.Count; i++)
                {
                    if (i < _chain.Count && IsWordCharacters(_chain[i].Token)) continue;
                    CloseRun(start, i);
                    start = i + 1;
                }
            }
            _chain.Clear();
        }

        /// <summary>The run <c>_chain[start..end)</c> of adjacent word-character tokens.</summary>
        private void CloseRun(int start, int end)
        {
            bool hasMinus = false, hasOperator = false;
            for (int i = start; i < end; i++)
            {
                hasMinus |= _chain[i].Token.Type == CobolLexer.MINUS;
                hasOperator |= _chain[i].Operator;   // §8.7.1's boundary, reported by SeparatorRule
            }
            if (end - start >= 2 && hasMinus && !hasOperator)
            {
                var text = new System.Text.StringBuilder();
                for (int i = start; i < end; i++) text.Append(_chain[i].Token.Text);
                string word = text.ToString();
                if (!IsSignedInteger(word))
                {
                    var first = _chain[start].Token;
                    (_words ??= []).Add(new(word, first.Line, first.Column));
                    return;
                }
            }
            for (int i = start; i < end; i++) AddCandidate(_chain[i].Token);
        }

        private void AddCandidate(IToken token)
        {
            if (IsCandidate(token, out string? text)) (_words ??= []).Add(new(text, token.Line, token.Column));
        }

        /// <summary>A hyphen followed by digits alone: a numeric literal's sign and its digits (§8.3.3.3.2 2).</summary>
        private static bool IsSignedInteger(string word)
        {
            if (word.Length < 2 || word[0] != '-') return false;
            for (int i = 1; i < word.Length; i++) if (!char.IsAsciiDigit(word[i])) return false;
            return true;
        }
    }

    /// <summary>Whether <paramref name="token"/>'s text is all word characters (<see cref="CobolCharacterRepertoire.IsWordCharacter"/>),
    /// so that it can be part of a word a hyphen touches: a word token, a reserved word, an integer, a hyphen — never a
    /// literal with its delimiters, a parenthesis or a period.</summary>
    private static bool IsWordCharacters(IToken token)
    {
        if (token.Type is CobolLexer.IDENTIFIER or CobolLexer.MINUS or CobolLexer.INTEGERLIT) return true;
        string? text = token.Text;
        if (string.IsNullOrEmpty(text)) return false;
        foreach (char c in text) if (!CobolCharacterRepertoire.IsWordCharacter(c)) return false;
        return true;
    }

    /// <summary>Whether the tree made this MINUS an arithmetic operator — a binary or unary <c>addOp</c> (§8.7.1), or a sign
    /// the VALUE clause's literal slot parses as one.</summary>
    private static bool IsArithmeticOperator(ITerminalNode t) =>
        t.Symbol.Type == CobolLexer.MINUS && t.Parent is CobolParserCore.AddOpContext;

    /// <summary>Whether <paramref name="tokens"/> hold anything the screen judges: a <see cref="IsCandidate"/> word token, or a
    /// MINUS that touches the token before or after it (a hyphen at a word's edge, or an operator §8.7.1 judges).</summary>
    private static bool HasCandidate(IList<IToken> tokens)
    {
        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (token.Channel != Lexer.DefaultTokenChannel) continue;
            if (token.Type == CobolLexer.MINUS)
            {
                if ((i > 0 && tokens[i - 1].StopIndex + 1 == token.StartIndex)
                    || (i + 1 < tokens.Count && tokens[i + 1].StartIndex == token.StopIndex + 1))
                    return true;
            }
            else if (IsCandidate(token, out _)) return true;
        }
        return false;
    }

    /// <summary>A word token holding an underscore or a character outside the basic repertoire. The text is read once:
    /// a token's text is cut from the input on every call. A word of a fixed-form debugging line that is a COMMENT (its
    /// unit has no WITH DEBUGGING MODE, so <see cref="DebuggingLineRewriter"/> moved it to the
    /// <see cref="CobolLexer.ABSENT_DEBUG_LINE"/> channel) is not a COBOL word of the program, as the sibling readers of
    /// the stream decide it too (<see cref="SeparatorRule"/>, <see cref="PictureSeparatorPeriodRule"/>; kb/Work PB1705):
    /// both callers pass only the default channel's tokens.</summary>
    private static bool IsCandidate(IToken token, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? text)
    {
        text = null;
        if (token.Type != CobolLexer.IDENTIFIER) return false;
        text = token.Text;
        return text.Contains('_') || CobolCharacterRepertoire.HasExtendedCharacter(text);
    }

    private static void Check(List<ScreenedWord> words, EditionInfo edition, IDiagnosticSink sink)
    {
        var reported = new HashSet<string>(CobolNames.Comparer);
        foreach (var (word, line, column) in words)
        {
            if (!reported.Add(word)) continue;
            using var _ = sink.At(line, column);
            if (word.Contains('_'))
                ConstructRegistry.Check(edition, sink, Constructs.UserWordUnderscore2002,
                    $"the underscore in the COBOL word '{word}'");
            string? character = CobolWordRule.CharacterViolation(word, edition.Year);
            // The 2002 gate names an EXTENDED LETTER, decided per CHARACTER: a character in no edition's Annex B is none
            // (CharacterViolation answers it as COBOL2773 at every target, kb/Work PB4130), but the same word may also hold
            // a real one, which COBOL-85 lacks and the gate must still name (kb/Work PB4156). From 2002 the gate is a no-op.
            if (CobolCharacterRepertoire.HasExtendedLetter(word))
                ConstructRegistry.Check(edition, sink, Constructs.UserWordExtendedLetter2002,
                    $"the extended letter in the COBOL word '{word}'");
            if (character is null) continue;
            if (CobolWordRule.RemovedAt2023(word, edition.Year))
                ConstructRegistry.Check(edition, sink, Constructs.UserWordCharacterRemoved2023, character);
            else
                sink.Report(new EditionDiagnostic(DiagnosticCatalog.WordCharacterNotPermitted.Code,
                    EditionSeverity.Error, DiagnosticCatalog.WordCharacterNotPermitted.Id, character, "",
                    "ISO §8.3.2.1; Annex B.3"));
        }
    }

    /// <summary>One walk of <paramref name="tree"/>, which does not descend into a comment-entry: the stream indexes of every
    /// token a comment-entry holds, and of every MINUS the tree made an arithmetic operator.</summary>
    private static (HashSet<int> CommentEntries, HashSet<int> Operators) TreeFacts(IParseTree tree)
    {
        var commentEntries = new HashSet<int>();
        var operators = new HashSet<int>();
        var pending = new Stack<IParseTree>();
        pending.Push(tree);
        while (pending.Count > 0)
        {
            var node = pending.Pop();
            if (node is CobolParserCore.CommentEntryContext entry)
            {
                if (entry.Stop is not null)
                    for (int i = entry.Start.TokenIndex; i <= entry.Stop.TokenIndex; i++) commentEntries.Add(i);
                continue;
            }
            if (node is ITerminalNode t)
            {
                if (IsArithmeticOperator(t)) operators.Add(t.Symbol.TokenIndex);
                continue;
            }
            for (int i = node.ChildCount - 1; i >= 0; i--) pending.Push(node.GetChild(i));
        }
        return (commentEntries, operators);
    }
}
