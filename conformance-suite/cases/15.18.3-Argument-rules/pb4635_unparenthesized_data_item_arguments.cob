*>--- conformance-case
*> id: pb4635_unparenthesized_data_item_arguments
*> rules: [AR-15.18.3-1, AR-15.12.3-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4635 review R3-1. ISO §2023:15.18.3 r1: "Argument-1 and argument-2 shall be data items or literals of class alphabetic,
      *> alphanumeric, boolean, numeric or national." (cite.py --check OK) and §2023:15.12.3 r1: "Argument-1 is the input data item to be
      *> converted and shall be a usage display or national data item or literal" (cite.py --check OK). The refusals of the parenthesized
      *> spellings are the pb4635-concat-parenthesized-item and pb4635-baseconvert-parenthesized-argument-1 cases; this case holds the
      *> other side, so the shape rule cannot grow into a refusal of a legal argument: an unsigned integer item, a reference-modified
      *> item, a group item, a nested function-identifier (§2023:8.4.3.2.4 GR1: a temporary data item) and literals of every written class.
      *>
      *> EXPECTED VALUES: U2 = 12 concatenates as its two digits (§2023:15.18.4 r1), AB(2:2) of "WXYZ" is "XY", UPPER-CASE("ab") is "AB", the
      *> group GH is "GH", so "12-XYABGH"; 12 in base 16 is "C" and the literal 255 in base 16 is "FF" (§2023:15.12.4).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4635DATAITEMARGS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U2 PIC 99 VALUE 12.
       01 AB PIC X(4) VALUE "WXYZ".
       01 GH.
          05 FILLER PIC X VALUE "G".
          05 FILLER PIC X VALUE "H".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "CONCAT=" FUNCTION CONCAT(U2 "-" AB(2:2) FUNCTION UPPER-CASE("ab") GH)
           DISPLAY "BASE16=" FUNCTION BASECONVERT(U2 10 16)
           DISPLAY "LITERAL=" FUNCTION BASECONVERT(255 10 16)
           STOP RUN.
       END PROGRAM PB4635DATAITEMARGS.
