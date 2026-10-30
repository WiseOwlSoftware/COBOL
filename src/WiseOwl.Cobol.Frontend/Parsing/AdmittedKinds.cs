// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Misc;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Frontend.Parsing;

/// <summary>
/// ⛔ THE KIND OF ELEMENT WRITTEN, AGAINST THE KINDS THE PARSE EXPECTS THERE (kb/Work PB3428, PB3861). A syntax error at
/// a token no general format admits there used to reach the user as the bare COBOL0001, or as one of two guesses that
/// named a kind in two shapes only ("a data-name is expected here, not a literal" where no literal was expected, and
/// its converse). The parse at the token knows what it expects: the tokens its configurations take, with their
/// predicates evaluated there, and the context words the refusing text predicates tested
/// (<c>CobolErrorStrategy.ResimulationWalk.AdmittedAt</c>). This names both sides in the standard's terms — keywords and
/// context words, the literal categories of §8.3.3.1 ("alphanumeric, boolean, national, or numeric", with the integer
/// literal of §8.3.3.3.2 among the numeric ones), a figurative constant — so every position of every general format is
/// named the same way, and a format added tomorrow is named the day it is added, with no table of statements or
/// clauses.
/// <para>⚠ WHAT THE PARSE EXPECTS, NOT WHAT THE STANDARD PRINTS. The grammar parses a superset of each general format
/// wherever a later phase names the departure better (INSPECT's tallying phrase takes FIRST and TRAILING for COBOL2269,
/// SET's integer-1 is any numeric literal for its bind check, a later edition's words are tokens at every edition), so
/// the expected elements can hold one the format does not print there. The sentence says "expected", the parser's own
/// word, and never "the general format admits only".</para>
/// <para>Three shapes, one rule (ISO §5.2.1: the elements of a general format are written in its sequence):</para>
/// <list type="bullet">
///   <item><b>A closed position</b>: everything it expects can be named — no user-defined word, no separator period
///         (the construct cannot end here), no statement (a position where a statement may begin expects every
///         statement, and the imperative-statement arms own it). The refusal names all of it: <c>MEMORY SIZE 12 FOO</c>
///         expects CHARACTERS, MODULES or WORDS; <c>SEGMENT-LIMIT IS .</c> an integer literal.</item>
///   <item><b>A literal at an open position that takes an operand</b> (a user-defined word or some literal): the
///         literals it expects are named, or that it expects none (<c>SET W-N TO "A"</c> expects a numeric literal,
///         never an alphanumeric one; <c>CALL "P" RETURNING 5</c> no literal).</item>
///   <item><b>A user-defined word at an open position that expects literals but no user-defined word</b>: both are
///         said.</item>
/// </list>
/// <para>An open position that takes the separator period is left to the missing-period reading wherever a period
/// would let the sentence or entry that starts at the token follow: <c>01 W-X PIC X</c> then <c>05 W-Y PIC X.</c> is a
/// forgotten period before a level-number, never an integer literal the data description entry does not take
/// (hint 14; the closed-format residue arm excludes the same tokens).</para>
/// </summary>
internal static class AdmittedKinds
{
    /// <summary>The refusal sentence for <paramref name="token"/>, written where the parse takes
    /// <paramref name="admitted"/> (predicates evaluated at the token) and the text predicates refusing it tested
    /// <paramref name="words"/>; null when the token is taken (the fault lies further on) or no shape applies.
    /// <paramref name="statementStart"/> is FIRST(<c>statementBlock</c>).
    /// <para>⛔ <c>ZERO_ARITH</c> IS NAMED ONLY WHERE A WRITTEN ZERO WOULD BECOME IT (kb/Work PB3861 review R1-1). It is a
    /// token no program writes: <c>ZeroTokenRewriter</c> makes a written ZERO that one by its neighbours, an arithmetic
    /// operator, the ref-mod colon or a paren pair around it alone. A parse that expects it expects a ZERO only in such
    /// a context, so it is named "the figurative constant ZERO" only when <paramref name="aZeroWrittenHereIsArithmetic"/>
    /// (the rewriter's own test, asked of the refused token's place): after <c>1 +</c> a ZERO written instead of
    /// <c>"A"</c> parses, while after <c>SET IX TO</c> it stays the figurative ZERO the position does not take, and naming
    /// it there sent the user to a rewrite the compiler refuses (and wrote "The word ZERO is not expected here: expected
    /// the figurative constant ZERO" for <c>SET IX TO ALL ZERO</c>).</para></summary>
    public static string? Refusal(IToken token, IntervalSet admitted, IReadOnlyCollection<string> words,
        IntervalSet statementStart, Func<bool> aPeriodLetsTheSentenceFollow, bool aZeroWrittenHereIsArithmetic)
    {
        if (!aZeroWrittenHereIsArithmetic && admitted.Contains(CobolParserCore.ZERO_ARITH))
        {
            var writable = new IntervalSet();
            writable.AddAll(admitted);
            writable.Remove(CobolParserCore.ZERO_ARITH);
            admitted = writable;
        }
        if (token.Type == TokenConstants.EOF || admitted.Contains(token.Type) || admitted.Count == 0 && words.Count == 0)
            return null;
        var named = Name(admitted, words, statementStart);
        string written = Written(token);
        if (!named.Open && named.Words.Count + named.Literals.Count + named.Symbols.Count == 0)
            return null;   // nothing the position expects can be named, so nothing is said
        if (!named.Open)
            return $"{written} is not expected here: expected {Join([.. named.Words, .. named.Literals, .. named.Symbols])} "
                   + "(ISO §5.2.1).";
        if (admitted.Contains(CobolLexer.DOT) && aPeriodLetsTheSentenceFollow())
            return null;
        // A literal's category is the fault only where the position takes an operand: a user-defined word or a literal of
        // another category. Where it takes neither (a level-number written before the FD that would own it) the token is
        // no operand at all, and saying "no literal" would call it one.
        if (IsLiteral(token.Type) && (admitted.Contains(CobolLexer.IDENTIFIER) || named.Literals.Count > 0))
            return named.Literals.Count == 0
                ? $"{written} is not expected here: no literal is expected at this position (ISO §5.2.1)."
                : $"{written} is not expected here: of the literals, only {Join(named.Literals)} is expected at this "
                  + "position (ISO §5.2.1).";
        if (token.Type == CobolLexer.IDENTIFIER && !admitted.Contains(CobolLexer.IDENTIFIER) && named.Literals.Count > 0)
            return $"{written} is not expected here: no user-defined word is expected at this position, and of the "
                   + $"literals only {Join(named.Literals)} (ISO §5.2.1).";
        return null;
    }

    private static bool IsLiteral(int type) => LiteralTokens.Whole.Contains(type);

    /// <summary>The literal families a position may admit WHOLE, named as one element, largest first. A position
    /// admits a family when it takes every token the family's rule opens with on a path no predicate guards; the
    /// guarded openings (the comma decimals <c>1,5</c> and <c>,5</c>, whose contiguity predicate the token written
    /// decides) are then the family's too, not separate elements.</summary>
    private static readonly (int Rule, string Name)[] Families =
    [
        (CobolParserCore.RULE_literal, "a literal"),
        (CobolParserCore.RULE_signedNumericLiteral, "a numeric literal"),
        (CobolParserCore.RULE_numericLiteralCore, "an unsigned numeric literal"),
        (CobolParserCore.RULE_figurativeConstant, "a figurative constant"),
    ];

    private static (IntervalSet Unguarded, IntervalSet All)[]? _familyFirsts;

    private static (IntervalSet Unguarded, IntervalSet All)[] FamilyFirsts()
        => _familyFirsts ??= [.. Families.Select(f => (UnguardedFirst(CobolParserCore._ATN, f.Rule),
               CobolParserCore._ATN.NextTokens(CobolParserCore._ATN.ruleToStartState[f.Rule])))];

    /// <summary>The figurative-constant words, each a literal (ISO §8.3.3.1: "A literal is defined by a reserved word
    /// that references a figurative constant or is a character-string …"): FIRST(<c>figurativeConstant</c>) but its ALL
    /// prefix, and ZERO_ARITH, the ZERO the token rewriter marks arithmetic (§8.8.1.1: an arithmetic expression may be
    /// "the figurative constant ZERO (ZEROS, ZEROES)"), which reaches here only where a ZERO written at the refused
    /// token would be that token (<see cref="Refusal"/>). A position that takes some of them, not the whole family,
    /// names each as a figurative constant among its literals.</summary>
    private static IntervalSet FigurativeWords => _figurativeWords ??= BuildFigurativeWords();

    private static IntervalSet? _figurativeWords;

    private static IntervalSet BuildFigurativeWords()
    {
        var atn = CobolParserCore._ATN;
        var set = new IntervalSet();
        set.AddAll(atn.NextTokens(atn.ruleToStartState[CobolParserCore.RULE_figurativeConstant]));
        set.Remove(CobolLexer.ALL);
        set.Add(CobolParserCore.ZERO_ARITH);
        set.SetReadonly(true);
        return set;
    }

    private sealed record Named(List<string> Words, List<string> Literals, List<string> Symbols, bool Open);

    private static Named Name(IntervalSet admitted, IReadOnlyCollection<string> words, IntervalSet statementStart)
    {
        bool open = admitted.Contains(CobolLexer.IDENTIFIER) || admitted.Contains(CobolLexer.DOT)
                    || admitted.Contains(TokenConstants.EOF) || admitted.And(statementStart).Count > 0;
        var wordNames = new SortedSet<string>(words, CobolNames.Comparer);
        var literals = new List<string>();
        var symbols = new List<string>();
        var rest = new IntervalSet();
        rest.AddAll(admitted);
        var firsts = FamilyFirsts();
        for (int f = 0; f < Families.Length; f++)
        {
            var (unguarded, all) = firsts[f];
            if (unguarded.Count == 0 || !unguarded.ToIntegerList().TrueForAll(rest.Contains)) continue;
            literals.Add(Families[f].Name);
            rest = rest.Subtract(all);
        }
        foreach (int type in rest.ToIntegerList())
        {
            if (type is CobolLexer.IDENTIFIER or CobolLexer.DOT or TokenConstants.EOF) continue;
            if (LiteralTokens.CategoryOf(type) is { } kind) Add(literals, Article(kind));
            else if (Spelling(type) is not { } spelling) open = true;   // a token with no name cannot be listed
            else if (FigurativeWords.Contains(type)) Add(literals, $"the figurative constant {spelling}");
            else if (char.IsAsciiLetter(spelling[0])) wordNames.Add(spelling);
            else Add(symbols, $"'{spelling}'");
        }
        return new([.. wordNames], literals, symbols, open);
    }

    private static void Add(List<string> list, string name)
    {
        if (!list.Contains(name)) list.Add(name);
    }

    private static string Article(string noun) => noun[0] is 'a' or 'e' or 'i' or 'o' or 'u' ? $"an {noun}" : $"a {noun}";

    /// <summary>How a token type is spelled in source: its literal name; for a paren twin (the FUNCTION-argument and
    /// reference parens the lexer retypes a '(' or ')' to, which carry no literal name) the paren; for a keyword several
    /// spellings or lexer modes define (PIC, IS), its symbolic name when the lexer makes that word this token; null for a
    /// token with none of these.</summary>
    private static string? Spelling(int type)
    {
        // Each side's three paren twins are the one character (ParenTokenTwinDriftTests: decided here, both twins).
        if (type is CobolLexer.LPAREN or CobolLexer.FNARG_LPAREN or CobolLexer.REF_LPAREN) return "(";
        if (type is CobolLexer.RPAREN or CobolLexer.FNARG_RPAREN or CobolLexer.REF_RPAREN) return ")";
        if (type == CobolParserCore.ZERO_ARITH) return "ZERO";   // the parser's virtual twin of the ZERO token
        var vocabulary = CobolParserCore.DefaultVocabulary;
        if (vocabulary.GetLiteralName(type) is ['\'', .. var inner, '\''] && inner.Length > 0) return new string(inner);
        return vocabulary.GetSymbolicName(type) is { Length: > 0 } symbolic && symbolic.Replace('_', '-') is var word
               && CobolKeywordTokens.TryTokenType(word, out int lexed) && lexed == type
            ? word
            : null;
    }

    /// <summary>The element written, as the refusal names it.</summary>
    private static string Written(IToken token)
    {
        string text = token.Text is { Length: > 40 } full ? full[..40] + "..." : token.Text ?? "";
        if (token.Type == CobolLexer.IDENTIFIER) return $"The word '{text}'";
        if (token.Type == CobolLexer.DOT) return "The separator period";
        if (LiteralTokens.CategoryOf(token.Type) is { } kind) return $"The {kind} {text}";
        return text is [var first, ..] && char.IsAsciiLetter(first) ? $"The word {text}" : $"'{text}'";
    }

    private static string Join(IReadOnlyList<string> names) => names.Count switch
    {
        0 => "",
        1 => names[0],
        _ => $"{string.Join(", ", names.Take(names.Count - 1))} or {names[^1]}",
    };

    /// <summary>FIRST(<paramref name="rule"/>) over the paths no semantic predicate guards: the tokens the rule opens
    /// with whatever the token written is.</summary>
    private static IntervalSet UnguardedFirst(ATN atn, int rule)
    {
        var set = new IntervalSet();
        var visited = new HashSet<int>();
        var work = new Stack<ATNState>();
        work.Push(atn.ruleToStartState[rule]);
        while (work.TryPop(out var state))
        {
            if (!visited.Add(state.stateNumber) || state is RuleStopState) continue;
            for (int i = 0; i < state.NumberOfTransitions; i++)
                switch (state.Transition(i))
                {
                    case PredicateTransition or PrecedencePredicateTransition or NotSetTransition:
                        break;
                    case RuleTransition call:
                        work.Push(call.target);
                        if (atn.NextTokens(call.target).Contains(TokenConstants.EPSILON)) work.Push(call.followState);
                        break;
                    case { IsEpsilon: true } epsilon:
                        work.Push(epsilon.target);
                        break;
                    case { Label: { } label }:
                        set.AddAll(label);
                        break;
                }
        }
        set.SetReadonly(true);
        return set;
    }
}
