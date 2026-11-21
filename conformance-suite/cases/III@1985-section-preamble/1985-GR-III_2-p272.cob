      *>--- conformance-case
      *> id: 1985-GR-III_2-p272
      *> rules: [1985-GR-III.2-p272, 1985-GR-III.2-p281]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p272; §1985:III.2 p281 ((section preamble)). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-05.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OUTER85.
      * 1985 III.2 Run Unit / Separately Compiled Program: one
      * separately compiled program and its contained program run as
      * one run unit and interact through a CALL parameter.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  V          PIC 9(4) VALUE 10.
       PROCEDURE DIVISION.
       P1.
           CALL "INNER85" USING V.
           DISPLAY "V=" V.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNER85.
       DATA DIVISION.
       LINKAGE SECTION.
       01  L          PIC 9(4).
       PROCEDURE DIVISION USING L.
       Q1.
           ADD 5 TO L.
           EXIT PROGRAM.
       END PROGRAM INNER85.
       END PROGRAM OUTER85.
