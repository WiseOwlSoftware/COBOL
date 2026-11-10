      *>--- conformance-case
      *> id: 1985-GR-XIII_3_20_4-11
      *> rules: [1985-GR-XIII.3.20.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.20.4 GR11 (THE TYPE CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWGR11.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTFILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  PRTFILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-N PIC 9(3) VALUE 0.
       01  WS-TOT PIC 9(4) VALUE 0.
       REPORT SECTION.
       RD  RPT
           CONTROL IS FINAL.
       01  DL TYPE IS DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC 9(3) SOURCE WS-N.
       01  TYPE IS CONTROL FOOTING FINAL LINE PLUS 1.
           05  TOT COLUMN 1 PIC 9(4) SUM WS-N.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT PRTFILE.
           INITIATE RPT.
           MOVE 5 TO WS-N.
           GENERATE RPT.
           MOVE 7 TO WS-N.
           GENERATE RPT.
           MOVE TOT TO WS-TOT.
           DISPLAY WS-TOT.
           TERMINATE RPT.
           CLOSE PRTFILE.
           STOP RUN.
