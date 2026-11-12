      *>--- conformance-case
      *> id: 1985-SR-VII_4_7_3-1
      *> rules: [1985-SR-VII.4.7.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.7.3 SR1 (THE WRITE STATEMENT). A refusal: the rule
      *> forbids line 18 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6827 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 audit's drafts pass (a
      *> draft that mismatched on an older build, re-run to MATCH with
      *> its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. WRTSR1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OUT-FILE ASSIGN TO OUTFILE.
       DATA DIVISION.
       FILE SECTION.
       FD  OUT-FILE
           LABEL RECORDS ARE STANDARD.
       01  OUT-REC.
           05  OUT-FLD PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-REC PIC X(10) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT OUT-FILE.
           WRITE OUT-REC FROM OUT-FLD.
           CLOSE OUT-FILE.
           STOP RUN.
