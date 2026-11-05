      *>--- conformance-case
      *> id: pb4677-reserved-words-85-2002
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4677 - ISO 8.9 reserves the facility's words at 85 and 2002, and the
      *> new lexer tokens keep the funnel's COBOL0901.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4677X4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ENABLE PIC X.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
