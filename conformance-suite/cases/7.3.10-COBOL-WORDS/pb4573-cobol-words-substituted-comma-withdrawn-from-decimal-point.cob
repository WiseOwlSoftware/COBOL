*>--- conformance-case
*> id: pb4573-cobol-words-substituted-comma-withdrawn-from-decimal-point
*> rules: [GR-7.3.10.4-4, GR-12.3.7.4-14]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 7.3.10.4 GR4 (kb/Work PB4573): "When the SUBSTITUTE option is specified, the COBOL word that is the
*> content of literal-5 shall be used in any syntax where the COBOL word that is the content of literal-4 is
*> documented as required or optional. The COBOL word that is the content of literal-4 shall no longer be a reserved
*> word, a context-sensitive word, nor an intrinsic function name within this compilation group."
*>   cite.py: OK  §2023:7.3.10.4 4)  (General rules)
*> COMMA, substituted by CMA, is no longer the word DECIMAL-POINT IS COMMA (12.3.7.2) requires: the clause is written
*> with CMA, and this program, which writes COMMA, is refused. The refusal said the clause "is not available", which GR4 makes false (it
*> is available, written with literal-5); it now names the directive and the word to write.
       >>COBOL-WORDS SUBSTITUTE "COMMA" BY "CMA"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4573NC.
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
