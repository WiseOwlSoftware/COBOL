*>--- conformance-case
*> id: pb3829_remainder_float_sender
*> rules: [GR-14.9.12.4-1, GR-14.9.12.4-6, GR-14.9.12.4-7,
*>    GR-14.7.4.3-3, GR-14.7.4.3-10, GR-14.7.5-2, GR-14.7.5-L2.1]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3829's sweep: DIVIDE ... GIVING ... REMAINDER with a sender on the binary64 carrier (a FLOAT-LONG item,
*> a float function's returned value) or the SDIDI carrier (a NUMVAL value) under native arithmetic.
*> 14.9.12.4 1): "When native arithmetic is in effect, the quotient is the result of dividing the dividend by the
*> divisor." The SAME quotient whether or not REMAINDER is written; it is stored per 14.7.4 at identifier-3.
*> 14.9.12.4 6) c): "a subsidiary quotient is developed that is signed and derived from the quotient by truncation of
*> digits at the least significant end and that has the same number of digits and the same decimal point location as
*> the data item referenced by identifier-3"; 7): the remainder is "the result of multiplying the subsidiary quotient
*> and the divisor and subtracting the product from the dividend".
*> The float values are WiseOwl's binary64 (FLOAT-LONG content and native float evaluation are the implementor's,
*> 14.6.8.2 2) and 8.8.1.3), so this lives in the WiseOwl tree. Each leg, from the binary64 values:
*>  AFZ / AFZR: FL = 0.12340000000001 (binary64 0.1234000000000100...), / 1. AWAY-FROM-ZERO (14.7.4.3 3)) gives
*>    0.1235 with or without REMAINDER; the subsidiary quotient 0.1234 leaves about 1.0E-14, which the
*>    nine-place R truncates to 0.000000000.
*>  AFZN: FN = -0.12340000000001, AWAY-FROM-ZERO: -0.1235; the remainder, about -1.0E-14, truncates to zero.
*>  TRUNC: 0.09 / FB, FB = 0.30000000000001: the quotient 0.29999999999998999... truncates to 0.2999 (14.7.4.3 10));
*>    the remainder 0.09 - 0.2999 x 0.30000000000001 = 0.0000299999999970... truncates to 0.000029999.
*>  NUMVAL: FUNCTION NUMVAL("0.12340000000001") / 1, AWAY-FROM-ZERO: 0.1235; remainder 1E-14 -> 0.000000000.
*>  ACOS: FUNCTION ACOS(-1) / 1 ROUNDED: 3.1416 (3.14159265358979311...); subsidiary 3.1415, remainder
*>    0.0000926535897931... -> 0.000092653.
*>  ZERO: FL / FZ with FZ = 0 under ON SIZE ERROR: 14.7.5's size error (a zero divisor), Q and R unchanged (9.9999 and
*>    9.999999999 placed there first).
*> WHY EACH LEG CAN FAIL: a lane that truncates a float or SDIDI sender at a working scale BEFORE dividing (the defect
*> PB3829's sweep removed) prints 0.1234 on AFZR and NUMVAL and 0.3000 / 0.000000000 on TRUNC.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3829DIVREM.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 Q4 PIC S9V9999 SIGN LEADING SEPARATE.
01 RM PIC S9V9(9) SIGN LEADING SEPARATE.
01 FL USAGE FLOAT-LONG.
01 FN USAGE FLOAT-LONG.
01 FB USAGE FLOAT-LONG.
01 FZ USAGE FLOAT-LONG.
01 N9 PIC 9V99 VALUE 0.09.
PROCEDURE DIVISION.
           MOVE 0.12340000000001 TO FL
           MOVE -0.12340000000001 TO FN
           MOVE 0.30000000000001 TO FB
           MOVE 0 TO FZ
           DIVIDE FL BY 1 GIVING Q4 ROUNDED MODE AWAY-FROM-ZERO
           DISPLAY "AFZ    " Q4
           DIVIDE FL BY 1 GIVING Q4 ROUNDED MODE AWAY-FROM-ZERO REMAINDER RM
           DISPLAY "AFZR   " Q4 " " RM
           DIVIDE FN BY 1 GIVING Q4 ROUNDED MODE AWAY-FROM-ZERO REMAINDER RM
           DISPLAY "AFZN   " Q4 " " RM
           DIVIDE N9 BY FB GIVING Q4 REMAINDER RM
           DISPLAY "TRUNC  " Q4 " " RM
           DIVIDE FUNCTION NUMVAL("0.12340000000001") BY 1 GIVING Q4 ROUNDED MODE AWAY-FROM-ZERO REMAINDER RM
           DISPLAY "NUMVAL " Q4 " " RM
           DIVIDE FUNCTION ACOS(-1) BY 1 GIVING Q4 ROUNDED REMAINDER RM
           DISPLAY "ACOS   " Q4 " " RM
           MOVE 9.9999 TO Q4
           MOVE 9.999999999 TO RM
           DIVIDE FL BY FZ GIVING Q4 ROUNDED REMAINDER RM
               ON SIZE ERROR DISPLAY "ZERO   SIZE ERROR " Q4 " " RM
               NOT ON SIZE ERROR DISPLAY "ZERO   NO SIZE ERROR " Q4 " " RM
           END-DIVIDE
           STOP RUN.
