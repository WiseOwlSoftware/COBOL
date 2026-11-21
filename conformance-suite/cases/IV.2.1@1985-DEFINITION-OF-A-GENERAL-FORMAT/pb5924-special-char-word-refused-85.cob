      *>--- conformance-case
      *> id: pb5924-special-char-word-refused-85
      *> rules: [1985-SR-IV.2.1.7-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.2.1.7 (cite.py: OK): "are required when such portions"
      *> COMPUTE without the special character word =: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SC2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9.
       PROCEDURE DIVISION.
       P1.
           COMPUTE X 1 + 2.
           DISPLAY X.
           STOP RUN.
