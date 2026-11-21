      *>--- conformance-case
      *> id: 1985-GR-IV_6_3_2_1-p5
      *> rules: [1985-GR-IV.6.3.2.1-p5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.3.2.1 p5 (Physical and Logical Aspects of Data
      *> Description). An output test: the program must translate and
      *> run at 85 and DISPLAY exactly the lines its .out holds, the
      *> outcome the rule requires. Placed from written and run by the
      *> PB5453 reader of lineage-reads-1985/s-06.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * 1985-GR-IV.6.3.2.1-p5: a Communication Section with an input CD
      * (the interface to the MCS). Expect: COMM OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. COMMS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W   PIC X.
       COMMUNICATION SECTION.
       CD  CIN FOR INPUT
           SYMBOLIC QUEUE IS Q1
           MESSAGE COUNT IS MC.
       01  CIN-REC.
           02 FILLER PIC X(87).
       PROCEDURE DIVISION.
       P0.
           DISPLAY "COMM OK".
           STOP RUN.
