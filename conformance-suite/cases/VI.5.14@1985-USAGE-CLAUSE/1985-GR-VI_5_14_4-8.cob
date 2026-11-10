      *>--- conformance-case
      *> id: 1985-GR-VI_5_14_4-8
      *> rules: [1985-GR-VI.5.14.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.14.4 GR8 (THE USAGE CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GR8TEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T.
           05  E PIC X OCCURS 5 TIMES INDEXED BY IX.
       01  G1.
           05  ID1 USAGE IS INDEX.
           05  F1 PIC X VALUE "A".
       01  G2.
           05  ID2 USAGE IS INDEX.
           05  F2 PIC X VALUE "B".
       01  N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           SET IX TO 3.
           SET ID1 TO IX.
           MOVE G1 TO G2.
           SET IX TO 1.
           SET IX TO ID2.
           SET N TO IX.
           DISPLAY N.
           DISPLAY F2.
           STOP RUN.
