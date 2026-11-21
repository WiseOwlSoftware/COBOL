      *>--- conformance-case
      *> id: pb5989-logop-spaced-runs-85
      *> rules: []
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5989. §1985:VI.6.3.2 (cite.py: OK): "The logical operators must be preceded by a space and followed by a space".
      *> AND, OR and NOT each set off by spaces, next to parentheses, compile and
      *> evaluate (A = 1, B = 2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB89LGS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A  PIC 9 VALUE 1.
       01  B  PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       P0.
           IF (A = 1) AND (B = 2) DISPLAY "BOTH".
           IF A = 1 AND NOT (B = 3) DISPLAY "NOT-B3".
           IF (A = 2) OR (B = 2) DISPLAY "EITHER".
           IF A = 1 AND (NOT (B = 3)) DISPLAY "PAREN-NOT".
           STOP RUN.
