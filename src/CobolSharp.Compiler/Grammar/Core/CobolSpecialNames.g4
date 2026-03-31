// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

// SPECIAL-NAMES paragraph and all its clauses.
// Imported by CobolParserCore.g4 — no options block.

parser grammar CobolSpecialNames;

options {
    tokenVocab = CobolLexer;
}

// SPECIAL-NAMES.
specialNamesParagraph
    : SPECIAL_NAMES DOT specialNameEntry*
    ;

specialNameEntry
    : currencySignClause DOT?
    | decimalPointClause DOT?
    | classDefinitionClause DOT?
    | symbolicCharactersClause DOT?
    | alphabetClause DOT?
    | crtStatusClause DOT?
    | cursorClause DOT?
    | channelClause DOT?
    | reserveClause DOT?
    | implementorSwitchEntry DOT?
    | genericClause DOT?
    ;

implementorSwitchEntry
    : IDENTIFIER (IS IDENTIFIER)? switchOnClause? switchOffClause?
    ;

switchOnClause
    : ON STATUS IS IDENTIFIER
    | ON IS? IDENTIFIER
    ;

switchOffClause
    : OFF STATUS IS? IDENTIFIER
    | OFF IS? IDENTIFIER
    ;

// CURRENCY SIGN IS literal [WITH PICTURE SYMBOL literal]
// Note: PICTURE lexes as PIC (pushes PICMODE); full WITH PICTURE SYMBOL
// support requires lexer changes. For now, the optional phrase is omitted.
currencySignClause
    : CURRENCY SIGN? IS? literal
    ;

decimalPointClause
    : DECIMAL_POINT IS IDENTIFIER    // DECIMAL-POINT IS COMMA (COMMA is IDENTIFIER)
    ;

// CLASS name IS literal [THRU literal] [, literal [THRU literal]]... [FOR {ALPHANUMERIC|NATIONAL}] [IN alphabet-name]
classDefinitionClause
    : CLASS IDENTIFIER IS? classValueSet (FOR (ALPHANUMERIC | NATIONAL))? (IN IDENTIFIER)?
    ;

classValueSet
    : classValueItem (COMMA classValueItem)*
    ;

classValueItem
    : literal ((THRU | THROUGH) literal)?
    ;

// SYMBOLIC CHARACTERS {name}... {IS|ARE} {integer}... [IN alphabet-name] ...
symbolicCharactersClause
    : SYMBOLIC CHARACTERS symbolicCharacterEntry+ (IN IDENTIFIER)?
    ;

symbolicCharacterEntry
    : IDENTIFIER (IS | ARE) literal
    ;

// ALPHABET name IS ... [FOR {ALPHANUMERIC|NATIONAL}]
alphabetClause
    : ALPHABET IDENTIFIER IS alphabetDefinition (FOR (ALPHANUMERIC | NATIONAL))?
    ;

// NATIVE, STANDARD-1, STANDARD-2 are dedicated lexer tokens.
alphabetDefinition
    : NATIVE
    | STANDARD_1
    | STANDARD_2
    | alphabetEntry (COMMA? alphabetEntry)*
    ;

alphabetEntry
    : (IDENTIFIER | literal) ((THRU | THROUGH) (IDENTIFIER | literal))?
      (ALSO (IDENTIFIER | literal))*
    ;

// CRT STATUS IS data-name
crtStatusClause
    : CRT STATUS IS dataReference
    ;

// CURSOR IS data-name
cursorClause
    : CURSOR IS dataReference
    ;

// CHANNEL integer IS data-name
channelClause
    : CHANNEL integerLiteral IS dataReference
    ;

// RESERVE integer [CHANNELS]
reserveClause
    : RESERVE integerLiteral IDENTIFIER?
    ;

// fallback for vendor extensions
vendorConfigurationParagraph
    : genericClause DOT
    ;
