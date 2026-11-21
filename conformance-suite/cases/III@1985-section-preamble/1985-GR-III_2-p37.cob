      *>--- conformance-case
      *> id: 1985-GR-III_2-p37
      *> rules: [1985-GR-III.2-p37, 1985-GR-III.2-p39]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p37; §1985:III.2 p39 ((section preamble)). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-01.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-GR-III.2-p37, p39: a Communication Section of two
      * communication description entries, each the level indicator
      * CD, a cd-name and its clauses. Expect: CD2 OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P39CD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W   PIC X.
       COMMUNICATION SECTION.
       CD  CIN FOR INPUT
           SYMBOLIC QUEUE IS Q1
           MESSAGE COUNT IS MC.
       01  CIN-REC.
           02 FILLER PIC X(87).
       CD  COUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK.
       PROCEDURE DIVISION.
       P0.
           MOVE 1 TO DC.
           MOVE 5 TO TL.
           IF DC = 1 AND TL = 5
               DISPLAY "CD2 OK"
           ELSE
               DISPLAY "CD2 BAD"
           END-IF.
           STOP RUN.
