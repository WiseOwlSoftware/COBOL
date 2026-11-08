      *>--- conformance-case
      *> id: pb4975_float_edited_wide_significand_quotient
      *> rules: [DOC-A.1-123, GR-8.8.1.2-1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4975: a floating-point edited receiver may carry a
      *> 1 to 36 digit significand (13.18.40.3 SR15; cite.py OK), and
      *> the arithmetic value is rounded ONCE into the resultant
      *> (14.7.4.3 GR2 truncates with no ROUNDED phrase, GR4 rounds
      *> to the nearest value). A quotient reduced to 34 digits first
      *> rounded twice for a 33 to 36 digit significand.
      *> The exact quotient of 9.99...9E-1 (34 nines) by 2.0E0 is
      *> .49999999999999999999999999999999995 (35 digits).
      *>   F33  truncated at 33 digits: a 4 and 32 nines
      *>   F33R ROUNDED at 33 digits: the 34th digit is 9: up to .5
      *>   F34  truncated at 34 digits: a 4 and 33 nines
      *>   F34R ROUNDED at 34 digits: the 35th digit is an exact 5:
      *>        away from zero is up to .5
      *>   F35  holds all 35 digits; F36 holds them and a final 0
      *> The same quotient from an exact wide dividend, A * A / D with
      *> A = 123456789012345678901 and D = 7:
      *>   2.17736839331983382149106193793237093(...) E+39
      *> truncated at 36 digits ...093, rounded ...094; at 34
      *> digits ...370 and ...371.
      *> EXPECTED, DERIVED (Python Decimal and Fraction).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4975WIDEQUOT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A    PIC 9(21) VALUE 123456789012345678901.
       01 D    PIC 9 VALUE 7.
       01 F33  PIC +.9(33)E+99.
       01 F33R PIC +.9(33)E+99.
       01 F34  PIC +.9(34)E+99.
       01 F34R PIC +.9(34)E+99.
       01 F35  PIC +.9(35)E+99.
       01 F36  PIC +.9(36)E+99.
       01 E34  PIC +.9(34)E+99.
       01 E34R PIC +.9(34)E+99.
       01 E36  PIC +.9(36)E+99.
       01 E36R PIC +.9(36)E+99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE F33 = 9.999999999999999999999999999999999E-1 / 2.0E0
           DISPLAY "F33  " F33
           COMPUTE F33R ROUNDED =
               9.999999999999999999999999999999999E-1 / 2.0E0
           DISPLAY "F33R " F33R
           COMPUTE F34 = 9.999999999999999999999999999999999E-1 / 2.0E0
           DISPLAY "F34  " F34
           COMPUTE F34R ROUNDED =
               9.999999999999999999999999999999999E-1 / 2.0E0
           DISPLAY "F34R " F34R
           COMPUTE F35 = 9.999999999999999999999999999999999E-1 / 2.0E0
           DISPLAY "F35  " F35
           COMPUTE F36 = 9.999999999999999999999999999999999E-1 / 2.0E0
           DISPLAY "F36  " F36
           COMPUTE E34 = A * A / D
           DISPLAY "E34  " E34
           COMPUTE E34R ROUNDED = A * A / D
           DISPLAY "E34R " E34R
           COMPUTE E36 = A * A / D
           DISPLAY "E36  " E36
           COMPUTE E36R ROUNDED = A * A / D
           DISPLAY "E36R " E36R
           STOP RUN.
