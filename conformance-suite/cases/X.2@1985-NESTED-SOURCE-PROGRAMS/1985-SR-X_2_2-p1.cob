      *>--- conformance-case
      *> id: 1985-SR-X_2_2-p1
      *> rules: [1985-SR-X.2.2-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.2.2 p1 (NESTED SOURCE PROGRAMS). A refusal: the rule
      *> forbids line 6 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0001 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-16.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. X22.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC XX VALUE "OK".
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X.
       PROCEDURE DIVISION.
       P-1.
           DISPLAY "W=" W.
           STOP RUN.
