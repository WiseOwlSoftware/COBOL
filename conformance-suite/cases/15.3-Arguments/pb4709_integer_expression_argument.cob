*>--- conformance-case
*> id: pb4709_integer_expression_argument
*> rules: [AR-15.3-6]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB4709 - ISO §15.7.1 (cite.py --check 15.7.1 OK): "The type of this function depends on the argument type as follows: Integer
      *> -> Integer, Numeric -> Numeric", and §15.3 item 6 (cite.py --check 15.3 OK): an Integer argument is "an arithmetic expression
      *> that will always result in an integer value or an integer data item". So ABS, MAX and MIN over an arithmetic expression that
      *> ALWAYS results in an integer - not only a bare integer item, a literal or a parenthesized one (pb4635_parenthesized_numeric_
      *> arguments) - are INTEGER functions, and an integer position may hold them. FACTORIAL's argument-1 "shall be an integer greater
      *> than or equal to zero" (§15.36.3 1), cite.py --check 15.36.3 OK), and §8.4.3.2.3 SR11 bars a NUMERIC function from such a
      *> position even when a particular reference would yield an integer; PERFORM ... TIMES asks the same of its count (§14.9.28.3 SR2).
      *> The refusal that bounds the admission is negative/pb4709-abs-non-integral-expression: S2 + 1 is not always an integer.
      *>
      *> EXPECTED VALUES, I2 = 3 and S2 = 2.5. I2 + 1 = 4, so FACTORIAL(ABS(4)) = 24; - I2 = -3, ABS = 3, FACTORIAL = 6; MAX(I2 + 1, 1)
      *> = 4, so the PERFORM body runs 4 times; MIN(I2 * 2 - 1, I2 + 7) = MIN(5, 10) = 5, FACTORIAL(5) = 120; INTEGER(2.5) is the
      *> greatest integer not greater than the argument, 2, so ABS(INTEGER(S2) + 1) = 3 and FACTORIAL(3) = 6 (an integer FUNCTION
      *> inside the expression is an integer for every reference, §15.2 type 5); ABS(I2 - 5) = 2, FACTORIAL(2) = 2; 2 ** 2 - 7 = -3,
      *> ABS = 3, FACTORIAL(3) = 6.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4709INTEXPR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I2   PIC 9(2) VALUE 3.
       01 S2   PIC 9V9 VALUE 2.5.
       01 T    PIC 9(4).
       01 CNT  PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE T = FUNCTION FACTORIAL(FUNCTION ABS(I2 + 1))
           DISPLAY "ADD=" T
           COMPUTE T = FUNCTION FACTORIAL(FUNCTION ABS(- I2))
           DISPLAY "NEGATE=" T
           PERFORM FUNCTION MAX(I2 + 1 1) TIMES
               ADD 1 TO CNT
           END-PERFORM
           DISPLAY "TIMES=" CNT
           COMPUTE T = FUNCTION FACTORIAL(FUNCTION MIN(I2 * 2 - 1 I2 + 7))
           DISPLAY "MIN=" T
           COMPUTE T = FUNCTION FACTORIAL(FUNCTION ABS(FUNCTION INTEGER(S2) + 1))
           DISPLAY "FUNCTION=" T
           COMPUTE T = FUNCTION FACTORIAL(FUNCTION ABS(I2 - 5))
           DISPLAY "SUBTRACT=" T
           COMPUTE T = FUNCTION FACTORIAL(FUNCTION ABS(2 ** 2 - 7))
           DISPLAY "POWER=" T
           STOP RUN.
       END PROGRAM PB4709INTEXPR.
