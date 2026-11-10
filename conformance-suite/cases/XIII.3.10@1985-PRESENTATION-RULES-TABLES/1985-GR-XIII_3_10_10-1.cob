      *>--- conformance-case
      *> id: 1985-GR-XIII_3_10_10-1
      *> rules: [1985-GR-XIII.3.10.10-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.10.10 GR1 (PRESENTATION RULES TABLES). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RFLIM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFILE ASSIGN TO RFOUT.
       DATA DIVISION.
       FILE SECTION.
       FD  RFILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-LC PIC 99.
       REPORT SECTION.
       RD  RPT
           PAGE LIMIT 20 LINES
           HEADING 1
           FIRST DETAIL 3
           LAST DETAIL 15
           FOOTING 16.
       01  TYPE PAGE FOOTING LINE 17.
           05  COLUMN 1 PIC X(4) VALUE "PF17".
       01  RFG TYPE REPORT FOOTING LINE PLUS 1.
           05  COLUMN 1 PIC X(2) VALUE "RF".
       01  DL TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(3) VALUE "DET".
       PROCEDURE DIVISION.
       DECLARATIVES.
       RF-USE SECTION.
           USE BEFORE REPORTING RFG.
       RF-USE-P.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "BEFORE RF " WS-LC.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT RFILE.
           INITIATE RPT.
           GENERATE DL.
           TERMINATE RPT.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "AFTER TERMINATE " WS-LC.
           CLOSE RFILE.
           STOP RUN.
