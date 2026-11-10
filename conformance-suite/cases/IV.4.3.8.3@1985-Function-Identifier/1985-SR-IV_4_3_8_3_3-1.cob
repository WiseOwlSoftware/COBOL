      *>--- conformance-case
      *> id: 1985-SR-IV_4_3_8_3_3-1
      *> rules: [1985-SR-IV.4.3.8.3.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.3.3 SR1 (Function-Identifier). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FNARG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A   PIC 9(2) VALUE 7.
       01  WS-TXT PIC X(4) VALUE "abcd".
       01  WS-N   PIC 9(3) VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY FUNCTION UPPER-CASE (WS-TXT).
           DISPLAY FUNCTION UPPER-CASE ("xyz").
           COMPUTE WS-N = FUNCTION MAX (WS-A * 2 + 1, 9).
           DISPLAY WS-N.
           COMPUTE WS-N = FUNCTION MAX (WS-A, 3).
           DISPLAY WS-N.
           STOP RUN.
