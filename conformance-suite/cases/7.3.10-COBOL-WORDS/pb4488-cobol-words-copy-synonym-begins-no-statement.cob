*>--- conformance-case
*> id: pb4488-cobol-words-copy-synonym-begins-no-statement
*> rules: [GR-7.3.10.4-6]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 7.3.10.4 GR6 (kb/Work PB4488): "6) A COBOL-WORDS
*> directive does not affect any Compiler directing statements or
*> Compiler directives." The compiler directing statements are the
*> COPY statement and the REPLACE statement (7.2.2.2), found in
*> Step 1 of 7.2.1, before the actions of the directive in Step 4.
*>   cite.py: OK  §7.3.10.4 6)  (General rules)
*>   cite.py: OK  §7.2.2.2   (Compiler directing statements)
*> EQUATE "COPY" WITH "CPY" is a legal directive (7.3.10.3 SR3 admits
*> any reserved word as literal-1), and it changes nothing the COPY
*> statement reads. So CPY BOOK. begins no COPY statement, and after
*> Step 4 CPY is a synonym for COPY, a word that no statement of the
*> compilation stage begins with: the program is refused. The report
*> says so beside the syntax error (COBOL6085, a warning) instead of
*> guessing that a period is missing from the sentence before.
       >>COBOL-WORDS EQUATE "COPY" WITH "CPY"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4488B.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "A".
           CPY BOOK.
           STOP RUN.
