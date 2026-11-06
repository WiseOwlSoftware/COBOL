*>--- conformance-case
*> id: pb4827_even_median_through_a_run_time_table_all_lands_by_the_binary64
*> rules: [RV-15.61.4-2, RV-15.61.4-1]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4827 (after PB4791): the number of occurrences of argument-1 is the number of elements a table(ALL) argument stands for
      *> (15.3 14): "When ALL is specified as a subscript, the effect is as if each table element associated with that subscript position
      *> were specified", cite.py: OK 15.3 14)). Over an OCCURS DEPENDING table that count is a RUN-TIME fact, so whether the value is a
      *> selection (15.61.4 1), odd) or the arithmetic mean of the two middle values (15.61.4 2), even, cite.py: OK 15.61.4 2)) is decided
      *> when the call runs. The mean is a NEW value: beside binary64 arguments the equivalent arithmetic expression is evaluated in
      *> binary64 (15.4.1: "an implementor-defined approximation of the value of that expression", cite.py: OK 15.4.1), exactly as for
      *> the fixed table of pb4791, and a binary64 receiver lands that double.
      *>   M1  N = 3: U (ALL) holds 0.0001 0.5 0.7, with EXP10 (-3) four values, the two middle ones are EXP10 (-3) and U (2): the double of
      *>       (U (2) + EXP10 (-3)) / 2.
      *>   M2  N = 2: three values 0.0001 0.5 and EXP10 (-3), odd, so the middle value is SELECTED, the approximation itself: EQ.
      *>   M3  N = 1: two values, the double of (U (1) + EXP10 (-3)) / 2.
      *>   M4  N = 3 again: the elements written out agree with the table.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4827ODO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N    PIC 9 VALUE 4.
       01 TC.
          05 U USAGE FLOAT-LONG OCCURS 1 TO 4 DEPENDING ON N.
       01 AF   USAGE FLOAT-LONG.
       01 BF   USAGE FLOAT-LONG.
       01 FUN3 PIC S9(3) VALUE -3.
       PROCEDURE DIVISION.
           MOVE 0.0001 TO U (1)
           MOVE 0.5 TO U (2)
           MOVE 0.7 TO U (3)
           MOVE 0.8 TO U (4)
           MOVE 3 TO N
           COMPUTE AF = FUNCTION MEDIAN (U (ALL) FUNCTION EXP10 (FUN3))
           COMPUTE BF = (U (2) + FUNCTION EXP10 (FUN3)) / 2
           IF AF = BF DISPLAY "M1 EQ" ELSE DISPLAY "M1 NE" END-IF
           MOVE 2 TO N
           COMPUTE AF = FUNCTION MEDIAN (U (ALL) FUNCTION EXP10 (FUN3))
           IF AF = FUNCTION EXP10 (FUN3) DISPLAY "M2 EQ" ELSE DISPLAY "M2 NE" END-IF
           MOVE 1 TO N
           COMPUTE AF = FUNCTION MEDIAN (U (ALL) FUNCTION EXP10 (FUN3))
           COMPUTE BF = (U (1) + FUNCTION EXP10 (FUN3)) / 2
           IF AF = BF DISPLAY "M3 EQ" ELSE DISPLAY "M3 NE" END-IF
           MOVE 3 TO N
           COMPUTE AF = FUNCTION MEDIAN (U (ALL) FUNCTION EXP10 (FUN3))
           COMPUTE BF = FUNCTION MEDIAN (U (1) U (2) U (3) FUNCTION EXP10 (FUN3))
           IF AF = BF DISPLAY "M4 EQ" ELSE DISPLAY "M4 NE" END-IF
           STOP RUN.
       END PROGRAM PB4827ODO.
