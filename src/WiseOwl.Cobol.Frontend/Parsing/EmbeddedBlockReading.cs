// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Frontend.Parsing;

/// <summary>
/// ⭐ AN EMBEDDED PRECOMPILER BLOCK IS ONE TOKEN THAT STANDS FOR WHAT ITS PRECOMPILER WRITES IN ITS PLACE, FROM BEFORE THE
/// FIRST READER OF THE STREAM (kb/Work PB4341, review R1-1). ISO/IEC 1989 defines no EXEC construct: <c>EXEC SQL …
/// END-EXEC</c> is the input of a vendor's precompiler, which ISO §4.2.14 lets an implementation require ("This permits an
/// implementation to require components outside the COBOL implementation, such as precompilers"), and this implementation
/// has none, so every block is refused, once, at its EXEC (COBOL5818, <see cref="Report"/>); a block left without its
/// END-EXEC is refused as that fault (COBOL5842).
/// <para>A block is EXEC, a word naming the embedded language, and the text through the first END-EXEC, within one unit:
/// no separator period between them (ISO §8.3.5 rule 3, "The COBOL character period, when followed by a space, is a
/// separator"), so the period of a qualified SQL name, <c>T.A</c>, is text of the block (review R1-2). <see cref="Read"/>
/// makes each block one <c>EMBEDDED_BLOCK</c> token spanning it and moves the rest of its tokens to the
/// <c>EMBEDDED_BLOCK_TEXT</c> channel, which the parser does not read and the screens of the COBOL text skip
/// (<see cref="ProgramText"/>). Where a statement may begin, the parse takes the token as one
/// (<c>embeddedBlockStatement</c>), so a block that is the only statement of an IF's or an ELSE's branch, a WHEN, or an
/// inline PERFORM leaves that construct whole (ISO §14.9.19.3 1)); elsewhere (an entry of WORKING-STORAGE)
/// <see cref="CobolErrorStrategy"/> consumes it as nothing. Done before the parse, every prediction sees the token: a
/// block the parse met as text was mispredicted (<c>WHEN 1 EXEC SQL …</c> read "1 IS EXEC", a class condition) or
/// failed a decision made before it (<c>WHEN OTHER EXEC SQL …</c>), and drew false cascades.</para>
/// <para>⛔ EXEC IS NO RESERVED WORD (§8.9 lists none of EXEC, END-EXEC), so a program may name a data item EXEC, and
/// then <c>DISPLAY EXEC SQL END-EXEC.</c> is three operands. The source reads EXEC as a block's opener only where it
/// cannot be a word: when no EXEC of the source is one. An EXEC that opens no block (a declaration, <c>01 EXEC PIC
/// X.</c>; a reference) means the program writes EXEC as a COBOL word, and the whole source is read as COBOL, as the
/// standard reads it. A precompiler, which takes every EXEC as a block's opener, could not translate that source either.
/// An EXEC followed by a user-defined word whose sentence ends before any END-EXEC (<see cref="Scan"/>) is a block left
/// unterminated, or a word the text cannot tell from one (<c>INDEXED BY EXEC IDX.</c>, <c>SYMBOLIC CHARACTERS EXEC BEL ARE
/// …</c>: a declaration followed by another user-defined word); only the COBOL grammar settles it, so <see cref="Read"/>
/// asks the parse (kb/Work PB4381, review R1-1).</para>
/// <para>One reading per lex: the tokens it changed are that stream's, and every parse pass over it reads them.</para>
/// </summary>
public sealed class EmbeddedBlockReading
{
    /// <summary>The EXEC token of every block (as the source spelled it, before the token stood in for the block), with
    /// the word naming its embedded language and whether the block closes (has its END-EXEC), in source order.</summary>
    private readonly List<(IToken Exec, string Language, bool Closed)> _blocks;

    private EmbeddedBlockReading(List<(IToken Exec, string Language, bool Closed)> blocks) => _blocks = blocks;

    /// <summary>What one EXEC is: the opener of a block that ends at its END-EXEC, the opener of a block its sentence
    /// ends before any END-EXEC (a block left unterminated), or a COBOL word.</summary>
    private enum Opening { Word, Closed, Unterminated }

    /// <summary>Make every embedded block of <paramref name="stream"/> one <c>EMBEDDED_BLOCK</c> token, unless one EXEC
    /// of it is a COBOL word (the source writes EXEC as a word): one scan of the default channel, which reads the text
    /// only of a four-character IDENTIFIER, and of the tokens of a block.
    /// <para><paramref name="parsesAsCobol"/> is asked only when an EXEC is followed by a user-defined word and its
    /// sentence ends before any END-EXEC (a block left unterminated, or a word the text cannot tell from one): does the
    /// source parse with no syntax error when every EXEC is read as a COBOL word? Then EXEC is one, the source is legal
    /// COBOL and is read as such; otherwise that EXEC opens a block left unterminated (COBOL5842). It is never asked of a
    /// source of closed blocks alone, nor of one that holds an EXEC no block can open.</para></summary>
    public static EmbeddedBlockReading Read(BufferedTokenStream stream, Func<bool> parsesAsCobol)
    {
        stream.Fill();
        var tokens = stream.GetTokens();
        var found = new List<(int Exec, string Language, int Last, bool Closed)>();
        bool unterminated = false;
        for (int i = 0; i < tokens.Count; i++)
        {
            if (!IsExec(tokens[i])) continue;
            var (opening, language, last) = Scan(tokens, i);
            if (opening == Opening.Word) return new([]);
            found.Add((i, language, last, opening == Opening.Closed));
            unterminated |= opening == Opening.Unterminated;
            i = last;
        }
        if (unterminated && parsesAsCobol()) return new([]);
        var blocks = new List<(IToken Exec, string Language, bool Closed)>(found.Count);
        foreach (var (exec, language, last, closed) in found)
        {
            blocks.Add((tokens[exec], language, closed));
            int end = tokens[last].StopIndex;
            for (int i = exec; i <= last; i++)
            {
                var t = tokens[i];
                // ⛔ Every token of the block that is the program's COBOL text, whatever its channel (kb/Work PB4384): a
                // semicolon with no space after it is on the HIDDEN channel so the separator screen can judge it (ISO
                // §8.3.5 rule 2), and inside a block it is SQL text. A token already off the COBOL text (an absent
                // debugging line) stays where it is.
                if (!ProgramText.Holds(t)) continue;
                tokens[i] = i == exec
                    ? new CommonToken(t) { Type = CobolParserCore.EMBEDDED_BLOCK, StopIndex = end }
                    : new CommonToken(t) { Channel = CobolLexer.EMBEDDED_BLOCK_TEXT };
            }
        }
        return new(blocks);
    }

    /// <summary>What the EXEC at <paramref name="exec"/> is, the word naming its block's language, and the index of the
    /// block's last default-channel token (its END-EXEC, or the token before the separator period or the end of the text
    /// that ends an unterminated one).
    /// <para>⛔ A block needs a word after its EXEC. When that word is a user-defined word (an IDENTIFIER: SQL, CICS and
    /// DLI are none of the reserved words of any edition, ISO §8.9) and the sentence ends before an END-EXEC (ISO §8.3.5
    /// rule 3, "The COBOL character period, when followed by a space, is a separator"), the EXEC may open a block left
    /// unterminated, a fault of the embedded text: <see cref="Read"/> reports it as itself and the blocks that do close
    /// stay one token each. EXEC followed by anything else (a reserved word, the period, a literal) is a COBOL word.</para>
    /// <para>That shape is only a CANDIDATE: a declaration that takes a list of user-defined words (<c>INDEXED BY EXEC
    /// IDX</c>, <c>SYMBOLIC CHARACTERS EXEC BEL</c>, <c>ASCENDING KEY IS EXEC B</c>) puts a user-defined word after a
    /// declared EXEC too, and so does any operand list. Which it is, only the grammar knows, and <see cref="Read"/> asks
    /// the parse rather than keep a list of the positions.</para></summary>
    private static (Opening Opening, string Language, int Last) Scan(IList<IToken> tokens, int exec)
    {
        int at = NextDefault(tokens, exec);
        if (at < 0 || !IsWord(tokens[at])) return (Opening.Word, "", exec);
        string language = CobolNames.UpperFold(tokens[at].Text);
        bool userWord = tokens[at].Type == CobolKeywordTokens.IdentifierType;
        int last = at;
        for (at = NextDefault(tokens, at); at >= 0; at = NextDefault(tokens, at))
        {
            var t = tokens[at];
            if (t.Type == CobolLexer.DOT && SeparatorRule.IsSeparatorPeriod(t)) break;
            if (IsWordSpelled(t, "END-EXEC")) return (Opening.Closed, language, at);
            last = at;
        }
        return userWord ? (Opening.Unterminated, language, last) : (Opening.Word, "", exec);
    }

    /// <summary>The index of the next default-channel token after <paramref name="index"/> that is not the end of the
    /// text, or -1.</summary>
    private static int NextDefault(IList<IToken> tokens, int index)
    {
        for (int i = index + 1; i < tokens.Count; i++)
        {
            if (tokens[i].Type == TokenConstants.EOF) return -1;
            if (tokens[i].Channel == TokenConstants.DefaultChannel) return i;
        }
        return -1;
    }

    /// <summary>A default-channel IDENTIFIER spelled EXEC. Only a four-character one has its text read.</summary>
    private static bool IsExec(IToken token)
        => token.Type == CobolKeywordTokens.IdentifierType && token.Channel == TokenConstants.DefaultChannel
           && token.StopIndex - token.StartIndex == 3 && CobolNames.Same(token.Text, "EXEC");

    /// <summary>A COBOL word token (a user-defined word or a keyword) whose spelling is <paramref name="word"/>.</summary>
    private static bool IsWordSpelled(IToken token, string word) => IsWord(token) && CobolNames.Same(token.Text, word);

    /// <summary>A COBOL word: an IDENTIFIER, or a keyword token of its own spelling (a literal, a separator or a picture
    /// string is none).</summary>
    private static bool IsWord(IToken token)
        => token.Type == CobolKeywordTokens.IdentifierType
           || CobolKeywordTokens.TryTokenType(token.Text, out int type) && type == token.Type;

    /// <summary>The message of COBOL5818 for a closed block, or of COBOL5842 for an unterminated one, of
    /// <paramref name="language"/>.</summary>
    private static string Message(string language, bool closed)
        => closed
            ? $"[{Diagnostics.DiagnosticDescriptors.COBOL5818.Code}] EXEC {language}: ISO/IEC 1989 defines no EXEC construct. "
              + $"The text through END-EXEC is an embedded {language} block, the input of a precompiler that translates it to "
              + "COBOL before compilation (ISO §4.2.14), and this implementation has no precompiler. Run the source through "
              + "the precompiler first and compile the COBOL it writes."
            : $"[{Diagnostics.DiagnosticDescriptors.COBOL5842.Code}] EXEC {language}: the embedded {language} block has no "
              + "END-EXEC. The separator period that ends the sentence (ISO §8.3.5 rule 3) comes first, so the block is taken "
              + "through the text before that period. Write END-EXEC after the block's text and before that period.";

    /// <summary>Does the source hold a block? Then it does not parse, whatever the parse's own syntax errors.</summary>
    public bool Any => _blocks.Count > 0;

    /// <summary>Report each block once, at its EXEC, in source order, through <paramref name="report"/> (the token and the
    /// message, the shape of a syntax error).</summary>
    public void Report(Action<IToken, string> report)
    {
        foreach (var (exec, language, closed) in _blocks) report(exec, Message(language, closed));
    }
}
