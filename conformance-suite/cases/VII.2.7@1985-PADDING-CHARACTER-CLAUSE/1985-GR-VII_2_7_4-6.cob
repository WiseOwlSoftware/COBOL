      *>--- conformance-case
      *> id: 1985-GR-VII_2_7_4-6
      *> rules: [1985-GR-VII.2.7.4-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.7.4 GR6 (THE PADDING CHARACTER CLAUSE). A refusal:
      *> the rule forbids line 8 of the program below, and WiseOwl
      *> refuses it at 85 with COBOL1624 there; a compiler that accepts
      *> it fails. Placed from written and run by the 1985 draft triage
      *> (witnesses-85k): a draft that still mismatched in rerun-85g,
      *> corrected from the rule's text and run to MATCH by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PADTST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OUT-FILE ASSIGN TO "PADFILE"
               ORGANIZATION IS SEQUENTIAL
               PADDING CHARACTER IS WS-PAD.
       DATA DIVISION.
       FILE SECTION.
       FD  OUT-FILE IS EXTERNAL.
       01  OUT-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-PAD PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "DONE".
           STOP RUN.
