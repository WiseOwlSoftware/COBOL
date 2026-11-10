      *>--- conformance-case
      *> id: 1985-GR-XV_3_2_4-1
      *> rules: [1985-GR-XV.3.2.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XV.3.2.4 GR1 (THE USE FOR DEBUGGING STATEMENT). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WRONG
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DBGGR1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANY-COMPUTER WITH DEBUGGING MODE.
       OBJECT-COMPUTER. ANY-COMPUTER.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "F1".
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X.
       WORKING-STORAGE SECTION.
       01  WATCHED PIC X VALUE SPACE.
       01  CNT     PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG-SECT SECTION.
           USE FOR DEBUGGING ON ERR-PARA MAIN-PARA WATCHED.
       DBG-PARA.
           ADD 1 TO CNT.
           DISPLAY "DEBUG " CNT " [" DEBUG-NAME "]".
           IF CNT = 1
               MOVE "X" TO WATCHED
               PERFORM ERR-PARA.
       ERR-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F1.
       ERR-PARA.
           DISPLAY "ERR-PARA".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
           DISPLAY "MAIN " WATCHED.
           MOVE "Y" TO WATCHED.
           STOP RUN.
