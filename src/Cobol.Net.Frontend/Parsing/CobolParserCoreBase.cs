// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Editions;

namespace CobolNet.Frontend.Generated;

/// <summary>
/// Base class for the ANTLR-generated CobolParserCore.
/// Provides semantic predicates for paragraph detection.
/// </summary>
public abstract class CobolParserCoreBase : Parser
{
    /// <summary>
    /// The targeted edition (rearch P2.7): the SINGLE source of the dialect year the grammar's introduction
    /// gates read (ending the pre-P2 triple-sourcing across the parser base, the frontend, and the compiler).
    /// Defaults to COBOL-85 (strict) — the level the NIST CCVS corpus targets; the CLI/front-end sets it
    /// explicitly (via <see cref="DialectLevel"/>) for every real parse.
    /// </summary>
    public EditionInfo Edition { get; set; } = EditionInfo.Of(85);

    /// <summary>
    /// Dialect level (ISO year) for gating non-COBOL-85 features — a shim over <see cref="Edition"/>, the single
    /// source. Kept for the <c>parser.DialectLevel = …</c> call sites (Frontend); the setter rebuilds
    /// <see cref="Edition"/> preserving its permissive axis.
    /// </summary>
    public int DialectLevel
    {
        get => Edition.Year;
        set => Edition = EditionInfo.Of(value, Edition.Permissive);
    }

    protected bool is85()   => Edition.Has(85);
    protected bool is2002() => Edition.Has(2002);
    protected bool is2014() => Edition.Has(2014);
    protected bool is2023() => Edition.Has(2023);

    /// <summary>
    /// True when the current token spells a reserved-as-facility keyword AT THE TARGETED EDITION — i.e. it can
    /// only be the (unsupported) facility verb here, never a user-defined word. Gates the recognize-and-name
    /// statement arms for the facilities COBOL.NET does not implement: MCS SEND/RECEIVE (ISO §14.9.31/§14.9.38,
    /// Annex A.3 item 4), COMMIT/ROLLBACK (A.3 items 6–7), VALIDATE (§14.9.50).
    /// <para>
    /// WHY A PREDICATE AT ALL, given these are now hard lexer tokens: their §8.9 reservation is NON-MONOTONIC —
    /// RECEIVE/SEND/END-RECEIVE are reserved at 85, USER WORDS at 2002/2014, and re-reserved at 2023;
    /// COMMIT/ROLLBACK/END-SEND are reserved at 2023 only; VALIDATE is a user word at 85; MESSAGE runs the other
    /// way (reserved 85/2002, user word 2014/2023). The tokens are admitted to the <c>cobolWord</c> nameSlot
    /// funnel so they remain legal user names wherever unreserved, and this predicate stops the STATEMENT arm
    /// from firing at those editions — so <c>01 RECEIVE PIC X.</c> at --std 2002 stays a data item.
    /// </para>
    /// <para>
    /// ⛔ The arms this gates are KEYWORD-TOKEN-LED, never IDENTIFIER-led. An IDENTIFIER-led <c>statement</c>
    /// alternative poisons ANTLR's ALL(*) boolean-factor prediction DFA and regresses
    /// <c>COMPUTE R = B-NOT A.</c> at every edition — empirically proven and reverted (DEVLOG 903). A predicate
    /// on a distinct leading token is unreachable during arithmetic/boolean prediction and cannot poison it.
    /// </para>
    /// Reads the SAME <see cref="ReservedWords"/> table the §8.9 funnel uses, so recognition and reservation
    /// can never diverge. Read-only — safe for ANTLR's repeated speculative prediction calls.
    /// </summary>
    protected bool facilityWord(string keyword)
    {
        var t = CurrentToken;
        if (t is null) return false;
        if (!string.Equals(t.Text, keyword, StringComparison.OrdinalIgnoreCase)) return false;
        return ReservedWords.Find(keyword)?.IsReservedAt(Edition.Year) ?? false;
    }

    protected CobolParserCoreBase(ITokenStream input) : base(input) { }
    protected CobolParserCoreBase(ITokenStream input, TextWriter output, TextWriter errorOutput)
        : base(input, output, errorOutput) { }

    /// <summary>
    /// Returns true if the current token is the first non-whitespace token on its line.
    /// Used to prevent stray identifiers (like LINES after WRITE ADVANCING)
    /// from being misinterpreted as paragraph names.
    /// </summary>
    protected bool IsAtLineStart()
    {
        var token = CurrentToken;
        if (token == null) return false;

        // Check if this token's column is 0, or if the previous token
        // is on a different line
        int tokenLine = token.Line;
        int tokenIndex = token.TokenIndex;

        if (tokenIndex <= 0) return true;

        var prevToken = TokenStream.Get(tokenIndex - 1);
        return prevToken.Line < tokenLine;
    }

    /// <summary>
    /// Predicate for the bare (adjective-less) INSPECT TALLYING count phrase. An ALL or
    /// LEADING adjective is transitive across the operands that follow it (ISO 1989:1985
    /// 14.9.22 GR 10), so "FOR LEADING ""S"" ""S"" ""T""" lists three operands under one
    /// counter. But a data-name immediately followed by FOR is the NEXT tallying counter,
    /// not a transitive operand. Returning false there stops the count-phrase repetition so
    /// the data-name begins a new inspectTallyingItem instead of being swallowed as a pattern.
    /// </summary>
    protected bool IsBareInspectOperand() => TokenStream.LA(2) != CobolLexer.FOR;

    /// <summary>
    /// COBOL-2002 boolean-condition discriminator (ISO §8.8.4.2.2 / §8.8.4.3): true when a boolean OPERATOR
    /// (B-AND / B-OR / B-XOR / B-NOT) appears in the CURRENT condition ahead of the parse position, before any
    /// condition boundary. This gates a dedicated <c>primaryCondition</c> alternative WITHOUT touching the
    /// shared <c>comparisonExpression</c> rule (whose modification regressed subscript/ref-mod comparisons at
    /// 2002+, DEVLOG 621) — a normal comparison (no B-op ahead) returns false and falls to comparisonExpression
    /// unchanged. The scan stops at the condition's end: a period, the logical connectives (AND/OR/THEN/ELSE),
    /// a WHEN / END-* / UNTIL / VARYING, or any statement-starting keyword (so it never crosses into an IF body),
    /// and is window-capped. Read-only over the token stream — safe for ANTLR's repeated prediction calls.
    /// </summary>
    protected bool boolExprAhead()
    {
        // Fires at ALL editions (superset parse — residue migration #2): a below-2002 boolean condition must PARSE so
        // the bind-time gate can name COBOLNET0900, rather than fail as a generic parse error. A B-op-free comparison
        // still returns false and falls to comparisonExpression unchanged (the shared rule is untouched — DEVLOG 621).
        int prev = 0;   // token immediately BEFORE position i (0 = condition start — a leading binary B-op has no left operand)
        for (int i = 1; i <= 96; i++)
        {
            int t = TokenStream.LA(i);
            switch (t)
            {
                // A BINARY boolean operator is genuine ONLY with a completed operand immediately before it. A leading /
                // operand-less occurrence is a §8.9 user data-name below 2002 (IF B-AND = 5) — NOT the operator, so it
                // must fall to the normal comparison unchanged (else a plain comparison mis-gates as boolean, and RETRY
                // #4's sibling mis-fire class returns). Below 2002 B-AND/B-OR/B-XOR are legal user words; at ≥2002 the
                // §8.9 funnel already reserves them, so a leading occurrence is a name-slot error either way — never here.
                // The four boolean SHIFT operators (§8.8.2 rule 8, 2023) are NEVER legal user words (absent from
                // _dataNameTokens), so a shift token always IS the operator — detecting it here recognizes a shift-only
                // boolean expression (e.g. `A B-SHIFT-L 2`), which no binary/unary B-op precedes.
                case CobolLexer.B_AND:
                case CobolLexer.B_OR:
                case CobolLexer.B_XOR:
                case CobolLexer.B_SHIFT_L:
                case CobolLexer.B_SHIFT_R:
                case CobolLexer.B_SHIFT_LC:
                case CobolLexer.B_SHIFT_RC:
                    if (IsBoolOperandTerm(prev)) return true;
                    break;
                // UNARY prefix B-NOT is genuine when a boolean operand can immediately FOLLOW (IF B-NOT A), not when it
                // heads a comparison as a user data-name (IF B-NOT = 5).
                case CobolLexer.B_NOT:
                    if (IsBoolOperandStart(TokenStream.LA(i + 1))) return true;
                    break;
                // ── Condition boundaries: no B-operator can belong to THIS condition past here ──
                case CobolLexer.DOT:
                case CobolLexer.AND:
                case CobolLexer.OR:
                case CobolLexer.THEN:
                case CobolLexer.ELSE:
                case CobolLexer.WHEN:
                case CobolLexer.END_IF:
                case CobolLexer.END_PERFORM:
                case CobolLexer.END_EVALUATE:
                case CobolLexer.END_SEARCH:
                case CobolLexer.UNTIL:
                case CobolLexer.VARYING:
                case CobolLexer.TIMES:
                case TokenConstants.EOF:
                // Statement-starting keywords — the condition ends where the IF/WHEN body begins, so the scan
                // must never cross into a body that might itself contain a boolean COMPUTE.
                case CobolLexer.ACCEPT: case CobolLexer.ADD: case CobolLexer.ALLOCATE: case CobolLexer.CALL:
                case CobolLexer.CANCEL: case CobolLexer.CLOSE: case CobolLexer.COMPUTE: case CobolLexer.CONTINUE:
                case CobolLexer.DELETE: case CobolLexer.DISPLAY: case CobolLexer.DIVIDE: case CobolLexer.EVALUATE:
                case CobolLexer.EXIT: case CobolLexer.FREE: case CobolLexer.GOBACK: case CobolLexer.GO:
                case CobolLexer.INITIALIZE: case CobolLexer.INSPECT: case CobolLexer.INVOKE: case CobolLexer.MERGE:
                case CobolLexer.MOVE: case CobolLexer.MULTIPLY: case CobolLexer.NEXT: case CobolLexer.OPEN:
                case CobolLexer.PERFORM: case CobolLexer.RAISE: case CobolLexer.READ: case CobolLexer.RELEASE:
                case CobolLexer.RESUME: case CobolLexer.RETURN: case CobolLexer.REWRITE: case CobolLexer.SEARCH:
                case CobolLexer.SET: case CobolLexer.SORT: case CobolLexer.START: case CobolLexer.STOP:
                case CobolLexer.STRING: case CobolLexer.SUBTRACT: case CobolLexer.UNSTRING: case CobolLexer.WRITE:
                case CobolLexer.IF:
                    return false;
            }
            prev = t;
        }
        return false;
    }

    /// <summary>An operand-ENDING token — an operand can end with an identifier, a right paren, or a literal. Used by
    /// <see cref="boolExprAhead"/> to confirm a binary B-operator has a genuine LEFT operand (so a §8.9 user data-name
    /// spelled B-AND/B-OR/B-XOR heading a comparison below 2002 is not mistaken for the operator).</summary>
    private static bool IsBoolOperandTerm(int t) => t is
        CobolLexer.IDENTIFIER or CobolLexer.RPAREN or CobolLexer.SUB_RPAREN
        or CobolLexer.INTEGERLIT or CobolLexer.DECIMALLIT or CobolLexer.FLOATLIT
        or CobolLexer.STRINGLIT or CobolLexer.NATLIT or CobolLexer.HEXLIT or CobolLexer.BOOLLIT
        or CobolLexer.SIGNED_INTEGERLIT or CobolLexer.SIGNED_DECIMALLIT;

    /// <summary>An operand-STARTING token — a boolean operand can start with an identifier, '(', a nested B-NOT, a
    /// boolean literal, or figurative ZERO. Used to confirm a prefix B-NOT is a genuine unary operator.</summary>
    private static bool IsBoolOperandStart(int t) => t is
        CobolLexer.IDENTIFIER or CobolLexer.LPAREN or CobolLexer.B_NOT
        or CobolLexer.BOOLLIT or CobolLexer.ZERO;

    /// <summary>
    /// COBOL-2002 RETRY-phrase forward detector (ISO §14.7.9) for the OPEN clause, where the phrase sits inside the
    /// <c>openFileSpec+</c> file-name list and <c>RETRY</c> is a legal §8.9 user-defined word below 2002. Returns
    /// true only when the tail after RETRY is UNAMBIGUOUSLY a retry phrase — it contains a numeric count
    /// (<c>n TIMES</c> | <c>FOR? n SECONDS</c>): an integer can never be a file name, so RETRY must be the phrase
    /// keyword, AND at least one further file-name token remains before the sentence terminator (<c>openFileSpec+</c>
    /// stays satisfiable). <c>RETRY FOREVER</c> (FOREVER is a §8.10 user-legal word) and a bare <c>RETRY name</c> are
    /// genuinely ambiguous below 2002, so they return false and DEFER to <c>is2002()</c> — below 2002 they parse as
    /// file names (INV-1 continuity), never a wrong edition claim (fail-safe: a missed gate degrades to a neutral
    /// parse error). The other five RETRY sites name their file BEFORE the phrase, so they carry no ambiguity and
    /// are bind-gated directly (no forward detect needed). Read-only over the token stream — safe for ANTLR's
    /// repeated prediction calls.
    /// </summary>
    protected bool retryPhraseAhead()
    {
        if (TokenStream.LA(1) != CobolLexer.RETRY) return false;
        bool sawNumber = false;
        for (int i = 2; i <= 24; i++)
        {
            switch (TokenStream.LA(i))
            {
                // A numeric literal in the count position (n TIMES | FOR? n SECONDS) — the ambiguity-free signal.
                case CobolLexer.INTEGERLIT:
                case CobolLexer.DECIMALLIT:
                case CobolLexer.FLOATLIT:
                case CobolLexer.SIGNED_INTEGERLIT:
                case CobolLexer.SIGNED_DECIMALLIT:
                    sawNumber = true;
                    break;
                // The numeric retry tail closes here — it is the phrase ONLY if a count was seen and a further
                // candidate file-name token still remains for openFileSpec+.
                case CobolLexer.TIMES:
                case CobolLexer.SECONDS:
                    if (!sawNumber) return false;
                    int next = TokenStream.LA(i + 1);
                    return next != CobolLexer.DOT && next != TokenConstants.EOF;
                // FOREVER is user-legal (ambiguous); a boundary means no numeric tail was found. Defer to is2002().
                case CobolLexer.FOREVER:
                case CobolLexer.DOT:
                case TokenConstants.EOF:
                    return false;
            }
        }
        return false;
    }

    /// <summary>
    /// The Format-3 PERFORM WHEN operand-list CONTINUATION stop-set (design §1.3 / §1.6): the context-sensitive
    /// verbs that are <c>cobolWord</c>s (so they would otherwise be annexed as a spurious exception-name /
    /// file-name) yet ALSO lead a dispatcher statement (imperative-statement-2) —
    /// RESUME/RAISE/VALIDATE/UNLOCK/SEND/RECEIVE/COMMIT/ROLLBACK/ENTER lead a statement now; GET/PARSE are
    /// carried anticipatorily (future statement verbs) so the set is forward-complete.
    /// Pure reserved verbs (MOVE, ADD, DISPLAY, IF, PERFORM…) are NOT cobolWords, so the <c>cobolWord</c> grammar
    /// element itself stops the loop at them — no entry needed. The Format-3 phrase keywords FINALLY / OTHER /
    /// COMMON are likewise not cobolWords (FINALLY is a pure reserved keyword; OTHER/COMMON always were), so they
    /// stop the loop naturally too — no entry here. Completeness is enforced by <c>WhenOperandAheadDriftTests</c>.
    /// Single source of truth: the predicate iterates this array.
    /// </summary>
    public static readonly int[] WhenOperandStopTokens =
    {
        CobolLexer.RESUME, CobolLexer.RAISE, CobolLexer.VALIDATE, CobolLexer.UNLOCK, CobolLexer.SEND,
        CobolLexer.RECEIVE, CobolLexer.COMMIT, CobolLexer.ROLLBACK, CobolLexer.GET, CobolLexer.ENTER,
        CobolLexer.PARSE,
    };

    /// <summary>
    /// True when LT(1) MAY continue a Format-3 WHEN operand list — i.e. it is not one of
    /// <see cref="WhenOperandStopTokens"/>. Gates only the CONTINUATION of the operand list (the first operand
    /// after WHEN / WHEN EXCEPTION is taken unconditionally), so a WHEN body's leading verb cannot be annexed as
    /// a spurious operand. (ISO §14.9.28.2 Format 3; design §1.2–§1.5.)
    /// </summary>
    protected bool whenOperandAhead()
    {
        int la1 = TokenStream.LA(1);
        foreach (int stop in WhenOperandStopTokens)
            if (la1 == stop) return false;
        return true;
    }
}
