      *>--- conformance-case
      *> id: g2-lvl-cd-records
      *> rules: [2002-GR-13.16.31.3-1, 2002-GR-13.16.31.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Three level-1 record description entries follow an OUTPUT CD
      *> (the CD facility draws obsolete warning COBOL0903).
      *> §2002:13.16.31.3 GR1 - "The level-number 1 identifies the first
      *> entry in each record description or report group." Each 01
      *> opens its own record: R1 is F1 + F2 = 5 + 18 = 23 positions,
      *> R2 is 3 and R3 is 4 (line LEN). Fails if a later 01 is folded
      *> into the record before it.
      *> §2002:13.16.31.3 GR3 - "Multiple level 1 entries subordinate to
      *> a CD, FD, or SD entry represent implicit redefinitions of the
      *> same area" (and §2002:A.1.2.2 SR9: they "implicitly redefine
      *> the record area established by the output CD entry"). All three
      *> start at the first position of one area:
      *> MOVE "ABCDEFGHIJ" TO R1 sets R1 to ABCDEFGHIJ and 13 spaces,
      *> so R2 = ABC, F1 = ABCDE, R3 = ABCD;
      *> MOVE "XY" TO R2 sets its 3 positions to "XY ", so R1 becomes
      *> "XY DEFGHIJ" and 13 spaces and R3 = "XY D".
      *> Fails if any record has an area of its own.
      *> The lengths are moved to PIC 99 items before DISPLAY so the
      *> implementor-defined DISPLAY form of an integer function
      *> (§2002:14.8.10.3 GR1) does not enter the expectation.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LVL03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-L1 PIC 99.
       01  WS-L2 PIC 99.
       01  WS-L3 PIC 99.
       COMMUNICATION SECTION.
       CD  COUT FOR OUTPUT.
       01  R1.
           05 F1 PIC X(5).
           05 F2 PIC X(18).
       01  R2 PIC X(3).
       01  R3.
           05 G3 PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE FUNCTION LENGTH(R1) TO WS-L1
           MOVE FUNCTION LENGTH(R2) TO WS-L2
           MOVE FUNCTION LENGTH(R3) TO WS-L3
           DISPLAY "LEN R1 " WS-L1 " R2 " WS-L2 " R3 " WS-L3
           MOVE "ABCDEFGHIJ" TO R1
           DISPLAY "R2=[" R2 "] F1=[" F1 "] R3=[" R3 "]"
           MOVE "XY" TO R2
           DISPLAY "R1=[" R1 "]"
           DISPLAY "R3=[" R3 "]"
           STOP RUN.
