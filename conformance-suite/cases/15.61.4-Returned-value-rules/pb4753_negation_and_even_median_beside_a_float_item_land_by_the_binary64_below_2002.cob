*>--- conformance-case
*> id: pb4753_negation_and_even_median_beside_a_float_item_land_by_the_binary64_below_2002
*> rules: [RV-15.61.4-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4753: the refused twin of pb4753_negation_and_even_median_beside_a_float_item_land_by_the_binary64. USAGE FLOAT-LONG is a
      *> COBOL-2002 introduction, so below 2002 the data description is refused and no float item exists.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4753NGB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 DS   USAGE FLOAT-LONG VALUE 0.0001.
       01 AF   USAGE FLOAT-LONG.
       01 FUN3 PIC S9(3) VALUE -3.
       PROCEDURE DIVISION.
           COMPUTE AF = FUNCTION MEDIAN (DS FUNCTION EXP10 (FUN3))
           STOP RUN.
       END PROGRAM PB4753NGB.
