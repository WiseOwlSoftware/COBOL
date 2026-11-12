      *>--- conformance-case
      *> id: 1985-SR-XIII_3_15_3-9
      *> rules: [1985-SR-XIII.3.15.3-9]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.15.3 SR9 (THE LINE NUMBER CLAUSE). A refusal: the
      *> rule forbids line 21 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL2199 there; a compiler that accepts it
      *> fails. Placed from written and run by the 1985 audit's drafts
      *> pass (a draft that mismatched on an older build, re-run to
      *> MATCH with its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWPF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT
           PAGE LIMIT IS 20 LINES
           HEADING 1
           FIRST DETAIL 2
           LAST DETAIL 15
           FOOTING 17.
       01  DET-LINE TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC X(4) VALUE "DATA".
       01  TYPE IS PAGE FOOTING.
           05  LINE NUMBER IS PLUS 1 COLUMN 1 PIC X(4) VALUE "FOOT".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DET-LINE.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
