      *>--- conformance-case
      *> id: pb4111_variance_family_standard_decimal
      *> rules: [RV-15.98.4-1, RV-15.86.4-1, RV-15.74.4-1]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4111: the STANDARD-DECIMAL twin of
      *> pb4111_variance_family_exact_native. Under a standard
      *> arithmetic mode the 34-digit evaluation IS the value, so the
      *> native exact bodies must not reach here.
      *> ISO 15.4.1 1): "the returned value shall equal the value of
      *> the equivalent arithmetic expression." (cite.py: OK
      *> 15.4.1). ISO 8.8.1.5.2 1): "If the operand value cannot be
      *> expressed exactly in an SDIDI, the value is rounded
      *> according to the rules in 11.9.11, INTERMEDIATE ROUNDING
      *> clause." (cite.py: OK 8.8.1.5.2); 11.9.11.2 3) a): "If the
      *> INTERMEDIATE ROUNDING clause is not specified, the
      *> NEAREST-AWAY-FROM-ZERO phrase is implied." (cite.py: OK).
      *> The FUNCTION MEAN inside 15.98.4 1) is itself an SDIDI.
      *>
      *> EXPECTED VALUES, DERIVED (BIG = 10**18 - 1, K3 = 999,
      *> A = 10**30):
      *>   W, I: BIG * BIG is 36 digits and rounds to 34, so
      *>      BIG * BIG * K3 and BIG * BIG * K3 - 1 are one SDIDI, as
      *>      are BIG * BIG and BIG * BIG - 1: VARIANCE 0, SD 0, and
      *>      PRESENT-VALUE at rate 0 sums P and -P: 0.
      *>   T: 10**33 and 10**33 + 1 are exact (34 digits); their
      *>      mean 10**33 + 0.5 rounds away from zero to 10**33 + 1,
      *>      deviations -1 and 0: VARIANCE 0.5, SD sqrt(0.5) =
      *>      0.70710678118654752440084436... truncated by COMPUTE at
      *>      22 places. PRESENT-VALUE (3, 10**33 + 1, -4 * 10**33):
      *>      the first term 250000000000000000000000000000000.25
      *>      rounds at 34 digits to ...000.3, the second is exact
      *>      -2.5 * 10**32: 0.3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4111VARSTD.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
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
