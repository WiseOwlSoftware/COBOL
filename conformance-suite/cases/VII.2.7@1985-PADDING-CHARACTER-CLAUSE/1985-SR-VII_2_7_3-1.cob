      *>--- conformance-case
      *> id: 1985-SR-VII_2_7_3-1
      *> rules: [1985-SR-VII.2.7.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.7.3 SR1 (THE PADDING CHARACTER CLAUSE). A refusal:
      *> the rule forbids line 8 of the program below, and WiseOwl
      *> refuses it at 85 with COBOL6848 there; a compiler that accepts
      *> it fails. Placed from written and run by the 1985 audit's
      *> drafts pass (a draft that mismatched on an older build, re-run
      *> to MATCH with its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PADTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OUT-FILE ASSIGN TO "OUTFILE"
               ORGANIZATION IS SEQUENTIAL
               PADDING CHARACTER IS "AB".
       DATA DIVISION.
       FILE SECTION.
       FD  OUT-FILE
           LABEL RECORDS ARE STANDARD.
       01  OUT-REC PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
