      *>--- conformance-case
      *> id: pb5924-arith-edited-operand-85
      *> rules: [1985-SR-VI.6.2.1-p2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VI.6.2.1 (cite.py: OK): "must represent
      *> either numeric elementary items or numeric literals"
      *> A numeric-edited item is not a numeric elementary item: as a
      *> COMPUTE operand it is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5924B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N  PIC 9(3) VALUE 5.
       01  ED PIC ZZ9 VALUE "  7".
       PROCEDURE DIVISION.
       P0.
           COMPUTE N = ED + 1.
           STOP RUN.
