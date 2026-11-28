// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Runtime;
using static WiseOwl.Cobol.Frontend.Parsing.CobolLexer;

namespace WiseOwl.Cobol.Frontend.Parsing;

/// <summary>
/// The hand-written superclass of the ANTLR-generated <c>CobolLexer</c> (<c>superClass</c> in
/// <c>Grammar/Core/CobolLexer.g4</c>; kb/Work PB2726): the lexer's state machine, which the grammar's token actions call as
/// one-line methods — the paren-region classifier (reference, argument and group parens; the list-region separator and
/// signed-literal decisions), the REPORT SECTION region, the <c>&gt;&gt;COBOL-WORDS</c> plan and the Annex C edition
/// spelling applied to each token as it is emitted (<see cref="NextToken"/>), the compiler-directive-expression prime, the
/// ADDRESS OF FUNCTION tracking, PICMODE's IS and character-string, and the cut of a match back to a prefix. The parser has
/// the same shape (<c>CobolParserCoreBase</c>): the behaviour is analyzer-visible C# source, never a block copied into a
/// generated file, and its word-set data is generated beside it (<see cref="CobolLexerWordSet"/>).
/// </summary>
public abstract class CobolLexerBase : Lexer
{
    protected CobolLexerBase(ICharStream input, TextWriter output, TextWriter errorOutput)
        : base(input, output, errorOutput)
    {
    }

    // Where each inline comment the lexer met begins, its 1-based resultant line and 0-based column, for the source listing
    // (DESIGN-listing.md section 7.2; kb/Work PB5949): a comment is SKIPPED, so no token says where it stands, and the listing
    // colours it from this record, which the comment rules write, never from a pattern over the text. Null, the default, records
    // nothing, so a compilation without a listing does no work for it (section 9).
    internal List<(int Line, int Column)>? CommentStarts { get; set; }

    // The action of the comment rules: record the comment's start when a record is kept.
    private protected void OnCommentStart() => CommentStarts?.Add((TokenStartLine, TokenStartColumn));

    // Track the types of the last TWO non-WS tokens emitted: one for the reference-paren decision, two for the
    // FUNCTION-argument suppression (P7 Step 12 — '(' after "FUNCTION name" opens an ARGUMENT list, ISO
    // §2023:8.4.3.2.3 SR6, lexed in DEFAULT mode so arguments parse as real arithmeticExpressions).
    private int _lastNonWsTokenType = -1;
    private int _prevNonWsTokenType = -1;

    // THE REFERENCE-PAREN TRIGGER: whitelist approach.
    //
    // In COBOL, '(' after a data-name means subscript/reference-modification; '(' after anything else means
    // arithmetic grouping (e.g., IF (A + B) > C). The whitelist is IDENTIFIER plus every context-sensitive
    // keyword that can appear as a user-defined data-name. Safe failure mode: a token missing from the set makes
    // the parser see LPAREN/RPAREN (arithmetic; GROUPING-PAREN-ONLY) instead of REF_LPAREN/REF_RPAREN — a clear parse error on the
    // first subscripted use.
    // ⛔ WHY THE DECISION STAYS IN THE LEXER (kb/Work PB2113, DESIGN-frontend-grammar §9.4): ISO §2023:8.3.5 SR1 makes the
    // SPACE a separator, so `X (I J)` is two subscripts and `X (I -1)` a subscript and the literal -1, while
    // `X (I - 1)` is one subtraction (§2023:8.7.1 spaces an operator, §2023:8.3.3.3.2 puts a literal's sign at its leftmost
    // character). The parser sees the skipped-WS stream, so only the lexer can tell `-1` from `- 1`; it does that
    // inside a LIST REGION (an argument list or a reference's parenthesized tail) and nowhere else, and it must know
    // which parens open one. What it no longer does is lex the region's CONTENT in a mode of its own.
    //
    // The set (CobolLexerWordSet.DataNameTokens) is GENERATED into Parsing/CobolLexerWordSet.g.cs from
    // tests/version-matrix/cobol-words.json (the subscriptTrigger=true rows) by scripts/gen-cobol-words.ps1
    // (rearchitecture PHASE 04, Group A) — single-sourced with the parser cobolWord rule (Grammar/Core/CobolWords.g4)
    // and cross-checked by CobolWordsDriftTests, retiring the by-hand "mirror cobolWord" discipline this comment
    // used to instruct. Do NOT hand-add a token anywhere: edit cobol-words.json and re-run the generator.

    private bool PreviousTokenCouldBeDataName()
        => CobolLexerWordSet.DataNameTokens.Contains(_lastNonWsTokenType) && !_reservedNonNames.Contains(_lastNonWsTokenType);

    // ⛔ A WORD THIS COMPILE RESERVES IS NEVER A DATA-NAME, SO A '(' AFTER IT IS NEVER A SUBSCRIPT (kb/Work PB1465).
    // CobolLexerWordSet.DataNameTokens is edition-blind: it holds every word that is a user-defined word in SOME edition, because
    // the reference-paren decision is frozen here, before any parser predicate can ask the edition. That made the '(' after
    // a boolean OPERATOR — B-AND / B-OR / B-XOR / B-NOT, user words at COBOL-85 and reserved by ISO §2023:8.9 from 2002 —
    // open a reference paren, so `IF BZ B-OR (BW B-AND BW)` and `COMPUTE BR = B-NOT (BW B-XOR BZ)` were COBOL0001
    // although §2023:8.8.2's Table 4 admits '(' after every boolean operator. §2023:8.3.2.1 SR1 ("Reserved words shall not
    // be used as user-defined words") settles it for EVERY reservation-gated word at once: the compile's own answer
    // to "is this word a user-defined word here" (ReservedWordSet.AdmitsAsUserWord — the question the parser's
    // userWordHere gate and the §2023:8.9 funnel ask) is handed in by TokenRetypes.PrimeLexer, and a word it refuses
    // stops triggering. An intrinsic-function-name word keeps its trigger (a keyword-omitted call, §2023:8.4.3.2.3 SR2),
    // which is why PrimeLexer excludes the intrinsicKeywordName rule's own tokens rather than this lexer listing them.
    private readonly HashSet<int> _reservedNonNames = [];
    public void SetReservedNonDataNames(IReadOnlySet<int> types)
    {
        _reservedNonNames.Clear();
        foreach (int t in types) _reservedNonNames.Add(t);
    }

    /// <summary>The edition-blind reference-paren trigger set (every word that is a data-name in some edition), for
    /// TokenRetypes.PrimeLexer to narrow per compile.</summary>
    public static IReadOnlySet<int> SubscriptTriggerTokens => CobolLexerWordSet.DataNameTokens;

    // ⛔ THE REPORT-WRITER SUM CLAUSE IS NOT THE SUM FUNCTION (kb/Work PB924). SUM carries subscriptTrigger for
    // §2023:8.4.3.2.3 SR2's keyword-omitted intrinsic call `SUM(1 2 3)`, and a reserved word can never be a data-name,
    // so that trigger exists ONLY for the function. ISO §2023:13.18.54.3 SR9: "Within a report description entry, if
    // the keyword SUM is preceded by the keyword FUNCTION, SUM is a reference to the SUM intrinsic function.
    // Otherwise, SUM refers to the report writer SUM clause" — so inside the REPORT SECTION a bare SUM's '(' can
    // only open a parenthesised addend (arithmetic-expression-1), and capturing it as a reference paren made
    // `SUM (WS-K * WS-M)` unparseable while `SUM WS-K * WS-M` compiled. SR9's discriminator is the REGION, which
    // is why the trigger is made contextual here rather than recovered after the fact: the reference-paren decision
    // is frozen at lex time. The FUNCTION-keyword form is unaffected (PreviousIsFunctionName keys on FUNCTION
    // before the name, and SUM then stays the argument-list FNARG_LPAREN), and outside the REPORT SECTION the
    // keyword-omitted SUM(…) still captures exactly as before. SUM is the only §2023:8.9 ∩ §2023:8.11 word (LENGTH,
    // RANDOM, SIGN, SUM) that begins a report or data description clause whose operand can open with '(' — the
    // SIGN clause (§2023:13.18.52) takes none — so it is the only word the region changes (CobolLexerModeDriftTests).
    // ⛔ BUT ONLY WHERE A CLAUSE CAN BEGIN (kb/Work PB5652). A clause keyword stands after something has ended — the
    // entry's name or level, or the last word of the clause before — never where an OPERAND is still owed: inside an
    // open parenthesis, after an operator, or after the word that introduces a report clause's operand (SOURCE IS,
    // SOURCES ARE, SUM OF, PRESENT WHEN, VARYING … FROM … BY, a relational operator). There `SUM (` can only be a
    // function-identifier, so its '(' is the keyword-omitted call's argument list, as everywhere else. The EDITION
    // decides nothing here: COBOL 2002 has no SR9, and §2002:12.2.7.3 GR13 lets a REPOSITORY-declared SUM drop the
    // word FUNCTION in a report entry as anywhere (`SOURCE WA + SUM(WB WC)` is legal 2002); from 2014
    // (§2014:13.18.54.2 SR9) the same tree is the SUM clause written where no clause can stand, which the binder
    // refuses by name (IntrinsicBinder, COBOL7933) rather than leaving a raw parse error. Where SUM CAN begin the
    // clause — after a complete operand, `SUM A SUM (B * C)` — the clause reading stays at every edition: SR9's
    // reading, and the one 2002's repeated-SUM format (§2002:13.16.52.2 SR1) leaves open.
    private bool _inReportSection;
    private bool IsReportSumClauseKeyword() =>
        _inReportSection && _lastNonWsTokenType == SUM
        && _parenRegions.Count == 0 && !ReportOperandFollows.Contains(_prevNonWsTokenType);

    // The tokens after which a report clause still owes an operand (the arithmetic operators of ListOperandFollows,
    // and the words that introduce a report clause's expression or condition operand). The separators are NOT here:
    // a comma or semicolon may stand between two clauses (§2023:8.3.5), so a SUM after one may begin the clause.
    // CobolLexerModeDriftTests pins the members that decide a report-entry SUM.
    private static readonly HashSet<int> ReportOperandFollows =
    [
        PLUS, MINUS, STAR, SLASH, POWER, AMPERSAND, OF, IN,
        SOURCE, SOURCES, IS, ARE, SUM, WHEN, FROM, BY,
        EQUALS, GT, LT, GTEQUAL, LTEQUAL, NOTEQUAL, EQUAL, GREATER, LESS, THAN, TO, AND, OR, NOT,
    ];

    // The REPORT SECTION region: opened by the `REPORT SECTION` header, closed by the next section or division
    // header — the data division's fixed section order puts only the SCREEN SECTION or the PROCEDURE DIVISION after
    // it, and a nested program's REPORT SECTION follows its own header again.
    private void TrackReportSection(int type)
    {
        if (type == SECTION) _inReportSection = _lastNonWsTokenType == REPORT;
        else if (type == DIVISION) _inReportSection = false;
    }

    // ⛔ >>COBOL-WORDS (ISO §2023:7.3.10.4 GR2/GR3/GR4) IS APPLIED HERE, TO EACH TOKEN AS IT IS EMITTED (kb/Work PB1372).
    // Every decision this lexer takes from a keyword — PIC's mode switch, the FUNCTION argument region, the
    // reference-paren trigger, the REPORT SECTION region — is frozen at lex time, so a retype that ran after lexing could
    // reach none of them: a synonym of PIC/PICTURE never entered PICMODE, a synonym of FUNCTION never opened an
    // argument region, a de-reserved PICTURE still swallowed the next word as a picture string, and the one seam that
    // existed (a set of de-reserved types that made '(' subscript) covered only the last of them. NextToken retypes
    // the token FIRST (CobolWordsRewriter.Plan.Apply — synonym → its keyword's type, de-reserved word → IDENTIFIER),
    // then records the EFFECTIVE type as the previous token and acts on it, so every one of those decisions reads the
    // word the directive made of it, and no later stage re-applies the directive to a token. The default plan is
    // empty: nothing is retyped and the lexer is byte-identical to one that knows no directive.
    private CobolWordsRewriter.Plan _cobolWords = CobolWordsRewriter.Plan.Empty;
    public void SetCobolWords(CobolWordsRewriter.Plan plan) => _cobolWords = plan;

    // ⛔ A WORD IS EMITTED IN ITS EDITION'S ANNEX C SPELLING (kb/Work PB1402). Every word comparison downstream folds
    // through the ONE comparer, CobolNames, whose table is COBOL 2023's Annex C. Below 2023 the edition's Annex C
    // differs observably in one place — E.2 item 14 deleted (0131,0069) and (03C2,03C3) — so a COBOL 2002/2014 word
    // is written here with those letters already folded (CobolNames.EditionSpelling), and from then on the one
    // comparer gives that edition's answer. Default 2023: the spelling is the source's, unchanged.
    private int _caseMappingEdition = 2023;
    public void SetCaseMappingEdition(int dialectLevel) => _caseMappingEdition = dialectLevel;

    private void ApplyEditionSpelling(IToken token)
    {
        if (!CobolNames.HasEditionSpelling(_caseMappingEdition) || token.Type != IDENTIFIER
            || token is not CommonToken word) return;
        // A word of basic characters has one spelling at every edition: look at the input, build no text.
        bool basic = true;
        for (int p = word.StartIndex; p <= word.StopIndex && basic; p++)
        {
            int offset = p - InputStream.Index;   // LA(k > 0) is the k-th character from the index, LA(-k) k before it
            basic = InputStream.LA(offset >= 0 ? offset + 1 : offset) < 0x80;
        }
        if (basic) return;
        string text = word.Text;
        string spelled = CobolNames.EditionSpelling(text, _caseMappingEdition);
        if (!ReferenceEquals(spelled, text)) word.Text = spelled;
    }

    // THE LIST REGIONS (P7 Step 12; kb/Work PB2113). '(' after "FUNCTION functionName" is the function's argument-list
    // paren (ISO §2023:8.4.3.2.3 SR6); '(' after a word that can name a data item is that reference's parenthesized tail —
    // its subscripts (§2023:8.4.2.3.2), its reference modifier (§2023:8.4.3.3.2) or, for the keyword-omitted form name(args)
    // (§2023:8.4.3.2.3 SR2, D2), its arguments. Both are LIST regions: the §2023:8.3.5 SR2 comma/semicolon separator survives
    // as FNARG_SEPARATOR and a sign touching its digits after a separator is a signed literal (SignedLiteralCanStart),
    // so juxtaposed operands stay apart. Every token inside either region is the DEFAULT-mode vocabulary. The paren
    // stack tracks what each OPEN paren is, so the matching ')' takes the same type and the region tests are exact.
    private bool PreviousIsFunctionName() => _prevNonWsTokenType == FUNCTION && !_functionIsAddressOperand;

    // ⛔ THE FUNCTION OF `ADDRESS [OF] FUNCTION word` DOES NOT HEAD A FUNCTION-IDENTIFIER (kb/Work PB1416). §2023:8.4.3.12.2
    // prints `ADDRESS OF FUNCTION { function-prototype-name-1 | identifier-1 }`: the word after FUNCTION is a prototype
    // name, which takes no argument list, or identifier-1, which §2023:8.4.3.1.3 SR1 makes ANY identifier format — so a '('
    // after it is that identifier's SUBSCRIPT or reference modifier, never §2023:8.4.3.2.3 SR6's argument paren. Retyping
    // it FNARG_LPAREN kept it from the reference paren that subscriptPart reads (REF_LPAREN), and
    // `ADDRESS OF FUNCTION WS-NAME(2)` died COBOL0001. Set as each FUNCTION token is emitted (NextToken), from the
    // two tokens before it: ADDRESS, or ADDRESS OF (OF is optional — §2023:8.4.3.12.2 does not underline it).
    private bool _functionIsAddressOperand;

    private enum ParenRegion : byte { Group, Arguments, Reference }
    private readonly List<ParenRegion> _parenRegions = [];
    private int _openListRegions;   // the open Arguments + Reference entries of _parenRegions

    // COMPILER-DIRECTIVE EXPRESSION region (compile-time expression evaluator, ISO §2023:7.3.6/§2023:7.3.7/§2023:7.3.8). The
    // frontend fragment-parses a directive operand / constant-conditional-expression with the lexer primed here.
    // Two directive-only effects (both context-sensitive — nil global blast radius): (1) 'DEFINED' becomes a
    // token (the §2023:7.3.8.4.4 defined-condition keyword — reserved nowhere else); (2) every '(' is a grouping
    // LPAREN, never a subscript. Effect (2) is required because the subscript-vs-grouping decision treats a '('
    // after any word that COULD be a data-name as a subscript, and the boolean operators (B-AND …) are legal
    // data-names below 2023, so `A B-AND (…)` would mis-lex the group as a reference paren; a directive operand
    // never subscripts, so every '(' is unambiguously a group (confirmed by token dump, DESIGN §4.1): GROUPING-PAREN-ONLY.
    private bool _primeDirectiveExpr;

    private bool InListRegion() => _openListRegions > 0;

    // ⛔ A LIST SEPARATOR IS A BOUNDARY ONLY BETWEEN AN ITEM THAT HAS ENDED AND ONE THAT BEGINS (kb/Work PB5793). The comma or
    // semicolon followed by a space is the space separator (§2023:8.3.5 SR2), so inside a list region it is kept as a token
    // only where a space there would separate two items: never after a token after which an operand must follow (an
    // opening parenthesis, an operator, a qualifier connective, another separator: ListOperandFollows), and never before
    // a closing parenthesis or an operator written with its spaces — `(A, * B)` is `(A * B)`, as `(A - 1)` is one
    // subtraction (§2023:8.7.1), while `(A, -1)` keeps the boundary before its signed literal (§2023:8.3.3.3.2).
    // ⛔ BOTH SIDES OF A WORD OPERATOR (kb/Work PB5793, review R1-1): a qualifier connective or a binary boolean operator
    // is a WORD, so the test before it reads the word ahead (InfixWordAhead), the twin of ListOperandFollows's OF/IN/B-AND
    // members after it: `(I, OF G)` is `(I OF G)`, as `(I OF, G)` is. B-NOT is unary and BEGINS an operand, so a
    // separator before it stays the boundary.
    // Asked by FNARG_SEPARATOR's action (OnListSeparator), after the match: LA(1) is the character after the separator's spaces.
    private bool ListItemBoundary()
    {
        if (ListOperandFollows.Contains(_lastNonWsTokenType)) return false;
        int next = InputStream.LA(1), after = InputStream.LA(2);
        return next switch
        {
            ')' or '*' or '/' or ':' or '&' => false,
            '+' or '-' => after != ' ' && after != '\n',
            _ => !InfixWordAhead(),
        };
    }

    // The words that stand only BETWEEN two operands of a list item: the qualifier connectives (§2023:8.4.2.2.2) and the
    // binary boolean operators (§2023:8.8.2), spelled as their keyword rules spell them. Matched case-insensitively
    // (the grammar's caseInsensitive), and only as a whole word: `OFX`, `INDEX-1`, `B-ANDER` are names.
    // CobolLexerModeDriftTests.ListSeparator_BeforeAnInfixWord_IsSkipped pins every member.
    private static readonly string[] InfixWords =
        ["OF", "IN", "B-AND", "B-OR", "B-XOR", "B-SHIFT-LC", "B-SHIFT-RC", "B-SHIFT-L", "B-SHIFT-R"];

    private bool InfixWordAhead()
    {
        foreach (string word in InfixWords)
        {
            int i = 0;
            while (i < word.Length && char.ToUpperInvariant((char)InputStream.LA(i + 1)) == word[i]) i++;
            if (i == word.Length && !IsWordCharacter(InputStream.LA(i + 1))) return true;
        }
        return false;
    }

    // A character that continues a COBOL word (§2023:8.3.2.1: basic letters and digits, extended letters, hyphen and underscore).
    private static bool IsWordCharacter(int c) =>
        c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-' or '_' || c > 127;

    // The tokens after which an OPERAND must follow, so a separator after one stands where only a space can and
    // separates nothing: the three opening parentheses, the arithmetic and concatenation operators (§2023:8.8.1.1, §2023:8.8.3),
    // the boolean operators (§2023:8.8.2), the reference modifier's colon, the OF/IN qualifier connectives (§2023:8.4.2.2.2)
    // and a separator itself (more than one space is one separator, §2023:8.3.5 SR1). CobolLexerModeDriftTests
    // pins every member.
    private static readonly HashSet<int> ListOperandFollows =
    [
        LPAREN, REF_LPAREN, FNARG_LPAREN, PLUS, MINUS, STAR, SLASH, POWER, AMPERSAND, COLON, OF, IN,
        B_AND, B_OR, B_XOR, B_NOT, B_SHIFT_LC, B_SHIFT_RC, B_SHIFT_L, B_SHIFT_R, FNARG_SEPARATOR, COMMA,
    ];

    // Fragment re-parse hook (the compile-time directive-expression parse): treat the WHOLE input as a directive
    // operand / cce region — DEFINED is a token and every '(' groups (DESIGN §4.1).
    public void PrimeDirectiveExpr() => _primeDirectiveExpr = true;

    // IDENTIFIER's action in a primed directive expression: the §2023:7.3.8.4.4 keyword DEFINED is the one word the region
    // makes a token (a virtual token — see the tokens block). Reached only when primed, so the Text it reads is never
    // built for an ordinary source identifier. ⛔ A KEYWORD MATCHES ON THE BASIC LETTERS ALONE (CobolKeywords; ISO §2023:8.1.3.2
    // 3) a) and 4), DESIGN-frontend-grammar §3.13, kb/Work PB4240): the Annex C fold of an extended letter governs
    // user-defined words only, so DEF<U+0130>NED is not DEFINED, exactly as the keyword rules (caseInsensitive) read every
    // other keyword (kb/Work PB4420: and the fold applies no pair from an extended letter to a basic one, so no user-defined
    // word folds to DEFINED either). The source's text is compared, never an edition spelling.
    private void RetypeDirectiveKeyword()
    {
        if (CobolKeywords.Same(Text, "DEFINED")) Type = DEFINED;
    }

    // IDENTIFIER's action: a primed directive expression's one keyword (RetypeDirectiveKeyword); anywhere else a word.
    private protected void OnIdentifier()
    {
        if (_primeDirectiveExpr) RetypeDirectiveKeyword();
    }

    // FNARG_SEPARATOR's action: a comma or semicolon separator is a token only where it is a list-item boundary
    // (ListItemBoundary); everywhere else it is the space separator it stands for, and is skipped.
    private protected void OnListSeparator()
    {
        if (!InListRegion() || !ListItemBoundary()) Skip();
    }

    // ⛔ AN ARGUMENT-LIST PARENTHESIS IS NOT AN ARITHMETIC PARENTHESIS (GROUPING-PAREN-ONLY), AND EMITTING BOTH AS LPAREN MADE EVERY
    // DOWNSTREAM PASS GUESS (fix-queue PB48). §2023:8.4.3.2.3 SR6: "If a function's definition permits arguments and a
    // left parenthesis immediately follows function-prototype-name-1 or intrinsic-function-name-1, the left
    // parenthesis is ALWAYS treated as the left parenthesis of that function's arguments" — categorically, so it
    // is never the '(' of a parenthesized arithmetic expression. ZeroTokenRewriter reads adjacency to '(' / ')'
    // as proof that a figurative ZERO sits inside parenthesized arithmetic, which is true for a grouping paren and
    // FALSE for this one; the result was that `FUNCTION LOWER-CASE(ZERO)` reached the binder as an arithmetic
    // zero and was rejected as class numeric, while the keyword-omitted `LOWER-CASE(ZERO)` — whose arguments were
    // then re-lexed from their text, where no parens are present — returned the correct "0". Two arms of one
    // dispatch, one right (feedback_two_arm_dispatch).
    // ⛔ THE FIX IS THE TOKEN TYPE, NOT A SECOND COPY OF THE PREDICATE. The rewriter could have re-derived "is
    // this '(' an argument list" by walking back two tokens, but that is this method's rule written down twice,
    // and the reference-paren decision above proves how easily the two copies drift (feedback_one_rule_one_place). The
    // stack below is the single source of truth; retyping publishes it, so the rewriter's own rule — "ZERO
    // adjacent to an ARITHMETIC paren is arithmetic" — becomes true as written, with no change to it at all.
    // The reference paren is published the same way, and for the same reason (kb/Work PB2113): REF_LPAREN is the
    // paren after a data-name-capable word, so the parser's subscriptPart / refModPart take exactly the parens this
    // method classified, and a grouping paren after a ')' (`MAX(T (1) (A + B))`, two arguments) never becomes a
    // second subscript list.
    private protected void OnDefaultLParen()
    {
        // A directive-expression region never subscripts: every '(' groups (DESIGN §4.1). Checked first so a '('
        // after a boolean-operator data-name (e.g. `A B-AND (…)`, B-AND legal as a name below 2023) stays a group.
        // ⛔ A WORD RIGHT AFTER THE INVOCATION OPERATOR `::` IS literal-1, NEVER A DATA-NAME (kb/Work PB780): §2023:8.4.3.4.2
        // prints `identifier-1 :: literal-1 [ ( … ) ]`, and a constant-name spelling literal-1 (§2023:13.10.3 SR2) is the one
        // word that stands there — so its '(' is the argument-list paren the written literal's '(' is, and `O :: K (X)`
        // lexes exactly as `O :: "M" (X)` does.
        if (!_primeDirectiveExpr && PreviousTokenCouldBeDataName() && !PreviousIsFunctionName()
            && _prevNonWsTokenType != COLONCOLON && !IsReportSumClauseKeyword())
        {
            OpenRegion(ParenRegion.Reference);
            Type = REF_LPAREN;   // subscripts / reference modifier / keyword-omitted arguments; its ')' is REF_RPAREN
            return;
        }
        // Not a reference: record whether this paren opens a FUNCTION argument region, and publish it as the type.
        // ⚠ THE `&& PreviousTokenCouldBeDataName()` CONJUNCT THAT STOOD HERE IS GONE, and dropping it is part of
        // the fix rather than a tidy-up. SR6 keys on the '(' following an intrinsic-function-name — it says
        // nothing about whether that name's TOKEN happens to be one of the context-sensitive words that can also
        // be a data name. Of the nine `intrinsicKeywordName` tokens, eight are in DataNameTokens and BIT is not, so
        // the conjunct silently made `FUNCTION BIT(…)` the one function call whose parens were not an argument
        // region — a difference with no rule behind it, and exactly the sort of accidental exception a later
        // reader would preserve as intentional.
        if (PreviousIsFunctionName())
        {
            OpenRegion(ParenRegion.Arguments);
            Type = FNARG_LPAREN;
        }
        else OpenRegion(ParenRegion.Group);
    }

    private void OpenRegion(ParenRegion region)
    {
        _parenRegions.Add(region);
        if (region != ParenRegion.Group) _openListRegions++;
    }

    private protected void OnDefaultRParen()
    {
        if (_parenRegions.Count == 0) return;   // unbalanced source — the parser reports it
        var region = _parenRegions[_parenRegions.Count - 1];
        _parenRegions.RemoveAt(_parenRegions.Count - 1);
        if (region == ParenRegion.Group) return;
        _openListRegions--;
        Type = region == ParenRegion.Arguments ? FNARG_RPAREN : REF_RPAREN;
    }

    // ⛔ NO SEMANTIC PREDICATE ON A PATH THE DFA MUST CACHE — CONTEXT GOES IN AN ACTION (kb/Work PB1715). ANTLR
    // 4.13.2 never caches a mode's start state whose closure passed through a predicate (LexerATNSimulator.MatchATN)
    // nor a DFA edge whose target did (AddDFAEdge), so every token that REACHES a predicate re-runs ATN simulation
    // and re-adds its DFA state under lock(dfa.states) — on every occurrence, not once. Six left-edge predicates in
    // this mode (the four FN_SIGNED_* twins, DEFINED, FNARG_SEPARATOR) did that to EVERY token of every compile:
    // the CLI paid the simulation serially and parallel compiles serialized on the one lock (3.5x peak at 8
    // threads, DESIGN-test-build-ci.md §3.14.5 M6). Each match is now unconditional and an ACTION decides what it
    // is (DEFINED became a virtual token IDENTIFIER's action retypes); actions do not suppress DFA edges.
    // LexerDfaCacheDriftTests holds it: re-lexing the suite's sources after a warm-up leaves the cached DFA nowhere.

    // A signed numeric literal's sign is the leftmost CHARACTER of the literal (ISO §2023:8.3.3.3.2 SR2): "If a sign is
    // used, it shall appear as the leftmost character of the literal") and an arithmetic operator "shall be
    // preceded by a space and followed by a space" (§2023:8.7.1) — so inside a list region a [+-] that follows a
    // separator and touches its digits starts a NEW argument or subscript (a signed literal), never a binary
    // operator: MAX(A -4) and T(I -4) are two each, MAX(A - 4) and T(I - 4) one subtraction each. Asked by the
    // FN_SIGNED_* action after the match, so the separator test reads the character before the TOKEN'S START, not
    // before the input index.
    private bool SignedLiteralCanStart()
    {
        if (!InListRegion()) return false;
        int start = TokenStartCharIndex;
        int before = InputStream.LA(start - 1 - InputStream.Index);   // LA(-k) is the character k before the index
        return before == ' ' || before == '\n' || before == ',' || before == ';'
            || before == '(';
    }

    // The FN_SIGNED_* action. A [+-] touching a numeric body always matches as ONE token (maximal munch over the
    // unconditional rule); where SignedLiteralCanStart holds it is the signed literal. Elsewhere the sign is the
    // arithmetic operator on its own, exactly what PLUS / MINUS lex, and the digits are a literal of their own: the
    // token is cut back to its first character and the lexer resumes at the digits, which re-lex as the unsigned
    // literal the old predicate-false path produced. No body character is a line break, so only the column moves.
    private protected void OnSignedLiteral(int signedType)
    {
        if (SignedLiteralCanStart()) { Type = signedType; return; }
        Type = InputStream.LA(TokenStartCharIndex - InputStream.Index) == '+' ? PLUS : MINUS;
        CutTokenTo(1);
    }

    // ⛔ THE ONE WAY AN ACTION GIVES BACK PART OF ITS MATCH (kb/Work PB1728). Ending a token early is two facts, not
    // one: where the input resumes AND the column the next token starts at — the lexer's Column is advanced by the
    // match and is NOT recomputed by a Seek. PIC_STRING's two trims used to Seek back over the separator alone, so
    // every token after `PIC 9,` on that line reported a column one too high (`01 Y PIC 9, VALUE 1.`: VALUE at 26,
    // correct 25) and every diagnostic anchored there pointed one column right. The token's text follows from the
    // cut (ANTLR reads it from the input interval the token spans), so no caller sets Text. Every caller cuts a
    // match that holds no line break — a sign and its digits, a PICTURE character-string — which is what makes the
    // column arithmetic exact; Line is untouched for the same reason.
    private void CutTokenTo(int length)
    {
        InputStream.Seek(TokenStartCharIndex + length);
        Column = TokenStartColumn + length;
    }

    // The action of the four delimited-literal tokens (kb/Work PB3245). A literal whose closing delimiter the line never
    // writes is still that literal's token (see STR_BODY), and it runs to the end of the line, so a line's trailing
    // separator period would be inside it and the statement it ends would run on into the next. The token therefore
    // gives back its trailing spaces and ONE trailing period: `DISPLAY "HELLO.` is the unterminated literal "HELLO, the
    // separator period, and nothing else to report. Content only: neither a prefix letter nor the opening delimiter is
    // a space or a period, so the cut never reaches them. A closed literal is never cut: it ends on its quotation
    // symbol, so a match that ends on a space or a period is an open one, and that one-character test is all the action
    // asks of the common case (no text is materialized for it).
    // ⛔ THE IS AFTER PIC / PICTURE IS A WORD OF THE SOURCE, AND IT IS WRITTEN AT MOST ONCE (kb/Work PB5213). The PICTURE
    // clause's format is `{PICTURE | PIC} IS character-string-1` (§2023:13.18.40.2; §1985:VI.5.9.2): IS is an optional word
    // the format prints ONCE, so the word after it is the character-string. The SPECIAL-NAMES CURRENCY SIGN clause's PICTURE SYMBOL
    // phrase prints no IS at all (§2023:12.3.7.2). PIC_IS used to `skip` every IS it met, so `PIC IS IS IS X(4)` compiled
    // as `PIC X(4)` and `WITH PICTURE IS SYMBOL "#"` as `WITH PICTURE SYMBOL "#"`: the lexer decided a syntax question
    // and erased the evidence. Now the FIRST IS of the region is the IS keyword token, which the parser takes where a
    // format prints it (`pictureClause`) and refuses where none does (`currencySignClause`); a SECOND is the
    // character-string PIC_STRING would have lexed (the two rules tie on `IS` and PIC_IS is listed first, so this
    // action gives it that rule's type and ends the mode as that rule does), which the PICTURE rules refuse, as they
    // refuse any string with no valid symbol. An action, not a predicate: a lexer predicate keeps its DFA edges out of
    // the cache (LexerDfaCacheDriftTests).
    private bool _pictureIsWritten;

    private protected void OnPictureIs()
    {
        if (!_pictureIsWritten) { _pictureIsWritten = true; Type = IS; return; }
        Type = PIC_STRING;
        PopMode();
    }

    // The action of PIC_STRING: the greedy match ends where the character-string ends, and the mode with it.
    // (1) `PIC 999999999999..` — the match took the separator period too: a trailing '.' is given back, so it becomes
    // the DOT token (CutTokenTo: the input AND the column).
    // (2) A trailing ',' or ';' IMMEDIATELY FOLLOWED BY A SPACE is the CLAUSE SEPARATOR the greedy match over-captured
    // (§2023:8.3.5 SR2) — `77 X PIC 99, VALUE 3.` lexes the picture as "99" (VCR Table 7 row 7.14; the W2
    // adversarial-review finding; the ';' twin was NC203A/245A/251A/252A). The following-character GUARD is
    // load-bearing: a LEGAL trailing ',' (§2023:13.18.40.3 SR7 — PICTURE as the last clause) is followed by the
    // separator PERIOD, not a space (NC125A's `PIC 9,9,…,9,.` — the token ends at the ',' because '.'+newline cannot
    // extend the match, so LA(1) is '.' and the ',' is a PICTURE SYMBOL to keep). The given-back separator re-lexes in
    // the mode that follows: in DEFAULT mode FNARG_SEPARATOR's action skips it (a PICTURE clause is never inside a list
    // region), and in PICMODE (3) PIC_WS does.
    // (3) What is left may be the word IS (kb/Work PB5867): in `PICTURE IS, X(2)` the match was "IS,", one character
    // longer than PIC_IS's "IS", so this rule won it; with its separator given back it is the IS PIC_IS would have
    // lexed, and the one decision about an IS in this mode is OnPictureIs's (the IS keyword once, the string after).
    private protected void OnPictureString()
    {
        var t = Text;
        if (t.Length > 1 && t[t.Length - 1] == '.')
        {
            CutTokenTo(t.Length - 1);
        }
        else if (t.Length > 1 && (t[t.Length - 1] == ',' || t[t.Length - 1] == ';'))
        {
            int la = InputStream.LA(1);
            if (la == ' ' || la == '\n' || la == IntStreamConstants.EOF)
            {
                CutTokenTo(t.Length - 1);
                // PIC_IS's own spelling: the grammar's caseInsensitive folds A-Z and a-z only, so this asks no culture
                if (t.Length == 3 && (t[0] | 0x20) == 'i' && (t[1] | 0x20) == 's') { OnPictureIs(); return; }
            }
        }
        PopMode();
    }

    private protected void OnDelimitedLiteral()
    {
        int last = InputStream.LA(-1);   // the match's last character
        if (last != ' ' && last != '.') return;
        string text = Text;
        int end = text.Length;
        while (text[end - 1] == ' ') end--;
        if (text[end - 1] == '.') end--;
        if (end < text.Length) CutTokenTo(end);
    }

    public override IToken NextToken()
    {
        var token = base.NextToken();
        // The group's >>COBOL-WORDS first, so everything below reads the EFFECTIVE word (see SetCobolWords).
        if (!_cobolWords.IsEmpty && token is CommonToken word) _cobolWords.Apply(word);
        ApplyEditionSpelling(token);
        // PIC / PICTURE opens PICMODE (the picture character-string is lexed as ONE token) — here, not as a lexer command
        // of the PIC rule, because whether this token IS the PIC keyword is the directive's answer (UNDEFINE "PICTURE"
        // makes a data-name of it; EQUATE "PIC" WITH "PX" makes PX the keyword).
        if (token.Type == PIC) { PushMode(PICMODE); _pictureIsWritten = false; }
        // DEBUG_LINE is a marker, not a word: like a comment it must not become the "previous token" of the next one.
        if (token.Type != WS && token.Type != DEBUG_LINE && token.Type != DEBUG_LINE_IN_REGION
            && token.Type != TokenConstants.EOF)
        {
            TrackReportSection(token.Type);   // before the shift: it asks what PRECEDED this token
            if (token.Type == FUNCTION)       // before the shift too (see _functionIsAddressOperand)
                _functionIsAddressOperand = _lastNonWsTokenType == ADDRESS
                    || (_lastNonWsTokenType == OF && _prevNonWsTokenType == ADDRESS);
            _prevNonWsTokenType = _lastNonWsTokenType;
            _lastNonWsTokenType = token.Type;
        }
        return token;
    }
}
