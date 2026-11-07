*>--- conformance-case
*> id: pb4572-cobol-words-undefined-comma-is-no-decimal-point-word
*> rules: [GR-7.3.10.4-3, GR-12.3.7.4-14]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 7.3.10.4 GR3 (kb/Work PB4572): "When the UNDEFINE option is specified, the COBOL word that is the
*> content of literal-3 shall no longer be reserved or restricted in any way, and may be used as a user-defined
*> intrinsic name, data-name or any other user-defined word."
*>   cite.py: OK  §2023:7.3.10.4 3)  (General rules)
*> COMMA, undefined, is a user-defined word in this compilation group and the syntax that requires the reserved word
*> COMMA is not available there: DECIMAL-POINT IS COMMA is refused (COBOL0894) and the comma is no decimal separator.
*> Before the fix the binder's spelling test of COMMA took the withdrawn word for the clause's word and accepted it.
       >>COBOL-WORDS UNDEFINE "COMMA"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4572NC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 DP PIC 9V99 VALUE 1.5.
       PROCEDURE DIVISION.
           DISPLAY DP
           STOP RUN.
