*>--- conformance-case
*> id: pb4488-cobol-words-synonym-of-copy-is-no-user-word
*> rules: [GR-7.3.10.4-2, SR-8.3.2.1-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 7.3.10.4 GR2 (kb/Work PB4488): "When the EQUATE
*> option is specified, the COBOL word that is the content of
*> literal-2 shall be treated as a synonym for the COBOL word that is
*> the content of literal-1".
*>   cite.py: OK  §7.3.10.4 2)  (General rules)
*> COPY is a reserved word (8.9), so CPY is a synonym for a reserved
*> word, and "Reserved words shall not be used as user-defined words
*> or system-names."
*>   cite.py: OK  §8.3.2.1 1)  (General)
*> The lexer has no token for COPY (the preprocessor reads it), so CPY
*> stayed a plain word and 01 CPY compiled.
       >>COBOL-WORDS EQUATE "COPY" WITH "CPY"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4488D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CPY PIC X VALUE "C".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY CPY.
           STOP RUN.
