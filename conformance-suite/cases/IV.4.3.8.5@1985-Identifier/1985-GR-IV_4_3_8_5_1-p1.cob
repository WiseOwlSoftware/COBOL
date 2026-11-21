      *>--- conformance-case
      *> id: 1985-GR-IV_4_3_8_5_1-p1
      *> rules: [1985-GR-IV.4.3.8.5.1-p1, 1985-SR-IV.4.3.8-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.5.1 p1; §1985:IV.4.3.8 p1 (Identifier). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-05.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-SR-IV.4.3.8-p1, 1985-GR-IV.4.3.8.5.1-p1: a user-defined
      * name is made unique by qualification, subscripting and
      * reference modification (each form accepted, each resolves).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S05UNIQ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1.
           05  X       PIC X(4) VALUE "AAAA".
           05  T       PIC X(3) OCCURS 3 TIMES.
       01  G2.
           05  X       PIC X(4) VALUE "BBBB".
       77  I           PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       P1.
           MOVE "ONE" TO T OF G1 (1).
           MOVE "TWO" TO T (2).
           MOVE "SIX" TO T IN G1 (3).
           DISPLAY "Q1=" X OF G1.
           DISPLAY "Q2=" X IN G2.
           DISPLAY "S=" T (I).
           DISPLAY "R=" X OF G2 (2:2).
           DISPLAY "SR=" T OF G1 (3) (2:1).
           STOP RUN.
