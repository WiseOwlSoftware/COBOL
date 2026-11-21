      *>--- conformance-case
      *> id: pb5924-concatenation-refused-85
      *> rules: [1985-SR-IV.4.2-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.4.2 (cite.py: OK): "A character-string may only be concatenated"
      *> A nonnumeric literal concatenated with the word X and no
      *> separator between: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CC1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X VALUE "Z".
       PROCEDURE DIVISION.
       P1.
           DISPLAY "AB"X.
           STOP RUN.
