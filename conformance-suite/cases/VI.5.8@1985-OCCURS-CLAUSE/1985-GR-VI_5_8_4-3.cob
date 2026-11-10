      *>--- conformance-case
      *> id: 1985-GR-VI_5_8_4-3
      *> rules: [1985-GR-VI.5.8.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.8.4 GR3 (THE OCCURS CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ODOGRP.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  K               PIC 9 VALUE 2.
       77  W1              PIC X(6).
       77  W2              PIC X(5).
       01  G.
           05  N           PIC 9.
           05  T           PIC X OCCURS 1 TO 5 TIMES
                           DEPENDING ON N.
       01  H.
           05  U           PIC X OCCURS 1 TO 5 TIMES
                           DEPENDING ON K.
       PROCEDURE DIVISION.
       P1.
           MOVE 2 TO N.
           MOVE "3ABCDE" TO G.
           MOVE G TO W1.
           DISPLAY "[" W1 "]".
           MOVE "VWXYZ" TO H.
           MOVE H TO W2.
           DISPLAY "[" W2 "]".
           STOP RUN.
