      *>--- conformance-case
      *> id: 1985-FMT-IV_4_3_8_4_2
      *> rules: [1985-FMT-IV.4.3.8.4.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.4.2 (Reference Modification). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witness-drafting pass (a rule the
      *> audit never drafted) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. REFMOD1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-ALPHA            PIC X(10) VALUE "ABCDEFGHIJ".
       01  WS-LOWER            PIC X(6)  VALUE "pqrstu".
       01  WS-POS              PIC 9     VALUE 3.
       01  WS-LEN              PIC 9     VALUE 2.
       01  WS-OUT              PIC X(4)  VALUE SPACES.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY WS-ALPHA (2:3).
           DISPLAY WS-ALPHA (8:).
           DISPLAY WS-ALPHA (WS-POS + 1:WS-LEN * 2).
           DISPLAY FUNCTION UPPER-CASE (WS-LOWER) (2:3).
           DISPLAY FUNCTION UPPER-CASE (WS-LOWER) (5:).
           DISPLAY FUNCTION REVERSE (WS-ALPHA) (WS-POS:WS-LEN).
           MOVE FUNCTION LOWER-CASE (WS-ALPHA) (WS-LEN:WS-POS)
               TO WS-OUT.
           DISPLAY WS-OUT "|".
           STOP RUN.
