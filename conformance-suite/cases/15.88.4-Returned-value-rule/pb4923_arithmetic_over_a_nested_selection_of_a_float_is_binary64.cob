      *>--- conformance-case
      *> id: pb4923_arithmetic_over_a_nested_selection_of_a_float_is_binary64
      *> rules: [RV-15.88.4-1, RV-15.60.4-1, RV-15.62.4-1, RV-15.76.4-1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4923: MAX and MIN return "the content of the argument" they select (ISO 15.59.4 1), 15.63.4 1)), so a nested
      *> selection that selected a float item returns that binary64, and SUM, MEAN, MIDRANGE and RANGE of it, like an arithmetic
      *> expression over it, are evaluated in binary64 under native arithmetic (15.4.1: "an implementor-defined approximation of
      *> the value of that expression"; docs/CONFORMANCE.md DOC-A.1-92b), exactly as they are for the float item written
      *> directly. Which argument the nested call selects is a run-time fact; the exact lane took the nested value's kind from
      *> nothing and summed the float's 55-place expansion, 0.60000000000000000555 where F + M is the double 0.6.
      *> F = the double of 0.1, M2 = 0.05, M = 0.5, N = 0.7; R is truncated at 20 places (no ROUNDED). Values by Python floats:
      *>   S1  SUM (MAX (F M2) M):  MAX selects F, so 0.1 + 0.5 = 0.6;  S2 COMPUTE: the double 0.6 is 0.59999999999999997779
      *>   E1  MEAN: (0.1 + 0.5) / 2 = 0.3;  E2: the double 0.3 is 0.29999999999999998889
      *>   D1  MIDRANGE: (0.5 + 0.1) / 2 = 0.3;  D2: 0.29999999999999998889
      *>   G1  RANGE: 0.5 - 0.1 = 0.4;  G2: 0.40000000000000002220
      *>   P  MAX (F M2) + N = 0.7999999999999999 (0.79999999999999993338); Q  MAX (F M2) - N = -0.6 (-0.59999999999999997779)
      *>   T  MAX (F M2) * N = 0.06999999999999999 (0.06999999999999999278)
      *>   V  MAX (F M2) / N = 0.14285714285714288 (0.14285714285714287696)
      *>   W  MAX (F M2) ** 3 = 0.0010000000000000002 (0.00100000000000000023)
      *>   X  (MAX (F M2) + N) / N = 1.1428571428571428 (1.14285714285714279370)
      *>   Z1 M ** MAX (F M2) = 0.5 ** 0.1 = 0.9330329915368074 (0.93303299153680741007), the power of a non-integer exponent
      *>   Z2 MAX (F M2) ** M = 0.1 ** 0.5 = 0.31622776601683794 (0.31622776601683794117), the same in the base position
      *>   Y  MAX (M F) + N selects M (both fixed-point): the exact 1.20000000000000000000
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4923ARI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F USAGE FLOAT-LONG VALUE 0.1.
       01 M PIC 9V9(20) VALUE 0.5.
       01 M2 PIC 9V9(20) VALUE 0.05.
       01 N PIC 9V9(20) VALUE 0.7.
       01 R PIC S9V9(20).
       PROCEDURE DIVISION.
           DISPLAY "S1 " FUNCTION SUM (FUNCTION MAX (F M2) M)
           COMPUTE R = FUNCTION SUM (FUNCTION MAX (F M2) M)
           DISPLAY "S2 " R
           DISPLAY "E1 " FUNCTION MEAN (FUNCTION MAX (F M2) M)
           COMPUTE R = FUNCTION MEAN (FUNCTION MAX (F M2) M)
           DISPLAY "E2 " R
           DISPLAY "D1 " FUNCTION MIDRANGE (FUNCTION MAX (F M2) M)
           COMPUTE R = FUNCTION MIDRANGE (FUNCTION MAX (F M2) M)
           DISPLAY "D2 " R
           DISPLAY "G1 " FUNCTION RANGE (FUNCTION MAX (F M2) M)
           COMPUTE R = FUNCTION RANGE (FUNCTION MAX (F M2) M)
           DISPLAY "G2 " R
           COMPUTE R = FUNCTION MAX (F M2) + N
           DISPLAY "P " R
           COMPUTE R = FUNCTION MAX (F M2) - N
           DISPLAY "Q " R
           COMPUTE R = FUNCTION MAX (F M2) * N
           DISPLAY "T " R
           COMPUTE R = FUNCTION MAX (F M2) / N
           DISPLAY "V " R
           COMPUTE R = FUNCTION MAX (F M2) ** 3
           DISPLAY "W " R
           COMPUTE R = (FUNCTION MAX (F M2) + N) / N
           DISPLAY "X " R
           COMPUTE R = FUNCTION MAX (M F) + N
           DISPLAY "Y " R
           COMPUTE R = M ** FUNCTION MAX (F M2)
           DISPLAY "Z1 " R
           COMPUTE R = FUNCTION MAX (F M2) ** M
           DISPLAY "Z2 " R
           STOP RUN.
       END PROGRAM PB4923ARI.
