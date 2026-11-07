*>--- conformance-case
*> id: pb4635-convert-parenthesized-display-item-as-national
*> rules: [AR-15.19.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> PB4635 review R1-2 - ISO §2023:15.19.3 r6: "When the source-format is NAT, argument-1 shall be a valid string of national characters."
      *> (cite.py --check OK). BB is a usage display item, so CONVERT(BB NAT ANUM) is refused, and so is CONVERT((BB) NAT ANUM).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGCONVPARENDISP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BB    PIC 1(4) VALUE B"1010".
       01 T     PIC X(8).
       PROCEDURE DIVISION.
           MOVE FUNCTION CONVERT((BB) NAT ANUM) TO T.
           STOP RUN.
