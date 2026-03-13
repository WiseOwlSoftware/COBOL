       IDENTIFICATION DIVISION.
       PROGRAM-ID. DEMO3.
       *> Phase 3 Demo: Control Flow, Preprocessor

       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-NAME        PIC X(30) VALUE "CobolSharp Compiler".
       01 WS-COUNTER     PIC 9(3) VALUE 0.
       01 WS-TOTAL       PIC 9(5) VALUE 0.
       01 WS-FLAG        PIC 9    VALUE 0.
          88 DONE-FLAG   VALUE 1.

       PROCEDURE DIVISION.
           DISPLAY "=== Phase 3 Demo: Advanced Features ===".
           DISPLAY " ".

           DISPLAY "1. PERFORM THRU:".
           PERFORM STEP-A THRU STEP-C.
           DISPLAY "   Counter after THRU: " WS-COUNTER.

           DISPLAY "2. Figurative constants:".
           MOVE ZEROS TO WS-TOTAL.
           DISPLAY "   ZEROS: " WS-TOTAL.

           DISPLAY "3. EXIT and CONTINUE:".
           PERFORM EXIT-DEMO.
           CONTINUE.
           DISPLAY "   CONTINUE is a no-op (reached here)".

           DISPLAY "4. COPY preprocessor: enabled".
           DISPLAY "5. Fixed-form detection: enabled".

           DISPLAY " ".
           DISPLAY "=== Phase 3 Demo Complete ===".
           STOP RUN.

       STEP-A.
           ADD 10 TO WS-COUNTER.
       STEP-B.
           ADD 20 TO WS-COUNTER.
       STEP-C.
           ADD 5 TO WS-COUNTER.

       EXIT-DEMO.
           DISPLAY "   Entered EXIT-DEMO paragraph".
           EXIT PARAGRAPH.
           DISPLAY "   ERROR: After EXIT (should not appear)".
