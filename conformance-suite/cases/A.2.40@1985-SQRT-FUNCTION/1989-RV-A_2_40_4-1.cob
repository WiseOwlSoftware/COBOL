      *>--- conformance-case
      *> id: 1989-RV-A_2_40_4-1
      *> rules: [1989-RV-A.2.40.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.40.4 RV1 (THE SQRT FUNCTION). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witness-drafting pass (a rule the
      *> audit never drafted) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SQRT85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-ARG                  PIC 9(4)V9(4) VALUE 2.25.
       01  WS-SIDE                 PIC 9(2)      VALUE 12.
       01  WS-ROOT                 PIC S9(4)V9(4).
       01  WS-SHOW                 PIC +9(4).9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE WS-ROOT ROUNDED = FUNCTION SQRT (0).
           PERFORM SHOW-PARA.
           COMPUTE WS-ROOT ROUNDED = FUNCTION SQRT (1).
           PERFORM SHOW-PARA.
           COMPUTE WS-ROOT ROUNDED = FUNCTION SQRT (16).
           PERFORM SHOW-PARA.
           COMPUTE WS-ROOT ROUNDED = FUNCTION SQRT (WS-ARG).
           PERFORM SHOW-PARA.
           COMPUTE WS-ROOT ROUNDED = FUNCTION SQRT (0.0625).
           PERFORM SHOW-PARA.
           COMPUTE WS-ROOT ROUNDED = FUNCTION SQRT (2).
           PERFORM SHOW-PARA.
           COMPUTE WS-ROOT ROUNDED =
               FUNCTION SQRT (WS-SIDE * WS-SIDE).
           PERFORM SHOW-PARA.
           IF FUNCTION SQRT (9) < 0
               DISPLAY "SQRT 9 IS NEGATIVE"
           ELSE
               DISPLAY "SQRT 9 IS NOT NEGATIVE".
           STOP RUN.
       SHOW-PARA.
           MOVE WS-ROOT TO WS-SHOW.
           DISPLAY WS-SHOW.
