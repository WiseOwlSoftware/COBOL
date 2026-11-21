      *>--- conformance-case
      *> id: 1985-SR-VI_6_3_1_1_3-p1
      *> rules: [1985-SR-VI.6.3.1.1.3-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.3.1.1.3 p1 (Relation Condition). A refusal: the rule
      *> forbids line 14 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2533 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-10.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S10I.
      * VI.6.3.1.1.3: index data item vs numeric literal is not a listed pair
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TB.
           05  TE   PIC 9 OCCURS 5 TIMES INDEXED BY IX IY.
       01  XD       USAGE INDEX.
       01  YD       USAGE INDEX.
       01  N3       PIC 9(3) VALUE 3.
       PROCEDURE DIVISION.
       P0.
           SET IX TO 3. SET IY TO 3. SET XD TO IX. SET YD TO IX.
           IF XD = 3 DISPLAY "T" ELSE DISPLAY "F".
           STOP RUN.
