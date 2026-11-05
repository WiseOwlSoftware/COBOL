*>--- conformance-case
*> id: pb4635-convert-parenthesized-bit-item
*> rules: [AR-15.19.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> PB4635 review R1-2 - ISO §15.19.3 r5: "When the source-format is ANUM, argument-1 shall be a valid string of characters from the
      *> program's alphanumeric coded character set." (cite.py --check OK), a string BT (usage bit) does not hold; the enclosing
      *> parentheses of (BT) leave the representation as it is, so CONVERT((BT) ANUM NAT) is refused as CONVERT(BT ANUM NAT) is.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGCONVPARENBIT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BT    PIC 1(4) USAGE BIT VALUE B"1010".
       01 T     PIC N(8).
       PROCEDURE DIVISION.
           MOVE FUNCTION CONVERT((BT) ANUM NAT) TO T.
           STOP RUN.
