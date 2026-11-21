      *>--- conformance-case
      *> id: pb5924-level-number-refused-85
      *> rules: [1985-SR-IV.2.1.3-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.2.1.3 (cite.py: OK): "those specific level-numbers are required"
      *> A RENAMES entry written with level-number 01 instead of the
      *> 66 its format prints: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LV2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 A PIC XX.
          05 B PIC XX.
       01 R RENAMES A THRU B.
       PROCEDURE DIVISION.
       P1.
           MOVE "ABCD" TO G.
           DISPLAY G.
           STOP RUN.
