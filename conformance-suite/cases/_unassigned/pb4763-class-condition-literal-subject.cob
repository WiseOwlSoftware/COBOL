*>--- conformance-case
*> id: pb4763-class-condition-literal-subject
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4763 (review R1-2): a class condition prints identifier-1 alone (ISO 8.8.4.4.2); a literal or a
      *> figurative constant is no identifier. IF 5 IS NUMERIC, IF "5" IS NUMERIC and IF ZERO IS NUMERIC compiled
      *> (the first aborted at run time). COBOL3318 at every edition.
      *>   cite.py --check 8.8.4.4.2 "identifier-1" -> OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG4763D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
           IF 5 IS NUMERIC
               MOVE 1 TO R
           END-IF
           IF "5" IS NUMERIC
               MOVE 2 TO R
           END-IF
           IF ZERO IS NUMERIC
               MOVE 3 TO R
           END-IF
           DISPLAY R
           STOP RUN.
