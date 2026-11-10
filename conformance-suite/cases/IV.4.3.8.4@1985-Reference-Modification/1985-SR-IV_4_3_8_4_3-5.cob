      *>--- conformance-case
      *> id: 1985-SR-IV_4_3_8_4_3-5
      *> rules: [1985-SR-IV.4.3.8.4.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.4.3 SR5 (Reference Modification). A refusal: the
      *> rule forbids line 13 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL1629 there; a compiler that accepts it
      *> fails. Placed from written and run by the witness-drafting pass
      *> (a rule the audit never drafted) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RM85SR5.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A                PIC X(4) VALUE "abcd".
       01  WS-B                PIC X(2) VALUE SPACES.
       01  WS-N                PIC 9(4) VALUE ZERO.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE FUNCTION UPPER-CASE (WS-A) (2:2) TO WS-B.
           DISPLAY WS-B.
           COMPUTE WS-N = FUNCTION ORD (WS-B (1:1)) (1:1).
           DISPLAY "DONE".
           STOP RUN.
