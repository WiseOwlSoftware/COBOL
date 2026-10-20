       IDENTIFICATION DIVISION.
       PROGRAM-ID. FUZZ.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-370.
       OBJECT-COMPUTER. IBM-370.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-RESULT    PIC 9(10)V9(10) VALUE 0.
       01  WS-OPERAND1  PIC 9(5)V9(5) VALUE 2.0.
       01  WS-OPERAND2  PIC 9(5)V9(5) VALUE 3.0.
       01  WS-TEST-CASE PIC 9(2) VALUE 0.
       01  WS-EXCEPTION-1 PIC X VALUE SPACE.
       01  WS-EXCEPTION-2 PIC X VALUE SPACE.
       01  WS-EXCEPTION-3 PIC X VALUE SPACE.
       01  WS-EXCEPTION-4 PIC X VALUE SPACE.
       01  WS-EXCEPTION-5 PIC X VALUE SPACE.
       01  WS-EXCEPTION-6 PIC X VALUE SPACE.
       01  WS-EXCEPTION-7 PIC X VALUE SPACE.
       01  WS-EXCEPTION-8 PIC X VALUE SPACE.
       01  WS-EXCEPTION-9 PIC X VALUE SPACE.
       01  WS-EXCEPTION-10 PIC X VALUE SPACE.
       PROCEDURE DIVISION.
           MOVE 1 TO WS-TEST-CASE
           PERFORM TEST-EXPONENTIATION

           MOVE 2 TO WS-TEST-CASE
           PERFORM TEST-EXPONENTIATION

           MOVE 3 TO WS-TEST-CASE
           PERFORM TEST-EXPONENTIATION

           MOVE 4 TO WS-TEST-CASE
           PERFORM TEST-EXPONENTIATION

           MOVE 5 TO WS-TEST-CASE
           PERFORM TEST-EXPONENTIATION

           MOVE 6 TO WS-TEST-CASE
           PERFORM TEST-EXPONENTIATION

           MOVE 7 TO WS-TEST-CASE
           PERFORM TEST-EXPONENTIATION

           MOVE 8 TO WS-TEST-CASE
           PERFORM TEST-EXPONENTIATION

           MOVE 9 TO WS-TEST-CASE
           PERFORM TEST-EXPONENTIATION

           MOVE 10 TO WS-TEST-CASE
           PERFORM TEST-EXPONENTIATION

           STOP RUN.

       TEST-EXPONENTIATION.
           EVALUATE WS-TEST-CASE
               WHEN 1
                   MOVE 2.0 TO WS-OPERAND1
                   MOVE 0.0 TO WS-OPERAND2
                   COMPUTE WS-RESULT = WS-OPERAND1 ** WS-OPERAND2
                   DISPLAY "CASE 1: 2 ** 0 = " WS-RESULT

               WHEN 2
                   MOVE 2.0 TO WS-OPERAND1
                   MOVE 1.0 TO WS-OPERAND2
                   COMPUTE WS-RESULT = WS-OPERAND1 ** WS-OPERAND2
                   DISPLAY "CASE 2: 2 ** 1 = " WS-RESULT

               WHEN 3
                   MOVE 2.0 TO WS-OPERAND1
                   MOVE 2.0 TO WS-OPERAND2
                   COMPUTE WS-RESULT = WS-OPERAND1 ** WS-OPERAND2
                   DISPLAY "CASE 3: 2 ** 2 = " WS-RESULT

               WHEN 4
                   MOVE 2.0 TO WS-OPERAND1
                   MOVE 3.0 TO WS-OPERAND2
                   COMPUTE WS-RESULT = WS-OPERAND1 ** WS-OPERAND2
                   DISPLAY "CASE 4: 2 ** 3 = " WS-RESULT

               WHEN 5
                   MOVE 2.0 TO WS-OPERAND1
                   MOVE 4.0 TO WS-OPERAND2
                   COMPUTE WS-RESULT = WS-OPERAND1 ** WS-OPERAND2
                   DISPLAY "CASE 5: 2 ** 4 = " WS-RESULT

               WHEN 6
                   MOVE 3.0 TO WS-OPERAND1
                   MOVE 2.0 TO WS-OPERAND2
                   COMPUTE WS-RESULT = WS-OPERAND1 ** WS-OPERAND2
                   DISPLAY "CASE 6: 3 ** 2 = " WS-RESULT

               WHEN 7
                   MOVE 2.0 TO WS-OPERAND1
                   MOVE -1.0 TO WS-OPERAND2
                   COMPUTE WS-RESULT = WS-OPERAND1 ** WS-OPERAND2
                   DISPLAY "CASE 7: 2 ** -1 = " WS-RESULT

               WHEN 8
                   MOVE 2.0 TO WS-OPERAND1
                   MOVE -2.0 TO WS-OPERAND2
                   COMPUTE WS-RESULT = WS-OPERAND1 ** WS-OPERAND2
                   DISPLAY "CASE 8: 2 ** -2 = " WS-RESULT

               WHEN 9
                   MOVE 0.0 TO WS-OPERAND1
                   MOVE 0.0 TO WS-OPERAND2
                   COMPUTE WS-RESULT = WS-OPERAND1 ** WS-OPERAND2
                   DISPLAY "CASE 9: 0 ** 0 = " WS-RESULT

               WHEN 10
                   MOVE 0.0 TO WS-OPERAND1
                   MOVE 5.0 TO WS-OPERAND2
                   COMPUTE WS-RESULT = WS-OPERAND1 ** WS-OPERAND2
                   DISPLAY "CASE 10: 0 ** 5 = " WS-RESULT

               WHEN OTHER
                   DISPLAY "INVALID TEST CASE"
           END-EVALUATE.
