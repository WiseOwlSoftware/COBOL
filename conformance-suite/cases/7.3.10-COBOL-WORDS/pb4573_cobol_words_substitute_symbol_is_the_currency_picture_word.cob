*>--- conformance-case
*> id: pb4573_cobol_words_substitute_symbol_is_the_currency_picture_word
*> rules: [GR-7.3.10.4-4, SR-12.3.7.3-23]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 7.3.10.4 GR4 (kb/Work PB4573): "When the SUBSTITUTE option is specified, the COBOL word that is the
*> content of literal-5 shall be used in any syntax where the COBOL word that is the content of literal-4 is
*> documented as required or optional."
*>   cite.py: OK  §7.3.10.4 4)  (General rules)
*> 12.3.7.2 general format: [ CURRENCY SIGN IS literal-7 [ WITH PICTURE SYMBOL literal-8 ] ] ...
*>   cite.py: OK  §12.3.7.2 (General format)  (the SIGN IS literal-7 line)
*> 12.3.7.3 SR23: "If the PICTURE SYMBOL phrase is specified, literal-7 is the currency string and literal-8 is the
*> associated currency symbol."
*> SYMBOL is a context-sensitive word (8.10, CURRENCY clause), so SUBSTITUTE may name it (7.3.10.3 SR3) and SYM is the
*> word the phrase is written with. The refusal of the withdrawn word SYMBOL names the directive (see
*> wiseowl/diagnostics/pb4573-cobol-words-undefined-symbol-withdrawn-from-currency-picture.json).
*> Why each leg can fail: with the clause bound, U is the currency symbol of the string "USD" (SR23), so PIC U9.99
*> is a fixed insertion of the currency string before the digits (13.18.40.5 r5) and 5.25 edits to USD5.25 (the same
*> derivation as pb4572's FU). If the clause were refused, U would be no picture symbol.
*> EXPECTED OUTPUT: FU=[USD5.25]
       >>COBOL-WORDS SUBSTITUTE "SYMBOL" BY "SYM"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4573SY.
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
