      *>--- conformance-case
      *> id: pb5924-level-01-area-b-85
      *> rules: [1985-SR-IV.7.4-p8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.7.4 (cite.py: OK):
      *> "level-number 01 or 77 must begin in area A"
      *> A level-01 entry beginning in area B is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LV1B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
           01 G PIC X VALUE "A".
       PROCEDURE DIVISION.
       P1.
           DISPLAY G.
           STOP RUN.
