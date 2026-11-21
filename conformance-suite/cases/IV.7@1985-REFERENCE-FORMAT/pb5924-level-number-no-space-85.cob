      *>--- conformance-case
      *> id: pb5924-level-number-no-space-85
      *> rules: [1985-SR-IV.7.4-p6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.7.4 (cite.py: OK):
      *> "At least one space must separate a level-number"
      *> 05X (no space after the level-number) is no data description entry.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LVNS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
           05X PIC X VALUE "N".
       PROCEDURE DIVISION.
       P1.
           DISPLAY G.
           STOP RUN.
