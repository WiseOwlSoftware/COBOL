       IDENTIFICATION DIVISION.
       PROGRAM-ID. DEMO.

       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-NAME        PIC X(20) VALUE "CobolSharp".
       01 WS-COUNTER     PIC 9(3)  VALUE 0.
       01 WS-TOTAL        PIC 9(5)  VALUE 0.
       01 WS-RESULT       PIC 9(5)  VALUE 0.

       PROCEDURE DIVISION.
           DISPLAY "=== CobolSharp Compiler Demo ===".
           DISPLAY " ".

           DISPLAY "1. String display: Hello from " WS-NAME "!".

           DISPLAY "2. Arithmetic:".
           ADD 10 TO WS-TOTAL.
           ADD 20 TO WS-TOTAL.
           ADD 15 TO WS-TOTAL.
           DISPLAY "   Sum of 10+20+15 = " WS-TOTAL.

           SUBTRACT 5 FROM WS-TOTAL.
           DISPLAY "   After subtract 5 = " WS-TOTAL.

           COMPUTE WS-RESULT = 3 + 4 * 2.
           DISPLAY "   COMPUTE 3+4*2 = " WS-RESULT.

           DISPLAY "3. Conditionals:".
           IF WS-TOTAL > 30
               DISPLAY "   Total is greater than 30"
           ELSE
               DISPLAY "   Total is 30 or less"
           END-IF.

           DISPLAY "4. PERFORM paragraph:".
           PERFORM COUNT-UP.
           PERFORM COUNT-UP.
           PERFORM COUNT-UP.
           DISPLAY "   Counter after 3 PERFORMs: " WS-COUNTER.

           DISPLAY " ".
           DISPLAY "=== Demo Complete ===".
           STOP RUN.

       COUNT-UP.
           ADD 1 TO WS-COUNTER.
