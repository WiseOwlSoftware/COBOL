// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolSharp.Compiler.Generated;

namespace CobolSharp.Compiler.Parsing;

/// <summary>
/// The W1.5 edition-gate diagnostic mapping (VERSION_TEST_MATRIX_DESIGN P2.8 / roadmap Phase 2 W1.5): the
/// grammar's introduction predicates (<c>{is2002()}?</c> …) reject a too-new construct during ANTLR adaptive
/// prediction, so the failure surfaces as a GENERIC <c>NoViableAlternative</c> parse error — a G1
/// co-equal-diagnostic violation (the reject is correct, the diagnosis is not). This table recognizes the
/// characteristic (offending-token, rule-stack, lookahead) signature of each gated construct and, when the
/// parser's targeted edition is BELOW the construct's introduction edition, upgrades the message to the
/// COBOLNET0900 edition-naming form used by <c>ConstructRegistry.Check</c> (one wording, two emit layers).
/// Every ISO entry names its <c>tests/version-matrix/constructs.json</c> row id — the registry row is the
/// canonical metadata; this table only maps a PARSE-time surface onto it (bind-time gates keep routing
/// through the registry itself).
/// </summary>
/// <remarks>
/// Signatures were derived empirically (DEVLOG 594): every gated site probed below its edition reports
/// <c>NoViableAlternative</c> with the construct's own keyword as (or adjacent to) the offending token.
/// NOT mapped (documented residue): <c>inlineMethodInvocationStatement</c> (2023 — <c>x(…)</c> has no
/// distinctive token; the OO wave owns it), <c>parameterDescription</c> (2002 — unreachable without a UDF
/// prototype context), and <c>SET … TO objectReference</c> (2002 — no signature distinct from the 85 SET
/// forms). JSON/XML are NOT ISO constructs (0 spec hits; owner decision 2, DEVLOG 581) — they map to the
/// vendor-extension hint (COBOL0313), never to the 0900 band.
/// </remarks>
public static class EditionGateHints
{
    /// <summary>One recognized edition-gated construct: the display name, its introducing edition, the ISO
    /// citation, and the constructs.json row id that canonically carries the metadata.</summary>
    private readonly record struct Gate(string Display, int IntroducedIn, string Citation, string RowId);

    private static readonly Gate DeleteFile = new("the DELETE FILE statement", 2023, "ISO 2023 §14.9.10 Format 2; Annex E.3.3 item 15", "delete-file-2023");
    private static readonly Gate Allocate = new("the ALLOCATE statement", 2002, "ISO §14.9.3", "allocate-2002");
    private static readonly Gate Free = new("the FREE statement", 2002, "ISO §14.9.15", "free-2002");
    private static readonly Gate Invoke = new("the INVOKE statement", 2002, "ISO §14.9.23 (OO)", "invoke-2002");
    private static readonly Gate GobackReturning = new("GOBACK RETURNING", 2002, "ISO §14.9.18", "goback-returning-2002");
    private static readonly Gate ProcedureReturning = new("PROCEDURE DIVISION RETURNING", 2002, "ISO §14.2", "procedure-returning-2002");
    private static readonly Gate StopRunStatus = new("STOP RUN WITH … STATUS", 2002, "ISO §14.9.42", "stop-run-status-2002");
    private static readonly Gate StartWithLength = new("the START KEY … WITH LENGTH phrase", 2002, "ISO §14.9.41", "start-with-length-2002");
    private static readonly Gate Based = new("the BASED clause", 2002, "ISO §13.18.5", "based-clause-2002");
    private static readonly Gate TypeClause = new("the TYPE clause (TYPEDEF family)", 2002, "ISO §13.18.58; provisional 2002 edge", "type-clause-2002");
    private static readonly Gate UsageObject = new("USAGE OBJECT REFERENCE", 2002, "ISO §13.18.60 (OO)", "usage-object-reference-2002");
    private static readonly Gate RepositoryClass = new("the REPOSITORY CLASS entry", 2002, "ISO §12.3.8 (OO)", "repository-class-2002");
    private static readonly Gate SpecialNamesFor = new("the FOR ALPHANUMERIC/NATIONAL phrase (ALPHABET/CLASS/SYMBOLIC CHARACTERS)", 2002, "ISO §12.3.7", "special-names-for-national-2002");
    private static readonly Gate CallByValue = new("the CALL BY VALUE phrase", 2002, "ISO §14.9.4", "call-by-value-2002");
    private static readonly Gate ClassDefinition = new("a class definition (CLASS-ID compilation unit)", 2002, "ISO §11.2/§11.3 (OO)", "class-definition-2002");
    private static readonly Gate InterfaceDefinition = new("an interface definition (INTERFACE-ID compilation unit)", 2002, "ISO §11.5/§11.6 (OO)", "interface-definition-2002");
    private static readonly Gate RepositoryInterface = new("the REPOSITORY INTERFACE entry", 2002, "ISO §12.3.8 (OO)", "repository-interface-2002");
    private static readonly Gate RepositoryProperty = new("the REPOSITORY PROPERTY entry", 2002, "ISO §12.3.8 (OO; §8.4.3.9.3 SR1)", "repository-property-2002");
    private static readonly Gate PropertyClause = new("the PROPERTY data-description clause", 2002, "ISO §13.18.42 (OO)", "property-clause-2002");
    private static readonly Gate SetObjectRef = new("SET … TO object-reference (Format 5)", 2002, "ISO §14.9.39 F5 (OO)", "set-object-reference-2002");
    private static readonly Gate LogicalXor = new("the logical XOR/EXCLUSIVE-OR operator", 2023, "ISO §8.8.4.9; Annex E.2 item 25 (VCR rows 32/41)", "logical-xor-operator-2023");
    private static readonly Gate BooleanOps = new("the boolean operators B-AND/B-OR/B-XOR/B-NOT", 2002, "ISO §8.7.2/§8.8.2 (COMPUTE F2 §14.9.8; relation §8.8.4.2.2)", "boolean-operators-2002");
    private static readonly Gate Sharing = new("the SHARING clause / OPEN SHARING phrase", 2002, "ISO §12.4.5.15 / §14.9.27", "file-sharing-clause-2002");
    private static readonly Gate LockMode = new("the LOCK MODE clause", 2002, "ISO §12.4.5.9", "lock-mode-clause-2002");
    private static readonly Gate Retry = new("the RETRY phrase", 2002, "ISO §14.7.9", "retry-phrase-2002");
    private static readonly Gate Unlock = new("the UNLOCK statement", 2002, "ISO §14.9.47", "unlock-statement-2002");
    private static readonly Gate FunctionPrototype = new("a FUNCTION-ID … IS PROTOTYPE (function prototype)", 2002, "ISO §11.5 Format 2 / §10.6", "function-prototype-2002");
    private static readonly Gate OccursDynamic = new("the OCCURS DYNAMIC clause (dynamic-capacity table)", 2014, "ISO §13.18.38 Format 4", "occurs-dynamic-2014");

    /// <summary>
    /// Recognize an edition-gated construct behind a generic parse error. Returns the COBOLNET0900-band
    /// message when the signature matches AND the parser targets an edition below the construct's
    /// introduction (at ≥ the introduction edition the same signature means some OTHER syntax problem — the
    /// generic diagnosis stands). Returns the vendor-extension message for the non-ISO JSON/XML statements at
    /// every edition. Null = no mapping; the caller keeps its generic hints.
    /// </summary>
    public static (string Code, string Message)? Recognize(Parser recognizer, IToken token, string[] ruleStack)
    {
        if (recognizer is not CobolParserCoreBase core) return null;
        int dialect = core.DialectLevel;
        var stream = (ITokenStream)recognizer.InputStream;

        // The non-ISO vendor statements first — edition-independent (owner decision 2: vendor-dialect,
        // deferred post-G8; the 0900 band would be a lie, no ISO edition has them).
        if (token.Type is CobolLexer.JSON or CobolLexer.XML && InRule(ruleStack, "procedureDivision"))
            return ("COBOL0313",
                $"{(token.Type == CobolLexer.JSON ? "JSON" : "XML")} GENERATE/PARSE is not an ISO/IEC 1989 construct — "
                + "vendor-dialect extension, deferred (owner decision 2, DEVLOG 581)");

        // Each condition is dual-path where the site is an OPTIONAL statement tail: the enclosing rule can
        // have POPPED off the invocation stack by the time the error is reported (the optional subrule's
        // prediction fails, the rule completes empty, the error surfaces one level up), so the adjacent-token
        // signature is the fallback the stack test cannot give.
        Gate? gate = token.Type switch
        {
            CobolLexer.DELETE when Next(stream, token, 1)?.Type == CobolLexer.FILE => DeleteFile,
            // OCCURS DYNAMIC (Format 4, 2014): the error surfaces at OCCURS (its DYNAMIC alt is is2014()-gated) or at
            // the CAPACITY token (which appears ONLY in this clause). DYNAMIC alone also means ACCESS MODE DYNAMIC —
            // so gate on the OCCURS-then-DYNAMIC pair, not a bare DYNAMIC.
            CobolLexer.OCCURS when Next(stream, token, 1)?.Type == CobolLexer.DYNAMIC => OccursDynamic,
            CobolLexer.CAPACITY => OccursDynamic,
            CobolLexer.ALLOCATE => Allocate,
            CobolLexer.FREE => Free,
            CobolLexer.INVOKE => Invoke,
            CobolLexer.RETURNING when InRule(ruleStack, "gobackStatement")
                || Next(stream, token, -1)?.Type == CobolLexer.GOBACK => GobackReturning,
            // The division-header RETURNING: inside procedureDivision but NOT inside any statement (the
            // header parses before the first statement rule is entered). CALL … RETURNING is 85-legal and
            // parses through callReturningPhrase — its stack contains "statement", so it never lands here.
            CobolLexer.RETURNING when InRule(ruleStack, "procedureDivision") && !InRule(ruleStack, "statement") => ProcedureReturning,
            CobolLexer.WITH when InRule(ruleStack, "stopStatement")
                || (Next(stream, token, -1)?.Type == CobolLexer.RUN && Next(stream, token, -2)?.Type == CobolLexer.STOP) => StopRunStatus,
            CobolLexer.WITH when Next(stream, token, 1)?.Type == CobolLexer.LENGTH
                && (InRule(ruleStack, "startStatement") || PrevWithin(stream, token, 8, CobolLexer.START)) => StartWithLength,
            CobolLexer.BASED when InRule(ruleStack, "dataDescriptionEntry") || InRule(ruleStack, "dataDescription") => Based,
            CobolLexer.TYPE when InRule(ruleStack, "dataDescriptionEntry") || InRule(ruleStack, "dataDescription") => TypeClause,
            CobolLexer.OBJECT when Next(stream, token, 1)?.Type == CobolLexer.REFERENCE
                && (InRule(ruleStack, "dataDescriptionEntry") || InRule(ruleStack, "dataDescription") || Next(stream, token, -1)?.Type == CobolLexer.USAGE) => UsageObject,
            CobolLexer.CLASS when InRule(ruleStack, "repositoryParagraph") => RepositoryClass,
            CobolLexer.INTERFACE when InRule(ruleStack, "repositoryParagraph") => RepositoryInterface,
            CobolLexer.PROPERTY when InRule(ruleStack, "repositoryParagraph") => RepositoryProperty,
            CobolLexer.PROPERTY when InRule(ruleStack, "dataDescriptionEntry") || InRule(ruleStack, "dataDescription")
                || InRule(ruleStack, "workingStorageSection") => PropertyClause,
            // SET … TO NULL/SELF (F5): the NULL_/SELF token after TO inside a SET statement is the
            // signature (a dataReference sender parses as the 85-legal Format-1 shape and never errors).
            CobolLexer.NULL_ when InRule(ruleStack, "setStatement")
                || Next(stream, token, -1)?.Type == CobolLexer.TO => SetObjectRef,
            CobolLexer.SELF when InRule(ruleStack, "setStatement") => SetObjectRef,
            CobolLexer.FOR when InRule(ruleStack, "specialNamesParagraph") => SpecialNamesFor,
            CobolLexer.BY when InRule(ruleStack, "callStatement") && Next(stream, token, 1)?.Type == CobolLexer.VALUE => CallByValue,
            CobolLexer.VALUE when InRule(ruleStack, "callStatement") && Next(stream, token, -1)?.Type == CobolLexer.BY => CallByValue,
            // The XOR OPERATOR below 2023 (the W3 regating): a parse error AT the XOR/EXCLUSIVE-OR token is
            // the gated operator by construction — as a USER word the token parses through cobolWord and
            // never errors (the condition-rule stack has popped by report time, so no rule filter applies).
            CobolLexer.XOR or CobolLexer.EXCLUSIVE_OR => LogicalXor,
            // The boolean operators (2002): as user words they parse through cobolWord and never error, so an
            // error AT one of these tokens is the {is2002()}?-gated operator meaning below 2002 (the XOR argument).
            CobolLexer.B_AND or CobolLexer.B_OR or CobolLexer.B_XOR or CobolLexer.B_NOT => BooleanOps,
            // A boolean COMPUTE (Format 2) below 2002: the {is2002()}?-gated F2 alt is dead, so the whole
            // computeStatement fails to predict and the error surfaces AT the COMPUTE token (not the B-op).
            // Recognize it by a B-operator ahead in the statement (before the sentence terminator).
            CobolLexer.COMPUTE when NextWithin(stream, token, 24,
                    CobolLexer.B_AND, CobolLexer.B_OR, CobolLexer.B_XOR, CobolLexer.B_NOT) => BooleanOps,
            // The file-sharing family (2002). SHARING/RETRY/UNLOCK are §8.9 reserved-since-2002 words: below
            // 2002 they parse as user words through cobolWord and never error, so an error AT one of these
            // tokens IS the gated construct (the XOR argument). LOCK is continuous-since-85 (CLOSE … WITH
            // LOCK), so the LOCK-MODE clause needs the MODE lookahead to disjoin it from that legal 85 form.
            CobolLexer.SHARING => Sharing,
            CobolLexer.LOCK when Next(stream, token, 1)?.Type == CobolLexer.MODE => LockMode,
            CobolLexer.RETRY => Retry,
            CobolLexer.UNLOCK => Unlock,
            // FUNCTION-ID … IS PROTOTYPE (§11.5 Format 2): the {is2002()}?-gated tail is dead below 2002, so the
            // error lands on the IS token (PROTOTYPE ahead) or on PROTOTYPE itself (IS omitted), inside the
            // functionIdParagraph. PROTOTYPE is a §8.9 user word below 2002, so an error AT it there IS the gate.
            CobolLexer.PROTOTYPE when InRule(ruleStack, "functionIdParagraph") => FunctionPrototype,
            CobolLexer.IS when InRule(ruleStack, "functionIdParagraph")
                && Next(stream, token, 1)?.Type == CobolLexer.PROTOTYPE => FunctionPrototype,
            _ => null,
        };

        // A classDefinition/interfaceDefinition rejected at the compilationGroup level reports the
        // offending token at the unit start (empirically 'IDENTIFICATION'); the CLASS-ID/INTERFACE-ID token
        // a few tokens ahead is the signature. (IMPLEMENTS and the METHOD-ID GET/SET PROPERTY selector are
        // NOT mapped — they only occur INSIDE a class/interface unit, whose own gate fires first; same
        // transitive-coverage argument as the remarks' residue list.)
        if (gate is null && token.Type is CobolLexer.IDENTIFICATION or CobolLexer.CLASS_ID or CobolLexer.INTERFACE_ID)
            for (int i = 0; gate is null && i <= 4; i++)
                gate = Next(stream, token, i)?.Type switch
                {
                    CobolLexer.CLASS_ID => ClassDefinition,
                    CobolLexer.INTERFACE_ID => InterfaceDefinition,
                    _ => null,
                };

        if (gate is not { } g || dialect >= g.IntroducedIn) return null;
        return ("COBOLNET0900",
            $"{g.Display} requires COBOL-{g.IntroducedIn} (targeting COBOL-{dialect}) — {g.Citation} "
            + $"(constructs.json row {g.RowId})");
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
