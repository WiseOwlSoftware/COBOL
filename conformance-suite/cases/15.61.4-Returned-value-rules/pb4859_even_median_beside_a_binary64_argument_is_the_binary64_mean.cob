      *>--- conformance-case
      *> id: pb4859_even_median_beside_a_binary64_argument_is_the_binary64_mean
      *> rules: [RV-15.61.4-2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4859 (the DISPLAY and fixed-point-neighbour arms of PB4753 and PB4827): an even-count MEDIAN returns "the arithmetic
      *> mean of the two middle values" (ISO 15.61.4 2), a NEW value, which 15.4.1 leaves under native arithmetic to the implementor as
      *> "an implementor-defined approximation of the value of that expression"; the determination (docs/CONFORMANCE.md DOC-A.1-92b)
      *> is that a binary64 operand puts the equivalent expression in binary64, whatever channel reads it; the operands of the mean
      *> are the two MIDDLE values only, so a binary64 elsewhere in the list changes nothing (pb65_selection_mixed_carriers T12). The
      *> mean was the exact mean of the float's SDIDI lift when a fixed-point item was the other middle, and on DISPLAY of two
      *> binary64 middles it was the SDIDI mean of their approximations, where COMPUTE gave the binary64 mean.
      *> F = the double of 0.1, G = the double of 0.2, M = 0.5 in a PIC 9V9(20) item. Values by Python floats:
      *>   B1  DISPLAY MEDIAN (F G)   (0.1 + 0.2) / 2 = 0.15000000000000002 (0.1 + 0.2 is 0.30000000000000004)
      *>   B2  COMPUTE R = MEDIAN (F G) into 9V9(20), no ROUNDED so truncated at 20 places: the double 0.15000000000000002 is
      *>       0.1500000000000000222044604925031308..., so 0.15000000000000002220
      *>   C1  DISPLAY MEDIAN (F M)   (0.1 + 0.5) / 2 = 0.3 (the exact mean of the lifts, .30000000000000000277..., is the wrong value)
      *>   C2  COMPUTE R = MEDIAN (F M) into 9V9(20): the double 0.3 is 0.299999999999999988897769753748..., so 0.29999999999999998889
      *>   C3  COMPUTE D = MEDIAN (F M) into a FLOAT-LONG item, DISPLAY D: the double 0.3, shown 0.3
      *>   E1  DISPLAY MEDIAN (F M N) with N = 0.7: an odd count SELECTS the middle value M, whole: 0.50000000000000000000
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4859MED.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F USAGE FLOAT-LONG VALUE 0.1.
       01 G USAGE FLOAT-LONG VALUE 0.2.
       01 D USAGE FLOAT-LONG.
       01 M PIC 9V9(20) VALUE 0.5.
       01 N PIC 9V9(20) VALUE 0.7.
       01 R PIC 9V9(20).
       PROCEDURE DIVISION.
           DISPLAY "B1 " FUNCTION MEDIAN (F G)
           COMPUTE R = FUNCTION MEDIAN (F G)
           DISPLAY "B2 " R
           DISPLAY "C1 " FUNCTION MEDIAN (F M)
           COMPUTE R = FUNCTION MEDIAN (F M)
           DISPLAY "C2 " R
           COMPUTE D = FUNCTION MEDIAN (F M)
           DISPLAY "C3 " D
           DISPLAY "E1 " FUNCTION MEDIAN (F M N)
           STOP RUN.
       END PROGRAM PB4859MED.
