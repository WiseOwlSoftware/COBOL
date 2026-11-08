      *>--- conformance-case
      *> id: pb4924_even_median_mean_is_one_binary64_kind_on_display_and_compute
      *> rules: [RV-15.61.4-2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4924: the mean of an even MEDIAN with a binary64 middle is a binary64 (ISO 15.61.4 2), 15.4.1; docs/CONFORMANCE.md
      *> DOC-A.1-92b), a float item's content, and a MAX or MIN comparing it "according to the rules for simple conditions"
      *> (15.59.4 1), 15.63.4 1)) compares its exact value on DISPLAY as on COMPUTE. DISPLAY once ordered it by its shortest
      *> image, so a fixed value equal to that image tied with it and the leftmost won.
      *> F = the double of 0.1, M = 0.5, V = 0.30 (PIC 9V99), R PIC 9V9(20) (no ROUNDED: truncated).
      *> The mean of F and M as a double is 0.2999999999999999888977697537..., below V.
      *>   A1  DISPLAY MAX (MEDIAN (F M) V): V is greater, so V's own image 0.30 (the old image ordering tied and showed 0.3)
      *>   A2  COMPUTE R = the same: 0.30000000000000000000
      *>   B1  DISPLAY MIN (V MEDIAN (F M)): the mean is less, so its binary64 image 0.3 (the old tie showed V, 0.30)
      *>   B2  COMPUTE R = the same: 0.29999999999999998889
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4924MED.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F USAGE FLOAT-LONG VALUE 0.1.
       01 M PIC 9V9(20) VALUE 0.5.
       01 V PIC 9V99 VALUE 0.30.
       01 R PIC 9V9(20).
       PROCEDURE DIVISION.
           DISPLAY "A1 " FUNCTION MAX (FUNCTION MEDIAN (F M) V)
           COMPUTE R = FUNCTION MAX (FUNCTION MEDIAN (F M) V)
           DISPLAY "A2 " R
           DISPLAY "B1 " FUNCTION MIN (V FUNCTION MEDIAN (F M))
           COMPUTE R = FUNCTION MIN (V FUNCTION MEDIAN (F M))
           DISPLAY "B2 " R
           STOP RUN.
       END PROGRAM PB4924MED.
