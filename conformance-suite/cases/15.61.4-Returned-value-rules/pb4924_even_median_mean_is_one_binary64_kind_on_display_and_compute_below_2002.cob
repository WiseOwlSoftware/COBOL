      *>--- conformance-case
      *> id: pb4924_even_median_mean_is_one_binary64_kind_on_display_and_compute_below_2002
      *> rules: [RV-15.61.4-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4924: the refused twin of pb4924_even_median_mean_is_one_binary64_kind_on_display_and_compute. USAGE
      *> FLOAT-LONG is a COBOL-2002 introduction, so below 2002 the data description is refused and no float item exists.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4924MDB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F USAGE FLOAT-LONG VALUE 0.1.
       01 M PIC 9V9(20) VALUE 0.5.
       01 V PIC 9V99 VALUE 0.30.
       PROCEDURE DIVISION.
           DISPLAY "A1 " FUNCTION MAX (FUNCTION MEDIAN (F M) V)
           STOP RUN.
       END PROGRAM PB4924MDB.
