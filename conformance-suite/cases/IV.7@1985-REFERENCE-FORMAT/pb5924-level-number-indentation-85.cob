      *>--- conformance-case
      *> id: pb5924-level-number-indentation-85
      *> rules: [1985-SR-IV.7.4-p8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.7.4 (cite.py: OK):
      *> "may begin any number of positions to the right of margin A"
      *> Level 02 entries in area A and indented into area B, 01 and 77 in
      *> area A, all compile; the record and the 77 item display ABCDE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LVIL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
       02 G1 PIC X VALUE "A".
         02 G2 PIC X VALUE "B".
                             02 G3 PIC X VALUE "C".
                                                 02 G4 PIC X VALUE "D".
       77 S PIC X VALUE "E".
       PROCEDURE DIVISION.
       P1.
           DISPLAY G S.
           STOP RUN.
