*>--- conformance-case
*> id: pb6304-function-result-ref-mod-out-of-range
*> rules: [GR-8.4.3.3.4-5]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB6304 (the Revisit line of kb/Work PB1707). ISO 8.4.3.3.4 GR5b: leftmost-position is a position of the
      *> item, and GR5c: "The sum of leftmost-position and length minus the value one shall be less than or equal to
      *> the number of positions in the data item referenced by identifier-1" (cite.py OK). A user-defined
      *> function's result is a temporary of the RETURNING item's description (8.4.3.2.4 GR1), here PIC X(8), so
      *> (20:5) starts past it and is refused at compile time while checking is off (COBOL2670), as W (20:5) over a
      *> PIC X(8) item is. The function-result path never asked the literal range screen.
      *> WHY THE LEG FAILS. A reference modifier applied to the result without the screen accepts it.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB6304RF.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-R PIC X(8).
       PROCEDURE DIVISION RETURNING LK-R.
           MOVE "ABCDEFGH" TO LK-R.
           GOBACK.
       END FUNCTION PB6304RF.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6304FUNC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB6304RF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(3).
       PROCEDURE DIVISION.
           MOVE FUNCTION PB6304RF (20:5) TO X.
           STOP RUN.
       END PROGRAM PB6304FUNC.
