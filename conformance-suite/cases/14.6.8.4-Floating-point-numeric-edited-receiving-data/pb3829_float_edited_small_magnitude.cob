*>--- conformance-case
*> id: pb3829_float_edited_small_magnitude
*> rules: [GR-14.6.8.4-1, GR-14.6.8.4-2, GR-14.7.4.3-2, GR-14.7.4.3-4,
*>    GR-14.7.4.3-8, GR-14.7.4.3-9, GR-14.9.8.4-2, RV-15.69.4-2]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3829's sweep: a float function's returned value, or a native power, stored into a FLOATING-POINT
*> numeric-edited receiver keeps every significant digit until the receiver's own store.
*> 14.6.8.4 1): "If the algebraic value of the sending operand is not zero, the exponent and significand of the value
*> are adjusted such that the most significant digit of the significand is not zero." so the four-digit significand of
*> FE (PIC +9.999E+99) holds the value's FIRST four significant digits, however small the value; 14.7.4.3 rounds
*> that significand at its last digit (2) no phrase = TRUNCATION, 4) ROUNDED, 8) TOWARD-GREATER, 9) TOWARD-LESSER).
*> Every leg, from the value:
*>   SQRT(1.5241383936E-24) = 1.23456E-12 (1.23456 squared is 1.5241383936): +1.234E-12, ROUNDED +1.235E-12, and the
*>     same through `+ 0`.
*>   2 ** -40 = 9.094947017729282...E-13: +9.094E-13, ROUNDED +9.095E-13.
*>   ATAN(1.2341E-11) = 1.2340999...E-11 (atan x = x - x**3/3 + ...): TOWARD-GREATER +1.235E-11.
*>   ATAN(1.2349E-11) = 1.2348999...E-11: TOWARD-LESSER +1.234E-11.
*>   ACOS(-1) = 3.14159265...: ROUNDED +3.142E+00 (past pi: 15.8.4 1) bounds the returned value, not the store).
*>   NUMVAL-F("1.5E-12"), the approximation of 1.5E-12 (15.69.4 2)): +1.500E-12.
*> WHY EACH LEG CAN FAIL: a landing that quantizes the value at a fixed working scale before the mask normalizes it
*> (nine places, the defect the sweep removed) prints +0.000E+00 for the SQRT, power, ATAN and NUMVAL-F legs (every one of
*> them below 1E-9); a clamp to the codomain prints +3.141E+00.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3829FLTEDIT.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 FE PIC +9.999E+99.
PROCEDURE DIVISION.
           COMPUTE FE = FUNCTION SQRT(1.5241383936E-24)
           DISPLAY "SQRT    " FE
           COMPUTE FE ROUNDED = FUNCTION SQRT(1.5241383936E-24)
           DISPLAY "SQRT-R  " FE
           COMPUTE FE ROUNDED = FUNCTION SQRT(1.5241383936E-24) + 0
           DISPLAY "SQRT-R0 " FE
           COMPUTE FE = 2 ** -40
           DISPLAY "POW     " FE
           COMPUTE FE ROUNDED = 2 ** -40
           DISPLAY "POW-R   " FE
           COMPUTE FE ROUNDED MODE TOWARD-GREATER = FUNCTION ATAN(0.000000000012341)
           DISPLAY "ATAN-TG " FE
           COMPUTE FE ROUNDED MODE TOWARD-LESSER = FUNCTION ATAN(0.000000000012349)
           DISPLAY "ATAN-TL " FE
           COMPUTE FE ROUNDED = FUNCTION ACOS(-1)
           DISPLAY "ACOS-R  " FE
           COMPUTE FE = FUNCTION NUMVAL-F("1.5E-12")
           DISPLAY "NUMVALF " FE
           STOP RUN.
