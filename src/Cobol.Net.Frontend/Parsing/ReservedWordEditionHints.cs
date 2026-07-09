// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Editions;
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Parsing;

/// <summary>
/// The parse-layer RESERVATION-WORD edition-gate recognizer (rearch PHASE 02): a small, PERMANENT residue of
/// edition-gated constructs whose introduction gate is enforced at PARSE time — because each is spelled with a
/// §8.9 context-sensitive keyword that is a legal user-defined word below its edition (<c>XOR</c>/<c>EXCLUSIVE-OR</c>,
/// the boolean operators <c>B-AND/B-OR/B-XOR/B-NOT</c>, <c>SHARING</c>, <c>RETRY</c>, <c>UNLOCK</c>,
/// <c>PROPERTY</c>). Their grammar <c>{isYYYY()}?</c> predicate is LOAD-BEARING for tokenization and cannot be
/// removed (ungating would let the word bind as a user name and miscompile). When such a construct appears below
/// its edition the predicate rejects it during ANTLR prediction, so the failure surfaces as a GENERIC
/// <c>NoViableAlternative</c> parse error — the reject is correct, the diagnosis is not. This recognizer matches
/// the characteristic (offending-token, rule-stack, lookahead) SIGNATURE and returns the construct's
/// <c>tests/version-matrix/constructs.json</c> row id; the caller (<see cref="CobolErrorStrategy"/>) then renders
/// the COBOLNET0900 edition-naming message through the ONE <see cref="ConstructRegistry.Check"/> funnel, so the
/// display name / introduction edition / ISO citation are the registry row's — NOT hand-copied here. The vendor
/// JSON/XML statements are a documented sub-case (hard-reserved lexer tokens, NOT ISO constructs) → COBOL0313.
/// </summary>
/// <remarks>
/// <para>
/// WHY THIS IS THE PERMANENT RESIDUE AND NOT A BIND-TIME GATE: rearch PHASE 02 moved edition
/// introduction-gating for every HARD-reserved construct (ALLOCATE, INVOKE, CLASS-ID, DELETE FILE, GOBACK
/// RETURNING, LOCK MODE, the record-lock phrases, …) OUT of the grammar to a bind-time
/// <see cref="ConstructRegistry.Check"/> at each construct's recognition point (DEVLOG 680–690) — those parse
/// unconditionally and are gated where they are bound, so they left this table entirely. What CANNOT move is the
/// reservation-word set above: their tokens double as user-defined words below their edition, so the parse-time
/// predicate is the only place the ambiguity can be resolved, and this recognizer is the reliable re-diagnosis.
/// </para>
/// <para>
/// WHY A REVERSE SIGNATURE AND NOT A FORWARD PREDICATE STAMP: rearch P2.7 first tried stamping the rejected
/// <c>GateId</c> on the parser base (a forward <c>{Gate(edition, GateId)}?</c> predicate) and reading it here.
/// An adversarial review disproved that premise (DEVLOG 679): ANTLR evaluates a hoisted predicate SPECULATIVELY,
/// at the stuck token, during a FAILING prediction/recovery — so an ordinary typo (<c>IF W = .</c>, a stray
/// <c>)</c> in a data description, an unsupported statement like <c>SUPPRESS</c>) records a gate for a construct
/// the program never used, and emits a confidently-wrong "requires COBOL-YYYY". A forward stamp cannot reliably
/// mean "the user wrote this construct". The signature match below is reliable precisely because it keys off the
/// construct's OWN tokens actually being present (a real <c>B-AND</c> operator, a real <c>SHARING</c> keyword),
/// so a typo matches nothing and gets a neutral parse error.
/// </para>
/// <para>
/// Signatures were derived empirically (DEVLOG 594): every gated site probed below its edition reports
/// <c>NoViableAlternative</c> with the construct's own keyword as (or adjacent to) the offending token. Since
/// <see cref="ConstructRegistry.Check"/> no-ops when the construct is available at the targeted edition, this
/// recognizer need not itself gate on the introduction edition. JSON/XML are NOT ISO constructs (0 spec hits;
/// owner decision 2, DEVLOG 581) — they map to the vendor-extension hint (COBOL0313), never the 0900 band.
/// </para>
/// </remarks>
public static class ReservedWordEditionHints
{
    /// <summary>The disposition of a recognized parse-layer edition gate: EITHER an ISO construct id (whose
    /// COBOLNET0900 message, introduction edition, and ISO citation come from <see cref="ConstructRegistry.Check"/>)
    /// OR the fixed vendor JSON/XML disposition (<c>COBOL0313</c>, not an ISO construct so it has no registry row).</summary>
    public readonly record struct Hint(string? ConstructId, string? VendorCode, string? VendorMessage);

    /// <summary>
    /// Recognize an edition-gated construct behind a generic parse error and return its
    /// <c>constructs.json</c> row id (the caller renders the COBOLNET0900 text via
    /// <see cref="ConstructRegistry.Check"/>), or the vendor JSON/XML disposition, or <see langword="null"/> when
    /// no signature matches (the caller keeps its generic hints).
    /// </summary>
    public static Hint? Recognize(Parser recognizer, IToken token, string[] ruleStack)
    {
        if (recognizer is not CobolParserCoreBase) return null;
        var stream = (ITokenStream)recognizer.InputStream;

        // The non-ISO vendor statements first — edition-independent (owner decision 2: vendor-dialect,
        // deferred post-G8; the 0900 band would be a lie, no ISO edition has them). JSON/XML are HARD-RESERVED
        // lexer tokens (NOT in the cobolWord user-word set), so an offending JSON/XML token can only be a
        // misplaced JSON/XML statement — the token type alone is the signature (the rule stack is unwound after
        // the non-ISO grammar was hard-deleted at rearch P1).
        if (token.Type is CobolLexer.JSON or CobolLexer.XML)
            return new Hint(null, "COBOL0313",
                $"{(token.Type == CobolLexer.JSON ? "JSON" : "XML")} GENERATE/PARSE is not an ISO/IEC 1989 construct — "
                + "vendor-dialect extension, deferred (owner decision 2, DEVLOG 581)");

        // Each condition is dual-path where the site is an OPTIONAL statement tail: the enclosing rule can
        // have POPPED off the invocation stack by the time the error is reported (the optional subrule's
        // prediction fails, the rule completes empty, the error surfaces one level up), so the adjacent-token
        // signature is the fallback the stack test cannot give. Each arm yields the constructs.json row id.
        string? id = token.Type switch
        {
            // (PROPERTY migrated to a bind-time Check — residue migration #7, DESIGN-version-conformance-pipeline.md —
            // its {is2002()}? predicate is gone; the PROPERTY clause parses at all editions and is gated in the
            // DataBinder data-description clause loop, so the reverse-signature arm is deleted.)
            // (XOR / EXCLUSIVE-OR migrated to a bind-time Check — residue migration #1,
            // DESIGN-version-conformance-pipeline.md — its {is2023()}? predicate is gone; the operator parses at all
            // editions and is gated in BindXorSequence when genuinely present, so the reverse-signature arm is deleted.)
            // The boolean operators (2002): as user words they parse through cobolWord and never error, so an
            // error AT one of these tokens is the {is2002()}?-gated operator meaning below 2002 (the XOR argument).
            CobolLexer.B_AND or CobolLexer.B_OR or CobolLexer.B_XOR or CobolLexer.B_NOT => Constructs.BooleanOperators2002,
            // A boolean COMPUTE (Format 2) below 2002: the {is2002()}?-gated F2 alt is dead, so the whole
            // computeStatement fails to predict and the error surfaces AT the COMPUTE token (not the B-op).
            // Recognize it by a B-operator ahead in the statement (before the sentence terminator).
            CobolLexer.COMPUTE when NextWithin(stream, token, 24,
                    CobolLexer.B_AND, CobolLexer.B_OR, CobolLexer.B_XOR, CobolLexer.B_NOT) => Constructs.BooleanOperators2002,
            // RETRY is a §8.9 reserved-since-2002 word: below 2002 it parses as a user word through cobolWord and
            // never errors, so an error AT the RETRY token IS the gated construct (the XOR argument). (SHARING #3 +
            // UNLOCK #5 migrated to bind-time Checks — DESIGN-version-conformance-pipeline.md — their {is2002()}?
            // predicates are gone; they parse at all editions and are gated in BindOpen/DataBinder/BindUnlock, so
            // their reverse-signature arms are deleted. RETRY #4 is the last file-family arm remaining.)
            CobolLexer.RETRY => Constructs.RetryPhrase2002,
            // (PROCEDURE DIVISION … RAISING migrated to a bind-time Check — residue migration #6,
            // DESIGN-version-conformance-pipeline.md — its {is2002()}? predicate is gone; RAISING parses at all editions
            // and is gated in DataBinder.Linkage, so the mis-firing reverse-signature arm is deleted.)
            // FUNCTION-ID … IS PROTOTYPE (§11.5 Format 2): the {is2002()}?-gated tail is dead below 2002, so the
            // error lands on the IS token (PROTOTYPE ahead) or on PROTOTYPE itself (IS omitted), inside the
            // functionIdParagraph. PROTOTYPE is a §8.9 user word below 2002, so an error AT it there IS the gate.
            _ => null,
        };

        return id is null ? null : new Hint(id, null, null);
    }

    private static IToken? Next(ITokenStream stream, IToken from, int offset)
    {
        int i = from.TokenIndex + offset;
        return i >= 0 && i < stream.Size ? stream.Get(i) : null;
    }

    private static bool PrevWithin(ITokenStream stream, IToken from, int window, int tokenType)
    {
        for (int i = 1; i <= window; i++)
            if (Next(stream, from, -i)?.Type == tokenType) return true;
        return false;
    }

    /// <summary>True when any of <paramref name="tokenTypes"/> appears within <paramref name="window"/> tokens
    /// AHEAD of <paramref name="from"/>, stopping at the sentence terminator (a period). Used to recognize a
    /// gated OPERATOR whose statement fails to predict at the keyword (so the error lands on the keyword, not
    /// the operator) — e.g. a boolean COMPUTE below 2002.</summary>
    private static bool NextWithin(ITokenStream stream, IToken from, int window, params int[] tokenTypes)
    {
        for (int i = 1; i <= window; i++)
        {
            var t = Next(stream, from, i);
            if (t is null || t.Type == CobolLexer.DOT) return false;
            if (System.Array.IndexOf(tokenTypes, t.Type) >= 0) return true;
        }
        return false;
    }

    private static bool InRule(string[] ruleStack, string ruleName)
        => ruleStack.Any(r => string.Equals(r, ruleName, StringComparison.OrdinalIgnoreCase));
}
