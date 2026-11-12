      *>--- conformance-case
      *> id: 1985-SR-VI_6_11_3-3
      *> rules: [1985-SR-VI.6.11.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.11.3 SR3 (THE DIVIDE STATEMENT). A refusal: the rule
      *> forbids line 10 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0805 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 audit's drafts pass (a
      *> draft that mismatched on an older build, re-run to MATCH with
      *> its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DIVCMP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC 9 VALUE 2.
       01  B PIC 9(18) VALUE 10.
       01  C PIC V99 VALUE .50.
       PROCEDURE DIVISION.
       P1.
           DIVIDE A INTO B C.
           STOP RUN.
