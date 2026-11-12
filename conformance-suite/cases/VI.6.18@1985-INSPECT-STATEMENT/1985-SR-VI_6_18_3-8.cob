      *>--- conformance-case
      *> id: 1985-SR-VI_6_18_3-8
      *> rules: [1985-SR-VI.6.18.3-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.18.3 SR8 (THE INSPECT STATEMENT). A refusal: the
      *> rule forbids line 10 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL0846 there; a compiler that accepts it
      *> fails. Placed from written and run by the confirmation pass
      *> (the redraft of a WEAK or WRONG record, whose filed defect was
      *> fixed; re-run to MATCH with its expectation unchanged,
      *> witnesses-85g) by scripts/spec/place_witnesses.py (kb/Work
      *> PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INSP8.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A  PIC X(4) VALUE "ABCD".
       01  WS-R1 PIC X VALUE "X".
       01  WS-R2 PIC XX VALUE "XY".
       PROCEDURE DIVISION.
       P1.
           INSPECT WS-A REPLACING CHARACTERS BY WS-R2.
           DISPLAY WS-A.
           STOP RUN.
