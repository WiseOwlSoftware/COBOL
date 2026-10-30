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
/// (COBOL0900);</item>
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
/// token stream asks the rule of every word in every position — a name slot, a phrase word, a word the parser
/// rejects — and a new grammar position needs no arm. The one text that is not words is a comment-entry, which the
/// tree names (<see cref="CobolParserCore.CommentEntryContext"/>). The directive stages ask the same rule of the
/// words they scan themselves (<see cref="CobolWordRule.DirectiveWordViolations"/>): a directive is removed from the
/// text before the lexer runs.</para>
/// <para>One report per DISTINCT word per compilation (the Annex C fold), the posture the length ceiling has.</para>
/// </summary>
public static class WordCharacterScreen
{
    /// <summary>Screen every word of <paramref name="tokens"/> against the targeted <paramref name="edition"/>,
    /// reporting each violation to the sink <paramref name="sinkAt"/> answers for the word's token (the sink carries
    /// the diagnostic's location). <paramref name="tree"/> is the unit's parse tree, complete or not: it names only
    /// the tokens that are NOT words, those of a comment-entry (<c>AUTHOR. JOSÉ</c> is a legal '85 comment).</summary>
    public static void Run(IEnumerable<IToken> tokens, IParseTree tree, EditionInfo edition, Func<IToken, IDiagnosticSink> sinkAt)
    {
        List<IToken>? candidates = null;
        foreach (var token in tokens)
            if (token.Type == CobolLexer.IDENTIFIER && (token.Text.Contains('_') || CobolCharacterRepertoire.HasExtendedCharacter(token.Text)))
                (candidates ??= []).Add(token);
        if (candidates is null) return;   // the ordinary program: no word holds a character beyond letters, digits and hyphen

        var commentEntries = CommentEntryTokens(tree);
        var reported = new HashSet<string>(CobolNames.Comparer);
        foreach (var token in candidates)
        {
            string word = token.Text;
            if (commentEntries.Contains(token.TokenIndex) || !reported.Add(word)) continue;
            var sink = sinkAt(token);
            if (word.Contains('_'))
                ConstructRegistry.Check(edition, sink, Constructs.UserWordUnderscore2002,
                    $"the underscore in the COBOL word '{word}'");
            if (!CobolCharacterRepertoire.HasExtendedCharacter(word)) continue;
            ConstructRegistry.Check(edition, sink, Constructs.UserWordExtendedLetter2002,
                $"the extended letter in the COBOL word '{word}'");
            if (CobolWordRule.CharacterViolation(word, edition.Year) is not { } character) continue;
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
