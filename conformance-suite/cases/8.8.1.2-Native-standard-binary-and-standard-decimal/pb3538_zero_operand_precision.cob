      *>--- conformance-case
      *> id: pb3538_zero_operand_precision
      *> rules: [GR-8.8.1.2-3]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB3538 branch review finding R5-1: a ZERO operand
      *> carries its places like any other value. A function's
      *> returned value has the precision it is held at
      *> (CONFORMANCE.md DOC-A.1-123), and a sum is held at the larger
      *> of its operands' places, a zero operand included, as
      *> 8.8.1.5.3 performs the sum by ISO/IEC 60559 5.4.1 (the
      *> preferred exponent of an exact sum is the smaller one), so
      *> 0.000 + 2.5 is 2.500 (3 places) exactly as 0.001 + 2.5 is
      *> 2.501. The sum with a zero term used to be held at the other
      *> operand's places (1), so the outcome flipped on the VALUE of
      *> the operand. 8.8.4.2.4 compares the algebraic values "by the
      *> rules of native arithmetic"; 8.8.1.2 3) evaluates left to
      *> right: the quotient first, then the product.
      *>   ZITM ZSUM   ABS (Z + NUMVAL ("2.5")) with Z PIC 9V999 VALUE
      *>          0, and ABS (NUMVAL ("0.000") + NUMVAL ("2.5")):
      *>          2.500 at 3 places, w = 3 + 14 = 17, 2.500 / 3 is
      *>          0.83333333333333333, times 10 ** 15 is
      *>          833333333333333.33 > 833333333333333: GREATER (at
      *>          1 place, w = 15, it was 833333333333333: NOT GREATER);
      *>   NZ     ABS (NUMVAL ("0.001") + NUMVAL ("2.5")) = 2.501,
      *>          3 places: GREATER (the nonzero spelling never moved);
      *>   ZDIF   ABS (NUMVAL ("2.5") - Z) = 2.500, 3 places: GREATER;
      *>   ZCAN   ABS (NUMVAL ("1.250") - NUMVAL ("1.250") +
      *>          NUMVAL ("2.5")): the cancelled difference is 0.000,
      *>          held at 3 places, and so is the sum: GREATER;
      *>   ZMUL   ABS (Z * NUMVAL ("1.0") + NUMVAL ("2.5")): the zero
      *>          product is held at 3 + 1 = 4 places, the sum at 4,
      *>          w = 18: GREATER;
      *>   ZEQ    the ZITM spelling equals the
      *>          fixed-point 2.500 / 3 * 10 ** 15: EQUAL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3538ZP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Z    PIC 9V999 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF FUNCTION ABS (Z + FUNCTION NUMVAL ("2.5"))
                 / 3 * 1000000000000000 > 833333333333333
              DISPLAY "ZITM GREATER"
           ELSE
              DISPLAY "ZITM NOT GREATER"
           END-IF.
           IF FUNCTION ABS (FUNCTION NUMVAL ("0.000")
                 + FUNCTION NUMVAL ("2.5")) / 3 * 1000000000000000
                 > 833333333333333
              DISPLAY "ZSUM GREATER"
           ELSE
              DISPLAY "ZSUM NOT GREATER"
           END-IF.
           IF FUNCTION ABS (FUNCTION NUMVAL ("0.001")
                 + FUNCTION NUMVAL ("2.5")) / 3 * 1000000000000000
                 > 833333333333333
              DISPLAY "NZ GREATER"
           ELSE
              DISPLAY "NZ NOT GREATER"
           END-IF.
           IF FUNCTION ABS (FUNCTION NUMVAL ("2.5") - Z)
                 / 3 * 1000000000000000 > 833333333333333
              DISPLAY "ZDIF GREATER"
           ELSE
              DISPLAY "ZDIF NOT GREATER"
           END-IF.
           IF FUNCTION ABS (FUNCTION NUMVAL ("1.250")
                 - FUNCTION NUMVAL ("1.250")
                 + FUNCTION NUMVAL ("2.5")) / 3 * 1000000000000000
                 > 833333333333333
              DISPLAY "ZCAN GREATER"
           ELSE
              DISPLAY "ZCAN NOT GREATER"
           END-IF.
           IF FUNCTION ABS (Z * FUNCTION NUMVAL ("1.0")
                 + FUNCTION NUMVAL ("2.5")) / 3 * 1000000000000000
                 > 833333333333333
              DISPLAY "ZMUL GREATER"
           ELSE
              DISPLAY "ZMUL NOT GREATER"
           END-IF.
           IF FUNCTION ABS (Z + FUNCTION NUMVAL ("2.5"))
                 / 3 * 1000000000000000
                 = 2.500 / 3 * 1000000000000000
              DISPLAY "ZEQ EQUAL"
           ELSE
              DISPLAY "ZEQ NOT EQUAL"
           END-IF.
           STOP RUN.
