      *>--- conformance-case
      *> id: 1985-SR-XIII_3_16_3-1
      *> rules: [1985-SR-XIII.3.16.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.16.3 SR1 (THE NEXT GROUP CLAUSE). A refusal: the
      *> rule forbids line 16 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL2284 there; a compiler that accepts it
      *> fails. Placed from written and run by the 1985 audit's drafts
      *> pass (a draft that mismatched on an older build, re-run to
      *> MATCH with its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NGTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE LABEL RECORDS ARE OMITTED REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-N PIC 9(3) VALUE 5.
       REPORT SECTION.
       RD  RPT CONTROL IS FINAL.
       01  DET TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC 9(3) SOURCE WS-N.
       01  TYPE CONTROL FOOTING FINAL NEXT GROUP PLUS 1.
           05  TOT PIC 9(5) SUM WS-N.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DET.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
