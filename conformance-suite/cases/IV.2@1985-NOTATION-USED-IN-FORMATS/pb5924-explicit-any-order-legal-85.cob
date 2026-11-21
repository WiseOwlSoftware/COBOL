      *>--- conformance-case
      *> id: pb5924-explicit-any-order-legal-85
      *> rules: [1985-SR-IV.2.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.2.1 (cite.py: OK): "the clauses may appear in sequences other"
      *> VI.5.3.3 SR2 states the data description clauses after the
      *> data-name may be in any order: VALUE before PIC is accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEQ5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X VALUE 5 PIC 9.
       01 Y USAGE DISPLAY VALUE "AB" PIC XX.
       PROCEDURE DIVISION.
       P1.
           DISPLAY X Y.
           STOP RUN.
