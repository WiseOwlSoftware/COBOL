*>--- conformance-case
*> id: pb4865-perform-varying-user-function-below-2002
*> rules: [SR-14.9.28.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> PB4865 - the twin of pb4865_perform_varying_user_function_integer_operand below COBOL-2002: a user-defined function and
      *> its FUNCTION-ID paragraph do not exist in COBOL-85 (ISO/IEC 1989:2002 Annex F.2), so the program is refused there.
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
       PROGRAM-ID. NEGPV4865.
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
       END PROGRAM NEGPV4865.
