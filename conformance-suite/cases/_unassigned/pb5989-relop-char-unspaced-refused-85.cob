      *>--- conformance-case
      *> id: pb5989-relop-char-unspaced-refused-85
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5989. §1985:VI.6.3.1.1 (cite.py: OK): "A space must precede and follow each reserved word comprising the relational operator".
      *> '=' is a relation character, a special character word and so a reserved word
      *> (§1985:IV.4.2.2.1.3.1): N=M has no space around it and is refused at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB89RLC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N  PIC 9(3) VALUE 5.
       01  M  PIC 9(3) VALUE 2.
       PROCEDURE DIVISION.
       P0.
           IF N=M DISPLAY "Y" ELSE DISPLAY "N".
           STOP RUN.
