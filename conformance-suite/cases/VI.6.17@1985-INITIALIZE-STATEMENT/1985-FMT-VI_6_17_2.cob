      *>--- conformance-case
      *> id: 1985-FMT-VI_6_17_2
      *> rules: [1985-FMT-VI.6.17.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.17.2 (THE INITIALIZE STATEMENT). A refusal: the rule
      *> forbids line 10 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL5506 there; a compiler that accepts it fails.
      *> Placed from written and run by the triage of the drafts that
      *> did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INITT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-GRP.
           05  WS-N PIC 9(3) VALUE 123.
           05  WS-A PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       P1.
           INITIALIZE WS-GRP REPLACING NUMERIC DATA 5.
           DISPLAY WS-N.
           STOP RUN.
