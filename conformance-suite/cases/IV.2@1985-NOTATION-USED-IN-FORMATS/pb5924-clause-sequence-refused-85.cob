      *>--- conformance-case
      *> id: pb5924-clause-sequence-refused-85
      *> rules: [1985-SR-IV.2.1-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.2.1 (cite.py: OK): "Clauses must be written in the sequence"
      *> REMAINDER written before GIVING, against DIVIDE format 5's
      *> sequence: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEQ2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Q PIC 9.
       01 R PIC 9.
       PROCEDURE DIVISION.
       P1.
           DIVIDE 7 INTO 20 REMAINDER R GIVING Q.
           DISPLAY Q.
           STOP RUN.
