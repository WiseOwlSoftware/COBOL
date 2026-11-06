      *>--- conformance-case
      *> id: pb4826_a_bounded_list_leaving_the_carrier_is_the_exact_value
      *> rules: [RV-15.88.4-1, RV-15.76.4-1, RV-15.61.4-2, RV-15.62.4-1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4826 (the bounded arm of PB4792): a native SUM, RANGE, MEDIAN or MIDRANGE whose arguments all have a digit bound but whose
      *> aligned bound leaves the Int128 carrier (a PIC 9(31) item beside a PIC V9(9) one aligns to 40 digits) returns the value of its
      *> equivalent arithmetic expression (ISO 15.88.4 1), 15.76.4 1), 15.61.4 2), 15.62.4 1)), which 15.4.1 leaves to the implementor
      *> as "an implementor-defined approximation of the value of that expression"; the precedence (CLAUDE.md rule 1) sends that latitude
      *> to GnuCOBOL's exact decimal temporary, and the receiver rounds ONCE (14.7.4.3). The value was rounded to the SDIDI's 34
      *> digits first, so a 35-digit value rounded at its last digit, and that rounding carried through the receiver's ROUNDED phrase.
      *> A = 12345678901234567890123456 (26 digits), B = .004999999, C = .005000001. Values by Python Decimal, ROUND_HALF_UP:
      *>   S1  SUM (A B) ROUNDED into 9(26)V99      A + B = ...456.004999999            -> ...456.00   (the 34-digit value .00500000 gave .01)
      *>   S2  A + B ROUNDED, the expression itself the same                            -> ...456.00
      *>   R1  RANGE (A C) ROUNDED into 9(26)V99    A - C = ...455.994999999            -> ...455.99   (the 34-digit value .99500000 gave 456.00)
      *>   D1  MIDRANGE (A B) ROUNDED into 9(25)V999 (A + B) / 2 = ...728.0024999995    -> ...728.002  (the 34-digit value .00250000 gave .003)
      *>   M1  MEDIAN (A B) ROUNDED into 9(25)V999, the mean of the two middle values    -> ...728.002
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4826BND.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(31) VALUE 12345678901234567890123456.
       01 B PIC V9(9) VALUE 0.004999999.
       01 C PIC V9(9) VALUE 0.005000001.
       01 R PIC 9(26)V99.
       01 H PIC 9(25)V999.
       PROCEDURE DIVISION.
           COMPUTE R ROUNDED = FUNCTION SUM (A B)
           DISPLAY "S1 " R
           COMPUTE R ROUNDED = A + B
           DISPLAY "S2 " R
           COMPUTE R ROUNDED = FUNCTION RANGE (A C)
           DISPLAY "R1 " R
           COMPUTE H ROUNDED = FUNCTION MIDRANGE (A B)
           DISPLAY "D1 " H
           COMPUTE H ROUNDED = FUNCTION MEDIAN (A B)
           DISPLAY "M1 " H
           STOP RUN.
       END PROGRAM PB4826BND.
