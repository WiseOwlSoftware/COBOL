      *>--- conformance-case
      *> id: pb4111_variance_family_exact_native
      *> rules: [RV-15.98.4-1, RV-15.86.4-1, RV-15.74.4-1, DOC-A.1-123]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4111 (docs/CONFORMANCE.md DOC-A.1-123): under NATIVE
      *> arithmetic VARIANCE, STANDARD-DEVIATION and PRESENT-VALUE
      *> return the EXACT value of their equivalent arithmetic
      *> expression, rounded once. They used to evaluate it on 34
      *> digits: each argument as its SDIDI image and a 34-digit mean
      *> or term quotient.
      *> ISO 15.4.1: "When native arithmetic is in effect and an
      *> equivalent arithmetic expression is specified, the value
      *> returned is an implementor-defined approximation of the
      *> value of that expression." (cite.py: OK 15.4.1). ISO
      *> 8.8.1.3 leaves the method to the implementor (cite.py: OK
      *> 8.8.1.3), so CLAUDE.md rule 1 makes GnuCOBOL 3.2.0 the
      *> answer: it evaluates the three from the exact arguments.
      *> 15.98.4 1): the mean of the squared deviations from FUNCTION
      *> MEAN, "where argument-list is the argument-1 list for the
      *> VARIANCE function itself"; 15.86.4 1): "(FUNCTION SQRT
      *> (FUNCTION VARIANCE (argument-list)))"; 15.74.4 1) b):
      *> "(argument-21 / (1 + argument-1) + argument-22 / (1 +
      *> argument-1) ** 2)" (each cite.py: OK).
      *>
      *> EXPECTED VALUES, DERIVED (Python fractions.Fraction):
      *>   BIG = 10**18 - 1, K3 = 999, A = 10**30.
      *>   W: P = BIG * BIG * K3 has 39 digits (the exact wide form).
      *>      mean P - 0.5, deviations +-0.5: VARIANCE 0.25, SD 0.5;
      *>      PRESENT-VALUE at rate 0 is P + (1 - P) = 1. The image
      *>      gave VARIANCE 10**10 (shown 0 here: RV's integer part
      *>      keeps 9 digits), SD 100000 and PV 0.
      *>   I: BIG * BIG has 36 digits (inside Int128). The same
      *>      values; the 34-digit body gave 0.5, 0.7071... and 0.
      *>   T: A * 1000 = 10**33 has 34 digits, lifted exactly. The
      *>      mean 10**33 + 0.5 needs 35 digits, so the 34-digit mean
      *>      was 10**33 + 1 and VARIANCE 0.5; the value is 0.25 (SD
      *>      0.5). PRESENT-VALUE (3, 10**33 + 1, -4 * 10**33) is
      *>      (10**33 + 1) / 4 - 4 * 10**33 / 16 = 0.25 exactly; the
      *>      34-digit term (10**33 + 1) / 4 rounded to ...0.3.
      *> Each value is exact in the receiver, so the returned-value
      *> carrier cannot change the expected output. The
      *> STANDARD-DECIMAL twin is
      *> pb4111_variance_family_standard_decimal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4111VARNAT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BIG PIC 9(18) VALUE 999999999999999999.
       01 K3  PIC 999 VALUE 999.
       01 A   PIC 9(31) VALUE 1000000000000000000000000000000.
       01 RV  PIC S9(9)V9(22).
       PROCEDURE DIVISION.
           COMPUTE RV = FUNCTION VARIANCE (BIG * BIG * K3,
                                           BIG * BIG * K3 - 1)
           DISPLAY "W-VAR " RV
           COMPUTE RV = FUNCTION STANDARD-DEVIATION (BIG * BIG * K3,
                                           BIG * BIG * K3 - 1)
           DISPLAY "W-SD  " RV
           COMPUTE RV = FUNCTION PRESENT-VALUE (0, BIG * BIG * K3,
                                           1 - BIG * BIG * K3)
           DISPLAY "W-PV  " RV
           COMPUTE RV = FUNCTION VARIANCE (BIG * BIG, BIG * BIG - 1)
           DISPLAY "I-VAR " RV
           COMPUTE RV = FUNCTION STANDARD-DEVIATION (BIG * BIG,
                                                     BIG * BIG - 1)
           DISPLAY "I-SD  " RV
           COMPUTE RV = FUNCTION PRESENT-VALUE (0, BIG * BIG,
                                                1 - BIG * BIG)
           DISPLAY "I-PV  " RV
           COMPUTE RV = FUNCTION VARIANCE (A * 1000, A * 1000 + 1)
           DISPLAY "T-VAR " RV
           COMPUTE RV = FUNCTION STANDARD-DEVIATION (A * 1000,
                                                     A * 1000 + 1)
           DISPLAY "T-SD  " RV
           COMPUTE RV = FUNCTION PRESENT-VALUE (3, A * 1000 + 1,
                                                -4 * A * 1000)
           DISPLAY "T-PV  " RV
           STOP RUN.
