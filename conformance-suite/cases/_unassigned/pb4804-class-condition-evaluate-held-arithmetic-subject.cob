*>--- conformance-case
*> id: pb4804-class-condition-evaluate-held-arithmetic-subject
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4804: the arithmetic or parenthesized EVALUATE subject that more than one WHEN reads is held in an
      *> intermediate (14.9.13.4 GR3), and the held operand must stay "an expression as written": a class partial forms
      *> a conditional expression with its subject (14.9.13.3 SR7 d), whose subject is identifier-1 of 8.8.4.4.2, not
      *> an arithmetic expression. COBOL3318 at every edition.
      *>   cite.py --check 8.8.4.4.2 "identifier-1" -> OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG4804A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(3) VALUE 5.
       PROCEDURE DIVISION.
           EVALUATE (N + 1)
               WHEN NUMERIC DISPLAY "A"
               WHEN NOT NUMERIC DISPLAY "B"
               WHEN OTHER DISPLAY "C"
           END-EVALUATE
           EVALUATE (N)
               WHEN NUMERIC DISPLAY "A"
               WHEN NOT NUMERIC DISPLAY "B"
               WHEN OTHER DISPLAY "C"
           END-EVALUATE
           STOP RUN.
