*>--- conformance-case
*> id: pb4795-max-identity-sum-non-integral
*> rules: [AR-15.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> PB4795 - ISO §15.3 item 6 (cite.py --check 15.3 OK): an Integer argument is "an arithmetic expression that will always
      *> result in an integer value". FUNCTION MAX of ONE argument returns that argument (§15.59.4 1), cite.py --check 15.59.4 OK:
      *> "The returned value is the content of the argument-1 having the greatest value"), so S2 + FUNCTION MAX (S2) over the
      *> item S2 is 2 * S2, which is 0.4 for S2 = 0.2: not an integer. FACTORIAL's argument position (§15.36.3 1), cite.py
      *> --check 15.36.3 OK) refuses it - COBOL1627. The screen read only FUNCTION ABS of a bare item and failed open here.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB4795MAX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S2   PIC 9V9 VALUE 0.2.
       01 T    PIC 9(9).
       PROCEDURE DIVISION.
           COMPUTE T = FUNCTION FACTORIAL (S2 + FUNCTION MAX (S2))
           STOP RUN.
