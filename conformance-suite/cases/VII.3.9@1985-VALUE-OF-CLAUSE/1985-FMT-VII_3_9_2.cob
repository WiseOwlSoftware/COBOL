      *>--- conformance-case
      *> id: 1985-FMT-VII_3_9_2
      *> rules: [1985-FMT-VII.3.9.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.9.2 (THE VALUE OF CLAUSE). A refusal: the rule
      *> forbids line 11 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL5506 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 audit's drafts pass (a
      *> draft that mismatched on an older build, re-run to MATCH with
      *> its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. VALOF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "F1DATA".
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           LABEL RECORDS ARE STANDARD
           VALUE OF "F1DATA".
       01  R1 PIC X(10).
       PROCEDURE DIVISION.
       P1.
           DISPLAY "OK".
           STOP RUN.
