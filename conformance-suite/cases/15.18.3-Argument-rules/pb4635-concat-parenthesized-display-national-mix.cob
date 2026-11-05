*>--- conformance-case
*> id: pb4635-concat-parenthesized-display-national-mix
*> rules: [AR-15.18.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> PB4635 review R1-1 - ISO §15.18.3 r2: "If any argument is usage national, all arguments shall be usage national, otherwise all
      *> arguments shall be usage display." (cite.py --check OK). (BB) is a usage display boolean item, N"A" a national literal, so the
      *> mix is refused exactly as CONCAT(BB N"A") is.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGCONCATPARENMIX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BB    PIC 1(4) VALUE B"1010".
       01 T     PIC N(8).
       PROCEDURE DIVISION.
           MOVE FUNCTION CONCAT((BB) N"A") TO T.
           STOP RUN.
