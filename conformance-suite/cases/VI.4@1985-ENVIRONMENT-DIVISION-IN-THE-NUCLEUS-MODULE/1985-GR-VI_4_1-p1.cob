      *>--- conformance-case
      *> id: 1985-GR-VI_4_1-p1
      *> rules: [1985-GR-VI.4.1-p1, 1985-GR-VI.5.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.4.1 p1; §1985:VI.5.1 p1 (ENVIRONMENT DIVISION IN THE
      *> NUCLEUS MODULE). An output test: the program must translate and
      *> run at 85 and DISPLAY exactly the lines its .out holds, the
      *> outcome the rule requires. Placed from written and run by the
      *> PB5453 reader of lineage-reads-1985/s-09.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * s-09 probe: no Environment and no Data Division (VI.4.1, VI.5.1:
      * each optional). Expect: accepted, DISPLAYs MIN-OK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S09MIN.
       PROCEDURE DIVISION.
       P0.
           DISPLAY "MIN-OK".
           STOP RUN.
