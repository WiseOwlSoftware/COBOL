*>--- conformance-case
*> id: pb4507-findstr-parenthesized-function
*> rules: [AR-15.37.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> PB4507 review R1-1 - ISO §15.37.3 r3: argument-3 "shall be an integer data item or integer literal". A function-identifier
      *> is a data item (§8.4.3.2.4 r1), but a PARENTHESIZED function-identifier is an arithmetic expression (§8.8.1.1: "an
      *> arithmetic expression enclosed in parentheses"), and an arithmetic expression is neither. The expression binder drops
      *> the grouping parentheses and binds (FUNCTION ABS(I2)) to the very tree FUNCTION ABS(I2) binds to, so the screen must
      *> take the WRITTEN shape from the binder's statement and not from the tree (PB3678's doctrine).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGFINDSTRPARENFN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X9    PIC X(9) VALUE "ABCABCABC".
       01 I2    PIC 9 VALUE 2.
       01 T     PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE T = FUNCTION FIND-STRING(X9 "ABC" (FUNCTION ABS(I2))).
           STOP RUN.
