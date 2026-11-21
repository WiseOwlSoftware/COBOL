      *>--- conformance-case
      *> id: 1985-GR-VI_5_7_1-p1
      *> rules: [1985-GR-VI.5.7.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.7.1 p1 (LEVEL-NUMBER). An output test: the program
      *> must translate and run at 85 and DISPLAY exactly the lines its
      *> .out holds, the outcome the rule requires. Placed from written
      *> and run by the PB5453 reader of lineage-reads-1985/s-09.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * s-09 probe: level-number 77 identifying a linkage item
      * (VI.5.7.1) in a contained program called BY REFERENCE.
      * Expect: accepted, DISPLAYs 08.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S09LNK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  N PIC 99 VALUE 7.
       PROCEDURE DIVISION.
       P0.
           CALL "S09SUB" USING N.
           DISPLAY N.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S09SUB.
       DATA DIVISION.
       LINKAGE SECTION.
       77  L PIC 99.
       PROCEDURE DIVISION USING L.
       P1.
           ADD 1 TO L.
           EXIT PROGRAM.
       END PROGRAM S09SUB.
       END PROGRAM S09LNK.
