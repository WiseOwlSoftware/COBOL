      *>--- conformance-case
      *> id: 1985-SR-VI_6_18_3-9
      *> rules: [1985-SR-VI.6.18.3-9]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.18.3 SR9 (THE INSPECT STATEMENT). A refusal: the
      *> rule forbids line 11 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL0846 there; a compiler that accepts it
      *> fails. Placed from written and run by the confirmation pass
      *> (the redraft of a WEAK or WRONG record, whose filed defect was
      *> fixed; re-run to MATCH with its expectation unchanged,
      *> witnesses-85g) by scripts/spec/place_witnesses.py (kb/Work
      *> PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INSP9.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W      PIC X(6) VALUE "ABCABC".
       01  W-FROM PIC XX VALUE "AB".
       01  W-TO1  PIC X VALUE "X".
       01  W-TO2  PIC XX VALUE "XY".
       PROCEDURE DIVISION.
       P1.
           INSPECT W CONVERTING W-FROM TO W-TO1.
           DISPLAY W.
           STOP RUN.
