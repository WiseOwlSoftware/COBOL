      *>--- conformance-case
      *> id: pb5850-report-element-blank-when-zero
      *> rules: [1985-SR-VI.5.4.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *> output-file: {name: pb5850b.txt, expected: pb5850-report-element-blank-when-zero.exp}
      *>---
      *> The control of pb5850-report-group-blank-when-zero: BLANK WHEN
      *> ZERO on the elementary entry COLUMN 1 PIC 9(3) whose source is
      *> zero, so the item contains "nothing but spaces"
      *> (§1985:VI.5.4.4 GR1, cite.py --check: OK) and prints as three
      *> spaces; the literal X at column 5 follows, so the report line
      *> is four spaces then X.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5850D.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb5850b.txt".
       DATA DIVISION.
       FILE SECTION.
       FD PRTF REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01 W PIC X(4) VALUE "ABCD".
       01 Z PIC 9(3) VALUE 0.
       REPORT SECTION.
       RD RPT PAGE LIMIT 20.
       01 DETL TYPE DETAIL.
          05 LINE 1.
             10 COLUMN 1 PIC 9(3) BLANK WHEN ZERO SOURCE Z.
             10 COLUMN 5 PIC X VALUE "X".
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT PRTF.
           INITIATE RPT. GENERATE DETL. TERMINATE RPT.
           CLOSE PRTF.
           DISPLAY "RAN".
           STOP RUN.
