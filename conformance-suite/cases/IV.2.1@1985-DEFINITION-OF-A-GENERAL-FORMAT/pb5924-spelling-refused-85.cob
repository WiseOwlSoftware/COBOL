      *>--- conformance-case
      *> id: pb5924-spelling-refused-85
      *> rules: [1985-SR-IV.2.1.2-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.2.1.2 (cite.py: OK): "must be spelled correctly"
      *> The optional (not underlined) word THAN misspelled THN in a
      *> relation condition: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. KW2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       P1.
           IF X GREATER THN 0 DISPLAY "Y".
           STOP RUN.
