      *>--- conformance-case
      *> id: 1985-SR-VI_1_3_4-p1
      *> rules: [1985-SR-VI.1.3.4-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.1.3.4 p1 (RESTRICTIONS ON OVERALL LANGUAGE). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-08.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-SR-VI.1.3.4-p1: one through seven subscripts in level 2.
      * Expect: 1 2 3 4 5 6 7
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUB7.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
        02 A1 OCCURS 2.
         03 B1 PIC 9.
         03 A2 OCCURS 2.
          04 B2 PIC 9.
          04 A3 OCCURS 2.
           05 B3 PIC 9.
           05 A4 OCCURS 2.
            06 B4 PIC 9.
            06 A5 OCCURS 2.
             07 B5 PIC 9.
             07 A6 OCCURS 2.
              08 B6 PIC 9.
              08 A7 OCCURS 2 PIC 9.
       01 J PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       P1.
           MOVE 1 TO B1 (2).
           MOVE 2 TO B2 (2, 1).
           MOVE 3 TO B3 (1, 2, J).
           MOVE 4 TO B4 (2, 2, 1, 2).
           MOVE 5 TO B5 (1, 1, 1, 1, 2).
           MOVE 6 TO B6 (2, 1, 2, 1, 2, 1).
           MOVE 7 TO A7 (J, J, J, J, J, J, J).
           DISPLAY B1 (2) " " B2 (2, 1) " " B3 (1, 2, 2) " "
                   B4 (2, 2, 1, 2) " " B5 (1, 1, 1, 1, 2) " "
                   B6 (2, 1, 2, 1, 2, 1) " " A7 (2, 2, 2, 2, 2, 2, 2).
           STOP RUN.
