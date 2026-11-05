*>--- conformance-case
*> id: pb4730_mixed_selection_below_2002
*> rules: [RV-15.59.4-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4730: the refused twin of pb4730_mixed_selection_keeps_a_float_items_content. USAGE FLOAT-LONG and the
      *> floating-point literal 2.5E+40 are COBOL-2002 introductions, so below 2002 the data description is refused and no
      *> float item exists to select among beside a fixed-point item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4730BLW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X5   PIC 9(5).
       01 ONE  PIC 9 VALUE 1.
       01 DP   USAGE FLOAT-LONG VALUE 2.5E+40.
       PROCEDURE DIVISION.
           MOVE FUNCTION MAX (DP ONE) TO X5
           STOP RUN.
       END PROGRAM PB4730BLW.
