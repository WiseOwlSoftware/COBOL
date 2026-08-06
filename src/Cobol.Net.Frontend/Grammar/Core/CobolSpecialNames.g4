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
    // MUST precede implementorSwitchEntry: `LOCALE FR IS "fr_FR"` otherwise matches `cobolWord (IS cobolWord)?`
    // as far as the bare word LOCALE, then re-enters as `FR IS "fr_FR"` and dies on the LITERAL with
    // COBOL0308 "a data-name is expected here, not a literal" — a parse error pointing at the wrong token, for a
    // clause the standard defines (fix-queue PB25).
    | localeClause DOT?
    | implementorSwitchEntry DOT?
    | genericClause DOT?
    ;

// LOCALE locale-name-1 IS { external-locale-name-1 | literal-4 } (§12.3.7).
// ⛔ PARSED SO IT CAN BE DIAGNOSED, NOT SO IT CAN BE USED. This is an §A.4.9 item 10 optional-locale element —
// "SPECIAL-NAMES paragraph: LOCALE clause and LOCALE phrases in the ALPHABET clause (12.3.7)" — and COBOL.NET's
// documented non-support of the locale module is conformant per §4.2.7 / §A.4.1 ONLY if it is DIAGNOSED. The
// binder emits COBOLNET1518, the same cited message the LOCALE phrase of LOWER-CASE / NUMVAL-C already gets; a
// raw parse error is not documented non-support, it is an unexplained rejection. Superset-parse / bind-narrow,
// exactly as the rest of this grammar does it.
// ⚠ The predicate is EDITION-GATED (2002+, where §8.9 reserves LOCALE). At COBOL-85 LOCALE is an ordinary user
// word, so `SPECIAL-NAMES. LOCALE IS FOO.` is a legal implementor-switch entry and must keep parsing as one —
// below 2002 localeClauseAhead() is false and this alternative is unreachable.
localeClause
    : {localeClauseAhead()}? cobolWord cobolWord IS? (cobolWord | literal)
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
// PICMODE exploit: PICTURE triggers PIC token + pushes PICMODE, which captures
// "SYMBOL" as PIC_STRING. Parser sees: WITH PIC PIC_STRING literal.
// Semantic validation ensures PIC_STRING == "SYMBOL".
currencySignClause
    : CURRENCY SIGN? IS? literal (WITH PIC PIC_STRING literal)?
    ;

decimalPointClause
    : DECIMAL_POINT IS IDENTIFIER    // DECIMAL-POINT IS COMMA (COMMA is IDENTIFIER)
    ;

// CLASS name IS literal [THRU literal] [, literal [THRU literal]]... [FOR {ALPHANUMERIC|NATIONAL}] [IN alphabet-name]
classDefinitionClause
    : CLASS cobolWord IS? classValueSet (FOR (ALPHANUMERIC | NATIONAL))? (IN cobolWord)?
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
    : SYMBOLIC CHARACTERS (FOR (ALPHANUMERIC | NATIONAL))?
      symbolicCharacterEntry+ (IN cobolWord)?
    ;

symbolicCharacterEntry
    : cobolWord+ (IS | ARE) integerLiteral+
    ;

// ALPHABET alphabet-name-1 [FOR ALPHANUMERIC] IS {NATIVE|STANDARD-1|STANDARD-2|literal-phrase…}
// ALPHABET alphabet-name-2 FOR NATIONAL IS {NATIVE|UCS-4|UTF-8|UTF-16|literal-phrase…}   (ISO §12.3.7.2)
// The FOR phrase's ISO position is BETWEEN the name and IS; the historical postfix position (after the
// definition) is kept as an accepted superset (pre-existing corpus surface). The binder reads either site
// and rejects a clause writing both. UCS-4/UTF-8/UTF-16 are §8.9 CONTEXT-SENSITIVE words (ALPHABET clause
// scope) — they arrive as ordinary cobolWord entries and are recognized BY TEXT in the binder/pass, never
// as lexer keywords (they stay user-definable outside this clause).
alphabetClause
    : ALPHABET cobolWord alphabetForPhrase? IS alphabetDefinition alphabetForPhrase?
    ;

alphabetForPhrase
    : FOR (ALPHANUMERIC | NATIONAL)
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
