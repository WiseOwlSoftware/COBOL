      *>--- conformance-case
      *> id: 1989-AR-A_2_2-p4
      *> rules: [1989-AR-A.2.2-p4, 1989-AR-A.2.2-p5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.2 p4; §1989:A.2.2 p5 (THE EVOLUTION OF CODASYL
      *> COBOL). An output test: the program must translate and run at
      *> 85 and DISPLAY exactly the lines its .out holds, the outcome
      *> the rule requires. Placed from written and run by the PB5453
      *> reader of lineage-reads-1985/s-19.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * s-19 p20: ALL subscripts (1989 A.2.2 p4, p5). A table named
      * with qualifiers and ALL; two ALL subscripts expand rightmost
      * first (ORD-MAX finds 6 at expanded position 4, row 2 col 1;
      * ORD-MIN finds 0 at position 3); ALL beside a literal
      * subscript; an ALL whose table has OCCURS DEPENDING ON uses
      * the object's current value. Each line prints OK or BAD.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S19P20.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1.
           05  T  PIC 9 OCCURS 4 TIMES.
       01  G2.
           05  T  PIC 9 OCCURS 4 TIMES.
       01  M.
           05  ROW OCCURS 2 TIMES.
               10  C PIC 9 OCCURS 3 TIMES.
       01  N   PIC 9 VALUE 3.
       01  V.
           05  E  PIC 9 OCCURS 1 TO 5 TIMES DEPENDING ON N.
       01  R   PIC S9(4).
       PROCEDURE DIVISION.
       P.
           MOVE "1928" TO G1.
           MOVE "5555" TO G2.
           MOVE "130612" TO M.
           MOVE 5 TO N.
           MOVE "12399" TO V.
           MOVE 3 TO N.
           COMPUTE R = FUNCTION MAX (T OF G1 (ALL)).
           IF R = 9 DISPLAY "OK  QUALIFIED ALL" ELSE DISPLAY "BAD " R.
           COMPUTE R = FUNCTION SUM (T IN G1 (ALL)).
           IF R = 20 DISPLAY "OK  SUM ALL" ELSE DISPLAY "BAD " R.
           COMPUTE R = FUNCTION ORD-MAX (C (ALL, ALL)).
           IF R = 4 DISPLAY "OK  ORDER MAX" ELSE DISPLAY "BAD " R.
           COMPUTE R = FUNCTION ORD-MIN (C (ALL, ALL)).
           IF R = 3 DISPLAY "OK  ORDER MIN" ELSE DISPLAY "BAD " R.
           COMPUTE R = FUNCTION SUM (C (ALL, 2)).
           IF R = 4 DISPLAY "OK  ALL ROW COL 2" ELSE DISPLAY "BAD " R.
           COMPUTE R = FUNCTION SUM (C (2, ALL)).
           IF R = 9 DISPLAY "OK  ROW 2 ALL" ELSE DISPLAY "BAD " R.
           COMPUTE R = FUNCTION SUM (E (ALL)).
           IF R = 6 DISPLAY "OK  ODO RANGE" ELSE DISPLAY "BAD " R.
           MOVE 5 TO N.
           COMPUTE R = FUNCTION SUM (E (ALL)).
           IF R = 24 DISPLAY "OK  ODO RANGE 5" ELSE DISPLAY "BAD " R.
           COMPUTE R = FUNCTION MAX (T OF G1 (ALL), T OF G2 (2), 0).
           IF R = 9 DISPLAY "OK  ALL AMONG ARGS" ELSE DISPLAY "BAD " R.
           STOP RUN.
