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
    /// <para>A hint is STRUCTURAL (priority 0 or less) when it names the cause from the parser state or the ATN, and a
    /// GUESS (priority above 0) when it infers one from a token's kind or its neighbours. A guess that a structural
    /// diagnosis would contradict or restate stands behind <see cref="HasStructuralHint"/> (<c>GO TO DEPENDING ON X.</c>
    /// read "at least one procedure-name is required. A period may be missing", kb/Work PB412); one that adds to it
    /// does not (the OCCURS KEY phrase after INDEXED BY, hint 8, completes the closed-format refusal of 0g).</para>
    /// </summary>
    private readonly record struct DiagnosticHint(Diagnostics.DiagnosticDescriptor Descriptor, string Message, int Priority)
    {
        public string Code => Descriptor.Code;

        public bool IsStructural => Priority <= 0;
    }

    /// <summary>Has a structural hint named the cause (<see cref="DiagnosticHint.IsStructural"/>)?</summary>
    private static bool HasStructuralHint(List<DiagnosticHint> hints) => hints.Exists(h => h.IsStructural);

    /// <summary>The token indexes the last report covered: every token from where the parser stood through the token
    /// it named; empty before the first. The strategy is created per parse (<c>Frontend</c>), so this never crosses
    /// programs.</summary>
    private (int From, int Through) _lastReported = (-1, -2);

    /// <summary>⛔ ONE REPORT PER FAULT (kb/Work PB3832). A failed adaptive prediction reports the token where every
    /// alternative died while the parser still stands at the decision's first token, and the recovery resumes THERE:
    /// after an open <c>IF … ELSE IF … END-IF</c> the parse re-takes the END-IF and the next statement and fails at the
    /// same token again, so <c>IF LENGTH OF X = 30</c> drew the same COBOL0001 twice where the lone IF draws it once.
    /// A report carried forward from where the parser stood to the token where the closed construct's own parse dies
    /// covers the tokens between the same way (the recovery resumes among them). One fault is one diagnostic; a later
    /// report at a token the last one covered is that fault re-met by the recovery, never a new one.</summary>
    private bool Reported(OffendingPosition at)
        => at.Token.TokenIndex >= _lastReported.From && at.Token.TokenIndex <= _lastReported.Through;

    /// <summary>The one report path every Report* override takes: the one-report-per-fault rule, the hints, the position
    /// the report names, the message. A STRUCTURAL diagnosis of the token where the parser stands (a hint of priority 0
    /// or less, the convention every guess arm below already reads) is the report; only where the hints there are
    /// guesses or none is the report carried forward into a construct the prediction closed early
    /// (<see cref="OffendingPosition.Carried"/>, kb/Work PB3832), and then worded as the failed prediction the same
    /// failure draws inside an open construct, so a fault draws one message at every depth. Every report kind reads
    /// its hints off the same position (<see cref="OffendingPosition.Expected"/>, <see cref="OffendingPosition.OpenRules"/>),
    /// never off an expected set only some kinds carry: a report the one-report rule suppresses can say nothing the
    /// report it repeats could not (kb/Work PB3832, review R2-1).</summary>
    private void Report(Parser recognizer, IToken token, RecognitionException? e, ErrorKind kind)
    {
        var at = new OffendingPosition(recognizer, token, IsPredictionFailedAt(e, token));
        if (Reported(at)) return;
        var hints = GuessCobolIntent(recognizer, at);
        if (!HasStructuralHint(hints) && at.Carried is { } carried)
        {
            if (Reported(carried)) return;
            (at, kind, hints) = (carried, ErrorKind.NoViableAlternative, GuessCobolIntent(recognizer, carried));
        }
        _lastReported = (Math.Min(at.ParserTokenIndex, at.Token.TokenIndex), at.Token.TokenIndex);
        _orphansItsTail = OrphansItsTail(hints, kind);
        recognizer.NotifyErrorListeners(at.Token, BuildMessage(at.Token, kind, hints), e);
    }

    /// <summary>Did the last report name a fault whose tail the recovery cannot place (<see cref="OrphansItsTail"/>)?
    /// The recovery that follows it discards the rest of the sentence or entry (<see cref="ConsumeUntil"/>).</summary>
    private bool _orphansItsTail;

    /// <summary>⛔ THE FAULTS WHOSE TAIL THE PARSER CANNOT PLACE (kb/Work PB3843, PB4173): the rest of the unit after
    /// them belongs to a construct the parse has already left, and re-parsed as a unit of its own it draws a false
    /// second diagnostic. Two kinds. A construct the parse CLOSED EARLY (<see cref="EndsAConstructEarly"/>). And an
    /// operand REFUSED where the parse stood inside its phrase (hint 0i, COBOL5506: <c>OCCURS "A" TIMES</c>,
    /// <c>PERFORM "A" TIMES … END-PERFORM</c>, <c>RECORD CONTAINS "A" CHARACTERS</c>): the rule that failed returns, and
    /// its phrase's remaining words (TIMES, CHARACTERS) are met by the enclosing list as a clause or a statement of
    /// their own, with a false COBOL1941/COBOL1970/COBOL0306 for each. A MISSING token is no such fault: the recovery
    /// conjures it and the rule goes on, so there is no tail to place.</summary>
    private static bool OrphansItsTail(List<DiagnosticHint> hints, ErrorKind kind)
        => EndsAConstructEarly(hints)
           || (kind != ErrorKind.MissingToken && hints.Count > 0
               && hints.MinBy(h => h.Priority).Code == Diagnostics.DiagnosticDescriptors.COBOL5506.Code);

    /// <summary>⛔ THE FAULTS THAT FIND THE ENCLOSING STATEMENTS ALREADY CLOSED (kb/Work PB3843): a word the statement
    /// just written does not print there (hint 0h, COBOL2269), a WHEN OTHER phrase repeated or out of place (hint 0b,
    /// COBOL2073: the EVALUATE or PERFORM has taken its one) and an explicit scope terminator no open statement owns
    /// (hint 13, COBOL0306). All are found AFTER the parser has ended the statements around the word, because an
    /// optional END-x (and the lack of a phrase) lets every rule end where its next token is not its own and defers the
    /// refusal to the sentence's period. The code that wins the message is the test, as <see cref="BuildMessage"/>
    /// picks it.</summary>
    private static bool EndsAConstructEarly(List<DiagnosticHint> hints)
        => hints.Count > 0 && hints.MinBy(h => h.Priority).Code
               is var code && (code == Diagnostics.DiagnosticDescriptors.COBOL2269.Code
                               || code == Diagnostics.DiagnosticDescriptors.COBOL2073.Code
                               || code == Diagnostics.DiagnosticDescriptors.COBOL0306.Code);

    protected override void ReportNoViableAlternative(Parser recognizer, NoViableAltException e)
        => Report(recognizer, e.OffendingToken ?? recognizer.CurrentToken, e, ErrorKind.NoViableAlternative);

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
        _discardedUnitTail = false;   // a loop recovery (Sync) discards without a Recover to finish it: never carry its mark over
        if (e is NoViableAltException { OffendingToken: { } word } && IsPredictionFailedAt(e, word)
            && ((ITokenStream)recognizer.InputStream).LT(1).TokenIndex == word.TokenIndex
            && ClosedFormatResidue(recognizer, new OffendingPosition(recognizer, word), predictionFailedAt: true) is { } residue)
        {
            recognizer.Consume();
            ConsumeUntil(recognizer, GetErrorRecoverySet(recognizer).Subtract(ResidueRunTokens(residue.Production)));
        }
        base.Recover(recognizer, e);
        FinishUnitTailDiscard(recognizer);
    }

    /// <summary>⛔ A FAULT'S TAIL STAYS WITH THE UNIT THE FAULT IS IN (kb/Work PB3843, PB4173). When the report names a
    /// fault whose tail the parser cannot place (<see cref="OrphansItsTail"/>: a construct closed early, an operand
    /// refused inside its phrase), the tokens that follow are the TAIL of the construct that failed: the rest of the
    /// phrase, the phrases of the same statement that would follow it (a NOT ON EXCEPTION), the statements still inside
    /// the construct, its explicit terminator (<c>IF c MOVE 1 TO N END-ADD MOVE 2 TO N END-IF.</c>, a second
    /// <c>ON OVERFLOW … END-STRING</c>, <c>PERFORM "A" TIMES … END-PERFORM</c>), the words after a refused
    /// operand (<c>OCCURS "A" TIMES</c>). ANTLR's recovery resumes at the first token the enclosing list or loop can take
    /// and parses that tail as a unit of its own, so its END-IF met no IF, its NOT phrase no statement, its TIMES no
    /// clause, and the one fault drew a second, false diagnostic naming the recovery's own damage. A sentence, and every
    /// entry and paragraph that ends at a separator period, is the unit of COBOL's syntax that a fault inside it cannot
    /// cut, so the resynchronization of such a fault stops only at the unit's period
    /// (<see cref="UnitTailDiscard"/>), and the next unit is parsed afresh. It is not a missing period: hint 0h is
    /// refused where a period lets the sentence that starts at the token follow
    /// (<see cref="OffendingPosition.APeriodLetsTheSentenceFollow"/>), and a scope terminator never begins a sentence.
    /// And it stops EARLIER where the unit's period is missing and the structure around it resumes
    /// (<see cref="UnitTailDiscard.Resumes"/>, kb/Work PB4134). This is the ONE place the resynchronization is bent, for
    /// both recoveries that reach <see cref="ConsumeUntil"/>: the loop recovery of <see cref="Sync"/> (the sentence's own
    /// <c>statement+</c> loop meets the stray word) and <see cref="Recover"/> (a rule whose match failed). Outside a unit
    /// that ends at a period the set is the caller's, and stays so.</summary>
    private UnitTailDiscard? BeginUnitTailDiscard(Parser recognizer)
    {
        int la = ((ITokenStream)recognizer.InputStream).LT(1).TokenIndex;
        bool discard = _orphansItsTail && la >= _lastReported.From && la <= _lastReported.Through;
        _orphansItsTail = false;   // this fault's recovery: a later fault decides afresh
        var tail = discard ? UnitTailDiscard.Of(recognizer) : null;
        _discardedUnitTail |= tail is not null;
        return tail;
    }

    /// <summary>The recovery that discarded a unit's tail stands at the unit's period; when the rule that recovered is
    /// the unit itself (<see cref="EndsWithPeriod"/>: the sentence, a data description entry) its match of the period
    /// has failed, so the period is consumed here and the list goes on with the next unit. An inner rule that
    /// recovered leaves the period for the unit.</summary>
    private void FinishUnitTailDiscard(Parser recognizer)
    {
        if (!_discardedUnitTail) return;
        _discardedUnitTail = false;
        if (EndsWithPeriod(recognizer.Context.RuleIndex) && ((ITokenStream)recognizer.InputStream).LA(1) == CobolLexer.DOT)
            recognizer.Consume();
    }

    private bool _discardedUnitTail;

    private static readonly IntervalSet UnitEnd = IntervalSet.Of(CobolLexer.DOT);

    /// <summary>Does the rule end at a separator period: some path through it takes a DOT as its last token (the
    /// sentence, <c>statement+ DOT</c>; an entry)? Read from the ATN once per rule, so a unit the grammar adds
    /// tomorrow is one.</summary>
    private static bool EndsWithPeriod(int rule)
        => EndsWithPeriodRules.GetOrAdd(rule, static r => EndsWithPeriodIn(CobolParserCore._ATN, r));

    private static readonly ConcurrentDictionary<int, bool> EndsWithPeriodRules = new();

    /// <summary>Every rule that ends at a separator period (<see cref="EndsWithPeriod"/>), by name: the units whose tail a
    /// discarded fault leaves at the period, the list the drift test of the recovery reads
    /// (<c>ParseRecoveryUnitTailTests</c>, kb/Work PB4173).</summary>
    internal static IReadOnlyList<string> PeriodTerminatedUnits(Parser recognizer)
        => [.. Enumerable.Range(0, recognizer.RuleNames.Length).Where(EndsWithPeriod).Select(r => recognizer.RuleNames[r])];

    private static bool EndsWithPeriodIn(ATN atn, int rule)
    {
        var stop = atn.ruleToStopState[rule];
        foreach (var state in atn.states.Where(s => s is not null && s.ruleIndex == rule))
            for (int i = 0; i < state.NumberOfTransitions; i++)
                if (state.Transition(i) is { IsEpsilon: false } t and not NotSetTransition
                    && t.Label is { Count: 1 } label && label.Contains(CobolLexer.DOT) && ReachesEnd(t.target, stop))
                    return true;
        return false;
    }

    /// <summary>Does <paramref name="from"/> reach <paramref name="stop"/> over epsilon edges alone, without calling a
    /// rule?</summary>
    private static bool ReachesEnd(ATNState from, ATNState stop)
    {
        var seen = new HashSet<int>();
        var work = new Stack<ATNState>([from]);
        while (work.TryPop(out var state))
        {
            if (state == stop) return true;
            if (!seen.Add(state.stateNumber)) continue;
            for (int i = 0; i < state.NumberOfTransitions; i++)
                if (state.Transition(i) is { IsEpsilon: true } and not RuleTransition)
                    work.Push(state.Transition(i).target);
        }
        return false;
    }

    /// <summary>Every token a unit of rule <paramref name="rule"/> may hold anywhere in it: the labels of the token edges
    /// of the rule and of every rule it calls, transitively. A token outside it cannot be part of the unit's tail.</summary>
    private static IntervalSet TokensWithin(int rule)
        => TokensWithinRules.GetOrAdd(rule, static r => TokensWithinIn(CobolParserCore._ATN, r));

    private static readonly ConcurrentDictionary<int, IntervalSet> TokensWithinRules = new();

    private static IntervalSet TokensWithinIn(ATN atn, int rule)
    {
        var tokens = new IntervalSet();
        var seen = new HashSet<int>();
        var work = new Stack<int>([rule]);
        while (work.TryPop(out int r))
        {
            if (!seen.Add(r)) continue;
            foreach (var state in atn.states.Where(s => s is not null && s.ruleIndex == r))
                for (int i = 0; i < state.NumberOfTransitions; i++)
                    if (state.Transition(i) is RuleTransition call) work.Push(call.target.ruleIndex);
                    else if (state.Transition(i) is { IsEpsilon: false } t and not NotSetTransition && t.Label is { } label)
                        tokens.AddAll(label);
        }
        tokens.SetReadonly(true);
        return tokens;
    }

    /// <summary>⛔ THE DISCARD OF A UNIT'S TAIL ENDS AT ITS PERIOD, OR WHERE THE STRUCTURE AROUND THE UNIT RESUMES
    /// (kb/Work PB3843, PB4134). The tail of a faulted sentence or entry belongs to the unit up to its separator period,
    /// but a unit may lack its period, and the tokens after it then are not its tail: <c>END PROGRAM A.</c>, the next
    /// program's <c>IDENTIFICATION DIVISION.</c>, <c>END DECLARATIVES.</c>, the next section's header. Discarded up to the
    /// first period they were eaten with the fault's tail, the program was left without its end, and a true fault after it,
    /// or the structure's own refusal, was lost or drew a false diagnostic. So the discard also stops at a position
    /// where some rule AROUND the unit, completed, takes the next two tokens: the walk of the parse
    /// (<see cref="ResimulationWalk"/>) from the end of each enclosing rule, in its real context, the ATN's own follow of
    /// the unit and not a list of words. Two tokens, because <c>END</c> alone is also the AT END phrase of a READ.
    /// <para>A word and the period after it is never a resumption: the unit's own last operand then its period, or a
    /// paragraph header, and no layout rule of the parser tells them apart (a missing period before a paragraph header
    /// costs the header, as before). It is a resumption where the unit itself cannot hold the word at all
    /// (<c>PROGRAM-ID.</c> of a nested program).</para></summary>
    private sealed class UnitTailDiscard
    {
        private readonly Parser _recognizer;
        private readonly int _unitRule;
        private readonly List<(ParserRuleContext Container, IntervalSet Follow)> _containers = [];
        private readonly Dictionary<ParserRuleContext, ResimulationWalk> _walks = [];

        private UnitTailDiscard(Parser recognizer, ParserRuleContext unit)
        {
            (_recognizer, _unitRule) = (recognizer, unit.RuleIndex);
            var atn = recognizer.Atn;
            for (var c = unit.Parent as ParserRuleContext; c is not null; c = c.Parent as ParserRuleContext)
            {
                var follow = atn.NextTokens(atn.ruleToStopState[c.RuleIndex], c);
                if (follow.Count > 0) _containers.Add((c, follow));
            }
        }

        /// <summary>The discard of the innermost unit that ends at a period around the parser's position, or null
        /// outside every such unit.</summary>
        public static UnitTailDiscard? Of(Parser recognizer)
        {
            for (var c = recognizer.Context; c is not null; c = c.Parent as ParserRuleContext)
                if (EndsWithPeriod(c.RuleIndex)) return new(recognizer, c);
            return null;
        }

        /// <summary>Does the structure around the unit resume at the next token: do the next two tokens (the next only,
        /// at the end of the file) continue some enclosing rule completed, and is the word before a period not the
        /// unit's own?</summary>
        public bool Resumes(ITokenStream input)
        {
            int first = input.LA(1), second = input.LA(2);
            if (first is TokenConstants.EOF or CobolLexer.DOT) return false;
            if (second == CobolLexer.DOT && TokensWithin(_unitRule).Contains(first)) return false;
            foreach (var (container, follow) in _containers)
            {
                if (!follow.Contains(first)) continue;
                if (!_walks.TryGetValue(container, out var walk)) _walks[container] = walk = ResimulationWalk.After(_recognizer, container);
                if (second == TokenConstants.EOF ? walk.Takes(first) : walk.TakesAll([first, second])) return true;
            }
            return false;
        }
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
        => Report(recognizer, e.OffendingToken ?? recognizer.CurrentToken, e, ErrorKind.InputMismatch);

    protected override void ReportMissingToken(Parser recognizer)
    {
        if (errorRecoveryMode) return;
        BeginErrorCondition(recognizer);
        Report(recognizer, recognizer.CurrentToken, null, ErrorKind.MissingToken);
    }

    protected override void ReportUnwantedToken(Parser recognizer)
    {
        if (errorRecoveryMode) return;
        BeginErrorCondition(recognizer);
        Report(recognizer, recognizer.CurrentToken, null, ErrorKind.UnwantedToken);
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
        var discard = BeginUnitTailDiscard(recognizer);
        if (discard is not null) set = UnitEnd;
        var input = (ITokenStream)recognizer.InputStream;
        // A reserved word the report named as an operand is consumed before the resynchronization asks the set (PB3248).
        // A unit-tail discard's set is the period alone, which is never such a word: the discard consumes the word
        // with the rest of the tail (or stops at it where the structure around the unit resumes there), so the two
        // never decide the same token.
        if (set.Contains(input.LA(1)) && IsReportedReservedOperand(recognizer, input.LT(1))) recognizer.Consume();
        for (int ttype = input.LA(1); ttype != TokenConstants.EOF && !set.Contains(ttype) && discard?.Resumes(input) != true;
             ttype = input.LA(1))
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

    /// <summary>⛔ A REFERENCE REFUSED AS A RESERVED WORD IS CONSUMED AS THE NAME IT WAS WRITTEN AS (kb/Work PB3248). A
    /// syntax error on a reservation-gated word §8.9 reserves at this edition is reported as that word's §8.9 violation
    /// (COBOL0901, <see cref="CobolParserCoreBase.ReservedUserWordViolation"/>), and ISO §8.3.2.1 1) ("Reserved words
    /// shall not be used as user-defined words") is broken once, by that occurrence. A verb such as RECEIVE or SEND
    /// begins a statement, so it is in every statement-level recovery set, and the base recovery resumed AT it:
    /// <c>DISPLAY RECEIVE.</c> then parsed RECEIVE as a RECEIVE statement and refused the period, a second diagnostic
    /// naming the recovery's own reading of a word the first had already called a name. So such a word is consumed
    /// first, as the operand it stood for, and the resynchronization goes on from the token after it.
    /// <para>⛔ BUT THE SAME REPORT FIRES AT A VERB THAT CORRECTLY BEGINS THE NEXT STATEMENT (review R1-1). After an
    /// incomplete statement (<c>MOVE W-X TO</c>, its receiver missing) the error token is the next statement's verb,
    /// and the listener re-codes it to COBOL0901 just the same. Consuming it there resumed inside that correct
    /// statement's operands (<c>RECEIVE FROM W-X GIVING W-Y W-N</c> drew "unexpected 'GIVING'"; <c>RESUME AT NEXT
    /// STATEMENT</c> was told to write SENTENCE). The two are told apart by the grammar, never by a list: the word is
    /// the operand the report named exactly when the statement it would begin is NOT a complete reading
    /// (<see cref="CobolParserCoreBase.LookaheadBeginsAStatementThatParses"/>); where it is, the recovery resumes
    /// at it and the statement is read as written. A statement that parses only as a prefix, stopping at a token no
    /// statement is followed by (<c>GOBACK</c> in <c>MOVE GOBACK TO W-X</c>, review R2-1), is an operand.</para>
    /// Only the token the last report named (<see cref="_lastReported"/>), only where the listener re-codes it to
    /// COBOL0901, and only when it is a resynchronization point at all (the caller asks the set first, so the speculative
    /// parse runs only where its answer decides something): every other refused token keeps the base recovery.</summary>
    private bool IsReportedReservedOperand(Parser recognizer, IToken token)
        => token.TokenIndex == _lastReported.Through && recognizer is CobolParserCoreBase parser
           && parser.ReservedUserWordViolation(token) is not null
           && !parser.LookaheadBeginsAStatementThatParses();

    /// <summary><para>The other half of the same rule: single-token deletion never deletes a '(' (kb/Work PB2152). Deleting
    /// one paren of a balanced pair (§8.3.5 4) orphans the other, so <c>DISPLAY (W-S).</c> recovered by dropping the
    /// '(' and then reported its ')' as a second error. Declining here sends the error to <see cref="ConsumeUntil"/>,
    /// which consumes the group whole.</para>
    /// <para>⛔ AND NEVER THE TOKEN THAT ENDS AN EMPTY REQUIRED BLOCK (kb/Work PB911). Where one statement written here
    /// would let the token follow (<see cref="OffendingPosition.EmptyBlockOwner"/>), the token is the OWNER's own
    /// terminator or next phrase (<c>END-PERFORM</c>, a <c>WHEN</c>), not a stray word: the block is what is missing.
    /// Deleting it because the token after it can start a statement turned that next statement into the body, and the
    /// PERFORM, whose END-PERFORM the deletion had just consumed, drew a second, false COBOL0001 at the next
    /// sentence's period (<c>PERFORM UNTIL X = 0 END-PERFORM STOP RUN.</c>) — the IF, EVALUATE and SEARCH, whose
    /// terminators are optional, hid it. Declining leaves the token for the owner to match once the block's missing
    /// statement is reported.</para>
    /// <para>⛔ AND NEVER A TOKEN THE MISSING ELEMENT WOULD LET FOLLOW (kb/Work PB4133), the same finding for every other
    /// element. ANTLR deletes the token when the one after it is what the rule expects, so a token that merely sits
    /// where an element is required reads as stray. When it is what may follow that element
    /// (<see cref="TheMissingElementLetsItFollow"/>), the element is what is missing, and deleting the token repairs the
    /// wrong thing: the token after it takes the element's place and the parse goes on from something the user did not
    /// write. <c>IF DISPLAY "X" END-IF</c> lost the DISPLAY, took <c>"X"</c> for the condition and left the IF with the
    /// empty block of a false COBOL2072; <c>MERGE SW ON ASCENDING KEY USING F1 F2 GIVING F3</c> took F1 for the key and
    /// met the GIVING where USING was due. Declining leaves the token for the rule that follows the element, once the
    /// missing element is reported.</para></summary>
    protected override IToken? SingleTokenDeletion(Parser recognizer)
    {
        var stream = (ITokenStream)recognizer.InputStream;
        if (IsOpenParen(stream.LA(1)) || TheMissingElementLetsItFollow(recognizer)) return null;
        if (new OffendingPosition(recognizer, recognizer.CurrentToken).EmptyBlockOwner() is not null) return null;
        return base.SingleTokenDeletion(recognizer);
    }

    /// <summary>Is the current token one that may follow the element the parser stands before, in the real context? The
    /// element is, in turn: the token a matching state takes or the block a block-start state opens (the tokens that
    /// follow the state after it are ANTLR's own single-token INSERTION test, so a token insertion would take is never
    /// deleted instead), and the whole rule when it has consumed nothing yet (the follow of the rule: the condition of
    /// <c>IF DISPLAY "X"</c> is missing though the parser stands at the first, optional, sign of an operand).</summary>
    private static bool TheMissingElementLetsItFollow(Parser recognizer)
    {
        var atn = recognizer.Atn;
        var state = atn.states[recognizer.State];
        var current = ((ITokenStream)recognizer.InputStream).LT(1);
        var after = state switch
        {
            BlockStartState block => block.endState,
            { NumberOfTransitions: > 0 } when !state.Transition(0).IsEpsilon => state.Transition(0).target,
            _ => null,
        };
        if (after is not null && atn.NextTokens(after, recognizer.Context).Contains(current.Type)) return true;
        return recognizer.Context.Start?.TokenIndex == current.TokenIndex
               && atn.NextTokens(atn.ruleToStopState[recognizer.Context.RuleIndex], recognizer.Context).Contains(current.Type);
    }

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
               && ClosedFormatResidue(recognizer, new OffendingPosition(recognizer, token), predictionFailedAt: false) is not null;
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

    private static string BuildMessage(IToken token, ErrorKind kind, List<DiagnosticHint> hints)
    {
        string baseMsg = kind switch
        {
            ErrorKind.NoViableAlternative => $"cannot parse construct near '{Truncate(token.Text, 40)}'",
            ErrorKind.InputMismatch => $"unexpected '{Truncate(token.Text, 40)}'",
            ErrorKind.MissingToken => $"missing token before '{Truncate(token.Text, 40)}'",
            ErrorKind.UnwantedToken => $"unexpected '{Truncate(token.Text, 40)}'",
            _ => "syntax error"
        };

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
        Parser recognizer, OffendingPosition at)
    {
        var hints = new List<DiagnosticHint>();
        var token = at.Token;
        bool predictionFailedAt = at.PredictionFailedAt;
        var stream = (ITokenStream)recognizer.InputStream;
        var prev = GetToken(stream, token.TokenIndex - 1);
        // ⛔ The expected set AT the offending token, for every report kind (kb/Work PB3832, review R2-1). It used to be
        // the text of the exception's set, which a failed prediction never carries, so every guess below that reads it
        // was silent on the FIRST report of a fault and spoke only on the recovery's repeat at the same token, which the
        // one-report rule now drops: a missing separator period drew a bare COBOL0001. Each of those guesses says the
        // token is refused here and something else was wanted, so the set is read only where it does not hold the token
        // (a failed prediction's walk may take the token and die further on; the parser's own report never holds it).
        var expected = at.Expected() is { } atToken && !atToken.Contains(token.Type) ? atToken : null;
        // ⛔ The rules open AT the offending token, never the parser's stack at a rewound decision (kb/Work PB3832);
        // null when the walk cannot reach the token, and then no arm claims a construct is open or missing.
        var ruleStack = at.OpenRules();

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
        // cannot match empty. So the violation always arrives in the SAME shape: at the offending token a statement
        // block has begun and holds nothing, and ONE statement written there would let the token follow it. That
        // test is computed FROM THE ATN, not from a table of verbs, so a general format
        // added tomorrow inherits the diagnostic without editing this method — which is the whole reason the
        // cardinality lives in the grammar rather than in nine bind-time checks. ⛔ The emptiness is asked of the parse
        // AT the offending token, never of the parser's position (kb/Work PB3832; OffendingPosition).
        if (at.EmptyBlockOwner() is { } stmtName)
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
            // A body that is present says the phrase is misplaced only where no parse can take the phrase here: when the
            // construct the prediction closed takes it, and its parse dies later (`WHEN OTHER IF LENGTH OF X`), the
            // phrase is in place and the fault is where that parse dies (kb/Work PB3832; OffendingPosition.Carried).
            if (!(bodyPresent && at.Carried is not null))
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
        if (FigurativeSpellingMessage(recognizer, token, at) is { } spellingMsg)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL2418, spellingMsg, -1));   // outranks 0a: the
            // misspelled keyword is the cause wherever the parse gives up. (The sign-condition shape fails at an IS / NOT
            // the enclosing statement cannot continue with; 0a asked the parser's position and read that as an empty
            // imperative-statement until kb/Work PB3832 made it ask whether one statement would let the token follow.)
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
        if (ClosedFormatResidue(recognizer, at, predictionFailedAt) is { Format: var closedFormat })
            hints.Add(new(new Diagnostics.DiagnosticDescriptor(closedFormat.Diagnostic.Code,
                    Diagnostics.DiagnosticSeverity.Error, "{0}"),   // the catalogue code, single-sourced as every twin's is
                closedFormat.Refusal(WrittenResidueItem(recognizer, token), ClosedFormats.OwnerName(recognizer.Context)) + ".", 0));

        if (token.Type is CobolLexer.CORRESPONDING or CobolLexer.CORR && IsInRule(ruleStack, "moveStatement"))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL2173,
                "Both general formats of the MOVE statement put the whole sending specification directly after "
                + "the verb — Format 1 is 'MOVE { identifier-1 | literal-1 } TO { identifier-2 } …' and Format 2 "
                + "is 'MOVE { CORRESPONDING | CORR } identifier-3 TO identifier-4' — so no format admits a "
                + "CORRESPONDING phrase after a sending operand (ISO §14.9.25.2).", 0));

        // ⛔ 0h. A WORD THE STATEMENT JUST WRITTEN DOES NOT PRINT HERE (kb/Work PB906). A statement the parser has ended
        // and a word that no statement, phrase or entry can take after it (not a scope terminator, which hint 13
        // names with the statement it lacks) are a statement written with a phrase its general formats do not print
        // there: an unprinted phrase (`WRITE R IGNORING LOCK`), a second alternative of a bracket that admits one
        // (`READ … ADVANCING ON LOCK RETRY 3 TIMES`), a phrase out of its sequence (`READ … KEY IS K WITH NO LOCK`),
        // an unprinted second occurrence (`WRITE … BEFORE ADVANCING 4 LINES AFTER ADVANCING 0`). The parser reports the
        // word because the statement ended and the sentence cannot go on, and hint 14 used to read that as a missing
        // period — a guess about punctuation that sent the reader to the previous sentence — wherever the word could
        // also be a paragraph-name. It is a STRUCTURAL answer, read from the statement the parser completed
        // (OffendingPosition.StatementThatEndedBefore, whose re-simulation over every path shows the statement itself
        // cannot take the word, and which names the statement whose own syntax prints the word, the IF of a second
        // ELSE) and from the fact that no period lets the sentence that starts at the word follow
        // (OffendingPosition.APeriodLetsTheSentenceFollow), so the sentence is never a missing period's.
        if (!HasStructuralHint(hints) && expected is not null && ScopeTerminator(recognizer, token.Type) is null
            && !at.APeriodLetsTheSentenceFollow() && at.StatementThatEndedBefore() is { } endedStatement)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL2269,
                $"The {endedStatement} statement ends before this token: no general format of {endedStatement} "
                + "prints it after what is written, and no missing period would let it begin the next sentence. The "
                + "words, phrases, clauses, punctuation, and operands in each general format shall be written in the "
                + "compilation group in the sequence given in the general format (ISO §5.2.1).", 0));

        // ⛔ 0i. THE KIND WRITTEN IS NOT A KIND THE POSITION EXPECTS (kb/Work PB3428, PB3861). Read from the parse AT the
        // token with every predicate on the way evaluated (OffendingPosition.Admitted), so a context word a text
        // predicate refused (`MEMORY SIZE 12 FOO`: the unit is CHARACTERS, MODULES or WORDS) is named with the words
        // expected, a missing operand (`SEGMENT-LIMIT IS .`) with the literal expected, and a literal of the wrong
        // category (`SET W-N TO "A"`) with the categories expected. AdmittedKinds says which positions can be named and
        // how. It replaces the two guesses that named a kind in two shapes only, "a data-name is expected
        // here, not a literal" (COBOL0308, whose silence where some literal was expected left `SET W-N TO "A"` the bare
        // COBOL0001, kb/Work PB906) and its converse (COBOL0309). After 0h, as a later structural diagnosis: a statement
        // that ended before the token, a closed format's residue and an empty block name the fault better; a scope
        // terminator is hint 13's.
        if (!HasStructuralHint(hints) && ScopeTerminator(recognizer, token.Type) is null && at.Admitted() is { } admitted
            && AdmittedKinds.Refusal(token, admitted.Tokens, admitted.Words, StatementBlockStart(recognizer),
                   at.APeriodLetsTheSentenceFollow,
                   // The words the compile edition has (kb/Work PB4174): the parse is the superset of the editions.
                   recognizer is CobolParserCoreBase editionParser ? editionParser.PrintsKeyword : _ => true) is { } kindMsg)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL5506, kindMsg, 0));

        // ⛔ NO "MISSING SPACE BEFORE / AFTER STRING LITERAL" GUESS HERE (kb/Work PB1394). A literal delimiter that is not
        // separated is §8.3.5 rule 5's violation, and SeparatorRule reports it — for every literal token, whether or
        // not the parse happens to fail there — as COBOL2633. The two recovery hints that stood here (COBOL0301 /
        // COBOL0302) stated that rule a second time, only on a parse error, and only for a quotation-mark delimiter.

        // 3. Missing TO in MOVE statement — a GUESS, suppressed by a structural diagnosis as 7 is: where TO alone may
        // follow the sending operand, 0i already names it (kb/Work PB3428).
        if (IsInRule(ruleStack, "moveStatement") && IsIdentifier(token) && prev != null && IsLiteral(prev)
            && !HasStructuralHint(hints))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0303, "In a MOVE statement, did you forget TO before the target?", 10));

        // ⛔ NO "MISSING PERIOD AFTER PARAGRAPH NAME" GUESS KEYED ON A TOKEN'S TEXT (kb/Work PB2770, review R1-1). The
        // COBOL0304 hint that stood here fired when a token's TEXT contained ".MOVE", ".ADD", ".SUBTRACT", ".PERFORM" or
        // ".IF". No COBOL word can carry a period (ISO §8.3.2.1 limits its characters to letters, digits, hyphen and
        // underscore, and NAME_BODY lexes exactly that), so `MAIN-PARA.MOVE` is three tokens and the separator rule
        // reports the unspaced period (COBOL2632); the only tokens whose text could match were literals and
        // PICTURE strings, where `GO TO "PARA.PERFORM".` answered "the parser is treating it as a qualified
        // reference" — false twice over. The hint is deleted.

        // 5. STATUS where IDENTIFIER expected
        if (token.Type == CobolLexer.STATUS && expected?.Contains(CobolLexer.IDENTIFIER) == true)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0200, "STATUS is a reserved word here. For file status, use 'FILE STATUS IS <data-name>'.", 10));

        // 6. PROGRAM where IDENTIFIER expected
        if (token.Type == CobolLexer.PROGRAM && expected?.Contains(CobolLexer.IDENTIFIER) == true)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0201, "PROGRAM is a reserved word. If this is a paragraph name, it cannot be named PROGRAM.", 10));

        // 7. Unrecognized keyword in SPECIAL-NAMES — a GUESS, suppressed once a structural diagnosis has named the
        // rule (priority 0): 0g names the closed format a stray reserved word violates, and this sentence would only
        // restate it vaguely (kb/Work PB2533).
        if (IsInRule(ruleStack, "specialNamesParagraph") && !IsIdentifier(token) && !IsLiteral(token)
            && !HasStructuralHint(hints))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0305, "Unexpected token in SPECIAL-NAMES. Check implementor-name or mnemonic-name syntax.", 15));

        // 8. ASCENDING/DESCENDING KEY where the entry can only end (kb/Work PB1266). KEY is supported; what this
        // catches is its POSITION: §13.18.38.2 writes the OCCURS clause's KEY phrases BEFORE INDEXED BY, so a KEY phrase
        // after it finds the entry ready to end. The hint used to claim the clause was "not yet supported" and that a
        // table had been created without a key — false on both counts.
        if (token.Type is CobolLexer.ASCENDING or CobolLexer.DESCENDING && expected?.Contains(CobolLexer.DOT) == true)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0100, "ASCENDING/DESCENDING KEY is out of place: in an OCCURS clause the KEY phrases are written before INDEXED BY (ISO §13.18.38.2).", 5));

        // ⛔ NO "MAY NOT BE SUPPORTED" GUESS KEYED ON A WORD'S SPELLING (kb/Work PB2770). The hints that stood here
        // (COBOL0101 BLANK WHEN ZERO "may not be recognized", COBOL0102 "this SET form may not be supported",
        // COBOL0103 "SEARCH statement may not be fully supported", and COBOL0107 "EVALUATE with ALSO may not be fully
        // supported" below) fired on the WORD, wherever the parse happened to fail at it, and each stated a
        // limitation the compiler does not have: every one of those constructs is implemented, so the claim sent the
        // user after a cause that does not exist (`MOVE 1 SET X TO 2` lacks MOVE's TO). They are deleted, as DEVLOG
        // 232 deleted COBOL0104-0106 and COBOL0311; a structural hint above (0a-0f) reads the ATN or the parser state.

        // 12. THROUGH/THRU in unexpected context
        if (token.Type is CobolLexer.THROUGH or CobolLexer.THRU && expected?.Contains(CobolLexer.DOT) == true)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0300, "THROUGH/THRU is not recognized in this context. Check PERFORM or VALUE THROUGH syntax.", 10));

        // 13. A misplaced explicit scope terminator (ISO §14.5.3.2), keyed on the token TYPE and on the statement
        // rules the ATN says can match it (kb/Work PB2770), never on the word's spelling: END-SEARCH closes both SEARCH
        // rules, and a word that closes no statement (END-OF-PAGE, a user word spelled END-…) is no terminator at all.
        var terminator = ScopeTerminator(recognizer, token.Type);
        if (terminator is not null && ruleStack is not null && !ruleStack.Any(terminator.Statements.Contains))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0306,
                $"{terminator.Word} appears without a matching {terminator.StatementName} statement.", 5));

        // 14. Missing period at end of sentence — a GUESS, and the lowest-priority one here, so it is suppressed
        // once a STRUCTURAL diagnosis has already named what the general format requires at this position
        // (priority 0). Otherwise `GO TO DEPENDING ON X.` read "…at least one procedure-name is required. A
        // period may be missing at the end of the previous sentence." — the second sentence contradicting the
        // first (kb/Work PB412). A word SPELLED like a scope terminator is exempt whether or not it is one: whoever
        // wrote END-INVOKE (no ISO word, kb/Work PB758) or END-OF-PAGE at a sentence end meant to close something,
        // and a missing period is not what went wrong. Here the spelling is the evidence, so the text is the test.
        // ⛔ And a period must be able to let the SENTENCE that starts at the token follow
        // (OffendingPosition.APeriodLetsTheSentenceFollow, kb/Work PB906, PB3832 review R2-1): a period being expected
        // here is not that. `05 A(1)` expects one after A, and no period lets '(' follow; `WRITE R IGNORING LOCK.`
        // lets the word IGNORING follow a period (a paragraph-name may be spelled so), and no period lets the LOCK after
        // it follow it. A guess about punctuation is false when the written words are a phrase the statement before
        // them does not print; hint 0h names that.
        if (expected?.Contains(CobolLexer.DOT) == true
            && !(token.Text is { } spelled && CobolNames.StartsWith(spelled, "END-"))
            && !HasStructuralHint(hints)
            && at.APeriodLetsTheSentenceFollow())
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0307, "A period may be missing at the end of the previous sentence.", 25));

        // ⛔ NO "A DATA-NAME IS EXPECTED HERE" / "A LITERAL VALUE IS EXPECTED HERE" GUESS (kb/Work PB3861). Hints 15
        // (COBOL0308) and 16 (COBOL0309) named the kind a position admits in two shapes only, from the predicate-blind
        // expected set; 0i names it in every shape, from what the parse admits at the token.

        // 17. Missing BY in INDEXED BY — a GUESS, suppressed by a structural diagnosis as 7 is: where BY alone may
        // follow, 0i already names it (kb/Work PB3428).
        if (token.Type != CobolLexer.BY && expected?.Contains(CobolLexer.BY) == true &&
            IsInRule(ruleStack, "dataDescriptionEntry") && !HasStructuralHint(hints))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0310, "Missing BY keyword. INDEXED BY requires 'INDEXED BY <index-name>'.", 10));

        // 19. FILE CONTROL / SELECT context errors — a GUESS, suppressed by a structural diagnosis as 7 is (kb/Work PB2533).
        if (IsInRule(ruleStack, "fileControlParagraph") &&
            !IsIdentifier(token) && !IsLiteral(token) &&
            token.Type is not (CobolLexer.SELECT or CobolLexer.ASSIGN or CobolLexer.FILE_CONTROL)
            && !HasStructuralHint(hints))
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
    /// <para><b>The position, from the ATN.</b> ANTLR's own full-context simulation (<see cref="ResimulationWalk"/>) re-runs
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
    private static Residue? ClosedFormatResidue(Parser recognizer, OffendingPosition at, bool predictionFailedAt)
    {
        var atn = recognizer.Atn;
        var token = at.Token;
        bool refusedWord = predictionFailedAt && token.Type == CobolLexer.IDENTIFIER;
        if (!(refusedWord || IsReservedWord(token) || IsLiteral(token)) || HeaderWords(atn).Contains(token.Type)) return null;
        var stream = (ITokenStream)recognizer.InputStream;
        // The residue is named only where the parse gave up near the parser: a closed construct carried forward, or a
        // prediction that failed past the bound, is not the place a clause list stands (MaxResimulatedTokens).
        if (at.CarriedForward || at.Lookahead > MaxResimulatedTokens || at.Walk is not { } walk) return null;
        int k = at.Lookahead;
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

    /// <summary>How far past the parser a closed format's residue may stand (<see cref="ClosedFormatResidue"/>,
    /// <see cref="WrittenResidueItem"/>): a clause-list prediction that failed further ahead than this is not at a
    /// position anyone would call the list's place, and the 0g hint says nothing. It bounds that reading only, never the
    /// <see cref="ResimulationWalk"/>: a dangling decision (the optional END-IF of an <c>IF … ELSE IF … END-IF</c> nest)
    /// looks ahead to the end of the sentence, and the parse at the token where it failed is read however far that is,
    /// or the empty-block test went silent past this bound (kb/Work PB3832, review R1-1). The walk costs what the failed
    /// prediction already spent over the same tokens, on the error path only.</summary>
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
    /// <para>The answer is then READ off the final configuration set: the closure keeps every configuration at a state
    /// with a token edge, each with its whole stack. <see cref="Probe"/> reads the closed-format residue there (a call of
    /// the error production is a frame of some stack, and a period in the parser's own context is a period edge whose
    /// stack is one of the parser's own, <see cref="_own"/>); <see cref="Expected"/> and <see cref="EmptyBlockOwner"/>
    /// read the position of the offending token itself for the hints that used to read the PARSER's position, which a
    /// failed prediction leaves at the decision's first token, not at the token it reports (kb/Work PB3832).</para></summary>
    private sealed class ResimulationWalk : ParserATNSimulator
    {
        /// <summary>A walk positioned just AFTER <paramref name="container"/> completes, in its real context: the
        /// parse's position at the end of an enclosing rule, where it takes what may follow that rule
        /// (<see cref="UnitTailDiscard.Resumes"/>).</summary>
        public static ResimulationWalk After(Parser recognizer, ParserRuleContext container)
            => new(recognizer, recognizer.Atn.ruleToStopState[container.RuleIndex], container);

        /// <summary>A walk re-run from the parser's state to <paramref name="token"/>, or null when no configuration
        /// takes the tokens before it (a recovery the walk cannot follow, or an SLL prediction that looked past a
        /// failure the full context would meet first).</summary>
        public static ResimulationWalk? To(Parser recognizer, IToken token)
        {
            var stream = (ITokenStream)recognizer.InputStream;
            var walk = new ResimulationWalk(recognizer, recognizer.Atn.states[recognizer.State], recognizer.Context);
            for (int lookahead = 1; ; lookahead++)
            {
                var at = stream.LT(lookahead);
                if (at.TokenIndex == token.TokenIndex) return walk;
                if (at.TokenIndex > token.TokenIndex || at.Type == TokenConstants.EOF || !walk.Consume(at.Type))
                    return null;
            }
        }

        /// <summary>⛔ WHAT THE PARSE ADMITS AT <paramref name="token"/>, EVERY PREDICATE ON THE WAY EVALUATED (kb/Work
        /// PB3428). The walks of <see cref="To"/> and <see cref="Over"/> evaluate no predicate, so a path a text predicate
        /// cuts stays open: an IDENTIFIER edge a predicate guards (<c>(CHARACTERS | {memoryUnitAhead()}?
        /// IDENTIFIER)</c>, MEMORY SIZE's unit) is in their expected set whatever word was written, and a clause whose
        /// left-edge predicate refused an earlier word goes on taking the tokens after it (<c>XYZ SEGMENT-LIMIT IS .</c>
        /// reached the period through CHARACTER CLASSIFICATION's path, which SEGMENT-LIMIT never opens, and the period
        /// seemed to admit a locale-name). This walk re-runs the same simulation from <paramref name="start"/> in
        /// <paramref name="context"/> over the tokens from <paramref name="first"/> to <paramref name="token"/> with every
        /// closure evaluating the predicates it meets AT the token it precedes, as ANTLR's full-context prediction
        /// evaluates a decision's (its <c>PredTransition</c> seeks the input to <c>startIndex</c>; a closure crosses no
        /// token, so every predicate it meets belongs to that token's position). Its expected set is what the parse
        /// admits there, and the last closure records the words its refusing predicates compared the token with
        /// (<see cref="EvalSemanticContext"/>). Null when it cannot reach the token. Error path only: the runtime restores
        /// the parser's input after each evaluation.</summary>
        public static Admitted? AdmittedAt(Parser recognizer, ATNState start, RuleContext context, IToken first, IToken token)
        {
            var stream = (ITokenStream)recognizer.InputStream;
            var walk = new ResimulationWalk(recognizer, start, context, evaluateBefore: first);
            for (var at = first; at.TokenIndex != token.TokenIndex; )
            {
                if (at.TokenIndex > token.TokenIndex || at.Type == TokenConstants.EOF) return null;
                int index = at.TokenIndex;
                if (NextDefault(stream, ref index) is not { } next) return null;
                walk.startIndex = next.TokenIndex;   // the closure after `at` precedes `next`: its predicates are next's
                if (!walk.Consume(at.Type)) return null;
                at = next;
            }
            return new Admitted(TokensOf(walk._configs), walk._closureWords ?? []);
        }

        /// <summary>A walk over <paramref name="closed"/>, a construct the parser has completed, re-run from its rule's
        /// start in its real context over its own tokens, so every path the parser's prediction did NOT take in it is
        /// open again; null when the walk cannot take them, or they cross a separator period (a construct closed by its
        /// period is no candidate: the period is where it ended).</summary>
        public static ResimulationWalk? Over(Parser recognizer, ParserRuleContext closed)
        {
            var stream = (ITokenStream)recognizer.InputStream;
            var walk = new ResimulationWalk(recognizer, recognizer.Atn.ruleToStartState[closed.RuleIndex], closed);
            for (int i = closed.Start.TokenIndex; i <= closed.Stop.TokenIndex; i++)
            {
                var t = stream.Get(i);
                if (t.Channel != Lexer.DefaultTokenChannel) continue;
                if (t.Type is CobolLexer.DOT or TokenConstants.EOF || !walk.Consume(t.Type)) return null;
            }
            return walk;
        }

        /// <summary>Can some configuration at the walk's position take a token of type <paramref name="ttype"/>?</summary>
        public bool Takes(int ttype) => _configs.Elements.Any(c => TakesToken(c, ttype));

        /// <summary>Can the parse at the walk's position take the tokens <paramref name="ttypes"/>, in order? The walk
        /// itself does not move.</summary>
        public bool TakesAll(ReadOnlySpan<int> ttypes)
        {
            var configs = _configs;
            foreach (int ttype in ttypes)
                if ((configs = Step(configs, ttype)).Count == 0) return false;
            return true;
        }

        /// <summary>Every token some configuration at the walk's position can take: the expected set AT the offending
        /// token, whatever decision the parser itself was rewound to. Predicate-blind, as the parser's own
        /// <c>GetExpectedTokens</c> is: a text-predicated IDENTIFIER edge is in it whatever word was written.</summary>
        public IntervalSet Expected() => TokensOf(_configs);

        private static IntervalSet TokensOf(ATNConfigSet configs)
        {
            var expected = new IntervalSet();
            foreach (var c in configs.Elements)
                for (int i = 0; i < c.state.NumberOfTransitions; i++)
                    if (c.state.Transition(i) is { IsEpsilon: false } t && t.Label is { } label && t is not NotSetTransition)
                        expected.AddAll(label);
            return expected;
        }

        /// <summary>Does this walk evaluate the predicates its closures meet (<see cref="AdmittedAt"/>)?</summary>
        private readonly bool _evaluatesPredicates;

        /// <summary>The words the refusing predicates of the last closure tested: the context words the format prints
        /// at the position that closure precedes. Null for a walk that evaluates no predicate.</summary>
        private SortedSet<string>? _closureWords;

        private readonly List<(string Word, bool Matched)> _tested = [];

        /// <summary>A predicate met by a closure of <see cref="AdmittedAt"/>'s walk, evaluated at the token the closure
        /// precedes through the parser base's one word funnel (<see cref="CobolParserCoreBase.EvaluateTestingWords"/>).
        /// A predicate that refuses the token with no word of it matching is a word predicate the token is not a word of,
        /// and its words are the ones the position prints there. One that refuses although a word matched (a negated
        /// predicate, <c>{!…Ahead()}?</c>, or a word whose following tokens are wrong, MEMORY without SIZE) names no word
        /// the position admits. A precedence or context-dependent predicate is not evaluated (it reads the parser's own
        /// rule state, which is not the token's): its path stays open, as every path of the other walks does.</summary>
        protected override bool EvalSemanticContext(SemanticContext pred, ParserRuleContext parserCallStack, int alt, bool fullCtx)
        {
            if (!_evaluatesPredicates || pred is not SemanticContext.Predicate { isCtxDependent: false }
                || parser is not CobolParserCoreBase wordFunnel)
                return true;
            _tested.Clear();
            bool holds = wordFunnel.EvaluateTestingWords(
                () => base.EvalSemanticContext(pred, parserCallStack, alt, fullCtx), startIndex, _tested);
            if (!holds && !_tested.Exists(w => w.Matched))
                foreach (var (word, _) in _tested) _closureWords?.Add(word);
            return holds;
        }

        /// <summary>The names of every rule OPEN at the walk's position: entered before it and not complete. Read off the
        /// configurations that took the token before the position (<see cref="_entered"/>): the rule of each one's state
        /// and of every frame on its stack, over all of its paths, where that invocation can still take a token of its
        /// own (<see cref="Continues"/>). The rule invocation stack AT the offending token, for the hints that ask
        /// whether a construct is open there (kb/Work PB3832).
        /// <para>⛔ Never the closed configurations at the position, and never a complete invocation (kb/Work PB3832,
        /// review R2-2). The closure at the position enters every rule that may BEGIN there, the rule of every statement
        /// where a statement may begin among them, and none of those has taken a token; and the statement whose
        /// terminator was the token before stands complete at its rule's end. Counted as open, either put an
        /// <c>ifStatement</c> on the stack of a stray END-IF (after a complete MOVE; after <c>IF … IF … END-IF END-IF</c>),
        /// which then found its IF and lost hint 13's COBOL0306: an explicit scope terminator pairs only with an
        /// UNTERMINATED statement (ISO §14.5.3.2). The parser's own stack, read where it stands at the token, holds
        /// neither: it enters the one rule its prediction chose, and a rule it completes it returns from.</para></summary>
        public string[] OpenRuleNames()
        {
            var rules = new HashSet<int>();
            var seen = new HashSet<PredictionContext>();
            var work = new Stack<PredictionContext>();
            foreach (var c in _entered)
            {
                if (Continues(c.state)) rules.Add(c.state.ruleIndex);
                work.Push(c.context);
            }
            while (work.TryPop(out var stack))
            {
                if (!seen.Add(stack)) continue;
                for (int i = 0; i < stack.Size; i++)
                {
                    int returnState = stack.GetReturnState(i);
                    if (returnState == PredictionContext.EMPTY_RETURN_STATE) continue;
                    if (Continues(atn.states[returnState])) rules.Add(atn.states[returnState].ruleIndex);
                    work.Push(stack.GetParent(i));
                }
            }
            return [.. rules.Select(r => CobolParserCore.ruleNames[r])];
        }

        /// <summary>Can a rule invocation standing at <paramref name="state"/> still take a token of its own: does its
        /// rule-local next set (which the ATN caches) hold a token, not only the rule's end (EPSILON)?</summary>
        private bool Continues(ATNState state)
        {
            var next = atn.NextTokens(state);
            return next.Count > (next.Contains(TokenConstants.EPSILON) ? 1 : 0);
        }

        /// <summary>⛔ THE EMPTY-BLOCK TEST ASKS THE PARSE, NOT THE EXPECTED SET (kb/Work PB3832). The statement whose
        /// required imperative-statement is EMPTY at the walk's position, named for the message (<c>IF</c>,
        /// <c>SEARCH ALL</c>), or null when no block is empty there.
        /// <para>A block is empty exactly when ONE statement written here would let <paramref name="offending"/> follow:
        /// the walk steps each configuration that can begin a statement over a CONTINUE (the no-operation statement,
        /// §14.9.9.1, whose general format §14.9.9.2 is the one word) and asks whether the closed result takes the offending token. A block that already holds
        /// a statement could end where it stands, so a token that may follow one more statement may follow it now and
        /// would not have failed; a token that may follow no statement at all (<c>OF</c> in <c>IF LENGTH OF X</c>, the
        /// NOT of <c>IF A NOT &lt;&gt; B</c>) is a fault of what was written, not a missing statement. The test it
        /// replaces — the expected set holds every statement start and the token is none — is true of both, and of the
        /// position after a complete statement too.</para>
        /// <para>The name is read off the stack of the configuration that passes: the frame that returns from
        /// <c>statementBlock</c> into its caller, then the first enclosing rule named <c>…Statement</c>, so an ON SIZE
        /// ERROR or AT END phrase is named by its statement. Each stack path is tested on its own, so a merged stack
        /// names the construct whose block the token actually follows.</para></summary>
        public string? EmptyBlockOwner(int offending)
        {
            foreach (var c in _configs.Elements)
            {
                if (c.state.ruleIndex != CobolParserCore.RULE_continueStatement || !TakesToken(c, CobolLexer.CONTINUE)) continue;
                foreach (var (branch, owner) in BlockPaths(c.context, [], inBlock: false))
                {
                    var written = Close([new ATNConfig(c.state, c.alt, branch)]);
                    if (Step(written, CobolLexer.CONTINUE).Elements.Any(a => TakesToken(a, offending)))
                        return owner;
                }
            }
            return null;
        }

        private ATNConfigSet Step(ATNConfigSet configs, int ttype) => Close(Move(configs, ttype));

        /// <summary>Every configuration of <paramref name="configs"/> moved over one token of type
        /// <paramref name="ttype"/>, not yet closed.</summary>
        private List<ATNConfig> Move(ATNConfigSet configs, int ttype)
        {
            var moved = new List<ATNConfig>();
            foreach (var c in configs.Elements)
                for (int i = 0; i < c.state.NumberOfTransitions; i++)
                    if (GetReachableTarget(c.state.Transition(i), ttype) is { } target)
                        moved.Add(new ATNConfig(c, target));
            return moved;
        }

        private bool TakesToken(ATNConfig c, int ttype)
        {
            for (int i = 0; i < c.state.NumberOfTransitions; i++)
                if (GetReachableTarget(c.state.Transition(i), ttype) is not null) return true;
            return false;
        }

        /// <summary>The paths of a CONTINUE configuration's <paramref name="stack"/> on which the CONTINUE would begin a
        /// statement DIRECTLY in a <c>statementBlock</c> (frames <c>statement</c>, then <c>statementBlock</c>), each as
        /// the single-path stack down to the frame of the statement that owns the block, above the merged remainder,
        /// with that statement's name. <paramref name="above"/> holds the return states walked so far, innermost
        /// first; a path at a sentence or any other statement list is not a block and yields nothing.</summary>
        private IEnumerable<(PredictionContext Branch, string Owner)> BlockPaths(PredictionContext stack,
            List<int> above, bool inBlock)
        {
            if (above.Count > MaxOwnerFrames) yield break;
            for (int i = 0; i < stack.Size; i++)
            {
                int returnState = stack.GetReturnState(i);
                if (returnState == PredictionContext.EMPTY_RETURN_STATE) continue;
                int ruleIndex = atn.states[returnState].ruleIndex;
                string rule = CobolParserCore.ruleNames[ruleIndex];
                bool nowInBlock = inBlock;
                if (!inBlock)
                {
                    if (ruleIndex == CobolParserCore.RULE_statementBlock) nowInBlock = true;
                    else if (ruleIndex != CobolParserCore.RULE_statement || above.Count > 0) continue;
                }
                else if (rule.EndsWith(StatementSuffix, StringComparison.Ordinal))
                {
                    PredictionContext branch = SingletonPredictionContext.Create(stack.GetParent(i), returnState);
                    for (int k = above.Count - 1; k >= 0; k--) branch = SingletonPredictionContext.Create(branch, above[k]);
                    yield return (branch, SplitCamelCase(rule[..^StatementSuffix.Length]).ToUpperInvariant());
                    continue;
                }
                above.Add(returnState);
                foreach (var found in BlockPaths(stack.GetParent(i), above, nowInBlock)) yield return found;
                above.RemoveAt(above.Count - 1);
            }
        }

        /// <summary>How far above a statement block its owning statement may stand: the block's caller is the statement
        /// itself or one phrase rule of it (an ON SIZE ERROR, AT END or WHEN rule), so a path that has not met one
        /// within this many frames is not a block's.</summary>
        private const int MaxOwnerFrames = 8;
        /// <summary>The stack of each rule context on the parser's real invocation stack: a configuration whose stack is
        /// one of these stands in the parser's own context, not in a rule the walk entered.</summary>
        private readonly Dictionary<PredictionContext, RuleContext> _own = [];

        private readonly Dictionary<PredictionContext, int> _frames = [];

        /// <summary>The configurations at the walk's position, closed: every state with a token edge the position can
        /// reach, each with its whole stack.</summary>
        private ATNConfigSet _configs;

        /// <summary>The configurations <see cref="_configs"/> was closed from: those that took the last token (or the
        /// walk's start, before any): the rules the walk had entered BEFORE its position, never one the closure entered
        /// at it (<see cref="OpenRuleNames"/>).</summary>
        private List<ATNConfig> _entered;

        /// <summary>A walk from ATN state <paramref name="start"/> inside the rule context <paramref name="context"/> on
        /// the parser's real invocation stack. It never predicts, so it never reads a DFA: the base constructor only
        /// stores the array it is given, and an empty one of the parser's size keeps the walk from touching the parser's
        /// shared DFA cache at all.
        /// <para>With <paramref name="evaluateBefore"/> the walk evaluates the predicates its closures meet, the first
        /// closure's at that token (<see cref="AdmittedAt"/>).</para></summary>
        private ResimulationWalk(Parser parser, ATNState start, RuleContext context, IToken? evaluateBefore = null)
            : base(parser, parser.Atn, new DFA[parser.Atn.NumberOfDecisions], new PredictionContextCache())
        {
            mergeCache = new MergeCache();
            if (evaluateBefore is not null)
            {
                (_evaluatesPredicates, input) = (true, (ITokenStream)parser.InputStream);
                startIndex = evaluateBefore.TokenIndex;
            }
            for (RuleContext? c = context; c is not null; c = c.Parent)
                _own.TryAdd(PredictionContext.FromRuleContext(atn, c), c);
            _entered = [new ATNConfig(start, 1, PredictionContext.FromRuleContext(atn, context))];
            _configs = Close(_entered);
        }

        /// <summary>Step every configuration over one token of type <paramref name="ttype"/> and close the result;
        /// false when none can take it, and the walk then stays where it was. A walk that evaluates predicates
        /// evaluates the closure's at <c>startIndex</c>, which <see cref="AdmittedAt"/> sets to the token that follows.</summary>
        public bool Consume(int ttype)
        {
            var moved = Move(_configs, ttype);
            if (moved.Count == 0) return false;
            (_entered, _configs) = (moved, Close(moved));
            return true;
        }

        private ATNConfigSet Close(IEnumerable<ATNConfig> seeds)
        {
            var closed = new ATNConfigSet(fullCtx: true);
            var busy = new HashSet<ATNConfig>();
            if (_evaluatesPredicates) _closureWords = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var c in seeds)
                Closure(c, closed, busy, collectPredicates: _evaluatesPredicates, fullCtx: true, treatEofAsEpsilon: false);
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

    /// <summary>What a position admits at a token (<see cref="ResimulationWalk.AdmittedAt"/>): the tokens its
    /// configurations take with their predicates evaluated there, and the context words the refusing text predicates
    /// tested (kb/Work PB3428).</summary>
    private readonly record struct Admitted(IntervalSet Tokens, IReadOnlyCollection<string> Words);

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
                + " KEY:" + formats + "neither has a KEY phrase — the key of a SEARCH ALL is declared by the "
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
                ? "SEARCH ALL NOT AT END:" + formats + "each has an AT END phrase alone — there is no "
                  + "NOT AT END phrase. Test for a hit in the WHEN branch instead."
                : "SEARCH NOT AT END:" + formats + "each has an AT END phrase alone — there is no NOT "
                  + "AT END phrase. Test for a hit in a WHEN branch instead.";

        if (token.Type == CobolLexer.WHEN)
            for (var c = ctx; c is not null; c = c.Parent as ParserRuleContext)
                for (var d = LastWrittenRuleChild(c); d is not null; d = LastWrittenRuleChild(d))
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
                for (var d = LastWrittenRuleChild(c); d is not null; d = LastWrittenRuleChild(d))
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

    /// <summary>The last child that WROTE something, when it is a rule context: <see cref="LastRuleChild"/> past the
    /// trailing rule contexts that matched no token. Every statement that owns conditional phrases ends in its
    /// <c>conditionalPhrases</c>, which the parser enters (and so adds) even when no phrase is written; a walk that asks
    /// which construct ENDED immediately before the offending token must look past it (kb/Work PB1528: ACCEPT's
    /// <c>acceptSource</c> and DISPLAY's <c>displayUpon</c> are followed by it). UnownedPhraseMessage keeps
    /// <see cref="LastRuleChild"/>: the empty phrase loop IS the ended statement it is looking for.</summary>
    internal static ParserRuleContext? LastWrittenRuleChild(ParserRuleContext c)
    {
        for (int i = c.ChildCount - 1; i >= 0; i--)
            if (c.GetChild(i) is not ParserRuleContext { ChildCount: 0 })
                return c.GetChild(i) as ParserRuleContext;
        return null;
    }

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

    /// <summary>Every explicit scope terminator the grammar spells, as its reserved word (<c>END-ADD</c>): the list
    /// the drift test of the recovery sweeps (<c>ParseRecoveryTailTests</c>, kb/Work PB3843), read from the same map
    /// the misplaced-terminator hint reads, so a terminator a statement gains tomorrow is swept the day it is added.</summary>
    internal static IReadOnlyList<string> ScopeTerminatorWords(Parser recognizer)
    {
        _scopeTerminators ??= BuildScopeTerminators(recognizer);
        return [.. _scopeTerminators.Values.Select(t => t.Word).Order(StringComparer.Ordinal)];
    }

    private static Dictionary<int, ScopeTerminatorInfo> BuildScopeTerminators(Parser recognizer)
    {
        const string prefix = "END-";
        var map = new Dictionary<int, ScopeTerminatorInfo>();
        foreach (var state in recognizer.Atn.states)
        {
            if (state is null) continue;
            string rule = recognizer.RuleNames[state.ruleIndex];
            if (!rule.EndsWith(StatementSuffix, StringComparison.Ordinal)) continue;
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
    private static string? FigurativeSpellingMessage(Parser recognizer, IToken token, OffendingPosition at)
    {
        if (SignConditionSpellingMessage(recognizer, token) is { } signMsg) return signMsg;
        var family = FigurativeFamilies(recognizer).FirstOrDefault(f => f.Contains(token.Type));
        if (family is null) return null;
        var expected = at.Expected();
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

    /// <summary>⛔ THE PARSE AT THE OFFENDING TOKEN — what every hint that infers a cause from parser state reads
    /// (kb/Work PB3832). Every report but a failed adaptive prediction's stands AT its token, and there the parser's own
    /// expected set and rule stack answer. A failed prediction reports the token where every alternative died while the
    /// parser stays at the decision's FIRST token: after an open <c>IF … ELSE IF … END-IF</c> the dangling END-IF
    /// decision looks ahead through the following statements, so <c>IF LENGTH OF X = 30</c> failed at OF with the parser
    /// still before the END-IF, inside the inner IF. Read there, the parser's state told the user an IF lacked its
    /// imperative-statement (and named IF for an empty SEARCH or PERFORM body), and that an END-ADD had no ADD when the
    /// ADD was written after the decision. So the position is re-simulated to the token (<see cref="ResimulationWalk"/>),
    /// once per report (every hint and the residue reading share the one walk) and only when a hint asks; when the walk
    /// cannot reach the token, the answers are null and no hint claims anything about it.</summary>
    private sealed class OffendingPosition
    {
        private readonly Parser _recognizer;
        private ResimulationWalk? _walk;
        private bool _walked;
        private int _lookahead;
        private OffendingPosition? _carried;
        private bool _carriedAsked;

        /// <summary>The position of <paramref name="token"/>, where the parser reported it;
        /// <paramref name="predictionFailedAt"/> says a prediction failed AT it (<see cref="IsPredictionFailedAt"/>).</summary>
        public OffendingPosition(Parser recognizer, IToken token, bool predictionFailedAt = false)
            => (_recognizer, Token, PredictionFailedAt) = (recognizer, token, predictionFailedAt);

        private OffendingPosition(Parser recognizer, IToken token, ResimulationWalk walk, ParserRuleContext closed)
            : this(recognizer, token)
            => (_walk, _walked, CarriedForward, _carriedAsked, _closed) = (walk, true, true, true, closed);

        /// <summary>The construct the parser closed early whose re-simulation found the token, for a position carried
        /// forward (<see cref="CarryForward"/>); null otherwise.</summary>
        private readonly ParserRuleContext? _closed;

        /// <summary>The token the report names.</summary>
        public IToken Token { get; }

        /// <summary>Did an adaptive prediction fail AT the token, refused by a semantic predicate (kb/Work PB3391)?</summary>
        public bool PredictionFailedAt { get; }

        /// <summary>The index of the token the parser stands at.</summary>
        public int ParserTokenIndex => ((ITokenStream)_recognizer.InputStream).LT(1).TokenIndex;

        /// <summary>Was the report carried forward from where the parser stood into a construct it had closed
        /// (<see cref="Carried"/>)?</summary>
        public bool CarriedForward { get; }

        /// <summary>The position the report is carried forward to, or null; computed once, on first use.</summary>
        public OffendingPosition? Carried
        {
            get
            {
                if (!_carriedAsked) (_carried, _carriedAsked) = (CarryForward(), true);
                return _carried;
            }
        }

        /// <summary>⛔ THE POSITION A REPORT NAMES: where the parse really failed, not where ANTLR's SLL prediction
        /// left it (kb/Work PB3832, review R1-2). An optional phrase is a decision whose exit alternative ENDS the
        /// statement, and SLL prediction does not know what may follow a rule it leaves, so when the phrase's own
        /// alternative dies further on (<c>ADD 1 TO A ON SIZE ERROR END-ADD</c>: the empty block fails at END-ADD) the
        /// exit is the one viable alternative: the ADD closes before ON, and the report stands at ON with the parser
        /// after a complete statement, where a period is expected, so the user was told "A period may be missing"
        /// about a phrase ISO §14.9.2.2 prints. Inside an open construct the same failure is a failed prediction of the
        /// enclosing decision, reported where it dies with the parse at that token, and drew the named COBOL2072:
        /// the message depended on the depth. So where the parser stands AT the reported token and the construct it
        /// completed just before it (<see cref="LastClosedConstruct"/>) can, re-simulated in full context from its own
        /// start (<see cref="ResimulationWalk.Over"/>), take that token, the construct was closed early, and the report
        /// moves to the token where that re-simulation dies, read with the walk at that token: the fault the open
        /// construct's prediction names. A token the closed construct cannot take is the parser's fault where it
        /// stands; a re-simulation that reaches the separator period without dying names nothing, and the report
        /// stays. So does a prediction that failed AT the token (<see cref="IsPredictionFailedAt"/>): a semantic
        /// predicate refused it (kb/Work PB3391), and the walk, which evaluates none, would take it where the parse
        /// may not (<c>OBJECT-COMPUTER. XYZ FOO IS.</c> is the 0g refusal of FOO). <see cref="Report"/> carries a report
        /// only when no structural hint names the token where the parser stands: such a diagnosis is about the token as
        /// written, and outranks the furthest point some parse reached. The one structural hint that read token
        /// adjacency as a position fault, a present WHEN OTHER body taken for a misplaced phrase (0b), asks it too.</summary>
        private OffendingPosition? CarryForward()
        {
            var stream = (ITokenStream)_recognizer.InputStream;
            if (!ParserIsThere || Token.Type == TokenConstants.EOF || PredictionFailedAt) return null;
            int i = Token.TokenIndex;
            if (PreviousDefault(stream, ref i) is not { } previous
                || LastClosedConstruct(_recognizer.Context as ParserRuleContext, previous.TokenIndex) is not { } closed
                || ResimulationWalk.Over(_recognizer, closed) is not { } walk
                || !walk.Consume(Token.Type))
                return null;
            for (int next = Token.TokenIndex; NextDefault(stream, ref next) is { } t; )
            {
                if (!walk.Takes(t.Type)) return new(_recognizer, t, walk, closed);
                if (t.Type == CobolLexer.DOT || !walk.Consume(t.Type)) break;
            }
            return null;
        }

        /// <summary>The construct the parser completed at token <paramref name="previous"/>, the last token before the
        /// one it stands at: the last child of the innermost open rule that holds one (a rule entered at the reported
        /// token holds none yet), when that child is a rule context ending there; null when the last thing written is
        /// a token of the open rule itself (nothing was closed).</summary>
        private static ParserRuleContext? LastClosedConstruct(ParserRuleContext? context, int previous)
        {
            for (ParserRuleContext? c = context, entered = null; c is not null; entered = c, c = c.Parent as ParserRuleContext)
            {
                int last = c.ChildCount - 1;
                if (last >= 0 && ReferenceEquals(c.GetChild(last), entered)) last--;
                if (last < 0) continue;
                return c.GetChild(last) is ParserRuleContext { Start: { } start, Stop: { } stop } closed
                       && stop.TokenIndex == previous && start.TokenIndex <= stop.TokenIndex
                    ? closed
                    : null;
            }
            return null;
        }

        /// <summary>The walk to the token: re-simulated from the parser once, on first use; or, carried forward, the
        /// walk that found the token.</summary>
        public ResimulationWalk? Walk
        {
            get
            {
                if (!_walked) (_walk, _walked) = (ResimulationWalk.To(_recognizer, Token), true);
                return _walk;
            }
        }

        /// <summary>The token's LT index from the parser (1 where the parser stands at it); int.MaxValue when it was
        /// carried forward, lies behind the parser, or past the end of input.</summary>
        public int Lookahead
        {
            get
            {
                if (_lookahead != 0) return _lookahead;
                var stream = (ITokenStream)_recognizer.InputStream;
                _lookahead = int.MaxValue;
                for (int k = 1; !CarriedForward; k++)
                {
                    var at = stream.LT(k);
                    if (at.TokenIndex == Token.TokenIndex) { _lookahead = k; break; }
                    if (at.TokenIndex > Token.TokenIndex || at.Type == TokenConstants.EOF) break;
                }
                return _lookahead;
            }
        }

        /// <summary>Does the parser stand AT the token?</summary>
        private bool ParserIsThere => !CarriedForward && ParserTokenIndex == Token.TokenIndex;

        /// <summary>The tokens the parse could take at the token, or null.</summary>
        public IntervalSet? Expected() => ParserIsThere ? _recognizer.GetExpectedTokens() : Walk?.Expected();

        /// <summary>What the parse admits at the token, every predicate on the way to it evaluated, or null when that walk
        /// cannot reach it (<see cref="ResimulationWalk.AdmittedAt"/>): from where the parser stands, or, carried forward,
        /// from the start of the construct closed early. Unlike <see cref="Expected"/>, it is walked even where the parser
        /// stands at the token, because the parser's own expected set is predicate-blind too. Computed once.</summary>
        public Admitted? Admitted()
        {
            if (_admittedAsked) return _admitted;
            _admittedAsked = true;
            if (Token.Type == TokenConstants.EOF) return null;
            var atn = _recognizer.Atn;
            return _admitted = _closed is { } closed
                ? ResimulationWalk.AdmittedAt(_recognizer, atn.ruleToStartState[closed.RuleIndex], closed, closed.Start, Token)
                : ResimulationWalk.AdmittedAt(_recognizer, atn.states[_recognizer.State], _recognizer.Context,
                    ((ITokenStream)_recognizer.InputStream).LT(1), Token);
        }

        private Admitted? _admitted;
        private bool _admittedAsked;

        /// <summary>The rules open at the token, or null.</summary>
        public string[]? OpenRules() => ParserIsThere ? _recognizer.GetRuleInvocationStack().ToArray() : Walk?.OpenRuleNames();

        /// <summary>Would a separator period let the SENTENCE that starts at the token follow: does the parse at the
        /// token take a period, the token and the rest of the sentence up to its separator period (at most
        /// <see cref="MaxResimulatedTokens"/> tokens of it), or a period, the word before the token and then the same?
        /// The second reading is the header a missing period ran into the statement before it: in <c>MOVE 1 TO A</c>
        /// then <c>S2 SECTION.</c> the S2 is a second receiving operand, and the parse dies at SECTION, which no period
        /// lets follow, while <c>. S2 SECTION</c> is the next section. A period the position merely admits (<c>05
        /// A(1)</c>: one may end the entry after A) is not enough (kb/Work PB3832, review R2-1), and neither is a
        /// period that lets the TOKEN follow: a word several context-sensitive words may be spelled (IGNORING, RETRY,
        /// PREVIOUS …) is a legal paragraph-name, so <c>WRITE R IGNORING LOCK.</c> took the period as missing because
        /// <c>. IGNORING</c> parses, and the LOCK after it, which no paragraph header takes, decides it (kb/Work PB906).
        /// Both readings rest on the word before the token being a
        /// word, so one whose characters no edition's Annex B permits makes neither a reading
        /// (<see cref="CobolCharacterRepertoire.IsWordAtSomeEdition"/>): the lexer takes a line holding only U+00A0 for a
        /// word (its word class is a superset, narrowed at bind), and "a period may be missing" after it named a
        /// paragraph header the source does not hold (the golden <c>pb1660-nbsp-only-line-is-not-blank</c>).</summary>
        public bool APeriodLetsTheSentenceFollow()
        {
            if (Walk is not { } walk || Token.Type == TokenConstants.EOF) return false;
            var stream = (ITokenStream)_recognizer.InputStream;
            int i = Token.TokenIndex;
            var before = PreviousDefault(stream, ref i);
            if (before is { Type: CobolLexer.IDENTIFIER, Text: { } word } && !CobolCharacterRepertoire.IsWordAtSomeEdition(word))
                return false;
            var sentence = SentenceTypesFrom(stream, Token);
            return walk.TakesAll([CobolLexer.DOT, .. sentence])
                   || before is not null && walk.TakesAll([CobolLexer.DOT, before.Type, .. sentence]);
        }

        /// <summary>The token types of the sentence that starts at <paramref name="first"/>: from it through the
        /// separator period that ends it, or <see cref="MaxResimulatedTokens"/> tokens, or the end of the input.</summary>
        private static int[] SentenceTypesFrom(ITokenStream stream, IToken first)
        {
            var types = new List<int> { first.Type };
            int index = first.TokenIndex;
            while (types.Last() != CobolLexer.DOT && types.Count < MaxResimulatedTokens
                   && NextDefault(stream, ref index) is { } next)
                types.Add(next.Type);
            return [.. types];
        }

        /// <summary>⛔ THE STATEMENT THE PARSER COMPLETED JUST BEFORE THE TOKEN, WHEN IT CANNOT TAKE THE TOKEN (kb/Work
        /// PB906), named from its rule (<c>writeStatement</c> → WRITE) as <see cref="ResimulationWalk.EmptyBlockOwner"/>
        /// names one; or null. The parser stands at the token (a prediction that failed earlier, or at the token, speaks
        /// of another place), the construct it closed last (<see cref="LastClosedConstruct"/>) ends at the token before
        /// it and holds a statement ending there, and that construct, re-simulated in full context from its own start
        /// over every path (<see cref="ResimulationWalk.Over"/>), cannot take the token. A construct that CAN take it was
        /// closed early by an SLL prediction that did not know what may follow (<see cref="CarryForward"/>): the phrase
        /// is the format's, and the fault lies further on.
        /// <para><b>Which statement is named.</b> The innermost of the statement that ended and the statements still open
        /// around it whose format prints the token AFTER a statement it holds (<see cref="TokensAfterANestedStatement"/>):
        /// a second ELSE after <c>IF c DISPLAY "A" ELSE DISPLAY "B"</c> follows a complete DISPLAY, but the format that
        /// prints ELSE, once, is the IF's, and naming DISPLAY would send the reader to the wrong statement. A token no
        /// enclosing statement prints there (a misspelled word, an unprinted phrase) is the innermost statement's,
        /// wherever else the enclosing one prints it: the <c>&gt;</c> of <c>IF c MOVE 1 TO A &gt; 2</c> belongs to the
        /// IF's condition, which the MOVE follows, so the IF has not ended and the MOVE has (review R1-1).</para></summary>
        public string? StatementThatEndedBefore()
        {
            if (!ParserIsThere || PredictionFailedAt || Token.Type == TokenConstants.EOF) return null;
            var stream = (ITokenStream)_recognizer.InputStream;
            int i = Token.TokenIndex;
            if (PreviousDefault(stream, ref i) is not { } previous
                || LastClosedConstruct(_recognizer.Context as ParserRuleContext, previous.TokenIndex) is not { } closed
                || StatementEndingAt(closed, previous.TokenIndex) is not { } ended
                || ResimulationWalk.Over(_recognizer, closed) is not { } walk
                || walk.Consume(Token.Type))
                return null;
            for (ParserRuleContext? c = ended; c is not null; c = c.Parent as ParserRuleContext)
                if (IsStatementRule(c.RuleIndex) && TokensAfterANestedStatement(c.RuleIndex).Contains(Token.Type))
                    return StatementName(c);
            return StatementName(ended);
        }

        /// <summary>The statement's name as the standard names it: from its rule (<c>ifStatement</c> → IF), and where
        /// the grammar spells a statement's formats as rules of their own under a statement rule (<c>setStatement</c> →
        /// <c>setIndexStatement</c>, one per SET format), the rule that dispatches them (SET, never SET INDEX).</summary>
        private static string StatementName(ParserRuleContext context)
        {
            while (context.Parent is ParserRuleContext parent && IsStatementRule(parent.RuleIndex)) context = parent;
            string rule = CobolParserCore.ruleNames[context.RuleIndex];
            return SplitCamelCase(rule[..^StatementSuffix.Length]).ToUpperInvariant();
        }

        /// <summary>The innermost statement rule context at or below <paramref name="context"/> whose last token is
        /// <paramref name="last"/> (the last child that holds a token, down), or null.</summary>
        private static ParserRuleContext? StatementEndingAt(ParserRuleContext context, int last)
        {
            if (context.Stop is not { } stop || stop.TokenIndex != last || context.Start.TokenIndex > last) return null;
            for (int k = context.ChildCount - 1; k >= 0; k--)
            {
                if (context.GetChild(k) is not ParserRuleContext child) break;   // a token ends the context: nothing deeper
                if (child.Stop is not { } childStop || childStop.TokenIndex != last || child.Start.TokenIndex > last) continue;
                if (StatementEndingAt(child, last) is { } deeper) return deeper;
                break;
            }
            return IsStatementRule(context.RuleIndex) ? context : null;
        }

        /// <summary>The statement whose required imperative-statement is EMPTY at the token, named from its RULE
        /// (<c>ifStatement</c> → IF, <c>searchAllStatement</c> → SEARCH ALL), or null. Asked of the parse itself
        /// (<see cref="ResimulationWalk.EmptyBlockOwner"/>), even when the parser stands at the token: the expected-set
        /// test it replaces (every statement start expected, the token none) also held after a complete statement and
        /// after a condition the token wrongly continued (<c>IF A NOT &lt;&gt; B</c>).</summary>
        public string? EmptyBlockOwner()
        {
            var start = StatementBlockStart(_recognizer);
            if (start.Contains(Token.Type)) return null;   // it CAN start a statement: a later error
            // A block begun at the token expects every statement start. Where the parser stands at the token, its own
            // expected set rules that out without a walk: a necessary condition, never the test.
            if (ParserIsThere && Expected() is { } expected && !start.ToIntegerList().TrueForAll(expected.Contains)) return null;
            return Walk?.EmptyBlockOwner(Token.Type);
        }
    }

    /// <summary>The one suffix that marks a statement's grammar rule (<c>ifStatement</c>, <c>searchAllStatement</c>):
    /// a statement is named from its rule, never from a table.</summary>
    private const string StatementSuffix = "Statement";

    /// <summary>Is the grammar rule a statement's (<c>writeStatement</c>)? The dispatcher <c>statement</c> and
    /// <c>statementBlock</c> are not.</summary>
    private static bool IsStatementRule(int rule)
        => CobolParserCore.ruleNames[rule].EndsWith(StatementSuffix, StringComparison.Ordinal);

    /// <summary>⛔ THE TOKENS A STATEMENT'S OWN FORMAT PRINTS AFTER A STATEMENT IT HOLDS (kb/Work PB906, review R1-1): for
    /// every call a rule of the statement's own syntax (the statement's rule and every rule it calls except another
    /// statement, and the dispatcher and block rules that lead to every statement) makes to a nested statement or block,
    /// the tokens that may follow the call there, continued through the end of the rule it stands in into the rule that
    /// called that one, up to the statement's own end. For IF these are ELSE and END-IF, for PERFORM and EVALUATE
    /// their END-x and WHEN, for the conditional phrases the NOT ... phrase and END-x; the tokens of the condition
    /// before the nested statement (<c>=</c>, <c>AND</c>, <c>&gt;</c>) are not. A token in the set is one the
    /// statement can still be waiting for once the statement it holds is complete, so a second ELSE is the IF's; a token
    /// outside it is no enclosing statement's but the complete statement's own. Read from the ATN once per rule, so a
    /// phrase added to a format tomorrow is in it.</summary>
    private static IntervalSet TokensAfterANestedStatement(int statementRule)
        => AfterNestedStatementTokenSets.GetOrAdd(statementRule, AfterNestedStatementTokenSet);

    private static readonly ConcurrentDictionary<int, IntervalSet> AfterNestedStatementTokenSets = new();

    /// <summary>The ATN's states grouped by the rule they belong to, built once: the ATN is a static of the generated parser.</summary>
    private static readonly Lazy<ILookup<int, ATNState>> StatesByRule = new(
        static () => CobolParserCore._ATN.states.Where(s => s is not null).ToLookup(s => s.ruleIndex));

    private static bool IsStatementOrDispatcher(int rule)
        => IsStatementRule(rule) || rule is CobolParserCore.RULE_statement or CobolParserCore.RULE_statementBlock;

    private static IntervalSet AfterNestedStatementTokenSet(int statementRule)
    {
        var atn = CobolParserCore._ATN;
        // the rules of the statement's own syntax, the calls each makes to a rule of it (the way back up from a rule's
        // end) and the calls to a nested statement or block
        var own = new HashSet<int> { statementRule };
        var callsTo = new Dictionary<int, List<RuleTransition>>();
        var nested = new List<RuleTransition>();
        for (var work = new Stack<int>([statementRule]); work.TryPop(out int rule); )
            foreach (var state in StatesByRule.Value[rule])
                for (int i = 0; i < state.NumberOfTransitions; i++)
                {
                    if (state.Transition(i) is not RuleTransition call) continue;
                    int callee = call.target.ruleIndex;
                    if (IsStatementOrDispatcher(callee)) { nested.Add(call); continue; }
                    if (!callsTo.TryGetValue(callee, out var callers)) callsTo[callee] = callers = [];
                    callers.Add(call);
                    if (own.Add(callee)) work.Push(callee);
                }
        var set = new IntervalSet();
        var visited = new HashSet<ATNState>();
        var tails = new Stack<ATNState>(nested.Select(call => call.followState));
        while (tails.TryPop(out var from))
        {
            if (!visited.Add(from)) continue;
            var next = atn.NextTokens(from);
            set.AddAll(next);
            // the rule may end here: what follows its caller follows the nested statement too, up to the statement's own end
            if (next.Contains(TokenConstants.EPSILON) && from.ruleIndex != statementRule
                && callsTo.TryGetValue(from.ruleIndex, out var up))
                foreach (var call in up) tails.Push(call.followState);
        }
        set.Remove(TokenConstants.EPSILON);
        set.SetReadonly(true);
        return set;
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
    private static bool IsLiteral(IToken token) => LiteralTokens.Whole.Contains(token.Type);

    private static bool IsInRule(string[]? ruleStack, string ruleName)
        => ruleStack?.Any(r => CobolNames.Same(r, ruleName)) == true;

    private static string Truncate(string? text, int max)
    {
        if (text == null) return "<EOF>";
        return text.Length <= max ? text : text[..max] + "...";
    }
}
