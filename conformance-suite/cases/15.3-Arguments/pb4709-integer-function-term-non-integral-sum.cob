*>--- conformance-case
*> id: pb4709-integer-function-term-non-integral-sum
*> rules: [AR-15.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> PB4709 (the sibling arm of pb4709_integer_expression_argument) - ISO §15.3 item 6 (cite.py --check 15.3 OK): an Integer
      *> argument is "an arithmetic expression that will always result in an integer value". An INTEGER-type function-identifier is an
      *> integer for every reference (§15.2), so in I2 + 0.5 + FUNCTION INTEGER(S2) every term but 0.5 is an integer and the sum is
      *> never one (3 + 0.5 + 2 = 5.5): FACTORIAL's integer argument position (§15.36.3 1), cite.py --check 15.36.3 OK) refuses
      *> it - COBOL1627. The screen used to read the function term as unknown and fail open.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB4709FUNCSUM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I2    PIC 9(2) VALUE 3.
       01 S2    PIC 9V9 VALUE 2.5.
       01 T     PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE T = FUNCTION FACTORIAL(
               I2 + 0.5 + FUNCTION INTEGER(S2))
           STOP RUN.
