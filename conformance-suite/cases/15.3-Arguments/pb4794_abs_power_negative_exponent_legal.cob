*>--- conformance-case
*> id: pb4794_abs_power_negative_exponent_legal
*> rules: [AR-15.3-6]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB4794 review R1-1 - ISO §15.3 6) (cite.py --check 15.3 OK: "will always result in an integer value") and §15.7.4 1) a)
      *> (FUNCTION ABS of a zero or positive argument is "(argument-1)"). 2 ** -20 is a power to an integer exponent, not a nested
      *> quotient: the arithmetic stores it exactly (0.00000095367431640625, 20 fraction digits), so 2 ** -20 * 1048576 is exactly 1,
      *> the argument of ABS always results in an integer, ABS is an Integer function and FUNCTION FACTORIAL accepts it.
      *> A power must not be cut at the 14 fraction digits a nested QUOTIENT keeps (PB4794).
      *>
      *> EXPECTED: FACTORIAL (1) = 1 -> 1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4794POWNEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R    PIC 9(9).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION FACTORIAL (FUNCTION ABS (2 ** -20 * 1048576))
           DISPLAY R
           STOP RUN.
