      *>--- conformance-case
      *> id: 1985-SR-VI_6_1-p2
      *> rules: [1985-SR-VI.6.1-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.1 p2 (PROCEDURE DIVISION IN THE NUCLEUS MODULE). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-09.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-09 probe: Procedure Division format 1 of VI.6.1 (sections,
      * each holding paragraphs) run in order of appearance.
      * Expect: accepted, DISPLAYs S1P1 / S1P2 / S2P1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S09SEC.
       PROCEDURE DIVISION.
       S1 SECTION.
       S1P1.
           DISPLAY "S1P1".
       S1P2.
           DISPLAY "S1P2".
       S2 SECTION.
       S2P1.
           DISPLAY "S2P1".
           STOP RUN.
