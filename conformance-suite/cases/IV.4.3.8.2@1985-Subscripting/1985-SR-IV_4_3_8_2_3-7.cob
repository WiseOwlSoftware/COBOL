      *>--- conformance-case
      *> id: 1985-SR-IV_4_3_8_2_3-7
      *> rules: [1985-SR-IV.4.3.8.2.3-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.2.3 SR7 (Subscripting). A refusal: the rule
      *> forbids line 12 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2363 there; a compiler that accepts it fails.
      *> Placed from written and run by the witness-drafting pass (a
      *> rule the audit never drafted) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBALL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T1.
           02  E1 OCCURS 3 TIMES PIC 9 VALUE 2.
       01  W1 PIC 9(2) VALUE 0.
       PROCEDURE DIVISION.
       MAIN-1.
           COMPUTE W1 = FUNCTION SUM (E1 (ALL)).
           DISPLAY W1.
           MOVE E1 (ALL) TO W1.
           DISPLAY W1.
           STOP RUN.
