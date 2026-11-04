*>--- conformance-case
*> id: pb4488-cobol-words-replace-phrase-word-respelled
*> rules: [GR-7.3.10.4-6]
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
*> EQUATE "BY" WITH "BYX" changes nothing the REPLACE statement
*> reads either: BY is a word of the statement's own general format
*> (7.2.4.2), so BYX is no BY there, and the REPLACE statement lacks
*> the BY its format requires.
       >>COBOL-WORDS EQUATE "BY" WITH "BYX"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4488F.
       PROCEDURE DIVISION.
       MAIN-PARA.
           REPLACE ==QQ== BYX =="Z"==.
           DISPLAY QQ.
           STOP RUN.
