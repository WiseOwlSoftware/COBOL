       IDENTIFICATION DIVISION.
       PROGRAM-ID. CHAR-GROUP-MOVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-SRC.
          05 S-NAME PIC X(4).
          05 S-QTY  PIC 9(3).
       01 WS-DST.
          05 D-NAME PIC X(4).
          05 D-QTY  PIC 9(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "BOLT" TO S-NAME.
           MOVE 7 TO S-QTY.
           MOVE WS-SRC TO WS-DST.
           DISPLAY WS-DST.
           STOP RUN.
