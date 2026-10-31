*>--- conformance-case
*> id: pb4162_table_all_argument_held_for_order
*> rules: [GR-8.4.3.2.4-2, AR-15.3-14, DOC-A.1-92]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4162 - ISO §8.4.3.2.4 GR2: "At the time reference is made to a function, its arguments are evaluated
      *> individually in the order specified in the list of arguments, from left to right." (cite.py: OK 8.4.3.2.4 2)).
      *> §15.3 14): "When ALL is specified as a subscript, the effect is as if each table element associated with that
      *> subscript position were specified. The order of the implicit specification of each occurrence is from left to right,
      *> with ..." (cite.py: OK 15.3 14)): T (ALL) is a SEQUENCE of arguments, each evaluated where it stands in the list, so
      *> the elements are read BEFORE a later argument's user-defined function activation changes them.
      *> The activation (PB4162B) adds 100 to every element of the tables below, sets the OCCURS DEPENDING object to 1 and
      *> overwrites the text table; every call below names a table(ALL) argument BEFORE that activation, so each answer is
      *> computed over the values the tables held when the call was reached. Before the fix the ALL operand was enumerated where
      *> the call renders, after the activation: the MAX answered 121.5, the SUM 334, and the ODO table was read at count 1.
      *> After every call "F1" shows the first element of F, 121.5 = 21.5 + 100, which proves the activation ran and changed it.
      *> Data (reloaded before each call): F = 21.5 5.0 7.5 (PIC 9(4)V9); G = 1 2 3 / 4 5 6 (2 rows of 3, PIC S9(3));
      *> OD = 10 20 30 under OCCURS 1 TO 5 DEPENDING ON CNT (CNT = 3); S = AA BB CC (PIC X(2)).
      *> The filler argument is the activation's result, which equals its argument: 0 (LO) or 99999999 (HI).
      *>   MAX   MAX (F (ALL), 0)                        = 21.5
      *>   MIN   MIN (F (ALL), 99999999)                 = 5.0
      *>   SUM   SUM (F (ALL), 0) = 21.5 + 5.0 + 7.5     = 34.0
      *>   MEAN  MEAN (F (ALL), 0) = 34.0 / 4 = 8.5 - "the arithmetic mean (average) of its arguments" (§15.60.1, cite.py: OK
      *>         15.60.1), the activation's 0 being the fourth argument
      *>   MEDIAN MEDIAN (F (ALL), 0): 0 5.0 7.5 21.5, an even count, "the arithmetic mean of the two middle values"
      *>         (§15.61.4 2), cite.py: OK 15.61.4 2)) = 6.25
      *>   G-SUM  SUM (G (ALL, ALL), 0) = 21;  G-ROW SUM (G (2, ALL), 0) = 4 + 5 + 6 = 15;  G-COL SUM (G (ALL, 1), 0) = 1 + 4 = 5
      *>   G-OMAX ORD-MAX (G (ALL, ALL), 0) = 6 - "the ordinal number that corresponds to the position of the argument-1 having the
      *>         greatest value" (§15.71.4 1), cite.py: OK 15.71.4 1)): the sixth implicit specification, the rightmost ALL varying fastest
      *>   G-OMIN ORD-MIN (G (ALL, ALL), 0) = 7 - the least value is the activation's 0, the seventh argument, after the six
      *>         elements (§15.72.4 1), cite.py: OK 15.72.4 1))
      *>   ODO    SUM (OD (ALL), 0) = 10 + 20 + 30 = 60 - the range of the ALL subscript is CNT's value when the argument is
      *>          evaluated (§15.3 14)), 3; read after the activation (CNT = 1) it would be 110
      *>   TEXT   CONCAT (S (ALL), PB4162A) = AABBCC followed by the activation's QQ
      *>   NEST   MAX (MAX (F (ALL), 1), 0) = 21.5, the nested call naming ALL held as the call it is (the argument to its left)
      *>   NESTOD MIN (MAX (OD (ALL), 1), 99999999) = 30
      *> And the held call answers what the unheld one does: FUNCTION LENGTH of the nested MAX measured with a literal filler
      *> (nothing to hold) and with the activation as filler is the SAME length (the temporary's description is the
      *> implementor's, §15.4.1 and docs/CONFORMANCE.md DOC-A.1-92, so no length is asserted, only that holding changes none).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB4162B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-T EXTERNAL.
          05 WS-F PIC 9(4)V9 OCCURS 3.
          05 WS-G OCCURS 2.
             10 WS-GC PIC S9(3) OCCURS 3.
          05 WS-CNT PIC 9.
          05 WS-S PIC X(2) OCCURS 3.
          05 WS-OD PIC 9(3) OCCURS 1 TO 5 DEPENDING ON WS-CNT.
       LINKAGE SECTION.
       01 L-X PIC 9(8).
       01 L-R PIC 9(8).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           ADD 100 TO WS-F (1) WS-F (2) WS-F (3).
           ADD 100 TO WS-GC (1, 1) WS-GC (1, 2) WS-GC (1, 3).
           ADD 100 TO WS-GC (2, 1) WS-GC (2, 2) WS-GC (2, 3).
           ADD 100 TO WS-OD (1) WS-OD (2) WS-OD (3).
           MOVE 1 TO WS-CNT.
           MOVE L-X TO L-R.
           GOBACK.
       END FUNCTION PB4162B.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB4162A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-T EXTERNAL.
          05 WS-F PIC 9(4)V9 OCCURS 3.
          05 WS-G OCCURS 2.
             10 WS-GC PIC S9(3) OCCURS 3.
          05 WS-CNT PIC 9.
          05 WS-S PIC X(2) OCCURS 3.
          05 WS-OD PIC 9(3) OCCURS 1 TO 5 DEPENDING ON WS-CNT.
       LINKAGE SECTION.
       01 L-R PIC X(2).
       PROCEDURE DIVISION RETURNING L-R.
       P.
           MOVE "ZZ" TO WS-S (1) WS-S (2) WS-S (3).
           MOVE "QQ" TO L-R.
           GOBACK.
       END FUNCTION PB4162A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4162.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB4162B
           FUNCTION PB4162A
           FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-T EXTERNAL.
          05 WS-F PIC 9(4)V9 OCCURS 3.
          05 WS-G OCCURS 2.
             10 WS-GC PIC S9(3) OCCURS 3.
          05 WS-CNT PIC 9.
          05 WS-S PIC X(2) OCCURS 3.
          05 WS-OD PIC 9(3) OCCURS 1 TO 5 DEPENDING ON WS-CNT.
       01 WS-LO PIC 9(8) VALUE 0.
       01 WS-HI PIC 9(8) VALUE 99999999.
       01 WS-R  PIC S9(5)V99.
       01 WS-I  PIC 9(4).
       01 WS-U  PIC 9(4).
       01 WS-H  PIC 9(4).
       01 WS-X  PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           PERFORM LOAD-DATA
           COMPUTE WS-R = FUNCTION MAX (WS-F (ALL), FUNCTION PB4162B (WS-LO))
           DISPLAY "MAX=" WS-R " F1=" WS-F (1)
           PERFORM LOAD-DATA
           COMPUTE WS-R = FUNCTION MIN (WS-F (ALL), FUNCTION PB4162B (WS-HI))
           DISPLAY "MIN=" WS-R " F1=" WS-F (1)
           PERFORM LOAD-DATA
           COMPUTE WS-R = FUNCTION SUM (WS-F (ALL), FUNCTION PB4162B (WS-LO))
           DISPLAY "SUM=" WS-R " F1=" WS-F (1)
           PERFORM LOAD-DATA
           COMPUTE WS-R = FUNCTION MEAN (WS-F (ALL), FUNCTION PB4162B (WS-LO))
           DISPLAY "MEAN=" WS-R " F1=" WS-F (1)
           PERFORM LOAD-DATA
           COMPUTE WS-R = FUNCTION MEDIAN (WS-F (ALL), FUNCTION PB4162B (WS-LO))
           DISPLAY "MEDIAN=" WS-R " F1=" WS-F (1)
           PERFORM LOAD-DATA
           COMPUTE WS-R = FUNCTION SUM (WS-GC (ALL, ALL), FUNCTION PB4162B (WS-LO))
           DISPLAY "G-SUM=" WS-R " F1=" WS-F (1)
           PERFORM LOAD-DATA
           COMPUTE WS-R = FUNCTION SUM (WS-GC (2, ALL), FUNCTION PB4162B (WS-LO))
           DISPLAY "G-ROW=" WS-R " F1=" WS-F (1)
           PERFORM LOAD-DATA
           COMPUTE WS-R = FUNCTION SUM (WS-GC (ALL, 1), FUNCTION PB4162B (WS-LO))
           DISPLAY "G-COL=" WS-R " F1=" WS-F (1)
           PERFORM LOAD-DATA
           COMPUTE WS-I = FUNCTION ORD-MAX (WS-GC (ALL, ALL), FUNCTION PB4162B (WS-LO))
           DISPLAY "G-OMAX=" WS-I " F1=" WS-F (1)
           PERFORM LOAD-DATA
           COMPUTE WS-I = FUNCTION ORD-MIN (WS-GC (ALL, ALL), FUNCTION PB4162B (WS-LO))
           DISPLAY "G-OMIN=" WS-I " F1=" WS-F (1)
           PERFORM LOAD-DATA
           COMPUTE WS-R = FUNCTION SUM (WS-OD (ALL), FUNCTION PB4162B (WS-LO))
           DISPLAY "ODO=" WS-R " CNT=" WS-CNT
           PERFORM LOAD-DATA
           MOVE FUNCTION CONCAT (WS-S (ALL), FUNCTION PB4162A) TO WS-X
           DISPLAY "TEXT=" WS-X " S1=" WS-S (1)
           PERFORM LOAD-DATA
           COMPUTE WS-R = FUNCTION MAX (FUNCTION MAX (WS-F (ALL), 1), FUNCTION PB4162B (WS-LO))
           DISPLAY "NEST=" WS-R " F1=" WS-F (1)
           PERFORM LOAD-DATA
           COMPUTE WS-R = FUNCTION MIN (FUNCTION MAX (WS-OD (ALL), 1), FUNCTION PB4162B (WS-HI))
           DISPLAY "NESTOD=" WS-R " CNT=" WS-CNT
           PERFORM LOAD-DATA
           MOVE FUNCTION LENGTH (FUNCTION MAX (FUNCTION MAX (WS-F (ALL), 1), 0)) TO WS-U
           MOVE FUNCTION LENGTH (FUNCTION MAX (FUNCTION MAX (WS-F (ALL), 1), FUNCTION PB4162B (WS-LO))) TO WS-H
           IF WS-U = WS-H DISPLAY "NEST LENGTH SAME" ELSE DISPLAY "NEST LENGTH DIFFERENT" END-IF
           STOP RUN.
       LOAD-DATA.
           MOVE 21.5 TO WS-F (1)
           MOVE 5.0 TO WS-F (2)
           MOVE 7.5 TO WS-F (3)
           MOVE 1 TO WS-GC (1, 1)
           MOVE 2 TO WS-GC (1, 2)
           MOVE 3 TO WS-GC (1, 3)
           MOVE 4 TO WS-GC (2, 1)
           MOVE 5 TO WS-GC (2, 2)
           MOVE 6 TO WS-GC (2, 3)
           MOVE 3 TO WS-CNT
           MOVE 10 TO WS-OD (1)
           MOVE 20 TO WS-OD (2)
           MOVE 30 TO WS-OD (3)
           MOVE "AA" TO WS-S (1)
           MOVE "BB" TO WS-S (2)
           MOVE "CC" TO WS-S (3).
       END PROGRAM PB4162.
