      *>--- conformance-case
      *> id: 1985-SR-VI_6_3_1_2-p9
      *> rules: [1985-SR-VI.6.3.1.2-p9]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.3.1.2 p9 (Simple Conditions). A refusal: the rule
      *> forbids line 14 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6916 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-10.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S10Y.
      * VI.6.3.1.2 p9: class-name test not on numeric
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CLASS HEXD IS "0" THRU "9" "A" THRU "F".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  U3       PIC 9(3) VALUE 123.
       01  X3       PIC X(3) VALUE "123".
       PROCEDURE DIVISION.
       P0.
           IF U3 IS HEXD DISPLAY "T" ELSE DISPLAY "F".
           STOP RUN.
