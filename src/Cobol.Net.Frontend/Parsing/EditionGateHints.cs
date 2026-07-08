// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Editions;
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Parsing;

/// <summary>
/// The parse-layer edition-gate RECOGNIZER (VERSION_TEST_MATRIX_DESIGN P2.8; rearch PHASE 02): the grammar's
/// introduction predicates (<c>{is2002()}?</c> …) reject a too-new construct during ANTLR adaptive prediction,
/// so the failure surfaces as a GENERIC <c>NoViableAlternative</c> parse error — a co-equal-diagnostic
/// violation (the reject is correct, the diagnosis is not). This table recognizes the characteristic
/// (offending-token, rule-stack, lookahead) SIGNATURE of each gated construct and returns its
/// <c>tests/version-matrix/constructs.json</c> row id; the caller (<see cref="CobolErrorStrategy"/>) then renders
/// the COBOLNET0900 edition-naming message through the ONE <see cref="ConstructRegistry.Check"/> funnel, so the
/// construct's display name / introduction edition / ISO citation are the registry row's — NOT hand-copied here
/// (rearch PHASE 02 removed the duplicated metadata; this recognizer now carries only the signature → row-id map).
/// </summary>
/// <remarks>
/// <para>
/// WHY A REVERSE SIGNATURE AND NOT A FORWARD PREDICATE STAMP: rearch P2.7 first tried stamping the rejected
/// <c>GateId</c> on the parser base (a forward <c>{Gate(edition, GateId)}?</c> predicate) and reading it here.
/// An adversarial review disproved that premise (DEVLOG 679): ANTLR evaluates a hoisted predicate SPECULATIVELY,
/// at the stuck token, during a FAILING prediction/recovery — so an ordinary typo (<c>IF W = .</c>, a stray
/// <c>)</c> in a data description, an unsupported statement like <c>SUPPRESS</c>) records a gate for a construct
/// the program never used, and emits a confidently-wrong "requires COBOL-YYYY". A forward stamp cannot reliably
/// mean "the user wrote this construct". The signature match below is reliable precisely because it keys off the
/// construct's OWN tokens actually being present (a real ALLOCATE keyword, a real B_AND operator, a CLASS-ID),
/// so a typo matches nothing and gets a neutral parse error. The decades-target is to move gating to BIND time
/// (parse the construct unconditionally, gate at the recognition point through the same <c>Check</c> funnel — the
/// pattern step 6 already used for the five inline gates); until then this recognizer is the reliable path.
/// </para>
/// <para>
/// Signatures were derived empirically (DEVLOG 594): every gated site probed below its edition reports
/// <c>NoViableAlternative</c> with the construct's own keyword as (or adjacent to) the offending token.
/// NOT mapped (documented residue): <c>inlineMethodInvocationStatement</c> (2023 — <c>x(…)</c> has no
/// distinctive token; the OO wave owns it), <c>parameterDescription</c> (2002 — unreachable without a UDF
/// prototype context), and <c>SET … TO objectReference</c> beyond the NULL_/SELF senders. JSON/XML are NOT ISO
/// constructs (0 spec hits; owner decision 2, DEVLOG 581) — they map to the vendor-extension hint (COBOL0313),
/// never to the 0900 band. Since <see cref="ConstructRegistry.Check"/> no-ops when the construct is available at
/// the targeted edition, this recognizer need not itself gate on the introduction edition.
/// </para>
/// </remarks>
public static class EditionGateHints
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
            CobolLexer.PROPERTY when InRule(ruleStack, "dataDescriptionEntry") || InRule(ruleStack, "dataDescription")
                || InRule(ruleStack, "workingStorageSection") => Constructs.PropertyClause2002,
            // The XOR OPERATOR below 2023 (the W3 regating): a parse error AT the XOR/EXCLUSIVE-OR token is
            // the gated operator by construction — as a USER word the token parses through cobolWord and
            // never errors (the condition-rule stack has popped by report time, so no rule filter applies).
            CobolLexer.XOR or CobolLexer.EXCLUSIVE_OR => Constructs.LogicalXorOperator2023,
            // The boolean operators (2002): as user words they parse through cobolWord and never error, so an
            // error AT one of these tokens is the {is2002()}?-gated operator meaning below 2002 (the XOR argument).
            CobolLexer.B_AND or CobolLexer.B_OR or CobolLexer.B_XOR or CobolLexer.B_NOT => Constructs.BooleanOperators2002,
            // A boolean COMPUTE (Format 2) below 2002: the {is2002()}?-gated F2 alt is dead, so the whole
            // computeStatement fails to predict and the error surfaces AT the COMPUTE token (not the B-op).
            // Recognize it by a B-operator ahead in the statement (before the sentence terminator).
            CobolLexer.COMPUTE when NextWithin(stream, token, 24,
                    CobolLexer.B_AND, CobolLexer.B_OR, CobolLexer.B_XOR, CobolLexer.B_NOT) => Constructs.BooleanOperators2002,
            // The file-sharing family (2002). SHARING/RETRY/UNLOCK are §8.9 reserved-since-2002 words: below
            // 2002 they parse as user words through cobolWord and never error, so an error AT one of these
            // tokens IS the gated construct (the XOR argument). LOCK is continuous-since-85 (CLOSE … WITH
            // LOCK), so the LOCK-MODE clause needs the MODE lookahead to disjoin it from that legal 85 form.
            CobolLexer.SHARING => Constructs.FileSharingClause2002,
            CobolLexer.RETRY => Constructs.RetryPhrase2002,
            CobolLexer.UNLOCK => Constructs.UnlockStatement2002,
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
