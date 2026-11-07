      *>--- conformance-case
      *> id: pb4891_even_median_of_a_nested_selection_of_a_float_is_the_binary64_mean
      *> rules: [RV-15.61.4-2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4891: MAX, MIN and an odd MEDIAN return "the content of the argument" they select (ISO 15.59.4 1), 15.63.4 1),
      *> 15.61.4 1)), so a nested selection that selected a float item returns that binary64, and an even MEDIAN over it is the mean
      *> of the two middle values (15.61.4 2)) with a binary64 middle: the binary64 mean (docs/CONFORMANCE.md DOC-A.1-92b, as
      *> pb4859_even_median_beside_a_binary64_argument_is_the_binary64_mean). Which argument the nested call selects is a run-time
      *> fact; COMPUTE took the nested value's kind from the written operands of the outer call, saw no float item, and stored the
      *> exact mean of the float's expansion (0.30000000000000000277) where DISPLAY showed the binary64 mean 0.3.
      *> F = the double of 0.1 (0.1000000000000000055511...), M2 = 0.05, M = 0.5, N = 0.7, R PIC 9V9(20) (no ROUNDED: truncated).
      *> Values by Python floats:
      *>   A1  DISPLAY MEDIAN (MAX (F M2) M): MAX selects F, so (0.1 + 0.5) / 2 = 0.3
      *>   A2  COMPUTE R = the same: the double 0.3 is 0.2999999999999999888977697537..., so 0.29999999999999998889
      *>   B1  DISPLAY MEDIAN (MIN (F M) M): MIN selects F: 0.3;  B2  COMPUTE: 0.29999999999999998889
      *>   C1  DISPLAY MEDIAN (MEDIAN (F M2 N) M): the odd MEDIAN selects F: 0.3;  C2  COMPUTE: 0.29999999999999998889
      *>   D1  COMPUTE R = MEDIAN (MAX (F M) M): MAX selects M, both middles fixed-point: the exact mean 0.50000000000000000000
      *>   E1  DISPLAY MEDIAN (MEDIAN (F M) N): the inner even MEDIAN is the binary64 0.3, so (0.3 + 0.7) / 2 = 0.5
      *>   E2  COMPUTE: 0.50000000000000000000 (the exact mean of the inner double and 0.7 is 0.49999999999999999444)
      *>   N1  DISPLAY MEDIAN (- MAX (F M2) 1): the negation of the selected binary64 is a binary64, so (-0.1 + 1) / 2 = 0.45
      *>   N2  COMPUTE: the double 0.45 is 0.4500000000000000111022302462..., so 0.45000000000000001110
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4891MED.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F USAGE FLOAT-LONG VALUE 0.1.
       01 M PIC 9V9(20) VALUE 0.5.
       01 M2 PIC 9V9(20) VALUE 0.05.
       01 N PIC 9V9(20) VALUE 0.7.
       01 R PIC 9V9(20).
       PROCEDURE DIVISION.
           DISPLAY "A1 " FUNCTION MEDIAN (FUNCTION MAX (F M2) M)
           COMPUTE R = FUNCTION MEDIAN (FUNCTION MAX (F M2) M)
           DISPLAY "A2 " R
           DISPLAY "B1 " FUNCTION MEDIAN (FUNCTION MIN (F M) M)
           COMPUTE R = FUNCTION MEDIAN (FUNCTION MIN (F M) M)
           DISPLAY "B2 " R
           DISPLAY "C1 " FUNCTION MEDIAN (FUNCTION MEDIAN (F M2 N) M)
           COMPUTE R = FUNCTION MEDIAN (FUNCTION MEDIAN (F M2 N) M)
           DISPLAY "C2 " R
           COMPUTE R = FUNCTION MEDIAN (FUNCTION MAX (F M) M)
           DISPLAY "D1 " R
           DISPLAY "E1 " FUNCTION MEDIAN (FUNCTION MEDIAN (F M) N)
           COMPUTE R = FUNCTION MEDIAN (FUNCTION MEDIAN (F M) N)
           DISPLAY "E2 " R
           DISPLAY "N1 " FUNCTION MEDIAN (- FUNCTION MAX (F M2) 1)
           COMPUTE R = FUNCTION MEDIAN (- FUNCTION MAX (F M2) 1)
           DISPLAY "N2 " R
           STOP RUN.
       END PROGRAM PB4891MED.
