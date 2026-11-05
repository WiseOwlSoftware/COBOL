*>--- conformance-case
*> id: pb4635-baseconvert-parenthesized-argument-1
*> rules: [AR-15.12.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> PB4635 review R3-1 - ISO §15.12.3 r1: "Argument-1 is the input data item to be converted and shall be a usage display or national
      *> data item or literal". (U2) is an arithmetic expression enclosed in parentheses (§8.8.1.1), not the data item it encloses: the
      *> wrapper hid the item from the usage and unsigned-integer rules, and a signed or scaled item in parentheses converted to a wrong
      *> value or to an empty string where the bare spelling is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGBASECONVPARENARG1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U2    PIC 99 VALUE 12.
       01 T     PIC X(8).
       PROCEDURE DIVISION.
           MOVE FUNCTION BASECONVERT((U2) 10 16) TO T.
           STOP RUN.
