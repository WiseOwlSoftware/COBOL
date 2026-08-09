      *> PB65 / RV-15.75.4-1 — a bounded codomain clamps the QUANTIZED value; the interior is untouched.
      *> §15.75.4 r1: RANDOM "is greater than or equal to zero and less than one" — strict, so the
      *> away-from-zero quantization at the working scale may not round the top half-ulp to exactly 1.
      *> §15.10.4 r1 / §15.8.4 r1: ASIN/ACOS are bounded by ±π/2 / π — CLOSED bounds, but IRRATIONAL,
      *> so the nearest scaled value above them (1.570796327 / 3.141592654) is not "equal to the bound"
      *> and lies outside; the clamp lands on ⌊bound·10^ws⌋ (1.570796326 / 3.141592653). §15.11.4 r1:
      *> ATAN's ±π/2 is OPEN. FRACTION-PART's |v| < 1 follows from its §15.42.4 r1 EAE. SQRT has NO
      *> stated bound: SQRT(0.999999999) quantizing to 1.000000000 is a legal §15.4.1 approximation and
      *> stays — the contrast line proving the clamp is per-rule, not a blanket rounding change.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB65CODOMCLAMP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W9    PIC S9V9(9) SIGN LEADING SEPARATE.
       01 FSRC  USAGE COMP-2.
       01 RND   PIC 9V9(9).
       01 HITS  PIC 9(6) VALUE 0.
       01 I     PIC 9(6).
       PROCEDURE DIVISION.
           COMPUTE W9 = FUNCTION ASIN(1)
           DISPLAY "ASIN1 =" W9
           COMPUTE W9 = FUNCTION ASIN(-1)
           DISPLAY "ASINM =" W9
           COMPUTE W9 = FUNCTION ACOS(-1)
           DISPLAY "ACOSM =" W9
           COMPUTE W9 = FUNCTION ATAN(1000000000000000000)
           DISPLAY "ATANB =" W9
           COMPUTE W9 = FUNCTION ASIN(0.5)
           DISPLAY "ASINH =" W9
           MOVE 5.9999999996 TO FSRC
           COMPUTE W9 = FUNCTION FRACTION-PART(FSRC)
           DISPLAY "FRACP =" W9
           COMPUTE W9 = FUNCTION SQRT(0.999999999)
           DISPLAY "SQRTC =" W9
           COMPUTE RND = FUNCTION RANDOM(1)
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 20000
               COMPUTE RND = FUNCTION RANDOM
               IF RND NOT < 1 ADD 1 TO HITS END-IF
           END-PERFORM
           DISPLAY "GE1HITS=" HITS
           STOP RUN.
