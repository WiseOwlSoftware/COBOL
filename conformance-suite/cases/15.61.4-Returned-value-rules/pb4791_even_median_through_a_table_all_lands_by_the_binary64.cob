*>--- conformance-case
*> id: pb4791_even_median_through_a_table_all_lands_by_the_binary64
*> rules: [RV-15.61.4-2, RV-15.61.4-1]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4791 (after PB4753): an even MEDIAN is the arithmetic mean of the two middle values (15.61.4 r2: "the returned value is
      *> the arithmetic mean of the two middle values", cite.py: OK 15.61.4 2)), a NEW value and not a selection. Beside a binary64 item the
      *> equivalent arithmetic expression is evaluated in binary64 (15.4.1: "an implementor-defined approximation of the value of that
      *> expression", cite.py: OK 15.4.1), and a binary64 receiver lands that double. The number of occurrences of argument-1 is the
      *> number of elements a table(ALL) argument stands for (15.3 14): "When ALL is specified as a subscript, the effect is as if each
      *> table element associated with that subscript position were specified", cite.py: OK 15.3 14)), so a fixed table has a bind-time
      *> count and its parity decides the lane exactly as the same elements written out do.
      *>   M1  T (ALL) of one element and EXP10 (-3): two values, the double of (T (1) + EXP10 (-3)) / 2.
      *>   M2  U (ALL) of three elements and EXP10 (-3): four values 0.0001 0.001 0.5 0.7, the two middle values are EXP10 (-3) and U (2),
      *>       the double of (U (2) + EXP10 (-3)) / 2.
      *>   M3  V (ALL) of two elements and EXP10 (-3): three values, odd (15.61.4 1), so the middle value 0.001 is SELECTED, the
      *>       approximation itself (EXP10 (-3) is the double nearest 0.001 and lies above it: its decimal truncated lands one binary64
      *>       below): EQ.
      *>   M4  the elements written out agree with the table: MEDIAN (U (1) U (2) U (3) EXP10 (-3)) = M2's value: EQ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4791TBL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB.
          05 T USAGE FLOAT-LONG OCCURS 1 TIMES.
       01 TC.
          05 U USAGE FLOAT-LONG OCCURS 3 TIMES.
       01 TD.
          05 V USAGE FLOAT-LONG OCCURS 2 TIMES.
       01 AF   USAGE FLOAT-LONG.
       01 BF   USAGE FLOAT-LONG.
       01 FUN3 PIC S9(3) VALUE -3.
       PROCEDURE DIVISION.
           MOVE 0.0001 TO T (1)
           MOVE 0.0001 TO U (1)
           MOVE 0.5 TO U (2)
           MOVE 0.7 TO U (3)
           MOVE 0.0001 TO V (1)
           MOVE 0.5 TO V (2)
           COMPUTE AF = FUNCTION MEDIAN (T (ALL) FUNCTION EXP10 (FUN3))
           COMPUTE BF = (T (1) + FUNCTION EXP10 (FUN3)) / 2
           IF AF = BF DISPLAY "M1 EQ" ELSE DISPLAY "M1 NE" END-IF
           COMPUTE AF = FUNCTION MEDIAN (U (ALL) FUNCTION EXP10 (FUN3))
           COMPUTE BF = (U (2) + FUNCTION EXP10 (FUN3)) / 2
           IF AF = BF DISPLAY "M2 EQ" ELSE DISPLAY "M2 NE" END-IF
           COMPUTE AF = FUNCTION MEDIAN (V (ALL) FUNCTION EXP10 (FUN3))
           IF AF = FUNCTION EXP10 (FUN3) DISPLAY "M3 EQ" ELSE DISPLAY "M3 NE" END-IF
           COMPUTE BF = FUNCTION MEDIAN (U (1) U (2) U (3) FUNCTION EXP10 (FUN3))
           COMPUTE AF = (U (2) + FUNCTION EXP10 (FUN3)) / 2
           IF BF = AF DISPLAY "M4 EQ" ELSE DISPLAY "M4 NE" END-IF
           STOP RUN.
       END PROGRAM PB4791TBL.
