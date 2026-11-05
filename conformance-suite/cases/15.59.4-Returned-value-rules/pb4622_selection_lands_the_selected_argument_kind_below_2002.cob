*>--- conformance-case
*> id: pb4622_selection_lands_the_selected_argument_kind_below_2002
*> rules: [RV-15.59.4-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4622 (review round 1, R1-2): the refused twin of pb4622_selection_lands_the_selected_argument_kind. USAGE FLOAT-LONG is a
      *> COBOL-2002 introduction, so below 2002 the data description is refused and no float item exists to select among.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4622SLB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ZF   USAGE FLOAT-LONG VALUE 0.
       01 FUN  PIC S9(3) VALUE -6.
       01 R    PIC 9V9(6).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION MAX (ZF FUNCTION EXP10 (FUN))
           STOP RUN.
       END PROGRAM PB4622SLB.
