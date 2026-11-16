      *>--- conformance-case
      *> id: pb5850-report-element-justified
      *> rules: [1985-SR-VI.5.6.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *> output-file: {name: pb5850j.txt, expected: pb5850-report-element-justified.exp}
      *>---
      *> The control of pb5850-report-group-justified: JUSTIFIED RIGHT
      *> on the elementary entry COLUMN 1 PIC X(6). When the receiving
      *> item is larger than the sending item, "the data is aligned at
      *> the rightmost character position in the data item with space
      *> fill for the leftmost character positions" (§1985:VI.5.6.4
      *> GR1, cite.py --check: OK), so the report line is two spaces
      *> then ABCD.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5850B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb5850j.txt".
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
             10 COLUMN 1 PIC X(6) JUSTIFIED RIGHT SOURCE W.
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT PRTF.
           INITIATE RPT. GENERATE DETL. TERMINATE RPT.
           CLOSE PRTF.
           DISPLAY "RAN".
           STOP RUN.
