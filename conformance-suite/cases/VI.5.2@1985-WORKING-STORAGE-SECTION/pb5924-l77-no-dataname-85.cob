      *>--- conformance-case
      *> id: pb5924-l77-no-dataname-85
      *> rules: [1985-SR-VI.5.2.1-p2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VI.5.2.1 (cite.py: OK): "The following
      *> data clauses are required in each data description entry"
      *> Clause 2 of the list is data-name: a level-77 entry that omits
      *> its data-name is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB59243.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       P0.
           DISPLAY "X".
           STOP RUN.
