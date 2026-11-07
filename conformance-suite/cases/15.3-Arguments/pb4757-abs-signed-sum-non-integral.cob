*>--- conformance-case
*> id: pb4757-abs-signed-sum-non-integral
*> rules: [AR-15.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> PB4757 review R1-1 (signed item: ABS is the item on its non-negative valuations and one witness suffices) - ISO §2023:15.3 item 6 (cite.py --check 15.3 OK): an Integer argument is "an arithmetic expression that will always
      *> result in an integer value". FUNCTION ABS of an item is that item where the item is not negative (§2023:15.7.4 1) a), cite.py
      *> --check 15.7.4 OK: "When the value of argument-1 is zero or positive, (argument-1)"), so S2 + FUNCTION ABS (S2) over the
      *> item S2 is 2 * S2, which is 0.4 for S2 = 0.2: not an integer. FACTORIAL's argument position (§2023:15.36.3 1), cite.py
      *> --check 15.36.3 OK) refuses it - COBOL1627. The screen used to mark every item beneath ANY call opaque and fail open here.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB4757ABSSGN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S2   PIC S9V9 VALUE 0.2.
       01 T    PIC 9(9).
       PROCEDURE DIVISION.
           COMPUTE T = FUNCTION FACTORIAL (S2 + FUNCTION ABS (S2))
           STOP RUN.
