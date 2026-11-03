      *>--- conformance-case
      *> id: pb4305-use-without-procedure-at-85
      *> rules: [FMT-14.9.49.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4305 - the edition half of
      *>   pb4305_use_without_procedure_2002. COBOL-85 required the
      *>   word PROCEDURE in the USE AFTER STANDARD EXCEPTION/ERROR
      *>   statement (2002 Annex F.2 item 141). Expected: COBOL0900 at
      *>   85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305NUP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "PB4305NOFILE".
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X(10).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER STANDARD ERROR ON F.
       D1-P.
           DISPLAY "ERR".
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           STOP RUN.
