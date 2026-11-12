      *>--- conformance-case
      *> id: 1985-SR-XIII_3_6_3-1
      *> rules: [1985-SR-XIII.3.6.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.6.3 SR1 (THE CODE CLAUSE). A refusal: the rule
      *> forbids line 12 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2713 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 draft triage
      *> (witnesses-85k): a draft that still mismatched in rerun-85g,
      *> corrected from the rule's text and run to MATCH by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CODETST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PRTFILE.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT-1.
       REPORT SECTION.
       RD  RPT-1 CODE "A".
       01  TYPE IS DETAIL.
           05  LINE PLUS 1.
               10  COLUMN 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
