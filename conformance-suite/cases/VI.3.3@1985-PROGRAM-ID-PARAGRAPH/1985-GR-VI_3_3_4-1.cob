      *>--- conformance-case
      *> id: 1985-GR-VI_3_3_4-1
      *> rules: [1985-GR-VI.3.3.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.3.3.4 GR1 (THE PROGRAM-ID PARAGRAPH). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PGMID1.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "PGMID1 RAN".
           CALL "PGMID3".
           CALL "PGMID2".
           STOP RUN.
       END PROGRAM PGMID1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PGMID2.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "PGMID2 RAN".
           EXIT PROGRAM.
       END PROGRAM PGMID2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PGMID3.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "PGMID3 RAN".
           EXIT PROGRAM.
       END PROGRAM PGMID3.
