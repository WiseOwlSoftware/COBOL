      *>--- conformance-case
      *> id: pb5989-logop-not-before-paren-refused-85
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5989. §1985:VI.6.3.2 (cite.py: OK): "The logical operators must be preceded by a space and followed by a space".
      *> NOT followed by '(' with no space: refused at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB89LGN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A  PIC 9 VALUE 1.
       01  B  PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       P0.
           IF NOT(A = 2) DISPLAY "NOT-A2".
           STOP RUN.
