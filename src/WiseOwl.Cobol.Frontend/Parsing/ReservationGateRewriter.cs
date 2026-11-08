// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
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
/// <para><b>But a later construct's keyword is kept.</b> A word whose every keyword position heads or sits in a construct the
/// free editions LACK (SCREEN SECTION, LOCAL-STORAGE SECTION, the REPOSITORY and OPTIONS paragraphs at COBOL-85; the
/// COMMUNICATION SECTION and its CD at 2014 and 2023; the five comment paragraphs from 2002) carries the
/// <c>refusalWhereFree</c> determination, and the keyword tokens a pass read in such a position are not retyped
/// (<see cref="TokenRetypes.KeptKeywords"/>): the union grammar spells the construct there so its named edition refusal can
/// answer, and retyping the header made <c>01 SCREEN LINE 1.</c> in a COBOL-85 SCREEN SECTION a COBOL0001 at the header
/// (kb/Work PB4933). Kept rather than read by a keyword rule: an IDENTIFIER arm at a header would sit in every
/// predicate-blind reading of the ATN the error strategy makes (the recovery sets, the missing-period walk, the
/// closed-format residue) and read every user-defined word there as that header.</para>
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
    /// <see cref="CobolParserCoreBase.NoteGatedOffender"/>.</para></summary>
    public static CobolParserCore.CompilationUnitContext ParseToFixpoint(CobolParserCore parser,
        CommonTokenStream tokens, TokenRetypes retypes, DiagnosticBag diagnostics,
        Func<DiagnosticBag, IAntlrErrorListener<IToken>, CobolParserCore.CompilationUnitContext> parsePass)
    {
        // Seeded with the words an earlier attempt already found declared (the caller's retypes; kb/Work PB1669), so a
        // later round's decisions ADD to them instead of replacing them.
        var freed = new HashSet<string>(retypes.FreedReservedWords, StringComparer.Ordinal);
        var zeroSigns = new ZeroSignReading(tokens);
        while (true)
        {
            var passDiagnostics = new DiagnosticBag();
            var witness = new OffenderWitness();
            var tree = parsePass(passDiagnostics, witness);
            // The words the recovery of this pass read as names (kb/Work PB4218) are the pass's own repair: put back before
            // the gate looks for the keyword tokens it retypes, and before another pass reads the stream.
            parser.RestoreWordsReadAsNames();
            if (parser.FreeGatedDeclarations.Where(freed.Add).ToList() is { Count: > 0 } newlyFreed)
            {
                retypes = retypes with
                {
                    FreedReservedWords = new HashSet<string>(freed, StringComparer.Ordinal),
                    KeptKeywords = KeywordsReadIn(tree, newlyFreed, parser.DeclaredGatedWords, retypes.KeptKeywords),
                };
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

    /// <summary>⛔ THE KEYWORD TOKENS A PASS READ IN A KEYWORD POSITION OF A DECLARED <c>refusalWhereFree</c> WORD (kb/Work
    /// PB4933): <paramref name="kept"/> with, for each word of <paramref name="newlyFreed"/> that the pass DECLARED
    /// (<paramref name="declared"/>; a word only a syntax error freed was refused in its keyword reading) and whose row carries
    /// the determination (<see cref="CobolLexer.IsRefusalWhereFree"/>), the source offset of every token of it the tree holds
    /// outside a user-defined-word slot. Every keyword position of such a word heads or sits in a construct the editions that
    /// free it lack, where no reading of the free word competes (the row's determination), so the keyword the parse read
    /// there IS the construct's keyword: <c>SCREEN SECTION.</c> stays the SCREEN SECTION after <c>01 SCREEN LINE 1.</c>
    /// declares SCREEN at COBOL-85, and the source draws the section's named COBOL1560, not "unexpected 'SCREEN'". The tree
    /// is walked only when such a word was freed, which nearly no program does.</summary>
    private static IReadOnlySet<int> KeywordsReadIn(IParseTree tree, List<string> newlyFreed, IReadOnlyCollection<string> declared,
        IReadOnlySet<int> kept)
    {
        var words = newlyFreed.Where(w => CobolLexer.IsRefusalWhereFree(w) && declared.Contains(w)).ToHashSet(StringComparer.Ordinal);
        if (words.Count == 0) return kept;
        var result = new HashSet<int>(kept);
        var work = new Stack<IParseTree>();
        work.Push(tree);
        while (work.TryPop(out var node))
        {
            if (node is ITerminalNode { Symbol: { } symbol } terminal)
            {
                if (terminal is not IErrorNode && symbol.TokenIndex >= 0 && CobolLexer.IsReservationGated(symbol.Type)
                    && terminal.Parent is not CobolParserCore.ReservedGatedWordContext
                    && words.Contains(CobolNames.UpperFold(symbol.Text)))
                    result.Add(symbol.StartIndex);
                continue;
            }
            for (int i = 0; i < node.ChildCount; i++) work.Push(node.GetChild(i));
        }
        return result;
    }

    /// <summary>Retype every default-channel reservation-gated keyword token whose word is in
    /// <paramref name="freed"/> (upper-case) to <c>IDENTIFIER</c>, keeping its source spelling, but a token whose source
    /// offset is in <paramref name="kept"/> (<see cref="TokenRetypes.KeptKeywords"/>, kb/Work PB4933). Returns how many
    /// tokens changed; the stream is rewound.</summary>
    public static int Retype(CommonTokenStream tokens, IReadOnlySet<string> freed, IReadOnlySet<int> kept)
    {
        if (freed.Count == 0) return 0;
        tokens.Fill();
        var list = tokens.GetTokens();
        int idType = CobolKeywordTokens.IdentifierType, changed = 0;
        for (int i = 0; i < list.Count; i++)
        {
            var tok = list[i];
            if (tok.Channel != Lexer.DefaultTokenChannel || !CobolLexer.IsReservationGated(tok.Type)) continue;
            // By WORD, not by type alone: the decision is §2023:8.9's, and §2023:8.9 is keyed by spelling (the PB250 text rule).
            if (!freed.Contains(CobolNames.UpperFold(tok.Text)) || kept.Contains(tok.StartIndex)) continue;
            list[i] = new CommonToken(tok) { Type = idType };
            changed++;
        }
        tokens.Seek(0);
        return changed;
    }
}
