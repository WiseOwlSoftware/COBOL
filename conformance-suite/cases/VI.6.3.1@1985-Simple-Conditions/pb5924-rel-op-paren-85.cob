      *>--- conformance-case
      *> id: pb5924-rel-op-paren-85
      *> rules: [1985-SR-VI.6.3.1.1-p4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VI.6.3.1.1 (cite.py: OK): "A space must
      *> precede and follow each reserved word comprising the relational
      *> operator"
      *> A parenthesis does not stand in for the space: GREATER followed
      *> by '(' with no space: refused. DEFECT today: it compiles.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5924P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N  PIC 9(3) VALUE 5.
       01  M  PIC 9(3) VALUE 2.
       PROCEDURE DIVISION.
       P0.
           IF N GREATER(M) DISPLAY "Y" ELSE DISPLAY "N".
           STOP RUN.
