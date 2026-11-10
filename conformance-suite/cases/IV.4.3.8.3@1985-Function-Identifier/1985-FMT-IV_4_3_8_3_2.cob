      *>--- conformance-case
      *> id: 1985-FMT-IV_4_3_8_3_2
      *> rules: [1985-FMT-IV.4.3.8.3.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.3.2 (Function-Identifier). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FUNCID.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A PIC X(6) VALUE "abcdef".
       01  WS-N PIC 9(3).
       01  WS-Y PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY FUNCTION UPPER-CASE (WS-A).
           DISPLAY FUNCTION UPPER-CASE (WS-A) (2:3).
           COMPUTE WS-N = FUNCTION MAX (3 7 5).
           DISPLAY WS-N.
           MOVE FUNCTION CURRENT-DATE (1:4) TO WS-Y.
           IF WS-Y IS NUMERIC
               DISPLAY "YEAR NUMERIC"
           ELSE
               DISPLAY "YEAR NOT NUMERIC".
           STOP RUN.
