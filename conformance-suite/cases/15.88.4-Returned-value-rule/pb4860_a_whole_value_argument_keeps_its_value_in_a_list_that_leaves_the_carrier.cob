      *>--- conformance-case
      *> id: pb4860_a_whole_value_argument_keeps_its_value_in_a_list_that_leaves_the_carrier
      *> rules: [RV-15.88.4-1, RV-15.76.4-1, RV-15.62.4-1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4860 (the nested arm of PB4826): a native SUM, RANGE or MIDRANGE returns the value of its equivalent arithmetic
      *> expression (ISO 15.88.4 1), 15.76.4 1), 15.62.4 1)), which 15.4.1 leaves to the implementor as "an implementor-defined
      *> approximation of the value of that expression"; the precedence (CLAUDE.md rule 1) sends that latitude to GnuCOBOL's exact
      *> decimal temporary, and the receiver rounds ONCE (14.7.4.3). PB4826 made a list whose aligned bound leaves the Int128 carrier
      *> return that whole value. An ARGUMENT that is itself such a call is a whole value too, but it reached the outer call as an
      *> SDIDI operand, so the outer list took the SDIDI body and rounded the 35-digit value to 34 digits first.
      *> A = 12345678901234567890123456 (26 digits), B = .004999999, Z = 0. Values by Python Decimal, ROUND_HALF_UP:
      *>   S1  SUM (SUM (A B) Z) ROUNDED into 9(26)V99   A + B + Z = ...456.004999999     -> ...456.00  (the 34-digit value gave .01)
      *>   S2  A + B + Z ROUNDED, the expression itself the same                         -> ...456.00
      *>   R1  RANGE (SUM (A B) Z) ROUNDED into 9(26)V99 (A + B) - Z = ...456.004999999   -> ...456.00  (the 34-digit value gave .01)
      *>   D1  MIDRANGE (SUM (A B) Z) ROUNDED into 9(25)V999  (A + B + Z) / 2 = ...728.0024999995 -> ...728.002 (the 34-digit value gave .003)
      *>   D2  DISPLAY SUM (SUM (A B) Z), the same value on the device channel           -> ...456.004999999
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4860NST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(31) VALUE 12345678901234567890123456.
       01 B PIC V9(9) VALUE 0.004999999.
       01 Z PIC V9 VALUE 0.
       01 R PIC 9(26)V99.
       01 H PIC 9(25)V999.
       PROCEDURE DIVISION.
           COMPUTE R ROUNDED = FUNCTION SUM (FUNCTION SUM (A B) Z)
           DISPLAY "S1 " R
           COMPUTE R ROUNDED = A + B + Z
           DISPLAY "S2 " R
           COMPUTE R ROUNDED = FUNCTION RANGE (FUNCTION SUM (A B) Z)
           DISPLAY "R1 " R
           COMPUTE H ROUNDED = FUNCTION MIDRANGE (FUNCTION SUM (A B) Z)
           DISPLAY "D1 " H
           DISPLAY "D2 " FUNCTION SUM (FUNCTION SUM (A B) Z)
           STOP RUN.
       END PROGRAM PB4860NST.
