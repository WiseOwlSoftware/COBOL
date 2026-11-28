// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Parsing;

namespace WiseOwl.Cobol.Frontend.Parsing;

/// <summary>
/// ⭐ THE ONE PREPARATION OF A TOKEN STREAM AND ITS PARSER (kb/Work PB2735). Every reading of COBOL source text — the
/// whole group's parse (<c>Frontend.LexAndParseOnce</c>), its reading of the debugging lines as source
/// (<c>Frontend.ReportDebuggingLinesBreakingTheProgramAsSource</c>), the fragment re-parse
/// (<see cref="FragmentParse.Parse"/>), a recorded skeleton's re-parse (<see cref="FragmentParse.ParseUnit"/>) and the
/// keyword probe (<c>CobolKeywordTokens.LexProbe</c>) — takes its tokens from <see cref="Prepare"/> and its parser from
/// <see cref="Parser"/>, so the steps run in ONE order, written here:
/// <list type="number">
/// <item><b>Lex</b> (<see cref="Lexer"/>): the lexer is primed with the compile's word decisions at the edition
/// (<see cref="TokenRetypes.PrimeLexer"/>: the <c>&gt;&gt;COBOL-WORDS</c> plan, the reserved words that are no subscript
/// trigger, the Annex C spelling) BEFORE any tokenization, then the reading's own lexer setup (a fragment's region prime),
/// and it reports only to the reading's error listener — never to ANTLR's console listener (kb/Work PB4233).</item>
/// <item><b>The debugging lines</b>, FIRST of the rewriters (<see cref="DebuggingLineRewriter"/>: every later reader must
/// see only the lines that are source); the step may ask for the text to be lexed again (kb/Work PB1913).</item>
/// <item><b>ZERO beside an arithmetic operator</b> (<see cref="ZeroTokenRewriter"/>), when the reading asks.</item>
/// <item><b>The §2023:8.9 reservation gate's retype</b> (<see cref="TokenRetypes.Rewrite"/> for a stream of the whole source
/// text, <see cref="TokenRetypes.RewriteSlice"/> for a fragment of it).</item>
/// </list>
/// A reading chooses WHICH debugging-line step, whether ZERO is rewritten and which retype applies — each an explicit
/// argument, never a default, because the fragment copies that once inherited an answer by omission were both defects
/// (PB50, PB54; <see cref="FragmentParse"/>) — but it never chooses the order.
/// </summary>
public static class CobolTokenStream
{
    /// <summary>The debugging-line step of a reading: it settles the debugging lines of <paramref name="tokens"/>, freshly
    /// lexed from <paramref name="text"/>, and answers null, or answers the text to lex again (a debugging line that is source
    /// inside a region the lexer skipped has its carrier blanked in <paramref name="text"/>, kb/Work PB1913).</summary>
    public delegate string? DebuggingLineStep(CommonTokenStream tokens, string text);

    /// <summary>The step of a slice of text the whole parse already read: a debugging line that is source had its carrier
    /// blanked before the main lex (kb/Work PB1913), so a carrier left in the slice is a COMMENT line, hidden exactly as the
    /// main lexer's region skip hid it (kb/Work PB1705).</summary>
    public static readonly DebuggingLineStep HideDebuggingLines = (tokens, _) =>
    {
        DebuggingLineRewriter.HideAll(tokens);
        return null;
    };

    /// <summary>The step of a text that holds no debugging-line carrier to settle: a lone word the keyword probe asks about,
    /// and a recorded skeleton, which is free-form text after text manipulation (<see cref="FragmentParse.ParseUnit"/>).</summary>
    public static readonly DebuggingLineStep NoDebuggingLines = (_, _) => null;

    /// <summary>Which §2023:8.9 reservation-gate retype a stream takes (<see cref="TokenRetypes"/>).</summary>
    public enum Retype
    {
        /// <summary>A stream of the whole source text the decisions were taken on (<see cref="TokenRetypes.Rewrite"/>).</summary>
        WholeText,

        /// <summary>A stream of a slice of it, whose offsets are the slice's (<see cref="TokenRetypes.RewriteSlice"/>).</summary>
        Slice,
    }

    /// <summary>Step 1: the lexer of <paramref name="text"/>, primed with <paramref name="retypes"/> at
    /// <paramref name="edition"/>, then set up by <paramref name="setup"/> (a fragment's region prime, the listing's
    /// comment recorder), reporting to <paramref name="errors"/> alone.</summary>
    public static CobolLexer Lexer(string text, TokenRetypes retypes, EditionInfo edition, IAntlrErrorListener<int> errors,
        Action<CobolLexer>? setup = null)
    {
        var lexer = new CobolLexer(new AntlrInputStream(text));
        retypes.PrimeLexer(lexer, edition);
        setup?.Invoke(lexer);
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(errors);
        return lexer;
    }

    /// <summary>The filled, normalized token stream of <paramref name="text"/> (the class summary's steps, in their order),
    /// and the text it was lexed from — <paramref name="text"/> itself, or the text the debugging-line step blanked.</summary>
    /// <param name="errors">The lexer error listener of each lex: called once per lex, so a reading that lexes again keeps
    /// the LAST lexer's errors, which alone describe the text the parse reads (kb/Work PB4233).</param>
    public static (CommonTokenStream Tokens, string Text) Prepare(string text, EditionInfo edition, TokenRetypes retypes,
        Func<IAntlrErrorListener<int>> errors, DebuggingLineStep debuggingLines, bool rewriteZero, Retype retype,
        Action<CobolLexer>? setup = null)
    {
        CommonTokenStream tokens;
        while (true)
        {
            tokens = new CommonTokenStream(Lexer(text, retypes, edition, errors(), setup));
            if (debuggingLines(tokens, text) is not { } again) break;
            text = again;
        }
        if (rewriteZero) ZeroTokenRewriter.Rewrite(tokens);
        if (retype == Retype.WholeText) retypes.Rewrite(tokens);
        else retypes.RewriteSlice(tokens);
        return (tokens, text);
    }

    /// <summary>The parser of <paramref name="tokens"/>: handed the edition (both axes) and the words map its text
    /// predicates resolve §2023:8.9/§2023:8.10 words through (kb/Work PB250, PB693), reporting to <paramref name="errors"/>
    /// alone — none until the reading attaches its own, never ANTLR's console listener.</summary>
    public static CobolParserCore Parser(ITokenStream tokens, EditionInfo edition, CobolWordsMap words,
        IAntlrErrorListener<IToken>? errors = null)
    {
        var parser = new CobolParserCore(tokens) { Edition = edition, CobolWords = words };
        parser.RemoveErrorListeners();
        if (errors is not null) parser.AddErrorListener(errors);
        return parser;
    }

    /// <summary>The re-lex loop (kb/Work PB1669): a '(' after a reserved word opens a subscript only if the program declares the
    /// word, a finding of the PARSE, so <paramref name="attempt"/> lexes and parses with <paramref name="retypes"/>, and an
    /// attempt whose declarations prime the lexer differently (<see cref="TokenRetypes.LexesDifferentlyFrom"/>) is run again
    /// with them. Each attempt declares at least one more word of a finite set, so the loop ends; nearly every text is
    /// parsed once. Answers the last attempt's result.</summary>
    /// <param name="attempt">One lex and parse under the retypes it is given; answers its result and the retypes its parse
    /// found (<c>CompilationUnitContext.TokenRetypes</c>).</param>
    public static T UntilLexStable<T>(TokenRetypes retypes, EditionInfo edition, Func<TokenRetypes, (T Result, TokenRetypes Found)> attempt)
    {
        while (true)
        {
            var (result, found) = attempt(retypes);
            if (!found.LexesDifferentlyFrom(retypes, edition)) return result;
            retypes = found;
        }
    }
}
