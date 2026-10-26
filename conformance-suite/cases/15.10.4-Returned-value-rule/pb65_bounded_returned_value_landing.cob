*>--- conformance-case
*> id: pb65_bounded_returned_value_landing
*> rules: [RV-15.10.4-1, RV-15.11.4-1, RV-15.75.4-1, FMT-15.10.2,
*>    FMT-15.11.2, GR-14.7.4.3-2, GR-14.7.4.3-4, GR-14.9.8.4-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> A function whose returned-value rule bounds its value, landed in a fixed-point receiver with and
      *> without ROUNDED, beside an unbounded one (SQRT).
      *> 15.10.4 1): ASIN's returned value "is greater than or equal to -p/2 and less than or equal to +p/2";
      *> 15.8.4 1): ACOS's "is greater than or equal to zero and less than or equal to π"; 15.11.4 1): ATAN's
      *> lies strictly inside +-pi/2; 15.75.4 1): RANDOM's "is greater than or equal to zero and less than one".
      *> Those rules bound the RETURNED value, the 15.4 temporary, which the binary64 approximations here
      *> satisfy. The value STORED is governed by 14.9.8.4 2) (14.6.8, 14.7.4, 14.7.5), never by the bound.
      *> The no-phrase legs TRUNCATE at the receiver's nine places (14.7.4.3 2) "If the ROUNDED phrase is not
      *> specified, execution is as if ROUNDED MODE IS TRUNCATION had been specified"), so they land inside
      *> the bound: ASIN1 1.570796326, ACOSM 3.141592653, ATANB 1.570796326, ASINH 0.523598775 (asin(0.5) =
      *> 0.52359877559...), FRACP 0.999999999, SQRTC 0.999999999 (sqrt(0.999999999) is the binary64
      *> 0.99999999949999995...), SQRTD 0.999999999 (0.99999999994999999...).
      *> The ROUNDED legs round to the nearest nine-place value (14.7.4.3 4), the bare ROUNDED being
      *> NEAREST-AWAY-FROM-ZERO by 11.9.6.3 2)), which lies PAST the bound where the bound is irrational or
      *> open: RASIN1 1.570796327 (1.5707963267948965...), RASINM -1.570796327, RACOSM 3.141592654
      *> (3.1415926535897931...), RFRACP 1.000000000 (the COMP-2 5.9999999996 minus 5 is 0.99999999959999996...),
      *> RSQRTD 1.000000000, RASINH 0.523598776.
      *> ⛔ RE-DERIVED 2026-10-26 (kb/Work PB3829, the owner's approval): this program was pb65_codomain_clamp,
      *> and its ROUNDED legs asserted a CLAMP that cut a rounded value back inside the bound (RASIN1
      *> 1.570796326, RACOSM 3.141592653, RFRACP 0.999999999). That read a rule on the returned value as a rule
      *> on the receiver: the same returned value reaching the same receiver through `FUNCTION ACOS(-1) + 0`,
      *> through a COMP-2 item or under standard-decimal arithmetic was always rounded, and RANDOM ROUNDED into a
      *> receiver with no integer position was a silent .9999 where 14.7.5 3) makes it a size error
      *> (wiseowl/cases/15.75.4-Returned-value-rules/pb3829_random_fraction_rounded_store). The full mode matrix
      *> is 14.7.4-ROUNDED/pb3829_rounded_bounded_function_modes.
      *> WHY EACH LEG CAN FAIL: a clamp prints the old values on the ROUNDED legs; a store that ignores the
      *> ROUNDED phrase prints the truncated values there; a 20,000-draw RANDOM population with a value of 1
      *> or more counts it in GE1HITS (the RETURNED value's bound, which does hold).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB65BOUNDLANDING.
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
           COMPUTE W9 = FUNCTION SQRT(0.9999999999)
           DISPLAY "SQRTD =" W9
      *> The ROUNDED legs: the nearest nine-place value, past the bound where the bound is irrational.
           COMPUTE W9 ROUNDED = FUNCTION ASIN(1)
           DISPLAY "RASIN1=" W9
           COMPUTE W9 ROUNDED = FUNCTION ASIN(-1)
           DISPLAY "RASINM=" W9
           COMPUTE W9 ROUNDED = FUNCTION ACOS(-1)
           DISPLAY "RACOSM=" W9
           COMPUTE W9 ROUNDED = FUNCTION FRACTION-PART(FSRC)
           DISPLAY "RFRACP=" W9
      *> ... and SQRT, which states no bound: it rounds the same way, so the bounded functions are no
      *> different from it.
           COMPUTE W9 ROUNDED = FUNCTION SQRT(0.9999999999)
           DISPLAY "RSQRTD=" W9
           COMPUTE W9 ROUNDED = FUNCTION ASIN(0.5)
           DISPLAY "RASINH=" W9
           COMPUTE RND = FUNCTION RANDOM(1)
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 20000
               COMPUTE RND = FUNCTION RANDOM
               IF RND NOT < 1 ADD 1 TO HITS END-IF
           END-PERFORM
           DISPLAY "GE1HITS=" HITS
           STOP RUN.
