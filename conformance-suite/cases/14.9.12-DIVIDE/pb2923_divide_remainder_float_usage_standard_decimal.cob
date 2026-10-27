*>--- conformance-case
*> id: pb2923_divide_remainder_float_usage_standard_decimal
*> rules: [GR-14.9.12.4-5, GR-14.9.12.4-6, GR-14.9.12.4-7]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB2923 - DIVIDE ... GIVING identifier-3 REMAINDER identifier-4 WHEN IDENTIFIER-3 IS A BINARY
      *> FLOATING-POINT ITEM (FLOAT-LONG, FLOAT-SHORT). ARITHMETIC IS STANDARD-DECIMAL (the SDIDI twin of pb2923_divide_remainder_float_usage; the same values: the quotient and
      *> the remainder are SDIDI expressions, 14.9.12.4 1) and 7), and the subsidiary quotient is the SDIDI quotient truncated into the format).
      *>
      *> 14.9.12.4 6) b): "The quotient is then formed as specified in General rule 1 and stored in identifier-3 as
      *> specified in 14.7.7" - the REMAINDER phrase does not change what identifier-3 holds. 6) c): "a subsidiary
      *> quotient is developed that is signed and derived from the quotient by truncation of digits at the least
      *> significant end and that has the same number of digits and the same decimal point location as the data item
      *> referenced by identifier-3". A binary floating-point item has neither a digit count nor a fixed decimal point,
      *> only a format, so the subsidiary quotient is the quotient truncated (toward zero) into that format; 7): "the
      *> remainder is the result of multiplying the subsidiary quotient and the divisor and subtracting the product
      *> from the dividend". Before PB2923 the quotient was truncated at a FIXED scale of 0 (FLOAT-LONG, FLOAT-SHORT), so
      *> the whole quotient below 1 was lost (the remainder was the whole dividend) and the quotient STORED was an integer.
      *>
      *> The floating-point items are read through MOVE to a fixed-point item, which converts the item's exact value
      *> and truncates (14.6.8.2 and the documented conversion manner, docs/CONFORMANCE.md, kb/Work PB77), so the
      *> expectations do not depend on how DISPLAY writes a float.
      *> Worked by hand:
      *>   B1  1000 / 3: the binary64 nearest 333.33333... is 333.33333333333331438...; MOVEd to 9 places 333.333333333;
      *>       the remainder 1000 - 3 * 333.33333333333331438 = 5.7E-14 is 0.000000 at 6 places.
      *>   B2  1 / 3E24: 3.3333...E-25, a positive binary64 (its first six decimals past the 24th are 333333); the
      *>       remainder 1 - 3.33...E-25 * 3E24 is 1.1E-16 or less, 0.00 at 2 places (the old compiler left 1.00).
      *>   B3  2 / 3 into FLOAT-SHORT: without ROUNDED the quotient is truncated into the format (14.7.4.3 2), 10)):
      *>       11184810 * 2**-24 = 0.66666662693023681640625 (the binary32 nearest, 11184811 * 2**-24, lies ABOVE 2/3);
      *>       MOVEd to 9 places 0.666666626; remainder 2 - 3 * 0.66666662693023681640625 = 2**-23 =
      *>       0.00000011920928955078125.
      *>   B4  10 / 3 into FLOAT-SHORT: 13981013 * 2**-22 = 3.3333332538604736328125; MOVEd 3.333333253; remainder
      *>       10 - 3 * 3.3333332538604736328125 = 0.0000002384185791015625.
      *>   B5  -2 / 3: the same, negated.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2923FLSD.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FL   USAGE FLOAT-LONG.
       01 FS   USAGE FLOAT-SHORT.
       01 HUGE PIC 9(25) VALUE 3000000000000000000000000.
       01 T9   PIC S9(3)V9(9).
       01 T30  PIC V9(30).
       01 R2   PIC 9V99.
       01 R5   PIC S9(5)V9(6).
       01 R30  PIC S9V9(30).
       PROCEDURE DIVISION.
       MAIN.
           DIVIDE 1000 BY 3 GIVING FL REMAINDER R5.
           MOVE FL TO T9.
           DISPLAY "B1 [" T9 "] [" R5 "]".
           DIVIDE 1 BY HUGE GIVING FL REMAINDER R2.
           MOVE FL TO T30.
           DISPLAY "B2 [" T30 "] [" R2 "]".
           DIVIDE 2 BY 3 GIVING FS REMAINDER R30.
           MOVE FS TO T9.
           DISPLAY "B3 [" T9 "] [" R30 "]".
           DIVIDE 10 BY 3 GIVING FS REMAINDER R30.
           MOVE FS TO T9.
           DISPLAY "B4 [" T9 "] [" R30 "]".
           DIVIDE -2 BY 3 GIVING FS REMAINDER R30.
           MOVE FS TO T9.
           DISPLAY "B5 [" T9 "] [" R30 "]".
           STOP RUN.
       END PROGRAM PB2923FLSD.
