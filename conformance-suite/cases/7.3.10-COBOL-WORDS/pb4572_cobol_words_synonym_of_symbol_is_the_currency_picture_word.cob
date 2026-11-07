*>--- conformance-case
*> id: pb4572_cobol_words_synonym_of_symbol_is_the_currency_picture_word
*> rules: [GR-7.3.10.4-2, SR-12.3.7.3-23]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 7.3.10.4 GR2 (kb/Work PB4572): "When the EQUATE option is specified, the COBOL word that is the
*> content of literal-2 shall be treated as a synonym for the COBOL word that is the content of literal-1, and may
*> be used in any syntax requiring the use of the reserved word, context-sensitive word, or intrinsic function name
*> that is the content of literal-1."
*>   cite.py: OK  §2023:7.3.10.4 2)  (General rules)
*> 12.3.7.2 general format: [ CURRENCY SIGN IS literal-7 [ WITH PICTURE SYMBOL literal-8 ] ] ...
*>   cite.py: OK  §2023:12.3.7.2 (General format)  (the SIGN IS literal-7 line)
*> 12.3.7.3 SR23: "If the PICTURE SYMBOL phrase is specified, literal-7 is the currency string and literal-8 is the
*> associated currency symbol."
*> SYMBOL is a context-sensitive word (8.10, CURRENCY clause) the lexer has no token for, and the word after PICTURE
*> reaches the binder as a picture-string token whose text the binder compared with SYMBOL. SYM, its synonym, is the
*> word the clause requires (GR2); the clause was refused with COBOL0892 ("expected WITH PICTURE SYMBOL, found PICTURE SYM").
*> Why each leg can fail: with the clause bound, U is the currency symbol of the string "USD" (SR23), so PIC U9.99
*> is a fixed insertion of the currency string before the digits (13.18.40.5 r5) and 5.25 edits to USD5.25 (the same
*> derivation as pb60_currency_sign_set's FU). If the clause were refused, U would be no picture symbol.
*> EXPECTED OUTPUT: FU=[USD5.25]
       >>COBOL-WORDS EQUATE "SYMBOL" WITH "SYM"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4572SY.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "USD" WITH PICTURE SYM "U".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FU PIC U9.99.
       PROCEDURE DIVISION.
           MOVE 5.25 TO FU
           DISPLAY "FU=[" FU "]"
           STOP RUN.
