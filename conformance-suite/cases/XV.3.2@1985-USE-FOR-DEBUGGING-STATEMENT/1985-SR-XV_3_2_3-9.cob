      *>--- conformance-case
      *> id: 1985-SR-XV_3_2_3-9
      *> rules: [1985-SR-XV.3.2.3-9]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XV.3.2.3 SR9 (THE USE FOR DEBUGGING STATEMENT). A
      *> refusal: the rule forbids line 22 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL5092 there; a compiler that
      *> accepts it fails. Placed from written and run by the triage of
      *> the drafts that did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DBGRPT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X WITH DEBUGGING MODE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PRTOUT.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-AMT PIC 9(3) VALUE 5.
       REPORT SECTION.
       RD  RPT.
       01  DET-LINE TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  DET-AMT COLUMN 1 PIC 9(3) SOURCE WS-AMT.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG SECTION.
           USE FOR DEBUGGING ON DET-AMT.
       DBG-PARA.
           DISPLAY DEBUG-NAME.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DET-LINE.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
