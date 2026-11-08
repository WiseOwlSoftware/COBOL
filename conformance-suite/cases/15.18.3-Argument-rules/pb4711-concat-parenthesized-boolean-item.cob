*>--- conformance-case
*> id: pb4711-concat-parenthesized-boolean-item
*> rules: [AR-15.18.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> PB4711 - ISO §2023:15.18.3 AR1: "Argument-1 and argument-2 shall be data items or literals of class alphabetic, alphanumeric,
      *> boolean, numeric or national." (cite.py --check OK). (BB) is a boolean expression enclosed in parentheses (§2023:8.8.2), not the
      *> data item it encloses, so CONCAT((BB) "A") is refused as CONCAT((U2) "A") is (pb4635-concat-parenthesized-item); the bare
      *> CONCAT(BB "A") is legal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGCONCATPARENBOOL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BB    PIC 1(4) VALUE B"1010".
       01 T     PIC X(8).
       PROCEDURE DIVISION.
           MOVE FUNCTION CONCAT((BB) "A") TO T.
           STOP RUN.
