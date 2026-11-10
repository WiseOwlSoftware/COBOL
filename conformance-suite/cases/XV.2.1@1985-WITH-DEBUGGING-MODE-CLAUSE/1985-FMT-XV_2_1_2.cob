      *>--- conformance-case
      *> id: 1985-FMT-XV_2_1_2
      *> rules: [1985-FMT-XV.2.1.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XV.2.1.2 (THE WITH DEBUGGING MODE CLAUSE). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DBGMODE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-X WITH DEBUGGING MODE.
       OBJECT-COMPUTER. COMPUTER-X.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-N PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
      D    ADD 1 TO WS-N.
           DISPLAY "N=" WS-N.
      D    DISPLAY "DEBUGGING LINE COMPILED".
           STOP RUN.
