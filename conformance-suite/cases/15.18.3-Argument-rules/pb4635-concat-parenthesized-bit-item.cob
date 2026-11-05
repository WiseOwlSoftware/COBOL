*>--- conformance-case
*> id: pb4635-concat-parenthesized-bit-item
*> rules: [AR-15.18.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> PB4635 review R1-1 - ISO §15.18.3 r2: "If any argument is usage national, all arguments shall be usage national, otherwise all
      *> arguments shall be usage display." (cite.py --check OK). BT is an item of usage bit, which is neither, and the grouping
      *> parentheses of (BT) (a boolean expression enclosed in parentheses, §8.8.2) do not change the representation of the value it
      *> names: CONCAT(BT "A") is refused and so is CONCAT((BT) "A"). The parenthesized item bound to a boolean operand, which the
      *> usage reader answered null for, and the screen failed open.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGCONCATPARENBIT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BT    PIC 1(4) USAGE BIT VALUE B"1010".
       01 T     PIC X(8).
       PROCEDURE DIVISION.
           MOVE FUNCTION CONCAT((BT) "A") TO T.
           STOP RUN.
