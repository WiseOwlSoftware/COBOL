      *>--- conformance-case
      *> id: 1985-GR-XV_3_2_4-10
      *> rules: [1985-GR-XV.3.2.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XV.3.2.4 GR10 (THE USE FOR DEBUGGING STATEMENT). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DBGITM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANY-COMPUTER WITH DEBUGGING MODE.
       OBJECT-COMPUTER. ANY-COMPUTER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL.
           02  L1 OCCURS 2 TIMES.
               03  L2 OCCURS 3 TIMES.
                   04  ELEM PIC XX OCCURS 4 TIMES.
       01  WS-REC.
           02  W-LINE PIC X(6).
           02  W-S1   PIC X.
           02  W-NAME PIC X(30).
           02  W-S2   PIC X.
           02  W-SUB1 PIC X(5).
           02  W-S3   PIC X.
           02  W-SUB2 PIC X(5).
           02  W-S4   PIC X.
           02  W-SUB3 PIC X(5).
           02  W-S5   PIC X.
           02  W-REST PIC X(2).
       01  N PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG SECTION.
           USE FOR DEBUGGING ON ELEM.
       DBG-PARA.
           MOVE DEBUG-ITEM TO WS-REC.
           DISPLAY "[" W-S1 W-NAME W-S2 W-SUB1 W-S3 W-SUB2 W-S4
               W-SUB3 W-S5 W-REST "]".
           ADD DEBUG-SUB-1 DEBUG-SUB-2 DEBUG-SUB-3 TO N.
           DISPLAY N.
           IF DEBUG-LINE = W-LINE AND DEBUG-NAME = W-NAME
               DISPLAY "LINE AND NAME IN PLACE".
       END DECLARATIVES.
       MAIN SECTION.
       M0.
           MOVE "ZZ" TO ELEM (2, 3, 4).
           DISPLAY "END".
           STOP RUN.
