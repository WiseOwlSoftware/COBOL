      *>--- conformance-case
      *> id: 1985-SR-X_1_3_8_1-p1
      *> rules: [1985-SR-X.1.3.8.1-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.1.3.8.1 p1 (Scope of Names). A refusal: the rule
      *> forbids line 9 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1639 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-16.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. X1381O.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X PIC X(6).
       PROCEDURE DIVISION.
       P-1.
           CALL "X1381N".
           MOVE X1381N TO W-X.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. X1381N.
       PROCEDURE DIVISION.
       N-1.
           EXIT PROGRAM.
       END PROGRAM X1381N.
       END PROGRAM X1381O.
