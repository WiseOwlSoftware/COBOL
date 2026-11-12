      *>--- conformance-case
      *> id: 1985-SR-XIII_3_8_3-9
      *> rules: [1985-SR-XIII.3.8.3-9]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.8.3 SR9 (THE PAGE CLAUSE). A refusal: the rule
      *> forbids line 35 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2199 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 draft triage
      *> (witnesses-85k): a draft that still mismatched in rerun-85g,
      *> corrected from the rule's text and run to MATCH by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWPAGE.
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
           PAGE LIMIT IS 10 LINES.
       01  DET TYPE IS DETAIL.
           02 LINE PLUS 1.
              03 COLUMN 1 PIC X(3) VALUE "L01".
           02 LINE PLUS 1.
              03 COLUMN 1 PIC X(3) VALUE "L02".
           02 LINE PLUS 1.
              03 COLUMN 1 PIC X(3) VALUE "L03".
           02 LINE PLUS 1.
              03 COLUMN 1 PIC X(3) VALUE "L04".
           02 LINE PLUS 1.
              03 COLUMN 1 PIC X(3) VALUE "L05".
           02 LINE PLUS 1.
              03 COLUMN 1 PIC X(3) VALUE "L06".
           02 LINE PLUS 1.
              03 COLUMN 1 PIC X(3) VALUE "L07".
           02 LINE PLUS 1.
              03 COLUMN 1 PIC X(3) VALUE "L08".
           02 LINE PLUS 1.
              03 COLUMN 1 PIC X(3) VALUE "L09".
           02 LINE PLUS 1.
              03 COLUMN 1 PIC X(3) VALUE "L10".
           02 LINE PLUS 1.
              03 COLUMN 1 PIC X(3) VALUE "L11".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DET.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
