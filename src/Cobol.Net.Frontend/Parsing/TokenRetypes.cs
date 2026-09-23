// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Editions;
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Parsing;

/// <summary>
/// ⛔ THE POST-LEX TOKEN DECISIONS OF ONE COMPILATION GROUP — the ONE object every parse of that group's text applies
/// between lexing and parsing: the <c>&gt;&gt;COBOL-WORDS</c> retype (<see cref="CobolWordsRewriter"/>, ISO §7.3.10.4
/// GR2/GR3/GR4) and the §8.9 reservation gate (<see cref="ReservationGateRewriter"/>, kb/Work PB655).
/// <para>It exists because the decisions have more than one reader. The main parse applies them in
/// <c>Frontend.LexAndParse</c>, and the binder RE-LEXES source text for the D18 subscript / reference-modifier
/// segment and the D2 keyword-omitted argument list (<see cref="FragmentParse"/>). A fragment that lexed the text
/// afresh without them would read <c>T(GOBACK)</c> — GOBACK a declared data item at COBOL-85 — as the GOBACK
/// keyword, and a <c>&gt;&gt;COBOL-WORDS UNDEFINE</c>d word as its keyword again: the same text, two parses, two
/// answers. Carrying one value makes the fragment's token stream the main parse's by construction.</para>
/// </summary>
/// <param name="CobolWords">The group's <c>&gt;&gt;COBOL-WORDS</c> override (empty when none).</param>
/// <param name="FreedReservedWords">The reservation-gated words §8.9 leaves free at the compile edition that the
/// program declares — retyped to <c>IDENTIFIER</c> (upper-case spellings; empty when none).</param>
public sealed record TokenRetypes(CobolWordsMap CobolWords, IReadOnlySet<string> FreedReservedWords)
{
    /// <summary>No directive and no freed word — every parse's token stream is the lexer's, unchanged.</summary>
    public static readonly TokenRetypes None = new(CobolWordsMap.Empty, new HashSet<string>(StringComparer.Ordinal));

    /// <summary>Prime <paramref name="lexer"/> BEFORE any tokenization: a de-reserved keyword may be used as a
    /// SUBSCRIPTED data name, so the lexer must open SUBSCRIPT mode at its following '(' although the retype runs
    /// only after lexing. (A freed reservation-gated word needs no priming: every gated token is already a
    /// subscript trigger, generated from the same <c>cobol-words.json</c> rows.)</summary>
    public void PrimeLexer(CobolLexer lexer)
    {
        if (!CobolWords.IsEmpty)
            lexer.SetCobolWordsDataNames(CobolWordsRewriter.DeReservedTokenTypes(CobolWords));
    }

    /// <summary>Apply both retypes to the filled token stream. Byte-identical when <see cref="None"/>.</summary>
    public void Rewrite(CommonTokenStream tokens)
    {
        CobolWordsRewriter.Rewrite(tokens, CobolWords);
        ReservationGateRewriter.Retype(tokens, FreedReservedWords);
    }
}
