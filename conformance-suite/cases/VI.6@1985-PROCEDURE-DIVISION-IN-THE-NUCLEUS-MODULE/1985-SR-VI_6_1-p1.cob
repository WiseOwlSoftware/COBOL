      *>--- conformance-case
      *> id: 1985-SR-VI_6_1-p1
      *> rules: [1985-SR-VI.6.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.1 p1 (PROCEDURE DIVISION IN THE NUCLEUS MODULE). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-09.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-09 probe: no Procedure Division (VI.6.1: "The Procedure
      * Division is optional"). Expect: accepted and runs with no
      * output.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S09NOP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  A PIC 9 VALUE 1.
