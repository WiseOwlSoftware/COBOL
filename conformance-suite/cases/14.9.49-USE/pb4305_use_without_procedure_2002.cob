      *>--- conformance-case
      *> id: pb4305_use_without_procedure_2002
      *> rules: [FMT-14.9.49.2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4305 - 14.9.49.2 Format 1 prints [PROCEDURE] as
      *>   optional (2002 Annex F.2 item 141). The OPEN INPUT of a file
      *>   that does not exist fails with I-O status 35, and the
      *>   declarative runs. Expected: "ERR" then "DONE". It fails if
      *>   the USE statement is refused or its procedure is not run.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305UP.
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
           OPEN INPUT F
           DISPLAY "DONE"
           STOP RUN.
