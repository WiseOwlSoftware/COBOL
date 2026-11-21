      *>--- conformance-case
      *> id: pb5989-relop-or-equal-word-refused-85
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5989. §1985:VI.6.3.1.1 (cite.py: OK): "A space must precede and follow each reserved word comprising the relational operator".
      *> Every word of a multi-word operator is judged: the last word of GREATER THAN OR
      *> EQUAL TO is a word of the relational operator, so a '(' after it breaks the rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB89RLO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N  PIC 9(3) VALUE 5.
       01  M  PIC 9(3) VALUE 2.
       PROCEDURE DIVISION.
       P0.
           IF N GREATER THAN OR EQUAL TO(M)
               DISPLAY "Y" ELSE DISPLAY "N".
           STOP RUN.
