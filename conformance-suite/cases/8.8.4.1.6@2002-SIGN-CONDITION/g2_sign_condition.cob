      *>--- conformance-case
      *> id: g2_sign_condition
      *> rules: [2002-GR-8.8.4.1.6.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.8.4.1.6.2 GR1 (cite.py OK): "When used, NOT and the
      *> next keyword specify one sign condition that defines the
      *> algebraic test to be executed for truth value. An operand is
      *> positive, if its value is greater than zero, negative if its
      *> value is less than zero, and zero if its value is equal to
      *> zero."  NOTE: "NOT ZERO is a truth test for a nonzero
      *> (positive or negative) value."
      *> The 2002 format is arithmetic-expression-1 IS [NOT]
      *> {POSITIVE | NEGATIVE | ZERO} (§2002:8.8.4.1.6.1).
      *> Each line prints six flags, in the order POSITIVE NEGATIVE
      *> ZERO NOT-POSITIVE NOT-NEGATIVE NOT-ZERO, T or F:
      *>   P  S9(3) = +5      > 0: TFF FTT
      *>   N  S9(3) = -5      < 0: FTF TFT
      *>   Z  S9(3) = 0       = 0: FFT TTF
      *>   U  9(3)  = 0 (unsigned zero)        FFT TTF
      *>   D  S9V99 = +0.01   > 0: TFF FTT
      *>   E  S9V99 = -0.01   < 0: FTF TFT
      *>   B  BINARY S9(4) = -3                FTF TFT
      *>   X1 P + N  = 0                       FFT TTF
      *>   X2 P - 10 = -5                      FTF TFT
      *>   X3 P * N  = -25                     FTF TFT
      *>   X4 (N * N) / 25 = 1                 TFF FTT
      *> A leg fails if NOT negates the whole phrase set rather than
      *> pairing with its keyword (NOT POSITIVE on zero is T), if
      *> zero counts as positive or negative, or if an expression is
      *> tested by anything but its algebraic value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SGN01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  P PIC S9(3) VALUE +5.
       01  N PIC S9(3) VALUE -5.
       01  Z PIC S9(3) VALUE 0.
       01  U PIC 9(3) VALUE 0.
       01  D PIC S9V99 VALUE +0.01.
       01  E PIC S9V99 VALUE -0.01.
       01  B PIC S9(4) USAGE BINARY VALUE -3.
       01  F6 PIC X(6).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "FFFFFF" TO F6
           IF P IS POSITIVE MOVE "T" TO F6(1:1) END-IF
           IF P IS NEGATIVE MOVE "T" TO F6(2:1) END-IF
           IF P IS ZERO MOVE "T" TO F6(3:1) END-IF
           IF P IS NOT POSITIVE MOVE "T" TO F6(4:1) END-IF
           IF P IS NOT NEGATIVE MOVE "T" TO F6(5:1) END-IF
           IF P IS NOT ZERO MOVE "T" TO F6(6:1) END-IF
           DISPLAY "P  " F6
           MOVE "FFFFFF" TO F6
           IF N IS POSITIVE MOVE "T" TO F6(1:1) END-IF
           IF N IS NEGATIVE MOVE "T" TO F6(2:1) END-IF
           IF N IS ZERO MOVE "T" TO F6(3:1) END-IF
           IF N IS NOT POSITIVE MOVE "T" TO F6(4:1) END-IF
           IF N IS NOT NEGATIVE MOVE "T" TO F6(5:1) END-IF
           IF N IS NOT ZERO MOVE "T" TO F6(6:1) END-IF
           DISPLAY "N  " F6
           MOVE "FFFFFF" TO F6
           IF Z IS POSITIVE MOVE "T" TO F6(1:1) END-IF
           IF Z IS NEGATIVE MOVE "T" TO F6(2:1) END-IF
           IF Z IS ZERO MOVE "T" TO F6(3:1) END-IF
           IF Z IS NOT POSITIVE MOVE "T" TO F6(4:1) END-IF
           IF Z IS NOT NEGATIVE MOVE "T" TO F6(5:1) END-IF
           IF Z IS NOT ZERO MOVE "T" TO F6(6:1) END-IF
           DISPLAY "Z  " F6
           MOVE "FFFFFF" TO F6
           IF U IS POSITIVE MOVE "T" TO F6(1:1) END-IF
           IF U IS NEGATIVE MOVE "T" TO F6(2:1) END-IF
           IF U IS ZERO MOVE "T" TO F6(3:1) END-IF
           IF U IS NOT POSITIVE MOVE "T" TO F6(4:1) END-IF
           IF U IS NOT NEGATIVE MOVE "T" TO F6(5:1) END-IF
           IF U IS NOT ZERO MOVE "T" TO F6(6:1) END-IF
           DISPLAY "U  " F6
           MOVE "FFFFFF" TO F6
           IF D IS POSITIVE MOVE "T" TO F6(1:1) END-IF
           IF D IS NEGATIVE MOVE "T" TO F6(2:1) END-IF
           IF D IS ZERO MOVE "T" TO F6(3:1) END-IF
           IF D IS NOT POSITIVE MOVE "T" TO F6(4:1) END-IF
           IF D IS NOT NEGATIVE MOVE "T" TO F6(5:1) END-IF
           IF D IS NOT ZERO MOVE "T" TO F6(6:1) END-IF
           DISPLAY "D  " F6
           MOVE "FFFFFF" TO F6
           IF E IS POSITIVE MOVE "T" TO F6(1:1) END-IF
           IF E IS NEGATIVE MOVE "T" TO F6(2:1) END-IF
           IF E IS ZERO MOVE "T" TO F6(3:1) END-IF
           IF E IS NOT POSITIVE MOVE "T" TO F6(4:1) END-IF
           IF E IS NOT NEGATIVE MOVE "T" TO F6(5:1) END-IF
           IF E IS NOT ZERO MOVE "T" TO F6(6:1) END-IF
           DISPLAY "E  " F6
           MOVE "FFFFFF" TO F6
           IF B IS POSITIVE MOVE "T" TO F6(1:1) END-IF
           IF B IS NEGATIVE MOVE "T" TO F6(2:1) END-IF
           IF B IS ZERO MOVE "T" TO F6(3:1) END-IF
           IF B IS NOT POSITIVE MOVE "T" TO F6(4:1) END-IF
           IF B IS NOT NEGATIVE MOVE "T" TO F6(5:1) END-IF
           IF B IS NOT ZERO MOVE "T" TO F6(6:1) END-IF
           DISPLAY "B  " F6
           MOVE "FFFFFF" TO F6
           IF P + N IS POSITIVE MOVE "T" TO F6(1:1) END-IF
           IF P + N IS NEGATIVE MOVE "T" TO F6(2:1) END-IF
           IF P + N IS ZERO MOVE "T" TO F6(3:1) END-IF
           IF P + N IS NOT POSITIVE MOVE "T" TO F6(4:1) END-IF
           IF P + N IS NOT NEGATIVE MOVE "T" TO F6(5:1) END-IF
           IF P + N IS NOT ZERO MOVE "T" TO F6(6:1) END-IF
           DISPLAY "X1 " F6
           MOVE "FFFFFF" TO F6
           IF P - 10 IS POSITIVE MOVE "T" TO F6(1:1) END-IF
           IF P - 10 IS NEGATIVE MOVE "T" TO F6(2:1) END-IF
           IF P - 10 IS ZERO MOVE "T" TO F6(3:1) END-IF
           IF P - 10 IS NOT POSITIVE MOVE "T" TO F6(4:1) END-IF
           IF P - 10 IS NOT NEGATIVE MOVE "T" TO F6(5:1) END-IF
           IF P - 10 IS NOT ZERO MOVE "T" TO F6(6:1) END-IF
           DISPLAY "X2 " F6
           MOVE "FFFFFF" TO F6
           IF P * N IS POSITIVE MOVE "T" TO F6(1:1) END-IF
           IF P * N IS NEGATIVE MOVE "T" TO F6(2:1) END-IF
           IF P * N IS ZERO MOVE "T" TO F6(3:1) END-IF
           IF P * N IS NOT POSITIVE MOVE "T" TO F6(4:1) END-IF
           IF P * N IS NOT NEGATIVE MOVE "T" TO F6(5:1) END-IF
           IF P * N IS NOT ZERO MOVE "T" TO F6(6:1) END-IF
           DISPLAY "X3 " F6
           MOVE "FFFFFF" TO F6
           IF (N * N) / 25 IS POSITIVE MOVE "T" TO F6(1:1) END-IF
           IF (N * N) / 25 IS NEGATIVE MOVE "T" TO F6(2:1) END-IF
           IF (N * N) / 25 IS ZERO MOVE "T" TO F6(3:1) END-IF
           IF (N * N) / 25 IS NOT POSITIVE
               MOVE "T" TO F6(4:1) END-IF
           IF (N * N) / 25 IS NOT NEGATIVE
               MOVE "T" TO F6(5:1) END-IF
           IF (N * N) / 25 IS NOT ZERO MOVE "T" TO F6(6:1) END-IF
           DISPLAY "X4 " F6
           STOP RUN.
