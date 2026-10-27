*>--- conformance-case
*> id: pb2923_divide_remainder_float_edited_standard_decimal
*> rules: [GR-14.9.12.4-5, GR-14.9.12.4-6, GR-14.9.12.4-7]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB2923 - DIVIDE ... GIVING identifier-3 REMAINDER identifier-4 WHEN IDENTIFIER-3 IS A FLOATING-POINT
      *> NUMERIC-EDITED ITEM (PIC +9.99E+99: a three-digit significand and an exponent). ARITHMETIC IS STANDARD-DECIMAL (the SDIDI twin of pb2923_divide_remainder_float_edited; the same values: the quotient and
      *> the remainder are SDIDI expressions, 14.9.12.4 1) and 7)).
      *>
      *> 14.9.12.4 6) b): "The quotient is then formed as specified in General rule 1 and stored in identifier-3 as
      *> specified in 14.7.7" - the REMAINDER phrase does not change what identifier-3 holds. 6) c): "a subsidiary
      *> quotient is developed that is signed and derived from the quotient by truncation of digits at the least
      *> significant end and that has the same number of digits and the same decimal point location as the data item
      *> referenced by identifier-3". A floating-point numeric-edited item has no fixed decimal point; its value is a
      *> significand of three digits, normalized so its leading digit is nonzero (14.6.8.4 1)), at an exponent, so the
      *> subsidiary quotient is the quotient truncated to THREE significant digits at its own exponent. 7): "the
      *> remainder is the result of multiplying the subsidiary quotient and the divisor and subtracting the product
      *> from the dividend".
      *>
      *> Worked by hand (quotient -> stored FE; subsidiary quotient; remainder = dividend - subsidiary * divisor):
      *>   A1  1 / 3E24       -> +3.33E-25 ; 3.33E-25 ; 1 - 3.33E-25 * 3E24 = 1 - 0.999 = 0.001 -> PIC 9V99: 0.00
      *>   A2  10 / 3         -> +3.33E+00 ; 3.33     ; 10 - 9.99    = 0.01
      *>   A3  1000 / 3       -> +3.33E+02 ; 333      ; 1000 - 999   = 1
      *>   A4  1 / 3000       -> +3.33E-04 ; 0.000333 ; 1 - 0.999    = 0.001
      *>   A5  -10 / 3        -> -3.33E+00 ; -3.33    ; -10 - (-9.99) = -0.01
      *>   A6  2 / 3 ROUNDED  -> +6.67E-01 (14.7.4.3 4) rounds the STORED value) ; the subsidiary quotient is TRUNCATED,
      *>                         6.66E-01 ; 2 - 1.998 = 0.002
      *>   A7  DIVIDE 3 INTO 10 (Format 5) is A2
      *>   A8  0 / 3          -> +0.00E+00 ; 0 ; 0
      *>   A9  5E12 / 1 into +9.99E+9: the quotient is farther from zero than the item can hold, so the size
      *>       error condition exists (14.7.5 case 3); with the SIZE ERROR phrase "the values of all of the
      *>       resultant data items remain unchanged" and 6) c) stores identifier-4 only "if the size error
      *>       condition is not raised": both keep their sentinels.
      *> Each leg fails on the old compiler: A1 and A4 took the mask's fixed hint as a decimal point location, so
      *> the subsidiary quotient was 0 (A1: remainder 1.00, the whole dividend) and the quotient STORED was the
      *> truncated one (A1 +0.00E+00, A4 +3.30E-04); A2, A3 and A5 truncated the subsidiary quotient at 5 places
      *> (a remainder of 0.00001 where the rule gives 0.01, 1 and -0.01).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2923FESD.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FE   PIC +9.99E+99.
       01 FE1  PIC +9.99E+9 VALUE 2.0E0.
       01 HUGE PIC 9(25) VALUE 3000000000000000000000000.
       01 BIG  PIC 9(13) VALUE 5000000000000.
       01 R2   PIC 9V99.
       01 R5   PIC S9(5)V9(6) VALUE 7.
       PROCEDURE DIVISION.
       MAIN.
           DIVIDE 1 BY HUGE GIVING FE ROUNDED REMAINDER R2.
           DISPLAY "A1 [" FE "] [" R2 "]".
           DIVIDE 10 BY 3 GIVING FE REMAINDER R5.
           DISPLAY "A2 [" FE "] [" R5 "]".
           DIVIDE 1000 BY 3 GIVING FE REMAINDER R5.
           DISPLAY "A3 [" FE "] [" R5 "]".
           DIVIDE 1 BY 3000 GIVING FE REMAINDER R5.
           DISPLAY "A4 [" FE "] [" R5 "]".
           DIVIDE -10 BY 3 GIVING FE REMAINDER R5.
           DISPLAY "A5 [" FE "] [" R5 "]".
           DIVIDE 2 BY 3 GIVING FE ROUNDED REMAINDER R5.
           DISPLAY "A6 [" FE "] [" R5 "]".
           DIVIDE 3 INTO 10 GIVING FE REMAINDER R5.
           DISPLAY "A7 [" FE "] [" R5 "]".
           DIVIDE 0 BY 3 GIVING FE REMAINDER R5.
           DISPLAY "A8 [" FE "] [" R5 "]".
           MOVE 7 TO R5.
           DIVIDE BIG BY 1 GIVING FE1 REMAINDER R5
               ON SIZE ERROR DISPLAY "A9 SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "A9 WRONG - NO SIZE ERROR".
           DISPLAY "A9 [" FE1 "] [" R5 "]".
           STOP RUN.
       END PROGRAM PB2923FESD.
