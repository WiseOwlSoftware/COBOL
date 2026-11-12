      *>--- conformance-case
      *> id: 1985-SR-VI_6_20_3-3
      *> rules: [1985-SR-VI.6.20.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.20.3 SR3 (THE MULTIPLY STATEMENT). A refusal: the
      *> rule forbids line 13 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL0805 there; a compiler that accepts it
      *> fails. Placed from written and run by the 1985 audit's drafts
      *> pass (a draft that mismatched on an older build, re-run to
      *> MATCH with its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MULT18.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC 9 VALUE 2.
       01  B PIC 9 VALUE 3.
       01  C PIC 9(10).
       01  D PIC V9(9).
       01  E PIC 9(9).
       PROCEDURE DIVISION.
       P1.
           MULTIPLY A BY B GIVING C D.
           STOP RUN.
