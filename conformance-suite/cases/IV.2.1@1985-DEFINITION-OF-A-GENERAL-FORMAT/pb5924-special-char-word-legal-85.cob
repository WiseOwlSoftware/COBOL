      *>--- conformance-case
      *> id: pb5924-special-char-word-legal-85
      *> rules: [1985-SR-IV.2.1.7-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.2.1.7 (cite.py: OK): "are required when such portions"
      *> COMPUTE with its = and the relation > written: X is 3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SC1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9.
       PROCEDURE DIVISION.
       P1.
           COMPUTE X = 1 + 2.
           IF X > 2 DISPLAY X.
           STOP RUN.
