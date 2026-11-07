*>--- conformance-case
*> id: pb4635-baseconvert-parenthesized-bit-item
*> rules: [AR-15.12.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> PB4635 review R1-1 - ISO §2023:15.12.3 r1: "Argument-1 is the input data item to be converted and shall be a usage display or national
      *> data item or literal" (cite.py --check OK). BT is usage bit; (BT) is the boolean expression enclosing it (§2023:8.8.2), which holds the
      *> same bits, so BASECONVERT((BT) 10 16) is refused as BASECONVERT(BT 10 16) is.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGBASECONVPARENBIT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BT    PIC 1(4) USAGE BIT VALUE B"1010".
       01 T     PIC X(8).
       PROCEDURE DIVISION.
           MOVE FUNCTION BASECONVERT((BT) 10 16) TO T.
           STOP RUN.
