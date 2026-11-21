      *>--- conformance-case
      *> id: pb5924-level-numbers-not-identical-85
      *> rules: [1985-SR-IV.4.3.2.1-p2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.4.3.2.1 (cite.py: OK): "must be described using identical level-numbers"
      *> Two items immediately subordinate to one group carry level
      *> numbers 05 and 03: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LVLNID.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05  A             PIC X VALUE "A".
          03  B              PIC X VALUE "B".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY G.
           STOP RUN.
