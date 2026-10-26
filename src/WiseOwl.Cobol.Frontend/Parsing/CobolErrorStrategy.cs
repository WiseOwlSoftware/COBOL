// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Concurrent;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Cst;          // ClosedFormats — the ONE table of closed general formats (0g)
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Frontend.Parsing;

/// <summary>
/// COBOL-aware error strategy that replaces ANTLR's generic messages with
/// actionable, human-readable diagnostics. Pattern-matches common COBOL
/// mistakes and suggests fixes.
/// </summary>
public sealed class CobolErrorStrategy : DefaultErrorStrategy
{
    /// <summary>
    /// Structured diagnostic hint with a COBOL-level code and priority.
    /// Lower priority values sort first (higher importance).
    /// </summary>
    private readonly record struct DiagnosticHint(Diagnostics.DiagnosticDescriptor Descriptor, string Message, int Priority)
    {
        public string Code => Descriptor.Code;
    }

    protected override void ReportNoViableAlternative(Parser recognizer, NoViableAltException e)
    {
        var token = e.OffendingToken ?? recognizer.CurrentToken;
        var message = BuildMessage(recognizer, token, e, ErrorKind.NoViableAlternative);
        recognizer.NotifyErrorListeners(token, message, e);
    }

    /// <summary>⛔ A RESIDUE REFUSED WHERE ITS PREDICTION FAILED IS CONSUMED AS ONE RUN (kb/Work PB3391). When the 0g arm
    /// names the decision's first token as a closed format's residue (<see cref="ClosedFormatResidue"/> with
    /// <c>predictionFailedAt</c>: a word a text-predicated clause refused, or a reserved word such as the CHARACTER of
    /// <c>OBJECT-COMPUTER. XYZ CHARACTER FOO IS X.</c>), the base recovery resynchronizes on the first token of the
    /// recovery set, and the run's own tokens are in it: an IDENTIFIER is in nearly every recovery set, because the
    /// error production of an enclosing list opens with one. The rule that caught the error then returned, an
    /// enclosing list took the rest of the run as ITS residue, and the next token was refused a second time, often
    /// against the wrong format (<c>OBJECT-COMPUTER. XYZ FOO IS 12.</c> also drew "'IS' is not a paragraph of the
    /// CONFIGURATION SECTION"). So the refused token is consumed, and the rest up to a token the context may resume
    /// with that NO run of the production's form there holds (<see cref="ResidueRunTokens"/>: a period, a clause
    /// keyword, a header word, and in the data division's form the unsigned integer that may be the next entry's
    /// level-number, kb/Work PB3390), as the production itself would have consumed the run and the 0g Sync the
    /// reserved word after it; the base recovery then finds itself at that token. One refusal per residue, as
    /// <see cref="Sync"/> gives one.</summary>
    public override void Recover(Parser recognizer, RecognitionException e)
    {
        if (e is NoViableAltException { OffendingToken: { } word } && IsPredictionFailedAt(e, word)
            && ((ITokenStream)recognizer.InputStream).LT(1).TokenIndex == word.TokenIndex
            && ClosedFormatResidue(recognizer, word, predictionFailedAt: true) is { } residue)
        {
            recognizer.Consume();
            ConsumeUntil(recognizer, GetErrorRecoverySet(recognizer).Subtract(ResidueRunTokens(residue.Production)));
        }
        base.Recover(recognizer, e);
    }

    /// <summary>Every token a run of the error production's form <paramref name="production"/> may hold: the union of
    /// the FIRST sets of the run pieces the form calls (<c>residueOpening</c>, <c>residueItem</c>, and in
    /// <c>unrecognizedClause</c> <c>residueInteger</c>: words, every literal token but the reserved ones, a sign, the
    /// concatenation's <c>&amp;</c>, the unsigned integer where the form holds it), read from the ATN once per form, so
    /// a piece or token either form admits tomorrow is held here too.</summary>
    private static IntervalSet ResidueRunTokens(int production)
        => ResidueRunTokenSets.GetOrAdd(production, static rule => ResidueRunTokenSet(CobolParserCore._ATN, rule));

    private static readonly ConcurrentDictionary<int, IntervalSet> ResidueRunTokenSets = new();

    private static IntervalSet ResidueRunTokenSet(ATN atn, int production)
    {
        var set = new IntervalSet();
        foreach (var state in atn.states.Where(s => s is not null && s.ruleIndex == production))
            for (int i = 0; i < state.NumberOfTransitions; i++)
                if (state.Transition(i) is RuleTransition call)
                    set.AddAll(atn.NextTokens(call.target));
        set.SetReadonly(true);
        return set;
    }

    protected override void ReportInputMismatch(Parser recognizer, InputMismatchException e)
    {
        var token = e.OffendingToken ?? recognizer.CurrentToken;
        string expected = e.GetExpectedTokens().ToString(recognizer.Vocabulary);
        var message = BuildMessage(recognizer, token, e, ErrorKind.InputMismatch, expected);
        recognizer.NotifyErrorListeners(token, message, e);
    }

    protected override void ReportMissingToken(Parser recognizer)
    {
        if (errorRecoveryMode) return;
        BeginErrorCondition(recognizer);

        var token = recognizer.CurrentToken;
        string expected = GetExpectedTokens(recognizer).ToString(recognizer.Vocabulary);
        var message = BuildMessage(recognizer, token, null, ErrorKind.MissingToken, expected);
        recognizer.NotifyErrorListeners(token, message, null);
    }

    protected override void ReportUnwantedToken(Parser recognizer)
    {
        if (errorRecoveryMode) return;
        BeginErrorCondition(recognizer);

        var token = recognizer.CurrentToken;
        string expected = GetExpectedTokens(recognizer).ToString(recognizer.Vocabulary);
        var message = BuildMessage(recognizer, token, null, ErrorKind.UnwantedToken, expected);
        recognizer.NotifyErrorListeners(token, message, null);
    }

    /// <summary>⛔ A PARENTHESIZED GROUP IS NEVER A RESYNCHRONIZATION POINT (kb/Work PB2152). ISO §8.3.5 4): "Except in
    /// pseudo-text, parentheses may appear only in balanced pairs of left and right parentheses" — so a '(' the parser
    /// skips while recovering opens a group whose content belongs to the construct that failed, never to the next one.
    /// ANTLR's resync consumes token by token until one is in the recovery set, and a token INSIDE the group can be in
    /// it: since the reference paren became its own token (kb/Work PB2113) the integer in <c>05 A(1) PIC X(3).</c> or
    /// <c>01 X PICTURE X(3).</c> resyncs as a level-number, and its ')' then reports a second, spurious
    /// "unexpected ')'" for the one error the source holds. So a group met while consuming is consumed WHOLE, through
    /// its balancing ')' of any flavour (the three paren twins), and stops early at the separator period or end of
    /// file, so an unbalanced '(' cannot swallow the rest of the program. A paren that IS in the recovery set is still
    /// the resync point: the set is asked first.</summary>
    protected override void ConsumeUntil(Parser recognizer, IntervalSet set)
    {
        var input = (ITokenStream)recognizer.InputStream;
        for (int ttype = input.LA(1); ttype != TokenConstants.EOF && !set.Contains(ttype); ttype = input.LA(1))
        {
            if (!IsOpenParen(ttype))
            {
                recognizer.Consume();
                continue;
            }
            int depth = 0;
            do
            {
                if (IsOpenParen(ttype)) depth++;
                else if (IsCloseParen(ttype)) depth--;
                recognizer.Consume();
                ttype = input.LA(1);
            }
            while (depth > 0 && ttype != TokenConstants.EOF && ttype != CobolLexer.DOT);
        }
    }

    /// <summary>The other half of the same rule: single-token deletion never deletes a '(' (kb/Work PB2152). Deleting
    /// one paren of a balanced pair (§8.3.5 4) orphans the other, so <c>DISPLAY (W-S).</c> recovered by dropping the
    /// '(' and then reported its ')' as a second error. Declining here sends the error to <see cref="ConsumeUntil"/>,
    /// which consumes the group whole.</summary>
    protected override IToken? SingleTokenDeletion(Parser recognizer) =>
        IsOpenParen(((ITokenStream)recognizer.InputStream).LA(1)) ? null : base.SingleTokenDeletion(recognizer);

    /// <summary>⛔ A RESERVED WORD IS REFUSED WHERE THE CLOSED FORMAT'S LIST STANDS, NOT WHERE THE PARSE GIVES UP
    /// (kb/Work PB2533). ANTLR synchronizes at every subrule and loop iteration, and at a clause list that may END
    /// (<c>dataDescriptionClause*</c>, <c>sortMergeDescriptionClause+</c>, a paragraph's entry loop) the base
    /// <see cref="DefaultErrorStrategy.Sync"/> lets a token it cannot place pass, because the rule's follow might take
    /// it: the list is exited, and the error surfaces later at the entry's period (<c>SD SF RECORD 80 EXTERNAL.</c>) or
    /// several rules up (<c>SPECIAL-NAMES. GLOBAL.</c> failed in <c>compilationUnit</c>), where nothing remembers that
    /// the list was the place. So at a state whose closure reaches the error production, a reserved word (or a literal
    /// the production does not open with: in the data division's form, one that begins with an unsigned integer,
    /// kb/Work PB3373, PB3390) that NO path can take here — it is
    /// outside the full-context LL(1) set, so the parse fails at this token whatever happens next, and no legal
    /// program is touched — is reported at once (the 0g hint names the format) and the
    /// residue consumed up to what the list or its follow can take, as the base loop-back recovery does.
    /// <para>Cost: a per-state flag first (<see cref="ReachesErrorProduction"/>, computed once per ATN state), then the
    /// rule-local next set the ATN caches; the full-context set and the walk run only for a keyword the list's own
    /// rule cannot continue with — once per clause list's end in a legal program.</para></summary>
    public override void Sync(Parser recognizer)
    {
        if (!InErrorRecoveryMode(recognizer) && IsClosedFormatResidueAhead(recognizer))
        {
            ReportUnwantedToken(recognizer);
            ConsumeUntil(recognizer, GetExpectedTokens(recognizer).Or(GetErrorRecoverySet(recognizer)));
            return;
        }
        if (!InErrorRecoveryMode(recognizer) && IsStrayTokenBeforeOptionalPeriod(recognizer)
            && SingleTokenDeletion(recognizer) is not null)
            return;
        base.Sync(recognizer);
    }

    /// <summary>⛔ A SUPERSET-PARSED SEPARATOR PERIOD RECOVERS LIKE A REQUIRED ONE (kb/Work PB3370). The paragraphs
    /// <see cref="ParagraphPeriods"/> lists write their period as <c>DOT?</c> so the period can be refused BY NAME
    /// (and, in SOURCE-COMPUTER and OBJECT-COMPUTER, so the computer-name's prediction commits). A required period gets
    /// ANTLR's single-token deletion at its match — <c>OBJECT-COMPUTER. XYZ ) .</c> drops the ')' and goes on — but an
    /// optional one is only a decision whose rule may END, where the base <see cref="DefaultErrorStrategy.Sync"/> lets
    /// the stray token pass: the paragraph ends, the error surfaces divisions later, and its recovery swallows the next
    /// paragraphs' own diagnostics. So where the rule could take a period next, a token that NO path can take here (it
    /// is outside the full-context LL(1) set, so no legal program is touched) directly followed by a period is deleted
    /// as it would be before a required one. A paragraph, section or division header word (<see cref="HeaderWords"/>)
    /// is never stray: followed by its own period it OPENS the next paragraph, written out of order or after this
    /// paragraph's missing period (<c>SPECIAL-NAMES. DECIMAL-POINT IS COMMA</c> then <c>FILE-CONTROL.</c>), and deleting
    /// it would pour that paragraph's entries into this closed format as false "not a clause" refusals.
    /// <para>Cost: the rule-local next set the ATN caches, then two token types; the full-context set only for a stray
    /// token right before a period.</para></summary>
    private static bool IsStrayTokenBeforeOptionalPeriod(Parser recognizer)
    {
        var atn = recognizer.Atn;
        var state = atn.states[recognizer.State];
        var local = atn.NextTokens(state);
        if (!local.Contains(CobolLexer.DOT) || !local.Contains(TokenConstants.EPSILON)) return false;
        var stream = (ITokenStream)recognizer.InputStream;
        int la1 = stream.LA(1);
        return la1 != TokenConstants.EOF && !local.Contains(la1) && stream.LA(2) == CobolLexer.DOT
               && !HeaderWords(atn).Contains(la1) && !atn.NextTokens(state, recognizer.Context).Contains(la1);
    }

    private static bool IsClosedFormatResidueAhead(Parser recognizer)
    {
        var atn = recognizer.Atn;
        var state = atn.states[recognizer.State];
        // Inside the production itself (its word run, in either form) the residue CONTINUES:
        // `SOURCE-COMPUTER. XYZ WIBBLE GLOBAL.` stands in the paragraph's single optional residue, and no loop of the
        // list's own would Sync once the run ended (kb/Work PB3370).
        if (!ClosedFormats.IsErrorProduction(state.ruleIndex) && !ReachesErrorProduction(atn, state)) return false;
        var token = recognizer.CurrentToken;
        if (token.Type is CobolLexer.IDENTIFIER or TokenConstants.EOF || atn.NextTokens(state).Contains(token.Type))
            return false;   // the production takes an IDENTIFIER itself; a token the rule continues with is legal
        // A word (a reserved word is the candidate) or a literal the production does not open with — in the data
        // division's form, one that begins with an unsigned integer (kb/Work PB3373, PB3390); ClosedFormatResidue
        // decides.
        return (IsLiteral(token) || token.Text is [var first, ..] && char.IsAsciiLetter(first))
               && !atn.NextTokens(state, recognizer.Context).Contains(token.Type)
               && ClosedFormatResidue(recognizer, token, predictionFailedAt: false) is not null;
    }

    /// <summary>Per ATN state: does its closure — epsilon edges, every rule call entered, and a call to a rule that can
    /// match empty stepped over — reach a call of the error production (either form) without leaving the state's rule?
    /// It does not pop out of the rule, so it is a pre-filter, not the answer: <see cref="ClosedFormatResidue"/> decides with
    /// the real context. A list that may end is still caught, at the Sync of the loop that holds it. Cached in a
    /// static because the ATN is a static of the generated parser; a benign race recomputes the same answer.</summary>
    private static bool ReachesErrorProduction(ATN atn, ATNState start)
    {
        var cache = _reachesErrorProduction ??= new sbyte[atn.states.Count];
        if (cache[start.stateNumber] != 0) return cache[start.stateNumber] > 0;
        bool reaches = false;
        var visited = new HashSet<int>();
        var work = new Stack<ATNState>();
        work.Push(start);
        while (!reaches && work.TryPop(out var state))
        {
            if (!visited.Add(state.stateNumber) || state is RuleStopState) continue;
            for (int i = 0; i < state.NumberOfTransitions && !reaches; i++)
            {
                var t = state.Transition(i);
                if (t is RuleTransition call)
                {
                    reaches = ClosedFormats.IsErrorProduction(call.target.ruleIndex);
                    work.Push(call.target);
                    // Step over the call only when the called rule can match empty (its rule-local next set, which
                    // the ATN caches, holds EPSILON): a call that must consume a token ends the closure here.
                    if (atn.NextTokens(call.target).Contains(TokenConstants.EPSILON)) work.Push(call.followState);
                }
                else if (t.IsEpsilon) work.Push(t.target);
            }
        }
        cache[start.stateNumber] = reaches ? (sbyte)1 : (sbyte)-1;
        return reaches;
    }

    private static sbyte[]? _reachesErrorProduction;

    private static bool IsOpenParen(int ttype) =>
        ttype is CobolLexer.LPAREN or CobolLexer.FNARG_LPAREN or CobolLexer.REF_LPAREN;

    private static bool IsCloseParen(int ttype) =>
        ttype is CobolLexer.RPAREN or CobolLexer.FNARG_RPAREN or CobolLexer.REF_RPAREN;

    // ── Message construction ──

    private enum ErrorKind { NoViableAlternative, InputMismatch, MissingToken, UnwantedToken }

    private static string BuildMessage(
        Parser recognizer, IToken token, RecognitionException? e,
        ErrorKind kind, string? expectedTokens = null)
    {
        string baseMsg = kind switch
        {
            ErrorKind.NoViableAlternative => $"cannot parse construct near '{Truncate(token.Text, 40)}'",
            ErrorKind.InputMismatch => $"unexpected '{Truncate(token.Text, 40)}'",
            ErrorKind.MissingToken => $"missing token before '{Truncate(token.Text, 40)}'",
            ErrorKind.UnwantedToken => $"unexpected '{Truncate(token.Text, 40)}'",
            _ => "syntax error"
        };

        var hints = GuessCobolIntent(recognizer, token, expectedTokens, IsPredictionFailedAt(e, token));

        if (hints.Count == 0)
            return baseMsg;

        // Deduplicate by code prefix (first 8 chars), sort by priority, cap at 2
        var seen = new HashSet<string>();
        var filtered = new List<DiagnosticHint>();
        foreach (var h in hints.OrderBy(h => h.Priority))
        {
            string prefix = h.Code.Length >= 8 ? h.Code[..8] : h.Code;
            if (seen.Add(prefix) && filtered.Count < 2)
                filtered.Add(h);
        }

        string code = filtered[0].Code;
        string hintText = string.Join(" ", filtered.Select(h => h.Message));
        return $"[{code}] {baseMsg}. {hintText}";
    }

    // ── COBOL-specific heuristics ──

    private static List<DiagnosticHint> GuessCobolIntent(
        Parser recognizer, IToken token, string? expectedTokens, bool predictionFailedAt)
    {
        var hints = new List<DiagnosticHint>();
        var stream = (ITokenStream)recognizer.InputStream;
        var prev = GetToken(stream, token.TokenIndex - 1);
        var ruleStack = recognizer.GetRuleInvocationStack().ToArray();

        // 0. Vendor JSON/XML statements are NOT ISO/IEC 1989 constructs (0 spec occurrences; owner decision 2,
        // DEVLOG 581): the hard-reserved JSON/XML lexer tokens can only surface here as a misplaced vendor statement
        // behind a generic parse error → the vendor-extension hint (COBOL0313, priority 0 so its code wins the prefix),
        // never an edition gate. Every ISO reservation-word introduction gate now fires at BIND (superset parse +
        // bind-time ConstructRegistry.Check → the VersionConformancePass), so the former ReservedWordEditionHints
        // reverse-signature recogniser is gone (residue migration complete, DESIGN-version-conformance-pipeline.md).
        if (token.Type is CobolLexer.JSON or CobolLexer.XML)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0313,
                $"{(token.Type == CobolLexer.JSON ? "JSON" : "XML")} GENERATE/PARSE is not an ISO/IEC 1989 construct — "
                + "vendor-dialect extension, deferred (owner decision 2, DEVLOG 581).", 0));

        // ⛔ 0a. THE REQUIRED IMPERATIVE-STATEMENT ARM — ONE ARM FOR EVERY GENERAL FORMAT (kb/Work PB396).
        // Nine phrase rules carry an imperative-statement operand that the printed general format leaves
        // UNBRACKETED or stacks inside BRACES (IF's THEN and ELSE arms, EVALUATE's WHEN clause and its WHEN
        // OTHER tail, PERFORM's inline body and its WHEN / WHEN OTHER / WHEN COMMON / FINALLY bodies, SEARCH's
        // and SEARCH ALL's WHEN bodies), and every one of them is now spelled `statementBlock` — a rule that
        // cannot match empty. So the violation always arrives in the SAME shape: a parse position whose
        // expected set admits everything that can START a statement block, reached with a token that can start
        // none of them. That test is computed FROM THE ATN, not from a table of verbs, so a general format
        // added tomorrow inherits the diagnostic without editing this method — which is the whole reason the
        // cardinality lives in the grammar rather than in nine bind-time checks.
        if (IsEmptyRequiredStatementBlock(recognizer, token)
            && EnclosingStatementName(ruleStack) is { } stmtName)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL2072,
                $"The {stmtName} statement's general format shows an imperative-statement here that is not "
                + "bracketed, so at least one statement shall be specified: brackets are what license an "
                + "omission (ISO §5.2.6.2), and one alternative of a brace group shall be explicitly specified "
                + "(ISO §5.2.6.3).", 0));

        // ⛔ 0b. THE WHEN OTHER PHRASE — BOTH OF ITS RULES, SEPARATED BY LOOKAHEAD (kb/Work PB396).
        // `[ WHEN OTHER imperative-statement-2 ]` follows the `{ … } …` repetition in EVALUATE's format, and
        // `[ WHEN OTHER EXCEPTION imperative-statement-3 ]` follows the WHEN group in PERFORM Format 3: ONE
        // bracketed phrase, LAST, whose imperative-statement is inside the bracket and therefore required once
        // the phrase is written. A syntax error at the phrase can only be one of those two rules, and which one
        // is decided by what follows OTHER — a token that can START a statement means the body is there and the
        // PHRASE is misplaced or repeated; anything else means the phrase is where it belongs and its
        // imperative-statement is missing. The parser reports at WHEN (the clause-repetition decision) or at
        // OTHER (a WHEN group cannot spell it), so both spellings are recognised here.
        int otherIndex = token.Type == CobolLexer.OTHER ? token.TokenIndex
            : token.Type == CobolLexer.WHEN && GetToken(stream, token.TokenIndex + 1)?.Type == CobolLexer.OTHER
                ? token.TokenIndex + 1
                : -1;
        if (otherIndex >= 0)
        {
            // PERFORM Format 3 prints `WHEN OTHER EXCEPTION imperative-statement-3` with EXCEPTION NOT
            // underlined — an optional word (§8.3.2.4.3), so it is never the body.
            var afterOther = GetToken(stream, otherIndex + 1);
            if (afterOther?.Type == CobolLexer.EXCEPTION) afterOther = GetToken(stream, otherIndex + 2);
            bool bodyPresent = afterOther is not null
                && StatementBlockStart(recognizer).Contains(afterOther.Type);
            hints.Add(bodyPresent
                ? new(Diagnostics.DiagnosticDescriptors.COBOL2073,
                    "A WHEN OTHER phrase may be specified at most once, and only after every WHEN phrase: the "
                    + "general format brackets it as a single phrase FOLLOWING the repeated WHEN group, and an "
                    + "ellipsis applies only to the portion between the delimiters immediately to its left "
                    + "(ISO §14.9.13.2 / §14.9.28.2 Format 3, with ISO §5.2.7).", 0)
                : new(Diagnostics.DiagnosticDescriptors.COBOL2072,
                    "The WHEN OTHER phrase carries an imperative-statement INSIDE its bracket, so writing the "
                    + "phrase obliges the statement: brackets license omitting the whole phrase, never emptying "
                    + "it (ISO §5.2.6.2, with §14.9.13.2 / §14.9.28.2 Format 3).", 0));
        }

        // ⛔ 0c. A WRITTEN SHAPE NO GENERAL FORMAT OF ITS CLAUSE PRINTS (kb/Work PB412, PB421). The grammar
        // rules for GO TO and MOVE each now carry ONE ALTERNATIVE PER PRINTED FORMAT, so their complement — the
        // shapes the old optional-everything union rules admitted — arrives here as a syntax error instead of
        // reaching a bind arm that discarded operands or emitted a loud stage. Both hints are read from the
        // PARSER STATE, not from a catalogue of bad source: the GO TO one asks the ONE format classifier what
        // the partially-built statement context actually holds, and the MOVE one is the position of a
        // statement-selecting keyword inside the rule that cannot spell it.
        if (GoToComplementMessage(recognizer, token, stream) is { } goToMsg)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL2172, goToMsg, 0));

        // ⛔ 0d. A FIGURATIVE SPELLING WHERE A GENERAL FORMAT NAMES ONE KEYWORD (kb/Work PB510). ZERO / ZEROS /
        // ZEROES (and each singular/plural figurative pair) are distinct §8.9 reserved words, one lexer token
        // each, interchangeable only as the §8.3.3.6 figurative constant. A format that prints ONE of them as a
        // keyword (§5.2.2) — BLANK WHEN ZERO, the sign condition's ZERO, OPTIONS INITIALIZE's BINARY ZEROES /
        // HIGH-VALUES / LOW-VALUES / SPACES — therefore fails the parse on a sibling spelling, and it arrives here
        // in ONE shape: the expected set holds a sibling of the offending token's family but not the token. The
        // families are read FROM THE ATN (the FIRST sets of the figurative-word rules), so a format added tomorrow
        // that names one spelling inherits this diagnostic without editing this method.
        if (FigurativeSpellingMessage(recognizer, token) is { } spellingMsg)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL2418, spellingMsg, -1));   // outranks 0a: the sign-
            // condition shape fails at an IS / NOT the enclosing statement cannot continue with, which 0a reads as an
            // empty imperative-statement — the misspelled keyword is the cause, the empty block its symptom.
        // ⛔ 0e. THE SEARCH PHRASES NO SEARCH FORMAT PRINTS (kb/Work PB446). The two SEARCH rules now spell the
        // two printed formats exactly, so their complement arrives here; each shape is read from the PARSER
        // STATE (the half-built or just-completed SEARCH context), never from a scan for a keyword.
        if (SearchFormatShapeMessage(recognizer, token, stream) is { } searchMsg)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL2269, searchMsg, 0));
        // ⛔ 0e'. A SUBSCRIPT, REFERENCE MODIFICATION OR QUALIFIER ON A MNEMONIC-NAME (kb/Work PB2499). ACCEPT's
        // FROM, DISPLAY's UPON and SET Format 3's receivers are spelled `cobolWord` because a mnemonic-name is a word
        // (§8.3.2.2.16), so the suffix arrives here — read from the PARSER STATE, worded by MnemonicNameSlot.
        if (MnemonicSuffixMessage(recognizer, token, stream) is { } mnemonicMsg)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL2269, mnemonicMsg, 0));
        // ⛔ 0e''. A TABLE 12 [NOT] PHRASE THAT NO OPEN STATEMENT MAY TAKE (kb/Work PB3830). The one conditionalPhrases
        // rule associates a phrase by §14.5.3.3, so a phrase no open statement's general format admits — a second ON
        // SIZE ERROR, a CALL's NOT ON OVERFLOW, a READ's INVALID KEY beside its AT END — ends every statement and
        // arrives here; ConditionalPhraseAssociation reads the parser state and names the statement and the reason.
        // Added after 0e, so SEARCH's own NOT AT END wording (its WHEN-branch advice) wins the shared code.
        if (ConditionalPhraseAssociation.UnownedPhraseMessage(recognizer.Context, stream, token) is { } phraseMsg)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL2269, phraseMsg, 0));
        // ⛔ 0f. AN INLINE METHOD INVOCATION'S EMPTY PARENTHESIS PAIR (kb/Work PB1430). `inlineInvocationSegment`
        // requires its argumentList inside the optional parentheses, so `O :: "M" ( )` arrives here, at the '(' or
        // the ')'. Outranks 0a for the same reason 0d does: the empty pair is the cause wherever the parse gives up.
        if (IsEmptyInlineArgumentList(stream, token))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL2994,
                "An inline method invocation's parenthesis pair holds at least one argument: ISO §8.4.3.4.2 brackets "
                + "the whole parenthesised group but encloses the argument forms in BRACES repeated by the ellipsis "
                + "inside the one pair, and one alternative of a brace group shall be explicitly specified "
                + "(ISO §5.2.6.3). Omit the parentheses when the method is invoked without arguments.", -1));
        // ⛔ 0g. A RESERVED WORD WHERE A CLOSED GENERAL FORMAT'S ERROR PRODUCTION STANDS (kb/Work PB2533). Ten closed
        // formats end their alternative list in the error production (`unrecognizedClause`, or `unrecognizedDataClause`
        // in the data division), which ClosedFormatPass refuses BY NAME — but the production never begins at a reserved
        // word, nor its data form at a literal that begins with an unsigned integer (kb/Work PB3373, PB3390), so one the
        // format does not print (`SD SF GLOBAL.`, `FD F DUPLICATES.`, `SPECIAL-NAMES. GLOBAL.`, `01 X PIC X 1,5.`) never
        // enters it and used to arrive here as a bare parse error. Nor does a word the production opens with when a
        // text-predicated clause of the list was the prediction's sole survivor and refused it (`OBJECT-COMPUTER. XYZ
        // FOO IS 12.`, kb/Work PB3391: ClosedFormatResidue's predictionFailedAt). The
        // position is read FROM THE ATN (ClosedFormatResidue), the format and the wording from the ONE table the pass
        // reads, so the refusal is the pass's own — same code, same sentence — and a closed format added tomorrow
        // inherits it.
        if (ClosedFormatResidue(recognizer, token, predictionFailedAt) is { Format: var closedFormat })
            hints.Add(new(new Diagnostics.DiagnosticDescriptor(closedFormat.Diagnostic.Code,
                    Diagnostics.DiagnosticSeverity.Error, "{0}"),   // the catalogue code, single-sourced as every twin's is
                closedFormat.Refusal(WrittenResidueItem(recognizer, token), ClosedFormats.OwnerName(recognizer.Context)) + ".", 0));

        if (token.Type is CobolLexer.CORRESPONDING or CobolLexer.CORR && IsInRule(ruleStack, "moveStatement"))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL2173,
                "Both general formats of the MOVE statement put the whole sending specification directly after "
                + "the verb — Format 1 is 'MOVE { identifier-1 | literal-1 } TO { identifier-2 } …' and Format 2 "
                + "is 'MOVE { CORRESPONDING | CORR } identifier-3 TO identifier-4' — so no format admits a "
                + "CORRESPONDING phrase after a sending operand (ISO §14.9.25.2).", 0));

        // ⛔ NO "MISSING SPACE BEFORE / AFTER STRING LITERAL" GUESS HERE (kb/Work PB1394). A literal delimiter that is not
        // separated is §8.3.5 rule 5's violation, and SeparatorRule reports it — for every literal token, whether or
        // not the parse happens to fail there — as COBOL2633. The two recovery hints that stood here (COBOL0301 /
        // COBOL0302) stated that rule a second time, only on a parse error, and only for a quotation-mark delimiter.

        // 3. Missing TO in MOVE statement
        if (IsInRule(ruleStack, "moveStatement") && IsIdentifier(token) && prev != null && IsLiteral(prev))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0303, "In a MOVE statement, did you forget TO before the target?", 10));

        // ⛔ NO "MISSING PERIOD AFTER PARAGRAPH NAME" GUESS KEYED ON A TOKEN'S TEXT (kb/Work PB2770, review R1-1). The
        // COBOL0304 hint that stood here fired when a token's TEXT contained ".MOVE", ".ADD", ".SUBTRACT", ".PERFORM" or
        // ".IF". No COBOL word can carry a period (ISO §8.3.2.1 limits its characters to letters, digits, hyphen and
        // underscore, and NAME_BODY lexes exactly that), so `MAIN-PARA.MOVE` is three tokens and the separator rule
        // reports the unspaced period (COBOL2632); the only tokens whose text could match were literals and
        // PICTURE strings, where `GO TO "PARA.PERFORM".` answered "the parser is treating it as a qualified
        // reference" — false twice over. The hint is deleted.

        // 5. STATUS where IDENTIFIER expected
        if (token.Type == CobolLexer.STATUS && expectedTokens?.Contains("IDENTIFIER") == true)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0200, "STATUS is a reserved word here. For file status, use 'FILE STATUS IS <data-name>'.", 10));

        // 6. PROGRAM where IDENTIFIER expected
        if (token.Type == CobolLexer.PROGRAM && expectedTokens?.Contains("IDENTIFIER") == true)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0201, "PROGRAM is a reserved word. If this is a paragraph name, it cannot be named PROGRAM.", 10));

        // 7. Unrecognized keyword in SPECIAL-NAMES — a GUESS, suppressed once a structural diagnosis has named the
        // rule (priority 0): 0g names the closed format a stray reserved word violates, and this sentence would only
        // restate it vaguely (kb/Work PB2533).
        if (IsInRule(ruleStack, "specialNamesParagraph") && !IsIdentifier(token) && !IsLiteral(token)
            && hints.TrueForAll(h => h.Priority > 0))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0305, "Unexpected token in SPECIAL-NAMES. Check implementor-name or mnemonic-name syntax.", 15));

        // 8. ASCENDING/DESCENDING KEY where the entry can only end (kb/Work PB1266). KEY is supported; what this
        // catches is its POSITION: §13.18.38.2 writes the OCCURS clause's KEY phrases BEFORE INDEXED BY, so a KEY phrase
        // after it finds the entry ready to end. The hint used to claim the clause was "not yet supported" and that a
        // table had been created without a key — false on both counts.
        if (token.Type is CobolLexer.ASCENDING or CobolLexer.DESCENDING && expectedTokens?.Contains("'.'") == true)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0100, "ASCENDING/DESCENDING KEY is out of place: in an OCCURS clause the KEY phrases are written before INDEXED BY (ISO §13.18.38.2).", 5));

        // ⛔ NO "MAY NOT BE SUPPORTED" GUESS KEYED ON A WORD'S SPELLING (kb/Work PB2770). The hints that stood here
        // (COBOL0101 BLANK WHEN ZERO "may not be recognized", COBOL0102 "this SET form may not be supported",
        // COBOL0103 "SEARCH statement may not be fully supported", and COBOL0107 "EVALUATE with ALSO may not be fully
        // supported" below) fired on the WORD, wherever the parse happened to fail at it, and each stated a
        // limitation the compiler does not have: every one of those constructs is implemented, so the claim sent the
        // user after a cause that does not exist (`MOVE 1 SET X TO 2` lacks MOVE's TO). They are deleted, as DEVLOG
        // 232 deleted COBOL0104-0106 and COBOL0311; a structural hint above (0a-0f) reads the ATN or the parser state.

        // 12. THROUGH/THRU in unexpected context
        if (token.Type is CobolLexer.THROUGH or CobolLexer.THRU && expectedTokens?.Contains("'.'") == true)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0300, "THROUGH/THRU is not recognized in this context. Check PERFORM or VALUE THROUGH syntax.", 10));

        // 13. A misplaced explicit scope terminator (ISO §14.5.3.2), keyed on the token TYPE and on the statement
        // rules the ATN says can match it (kb/Work PB2770), never on the word's spelling: END-SEARCH closes both SEARCH
        // rules, and a word that closes no statement (END-OF-PAGE, a user word spelled END-…) is no terminator at all.
        var terminator = ScopeTerminator(recognizer, token.Type);
        if (terminator is not null && !ruleStack.Any(terminator.Statements.Contains))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0306,
                $"{terminator.Word} appears without a matching {terminator.StatementName} statement.", 5));

        // 14. Missing period at end of sentence — a GUESS, and the lowest-priority one here, so it is suppressed
        // once a STRUCTURAL diagnosis has already named what the general format requires at this position
        // (priority 0). Otherwise `GO TO DEPENDING ON X.` read "…at least one procedure-name is required. A
        // period may be missing at the end of the previous sentence." — the second sentence contradicting the
        // first (kb/Work PB412). A word SPELLED like a scope terminator is exempt whether or not it is one: whoever
        // wrote END-INVOKE (no ISO word, kb/Work PB758) or END-OF-PAGE at a sentence end meant to close something,
        // and a missing period is not what went wrong. Here the spelling is the evidence, so the text is the test.
        if (expectedTokens?.Contains("'.'") == true
            && !(token.Text is { } spelled && CobolNames.StartsWith(spelled, "END-"))
            && hints.TrueForAll(h => h.Priority > 0))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0307, "A period may be missing at the end of the previous sentence.", 25));

        // 15. Literal where identifier expected — a GUESS, suppressed once a structural diagnosis has named the rule
        // (priority 0), as 7 and 14 are: at a closed format's residue position the IDENTIFIER in the expected set is
        // the error production's own left edge, so "a data-name is expected here" would contradict 0g (kb/Work PB3373).
        if (IsLiteral(token) && expectedTokens?.Contains("IDENTIFIER") == true && hints.TrueForAll(h => h.Priority > 0))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0308, "A data-name is expected here, not a literal.", 20));

        // 16. Identifier where literal expected
        if (IsIdentifier(token) &&
            (expectedTokens?.Contains("STRINGLIT") == true || expectedTokens?.Contains("INTEGERLIT") == true))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0309, "A literal value is expected here, not a data-name.", 20));

        // 17. Missing BY in INDEXED BY
        if (token.Type != CobolLexer.BY && expectedTokens?.Contains("'BY'") == true &&
            IsInRule(ruleStack, "dataDescriptionEntry"))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0310, "Missing BY keyword. INDEXED BY requires 'INDEXED BY <index-name>'.", 10));

        // 19. FILE CONTROL / SELECT context errors — a GUESS, suppressed by a structural diagnosis as 7 is (kb/Work PB2533).
        if (IsInRule(ruleStack, "fileControlParagraph") &&
            !IsIdentifier(token) && !IsLiteral(token) &&
            token.Type is not (CobolLexer.SELECT or CobolLexer.ASSIGN or CobolLexer.FILE_CONTROL)
            && hints.TrueForAll(h => h.Priority > 0))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0312,
                "Unexpected token in FILE-CONTROL paragraph. Check SELECT/ASSIGN TO syntax.",
                10));

        return hints;
    }

    // ── A reserved word or a literal at a closed general format's error production (kb/Work PB2533, PB3373) ──

    /// <summary>The closed general format whose error production stands where <paramref name="token"/> — a RESERVED
    /// word, or a literal the production does not open with (in the data division's form, one that begins with an
    /// unsigned integer, kb/Work PB3373, PB3390: <c>01 X PIC X 1,5.</c>) — failed the parse, with the form of the
    /// production that stands there, or null.
    /// <para><b>A WORD the production does open with, where the prediction failed AT it (kb/Work PB3391).</b>
    /// <paramref name="predictionFailedAt"/> says an adaptive prediction found no viable alternative at
    /// <paramref name="token"/>, the decision's FIRST token (<see cref="IsPredictionFailedAt"/>). An IDENTIFIER the
    /// production could open with fails there only when a TEXT-PREDICATED clause of the same list was the decision's
    /// sole syntactic survivor and its predicate refused the word: ANTLR evaluates the predicates of an SLL accept
    /// state and throws NoViableAlt at the decision's start token when none holds. <c>OBJECT-COMPUTER. XYZ FOO IS
    /// 12.</c> is the shape: the run <c>FOO</c> cannot continue at the reserved IS, so only
    /// <c>{segmentLimitAhead()}? IDENTIFIER IS? integerLiteral</c> survives, and FOO is not SEGMENT-LIMIT. The word is
    /// then exactly what the production would have refused by name: no clause of the list opens with it. The
    /// after-period exclusion below does not apply to it: the decision's own lookahead already tried every path out of
    /// the list, its period included, and none took the tokens written.</para>
    /// <para><b>The position, from the ATN.</b> ANTLR's own full-context simulation (<see cref="ResidueWalk"/>) re-runs
    /// the parser from its current ATN state and its real invocation stack over the tokens between the parser's current
    /// token and <paramref name="token"/> — the ones a failed adaptive prediction looked past (a decision that read k
    /// tokens before it failed reports the k-th, with the decision's first token still current). If a configuration
    /// at <paramref name="token"/> holds a call of the error production (either form) on its stack, the rule making
    /// that call is the closed format's alternative list
    /// (<see cref="ClosedFormats.OfRule"/>): an IDENTIFIER there would have been taken by the error production and
    /// refused by name, and a reserved word or a literal it cannot hold is the same residue. The integer that may be
    /// the next entry's level-number is the after-period exclusion below, met in the data division's lists (<c>01 X
    /// PIC X 05 Y …</c>), where the missing period stands; the integer that heads a comma decimal (<c>1,5</c>) is no
    /// level-number and is refused here, quoted whole (kb/Work PB3390).
    /// So is the list whose call of the production the PARSER is still inside, a frame of the parser's own stack
    /// (<c>SOURCE-COMPUTER. XYZ WIBBLE GLOBAL.</c>: the run WIBBLE ended at GLOBAL, kb/Work PB3370) — the innermost
    /// list of all, since the word continues that run.
    /// Where two lists are open at once (<c>SPECIAL-NAMES. GLOBAL.</c> could continue the paragraph's entries or start
    /// a configuration-section paragraph) the INNERMOST wins: the word is written inside it. Nothing is keyed on a
    /// word or a rule name, so a closed format added to the grammar inherits this. Semantic predicates are not
    /// evaluated (it names the format of a source that has already failed; it never decides a parse).</para>
    /// <para><b>A missing separator period is NOT this.</b> A clause list such a format writes ends at a separator
    /// period, and a word that could begin what FOLLOWS that period (<c>PROCEDURE DIVISION</c>, the next <c>SD</c>,
    /// <c>WORKING-STORAGE SECTION</c> after <c>01 X PIC X</c> with its period forgotten) is far likelier the start of
    /// the next construct than a stray clause — so the walk also collects, at each period edge it reaches in the
    /// parser's own context, the tokens that may come after the period, and a word among them is left to the
    /// missing-period guess (hint 14).</para></summary>
    private static Residue? ClosedFormatResidue(Parser recognizer, IToken token, bool predictionFailedAt)
    {
        var atn = recognizer.Atn;
        bool refusedWord = predictionFailedAt && token.Type == CobolLexer.IDENTIFIER;
        if (!(refusedWord || IsReservedWord(token) || IsLiteral(token)) || HeaderWords(atn).Contains(token.Type)) return null;
        var stream = (ITokenStream)recognizer.InputStream;
        var walk = new ResidueWalk(recognizer);
        int k = 1;
        for (; ; k++)
        {
            var at = stream.LT(k);
            if (at.TokenIndex == token.TokenIndex) break;
            if (at.TokenIndex > token.TokenIndex || at.Type == TokenConstants.EOF || k > MaxResimulatedTokens) return null;
            if (!walk.Consume(at.Type)) return null;
        }
        var probe = walk.Probe();
        // A token that may begin what follows the period is the missing period's, unless it HEADS a longer literal: the
        // comma decimal `1,5` begins with INTEGERLIT, but it is one character-string (a comma that no space follows is
        // no separator, ISO §8.3.5 2)), never a level-number — the data division's run cannot hold it (kb/Work PB3390).
        // A word refused where its prediction failed is exempt (kb/Work PB3391, above).
        bool beginsWhatFollows = probe.AfterPeriod.Contains(token.Type)
                                 && CobolParserCoreBase.NumericLiteralWidth(stream, k) <= 1;
        return probe.Format is { } format && (refusedWord || !beginsWhatFollows)
            ? new Residue(format, probe.Production)
            : null;
    }

    /// <summary>A closed format's residue: the format whose list stands there, and the error production's form
    /// (<c>unrecognizedClause</c> or <c>unrecognizedDataClause</c>, kb/Work PB3390) that list calls.</summary>
    private readonly record struct Residue(ClosedFormat Format, int Production);

    /// <summary>The residue item as written from <paramref name="token"/>: the token's own text, or the whole literal it
    /// heads when the literal spans more tokens (the comma decimals <c>1,5</c> and <c>,5</c>), so the parse layer's
    /// refusal quotes a literal whole, as <c>ClosedFormatPass</c> quotes a run's opening (kb/Work PB3373, PB3390).</summary>
    private static string WrittenResidueItem(Parser recognizer, IToken token)
    {
        var stream = (ITokenStream)recognizer.InputStream;
        for (int k = 1; k <= MaxResimulatedTokens; k++)
        {
            var at = stream.LT(k);
            if (at.TokenIndex > token.TokenIndex || at.Type == TokenConstants.EOF) break;
            if (at.TokenIndex != token.TokenIndex) continue;
            int width = CobolParserCoreBase.NumericLiteralWidth(stream, k);
            return width > 1 ? stream.GetText(at, stream.LT(k + width - 1)) : token.Text;
        }
        return token.Text;
    }

    /// <summary>The words that OPEN a division, section or paragraph header — the first token of a rule whose next
    /// token, within that rule, is a separator period or the word SECTION or DIVISION (<c>FILE-CONTROL .</c>,
    /// <c>WORKING-STORAGE SECTION</c>, <c>PROCEDURE DIVISION</c>) — read ONCE from the ATN. Such a word where a clause
    /// list stands is a header written out of order (an I-O-CONTROL paragraph before FILE-CONTROL) or after a missing
    /// period, a STRUCTURE error, and naming it "not a clause of the I-O-CONTROL paragraph" would send the reader to
    /// the wrong rule; <see cref="ClosedFormatResidue"/> leaves it to the generic diagnosis. A clause keyword never
    /// qualifies: no clause rule writes a separator period. Cached in a static, as the other ATN-derived sets are.</summary>
    private static IntervalSet HeaderWords(ATN atn)
    {
        if (_headerWords is { } cached) return cached;
        var closers = new IntervalSet(CobolLexer.DOT, CobolLexer.SECTION, CobolLexer.DIVISION);
        var words = new IntervalSet();
        foreach (var ruleStart in atn.ruleToStartState)
        {
            // The rule's left edge: the token edges reachable from its start state through epsilon edges alone.
            var visited = new HashSet<int>();
            var work = new Stack<ATNState>();
            work.Push(ruleStart);
            while (work.TryPop(out var state))
            {
                if (!visited.Add(state.stateNumber)) continue;
                for (int i = 0; i < state.NumberOfTransitions; i++)
                {
                    var t = state.Transition(i);
                    if (t is RuleTransition) continue;   // a rule that opens with another rule opens with a word of THAT rule
                    if (t.IsEpsilon) work.Push(t.target);
                    else if (t is AtomTransition atom && atn.NextTokens(atom.target).And(closers).Count > 0)
                        words.AddAll(atom.Label);
                }
            }
        }
        return _headerWords = words;
    }

    private static IntervalSet? _headerWords;

    /// <summary>The bound on the tokens <see cref="ClosedFormatResidue"/> re-simulates: a prediction that failed further
    /// ahead than this is not at a clause position anyone would call "the place", and the walk declines.</summary>
    private const int MaxResimulatedTokens = 16;

    /// <summary>⛔ THE WALK IS ANTLR'S OWN FULL-CONTEXT SIMULATION, NOT A COPY OF IT (kb/Work PB3489). The re-simulation
    /// runs on the runtime's <see cref="ParserATNSimulator"/>: its <c>Closure</c> in full-context mode, its
    /// graph-structured call stacks (<see cref="PredictionContext"/>, seeded from the parser's real invocation stack by
    /// <see cref="PredictionContext.FromRuleContext"/>) and its merge of the configurations reaching one state. The walk
    /// used to keep its own closure with one linked call stack per path, and every argument reaches a function-identifier
    /// along many call paths, so the stacks multiplied with each nested argument list: <c>FUNCTION A(FUNCTION B(FUNCTION
    /// C(FUNCTION SUBSTRING(X FROM 1 FOR 4))))</c> never finished. Merged, the work grows with the nesting, never
    /// exponentially in it.
    /// <para>Why not the failed prediction's own <c>NoViableAltException.DeadEndConfigs</c>: the parse predicts in SLL
    /// mode first, and an SLL closure that leaves the decision's rule follows the links to EVERY caller of that rule
    /// (the outer context is a wildcard), so those configurations cannot say which list a word stands in. And not the
    /// runtime's <c>ComputeReachSet</c>: once a single alternative remains it returns the moved configurations without
    /// closing them, which serves prediction and defeats a walk with one alternative. So the token step is the loop of
    /// <see cref="Consume"/>, and everything else is the runtime's.</para>
    /// <para>The answer is then READ off the final configuration set (<see cref="Probe"/>): the closure keeps every
    /// configuration at a state with a token edge, each with its whole stack, so a call of the error production is a
    /// frame of some stack, and a period in the parser's own context is a period edge whose stack is one of the
    /// parser's own (<see cref="_own"/>).</para></summary>
    private sealed class ResidueWalk : ParserATNSimulator
    {
        /// <summary>The stack of each rule context on the parser's real invocation stack: a configuration whose stack is
        /// one of these stands in the parser's own context, not in a rule the walk entered.</summary>
        private readonly Dictionary<PredictionContext, RuleContext> _own = [];

        private readonly Dictionary<PredictionContext, int> _frames = [];

        private ATNConfigSet _configs;

        /// <summary>A walk from the parser's current state and real context. It never predicts, so it never reads a DFA:
        /// the base constructor only stores the array it is given, and an empty one of the parser's size keeps the walk
        /// from touching the parser's shared DFA cache at all.</summary>
        public ResidueWalk(Parser parser)
            : base(parser, parser.Atn, new DFA[parser.Atn.NumberOfDecisions], new PredictionContextCache())
        {
            mergeCache = new MergeCache();
            for (RuleContext? c = parser.Context; c is not null; c = c.Parent)
                _own.TryAdd(PredictionContext.FromRuleContext(atn, c), c);
            _configs = Close([new ATNConfig(atn.states[parser.State], 1, PredictionContext.FromRuleContext(atn, parser.Context))]);
        }

        /// <summary>Step every configuration over one token of type <paramref name="ttype"/> and close the result;
        /// false when none can take it.</summary>
        public bool Consume(int ttype)
        {
            var moved = new List<ATNConfig>();
            foreach (var c in _configs.Elements)
                for (int i = 0; i < c.state.NumberOfTransitions; i++)
                    if (GetReachableTarget(c.state.Transition(i), ttype) is { } target)
                        moved.Add(new ATNConfig(c, target));
            _configs = Close(moved);
            return _configs.Count > 0;
        }

        private ATNConfigSet Close(IEnumerable<ATNConfig> seeds)
        {
            var closed = new ATNConfigSet(fullCtx: true);
            var busy = new HashSet<ATNConfig>();
            foreach (var c in seeds)
                Closure(c, closed, busy, collectPredicates: false, fullCtx: true, treatEofAsEpsilon: false);
            return closed;
        }

        /// <summary>What the closed configurations show at the refused token: every call of the error production on a
        /// stack (the list rule that made it, at that list's depth — the parser's own call included, kb/Work PB3370),
        /// and, at every period edge in the parser's own context, the tokens that may follow the period.</summary>
        public ResidueProbe Probe()
        {
            var probe = new ResidueProbe();
            var searched = new HashSet<(PredictionContext, int)>();
            foreach (var c in _configs.Elements)
            {
                FindProductionCalls(c.context, c.state.ruleIndex, probe, searched);
                for (int i = 0; i < c.state.NumberOfTransitions; i++)
                {
                    var t = c.state.Transition(i);
                    if (t.IsEpsilon || t is NotSetTransition || t.Label?.Contains(CobolLexer.DOT) != true) continue;
                    foreach (var own in OwnContexts(c.context))
                        probe.AfterPeriod.AddAll(atn.NextTokens(t.target, own));
                }
            }
            return probe;
        }

        /// <summary>Walk <paramref name="stack"/> down from a configuration in rule <paramref name="rule"/>: where a
        /// frame returns from the error production, the rule it returns into is the list that called it.</summary>
        private void FindProductionCalls(PredictionContext stack, int rule, ResidueProbe probe,
            HashSet<(PredictionContext, int)> searched)
        {
            if (!searched.Add((stack, rule))) return;
            for (int i = 0; i < stack.Size; i++)
            {
                int returnState = stack.GetReturnState(i);
                if (returnState == PredictionContext.EMPTY_RETURN_STATE) continue;
                int caller = atn.states[returnState].ruleIndex;
                var beneath = stack.GetParent(i);
                if (ClosedFormats.IsErrorProduction(rule)) probe.Found(caller, rule, Frames(beneath) + 1);
                FindProductionCalls(beneath, caller, probe, searched);
            }
        }

        /// <summary>The number of frames on the longest stack <paramref name="stack"/> stands for.</summary>
        private int Frames(PredictionContext stack)
        {
            if (stack.IsEmpty) return 0;
            if (_frames.TryGetValue(stack, out int frames)) return frames;
            frames = 0;
            for (int i = 0; i < stack.Size; i++)
                if (stack.GetReturnState(i) != PredictionContext.EMPTY_RETURN_STATE)
                    frames = Math.Max(frames, 1 + Frames(stack.GetParent(i)));
            _frames[stack] = frames;
            return frames;
        }

        /// <summary>The parser's own rule contexts <paramref name="stack"/> stands in: a merged stack may hold one of
        /// them beside stacks the walk built.</summary>
        private IEnumerable<RuleContext> OwnContexts(PredictionContext stack)
        {
            if (_own.TryGetValue(stack, out var whole)) { yield return whole; yield break; }
            for (int i = 0; i < stack.Size; i++)
            {
                int returnState = stack.GetReturnState(i);
                var branch = returnState == PredictionContext.EMPTY_RETURN_STATE
                    ? EmptyPredictionContext.Instance
                    : SingletonPredictionContext.Create(stack.GetParent(i), returnState);
                if (_own.TryGetValue(branch, out var own)) yield return own;
            }
        }
    }

    /// <summary>What the final closure of <see cref="ClosedFormatResidue"/> found: the innermost closed format whose
    /// error production it reached, the form of the production that list calls, and the tokens that may follow a
    /// period it reached.</summary>
    private sealed class ResidueProbe
    {
        private int _depth = -1;
        public ClosedFormat? Format { get; private set; }
        public int Production { get; private set; } = -1;
        public IntervalSet AfterPeriod { get; } = new();

        public void Found(int listRule, int production, int depth)
        {
            if (depth <= _depth || ClosedFormats.OfRule(listRule) is not { } format) return;
            Format = format;
            Production = production;
            _depth = depth;
        }
    }

    /// <summary>A reserved or context-sensitive word, as the lexer itself classifies it: a token whose text the lexer
    /// makes a keyword token of exactly this type (<see cref="CobolKeywordTokens.TryTokenType"/>, which reaches the
    /// multi-spelling keywords the vocabulary publishes no literal name for). Every such word begins with a letter;
    /// testing that first keeps a numeric literal away from the lexer probe.</summary>
    private static bool IsReservedWord(IToken token)
        => token.Text is [var first, ..] text && char.IsAsciiLetter(first)
           && CobolKeywordTokens.TryTokenType(text, out int type) && type == token.Type;

    // ── The GO TO general-format complement (kb/Work PB412) ──

    /// <summary>The §14.9.17.2 message for a GO TO written in a shape NEITHER general format prints, or null.
    /// <para>Two parser states carry the two complement shapes, and each is read structurally:</para>
    /// <list type="bullet">
    ///   <item>The statement context is still on the rule-invocation stack — the parser committed to the
    ///         Format-2 alternative on seeing a LIST of procedure-names and then found no DEPENDING. The
    ///         half-built <c>GoToStatementContext</c> already holds the children matched so far, so the counts
    ///         handed to <see cref="GoToFormats.DiagnoseWrittenShape"/> are the ones actually written — no
    ///         re-scan of the token stream, and no second copy of the format rule.</item>
    ///   <item>The offending token IS <c>DEPENDING</c> immediately after <c>GO</c> or <c>GO TO</c> — the
    ///         target-less alternative matched and the statement ended, so the context has already been popped
    ///         and the shape has to be read off the stream. The <c>GO</c> anchor is what keeps this off an
    ///         OCCURS … DEPENDING ON, whose DEPENDING never follows a GO.</item>
    /// </list></summary>
    private static string? GoToComplementMessage(Parser recognizer, IToken token, ITokenStream stream)
    {
        for (RuleContext? c = recognizer.Context; c is not null; c = c.Parent)
            if (c is CobolParserCore.GoToStatementContext g)
                return GoToFormats.DiagnoseWrittenShape(g.procedureName().Length, g.DEPENDING() is not null);

        if (token.Type != CobolLexer.DEPENDING) return null;
        var prev = GetToken(stream, token.TokenIndex - 1);
        if (prev?.Type == CobolLexer.TO) prev = GetToken(stream, token.TokenIndex - 2);
        return prev?.Type == CobolLexer.GO ? GoToFormats.DiagnoseWrittenShape(0, hasDepending: true) : null;
    }

    /// <summary>Whether the failure is at the empty parenthesis pair of an inline method invocation segment —
    /// <c>:: literal-1 ( )</c> (§8.4.3.4.2), with <paramref name="token"/> its '(' or its ')'. The §8.7.4 operator has
    /// no other role (§8.3.5 rule 7 makes '::' one lexical unit), so the four default-channel tokens are this format and
    /// nothing else. literal-1 is a literal token or the constant-name word that spells it (§13.10.3 SR2, kb/Work PB780).
    /// GROUPING-PAREN-ONLY: a '(' after literal-1 is always the plain token — the lexer retypes a '(' to
    /// FNARG_LPAREN only after <c>FUNCTION</c> function-name (§8.4.3.2.3 SR6) and to REF_LPAREN only after a word that
    /// could name a data item and does not follow '::' (<c>OnDefaultLParen</c>), and literal-1 is neither (the same
    /// reasoning as <c>InvocationSite.OfInlineSegment</c>).</summary>
    private static bool IsEmptyInlineArgumentList(ITokenStream stream, IToken token)
    {
        var open = token.Type == CobolLexer.LPAREN ? token
            : token.Type == CobolLexer.RPAREN ? PreviousDefault(stream, token.TokenIndex) : null;
        if (open?.Type != CobolLexer.LPAREN || NextDefault(stream, open.TokenIndex)?.Type != CobolLexer.RPAREN)
            return false;
        var name = PreviousDefault(stream, open.TokenIndex);
        return name is not null && name.Type is not (CobolLexer.LPAREN or CobolLexer.RPAREN)
            && PreviousDefault(stream, name.TokenIndex)?.Type == CobolLexer.COLONCOLON;
    }

    // ── The SEARCH general-format complement (kb/Work PB446) ──

    /// <summary>The §14.9.37.2 message for a SEARCH written with a phrase neither general format prints, or null.
    /// Format 1 is <c>SEARCH identifier-1 [VARYING …] [AT END imperative-statement-1] {WHEN condition-1 {…}} …
    /// [END-SEARCH]</c>; Format 2 is <c>SEARCH ALL identifier-1 [AT END imperative-statement-1] WHEN … [AND …] …
    /// {…} [END-SEARCH]</c> (rendered from PDF page 750 / printed folio 720). Three complement shapes, each read
    /// from the parser state:
    /// <list type="bullet">
    ///   <item>A KEY phrase — the offending token is KEY and a SEARCH context with no WHEN phrase yet is on the stack:
    ///         the table operand is complete, and the failure is at the statement's next element (since the AT END
    ///         phrase became the shared <c>conditionalPhrases</c> rule, which matches empty, that is inside the first
    ///         WHEN clause the parse entered, kb/Work PB3830).</item>
    ///   <item>A NOT AT END phrase — a SEARCH context with no WHEN phrase yet is on the stack (the failure is in
    ///         the AT END region, possibly inside its last statement) and the tokens at the failure spell
    ///         <c>NOT [AT] END</c>.</item>
    ///   <item>A second Format-2 WHEN — the Format-2 statement has already COMPLETED (its one WHEN phrase
    ///         matched), so the SEARCH ALL context is the rightmost descendant of an enclosing context and ends
    ///         immediately before the offending WHEN.</item>
    /// </list></summary>
    private static string? SearchFormatShapeMessage(Parser recognizer, IToken token, ITokenStream stream)
    {
        const string formats = " ISO §14.9.37.2 prints two SEARCH formats and ";
        var ctx = recognizer.Context;
        if (token.Type == CobolLexer.KEY && NearestSearchBeforeWhen(ctx) is { } keyed)
            return (keyed is CobolParserCore.SearchAllStatementContext ? "SEARCH ALL" : "SEARCH")
                + " … KEY:" + formats + "neither has a KEY phrase — the key of a SEARCH ALL is declared by the "
                + "ASCENDING/DESCENDING KEY phrase of identifier-1's OCCURS clause (§14.9.37.3 SR7), and the "
                + "Format-2 WHEN phrase names it. Remove the phrase.";

        bool notAtEnd = token.Type == CobolLexer.NOT ? IsAtEndAhead(stream, token.TokenIndex + 1)
            : token.Type is CobolLexer.AT or CobolLexer.END
              && PreviousDefault(stream, token.TokenIndex)?.Type == CobolLexer.NOT
              && IsAtEndAhead(stream, token.TokenIndex);
        // "No WHEN phrase yet" is asked of the WHEN TOKEN, not of the clause context: single-token insertion
        // reports a missing WHEN from INSIDE the just-entered (Format-2) WHEN clause, whose context already exists.
        if (notAtEnd && NearestSearchBeforeWhen(ctx) is { } searching)
            return searching is CobolParserCore.SearchAllStatementContext
                ? "SEARCH ALL … NOT AT END:" + formats + "each has an AT END phrase alone — there is no "
                  + "NOT AT END phrase. Test for a hit in the WHEN branch instead."
                : "SEARCH … NOT AT END:" + formats + "each has an AT END phrase alone — there is no NOT "
                  + "AT END phrase. Test for a hit in a WHEN branch instead.";

        if (token.Type == CobolLexer.WHEN)
            for (var c = ctx; c is not null; c = c.Parent as ParserRuleContext)
                for (var d = LastRuleChild(c); d is not null; d = LastRuleChild(d))
                    if (d is CobolParserCore.SearchAllStatementContext sa && sa.Stop is { } stop
                        && NextDefault(stream, stop.TokenIndex)?.TokenIndex == token.TokenIndex)
                        return "SEARCH ALL with a second WHEN phrase: Format 2 prints exactly ONE WHEN phrase — its "
                            + "ellipsis sits on the [ AND … ] bracket, not on the WHEN brace (ISO §14.9.37.2, with "
                            + "§5.2.7), so a further key condition is written as an AND phrase of the one WHEN.";
        return null;
    }

    /// <summary>The nearest SEARCH or SEARCH ALL context on the stack, when its first WHEN phrase has not been read yet
    /// (the failure is in the region before it: the table operand, VARYING, the AT END phrase); null when the nearest
    /// one is already in its WHEN phrases, or there is none.</summary>
    private static ParserRuleContext? NearestSearchBeforeWhen(RuleContext? ctx)
    {
        for (RuleContext? c = ctx; c is not null; c = c.Parent)
        {
            if (c is CobolParserCore.SearchStatementContext s1)
                return s1.searchWhenClause().Any(w => w.WHEN() is not null) ? null : s1;
            if (c is CobolParserCore.SearchAllStatementContext s2)
                return s2.searchAllWhenClause()?.WHEN() is not null ? null : s2;
        }
        return null;
    }

    // ── A suffix on a mnemonic-name (kb/Work PB2499) ──

    /// <summary>The <see cref="MnemonicNameSlot"/> message for a subscript, reference modification or qualifier
    /// written on a mnemonic-name in a slot the grammar spells as a word, or null. Two shapes, each read from the
    /// parser state:
    /// <list type="bullet">
    ///   <item>ACCEPT … FROM and DISPLAY … UPON — the statement COMPLETED at the word, so the slot's context
    ///         (<c>acceptSource</c>'s word arm, <c>displayUpon</c>) is the rightmost descendant of an enclosing
    ///         context and ends immediately before the offending '(' / OF / IN.</item>
    ///   <item>SET Format 3 — no SET format admits the suffixed list, so the failure is inside the SET statement
    ///         itself; it is Format 3's when the receiving list that holds the suffix ends at <c>TO ON</c> or
    ///         <c>TO OFF</c>, which only Format 3 writes.</item>
    /// </list></summary>
    private static string? MnemonicSuffixMessage(Parser recognizer, IToken token, ITokenStream stream)
    {
        if (IsMnemonicSuffixStart(token.Type))
            for (var c = recognizer.Context; c is not null; c = c.Parent as ParserRuleContext)
                for (var d = LastRuleChild(c); d is not null; d = LastRuleChild(d))
                {
                    var (slot, format, word) = d switch
                    {
                        CobolParserCore.AcceptSourceContext a => ("ACCEPT … FROM", "§14.9.1.2 Format 1", a.cobolWord()),
                        CobolParserCore.DisplayUponContext u => ("DISPLAY … UPON", "§14.9.11.2 Format 1", u.cobolWord()),
                        _ => (null, null, null),
                    };
                    if (word?.Stop is { } stop && NextDefault(stream, stop.TokenIndex)?.TokenIndex == token.TokenIndex)
                        return MnemonicNameSlot.SuffixMessage(slot!, word.GetText(), format!);
                }

        for (RuleContext? c = recognizer.Context; c is not null; c = c.Parent)
            if (c is CobolParserCore.SetStatementContext set)
            {
                // SET word … <suffix> … TO {ON | OFF}: the first suffix names its word; the list must end at TO ON/OFF.
                IToken? suffixed = null;
                for (var t = NextDefault(stream, set.Start.TokenIndex); t is not null; t = NextDefault(stream, t.TokenIndex))
                {
                    if (t.Type is CobolLexer.DOT or TokenConstants.EOF) return null;
                    if (t.Type == CobolLexer.TO)
                        return suffixed is not null && NextDefault(stream, t.TokenIndex)?.Type is CobolLexer.ON or CobolLexer.OFF
                            ? MnemonicNameSlot.SuffixMessage("SET … TO ON/OFF", suffixed.Text, "§14.9.39.2 Format 3")
                            : null;
                    if (suffixed is null && IsMnemonicSuffixStart(t.Type))
                        suffixed = PreviousDefault(stream, t.TokenIndex);
                }
                return null;
            }
        return null;
    }

    /// <summary>A token that opens a subscript or reference modification (any of the three '(' twins) or a
    /// qualifier (OF / IN) — the suffixes a <c>dataReference</c> admits and a mnemonic-name does not.</summary>
    private static bool IsMnemonicSuffixStart(int type) =>
        IsOpenParen(type) || type is CobolLexer.OF or CobolLexer.IN;

    /// <summary>True when the default-channel tokens from <paramref name="index"/> spell <c>[AT] END</c>.</summary>
    private static bool IsAtEndAhead(ITokenStream stream, int index)
    {
        var t = GetToken(stream, index);
        while (t is not null && t.Channel != TokenConstants.DefaultChannel) t = GetToken(stream, t.TokenIndex + 1);
        if (t?.Type == CobolLexer.AT) t = NextDefault(stream, t.TokenIndex);
        return t?.Type == CobolLexer.END;
    }

    private static IToken? NextDefault(ITokenStream stream, int index)
    {
        for (var t = GetToken(stream, index + 1); t is not null; t = GetToken(stream, t.TokenIndex + 1))
            if (t.Channel == TokenConstants.DefaultChannel) return t;
        return null;
    }

    private static IToken? PreviousDefault(ITokenStream stream, int index)
    {
        for (var t = GetToken(stream, index - 1); t is not null; t = GetToken(stream, t.TokenIndex - 1))
            if (t.Channel == TokenConstants.DefaultChannel) return t;
        return null;
    }

    /// <summary>The LAST child when it is a rule context; null when it is a token (nothing to the right of a token
    /// can be a just-completed statement). Shared with ConditionalPhraseAssociation.UnownedPhraseMessage.</summary>
    internal static ParserRuleContext? LastRuleChild(ParserRuleContext c) =>
        c.ChildCount > 0 ? c.GetChild(c.ChildCount - 1) as ParserRuleContext : null;

    // ── Parse-layer edition-gate rendering (rearch PHASE 02) ──

    /// <summary>Captures the single <see cref="EditionDiagnostic"/> a one-shot <see cref="ConstructRegistry.Check"/>
    /// reports, so the parse-layer error strategy can reuse the ONE funnel's exact COBOL0900 text (display /
    /// introduction edition / ISO citation all sourced from the construct's registry row, not hand-copied).</summary>
    private sealed class CaptureSink : IDiagnosticSink
    {
        public string? Message { get; private set; }
        public void Report(in EditionDiagnostic d) => Message = d.Message;
    }

    // ── The explicit scope terminators (kb/Work PB2770) ──

    /// <summary>An explicit scope terminator (ISO §14.5.3.2): its reserved word, the statement-name it is defined for
    /// (Table 12 pairs END-x with x), and the grammar's statement rules that match it.</summary>
    private sealed record ScopeTerminatorInfo(string Word, string StatementName, HashSet<string> Statements);

    private static Dictionary<int, ScopeTerminatorInfo>? _scopeTerminators;

    /// <summary>The scope terminator a token TYPE is, or null — read ONCE from the ATN, never from the token's text:
    /// a lexer word spelled END-x that some <c>…Statement</c> rule matches in its own body is the terminator of the
    /// statements those rules spell (END-SEARCH → searchStatement and searchAllStatement). END-OF-PAGE, which only a
    /// WRITE phrase matches, and a user-defined word spelled END-… (END-INVOKE is no ISO word, kb/Work PB758) are
    /// no terminator, so the misplaced-terminator hint never names a statement for them. Cached in a static because
    /// the ATN is a static of the generated parser; a benign race recomputes the same map.</summary>
    private static ScopeTerminatorInfo? ScopeTerminator(Parser recognizer, int tokenType)
    {
        _scopeTerminators ??= BuildScopeTerminators(recognizer);
        return _scopeTerminators.GetValueOrDefault(tokenType);
    }

    private static Dictionary<int, ScopeTerminatorInfo> BuildScopeTerminators(Parser recognizer)
    {
        const string prefix = "END-";
        var map = new Dictionary<int, ScopeTerminatorInfo>();
        foreach (var state in recognizer.Atn.states)
        {
            if (state is null) continue;
            string rule = recognizer.RuleNames[state.ruleIndex];
            if (!rule.EndsWith("Statement", StringComparison.Ordinal)) continue;
            for (int i = 0; i < state.NumberOfTransitions; i++)
            {
                var t = state.Transition(i);
                if (t is not (Antlr4.Runtime.Atn.AtomTransition or Antlr4.Runtime.Atn.SetTransition)
                    || t is Antlr4.Runtime.Atn.NotSetTransition || t.Label is null)
                    continue;
                foreach (int type in t.Label.ToIntegerList())
                {
                    string? word = recognizer.Vocabulary.GetLiteralName(type)?.Trim('\'');
                    if (word is null || !word.StartsWith(prefix, StringComparison.Ordinal)) continue;
                    if (!map.TryGetValue(type, out var info))
                        map[type] = info = new(word, word[prefix.Length..], new HashSet<string>(StringComparer.Ordinal));
                    info.Statements.Add(rule);
                }
            }
        }
        return map;
    }

    // ── The required-imperative test (kb/Work PB396) ──

    /// <summary>FIRST(<c>statementBlock</c>) — every token that can begin the grammar's imperative-statement
    /// operand — read ONCE from the ATN. Cached in a static because the ATN is a static of the generated parser;
    /// a benign race recomputes the same set.</summary>
    private static IntervalSet? _statementBlockStart;

    private static IntervalSet StatementBlockStart(Parser recognizer)
        => _statementBlockStart ??= recognizer.Atn.NextTokens(
               recognizer.Atn.ruleToStartState[CobolParserCore.RULE_statementBlock]);

    private static IntervalSet[]? _figurativeFamilies;

    /// <summary>The §8.3.3.6.2 figurative-word families — ZERO/ZEROS/ZEROES, SPACE/SPACES, HIGH-VALUE/HIGH-VALUES,
    /// LOW-VALUE/LOW-VALUES, QUOTE/QUOTES — each the FIRST set of the grammar rule that writes the family's
    /// interchangeability once (kb/Work PB510). Derived from the ATN, never spelled here.</summary>
    public static IntervalSet[] FigurativeFamilies(Parser recognizer)
        => _figurativeFamilies ??= [.. new[]
            {
                CobolParserCore.RULE_zeroWord, CobolParserCore.RULE_spaceWord, CobolParserCore.RULE_highValueWord,
                CobolParserCore.RULE_lowValueWord, CobolParserCore.RULE_quoteWord,
            }.Select(r => recognizer.Atn.NextTokens(recognizer.Atn.ruleToStartState[r]))];

    /// <summary>The COBOL2418 message when the parser failed ON one spelling of a figurative family at a
    /// position whose general format names a DIFFERENT spelling of that family as its keyword; null otherwise.
    /// A position that admits the figurative constant admits every spelling, so the test cannot fire there.</summary>
    private static string? FigurativeSpellingMessage(Parser recognizer, IToken token)
    {
        if (SignConditionSpellingMessage(recognizer, token) is { } signMsg) return signMsg;
        var family = FigurativeFamilies(recognizer).FirstOrDefault(f => f.Contains(token.Type));
        if (family is null) return null;
        var expected = recognizer.GetExpectedTokens();
        if (expected is null || expected.Contains(token.Type)) return null;
        var wanted = family.ToIntegerList().Where(expected.Contains).ToList();
        if (wanted.Count == 0) return null;
        string Spell(int t) => recognizer.Vocabulary.GetLiteralName(t)?.Trim('\'') ?? recognizer.Vocabulary.GetDisplayName(t);
        string required = string.Join(" or ", wanted.Select(Spell));
        return $"The general format requires the keyword {required} here, and {token.Text?.ToUpperInvariant()} is a "
            + "different reserved word (ISO §8.9): the spellings of a figurative constant are interchangeable only "
            + "where the figurative constant itself is written (ISO §8.3.3.6.2), and a keyword underlined in a "
            + "general format is required as printed (ISO §5.2.2).";
    }

    /// <summary>The one keyword position the expected-set test above cannot see: the SIGN CONDITION
    /// (§8.8.4.7.2, <c>operand IS [NOT] { POSITIVE | NEGATIVE | ZERO }</c>). When no comparisonExpression
    /// alternative survives <c>W IS ZEROS</c>, ANTLR's prediction falls back to the alternative that FINISHED the
    /// decision rule — the bare operand — so the failure is reported one token EARLY, at the IS / NOT that the
    /// enclosing statement cannot continue with (<c>W IS ZEROS</c>), or as a no-viable-alternative AT the word
    /// with a decision-state expected set that names no ZERO (<c>W IS NOT ZEROES</c>). Both are recognized here:
    /// a run of IS / NOT that the offending token starts, or ends just before it, meets a ZERO-family spelling
    /// that is not the one the sign condition's format prints (the family read from the ATN).</summary>
    private static string? SignConditionSpellingMessage(Parser recognizer, IToken token)
    {
        // A condition is written only in the procedure division (and its declaratives); an IS / NOT before a
        // figurative spelling elsewhere (VALUE IS ZEROS) is a figurative position, never a sign condition.
        if (!recognizer.GetRuleInvocationStack().Contains("procedureDivision")) return null;
        var stream = (ITokenStream)recognizer.InputStream;
        var zeroFamily = FigurativeFamilies(recognizer)[0];
        IToken? next = token;
        if (token.Type is CobolLexer.IS or CobolLexer.NOT)
        {
            int i = token.TokenIndex;
            do next = NextDefault(stream, ref i);
            while (next is not null && next.Type is CobolLexer.IS or CobolLexer.NOT);
        }
        else
        {
            int j = token.TokenIndex;
            if (PreviousDefault(stream, ref j) is not { Type: CobolLexer.IS or CobolLexer.NOT }) return null;
        }
        if (next is null || next.Type == CobolLexer.ZERO || !zeroFamily.Contains(next.Type)) return null;
        return $"The sign condition's general format requires the keyword ZERO here, and "
            + $"{next.Text?.ToUpperInvariant()} is a different reserved word (ISO §8.9): its format is "
            + "'IS [NOT] { POSITIVE | NEGATIVE | ZERO }' (ISO §8.8.4.7.2), and the spellings of a figurative "
            + "constant are interchangeable only where the figurative constant itself is written (ISO §8.3.3.6.2). "
            + "To compare against the figurative constant, write a relation condition: '= ZEROS'.";
    }

    private static IToken? PreviousDefault(ITokenStream stream, ref int index)
    {
        while (--index >= 0)
        {
            var t = stream.Get(index);
            if (t.Channel == Lexer.DefaultTokenChannel) return t;
        }
        return null;
    }

    private static IToken? NextDefault(ITokenStream stream, ref int index)
    {
        while (++index < stream.Size)
        {
            var t = stream.Get(index);
            if (t.Type == TokenConstants.EOF) return null;
            if (t.Channel == Lexer.DefaultTokenChannel) return t;
        }
        return null;
    }

    /// <summary>True when the parser failed AT a position where a statement block was required and had matched
    /// nothing: the expected set admits every token that can start one, and the offending token starts none.
    /// <para>The "expected ⊇ FIRST(statementBlock)" direction is what makes this specific — at a position where
    /// the block is one of several continuations the parse does not fail at all, and at a position inside a
    /// half-written statement (<c>MOVE TO Y</c>) the expected set is that statement's operand set, not the
    /// statement-start set.</para></summary>
    private static bool IsEmptyRequiredStatementBlock(Parser recognizer, IToken token)
    {
        var start = StatementBlockStart(recognizer);
        if (start.Contains(token.Type)) return false;          // the token CAN start a statement — a later error
        var expected = recognizer.GetExpectedTokens();
        if (expected is null) return false;
        foreach (int t in start.ToIntegerList())
            if (!expected.Contains(t)) return false;
        return true;
    }

    /// <summary>The enclosing statement's COBOL name, taken from the rule invocation stack and spelled from the
    /// RULE NAME rather than a table: <c>ifStatement</c> → IF, <c>searchAllStatement</c> → SEARCH ALL. Null when
    /// the failure is not inside a statement at all (a bad token at paragraph level), which is what keeps the
    /// required-imperative arm from firing outside a general format's operand.</summary>
    private static string? EnclosingStatementName(string[] ruleStack)
    {
        const string suffix = "Statement";
        foreach (string rule in ruleStack)
            if (rule.Length > suffix.Length && rule.EndsWith(suffix, StringComparison.Ordinal))
                return SplitCamelCase(rule[..^suffix.Length]).ToUpperInvariant();
        return null;
    }

    private static string SplitCamelCase(string name)
    {
        var sb = new System.Text.StringBuilder(name.Length + 4);
        foreach (char c in name)
        {
            if (char.IsUpper(c) && sb.Length > 0) sb.Append(' ');
            sb.Append(c);
        }
        return sb.ToString();
    }

    // ── Helpers ──

    private static IToken? GetToken(ITokenStream stream, int index)
        => index >= 0 && index < stream.Size ? stream.Get(index) : null;

    private static bool IsIdentifier(IToken token)
        => token.Type == CobolLexer.IDENTIFIER;

    /// <summary>Did an adaptive prediction find no viable alternative AT <paramref name="token"/>, the decision's first
    /// token? A NoViableAltException carries the decision's start token and the token it reports; they are one token
    /// when the sole survivor's semantic predicate refused the start token (ANTLR rewinds the input to it before it
    /// evaluates an accept state's predicates), the case kb/Work PB3391 names.</summary>
    private static bool IsPredictionFailedAt(RecognitionException? e, IToken token)
        => e is NoViableAltException { StartToken: { } start } && start.TokenIndex == token.TokenIndex;

    /// <summary>A token that is a whole literal by itself: a <c>residueLiteral</c> token or the unsigned integer, which
    /// together are every single-token alternative of <c>nonNumericLiteral</c> and <c>numericLiteralCore</c>
    /// (ClosedFormatDriftTests holds the equality). Read from the ATN once. It used to be STRINGLIT, INTEGERLIT and
    /// DECIMALLIT alone, so a national, boolean, hexadecimal or floating-point literal was "no literal" to every hint
    /// below (kb/Work PB3373).</summary>
    private static bool IsLiteral(IToken token) => LiteralTokens.Contains(token.Type);

    private static IntervalSet LiteralTokens => _literalTokens ??= LiteralTokenSet();

    private static IntervalSet? _literalTokens;

    private static IntervalSet LiteralTokenSet()
    {
        var atn = CobolParserCore._ATN;
        var set = new IntervalSet();
        set.AddAll(atn.NextTokens(atn.ruleToStartState[CobolParserCore.RULE_residueLiteral]));
        set.Add(CobolLexer.INTEGERLIT);
        set.SetReadonly(true);
        return set;
    }

    private static bool IsInRule(string[] ruleStack, string ruleName)
        => ruleStack.Any(r => CobolNames.Same(r, ruleName));

    private static string Truncate(string? text, int max)
    {
        if (text == null) return "<EOF>";
        return text.Length <= max ? text : text[..max] + "...";
    }
}
