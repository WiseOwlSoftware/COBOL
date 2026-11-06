*>--- conformance-case
*> id: pb4791_even_median_through_a_table_all_lands_by_the_binary64_below_2002
*> rules: [RV-15.61.4-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4791: the refused twin of pb4791_even_median_through_a_table_all_lands_by_the_binary64. USAGE FLOAT-LONG is a
      *> COBOL-2002 introduction, so below 2002 the data description is refused and no float item exists.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4791TBB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB.
          05 T USAGE FLOAT-LONG OCCURS 1 TIMES.
       01 AF   USAGE FLOAT-LONG.
       01 FUN3 PIC S9(3) VALUE -3.
       PROCEDURE DIVISION.
           COMPUTE AF = FUNCTION MEDIAN (T (ALL) FUNCTION EXP10 (FUN3))
           STOP RUN.
       END PROGRAM PB4791TBB.
