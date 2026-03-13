       IDENTIFICATION DIVISION.
       PROGRAM-ID. MINI2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-COUNTER     PIC 9(3) VALUE 0.
       PROCEDURE DIVISION.
           DISPLAY "1. PERFORM THRU:".
           PERFORM STEP-A THRU STEP-C.
           DISPLAY "   Counter: " WS-COUNTER.
           DISPLAY "2. EXIT and CONTINUE:".
           PERFORM EXIT-DEMO.
           CONTINUE.
           DISPLAY "   CONTINUE reached".
           DISPLAY "3. (copybooks expand at compile time)".
           STOP RUN.
       STEP-A.
           ADD 10 TO WS-COUNTER.
       STEP-B.
           ADD 20 TO WS-COUNTER.
       STEP-C.
           ADD 5 TO WS-COUNTER.
       EXIT-DEMO.
           DISPLAY "   In EXIT-DEMO".
           EXIT PARAGRAPH.
           DISPLAY "   ERROR: after EXIT".
