      *>--- conformance-case
      *> id: 1985-GR-IV_4_3_2_1-p5
      *> rules: [1985-GR-IV.4.3.2.1-p5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.2.1 p5 (Concept of Levels). A refusal: the rule
      *> forbids line 8 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2771 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-04.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-GR-IV.4.3.2.1-p5 REFUSAL: a level-77 item is not itself
      * subdivided.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L77SUB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  W77.
           05  W77-A         PIC X VALUE "A".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY W77.
           STOP RUN.
