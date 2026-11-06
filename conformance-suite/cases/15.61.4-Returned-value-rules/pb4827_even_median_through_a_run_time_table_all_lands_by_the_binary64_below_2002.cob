*>--- conformance-case
*> id: pb4827_even_median_through_a_run_time_table_all_lands_by_the_binary64_below_2002
*> rules: [RV-15.61.4-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4827: the refused twin of pb4827_even_median_through_a_run_time_table_all_lands_by_the_binary64. USAGE FLOAT-LONG is a
      *> COBOL-2002 introduction, so below 2002 the data description is refused and no float item exists.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4827ODB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N    PIC 9 VALUE 3.
       01 TC.
          05 U USAGE FLOAT-LONG OCCURS 1 TO 4 DEPENDING ON N.
       01 AF   USAGE FLOAT-LONG.
       01 FUN3 PIC S9(3) VALUE -3.
       PROCEDURE DIVISION.
           COMPUTE AF = FUNCTION MEDIAN (U (ALL) FUNCTION EXP10 (FUN3))
           STOP RUN.
       END PROGRAM PB4827ODB.
