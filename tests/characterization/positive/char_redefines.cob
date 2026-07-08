       IDENTIFICATION DIVISION.
       PROGRAM-ID. CHAR-REDEFINES.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A     PIC 9(4).
       01 WS-A-R   REDEFINES WS-A PIC 9(4).
       01 WS-G.
          05 WS-P1 PIC X(2).
          05 WS-P2 PIC X(2).
       01 WS-G-R   REDEFINES WS-G PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 1234 TO WS-A.
           DISPLAY WS-A-R.
           MOVE "WXYZ" TO WS-G-R.
           DISPLAY WS-P1 WS-P2.
           STOP RUN.
