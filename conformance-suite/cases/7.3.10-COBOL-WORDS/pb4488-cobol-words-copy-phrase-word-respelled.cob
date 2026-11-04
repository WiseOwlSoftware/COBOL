*>--- conformance-case
*> id: pb4488-cobol-words-copy-phrase-word-respelled
*> rules: [GR-7.3.10.4-6]
*> files: [PB4488CB.cpy]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 7.3.10.4 GR6 (kb/Work PB4488): "6) A COBOL-WORDS
*> directive does not affect any Compiler directing statements or
*> Compiler directives." The compiler directing statements are the
*> COPY statement and the REPLACE statement (7.2.2.2), found and
*> applied in Steps 1 to 3 of 7.2.1, before the actions of the
*> directive in Step 4.
*>   cite.py: OK  §7.3.10.4 6)  (General rules)
*>   cite.py: OK  §7.2.2.2   (Compiler directing statements)
*> EQUATE "REPLACING" WITH "RPLG" changes nothing the COPY statement
*> reads: the COPY statement (Step 1) is read before the directive
*> acts (Step 4), so RPLG is no REPLACING phrase of it, and the
*> statement is refused for a word its general format (7.2.3.2) does
*> not allow there.
       >>COBOL-WORDS EQUATE "REPLACING" WITH "RPLG"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4488G.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COPY PB4488CB RPLG ==X== BY ==DISPLAY==.
           STOP RUN.
