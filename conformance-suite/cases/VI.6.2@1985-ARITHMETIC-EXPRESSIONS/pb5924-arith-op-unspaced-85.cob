      *>--- conformance-case
      *> id: pb5924-arith-op-unspaced-85
      *> rules: [1985-SR-VI.6.2.2-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VI.6.2.2 (cite.py: OK): "must be preceded
      *> by a space and followed by a space"
      *> A binary '+' with no space on either side: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5924E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N  PIC 9(3) VALUE 5.
       PROCEDURE DIVISION.
       P0.
           COMPUTE N = N+1.
           STOP RUN.
