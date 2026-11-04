*>--- conformance-case
*> id: pb4573-cobol-words-undefined-symbol-withdrawn-from-currency-picture
*> rules: [GR-7.3.10.4-3, SR-12.3.7.3-23]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 7.3.10.4 GR3 (kb/Work PB4573): "When the UNDEFINE option is specified, the COBOL word that is the
*> content of literal-3 shall no longer be reserved or restricted in any way, and may be used as a user-defined
*> intrinsic name, data-name or any other user-defined word, and any syntax requiring the use of the COBOL word that is
*> the content of literal-3 shall not be available for use in this compilation group."
*>   cite.py: OK  §7.3.10.4 3)  (General rules)
*> SYMBOL (a context-sensitive word, 8.10; 7.3.10.3 SR3 admits it as literal-3), undefined, is a user-defined word in
*> this compilation group, and the WITH PICTURE SYMBOL phrase of the CURRENCY SIGN clause (12.3.7.2) requires it, so the
*> phrase is not available and this program is refused. The refusal read "expected 'WITH PICTURE SYMBOL', found 'PICTURE
*> SYMBOL'", which asks for the word the program wrote; it now names the directive.
       >>COBOL-WORDS UNDEFINE "SYMBOL"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4573US.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "USD" WITH PICTURE SYMBOL "U".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FU PIC 9.99.
       PROCEDURE DIVISION.
           MOVE 5.25 TO FU
           DISPLAY FU
           STOP RUN.
