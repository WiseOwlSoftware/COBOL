*>--- conformance-case
*> id: pb3860_mean_one_value
*> rules: [RV-15.60.4-1, GR-14.7.4.3-2, GR-14.7.4.3-3, GR-14.7.4.3-4, GR-14.7.4.3-5, GR-14.7.4.3-6, GR-14.7.4.3-8,
*>     GR-14.7.4.3-9, GR-14.7.4.3-10]
*> accept: [2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB3860 - FUNCTION MEAN HAS ONE VALUE, WHATEVER RECEIVES IT. 15.60.1: "The MEAN function returns a numeric value that
      *> is the arithmetic mean (average) of its arguments." 15.60.4 r1c gives the equivalent arithmetic expression
      *> ((argument-11 + argument-12 + ... + argument-1n) / n), and under native arithmetic 15.4.1 grants "an
      *> implementor-defined approximation of the value of that expression". WiseOwl COBOL's determination (owner,
      *> 2026-10-26: one reference, one value) is the standard-mode value, the SDIDI quotient, in every mode and for
      *> every receiver. The receiving statement then rounds that one value ONCE, by the ROUNDED phrase it wrote
      *> (14.7.4.3 r3 AWAY-FROM-ZERO, r4 NEAREST-AWAY-FROM-ZERO, r5 NEAREST-EVEN, r6 NEAREST-TOWARD-ZERO, r8
      *> TOWARD-GREATER, r9 TOWARD-LESSER, r10 TRUNCATION) and by TRUNCATION when it wrote none (r2). Before PB3860 the native
      *> quotient was first cut at max(receiver scale, sum scale + 1, 6) places and the receiver's mode then rounded the
      *> cut, so MEAN of 0.00000001 and nineteen zeros (5E-10) stored 0.00 under ROUNDED MODE AWAY-FROM-ZERO into a PIC
      *> S9V99 where the one rounding of 5E-10 is 0.01, and 0.000000000 under NEAREST-AWAY-FROM-ZERO into a PIC S9V9(9)
      *> where the tie rounds away, 0.000000001.
      *> A: the mean 5E-10 (sum 1E-8, n = 20) into receivers of 2, 9, 10 and 12 places under every mode that tells the exact
      *>    value from its cut: AWAY-FROM-ZERO 0.01 (2 places), 0.0000000005 (10), 0.000000001 (9); TOWARD-GREATER 0.01;
      *>    TOWARD-LESSER 0.00; the exact tie at 9 places (5E-10 is half of 1E-9): NEAREST-EVEN 0.000000000 (the even
      *>    neighbour), NEAREST-AWAY-FROM-ZERO 0.000000001, NEAREST-TOWARD-ZERO 0.000000000; no ROUNDED truncates to 0.
      *> B: the negative mean -5E-10: TOWARD-LESSER 0.01 and the NEAREST-AWAY-FROM-ZERO tie -0.000000001.
      *> C: sum scale 6, n = 20, mean 5E-8: the cut working scale (7) was below the mean's own 8 places, so AWAY-FROM-ZERO
      *>    into 2 places stored 0.00 where 0.01 is owed; the unrounded store into 9 places keeps 0.000000050.
      *> D: the inexact quotient 5/3 of MEAN(1 2 2) rounds once at the receiver: 1.67 AWAY-FROM-ZERO, 1.66 TOWARD-LESSER
      *>    and unrounded, and 1.666666667 TOWARD-GREATER at 9 places.
      *> E: the function inside an arithmetic expression (* 1) keeps its one value: 0.000000001 NEAREST-AWAY-FROM-ZERO.
      *> F: the relation condition against the exact 5E-10 holds (EQ) and the function exceeds zero (GT).
      *> G: a table(ALL) argument (twenty elements, one of them 1E-8) is the same mean: 0.01 and 0.000000001.
      *> cite.py --check 15.60.1 "arithmetic mean (average) of its arguments" -> OK 15.60.1
      *> cite.py --check 15.60.4 "The equivalent arithmetic expression is as follows" -> OK 15.60.4 1)
      *> cite.py --check 15.4.1 "an implementor-defined approximation of the value of that expression" -> OK 15.4.1
      *> cite.py --check 14.7.4.3 "ROUNDED MODE IS TRUNCATION had been specified" -> OK 14.7.4.3 2)
      *> cite.py --check 14.7.4.3 "rounded to the nearest value farther from zero" -> OK 14.7.4.3 3)
      *> cite.py --check 14.7.4.3 "If two such values are equally near, the value farther from zero is chosen" -> OK 14.7.4.3 4)
      *> cite.py --check 14.7.4.3 "the value whose rightmost digit is even is chosen" -> OK 14.7.4.3 5)
      *> cite.py --check 14.7.4.3 "the value nearest to zero is chosen" -> OK 14.7.4.3 6)
      *> cite.py --check 14.7.4.3 "rounded to the nearest larger value" -> OK 14.7.4.3 8)
      *> cite.py --check 14.7.4.3 "rounded to the nearest smaller value" -> OK 14.7.4.3 9)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3860MEAN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R2   PIC S9V99      SIGN LEADING SEPARATE.
       01 R9   PIC S9V9(9)    SIGN LEADING SEPARATE.
       01 R10  PIC S9V9(10)   SIGN LEADING SEPARATE.
       01 R12  PIC S9V9(12)   SIGN LEADING SEPARATE.
       01 A    PIC S9V9(8)    VALUE 0.00000001.
       01 NA   PIC S9V9(8)    VALUE -0.00000001.
       01 B    PIC S9V9(6)    VALUE 0.000001.
       01 ONE  PIC 9          VALUE 1.
       01 TWO  PIC 9          VALUE 2.
       01 TBL.
          05 TE PIC S9V9(8)   OCCURS 20.
       PROCEDURE DIVISION.
           COMPUTE R12 = FUNCTION MEAN(A 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0).
           DISPLAY "A1=" R12.
           COMPUTE R2 ROUNDED MODE AWAY-FROM-ZERO = FUNCTION MEAN(A 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0).
           DISPLAY "A2=" R2.
           COMPUTE R10 ROUNDED MODE AWAY-FROM-ZERO = FUNCTION MEAN(A 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0).
           DISPLAY "A3=" R10.
           COMPUTE R9 ROUNDED MODE AWAY-FROM-ZERO = FUNCTION MEAN(A 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0).
           DISPLAY "A4=" R9.
           COMPUTE R9 = FUNCTION MEAN(A 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0).
           DISPLAY "A5=" R9.
           COMPUTE R2 ROUNDED MODE TOWARD-GREATER = FUNCTION MEAN(A 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0).
           DISPLAY "A6=" R2.
           COMPUTE R2 ROUNDED MODE TOWARD-LESSER = FUNCTION MEAN(A 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0).
           DISPLAY "A7=" R2.
           COMPUTE R9 ROUNDED MODE NEAREST-EVEN = FUNCTION MEAN(A 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0).
           DISPLAY "A8=" R9.
           COMPUTE R9 ROUNDED MODE NEAREST-AWAY-FROM-ZERO = FUNCTION MEAN(A 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0).
           DISPLAY "A9=" R9.
           COMPUTE R9 ROUNDED MODE NEAREST-TOWARD-ZERO = FUNCTION MEAN(A 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0).
           DISPLAY "A10=" R9.
           COMPUTE R2 ROUNDED MODE TOWARD-LESSER = FUNCTION MEAN(NA 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0).
           DISPLAY "B1=" R2.
           COMPUTE R9 ROUNDED MODE NEAREST-AWAY-FROM-ZERO = FUNCTION MEAN(NA 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0).
           DISPLAY "B2=" R9.
           COMPUTE R2 ROUNDED MODE AWAY-FROM-ZERO = FUNCTION MEAN(B 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0).
           DISPLAY "C1=" R2.
           COMPUTE R9 = FUNCTION MEAN(B 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0).
           DISPLAY "C2=" R9.
           COMPUTE R2 ROUNDED MODE AWAY-FROM-ZERO = FUNCTION MEAN(ONE TWO TWO).
           DISPLAY "D1=" R2.
           COMPUTE R2 ROUNDED MODE TOWARD-LESSER = FUNCTION MEAN(ONE TWO TWO).
           DISPLAY "D2=" R2.
           COMPUTE R2 = FUNCTION MEAN(ONE TWO TWO).
           DISPLAY "D3=" R2.
           COMPUTE R9 ROUNDED MODE TOWARD-GREATER = FUNCTION MEAN(ONE TWO TWO).
           DISPLAY "D4=" R9.
           COMPUTE R9 ROUNDED MODE NEAREST-AWAY-FROM-ZERO = FUNCTION MEAN(A 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0) * 1.
           DISPLAY "E1=" R9.
           IF FUNCTION MEAN(A 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0) = 0.0000000005
               DISPLAY "F1=EQ"
           ELSE
               DISPLAY "F1=NE"
           END-IF.
           IF FUNCTION MEAN(A 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0) > 0
               DISPLAY "F2=GT"
           ELSE
               DISPLAY "F2=LE"
           END-IF.
           MOVE ZERO TO TBL.
           MOVE 0.00000001 TO TE(1).
           COMPUTE R2 ROUNDED MODE AWAY-FROM-ZERO = FUNCTION MEAN(TE(ALL)).
           DISPLAY "G1=" R2.
           COMPUTE R9 ROUNDED MODE NEAREST-AWAY-FROM-ZERO = FUNCTION MEAN(TE(ALL)).
           DISPLAY "G2=" R9.
           STOP RUN.
       END PROGRAM PB3860MEAN.
