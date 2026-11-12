      *>--- conformance-case
      *> id: 1985-SR-VI_6_18_3-7
      *> rules: [1985-SR-VI.6.18.3-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.18.3 SR7 (THE INSPECT STATEMENT). A refusal: the
      *> rule forbids line 11 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL0846 there; a compiler that accepts it
      *> fails. Placed from written and run by the confirmation pass
      *> (the redraft of a WEAK or WRONG record, whose filed defect was
      *> fixed; re-run to MATCH with its expectation unchanged,
      *> witnesses-85g) by scripts/spec/place_witnesses.py (kb/Work
      *> PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INSP7.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-X  PIC X(6) VALUE "ABCDAB".
       01  WS-F  PIC XX VALUE "AB".
       01  WS-T1 PIC X VALUE "0".
       01  WS-T2 PIC XX VALUE "00".
       PROCEDURE DIVISION.
       P1.
           INSPECT WS-X REPLACING ALL WS-F BY WS-T1.
           INSPECT WS-X REPLACING ALL "CD" BY SPACE.
           DISPLAY WS-X "|".
           STOP RUN.
