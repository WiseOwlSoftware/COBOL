      *>--- conformance-case
      *> id: pb6266_unsigned_wide_scaled_float_arguments
      *> rules: [RV-15.84.4-4, RV-15.9.4-1, RV-15.11.4-1]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6266 sweep - a numeric function argument held in a
      *> SCALED unsigned 16-byte COMP-5 item (PIC 9(18)V99: 20 digits,
      *> past the 18 that a 64-bit carrier holds) is converted to
      *> binary64 by value; its USAGE decides nothing the function
      *> rules state (§2023:15.3 puts the rules on the argument's
      *> class and value).
      *> §2023:15.84.4 RV4: "When native arithmetic is in effect, the
      *> returned value is the absolute value of the approximation of
      *> the square root of argument-1."  SQRT(2.25) is exactly 1.5.
      *> §2023:15.11.4 RV1: the returned value is the approximation of
      *> the arctangent of argument-1; ATAN(0) is 0.
      *> §2023:15.9.4 RV1a: "When the value of argument-1 is zero,
      *> (1 / (argument-2))": ANNUITY(0, 4) = 1/4 = 0.25.
      *> Each answer is exact in binary64 and is printed at two
      *> places, so no approximation shows.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6266FLT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SQ   PIC 9(18)V99 COMP-5 VALUE 2.25.
       01 ZR   PIC 9(18)V99 COMP-5 VALUE 0.
       01 N4   PIC 9(20) COMP-5 VALUE 4.
       01 R    PIC 9.99.
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION SQRT (SQ)
           DISPLAY "SQRT    " R
           COMPUTE R = FUNCTION ATAN (ZR)
           DISPLAY "ATAN    " R
           COMPUTE R = FUNCTION ANNUITY (ZR N4)
           DISPLAY "ANNUITY " R
           STOP RUN.
