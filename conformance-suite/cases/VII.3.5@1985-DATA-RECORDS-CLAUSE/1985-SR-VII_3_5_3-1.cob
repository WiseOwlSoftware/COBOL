      *>--- conformance-case
      *> id: 1985-SR-VII_3_5_3-1
      *> rules: [1985-SR-VII.3.5.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.5.3 SR1 (THE DATA RECORDS CLAUSE). A refusal: the
      *> rule forbids line 11 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL6849 there; a compiler that accepts it
      *> fails. Placed from written and run by the 1985 audit's drafts
      *> pass (a draft that mismatched on an older build, re-run to
      *> MATCH with its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DRTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OUT-FILE ASSIGN TO OUTFILE.
       DATA DIVISION.
       FILE SECTION.
       FD  OUT-FILE
           LABEL RECORDS ARE STANDARD
           DATA RECORD IS OUT-FLD.
       01  OUT-REC.
           02  OUT-FLD PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
