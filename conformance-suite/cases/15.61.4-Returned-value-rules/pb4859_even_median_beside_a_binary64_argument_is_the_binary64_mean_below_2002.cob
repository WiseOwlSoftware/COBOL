      *>--- conformance-case
      *> id: pb4859_even_median_beside_a_binary64_argument_is_the_binary64_mean_below_2002
      *> rules: [RV-15.61.4-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4859: the refused twin of pb4859_even_median_beside_a_binary64_argument_is_the_binary64_mean. USAGE FLOAT-LONG is a
      *> COBOL-2002 introduction, so below 2002 the data description is refused and no float item exists.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4859MDB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F USAGE FLOAT-LONG VALUE 0.1.
       01 M PIC 9V9(20) VALUE 0.5.
       PROCEDURE DIVISION.
           DISPLAY "C1 " FUNCTION MEDIAN (F M)
           STOP RUN.
       END PROGRAM PB4859MDB.
