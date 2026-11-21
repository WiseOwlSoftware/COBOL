      *>--- conformance-case
      *> id: 1985-GR-II_6_5_1-1
      *> rules: [1985-GR-II.6.5.1-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:II.6.5.1 GR1 (INTRA-PROGRAM COMMUNICATION). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-99.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R01ALTER.
      * 1985 II.6.5.1 (1): an ALTERed format 1 GO TO transfers
      * control (VI.6.15.4 GR1, GR2; VI.6.7).
       PROCEDURE DIVISION.
       P0.
           ALTER SW TO PROCEED TO P2.
           GO TO SW.
       SW.
           GO TO.
       P1.
           DISPLAY "ALTER-NOT-FOLLOWED".
           STOP RUN.
       P2.
           DISPLAY "ALTER-OK".
           STOP RUN.
