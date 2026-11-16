      *>--- conformance-case
      *> id: pb5850-report-group-justified
      *> rules: [1985-SR-VI.5.6.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.6.3 SR1 (cite.py --check: OK): "The JUSTIFIED
      *> clause can be specified only at the elementary item level";
      *> §1985:XIII.3.9.4 GR2 (cite.py --check: OK) makes the Report
      *> Writer's JUSTIFIED clause the Nucleus clause, and the group
      *> formats of §1985:XIII.3.9.2 (formats 1 and 2) print no
      *> JUSTIFIED clause. LINE 1 JUSTIFIED RIGHT has a COLUMN entry
      *> below it, so it is a group entry and the program is refused
      *> with COBOL2247 (kb/Work PB5850); it compiled and ran.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5850A.
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
          05 LINE 1 JUSTIFIED RIGHT.
             10 COLUMN 1 PIC X(4) SOURCE W.
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT PRTF.
           INITIATE RPT. GENERATE DETL. TERMINATE RPT.
           CLOSE PRTF.
           DISPLAY "RAN".
           STOP RUN.
