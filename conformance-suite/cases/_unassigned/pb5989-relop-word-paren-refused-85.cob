      *>--- conformance-case
      *> id: pb5989-relop-word-paren-refused-85
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5989. §1985:VI.6.3.1.1 (cite.py: OK): "A space must precede and follow each reserved word comprising the relational operator".
      *> A parenthesis does not stand in for the space: GREATER followed by '(' is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB89RLW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N  PIC 9(3) VALUE 5.
       01  M  PIC 9(3) VALUE 2.
       PROCEDURE DIVISION.
       P0.
           IF N GREATER(M) DISPLAY "Y" ELSE DISPLAY "N".
           STOP RUN.
