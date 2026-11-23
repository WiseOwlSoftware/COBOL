      *>--- conformance-case
      *> id: pb5891_polynomial_integer_arguments
      *> rules: [AR-15.3-6, AR-15.36.3-1, AR-15.64.3-1, AR-15.75.3-2, AR-15.15.3-1]
      *> accept: [85, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5891 - an integer argument that is an integer by
      *> its value, not by the shape of its terms, is legal source
      *> and is admitted at every edition.
      *>
      *> §2023:15.3 AR6 (cite.py --check): "An arithmetic
      *> expression that will always result in an integer value
      *> or an integer data item shall be specified"; §1989:A.2.2
      *> AR4 (cite.py --check): "An arithmetic expression which
      *> will always result in an integer value must be specified".
      *> The rule sets no method of proof. N * (N + 1) is a product
      *> of two consecutive integers, so it is even and N * (N + 1)
      *> / 2 is an integer for every N; N * (N - 1) / 2 likewise;
      *> N * (N + 1) * (N + 2) is a multiple of 6. SQRT (N) - SQRT
      *> (N) + 5 and 0 * SQRT (N) + 5 are the constant 5.
      *> EXPECTED, with N = 3: N * (N + 1) / 2 = 6, FACTORIAL (6) =
      *> 720 (FTRI); MOD (6, 4) = 2 (MTRI); N * (N - 1) / 2 + 65 =
      *> 68, CHAR (68) is the 68th character of the native
      *> collating sequence, "C" (CTRI); RANDOM (6) is a value of
      *> at least 0 and less than 1 (RTRI OK); N * (N + 1) * (N + 2)
      *> / 6 = 10, FACTORIAL (10) = 3628800 (FTET); MOD (5, 7) = 5
      *> twice (SQID, SQZR). The first four are the refuter's forms
      *> of kb/Work PB5891 (slice 1985-247). Review R1-1: T (I) read
      *> twice is one term, so with I = 2 and T (2) = 3, T (I) *
      *> (T (I) + 1) / 2 = 6 and FACTORIAL (6) = 720 (TTRI).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5891A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N   PIC 99 VALUE 3.
       01  R   PIC 9(18).
       01  RS  PIC S9(18) SIGN LEADING SEPARATE.
       01  C   PIC X.
       01  F   PIC 9V9(8).
       01  TB.
           05  T   PIC 99 OCCURS 3 TIMES.
       01  I   PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       P1.
           COMPUTE R = FUNCTION FACTORIAL (N * (N + 1) / 2).
           DISPLAY "FTRI " R.
           COMPUTE RS = FUNCTION MOD (N * (N + 1) / 2, 4).
           DISPLAY "MTRI " RS.
           MOVE FUNCTION CHAR (N * (N - 1) / 2 + 65) TO C.
           DISPLAY "CTRI " C.
           COMPUTE F = FUNCTION RANDOM (N * (N + 1) / 2).
           IF F < 1
               DISPLAY "RTRI OK"
           ELSE
               DISPLAY "RTRI " F.
           COMPUTE R = FUNCTION FACTORIAL
               (N * (N + 1) * (N + 2) / 6).
           DISPLAY "FTET " R.
           COMPUTE R = FUNCTION MOD
               (FUNCTION SQRT (N) - FUNCTION SQRT (N) + 5, 7).
           DISPLAY "SQID " R.
           COMPUTE R = FUNCTION MOD (0 * FUNCTION SQRT (N) + 5, 7).
           DISPLAY "SQZR " R.
           MOVE 3 TO T (2).
           COMPUTE R = FUNCTION FACTORIAL (T (I) * (T (I) + 1) / 2).
           DISPLAY "TTRI " R.
           STOP RUN.
