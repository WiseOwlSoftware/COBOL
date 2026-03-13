       IDENTIFICATION DIVISION.
       PROGRAM-ID. ZTEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-TOTAL PIC 9(5) VALUE 0.
       01 WS-FLAG PIC 9 VALUE 0.
          88 DONE-FLAG VALUE 1.
       PROCEDURE DIVISION.
           MOVE ZEROS TO WS-TOTAL.
           DISPLAY WS-TOTAL.
           DISPLAY "   (copybooks expand at compile time)".
           DISPLAY "6. Fixed-form auto-detection enabled".
           STOP RUN.
