*>--- conformance-case
*> id: pb4709-counter-register-non-integral-sum
*> rules: [AR-15.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> PB4709 review round 1 (the sibling arm of pb4709_integer_row_scale_zero) - ISO §15.3 item 6 (cite.py --check 15.3 OK): an
      *> Integer argument is "an arithmetic expression that will always result in an integer value". LINAGE-COUNTER is an unsigned
      *> integer data item (§8.4.3.14.4 GR1), so every term of LINAGE-COUNTER OF F + 0.5 but 0.5 is an integer and the sum is never
      *> one: FACTORIAL's integer argument position (§15.36.3 1), cite.py --check 15.36.3 OK) refuses it - COBOL1627. The screen used
      *> to read the register as an unknown term and fail open.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB4709COUNTERSUM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "negpb4709countersum.txt".
       DATA DIVISION.
       FILE SECTION.
       FD F LINAGE IS 10 LINES.
       01 R    PIC X(10).
       WORKING-STORAGE SECTION.
       01 T    PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE T = FUNCTION FACTORIAL(LINAGE-COUNTER OF F + 0.5)
           STOP RUN.
