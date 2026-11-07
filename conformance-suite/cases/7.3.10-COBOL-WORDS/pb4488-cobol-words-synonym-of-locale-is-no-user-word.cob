*>--- conformance-case
*> id: pb4488-cobol-words-synonym-of-locale-is-no-user-word
*> rules: [GR-7.3.10.4-4, SR-8.3.2.1-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 7.3.10.4 GR4 (kb/Work PB4488): "When the SUBSTITUTE
*> option is specified, the COBOL word that is the content of
*> literal-5 shall be used in any syntax where the COBOL word that is
*> the content of literal-4 is documented as required or optional."
*>   cite.py: OK  §2023:7.3.10.4 4)  (General rules)
*> LOCALE is a reserved word (8.9) the lexer has no token for. GR4
*> also ends its reservation ("shall no longer be a reserved word"),
*> so 01 LOCALE is a legal user-defined word here and stays legal;
*> LOC is the reserved word's replacement, so 01 LOC is refused
*> (8.3.2.1 rule 1). The program is refused for LOC alone.
*>   cite.py: OK  §2023:8.3.2.1 1)  (General)
       >>COBOL-WORDS SUBSTITUTE "LOCALE" BY "LOC"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4488E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 LOCALE PIC X VALUE "D".
       01 LOC PIC X VALUE "C".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY LOC LOCALE.
           STOP RUN.
