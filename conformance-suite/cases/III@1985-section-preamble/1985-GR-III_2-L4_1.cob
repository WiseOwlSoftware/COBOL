      *>--- conformance-case
      *> id: 1985-GR-III_2-L4_1
      *> rules: [1985-GR-III.2-L4.1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 GRL4.1 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-99.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PICPAREN.
      * 1985 III.2 Text Word (1): parentheses are text words
      * REGARDLESS OF CONTEXT, so inside PICTURE X(6) the 6 is a
      * text word that ==6== BY ==3== replaces: W-A is PIC X(3).
       REPLACE ==6== BY ==3==.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-A          PIC X(6) VALUE "ABC".
       01  W-B          PIC X(4) VALUE "WXYZ".
       PROCEDURE DIVISION.
       P1.
           MOVE "PQRSTU" TO W-A.
           DISPLAY "[" W-A "]".
           IF W-A = "PQR" DISPLAY "PICPAREN PASS"
               ELSE DISPLAY "PICPAREN FAIL".
           STOP RUN.
