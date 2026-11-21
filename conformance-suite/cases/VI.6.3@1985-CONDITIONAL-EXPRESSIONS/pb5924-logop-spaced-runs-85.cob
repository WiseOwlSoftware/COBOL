      *>--- conformance-case
      *> id: pb5924-logop-spaced-runs-85
      *> rules: [1985-SR-VI.6.3.2-p3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VI.6.3.2 (cite.py: OK): "The logical operators must be preceded by a space and followed by a space"
      *> AND, OR and NOT each set off by spaces, next to parentheses, compile
      *> and evaluate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LOGOPSP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC 9 VALUE 1.
       01  B PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       P1.
           IF (A = 1) AND (B = 2)
               DISPLAY "BOTH"
           END-IF.
           IF A = 1 AND NOT (B = 3)
               DISPLAY "NOT-B3"
           END-IF.
           IF (A = 2) OR (B = 2)
               DISPLAY "EITHER"
           END-IF.
           STOP RUN.
