*>--- conformance-case
*> id: pb4827_even_median_through_a_dynamic_capacity_table_all_lands_by_the_binary64
*> rules: [RV-15.61.4-2, RV-15.61.4-1]
*> accept: [2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4827, the dynamic-capacity arm of pb4827_even_median_through_a_run_time_table_all_lands_by_the_binary64: a table(ALL)
      *> over an OCCURS DYNAMIC table stands for as many elements as the table's current capacity (15.3 14), a run-time fact, so the
      *> parity of the MEDIAN's argument count (15.61.4 1) odd, 15.61.4 2) even) is decided when the call runs and an even count is the
      *> binary64 mean of the two middle values beside binary64 arguments (15.4.1).
      *>   M1  capacity 3: with EXP10 (-3) four values, the double of (U (2) + EXP10 (-3)) / 2.
      *>   M2  capacity 2: three values, the middle one is SELECTED, the approximation itself: EQ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4827DYN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TC.
          05 U USAGE FLOAT-LONG OCCURS DYNAMIC CAPACITY IN CAP FROM 1 TO 4.
       01 AF   USAGE FLOAT-LONG.
       01 BF   USAGE FLOAT-LONG.
       01 FUN3 PIC S9(3) VALUE -3.
       PROCEDURE DIVISION.
           SET CAP TO 4
           MOVE 0.0001 TO U (1)
           MOVE 0.5 TO U (2)
           MOVE 0.7 TO U (3)
           MOVE 0.8 TO U (4)
           SET CAP TO 3
           COMPUTE AF = FUNCTION MEDIAN (U (ALL) FUNCTION EXP10 (FUN3))
           COMPUTE BF = (U (2) + FUNCTION EXP10 (FUN3)) / 2
           IF AF = BF DISPLAY "M1 EQ" ELSE DISPLAY "M1 NE" END-IF
           SET CAP TO 2
           COMPUTE AF = FUNCTION MEDIAN (U (ALL) FUNCTION EXP10 (FUN3))
           IF AF = FUNCTION EXP10 (FUN3) DISPLAY "M2 EQ" ELSE DISPLAY "M2 NE" END-IF
           STOP RUN.
       END PROGRAM PB4827DYN.
