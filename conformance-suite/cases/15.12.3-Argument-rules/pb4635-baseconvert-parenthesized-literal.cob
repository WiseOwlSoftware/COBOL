*>--- conformance-case
*> id: pb4635-baseconvert-parenthesized-literal
*> rules: [AR-15.12.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> PB4635 - ISO §2023:15.12.3 r1: "Argument-2 and argument-3 shall be positive nonzero numeric integer literals or data items ...". (10) is an
      *> arithmetic expression enclosed in parentheses (§2023:8.8.1.1), not the literal it encloses.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGBASECONVPARENLIT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T     PIC X(8).
       PROCEDURE DIVISION.
           MOVE FUNCTION BASECONVERT("1A" 16 (10)) TO T.
           STOP RUN.
