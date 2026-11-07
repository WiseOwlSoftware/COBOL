*>--- conformance-case
*> id: pb4635-concat-parenthesized-item
*> rules: [AR-15.18.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> PB4635 review R3-1 - ISO §2023:15.18.3 r1: "Argument-1 and argument-2 shall be data items or literals of class alphabetic, alphanumeric,
      *> boolean, numeric or national." (U2) is an arithmetic expression enclosed in parentheses (§2023:8.8.1.1), not the data item it
      *> encloses, although class numeric is in the list: the wrapper hid the item from CONCAT's usage and sign rules (r3), and the
      *> argument compiled and printed the item's raw image where the bare spelling of a signed or scaled item is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGCONCATPARENITEM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U2    PIC 99 VALUE 12.
       01 T     PIC X(8).
       PROCEDURE DIVISION.
           MOVE FUNCTION CONCAT((U2) "A") TO T.
           STOP RUN.
