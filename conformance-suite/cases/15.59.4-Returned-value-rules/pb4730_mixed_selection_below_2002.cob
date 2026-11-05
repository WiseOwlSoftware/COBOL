*>--- conformance-case
*> id: pb4730_mixed_selection_below_2002
*> rules: [RV-15.59.4-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4730: the refused twin of pb4730_mixed_selection_keeps_a_float_items_content. FUNCTION MAX is a COBOL-2002
      *> introduction in WiseOwl's catalog window and USAGE FLOAT-LONG is one too, so below 2002 the reference is refused and no
      *> selection among a float item and a fixed-point item exists.
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
