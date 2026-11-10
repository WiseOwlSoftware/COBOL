      *>--- conformance-case
      *> id: 1985-SR-IV_4_3_8_3_3-2
      *> rules: [1985-SR-IV.4.3.8.3.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.3.3 SR2 (Function-Identifier). A refusal: the
      *> rule forbids line 12 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL1629 there; a compiler that accepts it
      *> fails. Placed from written and run by the witness-drafting pass
      *> (a rule the audit never drafted) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FNREFM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A1 PIC X(5) VALUE "hello".
       01  N1 PIC 9(2) VALUE 17.
       01  N2 PIC 9(2) VALUE 5.
       01  W1 PIC 9(2) VALUE 0.
       PROCEDURE DIVISION.
       MAIN-1.
           DISPLAY FUNCTION UPPER-CASE (A1) (2:3).
           COMPUTE W1 = FUNCTION MOD (N1 N2) (1:1).
           DISPLAY W1.
           STOP RUN.
