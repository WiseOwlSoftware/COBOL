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
    /// True when a SPECIAL-NAMES entry is the §12.3.7 <c>LOCALE locale-name-1 IS {external-locale-name-1 |
    /// literal-4}</c> clause — an <b>§A.4.9 item 10</b> optional-locale element ("SPECIAL-NAMES paragraph: LOCALE
    /// clause and LOCALE phrases in the ALPHABET clause"), which must be DIAGNOSED as documented non-support
    /// rather than fail as a parse error (fix-queue PB25).
    /// <para>⛔ WHY A TEXT PREDICATE AND NOT A LEXER TOKEN. LOCALE <i>is</i> reserved at 2002+ (§8.9;
    /// <c>reserved-words.json</c> r2002/r2014/r2023), so a token would be defensible — but the intrinsic side
    /// DEPENDS on it arriving as a bare word: <c>IntrinsicBinder.KeywordWordOf</c> detects
    /// <c>LOWER-CASE(x LOCALE …)</c> precisely because the phrase parses as ordinary space-separated arguments.
    /// Tokenizing LOCALE would silently break that already-working diagnostic, so the reservation stays modelled
    /// where the rest of §8.9 lives and this reads the word.</para>
    /// <para>⚠ EDITION-GATED DELIBERATELY. At COBOL-85 LOCALE is NOT reserved (r85 = false), so
    /// <c>SPECIAL-NAMES. LOCALE IS FOO.</c> is a legal implementor-switch entry there and must keep parsing as
    /// one. Below 2002 this returns false and the entry falls through to <c>implementorSwitchEntry</c> unchanged.
    /// </para>
    /// <para>The shape check (a following word, then IS or the value) is what keeps it off that 85 switch entry
    /// even when the predicate is reached: a bare <c>LOCALE IS FOO</c> has no locale-name and is not this clause.
    /// </para>
    /// </summary>
    /// <summary>True when the word after the CHARACTER token (already consumed) spells CLASSIFICATION — the
    /// OBJECT-COMPUTER CHARACTER CLASSIFICATION clause (ISO §12.3.6.2; kb/Work PB78). CLASSIFICATION is not a lexer
    /// token (a plain word at COBOL-85), so the arm is predicated on the text; every edition recognizes the shape
    /// and the binder rejects it as the A.4.9 documented non-support it is.</summary>
    protected bool classificationAhead() =>
        string.Equals(TokenStream.LT(1)?.Text, "CLASSIFICATION", StringComparison.OrdinalIgnoreCase);

    private static readonly string[] LocaleCategories =
        ["LC_ALL", "LC_COLLATE", "LC_CTYPE", "LC_MESSAGES", "LC_MONETARY", "LC_NUMERIC", "LC_TIME", "USER-DEFAULT"];

    private static bool Word(IToken? t, string text) => t is not null && string.Equals(t.Text, text, StringComparison.OrdinalIgnoreCase);

    /// <summary>SET LOCALE {LC_… | USER-DEFAULT} TO … (ISO §14.9.39 Format 11; kb/Work PB92) — a LEFT-EDGE predicate
    /// (LT(1) is the SET token itself): true when LT(2)/LT(3) spell LOCALE and a locale category and LT(4) is TO — the
    /// format's own keywords, plain words reserved 2002+ (§8.9); below 2002 they are user words and a SET of a data
    /// item named LOCALE keeps its '85 reading.</summary>
    protected bool setLocaleAhead()
    {
        if (!Word(TokenStream.LT(2), "LOCALE")) return false;
        if (TokenStream.LT(3) is not { } cat || !Array.Exists(LocaleCategories, c => string.Equals(cat.Text, c, StringComparison.OrdinalIgnoreCase))) return false;
        // The category operand is a SET (choice indicators): LT(3), LT(4), … are categories until the TO (bounded —
        // seven categories exist, so eight words is already malformed and the ordinary SET forms get it).
        int i = 4;
        while (i <= 10 && TokenStream.LT(i) is { } w && Array.Exists(LocaleCategories, c => string.Equals(w.Text, c, StringComparison.OrdinalIgnoreCase))) i++;
        if (!Word(TokenStream.LT(i), "TO")) return false;
        // ⚠ EDITION-GATED ONLY FOR THE USER-DEFAULT-FIRST SHAPE (kb/Work PB64 T1). `SET LOCALE LC_… TO x` has NO
        // COBOL-85 reading — LC_ALL … LC_TIME carry an underscore, which is not an '85 word character (§8.3.2.1;
        // Constructs.UserWordUnderscore2002) — so the shape is recognized at every edition and the ONE construct gate
        // (set-locale-2002, VisitSetLocaleStatement) answers below 2002 with the explanatory introduction diagnostic
        // instead of "'LOCALE' is not defined". `SET LOCALE USER-DEFAULT TO x` IS a legal '85 Format-1 statement (two
        // receivers named LOCALE and USER-DEFAULT), so that shape keeps its '85 reading below 2002.
        return cat.Text.StartsWith("LC_", StringComparison.OrdinalIgnoreCase) || Edition.Has(2002);
    }

    /// <summary>SET identifier-11 TO LOCALE {LC_ALL | USER-DEFAULT} (ISO §14.9.39 Format 12; kb/Work PB92) — a
    /// LEFT-EDGE predicate: scans past identifier-11 (a bounded token walk to the first TO before the statement's
    /// period) and answers true when the two words after TO spell LOCALE and LC_ALL / USER-DEFAULT.</summary>
    protected bool saveLocaleAhead()
    {
        // NOT edition-gated (kb/Work PB64 T1): `SET p TO LOCALE LC_ALL` has no '85 reading (LC_ALL's underscore) and
        // `SET p TO LOCALE USER-DEFAULT` would be a Format-1 SET followed by a stray word — a parse error either way;
        // recognizing the shape everywhere lets the set-save-locale-2002 gate answer below 2002.
        for (int i = 2; i <= 40; i++)
        {
            var t = TokenStream.LT(i);
            if (t is null || t.Type == TokenConstants.EOF || t.Type == CobolLexer.DOT) return false;
            if (Word(t, "TO"))
                return Word(TokenStream.LT(i + 1), "LOCALE")
                    && (Word(TokenStream.LT(i + 2), "LC_ALL") || Word(TokenStream.LT(i + 2), "USER-DEFAULT"));
        }
        return false;
    }

    /// <summary>PICTURE Format 2 (locale) — the word after the picture character-string spells LOCALE (ISO
    /// §13.18.40.2 `PIC IS character-string-1 LOCALE [IS locale-name-1] SIZE IS integer-1`; kb/Work PB100, live at
    /// PB64 T6).
    /// <para>⛔ NOT EDITION-GATED (the ORDER TABLE precedent, <see cref="orderTableAhead"/>): no data description
    /// clause begins with a user-defined word, so at '85 a word LOCALE immediately after a PIC_STRING can begin
    /// nothing else — there is no legal '85 reading to protect (unlike <see cref="localeClauseAhead"/>'s
    /// implementor-switch hazard, which does not exist inside a data description entry). Recognizing the phrase at
    /// every edition is what lets the ONE construct gate (<c>picture-locale-format2-2002</c>) answer below 2002
    /// with the explanatory introduction diagnostic instead of a raw ANTLR error at SIZE.</para></summary>
    protected bool pictureLocaleAhead() =>
        string.Equals(TokenStream.LT(1)?.Text, "LOCALE", StringComparison.OrdinalIgnoreCase);

    /// <summary>The SPECIAL-NAMES <c>ORDER TABLE ordering-name-1 IS literal-9</c> clause (ISO §12.3.7.2 — the last
    /// item of the paragraph's general format; kb/Work PB101). ORDER is not a lexer token (the same choice the
    /// LOCALE clause's keyword rests on — §8.9 reserves it from 2002 and the funnel models the reservation), so the
    /// clause is recognized here by the word pair; TABLE is a token.
    /// <para>⛔ NOT EDITION-GATED, AND THE LOCALE PRECEDENT DELIBERATELY DOES NOT APPLY. <see cref="localeClauseAhead"/>
    /// is gated because <c>SPECIAL-NAMES. LOCALE IS FOO.</c> is a legal COBOL-85 implementor-switch entry that must
    /// keep its '85 reading. There is no such reading here: <b>TABLE is reserved at EVERY edition</b>
    /// (<c>reserved-words.json</c> r85 true), so it can be neither a mnemonic-name after <c>ORDER</c> nor the first
    /// word of a following entry — the pair ORDER + TABLE cannot begin anything else in a '85 SPECIAL-NAMES
    /// paragraph. Recognizing it at every edition is what lets the ONE construct gate answer with the explanatory
    /// <c>order-table-2002</c> introduction diagnostic below 2002 instead of a raw parse error at TABLE
    /// (superset-parse / bind-narrow, and the two-obligation rule's diagnostic half).</para></summary>
    protected bool orderTableAhead() =>
        string.Equals(TokenStream.LT(1)?.Text, "ORDER", StringComparison.OrdinalIgnoreCase)
        && TokenStream.LT(2)?.Type == CobolLexer.TABLE;

    /// <summary>SET screen-name-1 ATTRIBUTE … (ISO §14.9.39.2 Format 6 — Annex A.4.2 item 24; kb/Work PB260) —
    /// a LEFT-EDGE predicate (LT(1) is the SET token itself): true when the word ATTRIBUTE stands somewhere in
    /// the receiver position, i.e. after a bounded walk over screen-name-1 and any qualifiers.
    /// <para>ATTRIBUTE is a §8.10 CONTEXT-SENSITIVE word ("SET statement"), never reserved, so it is not a lexer
    /// token — the same reason ORDER and LOCALE are read as text here.</para>
    /// <para>⛔ NOT EDITION-GATED, the ORDER TABLE precedent. `SET x ATTRIBUTE …` has no other reading at any
    /// edition: Format 1 requires TO, Format 2 requires UP/DOWN BY, and a bare `SET x y` is not a SET at all — so
    /// there is no COBOL-85 reading to protect, and recognizing the shape everywhere is what lets the named
    /// COBOLNET1707 refusal replace a raw parse error below 2002 as well as above it.</para></summary>
    protected bool setAttributeAhead()
    {
        for (int i = 2; i <= 12; i++)
        {
            var t = TokenStream.LT(i);
            if (t is null || t.Type == TokenConstants.EOF || t.Type == CobolLexer.DOT) return false;
            if (Word(t, "ATTRIBUTE")) return i > 2;   // i == 2 would mean SET *with no receiver*
        }
        return false;
    }

    /// <summary>True when the next token opens the ACCEPT-format-3 / DISPLAY-format-2 POSITIONING PHRASE (ISO
    /// §14.9.1.2 / §14.9.11.2: <c>[AT {|[LINE NUMBER …] [{COLUMN|COL} NUMBER …]|}]</c>) — the guard that stops
    /// the DISPLAY operand loop from swallowing it.
    /// <para>⛔ WHY THIS IS SAFE RATHER THAN A HEURISTIC. AT, LINE and COLUMN are §8.9 reserved words at EVERY
    /// edition, so none of them can be a user-defined word and none can legally begin a DISPLAY operand; the
    /// plural/abbreviated spellings COL, COLS and COLUMNS are reserved only from 2002, so they are tested
    /// through <see cref="reservedHere"/> and stay ordinary user words at COBOL-85 (where <c>DISPLAY COLS</c> is
    /// legal and must keep binding as a one-operand device DISPLAY). Without this, `DISPLAY SG COLUMN 5` bound
    /// as a three-operand device DISPLAY while `DISPLAY SG COLUMN NUMBER 5` was a parse error — one construct,
    /// two non-diagnoses (kb/Work PB260).</para></summary>
    protected bool screenPositionAhead()
    {
        var t = TokenStream.LT(1);
        if (t is null) return false;
        return t.Type switch
        {
            CobolLexer.AT or CobolLexer.LINE or CobolLexer.COLUMN => true,
            CobolLexer.COL or CobolLexer.COLS or CobolLexer.COLUMNS => reservedHere(t.Text),
            _ => false,
        };
    }

    protected bool localeClauseAhead()
    {
        if (!string.Equals(TokenStream.LT(1)?.Text, "LOCALE", StringComparison.OrdinalIgnoreCase)) return false;
        // LOCALE <locale-name> [IS] <external-locale-name | literal> — the second token is the locale-name, a WORD,
        // so the '85 implementor-switch shapes `LOCALE IS x` / `LOCALE ON|OFF STATUS …` (IS/ON/OFF are tokens) are
        // excluded. ⚠ NOT edition-gated since kb/Work PB64 T1 (it was, because "SPECIAL-NAMES. LOCALE IS FOO." is a
        // legal '85 switch entry — but that shape is excluded HERE by the word test, and `LOCALE FR IS "fr_FR"` has
        // no '85 reading at all): recognizing the clause at every edition is what lets the ONE construct gate
        // (special-names-locale-2002, VisitLocaleClause) answer below 2002 with the explanatory introduction
        // diagnostic instead of a parse error at the clause's own literal — the ORDER TABLE precedent.
        return TokenStream.LT(2) is { } second
            && second.Type != CobolLexer.IS && second.Type != CobolLexer.ON && second.Type != CobolLexer.OFF
            && second.Type != CobolLexer.DOT && second.Type != TokenConstants.EOF
            && !string.Equals(second.Text, "IS", StringComparison.OrdinalIgnoreCase);
    }

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
    /// <summary>True when <paramref name="keyword"/> is a §8.9 reserved word at the compile edition — the
    /// cobolWord exclusion predicate (kb/Work PB137): a word the edition reserves cannot be a user-defined
    /// word, so the alternative that would absorb it into an operand list must not match.</summary>
    protected bool reservedHere(string keyword)
        => ReservedWords.Find(keyword)?.IsReservedAt(Edition.Year) ?? false;

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

    /// <summary>The ARGUMENT-scoped twin of <see cref="boolExprAhead"/> (kb/Work PB65, FMT-15.45.2): does a boolean
    /// operator belong to THIS intrinsic-function argument? §8.4.3.2.3 SR8 admits "a boolean expression" as
    /// argument-1 and §15.3 item 3 names it for a Boolean argument (INTEGER-OF-BOOLEAN(BIT-A B-AND BIT-B)). The
    /// scan stops where the ARGUMENT ends — a depth-0 comma, the argument list's ')', or a SPACE-SEPARATED
    /// boundary: two adjacent operand terms with no operator between them (kb/Work PB124, AR-15.3-3 — boolean
    /// expressions connect every term with a B-operator, so `CONCAT(WS-A B1 B-AND B2)` ends WS-A's argument at
    /// B1 and the B-AND belongs to the LATER argument; the old scan predicated EVERY argument of the list into
    /// the boolean alternative once ANY later one held a depth-0 B-operator). A '(' directly after a term is
    /// that term's subscript/ref-mod — the parser's own reading — never a new term. `COMPUTE BR = FUNCTION
    /// BOOLEAN-OF-INTEGER(5, 8) B-AND BB` still does not mistake its numeric argument for a boolean one.
    /// The 512-token cap is the implementor's scan bound: with the boundary arm the scan ends at the first
    /// adjacent-term pair, so only ONE argument written as a single >512-token boolean expression could ever
    /// reach it (it would fall to the non-boolean alternative and draw a bind diagnostic, never parse silently
    /// wrong).</summary>
    protected bool boolArgAhead()
    {
        int prev = 0, depth = 0;
        for (int i = 1; i <= 512; i++)
        {
            int t = TokenStream.LA(i);
            if (depth == 0 && IsBoolOperandTerm(prev) && IsTermStartToken(t))
                return false;   // adjacent operand terms — a space-separated argument boundary (PB124)
            switch (t)
            {
                case CobolLexer.LPAREN: case CobolLexer.FNARG_LPAREN: depth++; break;
                case CobolLexer.RPAREN: case CobolLexer.FNARG_RPAREN:
                    if (depth == 0) return false;   // the argument list's ')' — this argument is over
                    depth--; break;
                case CobolLexer.COMMA:
                    if (depth == 0) return false;   // the next argument
                    break;
                case CobolLexer.B_AND:
                case CobolLexer.B_OR:
                case CobolLexer.B_XOR:
                case CobolLexer.B_SHIFT_L:
                case CobolLexer.B_SHIFT_R:
                case CobolLexer.B_SHIFT_LC:
                case CobolLexer.B_SHIFT_RC:
                    if (IsBoolOperandTerm(prev)) return true;
                    break;
                case CobolLexer.B_NOT:
                    if (IsBoolOperandStart(TokenStream.LA(i + 1))) return true;
                    break;
                case CobolLexer.DOT:
                case TokenConstants.EOF:
                    return false;
            }
            prev = t;
        }
        return false;
    }

    /// <summary>An operand-ENDING token — an operand can end with an identifier, a right paren, or a literal. Used by
    /// <see cref="boolExprAhead"/> to confirm a binary B-operator has a genuine LEFT operand (so a §8.9 user data-name
    /// spelled B-AND/B-OR/B-XOR heading a comparison below 2002 is not mistaken for the operator).</summary>
    /// <remarks>FNARG_RPAREN is the function ARGUMENT-LIST ')' (ISO §8.4.3.2.3 SR6; fix-queue PB48) — the same
    /// character, a different token type. A function result IS an operand, so `FUNCTION f(x) B-AND y` ends its
    /// left operand on that token; without it here the predicate would answer false and the whole condition
    /// would take the comparison path instead. Found by sweeping every consumer of the plain paren types after
    /// the legacy binder's copy of this same omission cost 31 NIST regressions.</remarks>
    private static bool IsBoolOperandTerm(int t) => t is
        CobolLexer.IDENTIFIER or CobolLexer.RPAREN or CobolLexer.SUB_RPAREN or CobolLexer.FNARG_RPAREN
        or CobolLexer.INTEGERLIT or CobolLexer.DECIMALLIT or CobolLexer.FLOATLIT or CobolLexer.COMMA_FLOATLIT
        or CobolLexer.STRINGLIT or CobolLexer.NATLIT or CobolLexer.HEXLIT or CobolLexer.BOOLLIT
        or CobolLexer.SIGNED_INTEGERLIT or CobolLexer.SIGNED_DECIMALLIT;

    /// <summary>A token that STARTS A NEW TERM when it directly follows a completed operand term (kb/Work
    /// PB124 — <see cref="boolArgAhead"/>'s space-separated argument boundary): an identifier, any literal,
    /// figurative ZERO, a unary B-NOT, or the FUNCTION keyword opening a nested call. Deliberately NOT
    /// LPAREN — a paren straight after a term is that term's subscript/ref-mod, the parser's own reading.</summary>
    private static bool IsTermStartToken(int t) => t is
        CobolLexer.IDENTIFIER or CobolLexer.B_NOT or CobolLexer.ZERO or CobolLexer.FUNCTION
        or CobolLexer.INTEGERLIT or CobolLexer.DECIMALLIT or CobolLexer.FLOATLIT or CobolLexer.COMMA_FLOATLIT
        or CobolLexer.STRINGLIT or CobolLexer.NATLIT or CobolLexer.HEXLIT or CobolLexer.BOOLLIT
        or CobolLexer.SIGNED_INTEGERLIT or CobolLexer.SIGNED_DECIMALLIT;

    /// <summary>An operand-STARTING token — a boolean operand can start with an identifier, '(', a nested B-NOT, a
    /// boolean literal, or figurative ZERO. Used to confirm a prefix B-NOT is a genuine unary operator.</summary>
    /// <remarks>GROUPING-PAREN-ONLY (fix-queue PB48): FNARG_LPAREN is deliberately absent. An operand that
    /// STARTS with a parenthesis is a parenthesized sub-expression, and that paren is always a plain LPAREN; a
    /// function operand starts with the FUNCTION keyword or its name, never with the argument-list '('.</remarks>
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
                case CobolLexer.COMMA_FLOATLIT:
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
