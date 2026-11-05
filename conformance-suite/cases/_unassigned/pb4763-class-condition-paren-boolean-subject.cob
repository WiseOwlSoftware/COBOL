*>--- conformance-case
*> id: pb4763-class-condition-paren-boolean-subject
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4763 (review R1-1): a class condition prints identifier-1 alone (ISO 8.8.4.4.2); a boolean expression
      *> enclosed in parentheses is an expression (8.8.2), a value, not the identifier. (B1) IS BOOLEAN and
      *> EVALUATE (B1) WHEN BOOLEAN were admitted because the enclosed-boolean operand carried no trace of the
      *> parentheses. COBOL3318 at every edition that has boolean data.
      *>   cite.py --check 8.8.4.4.2 "identifier-1" -> OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG4763C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B1 PIC 1 VALUE B"1".
       01 R PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
           IF (B1) IS BOOLEAN
               MOVE 1 TO R
           END-IF
           EVALUATE (B1)
               WHEN BOOLEAN MOVE 2 TO R
           END-EVALUATE
           DISPLAY R
           STOP RUN.
