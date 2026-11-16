      *>--- conformance-case
      *> id: pb5850-report-group-blank-when-zero
      *> rules: [1985-SR-VI.5.4.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.4.3 SR1 (cite.py --check: OK): the BLANK WHEN ZERO
      *> clause "can be specified only for an elementary item";
      *> §1985:XIII.3.9.4 GR2 (cite.py --check: OK) makes the Report
      *> Writer's BLANK WHEN ZERO clause the Nucleus clause, and the
      *> group formats of §1985:XIII.3.9.2 print none. LINE 1 BLANK
      *> WHEN ZERO has a COLUMN entry below it, so it is a group entry
      *> and the program is refused with COBOL2247 (kb/Work PB5850); it
      *> compiled and ran.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5850C.
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
          05 LINE 1 BLANK WHEN ZERO.
             10 COLUMN 1 PIC 9(3) SOURCE Z.
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT PRTF.
           INITIATE RPT. GENERATE DETL. TERMINATE RPT.
           CLOSE PRTF.
           DISPLAY "RAN".
           STOP RUN.
