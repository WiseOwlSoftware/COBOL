      *>--- conformance-case
      *> id: pb5924-word-underscore-refused-85
      *> rules: [1985-SR-IV.4.2.2.1-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.4.2.2.1 (cite.py: OK): "selected from the set of letters"
      *> A data-name with an underscore, a character outside letters,
      *> digits and the hyphen: refused at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. WD3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W_X PIC 9 VALUE 7.
       PROCEDURE DIVISION.
       P1.
           DISPLAY W_X.
           STOP RUN.
