// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using WiseOwl.Cobol.Frontend.Cst;
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Frontend.Parsing;

/// <summary>
/// ⛔ THE TOKEN-LEVEL §2023:8.9 RESERVATION GATE (kb/Work PB655). ISO §2023:8.3.2.1 1): "Reserved words shall not be used as
/// user-defined words or system-names" — and §2023:8.9 reserves a word per EDITION, so one lexer token (GOBACK, NULL,
/// ALTER, CONSTANT, …) is a keyword at some editions and a legal user-defined word at the others.
/// <para><b>Why it is not a predicate.</b> It was one: every such token was a <c>cobolWord</c> alternative behind
/// <c>{userWordHere("W")}?</c>. ANTLR evaluates a semantic predicate only at the LEFT EDGE of the decision it is
/// predicting, and <c>cobolWord</c> sits deep inside every reference — so for every ENCLOSING decision the gate was
/// invisible. At COBOL-2002, where NULL is reserved, <c>SET P TO NULL</c> chose the SET-to-identifier format on
/// the strength of a NULL "name" the gate then refused, and legal source failed to parse (measured: 115
/// Conformance reds when the PB655 words were admitted that way; <c>feedback_left_edge_predicates</c>).</para>
/// <para><b>What it is instead.</b> A gated word is never a <c>cobolWord</c> alternative; it stays its keyword
/// token, and its only grammar home is the generated <c>reservedGatedWord</c> rule that every DEFINITION slot
/// offers, and <c>procedureName</c> with it, the procedure-name REFERENCES included (kb/Work PB4206: a reference matches
/// it only where no keyword reading of the word parses). A parse that matches it there at an edition where §2023:8.9 leaves
/// the word FREE has found a USER-DEFINED WORD
/// (<see cref="CobolParserCoreBase.FreeGatedDeclarations"/>); this rewriter then retypes EVERY occurrence of that
/// word to <c>IDENTIFIER</c> and the source is parsed again, so the authoritative parse — every prediction in it —
/// sees a plain user-defined word. The SECOND witness is a syntax error whose offending token is such a free word
/// (<see cref="CobolParserCoreBase.NoteGatedOffender"/>): its keyword reading failed to parse, and the only other
/// reading §2023:8.9 leaves it is a user-defined word — which is how a name introduced in a slot that does not offer
/// <c>reservedGatedWord</c> (an index-name, a SPECIAL-NAMES class-name) is reached. Where §2023:8.9 RESERVES the word
/// nothing is retyped: no other name slot can take the keyword token, a data or file reference fails with the
/// targeted COBOL0901 (<see cref="CobolParserCoreBase.ReservedUserWordViolation"/>, reported by
/// <c>CobolErrorStrategy.ReservedWordUse</c> only where a user-defined word could stand, so a stray END-DISPLAY is the
/// misplaced terminator it is, kb/Work PB4132), and a declaration or a procedure-name draws it from the funnel.</para>
/// <para><b>Its keyword occurrences are retyped too.</b> The retype is by word, so where a format still spells the
/// declared free word as a keyword (MESSAGE in the 2023 RECEIVE's CONTINUE phrase, METHOD in END METHOD), that keyword
/// arrives as <c>IDENTIFIER</c> as well. Such a word's row carries the <c>keywordWhereFree</c> determination and the
/// grammar reads it only through its generated <c>&lt;word&gt;Keyword</c> rule, which accepts the token or the retyped
/// spelling (kb/Work PB2623, PB3794; <c>CobolWordsDriftTests</c> re-derives the 2023 members from the standard).</para>
/// <para><b>But where the user-word reading fails, the keyword is put back.</b> The union grammar spells a later
/// edition's construct (and one an edition deleted) at the editions that lack it, so its named edition refusal can
/// answer, and the word that heads or begins it is free exactly there: retyped with the rest, <c>01 SCREEN LINE 1.</c> in
/// a COBOL-85 SCREEN SECTION made the section header a COBOL0001 (kb/Work PB4933), and <c>01 TYPEDEF PIC X.</c> made
/// <c>01 T PIC X TYPEDEF.</c> an unknown clause, COBOL1941, instead of the clause's COBOL0900 (kb/Work PB4958). So the gate
/// remembers each token of a freed word that the pass before its retype read in a keyword position, and a later pass
/// whose USER-WORD reading of that very token fails, a syntax error at it or a closed format's error production
/// (<see cref="ClosedFormats.IsErrorProduction"/>) that holds it, gets its keyword back
/// (<see cref="TokenRetypes.KeptKeywords"/>): the word's only readings are the user-defined word and its keyword
/// (§2023:8.3.2.1), and the first has failed there. Decided per OCCURRENCE by the parse, never per word by a list: the same
/// word is a name wherever a name reading parses (<c>ASCENDING KEY IS K TYPEDEF</c> at COBOL-85 keys the table on the item
/// TYPEDEF, where the clause reading also parses), and a statement verb at the start of a sentence is the paragraph
/// header it reads as (<c>GOBACK.</c> where the program declares GOBACK). Put back rather than read by a keyword rule: an
/// IDENTIFIER arm at a header would sit in every predicate-blind reading of the ATN the error strategy makes (the recovery
/// sets, the missing-period walk, the closed-format residue) and read every user-defined word there as that
/// header.</para>
/// <para><b>Why DECLARED, not merely free.</b> The union grammar still spells the word's constructs at editions
/// that lack them, so the named edition gate can answer (<c>GOBACK.</c> at COBOL-85 → "requires COBOL-2002"), and
/// the migration mode (<c>--permissive</c>) admits a word the edition added while it is ALSO that edition's
/// keyword. A free word the program never declares can only be that keyword or an unresolvable reference; one it
/// declares is, per §2023:8.3.2.1, a user-defined word — the same DETERMINATION PB805 recorded for operand lists
/// (<see cref="CobolParserCoreBase"/>'s <c>IsKeywordReadingHere</c>), now carried by the token itself. A free word in
/// a procedure-name reference whose keyword reading does not parse is that unresolvable reference: retyped, it draws
/// the binder's COBOL1639 ("names no procedure"), the true diagnosis at an edition where the word is a user word.</para>
/// <para>The same decision must reach every re-parse of this group's text (the D18 subscript and D2 argument
/// fragments), which is why it travels as <see cref="TokenRetypes"/>. Design SSOT:
/// <c>docs/rearchitecture/DESIGN-frontend-grammar.md</c> ("the token-level reservation gate").</para>
/// </summary>
public static class ReservationGateRewriter
{
    /// <summary>⛔ THE GATE LOOP — the ONE entry every WHOLE-GROUP parse goes through (kb/Work PB655). <c>Frontend.LexAndParse</c>
    /// calls it. A gated word is no longer a <c>cobolWord</c> alternative, so a parse that
    /// skips this loop can never read it as a name, and every program naming a table
    /// <c>COL</c> (free at COBOL-85, reserved by §2023:8.9 from 2002) fails with "no viable alternative". The loop
    /// therefore lives HERE, beside the retype it drives.
    /// <para>A pass that DECLARES a reservation-gated word §2023:8.9 leaves free at this edition retypes every
    /// occurrence of it to IDENTIFIER, and the source is parsed again so every prediction sees a user-defined word.
    /// Each round frees at least one more word and a retyped token can never be declared again, so the loop ends; a
    /// program that declares no such word — nearly every program — parses exactly once. Only the LAST pass's
    /// diagnostics describe the source; they are copied into <paramref name="diagnostics"/>, and the decisions reach
    /// the tree as <c>TokenRetypes</c> so every fragment re-parse reads each word exactly as the tree does.</para>
    /// <para>⭐ THE SAME LOOP SETTLES THE ONE OTHER TOKEN DECISION ONLY THE PARSE CAN MAKE (kb/Work PB3981): a figurative
    /// ZERO followed by a sign touching its literal (<c>ZERO -1</c>) is read as an operand-list item before the parse
    /// (<see cref="ZeroTokenRewriter"/>); a pass that fails at that sign has found an arithmetic expression's slot, so
    /// <see cref="ZeroSignReading"/> makes the ZERO that expression's operand and the source is
    /// parsed again. Every whole-group parse therefore gets both decisions from the one entry.</para>
    /// <para><paramref name="parsePass"/> parses the whole stream once from token 0 into the bag it is handed, and
    /// attaches the listener it is handed to its AUTHORITATIVE pass only (never a speculative SLL pass, which can
    /// fail where full LL succeeds) — that listener is the gate's second witness,
    /// <see cref="CobolParserCoreBase.NoteGatedOffender"/>.</para>
    /// <para>⛔ A THIRD ROUND PUTS A KEYWORD BACK (kb/Work PB4933, PB4958): before a word's retype, the pass's tree says
    /// which of its tokens it read in a keyword position (<see cref="KeywordsReadIn"/>); a later pass whose user-word reading
    /// of such a token fails (<see cref="NameReadingsRefused"/>) gives it its keyword type again
    /// (<see cref="TokenRetypes.KeptKeywords"/>), and the source is parsed again. Each such round keeps at least one more
    /// token of a finite set, so the loop still ends.</para></summary>
    public static CobolParserCore.CompilationUnitContext ParseToFixpoint(CobolParserCore parser,
        CommonTokenStream tokens, TokenRetypes retypes, DiagnosticBag diagnostics,
        Func<DiagnosticBag, IAntlrErrorListener<IToken>, CobolParserCore.CompilationUnitContext> parsePass)
    {
        // Seeded with the words an earlier attempt already found declared (the caller's retypes; kb/Work PB1669), so a
        // later round's decisions ADD to them instead of replacing them.
        var freed = new HashSet<string>(retypes.FreedReservedWords, StringComparer.Ordinal);
        var zeroSigns = new ZeroSignReading(tokens);
        // The source offsets of the tokens of a freed word that a pass read in a keyword position before the word's retype:
        // the occurrences whose keyword reading the gate has SEEN parse, the only ones it may put back.
        var keywordReadings = new HashSet<int>();
        while (true)
        {
            var passDiagnostics = new DiagnosticBag();
            var witness = new OffenderWitness();
            var tree = parsePass(passDiagnostics, witness);
            // The words the recovery of this pass read as names (kb/Work PB4218) are the pass's own repair: put back before
            // the gate looks for the keyword tokens it retypes, and before another pass reads the stream.
            parser.RestoreWordsReadAsNames();
            // A refused user-word reading is settled FIRST: what the pass found after it may be its consequence. `OPTIONS.`
            // read as an unknown paragraph left `DEFAULT ROUNDED MODE …` no paragraph to sit in, and the syntax error at
            // DEFAULT would free DEFAULT too, so the restored OPTIONS paragraph lost its clause (kb/Work PB4958). A
            // declaration the pass found is found again by the next one.
            if (NameReadingsRefused(tree, tokens, witness.Offenders, keywordReadings, retypes.KeptKeywords) is { Count: > 0 } refused)
            {
                retypes = retypes with { KeptKeywords = new HashSet<int>(retypes.KeptKeywords.Concat(refused)) };
                Retype(tokens, retypes.FreedReservedWords, retypes.KeptKeywords);
                continue;
            }
            if (parser.FreeGatedDeclarations.Where(freed.Add).ToList() is { Count: > 0 } newlyFreed)
            {
                keywordReadings.UnionWith(KeywordsReadIn(tree, newlyFreed, witness.Offenders));
                retypes = retypes with { FreedReservedWords = new HashSet<string>(freed, StringComparer.Ordinal) };
                Retype(tokens, retypes.FreedReservedWords, retypes.KeptKeywords);
                continue;
            }
            // Only a pass the gate accepts can say what a ZERO's sign is: a ZERO read as an operand-list item where the
            // grammar wanted an arithmetic expression (kb/Work PB3981).
            if (zeroSigns.Revise(witness.Offenders)) continue;
            foreach (var d in passDiagnostics.Diagnostics) diagnostics.Add(d);
            tree.TokenRetypes = retypes;
            return tree;
        }
    }

    /// <summary>Feeds each syntax error's offending token to the gate's second witness
    /// (<see cref="CobolParserCoreBase.NoteGatedOffender"/>, the parser holds that set) and keeps the tokens for the
    /// ZERO sign round (<see cref="ZeroSignReading"/>). One per pass.</summary>
    private sealed class OffenderWitness : BaseErrorListener
    {
        public List<IToken> Offenders { get; } = [];

        public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line,
            int charPositionInLine, string msg, RecognitionException e)
        {
            (recognizer as CobolParserCoreBase)?.NoteGatedOffender(offendingSymbol);
            if (offendingSymbol is not null) Offenders.Add(offendingSymbol);
        }
    }

    /// <summary>⛔ THE TOKENS OF A NEWLY FREED WORD THIS PASS READ IN A KEYWORD POSITION (kb/Work PB4933, PB4958): the source
    /// offset of every token of a word of <paramref name="newlyFreed"/> that the tree holds outside a user-defined-word slot,
    /// in a rule that spells the token (<see cref="RuleSpells"/>), and that was no syntax error's offending token
    /// (<paramref name="offenders"/>: its keyword reading is the one that failed, so <c>01 LOCAL-STORAGE SECTION.</c> at
    /// COBOL-85 stays an entry named LOCAL-STORAGE). These are the occurrences whose keyword reading the gate has seen parse;
    /// <see cref="NameReadingsRefused"/> gives one its keyword back only where its user-word reading then fails. The tree is
    /// walked only when a word was freed, which nearly no program does.</summary>
    private static HashSet<int> KeywordsReadIn(IParseTree tree, List<string> newlyFreed, List<IToken> offenders)
    {
        var words = newlyFreed.ToHashSet(StringComparer.Ordinal);
        var refused = offenders.Select(t => t.StartIndex).ToHashSet();
        var result = new HashSet<int>();
        var spells = new Dictionary<(int Rule, int Token), bool>();
        var work = new Stack<IParseTree>();
        work.Push(tree);
        while (work.TryPop(out var node))
        {
            if (node is ITerminalNode { Symbol: { } symbol } terminal)
            {
                if (terminal is not IErrorNode && symbol.TokenIndex >= 0 && CobolLexer.IsReservationGated(symbol.Type)
                    && terminal.Parent is ParserRuleContext { RuleIndex: var rule } && rule != CobolParserCore.RULE_reservedGatedWord
                    && words.Contains(CobolNames.UpperFold(symbol.Text)) && !refused.Contains(symbol.StartIndex)
                    && (spells.TryGetValue((rule, symbol.Type), out bool known) ? known : spells[(rule, symbol.Type)] = RuleSpells(rule, symbol.Type)))
                    result.Add(symbol.StartIndex);
                continue;
            }
            for (int i = 0; i < node.ChildCount; i++) work.Push(node.GetChild(i));
        }
        return result;
    }

    /// <summary>Does <paramref name="rule"/> itself match <paramref name="tokenType"/> (a token edge of one of its own ATN
    /// states)? The terminal of a keyword the format spells sits in the rule that spells it; a word the error recovery read
    /// as a name and then gave back its keyword type (<see cref="CobolParserCoreBase.RestoreWordsReadAsNames"/>, kb/Work
    /// PB4218) sits in a name rule that spells no such token, and is no keyword read (<c>DISPLAY SCREEN.</c> at 2002 under
    /// <c>--permissive</c>).</summary>
    private static bool RuleSpells(int rule, int tokenType)
    {
        foreach (var state in CobolParserCore._ATN.states)
            if (state.ruleIndex == rule)
                for (int i = 0; i < state.NumberOfTransitions; i++)
                    if (state.Transition(i) is { IsEpsilon: false, Label: { } label } && label.Contains(tokenType)) return true;
        return false;
    }

    /// <summary>⛔ THE TOKENS WHOSE USER-WORD READING THIS PASS REFUSED, AND WHOSE KEYWORD READING AN EARLIER PASS READ (kb/Work
    /// PB4958): each offset of <paramref name="keywordReadings"/> not yet in <paramref name="kept"/> whose token is now a
    /// user-defined word and is either a syntax error's offending token (<paramref name="offenders"/>) or held by a closed
    /// format's error production (<see cref="ClosedFormats.IsErrorProduction"/>, the run <c>ClosedFormatPass</c> refuses as no
    /// clause or paragraph of the format). §2023:8.3.2.1 leaves a word §2023:8.9 frees two readings, the user-defined word and
    /// its keyword; the first failed there, so the token is the keyword the earlier pass read it as, and its construct
    /// draws its own diagnosis (<c>01 T PIC X TYPEDEF.</c> at COBOL-85 is the TYPEDEF clause's COBOL0900, not the unknown
    /// clause's COBOL1941). The tree is walked only while such an offset waits, which nearly no program has.</summary>
    private static List<int> NameReadingsRefused(IParseTree tree, CommonTokenStream tokens, List<IToken> offenders,
        HashSet<int> keywordReadings, IReadOnlySet<int> kept)
    {
        var waiting = keywordReadings.Where(o => !kept.Contains(o)).ToHashSet();
        if (waiting.Count == 0) return [];
        var refused = offenders.Select(t => t.StartIndex).Where(waiting.Contains).ToHashSet();
        var work = new Stack<IParseTree>();
        work.Push(tree);
        while (work.TryPop(out var node))
        {
            if (node is not ParserRuleContext ctx) continue;
            if (ClosedFormats.IsErrorProduction(ctx.RuleIndex) && ctx.Start is { TokenIndex: >= 0 } start && ctx.Stop is { } stop)
            {
                for (int i = start.TokenIndex; i <= stop.TokenIndex; i++)
                    if (waiting.Contains(tokens.Get(i).StartIndex)) refused.Add(tokens.Get(i).StartIndex);
                continue;
            }
            for (int i = 0; i < ctx.ChildCount; i++) work.Push(ctx.GetChild(i));
        }
        return [.. refused];
    }

    /// <summary>Retype every default-channel reservation-gated keyword token whose word is in <paramref name="freed"/>
    /// (upper-case) to <c>IDENTIFIER</c>, keeping its source spelling, but a token whose source offset is in
    /// <paramref name="kept"/> (<see cref="TokenRetypes.KeptKeywords"/>, kb/Work PB4933, PB4958), which keeps its keyword type
    /// or, retyped by an earlier round of the same stream, gets it back (<see cref="RetypedToken"/>). The stream is
    /// rewound.</summary>
    public static void Retype(CommonTokenStream tokens, IReadOnlySet<string> freed, IReadOnlySet<int> kept)
    {
        if (freed.Count == 0) return;
        tokens.Fill();
        var list = tokens.GetTokens();
        for (int i = 0; i < list.Count; i++)
        {
            var tok = list[i];
            int keywordType = tok is RetypedToken retyped ? retyped.KeywordType : tok.Type;
            if (tok.Channel != Lexer.DefaultTokenChannel || !CobolLexer.IsReservationGated(keywordType)) continue;
            // By WORD, not by type alone: the decision is §2023:8.9's, and §2023:8.9 is keyed by spelling (the PB250 text rule).
            bool asName = freed.Contains(CobolNames.UpperFold(tok.Text)) && !kept.Contains(tok.StartIndex);
            if (asName == tok is RetypedToken) continue;
            list[i] = asName ? new RetypedToken(tok) : new CommonToken(tok) { Type = keywordType };
        }
        tokens.Seek(0);
    }

    /// <summary>A reservation-gated keyword token the gate retyped to <c>IDENTIFIER</c>, remembering its keyword type so a later
    /// round of the same stream can give it back (<see cref="NameReadingsRefused"/>).</summary>
    private sealed class RetypedToken : CommonToken
    {
        public RetypedToken(IToken keyword) : base(keyword)
        {
            KeywordType = keyword.Type;
            Type = CobolKeywordTokens.IdentifierType;
        }

        public int KeywordType { get; }
    }
}
