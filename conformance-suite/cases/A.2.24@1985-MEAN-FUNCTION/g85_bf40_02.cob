      *>--- conformance-case
      *> id: g85_bf40_02
      *> rules: [1989-RV-A.2.24.4-1, 1989-RV-A.2.24.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.24.4 RV1 - the arithmetic mean of the argument-1
      *> series; §1989:A.2.24.4 RV2 - the sum divided by the number of
      *> occurrences referenced by argument-1 (an ALL subscript
      *> references each one). MEAN1 10.5/4; MEAN2 (-5+2)/2; MEAN3
      *> TE(ALL) is 4 occurrences, 12/4; MEAN4 two occurrences, 7/2. A
      *> divisor of other than the occurrence count, or a lost sign or
      *> fraction, fails a line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF4002.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NEG20 PIC S9(2) VALUE -20.
       01 N1 PIC S9(3) VALUE -5.
       01 N2 PIC S9(3)V9 VALUE +3.5.
       01 N3 PIC 9(1) VALUE 2.
       01 R PIC -9(3).9(3).
       01 I PIC 9.
       01 TV PIC X(8) VALUE "01020306".
       01 T REDEFINES TV.
           05 TE PIC 99 OCCURS 4 TIMES.
       PROCEDURE DIVISION.
       P1.
           COMPUTE R = FUNCTION MEAN (1 2 3 4.5).
           DISPLAY "MEAN1 " R.
           COMPUTE R = FUNCTION MEAN (N1 N3).
           DISPLAY "MEAN2 " R.
           COMPUTE R = FUNCTION MEAN (TE (ALL)).
           DISPLAY "MEAN3 " R.
           COMPUTE R = FUNCTION MEAN (TE (1) TE (4)).
           DISPLAY "MEAN4 " R.
           STOP RUN.
