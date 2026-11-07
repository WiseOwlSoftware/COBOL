*>--- conformance-case
*> id: pb2079-baseconvert-expression-base
*> rules: [AR-15.12.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> PB2079 / PB4507 - ISO §2023:15.12.3 r1: argument-2 and argument-3 "shall be positive nonzero numeric integer literals or data
      *> items". An arithmetic expression is neither a literal nor a data item (a function-identifier IS one, §2023:8.4.3.2.4 r1, and
      *> is admitted: positive/pb4507_baseconvert_function_bases). This replaces the earlier negative that wrote a nested function
      *> and so refused legal source.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2079EXPR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I8   PIC 99 VALUE 8.
       01 R    PIC X(40).
       PROCEDURE DIVISION.
           MOVE FUNCTION BASECONVERT("1A" I8 + 8 10) TO R
           DISPLAY "R=" R
           STOP RUN.
