*>--- conformance-case
*> id: pb4865-perform-varying-user-function-not-integer
*> rules: [SR-14.9.28.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> PB4865 - the other arm of the per-evaluation carrier: SR4 a) wants an integer data item, and FRAC returns PIC 9V9, so it is
      *> refused in BY and in AFTER FROM although the carrier is transparent to the type.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. FRAC.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC 9V9.
       PROCEDURE DIVISION RETURNING R.
           MOVE 2 TO R
           GOBACK.
       END FUNCTION FRAC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPV4865N.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION FRAC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E PIC X OCCURS 9 INDEXED BY IX.
       01 I PIC 99.
       01 J PIC 99.
       PROCEDURE DIVISION.
           PERFORM VARYING IX FROM 1 BY FUNCTION FRAC UNTIL IX > 5
               SET J TO IX
           END-PERFORM
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 2
               AFTER IX FROM FUNCTION FRAC BY 1 UNTIL IX > 3
               SET J TO IX
           END-PERFORM
           STOP RUN.
       END PROGRAM NEGPV4865N.
