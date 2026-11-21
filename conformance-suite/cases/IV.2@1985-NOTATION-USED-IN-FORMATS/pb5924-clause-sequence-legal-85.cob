      *>--- conformance-case
      *> id: pb5924-clause-sequence-legal-85
      *> rules: [1985-SR-IV.2.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.2.1 (cite.py: OK): "Clauses must be written in the sequence"
      *> DIVIDE's GIVING then REMAINDER in the printed sequence: 20/7 is
      *> quotient 2 remainder 6.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEQ1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Q PIC 9.
       01 R PIC 9.
       PROCEDURE DIVISION.
       P1.
           DIVIDE 7 INTO 20 GIVING Q REMAINDER R.
           DISPLAY Q.
           DISPLAY R.
           STOP RUN.
