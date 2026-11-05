*>--- conformance-case
*> id: pb4757-abs-unsigned-sum-non-integral
*> rules: [AR-15.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> PB4757 - ISO §15.3 item 6 (cite.py --check 15.3 OK): an Integer argument is "an arithmetic expression that will always
      *> result in an integer value". FUNCTION ABS of an item that cannot hold a negative value is that item (§15.7.4 1) a), cite.py
      *> --check 15.7.4 OK: "When the value of argument-1 is zero or positive, (argument-1)"), so S2 + FUNCTION ABS (S2) over the
      *> unsigned S2 PIC 9V9 is 2 * S2, which is 0.4 for S2 = 0.2: not an integer. FACTORIAL's argument position (§15.36.3 1), cite.py
      *> --check 15.36.3 OK) refuses it - COBOL1627. The screen used to mark every item beneath ANY call opaque and fail open here.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB4757ABSSUM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S2   PIC 9V9 VALUE 0.2.
       01 T    PIC 9(9).
       PROCEDURE DIVISION.
           COMPUTE T = FUNCTION FACTORIAL (S2 + FUNCTION ABS (S2))
           STOP RUN.
