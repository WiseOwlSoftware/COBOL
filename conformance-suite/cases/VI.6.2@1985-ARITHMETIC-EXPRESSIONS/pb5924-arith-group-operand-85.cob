      *>--- conformance-case
      *> id: pb5924-arith-group-operand-85
      *> rules: [1985-SR-VI.6.2.1-p2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VI.6.2.1 (cite.py: OK): "must represent
      *> either numeric elementary items or numeric literals"
      *> A group item is not a numeric elementary item, even when its
      *> only subordinate is numeric: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5924C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N  PIC 9(3) VALUE 5.
       01  G.
           05  GE PIC 9(3) VALUE 4.
       PROCEDURE DIVISION.
       P0.
           COMPUTE N = G + 1.
           STOP RUN.
