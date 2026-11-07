*>--- conformance-case
*> id: pb4865_perform_varying_user_function_integer_operand
*> rules: [SR-14.9.28.3-4]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> PB4865 - an index-name is varied, so ISO §2023:14.9.28.3 SR4 a) asks of the identifier in the associated FROM and BY phrases an
      *> integer data item. FUNCTION TWO is a user-defined function-identifier (§2023:8.4.3.1.2 Format 1) that references a temporary item
      *> the function RETURNs, PIC 9, an integer. Evaluating it activates the function, so the binder carries the activation with
      *> the operand; SR4 a) reads the integer through that carrier. IX counts 1, 3, 5 by the function in BY; in the AFTER phrase
      *> IX starts at the function's value 2 and runs to 3 for each of the two values of I.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. TWO.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC 9.
       PROCEDURE DIVISION RETURNING R.
           MOVE 2 TO R
           GOBACK.
       END FUNCTION TWO.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4865ACCEPT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION TWO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E PIC X OCCURS 9 INDEXED BY IX.
       01 I PIC 99.
       01 J PIC 99.
       PROCEDURE DIVISION.
           PERFORM VARYING IX FROM 1 BY FUNCTION TWO UNTIL IX > 5
               SET J TO IX
               DISPLAY "BY " J
           END-PERFORM
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 2
               AFTER IX FROM FUNCTION TWO BY 1 UNTIL IX > 3
               SET J TO IX
               DISPLAY "AFTER " I " " J
           END-PERFORM
           STOP RUN.
       END PROGRAM PB4865ACCEPT.
