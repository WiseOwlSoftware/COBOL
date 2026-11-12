      *>--- conformance-case
      *> id: 1985-SR-VI_6_23_3-2
      *> rules: [1985-SR-VI.6.23.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.23.3 SR2 (THE SET STATEMENT). A refusal: the rule
      *> forbids line 11 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0900 there; a compiler that accepts it fails.
      *> Placed from written and run by the confirmation pass (the
      *> redraft of a WEAK or WRONG record, whose filed defect was
      *> fixed; re-run to MATCH with its expectation unchanged,
      *> witnesses-85g) by scripts/spec/place_witnesses.py (kb/Work
      *> PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SETTS2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TBL.
           05 ELT PIC X OCCURS 5 TIMES INDEXED BY IX.
       01 NUM PIC 99 VALUE 3.
       01 FRC PIC 9V9 VALUE 3.
       PROCEDURE DIVISION.
       P1.
           SET IX TO FRC.
           SET NUM TO IX.
           DISPLAY NUM.
           STOP RUN.
