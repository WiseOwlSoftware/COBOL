      *>--- conformance-case
      *> id: 1985-SR-IV_4_2_2_1_1-p1
      *> rules: [1985-SR-IV.4.2.2.1.1-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.1.1 p1 (COBOL Words). A refusal: the rule
      *> forbids line 8 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2773 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-02.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-02 probe: 1985-SR-IV.4.2.2.1-p1 "the hyphen may not appear
      * as the first ... character": -AB as a data-name is refused.
      * CONTROL: w30_legal.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LEADHY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  -AB                  PIC X VALUE "A".
       PROCEDURE DIVISION.
       P0.
           DISPLAY "NO".
           STOP RUN.
