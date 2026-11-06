*>--- conformance-case
*> id: pb4827_even_median_through_a_dynamic_capacity_table_all_lands_by_the_binary64_below_2014
*> rules: [RV-15.61.4-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4827: the refused twin of pb4827_even_median_through_a_dynamic_capacity_table_all_lands_by_the_binary64. OCCURS DYNAMIC
      *> is a COBOL-2014 addition (ISO 13.18.38 Format 4), so below 2014 the data description is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4827DYB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TC.
          05 U USAGE FLOAT-LONG OCCURS DYNAMIC CAPACITY IN CAP FROM 1 TO 4.
       01 AF   USAGE FLOAT-LONG.
       01 FUN3 PIC S9(3) VALUE -3.
       PROCEDURE DIVISION.
           SET CAP TO 3
           COMPUTE AF = FUNCTION MEDIAN (U (ALL) FUNCTION EXP10 (FUN3))
           STOP RUN.
       END PROGRAM PB4827DYB.
