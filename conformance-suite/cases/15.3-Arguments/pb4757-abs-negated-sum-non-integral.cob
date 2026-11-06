*>--- conformance-case
*> id: pb4757-abs-negated-sum-non-integral
*> rules: [AR-15.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> PB4757 review R1-1 (ABS of the negated item is the item again) - ISO §15.3 item 6 (cite.py --check 15.3 OK): an Integer argument is "an arithmetic expression that will always
      *> result in an integer value". The argument - S2 over the item S2 is negative (or zero) whatever S2 holds, so FUNCTION ABS (- S2)
      *> is its negation, S2 (§15.7.4 1) b), cite.py --check 15.7.4 OK: "When the value of argument-1 is negative"; at zero, a)
      *> gives the same S2), so S2 + FUNCTION ABS (- S2) over the item S2 is 2 * S2, which is 0.4 for S2 = 0.2: not an integer. FACTORIAL's argument position (§15.36.3 1), cite.py
      *> --check 15.36.3 OK) refuses it - COBOL1627. The screen used to mark every item beneath ANY call opaque and fail open here.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB4757ABSNEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S2   PIC 9V9 VALUE 0.2.
       01 T    PIC 9(9).
       PROCEDURE DIVISION.
           COMPUTE T = FUNCTION FACTORIAL (S2 + FUNCTION ABS (- S2))
           STOP RUN.
