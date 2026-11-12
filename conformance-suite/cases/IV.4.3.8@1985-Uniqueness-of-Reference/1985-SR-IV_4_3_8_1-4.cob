      *>--- conformance-case
      *> id: 1985-SR-IV_4_3_8_1-4
      *> rules: [1985-SR-IV.4.3.8.1-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.1 SR4 (Uniqueness of Reference). A refusal: the
      *> rule forbids line 7 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL6862 there; a compiler that accepts it
      *> fails. Placed from written and run by the 1985 audit's drafts
      *> pass (a draft that mismatched on an older build, re-run to
      *> MATCH with its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UNIQREF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  DUP-NAME PIC X VALUE "A" GLOBAL.
       01  GRP.
           02  DUP-NAME PIC X VALUE "B".
       PROCEDURE DIVISION.
       P1.
           DISPLAY GRP.
           STOP RUN.
