      *>--- conformance-case
      *> id: 1985-GR-XV_3_2_4-13
      *> rules: [1985-GR-XV.3.2.4-13]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XV.3.2.4 GR13 (THE USE FOR DEBUGGING STATEMENT). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DBGNAME.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANY-COMPUTER WITH DEBUGGING MODE.
       OBJECT-COMPUTER. ANY-COMPUTER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  GROUP-QUALIFIER.
           02  ELEMENT-NAME-TWENTYX PIC X.
       01  OTHER-GROUP.
           02  ELEMENT-NAME-TWENTYX PIC X.
       01  TBL.
           02  ELEM PIC X OCCURS 5 TIMES.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG-A SECTION.
           USE FOR DEBUGGING ON PARA-A ELEM.
       DBG-A-P.
           DISPLAY "[" DEBUG-NAME "]".
       DBG-B SECTION.
           USE FOR DEBUGGING
               ON ELEMENT-NAME-TWENTYX OF GROUP-QUALIFIER.
       DBG-B-P.
           IF DEBUG-NAME = "ELEMENT-NAME-TWENTYX OF GROUP-"
               OR DEBUG-NAME = "ELEMENT-NAME-TWENTYX IN GROUP-"
               DISPLAY "QUALIFIED NAME, FIRST 30 CHARACTERS"
           ELSE
               DISPLAY "[" DEBUG-NAME "]".
       END DECLARATIVES.
       MAIN-SEC SECTION.
       M-P.
           PERFORM PARA-A.
           MOVE "A" TO ELEM (3).
           MOVE "B" TO ELEMENT-NAME-TWENTYX OF GROUP-QUALIFIER.
           MOVE "C" TO ELEMENT-NAME-TWENTYX OF OTHER-GROUP.
           STOP RUN.
       PARA-A.
           DISPLAY "IN PARA-A".
