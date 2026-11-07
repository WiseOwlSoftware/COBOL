*>--- conformance-case
*> id: pb4865-perform-varying-parenthesized-user-function
*> rules: [SR-14.9.28.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> PB4865 - ISO §2023:8.8.1.1 makes a parenthesized operand an arithmetic expression, and §2023:14.9.28.2 prints { identifier-4 | literal-2 } for BY
      *> and { identifier-3 | index-name-2 | literal-1 } for FROM, so (FUNCTION TWO) is refused in BY, where the unparenthesized form is not.
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
       PROGRAM-ID. NEGPV4865P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION TWO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E PIC X OCCURS 9 INDEXED BY IX.
       01 J PIC 99.
       PROCEDURE DIVISION.
           PERFORM VARYING IX FROM 1 BY (FUNCTION TWO) UNTIL IX > 5
               SET J TO IX
               DISPLAY "BY " J
           END-PERFORM
           STOP RUN.
       END PROGRAM NEGPV4865P.
