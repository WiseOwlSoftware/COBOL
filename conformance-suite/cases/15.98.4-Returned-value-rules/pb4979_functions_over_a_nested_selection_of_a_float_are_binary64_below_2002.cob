      *>--- conformance-case
      *> id: pb4979_functions_over_a_nested_selection_of_a_float_are_binary64_below_2002
      *> rules: [RV-15.98.4-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4979: the refused twin of pb4979_functions_over_a_nested_selection_of_a_float_are_binary64. USAGE
      *> FLOAT-LONG is a COBOL-2002 introduction, so below 2002 the data description is refused and no float item exists.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4979NESTEDB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F USAGE FLOAT-LONG VALUE 0.1.
       01 M PIC 9V9(20) VALUE 0.5.
       01 M2 PIC 9V9(20) VALUE 0.05.
       PROCEDURE DIVISION.
           DISPLAY "V5 " FUNCTION VARIANCE (FUNCTION MAX (F M2) M)
           STOP RUN.
       END PROGRAM PB4979NESTEDB.
