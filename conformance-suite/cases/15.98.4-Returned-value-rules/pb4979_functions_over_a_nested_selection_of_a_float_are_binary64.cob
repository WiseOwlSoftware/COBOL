      *>--- conformance-case
      *> id: pb4979_functions_over_a_nested_selection_of_a_float_are_binary64
      *> rules: [RV-15.98.4-1, RV-15.86.4-1, RV-15.74.4-1, RV-15.9.4-1, GR-14.9.12.4-6]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4979: MAX and MIN return "the content of the argument" they select (ISO 15.59.4 1)), so a nested
      *> selection that selected a float item returns that binary64, and a function of it whose value is
      *> "an implementor-defined approximation of the value of that expression" (15.4.1; docs/CONFORMANCE.md
      *> DOC-A.1-92b) is evaluated in binary64 exactly as it is for the float item written directly. Which argument
      *> the nested call selects is a run-time fact. VARIANCE, STANDARD-DEVIATION, PRESENT-VALUE and ANNUITY took
      *> the exact decimal of the float's 55-place expansion (VARIANCE 0.03999999999999999889), and DIVIDE ... REMAINDER
      *> truncated the exact quotient 4.99999999999999972244 to 4.99999 where the binary64 quotient 5.0 truncates to 5.00000,
      *> leaving the remainder 0.00000099999999991773 where F * 5.00000 is exactly M.
      *> F = the double of 0.1, M2 = 0.05, M = 0.5, N = 3; MAX (F M2) selects F; MAX (M F) selects M.
      *> Values by Python floats (R and Q are truncated at 20 places, the approximation landing by its shortest decimal):
      *>   V1  VARIANCE (0.1 0.5) = 0.04;  V2  STANDARD-DEVIATION = sqrt (0.04) = 0.2
      *>   V3  PRESENT-VALUE (M; F, M) = 0.1 / 1.5 + 0.5 / 1.5 ** 2 = 0.28888888888888886
      *>   V4  ANNUITY (MAX (F M2) N) = ANNUITY (F N): equal, not the exact rate's annuity (differs in the 17th digit)
      *>   V5  DISPLAY of the variance: the shortest decimal, 0.04
      *>   V6  MAX (M F) selects M, so VARIANCE (M M2) is the exact 0.050625 (mean .275, deviations .225 and -.225)
      *>   D1  DIVIDE M BY MAX (F M2): the binary64 quotient 0.5 / 0.1 = 5.0, subsidiary quotient 5.00000, and the
      *>       remainder 0.5 - 5.00000 * 0.1 = 0 in binary64; D2 is the same for the float item itself
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4979NESTED.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F USAGE FLOAT-LONG VALUE 0.1.
       01 M PIC 9V9(20) VALUE 0.5.
       01 M2 PIC 9V9(20) VALUE 0.05.
       01 N PIC 9 VALUE 3.
       01 Q PIC S9(3)V9(20).
       01 FQ PIC S9V9(5).
       01 FR PIC S9V9(20).
       PROCEDURE DIVISION.
           COMPUTE Q = FUNCTION VARIANCE (FUNCTION MAX (F M2) M)
           DISPLAY "V1 " Q
           COMPUTE Q = FUNCTION STANDARD-DEVIATION
               (FUNCTION MAX (F M2) M)
           DISPLAY "V2 " Q
           COMPUTE Q = FUNCTION PRESENT-VALUE
               (M FUNCTION MAX (F M2) M)
           DISPLAY "V3 " Q
           IF FUNCTION ANNUITY (FUNCTION MAX (F M2) N)
               = FUNCTION ANNUITY (F N)
               DISPLAY "V4 EQ"
           ELSE
               DISPLAY "V4 NE"
           END-IF
           DISPLAY "V5 " FUNCTION VARIANCE (FUNCTION MAX (F M2) M)
           COMPUTE Q = FUNCTION VARIANCE (FUNCTION MAX (M F) M2)
           DISPLAY "V6 " Q
           DIVIDE M BY FUNCTION MAX (F M2) GIVING FQ REMAINDER FR
           DISPLAY "D1 " FQ " " FR
           DIVIDE M BY F GIVING FQ REMAINDER FR
           DISPLAY "D2 " FQ " " FR
           STOP RUN.
       END PROGRAM PB4979NESTED.
