*>--- conformance-case
*> id: pb4763-class-condition-paren-function-subject
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4763: a class condition prints identifier-1 alone (ISO 8.8.4.4.2, general format "identifier-1 IS [ NOT ]");
      *> (FUNCTION ABS(I2)) is "an arithmetic expression enclosed in parentheses" (8.8.1.1), a value, not an identifier whose
      *> characters the condition tests. The expression binder drops the grouping parentheses, so the screen that asked the
      *> TREE's shape admitted it while (I2) over the same item was COBOL3318. COBOL3318 at every edition that has functions.
      *>   cite.py --check 8.8.4.4.2 "identifier-1" -> OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG4763A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I2 PIC S9(4) VALUE -7.
       01 R PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
           IF (FUNCTION ABS(I2)) IS NUMERIC
               MOVE 1 TO R
           END-IF
           DISPLAY R
           STOP RUN.
