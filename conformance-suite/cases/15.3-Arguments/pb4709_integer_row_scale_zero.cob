*>--- conformance-case
*> id: pb4709_integer_row_scale_zero
*> rules: [AR-15.3-6]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB4709 review round 1 - ISO §15.2 item 5 (cite.py --check 15.2 OK): "An integer function has an operational sign and no digits
      *> to the right of the decimal point." §15.3 item 6 (cite.py --check 15.3 OK) makes "an arithmetic expression that will always
      *> result in an integer value" an Integer argument, and the §15.7.1 result table (cite.py --check 15.7.1 OK) gives ABS of an
      *> Integer argument the Integer type, as the sibling tables give MAX, MIN, RANGE and SUM. So an argument integral by VALUE but
      *> WRITTEN with fraction digits (I2 + 1.0, 1.5 + 0.5, 4 / 2, I2 ** 2.0, 1.0E0 + I2) selects the Integer row, and the returned
      *> value has no digits to the right of the decimal point: a MOVE of it to an alphanumeric receiver (§14.9.25.3 SR10, Table 16: a Noninteger numeric sender does not
      *> move there, an Integer one does; cite.py --check 14.9.25.3 OK) delivers the digits of the integer, "4", never "4.0". The
      *> receivers are PIC X(3) so that a stored fraction ("4.0") shows instead of being cut off by the receiver's width. The
      *> multi-receiver form moves the one value "to the data item referenced by each identifier-2" (§14.9.25.4 GR1, cite.py --check
      *> 14.9.25.4 OK), so the two forms may not differ.
      *>
      *> A COUNTER REGISTER is an unsigned integer data item (§8.4.3.14.4 GR1), so ABS(LINAGE-COUNTER OF F + 1) is an Integer function
      *> too, and the product with 0 is 0 whatever the counter holds (FACTORIAL(0) = 1). ABS(S2) equals S2 for every value of an
      *> UNSIGNED S2, so S2 - FUNCTION ABS(S2) is always 0, an integer (§15.3 item 6), and FACTORIAL(0) = 1.
      *>
      *> EXPECTED VALUES, I2 = 3: ABS(3 + 1) = 4; ABS(1.5 + 0.5) = 2; MAX(3 + 1, 1) = 4; ABS(3 * 2) = 6; SUM(3 + 1, 1) = 5;
      *> ABS(ABS(3 + 1) * 2) = 8; ABS(4 / 2) = 2; MIN(3 ** 2, 3) = 3; ABS(1 + 3) = 4; ABS(-2 * 3) = 6; FACTORIAL(ABS(3 - 3 + 1)) = 1.
      *> The bare literal 1.0 and the parenthesized (1.0) are a literal operand read by its FORM (§8.3.3.3.2: an integer literal
      *> "contains no decimal point"), so they keep the Numeric row and are refused as a MOVE sender (pb4709-abs-parenthesized-fraction-literal).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4709SCALEZERO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb4709scalezero.txt".
       DATA DIVISION.
       FILE SECTION.
       FD F LINAGE IS 10 LINES.
       01 R    PIC X(10).
       WORKING-STORAGE SECTION.
       01 I2   PIC 9(2) VALUE 3.
       01 S2   PIC 9V9 VALUE 2.5.
       01 A3   PIC X(3).
       01 A3B  PIC X(3).
       01 T    PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION ABS(I2 + 1.0) TO A3
           DISPLAY "ADD [" A3 "]"
           MOVE FUNCTION ABS(I2 + 1.0) TO A3 A3B
           DISPLAY "TWO [" A3 "] [" A3B "]"
           MOVE FUNCTION ABS(1.5 + 0.5) TO A3
           DISPLAY "FOLD [" A3 "]"
           MOVE FUNCTION MAX(I2 + 1.0 1) TO A3
           DISPLAY "MAX [" A3 "]"
           MOVE FUNCTION ABS(I2 * 2.0) TO A3
           DISPLAY "MUL [" A3 "]"
           MOVE FUNCTION SUM(I2 + 1.0 1) TO A3
           DISPLAY "SUM [" A3 "]"
           MOVE FUNCTION ABS(FUNCTION ABS(I2 + 1.0) * 2) TO A3
           DISPLAY "NEST [" A3 "]"
           MOVE FUNCTION ABS(4 / 2) TO A3
           DISPLAY "DIV [" A3 "]"
           MOVE FUNCTION MIN(I2 ** 2.0 3) TO A3
           DISPLAY "MIN [" A3 "]"
           MOVE FUNCTION ABS(1.0E0 + I2) TO A3
           DISPLAY "FLOAT [" A3 "]"
           MOVE FUNCTION ABS(-2.0 * I2) TO A3
           DISPLAY "NEG [" A3 "]"
           COMPUTE T = FUNCTION FACTORIAL(FUNCTION ABS(I2 - 3.0 + 1.0))
           DISPLAY "F3=" T
           COMPUTE T = FUNCTION FACTORIAL(
               FUNCTION ABS(LINAGE-COUNTER OF F + 1) * 0)
           DISPLAY "COUNTER=" T
           COMPUTE T = FUNCTION FACTORIAL(S2 - FUNCTION ABS(S2))
           DISPLAY "OPAQUE=" T
           STOP RUN.
       END PROGRAM PB4709SCALEZERO.
