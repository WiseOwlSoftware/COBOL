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
    // CRT STATUS / CURSOR (§12.3.7; Annex A.4.2 item 25 — the DECLINED screen module, refused by name at bind
    // with COBOLNET1560). ⚠ RESERVATION-GATED. §8.9 reserves CRT and CURSOR from 2002 ONLY, so at COBOL-85 they
    // are ordinary user words and `SPECIAL-NAMES. CURSOR IS FOO.` is a legal '85 implementor-switch entry that
    // must keep parsing as one — the localeClause precedent exactly. Without the gate the '85 program drew a
    // screen-facility refusal for a switch entry that has nothing to do with screens (kb/Work PB301).
    | {reservedHere("CRT")}?    crtStatusClause DOT?
    | {reservedHere("CURSOR")}? cursorClause DOT?
    | channelClause DOT?
    | reserveClause DOT?
    // MUST precede implementorSwitchEntry: `LOCALE FR IS "fr_FR"` otherwise matches `cobolWord (IS cobolWord)?`
    // as far as the bare word LOCALE, then re-enters as `FR IS "fr_FR"` and dies on the LITERAL with
    // COBOL0308 "a data-name is expected here, not a literal" — a parse error pointing at the wrong token, for a
    // clause the standard defines (fix-queue PB25).
    | localeClause DOT?
    // MUST precede implementorSwitchEntry, for the LOCALE clause's reason: ORDER is not a lexer token, so
    // `ORDER TABLE OT1 IS "…"` otherwise matches `cobolWord (IS cobolWord)?` as far as the bare word ORDER, and
    // the entry loop re-enters on TABLE — a keyword token no cobolWord admits — dying on a token the user never
    // wrote a clause around (kb/Work PB101).
    | orderTableClause DOT?
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

// ORDER TABLE ordering-name-1 IS literal-9 (ISO §12.3.7.2 — the LAST item of the SPECIAL-NAMES general format,
// bracketed and therefore at most once). ORDER is NOT a lexer token: it is reserved from 2002 through the §8.9
// funnel and arrives as an ordinary cobolWord, exactly as the LOCALE clause's own keyword does — so the clause is
// recognized by a left-edge predicate on the word pair (ORDER, TABLE). TABLE *is* a token.
// literal-9 names the cultural ordering table (§12.3.7.4 GR17 — "The implementor specifies the allowable content
// of literal-9"); ordering-name-1 "may be specified only in the STANDARD-COMPARE intrinsic function"
// (§12.3.7.3 SR9). The binder (DataBinder.OrderTableBind) enforces SR10/SR11 and registers the name.
// ⚠ NOT edition-gated, unlike localeClause: TABLE is reserved at EVERY edition, so the pair ORDER + TABLE has no
// competing COBOL-85 reading (nothing else in a SPECIAL-NAMES paragraph can be followed by, or begin with, TABLE).
// Recognizing the clause at every --std is what lets the version pass answer below 2002 with the explanatory
// `order-table-2002` introduction gate rather than a parse error — see orderTableAhead()'s comment.
orderTableClause
    : {orderTableAhead()}? cobolWord TABLE cobolWord IS? literal
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

// { literal-5 [ THROUGH literal-6 ] }… — JUXTAPOSED groups (the SPECIAL-NAMES paragraph's §12.3.7.2
// diagram carries its own ellipsis
// on the group; the comma is the optional separator it is everywhere). `(COMMA x)*` REQUIRED a comma between
// groups and rejected `CLASS HEXDIG IS "0" THRU "9" "A" THRU "F"` — legal COBOL at every edition (kb/Work PB60's
// configuration-inheritance golden surfaced it, 2026-08-17; the OO in-line invocation's argumentList had the
// same shape and was swept in the same change).
classValueSet
    : classValueItem (COMMA? classValueItem)*
    ;

classValueItem
    : literal ((THRU | THROUGH) literal)?
    ;

// SYMBOLIC CHARACTERS [FOR {ALPHANUMERIC|NATIONAL}]
//   {name}... {IS|ARE} {integer}... [IN alphabet-name] ...
// N:N positional mapping: first name ↔ first integer, etc. (SPECIAL-NAMES paragraph, §12.3.7)
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
