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
/// <para>⛔ EXCEPT FOR THE WORDS THE TARGETED EDITION DOES NOT HAVE (kb/Work PB4174). The grammar is the superset of the
/// four editions, so its expected set holds the 2002 boolean operators at COBOL-85 and the 2023 shift operators below
/// 2023 — words ISO §8.9 has not reserved there, which §8.3.2.1 makes user-defined words. A keyword is named only where
/// <c>theEditionPrints</c> says a general format of the compile edition can print it
/// (<c>CobolParserCoreBase.PrintsKeyword</c>: not a word the §8.9 table or the §8.10 row's <c>Introduced</c> edition
/// proves a LATER edition introduced); the symbols and literal categories carry no edition and are named as the parse
/// takes them.</para>
/// <para>⛔ A NOISE WORD IS NOT AN ALTERNATIVE (kb/Work PB4225). ISO §8.3.2.4.3: "Within each format, uppercase words that
/// are not underlined are called optional words and may be specified at the user's option with no effect on the
/// semantics of the format." A word after which the parse admits exactly what it admitted without the word
/// (<c>aWordChangesNothing</c>: the IS of <c>USAGE [IS] usage</c>) is left out of the expected words, unless it is all
/// there is to name.</para>
/// <para>Five shapes, one rule (ISO §5.2.1: the elements of a general format are written in its sequence):</para>
/// <list type="bullet">
///   <item><b>A closed position</b>: everything it expects can be named — no user-defined word, no separator period
///         (the construct cannot end here), no statement (a position where a statement may begin expects EVERY
///         statement's opening word; one verb that another clause shares, DISPLAY among the USAGE keywords, makes no
///         statement position, kb/Work PB4201). The refusal names all of it:
///         <c>MEMORY SIZE 12 FOO</c> expects CHARACTERS, MODULES or WORDS; <c>SEGMENT-LIMIT IS .</c> an integer
///         literal; <c>USAGE 5</c> the usage keywords.</item>
///   <item><b>A literal or a user-defined word at a statement position</b> (kb/Work PB4224): the refusal says that a
///         statement may begin there and that the token cannot, and lists nothing else the position takes: a position
///         that follows an operand also takes what would lengthen it (<c>IF W-N = 1 5</c>: AND, OR, an operator), which
///         the parser has already left behind. A statement "begins with the first word of the statement name"
///         (§14.5.3.1), the names are Table 12's (§14.5.1), and neither a literal nor a user-defined word is one.</item>
///   <item><b>A literal at an open position that takes an operand</b> (a user-defined word or some literal): the
///         literals it expects are named, or that it expects none (<c>SET W-N TO "A"</c> expects a numeric literal,
///         never an alphanumeric one; <c>CALL "P" RETURNING 5</c> no literal).</item>
///   <item><b>A user-defined word at an open position that expects literals but no user-defined word</b>: both are
///         said.</item>
///   <item><b>A reserved word where an operand is due</b> (kb/Work PB4301): the position takes a user-defined word or a
///         literal and nothing that continues an operand (no punctuation or operator), so the element before the
///         token is incomplete (<c>OCCURS 0 TO TIMES</c>: the integer after TO is missing, <c>DISPLAY WHEN OTHER</c>: the
///         operand of the DISPLAY): the words and literals it takes are named, the user-defined words once.</item>
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
    /// <paramref name="statementStart"/> is FIRST(<c>statementBlock</c>); <paramref name="theEditionPrints"/> says
    /// whether a general format of the compile edition can print a keyword (kb/Work PB4174), and a keyword it refuses
    /// is not named. <paramref name="aUserWordIsPrinted"/> is false where every user-defined word the position takes stands
    /// where a format specifies a literal and the compile edition has no constant-name to write there (kb/Work PB4435):
    /// the word is then no alternative to name. <paramref name="aWordChangesNothing"/> says whether a word, written there, leaves the parse
    /// admitting exactly what it admitted without it (the optional word of §8.3.2.4.3, kb/Work PB4225), and such a word
    /// is no alternative to name. <paramref name="whatTheConstructBeforeCouldTake"/> is what the construct the parser
    /// completed just before the token could still take, or null where none was (the operand arm says an operand is due
    /// only where nothing of it is missing from <paramref name="admitted"/>, kb/Work PB4426). <paramref name="anOperandIsDue"/> is true when the sentence says that the element before
    /// the token is incomplete (a reserved word was written where an operand is due): the token is the next element's
    /// opening, which the structure around the fault resumes at, so the fault has no tail to discard.
    /// <para>⛔ <c>ZERO_ARITH</c> IS NAMED "the figurative constant ZERO" WHEREVER THE PARSE EXPECTS IT (kb/Work PB4120). It is
    /// a token no program writes, and a ZERO written where the parse expects it becomes it: <c>ZeroTokenRewriter</c>
    /// makes it so by the ZERO's neighbours, and where no neighbour shows the arithmetic expression the parse does
    /// (<c>ZeroSignReading</c>: a figurative ZERO the slot refuses is read as the expression's operand), so after
    /// <c>SET IX TO</c> a ZERO written instead of <c>"A"</c> parses exactly as after <c>1 +</c>. The narrowing that once
    /// stood here (kb/Work PB3861 review R1-1: named only where the rewriter's neighbour test held) answered a defect of
    /// the rewrite, not of the name, and went with it.</para></summary>
    public static string? Refusal(IToken token, IntervalSet admitted, IReadOnlyCollection<string> words,
        Func<int, bool> aWordChangesNothing, IntervalSet statementStart, Func<bool> aPeriodLetsTheSentenceFollow,
        Func<string, bool> theEditionPrints, bool aUserWordIsPrinted, Func<IntervalSet?> whatTheConstructBeforeCouldTake,
        out bool anOperandIsDue)
    {
        anOperandIsDue = false;
        if (token.Type == TokenConstants.EOF || admitted.Contains(token.Type) || admitted.Count == 0 && words.Count == 0)
            return null;
        var named = Name(admitted, words, aWordChangesNothing, statementStart, theEditionPrints);
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
        // A statement position takes neither a literal nor a user-defined word: a statement begins with the first word of
        // its statement name (§14.5.3.1; the names are Table 12's, §14.5.1), and that is a reserved word.
        // A figurative constant is a literal too (§8.3.3.1: "A literal is defined by a reserved word that references a
        // figurative constant or is a character-string"), the `ALL literal-1` format of §8.3.3.6.2 included.
        // ⛔ THE SENTENCE SAYS WHAT THE TOKEN IS NOT, NEVER WHAT THE POSITION TAKES (kb/Work PB4224, review R1-1). A statement
        // position that directly follows an operand (an IF condition, an EVALUATE selection object) also takes what would
        // lengthen it (AND, OR, an operator, THRU), which the parse has already left behind: the parser stands after the
        // completed condition and its own admitted set holds the statements and not the operator, so a list of "what is
        // expected" would be a false one (`IF W-N = 1 5 CONTINUE END-IF`). That a statement may begin here is true wherever
        // this arm fires, and so is that the token cannot begin one.
        bool literal = IsLiteral(token.Type) || FigurativeStarts.Contains(token.Type);
        if (named.Statement && (literal || token.Type == CobolLexer.IDENTIFIER))
            return $"{written} is not expected here: a statement may begin at this position, and a statement begins with the "
                   + $"first word of its statement name, which {(literal ? "a literal" : "a user-defined word")} is not "
                   + "(ISO §14.5.3.1; the names are Table 12's, §14.5.1).";
        // ⛔ A RESERVED WORD WHERE AN OPERAND IS DUE (kb/Work PB4301, PB4426): the position takes a user-defined word or a
        // literal, and a keyword of the next phrase (the TIMES of `OCCURS 0 TO TIMES`, the WHEN of `DISPLAY WHEN OTHER`)
        // was written instead: the element before it is incomplete, and what it needs is the position's own. Not at a
        // position where a statement may begin, which the arm above words, and said only when the position takes an
        // operand at all (a keyword where nothing but words are due is the phrase out of its sequence, not a missing one).
        // The user-defined word is ONE element: the context-sensitive words `cobolWord` also takes (§8.10: a user-defined
        // word at the position) are spellings of it, not alternatives to list. Where it can only be a constant-name and the
        // edition has none (COBOL-85, kb/Work PB4435), it is not named at all.
        // ⛔ SAID ONLY OF A POSITION THAT TAKES NOTHING BUT AN OPERAND (kb/Work PB4426). A punctuation mark or an operator in
        // the set ( ')' , '(' , '*' ) means the element before the token is COMPLETE and goes on with it
        // (`FUNCTION NUMVAL(A FROM 1)`: the argument is whole, the ')' or a qualifier OF may follow), so "an operand is
        // expected" would be false, and a list of the parser's set after a completed operand is not the set that may follow
        // it (the parser leaves its optional phrases early); such a position is left to the other readings.
        bool takesAWord = admitted.Contains(CobolLexer.IDENTIFIER);
        if (!named.Statement && CobolErrorStrategy.IsReservedWord(token) && (takesAWord || named.Literals.Count > 0))
        {
            var operand = takesAWord
                ? Name(admitted.Subtract(UserWordSpellings), [.. words.Where(w => !CobolKeywordTokens.TryTokenType(w, out int t)
                                                                                  || !UserWordSpellings.Contains(t))],
                    aWordChangesNothing, statementStart, theEditionPrints)
                : named;
            if (operand.Symbols.Count > 0) return null;
            // ⛔ NOT AFTER AN OPERAND THE PARSER HAS ALREADY LEFT (kb/Work PB4426). The parser exits an operand as soon as its
            // prediction allows, so after `SET ADDRESS OF X UP BY` its set holds the next receiver and TO and lacks what
            // would lengthen X (OF, IN, a subscript): "expected" would name an incomplete set. The construct completed just
            // before the token says what it could still take; a token it takes that the set lacks (the user-defined word's
            // own spellings are no difference) leaves the position unsaid.
            if (whatTheConstructBeforeCouldTake() is { } more && !more.Subtract(admitted).Subtract(UserWordSpellings).IsNil)
                return null;
            anOperandIsDue = true;
            return $"{written} is not expected here: expected "
                   + $"{Join([.. takesAWord && aUserWordIsPrinted ? (string[])["a user-defined word"] : [], .. operand.Words, .. operand.Literals])} "
                   + "(ISO §5.2.1).";
        }
        return null;
    }

    /// <summary>The context-sensitive words the <c>cobolWord</c> rule takes as user-defined words besides
    /// <c>IDENTIFIER</c> (ISO §8.10, §8.3.2.1): FIRST(<c>cobolWord</c>) without it. Where a position admits a user-defined
    /// word they are that element, never an alternative of their own.</summary>
    private static IntervalSet UserWordSpellings => _userWordSpellings ??= BuildUserWordSpellings();

    private static IntervalSet? _userWordSpellings;

    private static IntervalSet BuildUserWordSpellings()
    {
        var atn = CobolParserCore._ATN;
        var set = new IntervalSet();
        set.AddAll(atn.NextTokens(atn.ruleToStartState[CobolParserCore.RULE_cobolWord]));
        set.Remove(CobolLexer.IDENTIFIER);
        set.SetReadonly(true);
        return set;
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
    /// "the figurative constant ZERO (ZEROS, ZEROES)"), which a ZERO written at the refused token becomes
    /// (<see cref="Refusal"/>). A position that takes some of them, not the whole family,
    /// names each as a figurative constant among its literals.</summary>
    private static IntervalSet FigurativeWords => _figurativeWords ??= BuildFigurativeWords();

    private static IntervalSet? _figurativeWords;

    /// <summary>Every token a figurative constant opens with: <see cref="FigurativeWords"/> and ALL, the required word of
    /// Format 6, <c>ALL literal-1</c> (ISO §8.3.3.6.2). ALL is no figurative word to NAME (a position that takes it names
    /// the literal), but a figurative constant to RECOGNISE in the token written.</summary>
    private static IntervalSet FigurativeStarts => _figurativeStarts ??= BuildFigurativeStarts();

    private static IntervalSet? _figurativeStarts;

    private static IntervalSet BuildFigurativeStarts()
    {
        var set = new IntervalSet();
        set.AddAll(FigurativeWords);
        set.Add(CobolLexer.ALL);
        set.SetReadonly(true);
        return set;
    }

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

    /// <summary>What a position expects, named. <paramref name="Statement"/>: a statement may begin here
    /// (<see cref="BeginsAStatement"/>), so the statement names are left out of <paramref name="Words"/> (kb/Work
    /// PB4224).</summary>
    private sealed record Named(List<string> Words, List<string> Literals, List<string> Symbols, bool Open, bool Statement);

    private static Named Name(IntervalSet admitted, IReadOnlyCollection<string> words, Func<int, bool> aWordChangesNothing,
        IntervalSet statementStart, Func<string, bool> theEditionPrints)
    {
        bool statement = BeginsAStatement(admitted, words, statementStart);
        bool open = admitted.Contains(CobolLexer.IDENTIFIER) || admitted.Contains(CobolLexer.DOT)
                    || admitted.Contains(TokenConstants.EOF) || statement;
        // At a statement position the statement names are not listed: no arm prints Words at a statement position (a
        // position that begins a statement is Open, so the closed-list arm never fires there), and the sentence of the
        // statement arm says "a statement" and nothing of what else the position takes (kb/Work PB4224, PB4426).
        bool ListedByName(int type) => !(statement && statementStart.Contains(type));
        var wordNames = new SortedSet<string>(
            words.Where(w => theEditionPrints(w) && (!CobolKeywordTokens.TryTokenType(w, out int t) || ListedByName(t))),
            CobolNames.Comparer);
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
            if (type is CobolLexer.IDENTIFIER or CobolLexer.DOT or TokenConstants.EOF || !ListedByName(type)) continue;
            if (LiteralTokens.CategoryOf(type) is { } kind) Add(literals, Article(kind));
            // The one character-string that is neither a word nor a literal (ISO §8.3.1: "A character-string is a
            // character or a sequence of contiguous characters that forms a COBOL word, a literal, or a picture
            // character-string"): PICMODE's token, which a separator period after PIC leaves unwritten (kb/Work PB4233).
            else if (type == CobolLexer.PIC_STRING) Add(symbols, "a picture character-string");
            else if (Spelling(type) is not { } spelling) open = true;   // a token with no name cannot be listed
            else if (char.IsAsciiLetter(spelling[0]) && !theEditionPrints(spelling)) continue;   // a word the edition lacks
            else if (FigurativeWords.Contains(type)) Add(literals, $"the figurative constant {spelling}");
            else if (char.IsAsciiLetter(spelling[0])) wordNames.Add(spelling);
            else Add(symbols, $"'{spelling}'");
        }
        // ⛔ A NOISE WORD IS NOT AN ALTERNATIVE (kb/Work PB4225). The optional word that may stand before what the position
        // expects (the IS of `USAGE [IS] usage`) is left out of the list, unless it is all there is to name.
        var noise = wordNames.Where(w => CobolKeywordTokens.TryTokenType(w, out int t) && aWordChangesNothing(t)).ToList();
        if (noise.Count < wordNames.Count + literals.Count + symbols.Count) wordNames.ExceptWith(noise);
        return new([.. wordNames], literals, symbols, open, statement);
    }

    /// <summary>⛔ A POSITION WHERE A STATEMENT MAY BEGIN EXPECTS EVERY STATEMENT'S OPENING WORD, not one of them (kb/Work
    /// PB4201). A verb that is also a word of another clause (DISPLAY, the USAGE keyword; INDEX, SET's and SEARCH's)
    /// puts one token in both sets, and that overlap said nothing about the position: <c>USAGE 5</c> expects the usage
    /// keywords, DISPLAY among them, so it counted as a statement position, took the open path and drew the bare
    /// COBOL0001. A block begun at the token expects all of <paramref name="statementStart"/>
    /// (<c>OffendingPosition.EmptyBlockOwner</c> tests the same containment); a start the position does not ADMIT but
    /// whose word a text predicate tested (<paramref name="words"/>: a verb of a declined or gated facility, COMMIT,
    /// SEND) is still one it expects.</summary>
    private static bool BeginsAStatement(IntervalSet admitted, IReadOnlyCollection<string> words, IntervalSet statementStart)
    {
        if (statementStart.Count == 0) return false;
        var tested = new HashSet<int>();
        foreach (string word in words)
            if (CobolKeywordTokens.TryTokenType(word, out int type)) tested.Add(type);
        return statementStart.ToIntegerList().TrueForAll(type => admitted.Contains(type) || tested.Contains(type));
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
