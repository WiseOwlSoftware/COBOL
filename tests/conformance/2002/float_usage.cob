      *> ISO 13.18 — standard floating-point USAGE FLOAT-SHORT (IEEE-754 single = COMP-1) and FLOAT-LONG
      *> (IEEE-754 double = COMP-2). 10.5*2 = 21, 100.25*4 = 401.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FLTEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-S USAGE FLOAT-SHORT.
       01 WS-L FLOAT-LONG.
       01 WS-R PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 10.5 TO WS-S.
           COMPUTE WS-R = WS-S * 2.
           DISPLAY "S=" WS-R.
           MOVE 100.25 TO WS-L.
           COMPUTE WS-R = WS-L * 4.
           DISPLAY "L=" WS-R.
           STOP RUN.
