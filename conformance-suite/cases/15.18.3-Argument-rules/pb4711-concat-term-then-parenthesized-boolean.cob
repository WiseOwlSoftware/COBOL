*>--- conformance-case
*> id: pb4711-concat-term-then-parenthesized-boolean
*> rules: [AR-15.18.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> PB4711 review R1-3 - the argument scan ends an argument where a space-separated term follows a completed one, a grouping '('
      *> included, so CONCAT("A" (B"1")) is TWO arguments, not one boolean expression (§2023:8.8.2: every term of a boolean expression is
      *> joined by a B-operator). The second is a boolean expression enclosed in parentheses, which §2023:15.18.3 AR1 ("data items or
      *> literals ...", cite.py --check OK) refuses in the argument-2 position as it does in argument-1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGCONCATTERMPAREN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T     PIC X(8).
       PROCEDURE DIVISION.
           MOVE FUNCTION CONCAT("A" (B"1")) TO T.
           STOP RUN.
