      *>--- conformance-case
      *> id: 1985-SR-XIII_3_9_3-8
      *> rules: [1985-SR-XIII.3.9.3-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.9.3 SR8 (THE REPORT GROUP DESCRIPTION ENTRY). A
      *> refusal: the rule forbids line 18 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL2247 there; a compiler that
      *> accepts it fails. Placed from written and run by the 1985
      *> audit's drafts pass (a draft that mismatched on an older build,
      *> re-run to MATCH with its expectation unchanged, witnesses-85g)
      *> by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWUSAGE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  PRT LABEL RECORDS ARE OMITTED REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-AMT PIC 9(3) VALUE 5.
       01  WS-KEY PIC X VALUE "A".
       REPORT SECTION.
       RD  RPT CONTROLS ARE WS-KEY PAGE LIMIT IS 60 LINES.
       01  DTL TYPE DETAIL LINE PLUS 1.
           05 COLUMN 1 PIC 9(3) SOURCE WS-AMT.
       01  TYPE CONTROL FOOTING WS-KEY LINE PLUS 1.
           05 TOT PIC 9(5) SUM WS-AMT USAGE DISPLAY.
           05 COLUMN 1 PIC 9(5) SOURCE TOT.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE RPT.
           GENERATE DTL.
           TERMINATE RPT.
           CLOSE PRT.
           STOP RUN.
