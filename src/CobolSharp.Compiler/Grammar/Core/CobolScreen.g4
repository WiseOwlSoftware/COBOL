// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

// SCREEN SECTION and all screen description entry clauses.
// Imported by CobolParserCore.g4 — no options block.

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
    | globalClause
    ;

// LINE NUMBER IS [PLUS | +] {identifier | integer}
// Note: the word PLUS lexes as IDENTIFIER, the symbol + lexes as PLUS
screenLineClause
    : LINE NUMBER? IS? (IDENTIFIER | PLUS)? (dataReference | integerLiteral)
    ;

// {COLUMN | COL} NUMBER IS [PLUS | +] {identifier | integer}
screenColumnClause
    : (COLUMN | COL) NUMBER? IS? (IDENTIFIER | PLUS)? (dataReference | integerLiteral)
    ;

// BLANK {LINE | SCREEN}
screenBlankClause
    : BLANK (LINE | SCREEN)
    ;

// ERASE {EOL | EOS}
screenEraseClause
    : ERASE (EOL | EOS)
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
