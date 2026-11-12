      *>--- conformance-case
      *> id: 1985-SR-XIII_3_9_3-6
      *> rules: [1985-SR-XIII.3.9.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.9.3 SR6 (THE REPORT GROUP DESCRIPTION ENTRY). A
      *> refusal: the rule forbids line 14 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL2247 there; a compiler that
      *> accepts it fails. Placed from written and run by the 1985 draft
      *> triage (witnesses-85k): a draft that still mismatched in
      *> rerun-85g, corrected from the rule's text and run to MATCH by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWSR6.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT.
       01  DTL TYPE IS DETAIL.
           05.
               10  LINE PLUS 1 COLUMN 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DTL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
