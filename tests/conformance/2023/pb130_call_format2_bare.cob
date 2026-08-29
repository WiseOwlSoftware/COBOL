      *> kb/Work PB130 — CALL Format 2's keyword-less and BY-less argument forms, pinned from the legal
      *> side. ISO 14.9.4.2 prints BY un-underlined before VALUE (5.2.3: optional word) — `USING VALUE N`
      *> parses; Format 2's BY phrases are plain brackets so literal-2 and arithmetic-expression-1 are legal
      *> keyword-less arguments — `USING 42` and `USING (N + 1)` bind BY CONTENT semantics (GR9 a)2). The
      *> parenthesized expression is this implementation's documented determination: whitespace is
      *> lexer-skipped, so `N + 1` is ambiguous between one expression and the two arguments N and +1 (both
      *> legal lists) — the list reading wins and parens force the expression. The negatives pb130-* pin
      *> Format 1's rejections of the same spellings.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB130F2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(4) VALUE 7.
       PROCEDURE DIVISION.
       MAIN.
           CALL "SUB1" AS NESTED USING VALUE N
           CALL "SUB2" AS NESTED USING 42
           CALL "SUB2" AS NESTED USING (N + 1)
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUB1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LA PIC 9(4).
       PROCEDURE DIVISION USING VALUE LA.
       M1.
           DISPLAY "S1 " LA
           GOBACK.
       END PROGRAM SUB1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUB2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LB PIC 9(4).
       PROCEDURE DIVISION USING LB.
       M2.
           DISPLAY "S2 " LB
           GOBACK.
       END PROGRAM SUB2.
       END PROGRAM PB130F2.
