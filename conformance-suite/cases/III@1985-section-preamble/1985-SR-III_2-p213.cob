      *>--- conformance-case
      *> id: 1985-SR-III_2-p213
      *> rules: [1985-SR-III.2-p213]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p213 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-04.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P213HDR.
       AUTHOR. A COMMENT ENTRY.
       INSTALLATION. A COMMENT ENTRY.
       DATE-WRITTEN. 21 NOV 2026.
       DATE-COMPILED. ANYTHING.
       SECURITY. NONE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANY-COMPUTER.
       OBJECT-COMPUTER. ANY-COMPUTER.
       SPECIAL-NAMES. DECIMAL-POINT IS COMMA.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "P213.DAT"
               ORGANIZATION IS SEQUENTIAL.
       I-O-CONTROL.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  R PIC X(4).
       WORKING-STORAGE SECTION.
       01  N PIC 9V9 VALUE 1,5.
       PROCEDURE DIVISION.
       P-MAIN.
           OPEN OUTPUT F.
           MOVE "ABCD" TO R.
           WRITE R.
           CLOSE F.
           IF N = 1,5
               DISPLAY "P213 OK"
           ELSE
               DISPLAY "P213 FAIL".
           STOP RUN.
