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
    : cobolWord (IS cobolWord)? switchOnClause? switchOffClause?
    ;

switchOnClause
    : ON STATUS IS cobolWord
    | ON IS? cobolWord
    ;

switchOffClause
    : OFF STATUS IS? cobolWord
    | OFF IS? cobolWord
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
    : CLASS cobolWord IS? classValueSet ({is2002()}? FOR (ALPHANUMERIC | NATIONAL))? (IN cobolWord)?
    ;

classValueSet
    : classValueItem (COMMA classValueItem)*
    ;

classValueItem
    : literal ((THRU | THROUGH) literal)?
    ;

// SYMBOLIC CHARACTERS [FOR {ALPHANUMERIC|NATIONAL}]
//   {name}... {IS|ARE} {integer}... [IN alphabet-name] ...
// N:N positional mapping: first name ↔ first integer, etc. (§12.3.7)
symbolicCharactersClause
    : SYMBOLIC CHARACTERS ({is2002()}? FOR (ALPHANUMERIC | NATIONAL))?
      symbolicCharacterEntry+ (IN cobolWord)?
    ;

symbolicCharacterEntry
    : cobolWord+ (IS | ARE) integerLiteral+
    ;

// ALPHABET name IS ... [FOR {ALPHANUMERIC|NATIONAL}]
alphabetClause
    : ALPHABET cobolWord IS alphabetDefinition ({is2002()}? FOR (ALPHANUMERIC | NATIONAL))?
    ;

// NATIVE, STANDARD-1, STANDARD-2 are dedicated lexer tokens.
alphabetDefinition
    : NATIVE
    | STANDARD_1
    | STANDARD_2
    | alphabetEntry (COMMA? alphabetEntry)*
    ;

alphabetEntry
    : (cobolWord | literal) ((THRU | THROUGH) (cobolWord | literal))?
      (ALSO (cobolWord | literal))*
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
