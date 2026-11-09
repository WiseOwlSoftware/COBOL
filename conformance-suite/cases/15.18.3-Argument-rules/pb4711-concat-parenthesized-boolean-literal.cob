*>--- conformance-case
*> id: pb4711-concat-parenthesized-boolean-literal
*> rules: [AR-15.18.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> PB4711 review R1-1 - ISO §2023:15.18.3 AR1: "Argument-1 and argument-2 shall be data items or literals of class alphabetic,
      *> alphanumeric, boolean, numeric or national." (cite.py --check OK). (B"1") is a boolean expression enclosed in parentheses
      *> (§2023:8.8.2), not the literal it encloses, so CONCAT((B"1") "A") is refused as CONCAT((BB) "A") is
      *> (pb4711-concat-parenthesized-boolean-item); the bare CONCAT(B"1" "A") is legal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGCONCATPARENBOOLLIT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T     PIC X(8).
       PROCEDURE DIVISION.
           MOVE FUNCTION CONCAT((B"1") "A") TO T.
           STOP RUN.
