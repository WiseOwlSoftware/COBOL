      *>--- conformance-case
      *> id: 1985-SR-VI_6_23_3-3
      *> rules: [1985-SR-VI.6.23.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.23.3 SR3 (THE SET STATEMENT). A refusal: the rule
      *> forbids line 13 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0900 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 audit's drafts pass (a
      *> draft that mismatched on an older build, re-run to MATCH with
      *> its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SETTST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL.
           05  E PIC X OCCURS 5 TIMES INDEXED BY IX.
       01  N PIC 9V9 VALUE 1.0.
       01  M PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       P1.
           SET IX TO 1.
           SET IX UP BY N.
           STOP RUN.
