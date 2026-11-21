      *>--- conformance-case
      *> id: 1985-SR-IV_4_2_2_1_2-p3
      *> rules: [1985-SR-IV.4.2.2.1.2-p3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.1.2 p3 (COBOL Words). A refusal: the rule
      *> forbids line 7 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL7624 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-03.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-SR-IV.4.2.2.1.2-p3: the implementor-name CONSOLE used as
      * a computer-name (the three sets are disjoint).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P06DISJ.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. CONSOLE.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "RAN".
           STOP RUN.
