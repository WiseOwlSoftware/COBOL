*>--- conformance-case
*> id: pb4635-findstr-parenthesized-item
*> rules: [AR-15.37.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> PB4635 - ISO §2023:15.37.3 r3: argument-3 "shall be an integer data item or integer literal". (I2) is an arithmetic
      *> expression enclosed in parentheses (§2023:8.8.1.1), not the data item it encloses; the expression binder drops the
      *> grouping parentheses, so the argument binder takes the WRITTEN shape (IntrinsicBinder.BindArgOperandCore).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGFINDSTRPARENITEM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X9    PIC X(9) VALUE "ABCABCABC".
       01 I2    PIC 9 VALUE 2.
       01 T     PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE T = FUNCTION FIND-STRING(X9 "ABC" (I2)).
           STOP RUN.
