      *>--- conformance-case
      *> id: 1985-GR-IV_4_4_2-L2_5
      *> rules: [1985-GR-IV.4.4.2-L2.5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.4.2 GRL2.5 (EXPLICIT AND IMPLICIT SPECIFICATIONS).
      *> An output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINP.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "BEFORE CALL".
           CALL "SUBP".
           DISPLAY "AFTER CALL".
           STOP RUN.
       END PROGRAM MAINP.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBP.
       PROCEDURE DIVISION.
       S1.
           DISPLAY "IN SUB".
       END PROGRAM SUBP.
