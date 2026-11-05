*>--- conformance-case
*> id: pb4622_selection_lands_the_selected_argument_kind
*> rules: [RV-15.59.4-1, RV-15.61.4-1, RV-15.63.4-1]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4622, PB4730 (review round 1, R1-2 and R1-3): MAX, MIN and an odd-count MEDIAN return the CONTENT of the selected
      *> argument, and the selected argument lands as the KIND of value it is.
      *>
      *> ISO 15.59.4 r1: "The returned value is the content of the argument-1 having the greatest value." (cite.py: OK 15.59.4 1)),
      *> MIN 15.63.4 r1 ("the least value"), MEDIAN 15.61.1 ("returns the content of the argument whose value is the middle value").
      *> Two kinds of binary64 meet in a list. A float ITEM (USAGE FLOAT-LONG) holds a binary64 that is an exact binary fraction and lands
      *> by that exact value (14.6.8.2 r4). An engine's APPROXIMATION - FUNCTION EXP10 (-6), whose value no finite decimal spells
      *> (15.4.1: "an implementor-defined approximation of the value of that expression", cite.py: OK 15.4.1) - lands in a fixed-point
      *> receiver by its shortest round-trip decimal, 0.000001 (docs/CONFORMANCE.md DOC-A.1-92), and in a floating-point receiver as the
      *> binary64 itself. Which argument is selected is a run-time fact, so the selected value must keep its own kind.
      *>   A  EXP10 (-6) alone into PIC 9V9(6): 0.000001 (the control).
      *>   B  MAX (ZF EXP10 (-6)) selects the approximation (ZF is 0): 0.000001 by COMPUTE and by MOVE; MIN (DQ EXP10 (-6)) selects it
      *>      (DQ is 0.5); the odd MEDIAN (ZF EXP10 (-6) DQ) is the middle value, the approximation. The exact binary expansion of the
      *>      approximation, 9.99999999999999954748E-7, stores 0.000000 in 6 places, the wrong landing.
      *>   C  MAX (DQ EXP10 (-6)) selects the float item: 0.500000.
      *>   D  A float receiver gets the selected float item's content whole, whatever the other arguments are (the SDIDI's 34 digits of
      *>      DP = 2.5E+40, exactly 25000000000000000155002161260194579873792, sit below the binary64 and truncate one below it):
      *>      MAX (DP ONE), MIN (DP 1.0E+50), the odd MEDIAN (DP ONE 1.0E+50) and MAX (DT ZERO) with DT = 0.1 all store the item (EQ).
      *>   E  An approximation selected beside a float item lands in a floating-point receiver as that binary64 (EXP10 (-3) is the double
      *>      nearest 0.001, which lies ABOVE 0.001, so its decimal truncated lands one binary64 below it): EQ.
      *>   F  A binary32 receiver rounds the exact value once: 0.1 (the binary64 0.1000000000000000055511151231257827...) truncates
      *>      to the binary32 0.0999999940395355224609375 and rounds to 0.100000001490116119384765625 (Python struct), shown at 12 places.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4622SEL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ZF   USAGE FLOAT-LONG VALUE 0.
       01 DQ   USAGE FLOAT-LONG VALUE 0.5.
       01 DP   USAGE FLOAT-LONG VALUE 2.5E+40.
       01 DT   USAGE FLOAT-LONG VALUE 0.1.
       01 DS   USAGE FLOAT-LONG VALUE 0.0001.
       01 AF   USAGE FLOAT-LONG.
       01 AB   USAGE FLOAT-SHORT.
       01 FUN  PIC S9(3) VALUE -6.
       01 FUN3 PIC S9(3) VALUE -3.
       01 ONE  PIC 9 VALUE 1.
       01 R    PIC 9V9(6).
       01 R2   PIC 9V9(6).
       01 S12  PIC 9V9(12).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION EXP10 (FUN)
           DISPLAY "A  " R
           COMPUTE R = FUNCTION MAX (ZF FUNCTION EXP10 (FUN))
           DISPLAY "B1 " R
           MOVE FUNCTION MAX (ZF FUNCTION EXP10 (FUN)) TO R2
           DISPLAY "B2 " R2
           COMPUTE R = FUNCTION MIN (DQ FUNCTION EXP10 (FUN))
           DISPLAY "B3 " R
           COMPUTE R = FUNCTION MEDIAN (ZF FUNCTION EXP10 (FUN) DQ)
           DISPLAY "B4 " R
           COMPUTE R = FUNCTION MAX (DQ FUNCTION EXP10 (FUN))
           DISPLAY "C  " R
           COMPUTE AF = FUNCTION MAX (DP ONE)
           IF AF = DP DISPLAY "D1 EQ" ELSE DISPLAY "D1 NE" END-IF
           COMPUTE AF = FUNCTION MIN (DP 1.0E+50)
           IF AF = DP DISPLAY "D2 EQ" ELSE DISPLAY "D2 NE" END-IF
           COMPUTE AF = FUNCTION MEDIAN (DP ONE 1.0E+50)
           IF AF = DP DISPLAY "D3 EQ" ELSE DISPLAY "D3 NE" END-IF
           COMPUTE AF = FUNCTION MAX (DT 0)
           IF AF = DT DISPLAY "D4 EQ" ELSE DISPLAY "D4 NE" END-IF
           COMPUTE AF = FUNCTION MAX (DS FUNCTION EXP10 (FUN3))
           IF AF = FUNCTION EXP10 (FUN3) DISPLAY "E  EQ" ELSE DISPLAY "E  NE" END-IF
           COMPUTE AB = FUNCTION MAX (DT 0)
           MOVE AB TO S12
           DISPLAY "F1 " S12
           COMPUTE AB ROUNDED = FUNCTION MAX (DT 0)
           MOVE AB TO S12
           DISPLAY "F2 " S12
           STOP RUN.
       END PROGRAM PB4622SEL.
