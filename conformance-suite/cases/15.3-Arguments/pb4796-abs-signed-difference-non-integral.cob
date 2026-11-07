*>--- conformance-case
*> id: pb4796-abs-signed-difference-non-integral
*> rules: [AR-15.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> PB4796 (review R2-1 of PB4757) - ISO §2023:15.3 item 6 (cite.py --check 15.3 OK): an Integer argument is "an arithmetic
      *> expression that will always result in an integer value". FUNCTION ABS of a negative argument is its negation (§2023:15.7.4 1) b),
      *> cite.py --check 15.7.4 OK: "When the value of argument-1 is negative"), so G2 - FUNCTION ABS (G2) over the SIGNED item G2
      *> is 0 where G2 is not negative and 2 * G2 where it is: -0.4 for G2 = -0.2, not an integer. FACTORIAL's argument position
      *> (§2023:15.36.3 1), cite.py --check 15.36.3 OK) refuses it - COBOL1627, whatever value G2 holds at this reference. The screen read the
      *> call as G2 alone, netted the difference to 0 and failed open; over an UNSIGNED item the difference is always 0 and legal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB4796SGNDIFF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G2   PIC S9V9 VALUE 0.2.
       01 T    PIC 9(9).
       PROCEDURE DIVISION.
           COMPUTE T = FUNCTION FACTORIAL (G2 - FUNCTION ABS (G2))
           STOP RUN.
