      *>--- conformance-case
      *> id: 1985-SR-VI_1_3_1-p1
      *> rules: [1985-SR-VI.1.3.1-p1, 1985-SR-VI.1.3.6-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.1.3.1 p1; §1985:VI.1.3.6 p1 (RESTRICTIONS ON OVERALL
      *> LANGUAGE). An output test: the program must translate and run
      *> at 85 and DISPLAY exactly the lines its .out holds, the outcome
      *> the rule requires. Placed from written and run by the PB5453
      *> reader of lineage-reads-1985/s-08.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * 1985-SR-VI.1.3.6-p1 / SR-VI.1.3.1-p1: reference modification
      * and the colon are permitted in level 2. Expect: BCD|E|XY
       IDENTIFICATION DIVISION.
       PROGRAM-ID. REFMOD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC X(5) VALUE "ABCDE".
       01 N PIC 9 VALUE 3.
       01 R PIC X(3) VALUE "AAA".
       PROCEDURE DIVISION.
       P1.
           MOVE "XY" TO R (2:).
           DISPLAY S (2:N) "|" S (5:1) "|" R (2:2).
           STOP RUN.
