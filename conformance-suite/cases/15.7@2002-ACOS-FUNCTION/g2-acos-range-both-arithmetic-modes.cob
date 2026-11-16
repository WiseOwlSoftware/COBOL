      *>--- conformance-case
      *> id: g2-acos-range-both-arithmetic-modes
      *> rules: [2002-RV-15.7.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.7.3 RV1 - "The returned value is the approximation
      *> of the arccosine of argument-1 and is greater than or equal
      *> to zero and less than or equal to π." ACOS has no equivalent
      *> arithmetic expression, so in BOTH 2002 arithmetic modes its
      *> value is an implementor-defined approximation (§2002:15.3.1
      *> "does not have an equivalent arithmetic expression, its
      *> returned value is implementor-defined") still bound by RV1.
      *> The outer program runs under native arithmetic (no ARITHMETIC
      *> clause, §2002:11.8.4.2 GR3); the contained program declares
      *> ARITHMETIC IS STANDARD, which overrides for it alone
      *> (§2002:11.8.3 GR1), so the returned value is held in a
      *> standard intermediate data item (§2002:15.3.1).
      *> Every argument lies in [-1, +1] (§2002:15.7.2 AR2). Each leg
      *> prints COMPUTE ROUNDED into +9.9(6), which for any
      *> approximation within 5E-8 of the true value gives:
      *>   M1 acos(-1)   = pi          -> +3.141593
      *>   M2 acos(-0.5) = 2pi/3       -> +2.094395
      *>   M3 acos(0)    = pi/2        -> +1.570796
      *>   M4 acos(0.5)  = pi/3        -> +1.047198
      *>   M5 acos(1)    = 0           -> +0.000000
      *>   M6 acos(W-H - 1), W-H = 0.5 -> +2.094395 (expression)
      *> and IN when 0 <= ACOS(x) <= 3.14159265358979323847 (a bound
      *> 7.4E-21 above pi; the pi literal truncated at 20 places lies
      *> BELOW pi and would refuse a more precise acos(-1)). M = native,
      *> S = standard. A leg fails if a value leaves [0, pi] (a
      *> negative or a principal-value error at the -1/+1 ends), if
      *> the approximation is wrong in the sixth place, or if the
      *> standard-arithmetic program does not compute the function.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ACOS01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-H     PIC 9V9 VALUE 0.5.
       01  W-NEG   PIC S9 VALUE -1.
       01  R       PIC +9.9(6).
       01  F       PIC XXX.
       PROCEDURE DIVISION.
       P-MAIN.
           COMPUTE R ROUNDED = FUNCTION ACOS(W-NEG)
           MOVE "OUT" TO F
           IF FUNCTION ACOS(W-NEG) >= 0 AND
              FUNCTION ACOS(W-NEG) <= 3.14159265358979323847
              MOVE "IN" TO F
           END-IF
           DISPLAY "M1=" F " " R
           COMPUTE R ROUNDED = FUNCTION ACOS(-0.5)
           MOVE "OUT" TO F
           IF FUNCTION ACOS(-0.5) >= 0 AND
              FUNCTION ACOS(-0.5) <= 3.14159265358979323847
              MOVE "IN" TO F
           END-IF
           DISPLAY "M2=" F " " R
           COMPUTE R ROUNDED = FUNCTION ACOS(0)
           MOVE "OUT" TO F
           IF FUNCTION ACOS(0) >= 0 AND
              FUNCTION ACOS(0) <= 3.14159265358979323847
              MOVE "IN" TO F
           END-IF
           DISPLAY "M3=" F " " R
           COMPUTE R ROUNDED = FUNCTION ACOS(W-H)
           MOVE "OUT" TO F
           IF FUNCTION ACOS(W-H) >= 0 AND
              FUNCTION ACOS(W-H) <= 3.14159265358979323847
              MOVE "IN" TO F
           END-IF
           DISPLAY "M4=" F " " R
           COMPUTE R ROUNDED = FUNCTION ACOS(1)
           MOVE "OUT" TO F
           IF FUNCTION ACOS(1) >= 0 AND
              FUNCTION ACOS(1) <= 3.14159265358979323847
              MOVE "IN" TO F
           END-IF
           DISPLAY "M5=" F " " R
           COMPUTE R ROUNDED = FUNCTION ACOS(W-H - 1)
           MOVE "OUT" TO F
           IF FUNCTION ACOS(W-H - 1) >= 0 AND
              FUNCTION ACOS(W-H - 1) <= 3.14159265358979323847
              MOVE "IN" TO F
           END-IF
           DISPLAY "M6=" F " " R
           CALL "G2ACOS0S"
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ACOS0S.
       OPTIONS.
           ARITHMETIC IS STANDARD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  S-H     PIC 9V9 VALUE 0.5.
       01  S-NEG   PIC S9 VALUE -1.
       01  S-R     PIC +9.9(6).
       01  S-F     PIC XXX.
       PROCEDURE DIVISION.
       S-MAIN.
           COMPUTE S-R ROUNDED = FUNCTION ACOS(S-NEG)
           MOVE "OUT" TO S-F
           IF FUNCTION ACOS(S-NEG) >= 0 AND
              FUNCTION ACOS(S-NEG) <= 3.14159265358979323847
              MOVE "IN" TO S-F
           END-IF
           DISPLAY "S1=" S-F " " S-R
           COMPUTE S-R ROUNDED = FUNCTION ACOS(-0.5)
           MOVE "OUT" TO S-F
           IF FUNCTION ACOS(-0.5) >= 0 AND
              FUNCTION ACOS(-0.5) <= 3.14159265358979323847
              MOVE "IN" TO S-F
           END-IF
           DISPLAY "S2=" S-F " " S-R
           COMPUTE S-R ROUNDED = FUNCTION ACOS(0)
           MOVE "OUT" TO S-F
           IF FUNCTION ACOS(0) >= 0 AND
              FUNCTION ACOS(0) <= 3.14159265358979323847
              MOVE "IN" TO S-F
           END-IF
           DISPLAY "S3=" S-F " " S-R
           COMPUTE S-R ROUNDED = FUNCTION ACOS(S-H)
           MOVE "OUT" TO S-F
           IF FUNCTION ACOS(S-H) >= 0 AND
              FUNCTION ACOS(S-H) <= 3.14159265358979323847
              MOVE "IN" TO S-F
           END-IF
           DISPLAY "S4=" S-F " " S-R
           COMPUTE S-R ROUNDED = FUNCTION ACOS(1)
           MOVE "OUT" TO S-F
           IF FUNCTION ACOS(1) >= 0 AND
              FUNCTION ACOS(1) <= 3.14159265358979323847
              MOVE "IN" TO S-F
           END-IF
           DISPLAY "S5=" S-F " " S-R
           COMPUTE S-R ROUNDED = FUNCTION ACOS(S-H - 1)
           MOVE "OUT" TO S-F
           IF FUNCTION ACOS(S-H - 1) >= 0 AND
              FUNCTION ACOS(S-H - 1) <= 3.14159265358979323847
              MOVE "IN" TO S-F
           END-IF
           DISPLAY "S6=" S-F " " S-R
           GOBACK.
       END PROGRAM G2ACOS0S.
       END PROGRAM G2ACOS01.
