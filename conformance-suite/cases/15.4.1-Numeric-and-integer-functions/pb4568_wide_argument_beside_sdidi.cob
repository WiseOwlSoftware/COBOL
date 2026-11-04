      *>--- conformance-case
      *> id: pb4568_wide_argument_beside_sdidi
      *> rules: [RV-15.4.1-1, RV-15.71.4-2, RV-15.72.4-2, RV-15.64.4-1,
      *>     RV-15.77.4-1, RV-15.88.4-1, RV-15.76.4-1, DOC-A.1-123]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4568 (docs/CONFORMANCE.md DOC-A.1-123): under NATIVE
      *> arithmetic a function argument that is an arithmetic
      *> expression of 39 to 77 digits enters EXACTLY beside an
      *> argument the SDIDI carries (a floating-point literal, a
      *> power) or a binary64 one. It used to enter as its 34-digit
      *> round-to-odd image there, so a selection could contradict
      *> the compiler's own relation and MOD lost every digit.
      *> ISO 15.71.4 2): "The comparisons used to determine the
      *> greatest valued argument are made according to the rules
      *> for simple conditions." 15.72.4 2) the same for the least
      *> (cite.py: OK 15.71.4, OK 15.72.4). 8.8.4.2.4: "a comparison
      *> is made with respect to the algebraic value of the
      *> operands" (cite.py: OK 8.8.4.2.4). 15.64.4 1): MOD is
      *> "((argument-1) - ((argument-2) * FUNCTION INTEGER
      *> ((argument-1) / (argument-2))))" (cite.py: OK); 15.77.4 1)
      *> REM the same over INTEGER-PART. Native evaluation is the
      *> implementor's (8.8.1.3, cite.py: OK), and CLAUDE.md rule 1
      *> makes GnuCOBOL 3.2.0 the answer: it is exact on every leg.
      *>
      *> EXPECTED VALUES, DERIVED (Python integers): BIG = 10**18 - 1,
      *> K3 = 999, P = BIG * BIG * K3 =
      *> 998999999999999998002000000000000000999 (39 digits);
      *> Q = 9.98999999999999998002E38 = P - 999 exactly (an SDIDI).
      *>   REL   P - 1 < P is true.
      *>   OMAX  ORD-MAX (P - 1, P, x) = 2 for x = 2.0E0, the COMP-2
      *>         item F2 (2) and 2 ** 0.5: P is the greatest. The
      *>         images of P - 1 and P are equal, so it answered 1.
      *>   OMIN  ORD-MIN (P, P - 1, 1.0E50) = 2 (it answered 1).
      *>   MOD   P = 1024 * 975585937499999998048828125000000000 +
      *>         999: MOD and REM (P, 2 ** 10) are 999 (they answered
      *>         0); MOD (0 - P, 2 ** 10) is 1024 - 999 = 25 (floored:
      *>         the divisor's sign) and REM -999 (the dividend's).
      *>   SUM   SUM (P, -Q) = 999 (the image gave 100000).
      *>   RANGE RANGE (P, P - 2000, Q) = P - (P - 2000) = 2000 (the
      *>         images gave 200000).
      *>   MEAN, MIDRANGE, MEDIAN of (P, -Q) = 999 / 2 = 499.5 (the
      *>         image gave 50000).
      *> Every value is exact in its receiver, so the returned-value
      *> carrier cannot change the expected output.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4568WIDEMIX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BIG PIC 9(18) VALUE 999999999999999999.
       01 K3  PIC 999 VALUE 999.
       01 F2  COMP-2 VALUE 2.
       01 R   PIC -(9)9.
       01 H   PIC -(9)9.9.
       PROCEDURE DIVISION.
           IF BIG * BIG * K3 - 1 < BIG * BIG * K3
              DISPLAY "REL   LT"
           ELSE
              DISPLAY "REL   NOT-LT"
           END-IF
           COMPUTE R = FUNCTION ORD-MAX (BIG * BIG * K3 - 1,
                                         BIG * BIG * K3, 2.0E0)
           DISPLAY "OMAXL " R
           COMPUTE R = FUNCTION ORD-MAX (BIG * BIG * K3 - 1,
                                         BIG * BIG * K3, F2)
           DISPLAY "OMAXF " R
           COMPUTE R = FUNCTION ORD-MAX (BIG * BIG * K3 - 1,
                                         BIG * BIG * K3, 2 ** 0.5)
           DISPLAY "OMAXP " R
           COMPUTE R = FUNCTION ORD-MIN (BIG * BIG * K3,
                                         BIG * BIG * K3 - 1, 1.0E50)
           DISPLAY "OMIN  " R
           COMPUTE R = FUNCTION MOD (BIG * BIG * K3, 2 ** 10)
           DISPLAY "MOD   " R
           COMPUTE R = FUNCTION REM (BIG * BIG * K3, 2 ** 10)
           DISPLAY "REM   " R
           COMPUTE R = FUNCTION MOD (0 - BIG * BIG * K3, 2 ** 10)
           DISPLAY "MODN  " R
           COMPUTE R = FUNCTION REM (0 - BIG * BIG * K3, 2 ** 10)
           DISPLAY "REMN  " R
           COMPUTE R = FUNCTION SUM (BIG * BIG * K3,
                                     -9.98999999999999998002E38)
           DISPLAY "SUM   " R
           COMPUTE R = FUNCTION RANGE (BIG * BIG * K3,
                                       BIG * BIG * K3 - 2000,
                                       9.98999999999999998002E38)
           DISPLAY "RANGE " R
           COMPUTE H = FUNCTION MEAN (BIG * BIG * K3,
                                      -9.98999999999999998002E38)
           DISPLAY "MEAN  " H
           COMPUTE H = FUNCTION MIDRANGE (BIG * BIG * K3,
                                          -9.98999999999999998002E38)
           DISPLAY "MID   " H
           COMPUTE H = FUNCTION MEDIAN (BIG * BIG * K3,
                                        -9.98999999999999998002E38)
           DISPLAY "MED   " H
           STOP RUN.
