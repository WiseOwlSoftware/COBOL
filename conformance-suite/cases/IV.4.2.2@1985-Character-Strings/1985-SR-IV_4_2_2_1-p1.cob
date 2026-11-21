      *>--- conformance-case
      *> id: 1985-SR-IV_4_2_2_1-p1
      *> rules: [1985-SR-IV.4.2.2.1-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.1 p1 (Character-Strings). A refusal: the rule
      *> forbids line 8 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1567 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-02.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-02 probe: 1985-SR-IV.4.2.2.1-p1 a COBOL word of 31 characters
      * exceeds "not more than 30 characters" (§1985:IV.4.2.2.1 p1).
      * CONTROL: w30_legal.cob, the same name one character shorter.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W31REF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A23456789-123456789-123456789AB PIC X(2) VALUE "OK".
       PROCEDURE DIVISION.
       P0.
           DISPLAY A23456789-123456789-123456789AB.
           STOP RUN.
