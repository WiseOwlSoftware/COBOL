*>--- conformance-case
*> id: pb4635-concat-parenthesized-boolean-item
*> rules: [AR-15.18.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> PB4635 review R3-3 - ISO §15.18.3 r1: "Argument-1 and argument-2 shall be data items or literals of class alphabetic, alphanumeric,
      *> boolean, numeric or national." (BB) is "a boolean expression enclosed in parentheses" (§8.8.2), which is neither a data item nor a
      *> literal; the argument binder returned it as the item BB, so a position that takes no boolean expression accepted it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGCONCATPARENBOOL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BB    PIC 1(4) VALUE B"1010".
       01 T     PIC X(8).
       PROCEDURE DIVISION.
           MOVE FUNCTION CONCAT((BB) "A") TO T.
           STOP RUN.
