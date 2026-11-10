      *>--- conformance-case
      *> id: 1985-GR-XV_3_2_4-24
      *> rules: [1985-GR-XV.3.2.4-24]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XV.3.2.4 GR24 (THE USE FOR DEBUGGING STATEMENT). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DBG24.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANY-COMPUTER WITH DEBUGGING MODE.
       OBJECT-COMPUTER. ANY-COMPUTER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  DBGX PIC XX VALUE SPACES.
       01  OTHER-ITEM PIC XX VALUE "ZZ".
       01  LINE-1 PIC X(6) VALUE SPACES.
       01  LINE-2 PIC X(6) VALUE SPACES.
       01  LINE-3 PIC X(6) VALUE SPACES.
       01  CNT    PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SEC SECTION.
           USE FOR DEBUGGING ON DBGX.
       D-PAR.
           ADD 1 TO CNT.
           IF CNT = 1 MOVE DEBUG-LINE TO LINE-1.
           IF CNT = 2 MOVE DEBUG-LINE TO LINE-2.
           IF CNT = 3 MOVE DEBUG-LINE TO LINE-3.
           DISPLAY "[" DEBUG-NAME "] [" DEBUG-CONTENTS (1:4) "]".
       END DECLARATIVES.
       MAIN-SEC SECTION.
       M-PAR.
           PERFORM P-PAR 2 TIMES.
           MOVE "CD" TO DBGX.
           MOVE DBGX TO OTHER-ITEM.
           DISPLAY "DONE " OTHER-ITEM.
           IF LINE-1 = SPACES
               DISPLAY "DEBUG-LINE IS SPACES".
           IF LINE-1 = LINE-2
               DISPLAY "SAME STATEMENT, SAME DEBUG-LINE"
           ELSE
               DISPLAY "SAME STATEMENT, ANOTHER DEBUG-LINE".
           IF LINE-1 = LINE-3
               DISPLAY "ANOTHER STATEMENT, SAME DEBUG-LINE"
           ELSE
               DISPLAY "ANOTHER STATEMENT, ANOTHER DEBUG-LINE".
           STOP RUN.
       P-PAR.
           MOVE "AB" TO DBGX.
