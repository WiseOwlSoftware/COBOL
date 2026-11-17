*>--- conformance-case
*> id: pb5805-baseconvert-plus-signed-literal
*> rules: [AR-15.12.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> PB5805 (its sibling sweep) - ISO §2023:15.12.3 AR1: argument-1, "if the base specified in argument-2 is less than 11, shall
      *> also be an unsigned integer data item or literal". §2023:8.3.3.3.2 SR2: "If a sign is used, it shall appear as the leftmost
      *> character of the literal", so +101 is a signed literal though its value is positive; a compiler that judges the value
      *> (or asks only for '-') accepts it and fails. Refused with COBOL1642.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGBASECONVPLUSLIT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T     PIC X(8).
       PROCEDURE DIVISION.
           MOVE FUNCTION BASECONVERT(+101 2 10) TO T.
           STOP RUN.
