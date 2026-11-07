*>--- conformance-case
*> id: pb4865-perform-varying-signed-user-function
*> rules: [SR-14.9.28.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> PB4865 - a unary sign SEPARATED from its operand is a unary operator applied to an expression (ISO §2023:8.8.1.1; §2023:8.3.3.3.2 rule 2 puts only
      *> an ADJACENT sign inside a literal), so BY + FUNCTION TWO is no identifier-4 of §2023:14.9.28.2 and is refused.
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
       PROGRAM-ID. NEGPV4865S.
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
           PERFORM VARYING IX FROM 1 BY + FUNCTION TWO UNTIL IX > 5
               SET J TO IX
               DISPLAY "BY " J
           END-PERFORM
           STOP RUN.
       END PROGRAM NEGPV4865S.
