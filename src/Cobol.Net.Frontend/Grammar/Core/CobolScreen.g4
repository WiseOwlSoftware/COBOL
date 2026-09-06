// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

// SCREEN SECTION, all screen description entry clauses, and the three screen STATEMENT surfaces
// (ACCEPT format 3, DISPLAY format 2, SET format 6) — ISO/IEC 1989:2023 Annex A.4.2.
// Imported by CobolParserCore.g4 — no options block.
//
// ⛔ THIS WHOLE FILE IS PARSE-SO-IT-CAN-BE-DIAGNOSED, NEVER PARSE-SO-IT-CAN-BE-USED. Screen handling is the
// largest DECLINED optional module (docs/CONFORMANCE.md §5 "A.4.2 … Not claimed"); A.4.1 admits an optional
// element's syntax only when support is claimed, so every construct here is refused at BIND with the named
// COBOLNET1560 (data/environment surface) or COBOLNET1707 (procedure surface) — see ScreenFacility.cs. The
// surface exists because "a named refusal that says WHICH facility" beats a generic COBOL0001 parse error, and
// because without it the two statement formats do not fail at all: `DISPLAY screen-name-1` and
// `ACCEPT screen-name-1` are token-identical to their DEVICE formats and were silently re-read as those, i.e.
// a declined facility that compiled and transferred the wrong data (kb/Work PB260).

parser grammar CobolScreen;

options {
    tokenVocab = CobolLexer;
}

// ==========================================
// SCREEN SECTION (ISO 2002 §13.9)
// ==========================================

screenSection
    : SCREEN SECTION DOT screenDescriptionEntry*
    ;

screenDescriptionEntry
    : levelNumber screenName? screenDescriptionBody DOT
    ;

screenName
    : cobolWord
    | FILLER
    ;

screenDescriptionBody
    : screenClause*
    ;

screenClause
    : screenLineClause
    | screenColumnClause
    | screenBlankClause
    | screenEraseClause
    | screenBellClause
    | screenBlinkClause
    | screenHighlightClause
    | screenLowlightClause
    | screenReverseVideoClause
    | screenUnderlineClause
    | screenForegroundColorClause
    | screenBackgroundColorClause
    | screenAutoClause
    | screenSecureClause
    | screenFullClause
    | screenRequiredClause
    | pictureClause
    | screenFromClause
    | screenToClause
    | screenUsingClause
    | valueClause
    | blankWhenZeroClause
    | justifiedClause
    | signClause
    | occursClause
    | usageClause
    | globalClause
    ;

// LINE NUMBER IS [PLUS | + | MINUS | -] {identifier-1 | integer-1}   (ISO §13.17.2 formats 1 and 2, §13.18.35
// format 2). Measured on the rendered page (PDF page 431 = §13.17.2): NUMBER and IS are NOT underlined, so both
// are optional words; the four-way sign group is a BRACKET, so it too is optional; and MINUS / '-' are printed
// alternatives the OCR'd list had, but this rule did not.
// PLUSWORD = the reserved word PLUS; PLUS = the '+' symbol. ⚠ There is NO lexer token for the WORD MINUS (it is
// §8.9-reserved from 2002 and modelled in the reserved-word funnel, not the lexer), so it arrives as an
// IDENTIFIER — which is why IDENTIFIER is an alternative here. That over-admits (`LINE FOO 5` parses), and that
// is harmless in a DECLINED module: everything this rule matches is refused by name at bind, so a wider surface
// only converts more COBOL0001 parse errors into the named COBOLNET1560.
screenLineClause
    : LINE NUMBER? IS? (IDENTIFIER | PLUS | PLUSWORD | MINUS)? (dataReference | integerLiteral)
    ;

// {COLUMN | COL | COLUMNS | COLS} NUMBER IS [PLUS | + | MINUS | -] {identifier-2 | integer-2}
// (ISO §13.17.2; §13.18.14 SR1 admits the plural spellings).
screenColumnClause
    : (COLUMN | COL | COLUMNS | COLS) NUMBER? IS? (IDENTIFIER | PLUS | PLUSWORD | MINUS)? (dataReference | integerLiteral)
    ;

// BLANK {LINE | SCREEN}   (§13.18.7; the screen description entry's §13.17.2 format 2 (elementary) is
// where it may appear — format 1 (group) admits SCREEN only, bind-narrowed rather than grammar-narrowed,
// since the whole entry is refused anyway)
screenBlankClause
    : BLANK (LINE | SCREEN)
    ;

// ERASE {[END] [OF] {LINE | SCREEN} | EOL | EOS}   (§13.17.2 format 2 / §13.18.21.3 SR1: "The word EOL is
// equivalent to the words END OF LINE"). The two long spellings were missing and produced a parse error.
// ⛔ END AND OF ARE OPTIONAL WORDS (§8.3.2.4.3; kb/Work PB695 family 3). MEASURED on printed page 429 / folio
// 399: ERASE carries a rule at 92.2% cover, LINE 89.7%, SCREEN 94.0%, EOL 87.9% and EOS 87.5% — while BOTH
// occurrences of END (boxes 127.54–148.05 and 127.53–148.03) and BOTH of OF (150.39–163.11 and 150.37–163.09)
// have NO horizontal rule in their band at all. The transcription's own figure note agrees.
// ⚠ THE BRACE IS STILL REQUIRED: LINE / SCREEN / EOL / EOS stays a mandatory choice (§5.2.6.3), so a bare
// `ERASE` remains a parse error exactly as before — this relaxes the two words INSIDE the alternative, it does
// not make the alternative empty. With END and OF gone the clause can open on LINE, which is also the head of
// screenLineClause; that is unambiguous because ANTLR is deciding INSIDE screenEraseClause once ERASE is
// consumed, and screenClause* only resumes after the alternative completes.
screenEraseClause
    : ERASE (END? OF? (LINE | SCREEN) | EOL | EOS)
    ;

// Screen attribute clauses
screenBellClause          : BELL ;
screenBlinkClause         : BLINK ;
screenHighlightClause     : HIGHLIGHT ;
screenLowlightClause      : LOWLIGHT ;
screenReverseVideoClause  : REVERSE_VIDEO ;
screenUnderlineClause     : UNDERLINE_ ;

// FOREGROUND-COLOR IS {identifier | integer}
screenForegroundColorClause
    : FOREGROUND_COLOR IS? (dataReference | integerLiteral)
    ;

// BACKGROUND-COLOR IS {identifier | integer}
screenBackgroundColorClause
    : BACKGROUND_COLOR IS? (dataReference | integerLiteral)
    ;

// AUTO, SECURE, FULL, REQUIRED
screenAutoClause     : AUTO ;
screenSecureClause   : SECURE ;
screenFullClause     : FULL_ ;
screenRequiredClause : REQUIRED ;

// FROM {identifier | literal}
screenFromClause
    : FROM (dataReference | literal)
    ;

// TO identifier
screenToClause
    : TO dataReference
    ;

// USING identifier
screenUsingClause
    : USING dataReference
    ;

// ==========================================
// The screen STATEMENT surfaces (ACCEPT ISO §14.9.1 format 3, DISPLAY §14.9.11 format 2,
// SET §14.9.39 format 6)
// ==========================================

// The positioning phrase shared VERBATIM by ACCEPT format 3 and DISPLAY format 2. Rendered from the printed
// general formats (PDF pages 607 and 640 — the OCR'd diagrams were the reason to render):
//
//     [ AT {| [ LINE NUMBER {identifier|integer} ] [ {COLUMN|COL} NUMBER {identifier|integer} ] |} ]
//
// AT and NUMBER are NOT underlined (optional words); LINE, COLUMN and COL are. The inner group carries CHOICE
// INDICATORS (§5.2.6.4 — one or more, each at most once, ANY ORDER), which is why this is a `+` over the two
// legs and not a fixed LINE-then-COLUMN sequence: `DISPLAY SG COLUMN 5` with no LINE is legal, and it is exactly
// the shape kb/Work PB260 measured binding as a THREE-operand device DISPLAY.
// ⛔ THE EXCEPTION PHRASES ARE BOUND TO THE POSITIONING PHRASE, AND THAT COUPLING IS LOAD-BEARING — DO NOT
// "SIMPLIFY" IT BACK INTO TWO INDEPENDENT OPTIONAL TAILS. `ON EXCEPTION` / `NOT ON EXCEPTION` is spelled the
// same on a screen ACCEPT/DISPLAY as on the half-dozen statements that already own one, and DISPLAY appears
// inside every one of their imperative-statement slots. With a free-standing `screenExceptionPhrases?` the
// inner DISPLAY STEALS the enclosing statement's arm:
//
//     DELETE FILE F ON EXCEPTION DISPLAY "EXC" NOT ON EXCEPTION DISPLAY "NOEXC" END-DELETE
//
// parsed as DELETE's ON EXCEPTION containing `DISPLAY "EXC" NOT ON EXCEPTION DISPLAY "NOEXC"` — a screen
// DISPLAY — and DELETE lost its NOT-arm entirely. The wave-local gate caught it on four corpus programs
// (delete_file_absent, ec_external_{file,data,format}_*). Requiring the AT/LINE/COLUMN phrase first makes the
// tail unambiguous: no other statement's exception arm can begin with it, and a real screen ACCEPT/DISPLAY
// that wants an exception handler is positioned in practice. The cost is one spelling — an UNPOSITIONED
// `DISPLAY screen-name ON EXCEPTION …` — which stays a parse error exactly as it was before this wave, while
// the far more common `DISPLAY screen-name` alone is caught by the binder's screen-name test.
screenTail
    : screenPositionPhrase screenExceptionPhrases?
    ;

screenPositionPhrase
    : AT? screenPositionLeg+
    ;

screenPositionLeg
    : LINE NUMBER? IS? (dataReference | integerLiteral)
    | (COLUMN | COL | COLUMNS | COLS) NUMBER? IS? (dataReference | integerLiteral)
    ;

// [ ON EXCEPTION imperative-statement-1 ] [ NOT ON EXCEPTION imperative-statement-2 ] — ON is not underlined.
// Either order, each at most once (the printed stack is a bracketed pair, mcsExceptionPhrases' shape).
screenExceptionPhrases
    : ON? EXCEPTION statementBlock (NOT ON? EXCEPTION statementBlock)?
    | NOT ON? EXCEPTION statementBlock (ON? EXCEPTION statementBlock)?
    ;

// SET screen-name-1 ATTRIBUTE {| BELL | BLINK | HIGHLIGHT | LOWLIGHT | REVERSE-VIDEO | UNDERLINE |} {OFF|ON} …
// (ISO §14.9.39.2 Format 6, rendered from PDF page 760.)
// ⛔ THE FORMAT HAS NO `TO`. A proposal to witness this as `SET SG ATTRIBUTE HIGHLIGHT TO ON` would have been
// illegal source; the printed diagram puts ON/OFF directly after the attribute group.
// ATTRIBUTE is a §8.10 CONTEXT-SENSITIVE word ("SET statement"), not a reserved word and not a lexer token —
// so it arrives as an ordinary word and the arm is recognized by a LEFT-EDGE predicate on the text, the
// ORDER TABLE / SET LOCALE precedent. Not edition-gated: `SET x ATTRIBUTE …` has no other reading at any
// edition (Format 1 requires TO, Format 2 requires UP/DOWN BY), so recognizing it everywhere is what lets the
// named refusal replace a raw parse error at COBOL-85 too.
setScreenAttributeStatement
    : {setAttributeAhead()}? SET dataReference cobolWord screenAttributeSetting+
    ;

screenAttributeSetting
    : screenAttributeName+ (ON | OFF)
    ;

screenAttributeName
    : BELL | BLINK | HIGHLIGHT | LOWLIGHT | REVERSE_VIDEO | UNDERLINE_
    ;
