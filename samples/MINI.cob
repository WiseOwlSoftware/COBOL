       IDENTIFICATION DIVISION.
       PROGRAM-ID. MINI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-COUNTER     PIC 9(3) VALUE 0.
       PROCEDURE DIVISION.
           DISPLAY "1. PERFORM THRU:".
           PERFORM STEP-A THRU STEP-C.
           DISPLAY "   Counter: " WS-COUNTER.
           DISPLAY "2. GO TO:".
           GO TO SHOW-GOTO.
       UNREACHABLE.
           DISPLAY "ERROR".
       SHOW-GOTO.
           DISPLAY "   GO TO worked".
           DISPLAY "3. Parens in string: (test)".
           DISPLAY "4. Fixed-form auto-detection enabled".
           DISPLAY "   (copybooks expand at compile time)".
           STOP RUN.
       STEP-A.
           ADD 10 TO WS-COUNTER.
       STEP-B.
           ADD 20 TO WS-COUNTER.
       STEP-C.
           ADD 5 TO WS-COUNTER.
