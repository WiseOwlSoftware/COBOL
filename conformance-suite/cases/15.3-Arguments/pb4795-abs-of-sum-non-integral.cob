*>--- conformance-case
*> id: pb4795-abs-of-sum-non-integral
*> rules: [AR-15.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> PB4795 - ISO §2023:15.3 item 6 (cite.py --check 15.3 OK): an Integer argument is "an arithmetic expression that will always
      *> result in an integer value". FUNCTION ABS of a zero or positive argument is that argument (§2023:15.7.4 1) a), cite.py --check
      *> 15.7.4 OK: "When the value of argument-1 is zero or positive"), and S2 + 1 over the item S2 is positive wherever S2 is not
      *> negative, so S2 + FUNCTION ABS (S2 + 1) is 2 * S2 + 1, which is 1.4 for S2 = 0.2: not an integer. FACTORIAL's argument
      *> position (§2023:15.36.3 1), cite.py --check 15.36.3 OK) refuses it - COBOL1627. The screen read only FUNCTION ABS of a bare
      *> item and failed open here.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB4795ABSSUM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S2   PIC 9V9 VALUE 0.2.
       01 T    PIC 9(9).
       PROCEDURE DIVISION.
           COMPUTE T = FUNCTION FACTORIAL (S2 + FUNCTION ABS (S2 + 1))
           STOP RUN.
