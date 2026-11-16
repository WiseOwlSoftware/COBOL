      *>--- conformance-case
      *> id: g85_bf40_08
      *> rules: [1989-RV-A.2.42.4-1, 1989-RV-A.2.42.4-2, 1989-RV-A.2.42.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.42.4 RV1 - the sum of the arguments (SUM1, SUM4,
      *> SUM5); §1989:A.2.42.4 RV2 - all-integer arguments give an
      *> integer, which §1989:A.2.3 lets stand where an integer operand
      *> is required: MOD takes integers only (§1989:A.2.28.3 AR1), and
      *> MOD(SUM(7 3) 4) is 10 mod 4 = 2 (SUM2; refusing it, or a
      *> non-integer, fails); §1989:A.2.42.4 RV3 - mixed arguments give
      *> a numeric value that keeps its fraction: 1.5 + 2.25 = 3.75
      *> (SUM3). SUM4 -5 + 3.5 + 2 = 0.5; SUM5 TE(ALL) 1+2+3+6 = 12.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF4008.
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
           COMPUTE R = FUNCTION SUM (1 2 3).
           DISPLAY "SUM1 " R.
           COMPUTE I = FUNCTION MOD (FUNCTION SUM (7 3) 4).
           DISPLAY "SUM2 " I.
           COMPUTE R = FUNCTION SUM (1.5 2.25).
           DISPLAY "SUM3 " R.
           COMPUTE R = FUNCTION SUM (N1 N2 N3).
           DISPLAY "SUM4 " R.
           COMPUTE R = FUNCTION SUM (TE (ALL)).
           DISPLAY "SUM5 " R.
           STOP RUN.
