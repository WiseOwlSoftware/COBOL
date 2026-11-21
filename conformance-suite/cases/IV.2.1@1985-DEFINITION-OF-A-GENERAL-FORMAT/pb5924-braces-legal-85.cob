      *>--- conformance-case
      *> id: pb5924-braces-legal-85
      *> rules: [1985-SR-IV.2.1.4-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.2.1.4 (cite.py: OK): "must be either explicitly specified"
      *> INSPECT REPLACING with ALL chosen from the braces: ABA
      *> becomes BBB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. BR1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC XXX VALUE "ABA".
       PROCEDURE DIVISION.
       P1.
           INSPECT X REPLACING ALL "A" BY "B".
           DISPLAY X.
           STOP RUN.
