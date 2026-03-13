       IDENTIFICATION DIVISION.
       PROGRAM-ID. PTEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-COUNTER PIC 9(3) VALUE 0.
       PROCEDURE DIVISION.
           PERFORM STEP-A THRU STEP-C.
           DISPLAY WS-COUNTER.
           GO TO SHOW-GOTO.
       UNREACHABLE-PARA.
           DISPLAY "ERROR".
       SHOW-GOTO.
           DISPLAY "Fixed-form auto-detection enabled".
           DISPLAY "   (copybooks expand at compile time)".
           PERFORM EXIT-DEMO.
           CONTINUE.
           DISPLAY "CONTINUE reached".
           STOP RUN.
       STEP-A.
           ADD 10 TO WS-COUNTER.
       STEP-B.
           ADD 20 TO WS-COUNTER.
       STEP-C.
           ADD 5 TO WS-COUNTER.
       EXIT-DEMO.
           DISPLAY "In EXIT-DEMO".
           EXIT PARAGRAPH.
           DISPLAY "ERROR: After EXIT (should not appear)".
