      *>--- conformance-case
      *> id: 1985-SR-IV_4_3_8_4_3-1
      *> rules: [1985-SR-IV.4.3.8.4.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.4.3 SR1 (Reference Modification). A refusal: the
      *> rule forbids line 12 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL1647 there; a compiler that accepts it
      *> fails. Placed from written and run by the witness-drafting pass
      *> (a rule the audit never drafted) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. REFUSG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  D1 PIC 9(4) USAGE DISPLAY VALUE 1234.
       01  B1 PIC 9(4) USAGE BINARY VALUE 1234.
       01  X1 PIC X(2) VALUE SPACE.
       PROCEDURE DIVISION.
       MAIN-1.
           MOVE D1 (3:2) TO X1.
           DISPLAY X1.
           MOVE B1 (1:2) TO X1.
           DISPLAY X1.
           STOP RUN.
