      *>--- conformance-case
      *> id: 1985-SR-VI_6_23_3-4
      *> rules: [1985-SR-VI.6.23.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.23.3 SR4 (THE SET STATEMENT). A refusal: the rule
      *> forbids line 9 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0900 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 audit's drafts pass (a
      *> draft that mismatched on an older build, re-run to MATCH with
      *> its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SETNEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL.
           05  ELT PIC X OCCURS 3 TIMES INDEXED BY IX.
       PROCEDURE DIVISION.
       P1.
           SET IX TO -1.
           STOP RUN.
