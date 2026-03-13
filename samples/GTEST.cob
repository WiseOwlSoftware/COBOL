       IDENTIFICATION DIVISION.
       PROGRAM-ID. GTEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-NAME PIC X(30) VALUE "Test".
       01 WS-TOTAL PIC 9(5) VALUE 0.
       PROCEDURE DIVISION.
           GO TO SHOW-GOTO.
       UNREACHABLE-PARA.
           DISPLAY "ERROR".
       SHOW-GOTO.
           MOVE ZEROS TO WS-TOTAL.
           DISPLAY WS-TOTAL.
           MOVE SPACES TO WS-NAME.
           DISPLAY "SPACES applied".
           DISPLAY "   (copybooks expand at compile time)".
           DISPLAY "6. Fixed-form auto-detection enabled".
           DISPLAY "   (column-based source processed)".
           STOP RUN.
