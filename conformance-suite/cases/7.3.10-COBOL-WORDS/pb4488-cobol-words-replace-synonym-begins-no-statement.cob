*>--- conformance-case
*> id: pb4488-cobol-words-replace-synonym-begins-no-statement
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
*> EQUATE "REPLACE" WITH "RPL" changes nothing the REPLACE statement
*> reads: RPL OFF. is no REPLACE statement, and after Step 4 RPL is a
*> synonym for a word that begins no statement of the compilation
*> stage, so the program is refused (and COBOL6085 names the
*> directive and the rule beside the syntax error).
       >>COBOL-WORDS EQUATE "REPLACE" WITH "RPL"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4488C.
       PROCEDURE DIVISION.
       MAIN-PARA.
           REPLACE ==QQ== BY =="Z"==.
           DISPLAY QQ.
           RPL OFF.
           STOP RUN.
