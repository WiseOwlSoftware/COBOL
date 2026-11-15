      *>--- conformance-case
      *> id: pb5416-condition-national-literal
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5416, the sibling position. The national literal's COBOL-2002
      *> gate is the literal token's, asked wherever the literal is written: a
      *> level-88 VALUE of an alphanumeric item seeds no national item, so no
      *> data-item gate names the edition, and before PB5416 the program drew
      *> only the category refusal COBOL0898. At COBOL-85 it is COBOL0900 too.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5416CN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X PIC X.
           88  C VALUE N"A".
       PROCEDURE DIVISION.
       P1.
           DISPLAY "OK".
           STOP RUN.
