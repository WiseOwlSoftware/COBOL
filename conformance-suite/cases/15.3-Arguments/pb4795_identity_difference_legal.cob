*>--- conformance-case
*> id: pb4795_identity_difference_legal
*> rules: [AR-15.3-6]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB4795 / PB4796 - the sound half of the type 6 screen (ISO §15.3 item 6, cite.py --check 15.3 OK: "an arithmetic expression
      *> that will always result in an integer value"). A one-argument FUNCTION MAX returns its argument (§15.59.4 1), cite.py
      *> --check 15.59.4 OK) and FUNCTION ABS of a zero or positive argument is the argument (§15.7.4 1) a), cite.py --check 15.7.4 OK),
      *> so over an item that is never negative each difference below is 0, and over a signed item the MAX difference is 0
      *> on every valuation. The reading must not make the screen refuse them: FACTORIAL (0) is 1 (§15.36.4 1) a), cite.py --check 15.36.4 OK: "When the value of argument-1 is 0 or 1").
      *>
      *> EXPECTED: four calls, each FACTORIAL (0) -> 000000001 four times.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4795IDENTITYDIFF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S2   PIC 9V9 VALUE 0.2.
       01 G2   PIC S9V9 VALUE -0.2.
       01 T    PIC 9(9).
       PROCEDURE DIVISION.
           COMPUTE T = FUNCTION FACTORIAL (S2 - FUNCTION MAX (S2))
           DISPLAY T
           COMPUTE T = FUNCTION FACTORIAL (S2 - FUNCTION ABS (S2 + 0))
           DISPLAY T
           COMPUTE T = FUNCTION FACTORIAL (S2 - FUNCTION ABS (- S2))
           DISPLAY T
           COMPUTE T = FUNCTION FACTORIAL (G2 - FUNCTION MAX (G2))
           DISPLAY T
           STOP RUN.
