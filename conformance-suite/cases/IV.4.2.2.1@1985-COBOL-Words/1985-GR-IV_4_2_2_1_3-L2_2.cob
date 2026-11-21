      *>--- conformance-case
      *> id: 1985-GR-IV_4_2_2_1_3-L2_2
      *> rules: [1985-GR-IV.4.2.2.1.3-L2.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.1.3 GRL2.2 (COBOL Words). A refusal: the rule
      *> forbids line 9 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0901 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-07.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985 IV.4.2.2.1.3 list item 2: an optional word is a
      * reserved word, so THAN as a data-name must be refused.
      * Control: rw-control.cob (THAN-X).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWOPT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  MOVE-X           PIC X VALUE "A".
       01  THAN             PIC X VALUE "B".
       01  LINAGE-COUNTER-X PIC X VALUE "C".
       PROCEDURE DIVISION.
       P1.
           DISPLAY "RWCTL " MOVE-X LINAGE-COUNTER-X.
           STOP RUN.
