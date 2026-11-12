      *>--- conformance-case
      *> id: 1985-SR-XIII_3_9_3-4
      *> rules: [1985-SR-XIII.3.9.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.9.3 SR4 (THE REPORT GROUP DESCRIPTION ENTRY). A
      *> refusal: the rule forbids line 17 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL2247 there; a compiler that
      *> accepts it fails. Placed from written and run by the 1985
      *> audit's drafts pass (a draft that mismatched on an older build,
      *> re-run to MATCH with its expectation unchanged, witnesses-85g)
      *> by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  PRT-FILE
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT
           PAGE LIMIT IS 60 LINES.
       01  DETAIL-LINE TYPE IS DETAIL.
           02  LINE 1.
               03  COLUMN 1 PIC X(5) VALUE "HELLO".
           02  LINE 2.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT-FILE.
           INITIATE RPT.
           GENERATE DETAIL-LINE.
           TERMINATE RPT.
           CLOSE PRT-FILE.
           STOP RUN.
