*>--- conformance-case
*> id: pb4763-class-condition-evaluate-boolean-expression-subject
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4763 (review R2-2): a class partial in EVALUATE forms a conditional expression with its subject
      *> (14.9.13.3 SR7 d), so the subject is identifier-1 of 8.8.4.4.2; a boolean EXPRESSION (an operator, with or
      *> without enclosing parentheses) is a value, not the identifier. COBOL3318 at every edition that has boolean data.
      *>   cite.py --check 8.8.4.4.2 "identifier-1" -> OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG4763D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B1 PIC 1 VALUE B"1".
       01 B2 PIC 1 VALUE B"1".
       01 R PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
           EVALUATE (B1 B-AND B2)
               WHEN BOOLEAN MOVE 1 TO R
           END-EVALUATE
           EVALUATE B-NOT B1
               WHEN NUMERIC MOVE 2 TO R
           END-EVALUATE
           DISPLAY R
           STOP RUN.
