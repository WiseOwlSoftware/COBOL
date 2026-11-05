*>--- conformance-case
*> id: pb4753_negation_and_even_median_beside_a_float_item_land_by_the_binary64
*> rules: [RV-15.61.4-2, RV-15.59.4-1]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4753 (after PB4622, PB4730): a binary64 that is an engine's APPROXIMATION keeps the double it came from when it is
      *> SELECTED, and a binary64 receiver lands that double, not the truncation of its shortest decimal one binary64 below it.
      *> Two operations on such a value are not a selection, and each lands as the binary64 evaluation of its own expression:
      *>   N1  the negation of a selected approximation: the sign flip of a binary64 is exact, so - MAX (DS EXP10 (-3)) is the double
      *>       - EXP10 (-3). EXP10 (-3) is the double nearest 0.001 (it lies ABOVE 0.001; its decimal truncated lands one below).
      *>   N2  the negation of a selected float item: - MAX (DT 0) is - DT, DT = 0.1.
      *>   M1  an even MEDIAN (15.61.4 r2: "the returned value is the arithmetic mean of the two middle values", cite.py: OK 15.61.4 2))
      *>       of a float item and an approximation is the mean of the two middle values in binary64 (15.4.1: "an implementor-defined
      *>       approximation of the value of that expression", cite.py: OK 15.4.1), the double of (DS + EXP10 (-3)) / 2, the
      *>       equivalent arithmetic expression, and not an exact mean of the two values truncated into the binary64.
      *>   M2  the same with the item above the approximation: (DQ + EXP10 (-3)) / 2.
      *>   M3  an odd MEDIAN keeps the selection: MEDIAN (DS EXP10 (-3) DQ) is the middle value, the approximation: EQ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4753NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 DS   USAGE FLOAT-LONG VALUE 0.0001.
       01 DQ   USAGE FLOAT-LONG VALUE 0.5.
       01 DT   USAGE FLOAT-LONG VALUE 0.1.
       01 AF   USAGE FLOAT-LONG.
       01 BF   USAGE FLOAT-LONG.
       01 FUN3 PIC S9(3) VALUE -3.
       PROCEDURE DIVISION.
           COMPUTE AF = - FUNCTION MAX (DS FUNCTION EXP10 (FUN3))
           COMPUTE BF = - FUNCTION EXP10 (FUN3)
           IF AF = BF DISPLAY "N1 EQ" ELSE DISPLAY "N1 NE" END-IF
           COMPUTE AF = - FUNCTION MAX (DT 0)
           COMPUTE BF = - DT
           IF AF = BF DISPLAY "N2 EQ" ELSE DISPLAY "N2 NE" END-IF
           COMPUTE AF = FUNCTION MEDIAN (DS FUNCTION EXP10 (FUN3))
           COMPUTE BF = (DS + FUNCTION EXP10 (FUN3)) / 2
           IF AF = BF DISPLAY "M1 EQ" ELSE DISPLAY "M1 NE" END-IF
           COMPUTE AF = FUNCTION MEDIAN (DQ FUNCTION EXP10 (FUN3))
           COMPUTE BF = (DQ + FUNCTION EXP10 (FUN3)) / 2
           IF AF = BF DISPLAY "M2 EQ" ELSE DISPLAY "M2 NE" END-IF
           COMPUTE AF = FUNCTION MEDIAN (DS FUNCTION EXP10 (FUN3) DQ)
           IF AF = FUNCTION EXP10 (FUN3) DISPLAY "M3 EQ" ELSE DISPLAY "M3 NE" END-IF
           STOP RUN.
       END PROGRAM PB4753NEG.
