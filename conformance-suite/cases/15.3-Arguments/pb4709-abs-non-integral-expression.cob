*>--- conformance-case
*> id: pb4709-abs-non-integral-expression
*> rules: [AR-15.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> PB4709 (the refusal that bounds pb4709_integer_expression_argument) - ISO §15.7.1 (cite.py --check 15.7.1 OK): ABS of a
      *> NUMERIC argument is a NUMERIC function. S2 + 1 over PIC 9V9 is an arithmetic expression that does NOT always result in an
      *> integer (§15.3 item 6; S2 = 2.5 gives 3.5), so ABS(S2 + 1) stays on the Numeric row, and FACTORIAL's integer argument position
      *> (§15.36.3 1)) bars a numeric function (§8.4.3.2.3 SR11) whatever this reference would yield - COBOL1627.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB4709NONINT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S2    PIC 9V9 VALUE 2.5.
       01 T     PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE T = FUNCTION FACTORIAL(FUNCTION ABS(S2 + 1))
           STOP RUN.
