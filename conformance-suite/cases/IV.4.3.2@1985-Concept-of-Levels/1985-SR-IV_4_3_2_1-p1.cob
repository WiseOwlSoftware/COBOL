      *>--- conformance-case
      *> id: 1985-SR-IV_4_3_2_1-p1
      *> rules: [1985-SR-IV.4.3.2.1-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.2.1 p1 (Concept of Levels). A refusal: the rule
      *> forbids line 8 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1746 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-04.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-SR-IV.4.3.2.1-p1 REFUSAL: level-numbers of less inclusive
      * items are not greater than 49 (66, 77, 88 excepted).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LVL50.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           50  G1            PIC X VALUE "A".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY G.
           STOP RUN.
