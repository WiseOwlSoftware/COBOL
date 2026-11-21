      *>--- conformance-case
      *> id: pb5924-arith-alnum-operand-85
      *> rules: [1985-SR-VI.6.2.1-p2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VI.6.2.1 (cite.py: OK): "must represent
      *> either numeric elementary items or numeric literals"
      *> An alphanumeric item as an operand of the arithmetic expression
      *> in a relation condition: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5924A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  AX PIC X(3) VALUE "001".
       PROCEDURE DIVISION.
       P0.
           IF AX + 1 = 2 DISPLAY "Y".
           STOP RUN.
