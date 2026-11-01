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
/// (<see cref="CobolCharacterRepertoire.SeparatorPositionViolation"/>; the lexer takes <c>_A</c> and <c>A_</c> whole as
/// ill-formed words so that this names them, kb/Work PB4234); at COBOL-85 the underscore itself is also the construct above;</item>
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
/// <para>⛔ TWO WORD SOURCES, ONE RULE, chosen by whether the binder will run. When the frontend reported ANY error (a
/// parse that FAILED, or one that parsed beside a separator, PICTURE or preprocessor error: the compiler driver stops
/// before the binder on any) the frontend asks it of the lexer's token stream (<see cref="RunOnTokens"/>; the tree may be
/// partial, so it only names the comment-entries) and reports as frontend errors beside that error. Otherwise the binder
/// driver asks it of the tree's own words (<see cref="RunOnTree"/>), so the reports ride the compiler's channel beside the
/// binder's own diagnostics instead of stopping the compile before it (a source whose only fault is a word character
/// still draws every bind-time error). Both run <see cref="Check"/>.</para>
/// <para>One report per DISTINCT word per compilation (the Annex C fold), the posture the length ceiling has.</para>
/// </summary>
public static class WordCharacterScreen
{
    /// <summary>The frontend reported an error, so the binder will not run (the parse failed, or parsed beside another
    /// frontend error): screen every word of <paramref name="tokens"/> (the lexer's whole stream).
    /// <paramref name="tree"/> is the unit's parse tree, complete or not: it names only the tokens that are NOT words,
    /// those of a comment-entry (<c>AUTHOR. JOSÉ</c> is a legal '85 comment).</summary>
    public static void RunOnTokens(IEnumerable<IToken> tokens, IParseTree tree, EditionInfo edition, IDiagnosticSink sink)
    {
        List<IToken>? candidates = null;
        foreach (var token in tokens)
            if (IsCandidate(token)) (candidates ??= []).Add(token);
        if (candidates is null) return;   // the ordinary program: no word holds a character beyond letters, digits and hyphen
        var commentEntries = CommentEntryTokens(tree);
        Check(candidates.Where(t => !commentEntries.Contains(t.TokenIndex)), edition, sink);
    }

    /// <summary>The frontend reported no error, so the binder runs on the complete tree: screen every word the tree holds,
    /// outside its comment-entries.</summary>
    public static void RunOnTree(IParseTree tree, EditionInfo edition, IDiagnosticSink sink)
    {
        List<IToken>? candidates = null;
        var pending = new Stack<IParseTree>();
        pending.Push(tree);
        while (pending.Count > 0)
        {
            var node = pending.Pop();
            if (node is CobolParserCore.CommentEntryContext) continue;
            if (node is ITerminalNode t)
            {
                if (IsCandidate(t.Symbol)) (candidates ??= []).Add(t.Symbol);
                continue;
            }
            for (int i = node.ChildCount - 1; i >= 0; i--) pending.Push(node.GetChild(i));
        }
        if (candidates is not null) Check(candidates, edition, sink);
    }

    /// <summary>A word token holding an underscore or a character outside the basic repertoire. The text is read once:
    /// a token's text is cut from the input on every call. A word of a fixed-form debugging line that is a COMMENT (its
    /// unit has no WITH DEBUGGING MODE, so <see cref="DebuggingLineRewriter"/> moved it to the
    /// <see cref="CobolLexer.ABSENT_DEBUG_LINE"/> channel) is not a COBOL word of the program, as the sibling readers of
    /// the stream decide it too (<see cref="SeparatorRule"/>, <see cref="PictureSeparatorPeriodRule"/>; kb/Work PB1705).</summary>
    private static bool IsCandidate(IToken token)
    {
        if (token.Type != CobolLexer.IDENTIFIER || token.Channel == CobolLexer.ABSENT_DEBUG_LINE) return false;
        string text = token.Text;
        return text.Contains('_') || CobolCharacterRepertoire.HasExtendedCharacter(text);
    }

    private static void Check(IEnumerable<IToken> words, EditionInfo edition, IDiagnosticSink sink)
    {
        var reported = new HashSet<string>(CobolNames.Comparer);
        foreach (var token in words)
        {
            string word = token.Text;
            if (!reported.Add(word)) continue;
            using var _ = sink.At(token.Line, token.Column);
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

    /// <summary>The stream indexes of every token a comment-entry holds, found by one walk of <paramref name="tree"/>
    /// that does not descend into an entry.</summary>
    private static HashSet<int> CommentEntryTokens(IParseTree tree)
    {
        var indexes = new HashSet<int>();
        var pending = new Stack<IParseTree>();
        pending.Push(tree);
        while (pending.Count > 0)
        {
            var node = pending.Pop();
            if (node is CobolParserCore.CommentEntryContext entry)
            {
                if (entry.Stop is not null)
                    for (int i = entry.Start.TokenIndex; i <= entry.Stop.TokenIndex; i++) indexes.Add(i);
                continue;
            }
            for (int i = node.ChildCount - 1; i >= 0; i--) pending.Push(node.GetChild(i));
        }
        return indexes;
    }
}
