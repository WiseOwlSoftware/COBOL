       IDENTIFICATION DIVISION.
       PROGRAM-ID. DEMO5.
       *> Phase 5 Demo: Advanced Features

       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-RESULT       PIC 9(5)V99 VALUE 0.
       01 WS-NAME         PIC X(20) VALUE "hello world".
       01 WS-DATE-STR     PIC X(21).
       01 WS-PI           PIC 9V9(10) VALUE 0.
       01 WS-VALUES.
          05 WS-A PIC 9(3) VALUE 10.
          05 WS-B PIC 9(3) VALUE 25.
          05 WS-C PIC 9(3) VALUE 5.
          05 WS-D PIC 9(3) VALUE 40.

       PROCEDURE DIVISION.
           DISPLAY "=== Phase 5 Demo: Advanced Features ===".
           DISPLAY " ".

           DISPLAY "1. Intrinsic Functions (Math):".
           COMPUTE WS-RESULT = FUNCTION SQRT(144).
           DISPLAY "   SQRT(144) = " WS-RESULT.
           COMPUTE WS-RESULT = FUNCTION ABS(-42).
           DISPLAY "   ABS(-42) = " WS-RESULT.
           COMPUTE WS-RESULT = FUNCTION MOD(17, 5).
           DISPLAY "   MOD(17,5) = " WS-RESULT.
           COMPUTE WS-RESULT = FUNCTION FACTORIAL(6).
           DISPLAY "   FACTORIAL(6) = " WS-RESULT.

           DISPLAY "2. Intrinsic Functions (Aggregates):".
           COMPUTE WS-RESULT = FUNCTION MAX(10, 25, 5, 40).
           DISPLAY "   MAX(10,25,5,40) = " WS-RESULT.
           COMPUTE WS-RESULT = FUNCTION MIN(10, 25, 5, 40).
           DISPLAY "   MIN(10,25,5,40) = " WS-RESULT.
           COMPUTE WS-RESULT = FUNCTION MEAN(10, 25, 5, 40).
           DISPLAY "   MEAN(10,25,5,40) = " WS-RESULT.

           DISPLAY "3. Intrinsic Functions (String):".
           DISPLAY "   UPPER-CASE: "
               FUNCTION UPPER-CASE("hello world").
           DISPLAY "   REVERSE: "
               FUNCTION REVERSE("CobolSharp").
           DISPLAY "   LENGTH: "
               FUNCTION LENGTH("CobolSharp").

           DISPLAY "4. OO COBOL parsing: supported".
           DISPLAY "   (CLASS-ID, METHOD-ID, INVOKE)".

           DISPLAY "5. Report Writer parsing: supported".
           DISPLAY "   (REPORT SECTION, RD, INITIATE,".
           DISPLAY "    GENERATE, TERMINATE)".

           DISPLAY "6. Exception handling: supported".
           DISPLAY "   (RAISE, RESUME)".

           DISPLAY "7. Compiler directives: supported".
           DISPLAY "   (>>SOURCE FORMAT IS FREE/FIXED)".

           DISPLAY " ".
           DISPLAY "=== Phase 5 Demo Complete ===".
           STOP RUN.
