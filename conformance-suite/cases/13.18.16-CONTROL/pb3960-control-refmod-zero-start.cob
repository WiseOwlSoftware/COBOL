*>--- conformance-case
*> id: pb3960-control-refmod-zero-start
*> rules: [SR-13.18.16.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3960, review finding R1-1. ISO 13.18.16.3 SR4: "Data-name-1
      *> may be reference-modified. If it is, leftmost-position and length shall
      *> be integer literals." (cite.py --check OK.) ZERO is a figurative
      *> constant (8.8.1.1), not an integer literal, so CONTROL IS CX(ZERO:2) is
      *> refused by SR4 itself. Before the fix the lexical reader took the
      *> rewritten token's text "0" for the literal and the refusal was the
      *> range screen's COBOL2670 quoting "CX(0:2)", text the program never wrote.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3960N3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb3960n3.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 CX     PIC X(6) VALUE SPACES.
       01 WS-SRC PIC 99 VALUE 7.
       REPORT SECTION.
       RD R-1 CONTROL IS CX(ZERO:2).
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC 99 SOURCE IS WS-SRC.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT
           INITIATE R-1
           GENERATE DET-A
           TERMINATE R-1
           CLOSE RPT
           STOP RUN.
