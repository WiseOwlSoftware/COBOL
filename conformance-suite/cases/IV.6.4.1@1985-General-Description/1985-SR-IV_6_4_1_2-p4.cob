      *>--- conformance-case
      *> id: 1985-SR-IV_6_4_1_2-p4
      *> rules: [1985-SR-IV.6.4.1.2-p4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.1.2 p4 (General Description). A refusal: the rule
      *> forbids line 8 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL5506 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-99.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R01BAD.
      * 1985 IV.6.4.1.2 p4: a statement begins with a COBOL verb.
      * The sentence below begins with a literal: expect refusal.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "OK".
           "BAD" DISPLAY.
           STOP RUN.
