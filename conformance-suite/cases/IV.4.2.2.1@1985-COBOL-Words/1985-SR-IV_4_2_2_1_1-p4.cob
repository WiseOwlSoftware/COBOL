      *>--- conformance-case
      *> id: 1985-SR-IV_4_2_2_1_1-p4
      *> rules: [1985-SR-IV.4.2.2.1.1-p4, 1985-SR-IV.4.2.2.1.1-p5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.1.1 p4; §1985:IV.4.2.2.1.1 p5 (COBOL Words). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-02.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-02 probe: 1985-SR-IV.4.2.2.1.1-p4/p5 segment-numbers and
      * level-numbers belong to no disjoint set and need not be
      * unique: segment-number 60 on two sections, a section named 60,
      * a paragraph named 05 beside level-number 05
      * (§1985:IV.4.2.2.1.1 p5).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEGNUM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1.
           05  A1               PIC X VALUE "A".
           05  B1               PIC X VALUE "B".
       PROCEDURE DIVISION.
       60 SECTION.
       05.
           DISPLAY G1.
           PERFORM S1.
           PERFORM S2.
           STOP RUN.
       S1 SECTION 60.
       P1.
           DISPLAY "S1-60".
       S2 SECTION 60.
       P2.
           DISPLAY "S2-60".
