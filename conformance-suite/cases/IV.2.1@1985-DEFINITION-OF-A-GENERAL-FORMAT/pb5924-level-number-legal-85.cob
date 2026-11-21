      *>--- conformance-case
      *> id: pb5924-level-number-legal-85
      *> rules: [1985-SR-IV.2.1.3-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.2.1.3 (cite.py: OK): "those specific level-numbers are required"
      *> A RENAMES entry with the level-number 66 its format prints:
      *> accepted; R spans A through B.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LV1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 A PIC XX.
          05 B PIC XX.
       66 R RENAMES A THRU B.
       PROCEDURE DIVISION.
       P1.
           MOVE "ABCD" TO G.
           DISPLAY R.
           STOP RUN.
