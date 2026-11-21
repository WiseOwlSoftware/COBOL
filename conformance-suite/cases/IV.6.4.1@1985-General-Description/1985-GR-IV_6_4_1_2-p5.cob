      *>--- conformance-case
      *> id: 1985-GR-IV_6_4_1_2-p5
      *> rules: [1985-GR-IV.6.4.1.2-p5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.1.2 p5 (General Description). A refusal: the rule
      *> forbids line 13 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1639 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-99.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R02BAD.
      * 1985 IV.6.4.1.2 p5: an identifier is the words necessary
      * to make unique reference. X alone is not unique: refuse.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          02 X PIC X VALUE "1".
       01 G2.
          02 X PIC X VALUE "2".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY X.
           STOP RUN.
