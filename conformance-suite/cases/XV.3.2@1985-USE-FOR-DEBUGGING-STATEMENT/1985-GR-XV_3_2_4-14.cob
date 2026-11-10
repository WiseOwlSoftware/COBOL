      *>--- conformance-case
      *> id: 1985-GR-XV_3_2_4-14
      *> rules: [1985-GR-XV.3.2.4-14]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XV.3.2.4 GR14 (THE USE FOR DEBUGGING STATEMENT). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DBGSUB.
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
       01  TBL1.
           02  ONE PIC X OCCURS 5 TIMES INDEXED BY IX.
       01  SUB PIC 9 VALUE 2.
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
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SEC SECTION.
           USE FOR DEBUGGING ON ELEM ONE.
       D-PAR.
           MOVE DEBUG-ITEM TO WS-REC.
           DISPLAY W-NAME (1:4) " [" W-SUB1 "] [" W-SUB2 "] ["
               W-SUB3 "]".
       END DECLARATIVES.
       MAIN-SEC SECTION.
       M-PAR.
           MOVE "AA" TO ELEM (2, 3, 4).
           MOVE "BB" TO ELEM (1, SUB, 3).
           SET IX TO 5.
           MOVE "C" TO ONE (IX).
           MOVE "D" TO ONE (IX - 3).
           STOP RUN.
