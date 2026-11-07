      *>--- conformance-case
      *> id: pb4891_even_median_of_a_nested_selection_of_a_float_is_the_binary64_mean_below_2002
      *> rules: [RV-15.61.4-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4891: the refused twin of pb4891_even_median_of_a_nested_selection_of_a_float_is_the_binary64_mean. USAGE
      *> FLOAT-LONG is a COBOL-2002 introduction, so below 2002 the data description is refused and no float item exists.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4891MDB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F USAGE FLOAT-LONG VALUE 0.1.
       01 M PIC 9V9(20) VALUE 0.5.
       01 M2 PIC 9V9(20) VALUE 0.05.
       PROCEDURE DIVISION.
           DISPLAY "A1 " FUNCTION MEDIAN (FUNCTION MAX (F M2) M)
           STOP RUN.
       END PROGRAM PB4891MDB.
