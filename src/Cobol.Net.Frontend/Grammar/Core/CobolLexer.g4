// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

lexer grammar CobolLexer;

options {
    caseInsensitive = true;
}

@members {
    // Track the types of the last TWO non-WS tokens emitted: one for subscript-mode detection, two for the
    // FUNCTION-argument suppression (P7 Step 12 — '(' after "FUNCTION name" opens an ARGUMENT list, ISO
    // §8.4.3.2 SR6, lexed in DEFAULT mode so arguments parse as real arithmeticExpressions).
    private int _lastNonWsTokenType = -1;
    private int _prevNonWsTokenType = -1;

    // SUBSCRIPT MODE TRIGGER: whitelist approach.
    //
    // In COBOL, '(' after a data-name means subscript/reference-modification; '(' after anything else means
    // arithmetic grouping (e.g., IF (A + B) > C). The whitelist is IDENTIFIER plus every context-sensitive
    // keyword that can appear as a user-defined data-name. Safe failure mode: a token missing from the set makes
    // the parser see LPAREN/RPAREN (arithmetic) instead of SUBSCRIPT-mode tokens — a clear parse error on the
    // first subscripted use.
    //
    // The set (_dataNameTokens) is GENERATED into Parsing/CobolLexerWordSet.g.cs from
    // tests/version-matrix/cobol-words.json (the subscriptTrigger=true rows) by scripts/gen-cobol-words.ps1
    // (rearchitecture PHASE 04, Group A) — single-sourced with the parser cobolWord rule (Grammar/Core/CobolWords.g4)
    // and cross-checked by CobolWordsDriftTests, retiring the by-hand "mirror cobolWord" discipline this comment
    // used to instruct. Do NOT hand-add a token here: edit cobol-words.json and re-run the generator.

    private bool PreviousTokenCouldBeDataName()
        => _dataNameTokens.Contains(_lastNonWsTokenType);

    // FUNCTION-ARGUMENT REGION (P7 Step 12). '(' after "FUNCTION functionName" is the function's argument-list
    // paren (ISO §8.4.3.2 SR6) — it stays in DEFAULT mode so the arguments parse through the ONE
    // arithmeticExpression grammar. The keyword-omitted form name(args) (§8.4.3.2 SR2, D2) has no FUNCTION
    // token before the name, so it still pushes SUBSCRIPT and stays token-captured. Inside an argument region a
    // '(' after a data-name still pushes SUBSCRIPT (nested subscripts/ref-mod are untouched — the D10/PHASE-15
    // deferral). The paren stack tracks which OPEN DEFAULT-mode parens are function-argument regions so the
    // sign-adjacent literal twins below fire only there.
    private bool PreviousIsFunctionName() => _prevNonWsTokenType == FUNCTION;

    private readonly System.Collections.Generic.List<bool> _fnParenStack = new();
    private bool _primeFunctionArgs;

    private bool InFunctionArgs() => _primeFunctionArgs || _fnParenStack.Contains(true);

    // Fragment re-parse hook (the D2 keyword-omitted argument re-parse): treat the WHOLE input as one
    // function-argument region (the fragment is the text inside the argument parens).
    public void PrimeFunctionArgs() => _primeFunctionArgs = true;

    private void OnDefaultLParen()
    {
        if (PreviousTokenCouldBeDataName() && !PreviousIsFunctionName())
        {
            PushMode(SUBSCRIPT);   // subscript / ref-mod capture — the matching ')' is SUB_RPAREN (popMode)
            return;
        }
        // Staying DEFAULT: record whether this paren opens a FUNCTION argument region.
        _fnParenStack.Add(PreviousIsFunctionName() && PreviousTokenCouldBeDataName());
    }

    private void OnDefaultRParen()
    {
        if (_fnParenStack.Count > 0) _fnParenStack.RemoveAt(_fnParenStack.Count - 1);
    }

    // A signed numeric literal's sign is the leftmost CHARACTER of the literal (ISO §8.3.3.3.2 r2) and an
    // arithmetic operator shall be preceded AND followed by a space (§8.7.1) — so inside a function-argument
    // region a [+-] that follows a separator and touches its digits starts a NEW argument (a signed literal),
    // never a binary operator: MAX(A -4) is two arguments, MAX(A - 4) is one subtraction.
    private bool SignedLiteralCanStart()
    {
        if (!InFunctionArgs()) return false;
        if (InputStream.Index == 0) return true;   // fragment start (the D2 re-parse) — a separator by definition
        int la = InputStream.LA(-1);
        return la == ' ' || la == '\t' || la == '\r' || la == '\n' || la == ',' || la == ';' || la == '('
            || la == Antlr4.Runtime.IntStreamConstants.EOF;
    }

    public override Antlr4.Runtime.IToken NextToken()
    {
        var token = base.NextToken();
        if (token.Type != WS && token.Type != SUB_WS && token.Type != Antlr4.Runtime.TokenConstants.EOF)
        {
            _prevNonWsTokenType = _lastNonWsTokenType;
            _lastNonWsTokenType = token.Type;
        }
        return token;
    }
}

// ==========================================
// DEFAULT MODE
// ==========================================
// Assumes preprocessed input: fixed→free normalized, COPY/REPLACE expanded.

WS           : [ \t\r\n]+ -> skip ;
COMMENT_START: '*>' -> skip, pushMode(COMMENT_MODE) ;

// ── END-xxx paired terminators (must precede END and IDENTIFIER) ──

END_IF       : 'END-IF' ;
END_PERFORM  : 'END-PERFORM' ;
END_EVALUATE : 'END-EVALUATE' ;
END_RECEIVE  : 'END-RECEIVE' ;  // MCS scope terminator (ISO 14.9.31)
END_READ     : 'END-READ' ;
END_SEND     : 'END-SEND' ;     // MCS scope terminator (ISO 14.9.38)
END_SEARCH   : 'END-SEARCH' ;
END_CALL     : 'END-CALL' ;
END_SORT     : 'END-SORT' ;
END_MERGE    : 'END-MERGE' ;
END_RETURN   : 'END-RETURN' ;
END_REWRITE  : 'END-REWRITE' ;
END_DELETE   : 'END-DELETE' ;
END_WRITE    : 'END-WRITE' ;
END_START    : 'END-START' ;
END_INVOKE   : 'END-INVOKE' ;
END_JSON     : 'END-JSON' ;
END_XML      : 'END-XML' ;
END_METHOD   : 'END-METHOD' ;
END_ADD      : 'END-ADD' ;
END_SUBTRACT : 'END-SUBTRACT' ;
END_MULTIPLY : 'END-MULTIPLY' ;
END_DIVIDE   : 'END-DIVIDE' ;
END_COMPUTE  : 'END-COMPUTE' ;
END_STRING   : 'END-STRING' ;
END_UNSTRING : 'END-UNSTRING' ;
END_ACCEPT   : 'END-ACCEPT' ;
END_DISPLAY  : 'END-DISPLAY' ;

END_OF_PAGE  : 'END-OF-PAGE' ;
EOP          : 'EOP' ;

// ── Hyphenated keywords (must precede IDENTIFIER) ──

PROGRAM_ID      : 'PROGRAM-ID' ;
FUNCTION_ID     : 'FUNCTION-ID' ;   // COBOL-2002 user-defined function unit header (ISO §11.5)
EXCLUSIVE_OR    : 'EXCLUSIVE-OR' ;  // COBOL-2023 logical exclusive-or operator, = XOR (ISO §8.8.4.9; a 2023 addition per Annex E.2 item 25 — VCR rows 32/41; the former "2002" note was the W3-corrected mislabel)
// COBOL-2002 BOOLEAN OPERATORS (ISO §8.7.2 / §8.8.2). Maximal-munch safe: `B-ANDER`/`B-ORDER` stay IDENTIFIER
// by longer match; `B-AND` exact reduces to the keyword by rule order (hyphenated keywords precede IDENTIFIER);
// BOOLLIT `B"…"` is disjoint (the char after B is a quote). User words at 85 (admitted via cobolWord +
// _dataNameTokens); the operator meaning is {is2002()}?-gated in the expression tiers.
B_AND           : 'B-AND' ;
B_OR            : 'B-OR' ;
B_XOR           : 'B-XOR' ;
B_NOT           : 'B-NOT' ;
// Boolean shift operators (ISO §8.8.2, COBOL-2023). Order-sensitive: the -LC/-RC literals FIRST so a longer
// match wins over the -L/-R prefix (ANTLR first-match; feedback_grammar_precedence).
B_SHIFT_LC      : 'B-SHIFT-LC' ;
B_SHIFT_RC      : 'B-SHIFT-RC' ;
B_SHIFT_L       : 'B-SHIFT-L' ;
B_SHIFT_R       : 'B-SHIFT-R' ;
FLOAT_SHORT     : 'FLOAT-SHORT' ;   // COBOL-2002 standard floating point: IEEE-754 single (= COMP-1)
FLOAT_LONG      : 'FLOAT-LONG' ;    // COBOL-2002 standard floating point: IEEE-754 double (= COMP-2)
FLOAT_EXTENDED  : 'FLOAT-EXTENDED' ;// COBOL-2002 extended float — mapped to IEEE-754 double (.NET has no quad)
// COBOL-2002 ROUNDED MODE names (ISO §14.9.4). Reserved words; corpus-checked (only in comments).
AWAY_FROM_ZERO          : 'AWAY-FROM-ZERO' ;
NEAREST_AWAY_FROM_ZERO  : 'NEAREST-AWAY-FROM-ZERO' ;
NEAREST_EVEN            : 'NEAREST-EVEN' ;
NEAREST_TOWARD_ZERO     : 'NEAREST-TOWARD-ZERO' ;
TOWARD_GREATER          : 'TOWARD-GREATER' ;
TOWARD_LESSER           : 'TOWARD-LESSER' ;
PROHIBITED              : 'PROHIBITED' ;
TRUNCATION              : 'TRUNCATION' ;
METHOD_ID       : 'METHOD-ID' ;
CLASS_ID        : 'CLASS-ID' ;
INTERFACE_ID    : 'INTERFACE-ID' ;
WORKING_STORAGE : 'WORKING-STORAGE' ;
LOCAL_STORAGE   : 'LOCAL-STORAGE' ;
SENTENCE        : 'SENTENCE' ;
DATE_WRITTEN    : 'DATE-WRITTEN' ;
DATE_COMPILED   : 'DATE-COMPILED' ;
SOURCE_COMPUTER : 'SOURCE-COMPUTER' ;
OBJECT_COMPUTER : 'OBJECT-COMPUTER' ;
OBJECT      : 'OBJECT' ;
OVERRIDE    : 'OVERRIDE' ;  // METHOD-ID attribute (ISO §11.7, 2002+; §8.9-reserved 2002+ — user word at 85, funnel row exists)
IMPLEMENTS  : 'IMPLEMENTS' ;  // FACTORY/OBJECT paragraph clause (§11.8; §8.10 CONTEXT-SENSITIVE — a user word at EVERY edition, never funneled)
PROPERTY    : 'PROPERTY' ;  // the PROPERTY clause / GET-SET selector / repository specifier (§13.18.42/§11.7/§12.3.8; §8.9-reserved 2002+)
GET         : 'GET' ;       // METHOD-ID GET PROPERTY selector (§11.7; §8.9-reserved 2002+ — user word at 85)
INTERFACE   : 'INTERFACE' ; // END INTERFACE + the repository INTERFACE specifier (§11.6/§12.3.8; §8.9-reserved 2002+)
FACTORY     : 'FACTORY' ;   // the FACTORY paragraph (ISO §11.4, 2002+; §8.9-reserved 2002+ — user word at 85, funnel row exists)
PROTOTYPE   : 'PROTOTYPE' ; // FUNCTION-ID … IS PROTOTYPE (ISO §11.5 Format 2 / §10.6; §8.9-reserved 2002+ — user word at 85, funnel row exists)
INHERITS    : 'INHERITS' ;   // COBOL-2002 OO: CLASS-ID. name INHERITS FROM base (ISO §11.3); corpus-clean
SPECIAL_NAMES   : 'SPECIAL-NAMES' ;
FILE_CONTROL    : 'FILE-CONTROL' ;
I_O_CONTROL     : 'I-O-CONTROL' ;
I_O             : 'I-O' ;
PACKED_DECIMAL  : 'PACKED-DECIMAL' ;
// COBOL-2002 fixed-width binary usages (ISO §13.18.60). Must precede BINARY/IDENTIFIER; maximal munch
// matches the full 'BINARY-xxx' over the shorter 'BINARY' token.
BINARY_CHAR     : 'BINARY-CHAR' ;
BINARY_SHORT    : 'BINARY-SHORT' ;
BINARY_LONG     : 'BINARY-LONG' ;
BINARY_DOUBLE   : 'BINARY-DOUBLE' ;
// COBOL-2002 OPTIONS-paragraph clause words (ISO §11.9). All are CONTEXT-SENSITIVE — reserved only inside the
// OPTIONS paragraph — so each is ALSO listed in cobolWord (CobolParserCore.g4) and mirrored in _dataNameTokens
// above, keeping it legal as a user-defined name everywhere else. Hyphenated forms must precede IDENTIFIER so
// maximal-munch + first-rule-wins picks the whole keyword (e.g. 'STANDARD-BINARY' over IDENTIFIER); a bare
// 'STANDARD' still lexes as STANDARD, and 'WC-COBOL' still lexes as one IDENTIFIER. Corpus-checked: zero of these
// appear as user-defined words in the test corpus.
STANDARD_BINARY   : 'STANDARD-BINARY' ;
STANDARD_DECIMAL  : 'STANDARD-DECIMAL' ;
ENTRY_CONVENTION  : 'ENTRY-CONVENTION' ;
FLOAT_BINARY      : 'FLOAT-BINARY' ;
FLOAT_DECIMAL     : 'FLOAT-DECIMAL' ;
// The COBOL-2014 IEEE-754 interchange float USAGES (ISO §13.18.60.4 GR14-18): FLOAT-BINARY-32/64/128 =
// ISO/IEC 60559:2020 binary32/64/128, FLOAT-DECIMAL-16/34 = decimal64/128. Dedicated hyphenated tokens declared
// BEFORE IDENTIFIER so maximal-munch + first-rule-wins picks the whole keyword (the source text 'FLOAT-BINARY-32'
// lexes as ONE IDENTIFIER otherwise — the numeric suffix is swallowed by NAME_BODY). Introduction-gated to 2014
// post-bind (VersionConformancePass, via the Usage member); binary32/64 are LIVE (native float/double), the
// binary128/decimal formats are processor-dependent non-support (Annex A.3 items 17/19, COBOLNET1564).
FLOAT_BINARY_32   : 'FLOAT-BINARY-32' ;
FLOAT_BINARY_64   : 'FLOAT-BINARY-64' ;
FLOAT_BINARY_128  : 'FLOAT-BINARY-128' ;
FLOAT_DECIMAL_16  : 'FLOAT-DECIMAL-16' ;
FLOAT_DECIMAL_34  : 'FLOAT-DECIMAL-34' ;
HIGH_ORDER_LEFT   : 'HIGH-ORDER-LEFT' ;
HIGH_ORDER_RIGHT  : 'HIGH-ORDER-RIGHT' ;
BINARY_ENCODING   : 'BINARY-ENCODING' ;
DECIMAL_ENCODING  : 'DECIMAL-ENCODING' ;
ARITHMETIC        : 'ARITHMETIC' ;
DEFAULT           : 'DEFAULT' ;
INTERMEDIATE      : 'INTERMEDIATE' ;
ROUNDING          : 'ROUNDING' ;
// BLANK [WHEN] ZERO is parsed as individual tokens in the parser grammar
// X3.23-1985 RERUN clause unit (obsolete '85 element deleted by ISO 2002 — accepted-inert at 85, 0902 ≥2002
// per VCR Table 7 row 7.15). Hyphenated, so it must precede IDENTIFIER; a longer user word like
// CLOCK-UNITS-X still lexes as one IDENTIFIER (maximal munch). Also a legal user word via cobolWord where
// the §8.9 funnel frees it (mirrored in _dataNameTokens).
CLOCK_UNITS     : 'CLOCK-UNITS' ;
DAY_OF_WEEK     : 'DAY-OF-WEEK' ;
REVERSE_VIDEO   : 'REVERSE-VIDEO' ;
FOREGROUND_COLOR: 'FOREGROUND-COLOR' ;
BACKGROUND_COLOR: 'BACKGROUND-COLOR' ;

// ── Division/section keywords ──

IDENTIFICATION : 'IDENTIFICATION' ;
CONFIGURATION : 'CONFIGURATION' ;
DIVISION    : 'DIVISION' ;
ENVIRONMENT : 'ENVIRONMENT' ;
DATA        : 'DATA' ;
PROCEDURE   : 'PROCEDURE' ;
REPORT      : 'REPORT' ;
SCREEN      : 'SCREEN' ;
SECTION     : 'SECTION' ;
LINKAGE     : 'LINKAGE' ;
INPUT_OUTPUT : 'INPUT-OUTPUT' ;
FD          : 'FD' ;
RD          : 'RD' ;
SD          : 'SD' ;

// ── Statement keywords ──

ACCEPT      : 'ACCEPT' ;
ADD         : 'ADD' ;
ALTER       : 'ALTER' ;
CALL        : 'CALL' ;
CANCEL      : 'CANCEL' ;
CAPACITY    : 'CAPACITY' ;   // OCCURS DYNAMIC … CAPACITY IN data-name (ISO §13.18.38 Format 4, COBOL-2014; D9)
CLOSE       : 'CLOSE' ;
COMPUTE     : 'COMPUTE' ;
CONTINUE    : 'CONTINUE' ;
DELETE      : 'DELETE' ;
DISPLAY     : 'DISPLAY' ;
// X3.23-1985 ENTER statement verb (obsolete '85 element deleted by ISO 2002 — accepted-inert at 85,
// 0902 ≥2002 per VCR Table 7 row 7.16). A legal user word via cobolWord at editions where the §8.9
// funnel frees it (85-only reserved per ReservedWords.Table); mirrored in _dataNameTokens.
ENTER       : 'ENTER' ;
GENERATE    : 'GENERATE' ;
DIVIDE      : 'DIVIDE' ;
EVALUATE    : 'EVALUATE' ;
EXIT        : 'EXIT' ;
GOBACK      : 'GOBACK' ;
GO          : 'GO' ;
IF          : 'IF' ;
INITIATE    : 'INITIATE' ;
INITIALIZE  : 'INITIALIZE' ;
INITIALIZED : 'INITIALIZED' ;
INSPECT     : 'INSPECT' ;
ALLOCATE    : 'ALLOCATE' ;
FREE        : 'FREE' ;
INVOKE      : 'INVOKE' ;
JSON        : 'JSON' ;
MESSAGE     : 'MESSAGE' ;      // MCS: the CONTINUE AFTER MESSAGE RECEIVED phrase (ISO 14.9.31)
MERGE       : 'MERGE' ;
MOVE        : 'MOVE' ;
MULTIPLY    : 'MULTIPLY' ;
OPEN        : 'OPEN' ;
PERFORM     : 'PERFORM' ;
READ        : 'READ' ;
RELEASE     : 'RELEASE' ;
REPOSITORY  : 'REPOSITORY' ;   // COBOL-2002 REPOSITORY paragraph header (ISO §12.3.8)
RETURN      : 'RETURN' ;
REWRITE     : 'REWRITE' ;
SEARCH      : 'SEARCH' ;
SET         : 'SET' ;
SORT        : 'SORT' ;
START       : 'START' ;
STOP        : 'STOP' ;
STRING      : 'STRING' ;
SUBTRACT    : 'SUBTRACT' ;
SUPPRESS    : 'SUPPRESS' ;
TERMINATE   : 'TERMINATE' ;
UNSTRING    : 'UNSTRING' ;
WRITE       : 'WRITE' ;
XML         : 'XML' ;

// ── Clause/phrase keywords ──

ACCESS      : 'ACCESS' ;
ADDRESS     : 'ADDRESS' ;
AREA        : 'AREA' ;
BASED       : 'BASED' ;
// CONSTANT (ISO §13.10 constant entry / §13.18.15 CONSTANT RECORD clause; reserved 2002+ per §8.9) — a legal
// user word at COBOL-85, so it is cobolWord/_dataNameTokens-admitted (cobol-words.json) and funnel-0901'd ≥2002
// by the VersionConformancePass §8.9 funnel (the PROTOTYPE/SHARING precedent).
CONSTANT    : 'CONSTANT' ;
// AS (the §13.10 constant entry's AS phrase; reserved 2002+ per §8.9) — the same interval as CONSTANT: a legal
// user word at COBOL-85, cobolWord/_dataNameTokens-admitted (cobol-words.json), funnel-0901'd ≥2002. Its only
// keyword slot is the constantEntryBody `AS` — a direct token, never a name slot.
AS          : 'AS' ;
// PROGRAM-POINTER / FUNCTION-POINTER (USAGE phrases, ISO §13.18.60; reserved 2002+ per §8.9) — the CONSTANT/AS
// interval treatment: legal user words at COBOL-85, cobolWord/_dataNameTokens-admitted (cobol-words.json),
// funnel-0901'd ≥2002. Their only keyword slots are the usageKeyword alternatives — direct tokens.
PROGRAM_POINTER  : 'PROGRAM-POINTER' ;
FUNCTION_POINTER : 'FUNCTION-POINTER' ;
AREAS       : 'AREAS' ;
ALPHABETIC       : 'ALPHABETIC' ;
ALPHABETIC_LOWER : 'ALPHABETIC-LOWER' ;
ALPHABETIC_UPPER : 'ALPHABETIC-UPPER' ;
ADVANCING   : 'ADVANCING' ;
AFTER       : 'AFTER' ;
ALL         : 'ALL' ;
ALSO        : 'ALSO' ;
ALPHANUMERIC_EDITED : 'ALPHANUMERIC-EDITED' ;
NUMERIC_EDITED : 'NUMERIC-EDITED' ;
ALPHANUMERIC : 'ALPHANUMERIC' ;
ALTERNATE   : 'ALTERNATE' ;
AND         : 'AND' ;
ANY         : 'ANY' ;
ASCENDING   : 'ASCENDING' ;
ASSIGN      : 'ASSIGN' ;
ARE         : 'ARE' ;
AT          : 'AT' ;
AUTO        : 'AUTO' ;
AUTHOR      : 'AUTHOR' ;
BACKWARD    : 'BACKWARD' ;   // COBOL-2002 INSPECT … BACKWARD (right-to-left inspection, ISO §14.9.21)
BEFORE      : 'BEFORE' ;
BELL        : 'BELL' ;
BLINK       : 'BLINK' ;
BINARY      : 'BINARY' ;
BIT         : 'BIT' ;
BLANK       : 'BLANK' ;
BLOCK       : 'BLOCK' ;
BOTTOM      : 'BOTTOM' ;
BY          : 'BY' ;
CF          : 'CF' ;
CH          : 'CH' ;
CHARACTER   : 'CHARACTER' ;
CHARACTERS  : 'CHARACTERS' ;
CLASS       : 'CLASS' ;
CODE        : 'CODE' ;
CODE_SET    : 'CODE-SET' ;
COL         : 'COL' ;
COLS        : 'COLS' ;      // COBOL-2002 COLUMN-clause spelling (ISO §13.18.14 SR1); usable as a user word via cobolWord (§8.9 funnel gates ≥2002)
COLUMN      : 'COLUMN' ;
COLUMNS     : 'COLUMNS' ;   // COBOL-2002 COLUMN-clause spelling (ISO §13.18.14 SR1); usable as a user word via cobolWord (§8.9 funnel gates ≥2002)
COLLATING   : 'COLLATING' ;
COMMIT      : 'COMMIT' ;       // commit/rollback facility (A.3 items 6-7) - recognized-not-supported
COMMON      : 'COMMON' ;
COMP        : 'COMP' ;
COMP_1      : 'COMP-1' ;
COMP_2      : 'COMP-2' ;
COMP_3      : 'COMP-3' ;
COMP_4      : 'COMP-4' ;
COMP_5      : 'COMP-5' ;
COMPUTATIONAL   : 'COMPUTATIONAL' ;
COMPUTATIONAL_1 : 'COMPUTATIONAL-1' ;
COMPUTATIONAL_2 : 'COMPUTATIONAL-2' ;
COMPUTATIONAL_3 : 'COMPUTATIONAL-3' ;
COMPUTATIONAL_4 : 'COMPUTATIONAL-4' ;
COMPUTATIONAL_5 : 'COMPUTATIONAL-5' ;
CONTAINS    : 'CONTAINS' ;
CONTENT     : 'CONTENT' ;
CONTROL     : 'CONTROL' ;
CONTROLS    : 'CONTROLS' ;
CONVERTING  : 'CONVERTING' ;
CORR        : 'CORR' ;
CURRENCY    : 'CURRENCY' ;
CYCLE       : 'CYCLE' ;
DECIMAL_POINT : 'DECIMAL-POINT' ;
CORRESPONDING : 'CORRESPONDING' ;
COUNT       : 'COUNT' ;
DATE        : 'DATE' ;
DAY         : 'DAY' ;
DE          : 'DE' ;
DETAIL      : 'DETAIL' ;
YYYYMMDD    : 'YYYYMMDD' ;
YYYYDDD     : 'YYYYDDD' ;
DECLARATIVES: 'DECLARATIVES' ;
DELIMITED   : 'DELIMITED' ;
DELIMITER   : 'DELIMITER' ;
DEPENDING   : 'DEPENDING' ;
DESCENDING  : 'DESCENDING' ;
DOWN        : 'DOWN' ;
DUPLICATES  : 'DUPLICATES' ;
DYNAMIC     : 'DYNAMIC' ;
// ── The X3.23-1985 notInGrammar 85-acceptance words (VCR Table 7 rows 7.15–7.18; obsolete '85 elements
//    deleted by ISO 2002; each is ALSO a legal user word via cobolWord at the editions where the §8.9
//    funnel frees it — all five are mirrored in _dataNameTokens above, with ENTER and CLOCK-UNITS in
//    their own bands) ──
DEBUGGING   : 'DEBUGGING' ;   // USE FOR DEBUGGING (the '85 debug facility, row 7.17)
EVERY       : 'EVERY' ;       // RERUN … EVERY (row 7.15)
RERUN       : 'RERUN' ;       // the I-O-CONTROL RERUN clause head (row 7.15)
REFERENCES  : 'REFERENCES' ;  // USE FOR DEBUGGING ON ALL REFERENCES OF (row 7.17); distinct from REFERENCE
PROCEDURES  : 'PROCEDURES' ;  // USE FOR DEBUGGING ON ALL PROCEDURES (row 7.17); distinct from PROCEDURE
EDITED      : 'EDITED' ;
EDITING     : 'EDITING' ;                 // PICTURE EDITING phrase (ISO §13.18.40.2; new-in-2023 reserved word, Annex E.2 item 25)
ELSE        : 'ELSE' ;
END         : 'END' ;
EOL         : 'EOL' ;
EOS         : 'EOS' ;
ERASE       : 'ERASE' ;
ENTRY       : 'ENTRY' ;
EQUAL       : 'EQUAL' ;
ERROR       : 'ERROR' ;
EXCEPTION   : 'EXCEPTION' ;
EXTEND      : 'EXTEND' ;
EXTERNAL    : 'EXTERNAL' ;
FIRST       : 'FIRST' ;
FOOTING     : 'FOOTING' ;
FOR         : 'FOR' ;
FALSE_      : 'FALSE' ;
FILE        : 'FILE' ;
FILLER      : 'FILLER' ;
FINAL       : 'FINAL' ;
FULL_       : 'FULL' ;
POSITIVE    : 'POSITIVE' ;
NEGATIVE    : 'NEGATIVE' ;
REQUIRED    : 'REQUIRED' ;
RESERVE     : 'RESERVE' ;
FROM        : 'FROM' ;
FUNCTION    : 'FUNCTION' ;
GROUP       : 'GROUP' ;
HEADING     : 'HEADING' ;
HIGHLIGHT   : 'HIGHLIGHT' ;
INDICATE    : 'INDICATE' ;
LABEL       : 'LABEL' ;
LAST        : 'LAST' ;
LINAGE      : 'LINAGE' ;
LINAGE_COUNTER : 'LINAGE-COUNTER' ;
LIMIT       : 'LIMIT' ;
LIMITS      : 'LIMITS' ;
LINE_COUNTER : 'LINE-COUNTER' ;
GENERIC     : 'GENERIC' ;
GIVING      : 'GIVING' ;
GLOBAL      : 'GLOBAL' ;
GREATER     : 'GREATER' ;
SYMBOLIC    : 'SYMBOLIC' ;
TABLE       : 'TABLE' ;
ALPHABET    : 'ALPHABET' ;
CRT         : 'CRT' ;
CURSOR      : 'CURSOR' ;
CHANNEL     : 'CHANNEL' ;
PROCEED     : 'PROCEED' ;
UPON        : 'UPON' ;
USE         : 'USE' ;
STANDARD    : 'STANDARD' ;
REPORTING   : 'REPORTING' ;
SUM         : 'SUM' ;
IN          : 'IN' ;
INDEX       : 'INDEX' ;
INDEXED     : 'INDEXED' ;
INITIAL_    : 'INITIAL' ;
INPUT       : 'INPUT' ;
INSTALLATION: 'INSTALLATION' ;
INTRINSIC   : 'INTRINSIC' ;   // COBOL-2002 REPOSITORY FUNCTION … INTRINSIC (ISO §12.3.8)
INTO        : 'INTO' ;
INVALID     : 'INVALID' ;
IS          : 'IS' ;
JUST        : 'JUST' ;
JUSTIFIED   : 'JUSTIFIED' ;
KEY         : 'KEY' ;
LEADING     : 'LEADING' ;
LEFT        : 'LEFT' ;
LENGTH      : 'LENGTH' ;
LESS        : 'LESS' ;
LINE        : 'LINE' ;
LINES       : 'LINES' ;
LOCK        : 'LOCK' ;
LOWLIGHT    : 'LOWLIGHT' ;
// ── COBOL-2002 file sharing / record locking (ISO §12.4.5.9/.15, §14.7.9, §14.9.27/.30/.47). SHARING/RETRY/
// UNLOCK are §8.9-reserved since 2002 (funnel-gated); MANUAL/AUTOMATIC/IGNORING/FOREVER/SECONDS/ONLY are
// §8.10 context-sensitive (user-legal at every edition). The operator meaning is {is2002()}?-gated in CobolIO.g4.
MANUAL      : 'MANUAL' ;
AUTOMATIC   : 'AUTOMATIC' ;
IGNORING    : 'IGNORING' ;
FOREVER     : 'FOREVER' ;
SECONDS     : 'SECONDS' ;
SHARING     : 'SHARING' ;
RETRY       : 'RETRY' ;
UNLOCK      : 'UNLOCK' ;
METHOD      : 'METHOD' ;
MODE        : 'MODE' ;
NATIONAL    : 'NATIONAL' ;
NATIVE      : 'NATIVE' ;
NEXT        : 'NEXT' ;
NORMAL      : 'NORMAL' ;
NO          : 'NO' ;
NUMBER      : 'NUMBER' ;
NUMBERS     : 'NUMBERS' ;   // COLUMN/LINE clause plural spelling (ISO §13.18.14/§13.18.35) — §8.10 context-sensitive (user word at every edition)
NOT         : 'NOT' ;
NUMERIC     : 'NUMERIC' ;
NULL_       : 'NULL' ;
OCCURS      : 'OCCURS' ;
OF          : 'OF' ;
OFF         : 'OFF' ;
ON          : 'ON' ;
ONLY        : 'ONLY' ;   // COBOL-2002 SHARING READ ONLY (ISO §12.4.5.15) — §8.10 context-sensitive (user word)
OR          : 'OR' ;
XOR         : 'XOR' ;   // COBOL-2023 logical exclusive-or operator (ISO §8.8.4.9; 2023 per Annex E.2 item 25 — VCR rows 32/41)
OMITTED     : 'OMITTED' ;
OPTIONAL    : 'OPTIONAL' ;
OPTIONS     : 'OPTIONS' ;   // COBOL-2002 OPTIONS paragraph header (ISO §11.9)
ORGANIZATION: 'ORGANIZATION' ;
OTHER       : 'OTHER' ;
OUTPUT      : 'OUTPUT' ;
OVERFLOW    : 'OVERFLOW' ;
PACKED      : 'PACKED' ;
PAGE        : 'PAGE' ;
PAGE_COUNTER : 'PAGE-COUNTER' ;
PADDING     : 'PADDING' ;
PARAGRAPH   : 'PARAGRAPH' ;
PARSE       : 'PARSE' ;        // JSON/XML PARSE (2014+); usable as a user word via cobolWord
PROCESSING  : 'PROCESSING' ;   // XML PARSE … PROCESSING PROCEDURE (2014+); usable as a user word via cobolWord
PF          : 'PF' ;
PH          : 'PH' ;
// PIC/PICTURE → push into PICMODE to capture the PIC string as one token.
// Handles: PIC X(120), PIC IS S9(18), PICTURE $$$,$$9.99CR, etc.
PIC         : ('PIC' | 'PICTURE') -> pushMode(PICMODE) ;
POINTER     : 'POINTER' ;
PLUSWORD    : 'PLUS' ;       // the reserved WORD PLUS (LINE/NEXT GROUP relative); distinct from PLUS ('+')
PRESENT     : 'PRESENT' ;   // PRESENT WHEN clause (ISO §13.18.41, 2002+); usable as a user word via cobolWord (§8.9 funnel gates ≥2002)
PREVIOUS    : 'PREVIOUS' ;
PRINTING    : 'PRINTING' ;
PROGRAM     : 'PROGRAM' ;
// ── EC exception-model words (ISO 2002+; each is ALSO a legal user word via cobolWord + _dataNameTokens) ──
RAISING     : 'RAISING' ;    // GOBACK/EXIT … RAISING + the PD-header RAISING phrase (ISO §14.9.18 / §14.2)
RAISE       : 'RAISE' ;      // RAISE statement (ISO §14.9.29)
RESUME      : 'RESUME' ;     // RESUME statement (ISO §14.9.33)
STATEMENT   : 'STATEMENT' ;  // RESUME AT NEXT STATEMENT (ISO §14.9.33)
CONDITION   : 'CONDITION' ;  // USE AFTER EXCEPTION CONDITION (ISO §14.9.49 Format 3)
EO          : 'EO' ;         // USE AFTER EO ≡ EXCEPTION OBJECT (ISO §14.9.49.3 SR15; the EC-OO wave) — same context-sensitive recipe as EC
EC          : 'EC' ;         // USE AFTER EC ≡ EXCEPTION CONDITION (ISO §14.9.49.3 SR12); maximal munch keeps
                             // EC-I-O-AT-END etc. one IDENTIFIER (the longer match wins)
RANDOM      : 'RANDOM' ;
RECEIVE     : 'RECEIVE' ;      // MCS RECEIVE (ISO 14.9.31) - recognized-not-supported (4.2.6, A.3 item 4)
RECORD      : 'RECORD' ;
RECORDS     : 'RECORDS' ;
REEL        : 'REEL' ;
REPORTS     : 'REPORTS' ;
RESET       : 'RESET' ;
RECURSIVE   : 'RECURSIVE' ;
REDEFINES   : 'REDEFINES' ;
REPLACING   : 'REPLACING' ;
REFERENCE   : 'REFERENCE' ;
RELATIVE    : 'RELATIVE' ;
REMAINDER   : 'REMAINDER' ;
REMOVAL     : 'REMOVAL' ;
REMARKS     : 'REMARKS' ;
RENAMES     : 'RENAMES' ;
RETURNING   : 'RETURNING' ;
REWIND      : 'REWIND' ;
REVERSED    : 'REVERSED' ;
RF          : 'RF' ;
RH          : 'RH' ;
ROLLBACK    : 'ROLLBACK' ;     // commit/rollback facility (A.3 items 6-7) - recognized-not-supported
ROUNDED     : 'ROUNDED' ;
RIGHT       : 'RIGHT' ;
RUN         : 'RUN' ;
SAME        : 'SAME' ;
STRONG      : 'STRONG' ;   // TYPEDEF STRONG (ISO §13.18.58.2, COBOL-2002; data-model D17)
SORT_MERGE  : 'SORT-MERGE' ;
MULTIPLE    : 'MULTIPLE' ;
TAPE        : 'TAPE' ;
POSITION    : 'POSITION' ;
SEND        : 'SEND' ;         // MCS SEND (ISO 14.9.38) - recognized-not-supported (4.2.6, A.3 item 4)
SECURE      : 'SECURE' ;
SECURITY    : 'SECURITY' ;
SELECT      : 'SELECT' ;
SELF        : 'SELF' ;
SEPARATE    : 'SEPARATE' ;
SEQUENCE    : 'SEQUENCE' ;
SEQUENTIAL  : 'SEQUENTIAL' ;
SIGN        : 'SIGN' ;
SIGNED      : 'SIGNED' ;     // COBOL-2002 BINARY-xxx SIGNED (ISO §13.18.60); longest-match beats SIGN
UNSIGNED    : 'UNSIGNED' ;   // COBOL-2002 BINARY-xxx UNSIGNED
SIZE        : 'SIZE' ;
SOURCE      : 'SOURCE' ;
STANDARD_1  : 'STANDARD-1' ;
STANDARD_2  : 'STANDARD-2' ;
STATUS      : 'STATUS' ;
SUPER       : 'SUPER' ;
SYNC        : 'SYNC' ;
SYNCHRONIZED: 'SYNCHRONIZED' ;
TALLYING    : 'TALLYING' ;
TEST        : 'TEST' ;
THAN        : 'THAN' ;
THEN        : 'THEN' ;
THROUGH     : 'THROUGH' ;
THRU        : 'THRU' ;
TIME        : 'TIME' ;
TIMES       : 'TIMES' ;
TO          : 'TO' ;
TOP         : 'TOP' ;
TRAILING    : 'TRAILING' ;
TRUE_       : 'TRUE' ;
TYPE        : 'TYPE' ;
TYPEDEF     : 'TYPEDEF' ;
UNDERLINE_  : 'UNDERLINE' ;
UNIT        : 'UNIT' ;
UNTIL       : 'UNTIL' ;
UP          : 'UP' ;
USAGE       : 'USAGE' ;
USING       : 'USING' ;
VALIDATE    : 'VALIDATE' ;     // VALIDATE (ISO 14.9.50) - optional 4.2.7/A.4.14 + obsolete 4.2.13
VALUE       : 'VALUE' ;
VALUES      : 'VALUES' ;
VARYING     : 'VARYING' ;
WHEN        : 'WHEN' ;
WITH        : 'WITH' ;
ZERO        : 'ZERO' | 'ZEROS' | 'ZEROES' ;
SPACE       : 'SPACE' | 'SPACES' ;
HIGH_VALUE  : 'HIGH-VALUE' | 'HIGH-VALUES' ;
LOW_VALUE   : 'LOW-VALUE' | 'LOW-VALUES' ;
QUOTE_      : 'QUOTE' | 'QUOTES' ;

// ── Numeric literals (must come BEFORE IDENTIFIER) ──
// DECIMALLIT handles DOT-based decimals in the lexer (maximal munch resolves
// DOT-as-decimal vs DOT-as-sentence-terminator). COMMA-based decimals for
// DECIMAL-POINT IS COMMA are handled in the parser via numericLiteralCore.

// Floating-point numeric literal (ISO §8.3.3.3.3): a significand (which SHALL include a decimal point, 1-36 digits)
// joined to an exponent (1-4 digits, optionally signed) by 'E', no intervening spaces — e.g. 1.5E3, 2.5E-2, .5E10.
// MUST precede DECIMALLIT so maximal munch keeps "1.5E3" ONE token, not DECIMALLIT "1.5" + IDENTIFIER "E3" (the
// old parse error). Additive: the no-space <decimal>E<digits> form was previously always a parse error. (D16.)
FLOATLIT    : ( [0-9]+ '.' [0-9]* | '.' [0-9]+ ) 'E' [-+]? [0-9]+ ;

// ── Shared literal fragment bodies (rearchitecture PHASE 04, Group B) ──
// One definition per literal tokenization shape, referenced by BOTH the DEFAULT-mode literal tokens and their
// SUBSCRIPT-mode SUB_* twins (fragments are mode-independent). The two modes previously re-declared each body
// char-for-char; now a future string-escape / national-literal / data-name fix is applied ONCE and cannot diverge
// between modes (DESIGN-frontend-grammar §3.3b). Bodies are byte-identical to the retired inline forms.
fragment STR_BODY  : '"' (~["\r\n] | '""')* '"' | '\'' (~['\r\n] | '\'\'')* '\'' ;   // STRINGLIT / SUB_STRINGLIT
fragment NAT_BODY  : 'N' STR_BODY ;                                                  // NATLIT / SUB_NATLIT (N + string)
fragment BOOL_BODY : 'B' '"' [01]+ '"' | 'B' '\'' [01]+ '\'' ;                       // BOOLLIT / SUB_BOOLLIT
fragment INT_BODY  : [0-9]+ ;                                                        // INTEGERLIT / SUB_INTEGERLIT
fragment DEC_BODY  : [0-9]+ '.' [0-9]+ | '.' [0-9]+ ;                                // DECIMALLIT / SUB_DECIMALLIT
fragment NAME_BODY                                                                   // IDENTIFIER / SUB_IDENTIFIER
    : [0-9]+ '-' [a-z0-9] [a-z0-9-]*   // digit-start with hyphen: 42-DATANAMES
    | [0-9]+ [a-z] [a-z0-9-]*           // digit-start with letter: 11A, 25COUNT, 80PARTS
    | [a-z] [a-z0-9-]* [a-z0-9]         // alpha-start: WRK-DS-01V00
    | [a-z]                               // single letter: A
    ;

// ── Function-argument signed literals (P7 Step 12) ──
// Inside a FUNCTION argument region (and only there — the predicate), a sign that follows a separator and
// touches its digits is the leftmost CHARACTER of a numeric literal (ISO §8.3.3.3.2 r2); a binary operator is
// space-surrounded (§8.7.1). These twins re-type to the SUBSCRIPT-mode SIGNED_* token types so the parser sees
// one vocabulary: MAX(A -4) lexes A SIGNED_INTEGERLIT(-4) = two arguments; MAX(A - 4) lexes A MINUS 4 = one
// subtraction. Outside argument regions the predicate is false and [+-] lexes PLUS/MINUS exactly as before.
// The decimal twin MUST precede the integer twin (the SUBSCRIPT-mode ordering note: else -15.6 orphans ".6").
FN_SIGNED_DECIMALLIT : {SignedLiteralCanStart()}? [+-] DEC_BODY -> type(SIGNED_DECIMALLIT) ;
FN_SIGNED_INTEGERLIT : {SignedLiteralCanStart()}? [+-] INT_BODY -> type(SIGNED_INTEGERLIT) ;

DECIMALLIT  : DEC_BODY ;

// ── IDENTIFIER (must come BEFORE INTEGERLIT) ──
// COBOL-85 user-defined words: 1-30 chars from {A-Z, a-z, 0-9, hyphen},
// must contain at least one letter, no leading/trailing hyphen.
// Digit-start forms: 42-DATANAMES (hyphen), 11A/25COUNT/80PARTS (letter).
// Pure digits remain INTEGERLIT (level numbers, paragraph numbers, etc.).

IDENTIFIER  : NAME_BODY ;

INTEGERLIT  : INT_BODY ;

// ── String literals ──

STRINGLIT   : STR_BODY ;
// National literal N"…" / N'…' (ISO §8.3.3.5, COBOL-2002). The leading N is part of the token so
// ANTLR's maximal-munch prefers it over IDENTIFIER (a bare N) and over a plain STRINGLIT; an
// identifier such as NAME is unaffected (it has no opening quote). NX"…" (hex national) is deferred.
NATLIT      : NAT_BODY ;
HEXLIT      : [x] '"' [0-9a-f]+ '"'
            | [x] '\'' [0-9a-f]+ '\''
            ;
// Boolean literal B"0101" / B'0101' (binary digits only; ISO §8.3.3.4, COBOL-2002). The leading B is part
// of the token so maximal-munch prefers it over IDENTIFIER (a bare B) and over a plain STRINGLIT ("B"…").
// BX"…" (hex boolean) is deferred.
BOOLLIT     : BOOL_BODY ;

// ── Operators (multi-char before single-char) ──

POWER       : '**' ;
LTEQUAL     : '<=' ;
GTEQUAL     : '>=' ;
NOTEQUAL    : '<>' ;

DOT         : '.' ;
// P7 Step 12: inside a FUNCTION-argument region the ','/';'-plus-space separator (§8.3.5 rules 1/2) is a REAL
// token — the argument boundary must survive to the parser: a '(' right after it opens a PARENTHESIZED
// ARGUMENT, and the LPAREN whitelist action sees the separator (not the previous argument's data-name) as the
// previous token, so `MAX(A * B, (C + 1) / 2, …)` (IF119A/IF123A) does not mis-lex the group as a subscript
// of B. Outside argument regions both separators stay skipped exactly as before. MUST precede COMMA_SEP
// (equal-length match — first rule wins when the predicate holds).
FNARG_SEPARATOR : {InFunctionArgs()}? [,;] [ \t\r\n]+ ;
// §8.3.5: comma followed by whitespace is a separator (equivalent to space).
// Comma NOT followed by whitespace is preserved for DECIMAL-POINT IS COMMA.
COMMA_SEP   : ',' [ \t\r\n]+ -> skip ;
COMMA       : ',' ;
LPAREN      : '(' { OnDefaultLParen(); } ;
RPAREN      : ')' { OnDefaultRParen(); } ;
LT          : '<' ;
GT          : '>' ;
EQUALS      : '=' ;
PLUS        : '+' ;
MINUS       : '-' ;
STAR        : '*' ;
SLASH       : '/' ;
COLON       : ':' ;
// The concatenation operator (ISO §8.7.3): the COBOL character '&', joining literals into one literal
// (§8.8.3). The §8.7.3 separator-space requirement ("immediately preceded and followed by a separator
// space") is not enforced at the token level — the parser sees the skipped-WS stream, the same leniency
// every other separator-adjacent operator (e.g. '::' §8.7.4) already has. '&' has no other lexical role,
// so the token is unambiguous with or without the spaces.
AMPERSAND   : '&' ;
SEMICOLON   : ';' -> skip ;   // §8.3.5: semicolon-space is equivalent to space

// ── Catch-all for unrecognized characters ──

ANY_CHAR    : . ;

// ==========================================
// PICMODE — captures PIC/PICTURE string as one token
// ==========================================
// After PIC/PICTURE, optionally skip IS, then capture the entire
// PIC string (e.g., X(120), S9(18), $$$,$$9.99CR) as one token.
//
// Key insight: PIC strings never contain spaces. A period within
// a PIC string (like 9.99) is always followed by another PIC char,
// while a sentence-ending period is followed by whitespace/EOF.

mode PICMODE;

PIC_IS      : 'IS' -> skip ;              // optional IS keyword
PIC_WS      : [ \t\r\n]+ -> skip ;        // skip whitespace
PIC_STRING  : ( ~[ \t\r\n.] | '.' ~[ \t\r\n] )+
    {
        // Handle PIC "999999999999.." — greedy match consumed sentence-ending period.
        // If the PIC string ends with '.' and the char that caused the match was also '.',
        // trim the trailing period and back up so it becomes a DOT token.
        var t = Text;
        if (t.Length > 1 && t[t.Length - 1] == '.')
        {
            Text = t.Substring(0, t.Length - 1);
            InputStream.Seek(InputStream.Index - 1);
        }
        // A trailing ',' or ';' IMMEDIATELY FOLLOWED BY A SPACE is the CLAUSE SEPARATOR over-captured by the
        // greedy match (ISO §8.3.5 rule 2) — `77 X PIC 99, VALUE 3.` must lex the picture as "99" (VCR Table 7
        // row 7.14; the W2 adversarial-review finding; the ';' twin was NC203A/245A/251A/252A). The
        // following-character GUARD is load-bearing: a LEGAL trailing ',' (§13.18.40.3 SR7 — PICTURE as the
        // last clause) is followed by the separator PERIOD, not a space (NC125A's `PIC 9,9,…,9,.` — the token
        // ends at the ',' because '.'+newline cannot extend the match, so LA(1) is '.' and the ',' is a
        // PICTURE SYMBOL to keep). The seek-back re-lexes a trimmed separator in DEFAULT mode
        // (COMMA_SEP / SEMICOLON — both skipped).
        else if (t.Length > 1 && (t[t.Length - 1] == ',' || t[t.Length - 1] == ';'))
        {
            int la = InputStream.LA(1);
            if (la == ' ' || la == '\t' || la == '\r' || la == '\n' || la == Antlr4.Runtime.IntStreamConstants.EOF)
            {
                Text = t.Substring(0, t.Length - 1);
                InputStream.Seek(InputStream.Index - 1);
            }
        }
    } -> popMode ;
    // Matches: any non-whitespace-non-period char,
    //      OR: a period followed by a non-whitespace char (embedded decimal)
    // Post-action: if PIC string ends with '.', the greedy match consumed a
    //   sentence-ending period — back up one char so it tokenizes as DOT;
    //   likewise a trailing ','/';' clause separator (§8.3.5 r2) backs up and re-lexes as a skip token.

// ==========================================
// COMMENT_MODE — *> to end of line
// ==========================================

// ==========================================
// SUBSCRIPT MODE — COBOL-85 §5.3 subscript lexing
// ==========================================
// Entered when '(' follows an IDENTIFIER. Whitespace is preserved (not skipped)
// and sign adjacency is distinguished: +1 (SIGNED_INTEGERLIT) vs + 1 (SUB_PLUS SUB_WS SUB_INTEGERLIT).

mode SUBSCRIPT;

SUB_WS              : [ \t\r\n]+ ;

// Keywords must precede SUB_IDENTIFIER (same length → first rule wins)
SUB_OF              : 'OF' ;
SUB_IN              : 'IN' ;
SUB_ALL             : 'ALL' ;

// Sign immediately adjacent to a decimal literal: -15.6, +0.2, -.5 (signed decimal
// argument to an intrinsic function, ISO §15). MUST precede SIGNED_INTEGERLIT so the
// fractional part is not orphaned: ANTLR longest-match makes "-15.6" one SIGNED_DECIMALLIT
// rather than SIGNED_INTEGERLIT "-15" + SUB_DECIMALLIT ".6" (which silently dropped the .6).
SIGNED_DECIMALLIT   : [+-] [0-9]+ '.' [0-9]+ | [+-] '.' [0-9]+ ;

// Sign immediately followed by digits: +1, -10 (signed literal subscript)
SIGNED_INTEGERLIT   : [+-] [0-9]+ ;

// Numeric literals
SUB_INTEGERLIT      : INT_BODY ;
SUB_DECIMALLIT      : DEC_BODY ;

// Alphanumeric literal — needed for string-valued intrinsic-function arguments,
// e.g. FUNCTION LOWER-CASE("ABC"), FUNCTION NUMVAL("12.3"). Mirrors STRINGLIT.
SUB_STRINGLIT       : STR_BODY ;

// National/boolean literal arguments (N"…"/B"…", ISO §8.3.3.5/§8.3.3.4) — mirror NATLIT/BOOLLIT. MUST
// precede SUB_IDENTIFIER (and win by longest match anyway) so the prefix letter is never orphaned as a
// one-character data-name with the quoted body becoming a separate SUB_STRINGLIT — that shape silently
// misbound FUNCTION LENGTH(N"AB") before these tokens existed (Phase 4a, the proper-token rule).
SUB_NATLIT          : NAT_BODY ;
SUB_BOOLLIT         : BOOL_BODY ;

// Data-name / index-name (must follow keywords to avoid capturing OF/IN/ALL)
SUB_IDENTIFIER      : NAME_BODY ;

// Operators and punctuation. Arithmetic operators (*, /, **) are needed because
// intrinsic-function arguments — captured in this mode to preserve comma/space
// separators — may be full arithmetic expressions (ISO §15), e.g. MEAN(9 * A, B / 2).
// SUB_POWER must precede SUB_STAR so '**' is one token, not two.
SUB_PLUS            : '+' ;
SUB_MINUS           : '-' ;
SUB_POWER           : '**' ;
SUB_STAR            : '*' ;
SUB_SLASH           : '/' ;
SUB_COMMA           : ',' ;
SUB_SEMICOLON       : ';' ;  // §8.3.5: semicolon is interchangeable with comma
SUB_COLON           : ':' ;
SUB_LPAREN          : '(' -> pushMode(SUBSCRIPT) ;
SUB_RPAREN          : ')' -> popMode ;
SUB_ANY             : . ;

// ==========================================
// COMMENT_MODE — *> to end of line
// ==========================================

mode COMMENT_MODE;

COMMENT_TEXT : ~[\r\n]+ -> skip ;
COMMENT_END  : [\r\n]   -> popMode, skip ;
