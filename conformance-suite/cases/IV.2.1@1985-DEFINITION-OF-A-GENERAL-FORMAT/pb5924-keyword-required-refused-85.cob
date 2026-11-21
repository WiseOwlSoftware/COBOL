      *>--- conformance-case
      *> id: pb5924-keyword-required-refused-85
      *> rules: [1985-SR-IV.2.1.2-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.2.1.2 (cite.py: OK): "key words and are required"
      *> MOVE written without its key word TO: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. KW1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9.
       PROCEDURE DIVISION.
       P1.
           MOVE 1 X.
           DISPLAY X.
           STOP RUN.
