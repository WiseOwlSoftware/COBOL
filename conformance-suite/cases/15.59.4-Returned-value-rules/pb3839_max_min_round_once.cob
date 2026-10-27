*>--- conformance-case
*> id: pb3839_max_min_round_once
*> rules: [RV-15.59.4-1, RV-15.63.4-1, GR-14.7.4.3-3, GR-14.7.4.3-5, GR-14.7.4.3-8, GR-14.7.4.3-9, GR-14.7.4.3-10]
*> accept: [2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB3839 (sibling sweep) - MAX AND MIN RETURN THE CONTENT OF AN ARGUMENT, SO THEIR VALUE DOES NOT DEPEND ON THE
      *> RECEIVER EITHER. 15.59.4 r1: "The returned value is the content of the argument-1 having the greatest value";
      *> 15.63.4 r1 the least. The receiving statement then rounds that content ONCE, "relative to the size provided for
      *> the resultant identifier" (14.7.4.1) and by the ROUNDED phrase it wrote (14.7.4.3 r3, r5, r8, r9, r10) - and by
      *> TRUNCATION when it wrote none (r2). Before PB3839 the selected value was first truncated at the RECEIVER's scale
      *> and the receiver's mode then rounded the truncation, so MAX(0.12345 0.1) into a PIC 9V99 ROUNDED MODE
      *> AWAY-FROM-ZERO stored 0.12 (the value 0.12000... has nothing to round away from zero) where the rule owes the
      *> one rounding of 0.12345: 0.13.
      *> A: every mode that distinguishes the exact value from its truncation, both signs, MAX and MIN.
      *> B: a receiver with MORE places than the argument scale and one with FEWER (the cap never loses a digit the
      *>    receiver can hold). C: a 24-digit integer argument beside a 15-place one - the wide selected value still
      *>    reaches a PIC 9(24) receiver whole, and the 0.5 selected by MIN rounds to 1 there (nearest-away, r4).
      *> D: a receiver WIDE enough that its Int128 landing headroom (38 less its integer digits) is below the arguments'
      *>    common scale - 24 integer digits leave 14 places for a 15-place argument, 30 integer digits leave 8 for a
      *>    9-place one. The selected 0.000000000000001 (resp. 0.000000001) must still reach the receiver's ROUNDED phrase
      *>    whole (review R1-1): AWAY-FROM-ZERO and TOWARD-GREATER owe 0.1, TOWARD-LESSER of the negative owes -0.1, and
      *>    the unrounded statement truncates to 0.0 - the same value a narrow receiver gets (A, B).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3839MAXMIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R2   PIC S9V99      SIGN LEADING SEPARATE.
       01 R0   PIC S9(3)      SIGN LEADING SEPARATE.
       01 R9   PIC S9V9(9)    SIGN LEADING SEPARATE.
       01 RB   PIC 9(24).
       01 A    PIC S9V9(8)    VALUE 0.12345.
       01 B    PIC S9V9(8)    VALUE 0.1.
       01 C    PIC S9V9(8)    VALUE 0.10000001.
       01 NA   PIC S9V9(8)    VALUE -0.12345.
       01 BIG  PIC 9(24)      VALUE 123456789012345678901234.
       01 SM   PIC 9V9(15)    VALUE 0.5.
       01 TINY15 PIC 9V9(15)   VALUE 0.000000000000001.
       01 TINY9  PIC 9V9(9)    VALUE 0.000000001.
       01 NTINY  PIC S9V9(15)  VALUE -0.000000000000001.
       01 ONE    PIC 9         VALUE 1.
       01 ZERO0  PIC S9        VALUE 0.
       01 W24    PIC 9(24)V9.
       01 W30    PIC 9(30)V9.
       01 WS24   PIC S9(24)V9  SIGN LEADING SEPARATE.
       PROCEDURE DIVISION.
           COMPUTE R2 ROUNDED MODE AWAY-FROM-ZERO = FUNCTION MAX(A B).
           DISPLAY "A1=" R2.
           COMPUTE R2 ROUNDED MODE AWAY-FROM-ZERO = FUNCTION MAX(C B).
           DISPLAY "A2=" R2.
           COMPUTE R2 ROUNDED MODE TOWARD-GREATER = FUNCTION MAX(C B).
           DISPLAY "A3=" R2.
           COMPUTE R2 ROUNDED MODE TOWARD-LESSER = FUNCTION MIN(NA B).
           DISPLAY "A4=" R2.
           COMPUTE R2 ROUNDED MODE AWAY-FROM-ZERO = FUNCTION MIN(NA B).
           DISPLAY "A5=" R2.
           COMPUTE R2 ROUNDED MODE NEAREST-EVEN = FUNCTION MAX(A B).
           DISPLAY "A6=" R2.
           COMPUTE R2 ROUNDED MODE TRUNCATION = FUNCTION MAX(A B).
           DISPLAY "A7=" R2.
           COMPUTE R2 = FUNCTION MAX(A B).
           DISPLAY "A8=" R2.
           COMPUTE R2 ROUNDED MODE AWAY-FROM-ZERO = FUNCTION MAX(A B) + 0.
           DISPLAY "A9=" R2.
           COMPUTE R0 ROUNDED = FUNCTION MAX(12.5 B).
           DISPLAY "B1=" R0.
           COMPUTE R9 ROUNDED MODE AWAY-FROM-ZERO = FUNCTION MIN(C 0.12345678901).
           DISPLAY "B2=" R9.
           COMPUTE RB = FUNCTION MAX(BIG SM).
           DISPLAY "C1=" RB.
           COMPUTE RB ROUNDED = FUNCTION MIN(BIG SM).
           DISPLAY "C2=" RB.
           MOVE FUNCTION MAX(A B) TO R2.
           DISPLAY "C3=" R2.
           COMPUTE W24 ROUNDED MODE AWAY-FROM-ZERO = FUNCTION MAX(TINY15 ZERO0).
           DISPLAY "D1=" W24.
           COMPUTE W24 ROUNDED MODE AWAY-FROM-ZERO = FUNCTION MIN(TINY15 ONE).
           DISPLAY "D2=" W24.
           COMPUTE W30 ROUNDED MODE AWAY-FROM-ZERO = FUNCTION MAX(TINY9 ZERO0).
           DISPLAY "D3=" W30.
           COMPUTE W30 ROUNDED MODE TOWARD-GREATER = FUNCTION MIN(TINY9 ONE).
           DISPLAY "D4=" W30.
           COMPUTE W24 = FUNCTION MAX(TINY15 ZERO0).
           DISPLAY "D5=" W24.
           COMPUTE WS24 ROUNDED MODE TOWARD-LESSER = FUNCTION MIN(NTINY ZERO0).
           DISPLAY "D6=" WS24.
           COMPUTE WS24 ROUNDED MODE AWAY-FROM-ZERO = FUNCTION MIN(NTINY ZERO0) * 1.
           DISPLAY "D7=" WS24.
           STOP RUN.
       END PROGRAM PB3839MAXMIN.
