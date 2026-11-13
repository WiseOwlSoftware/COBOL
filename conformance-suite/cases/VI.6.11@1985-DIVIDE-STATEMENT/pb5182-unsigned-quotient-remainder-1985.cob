      *>--- conformance-case
      *> id: pb5182-unsigned-quotient-remainder-1985
      *> rules: [1985-GR-VI.6.11.4-6]
      *> accept: [85]
      *> source-format: fixed
      *>---
      *> §1985:VI.6.11.4 GR6: "This intermediate field is defined as a
      *> numeric field which contains the same number of digits, the same
      *> decimal point location, and the same presence or absence of a
      *> sign as the quotient (identifier-3)." The remainder is
      *> "the result of subtracting the product of the quotient
      *> (identifier-3) and the divisor from the dividend".
      *> An UNSIGNED identifier-3 holds the absolute value of a negative
      *> quotient (§1985:VI.6.19.4), so -11 / 3 into PIC 9 gives
      *> quotient 3 and remainder -11 - 3 x 3 = -20; the same into a
      *> signed or sign-bearing edited item gives quotient -3 and
      *> remainder -11 - (-3 x 3) = -2; ROUNDED does not change the
      *> intermediate field (quotient -3.67 truncated, 3 unsigned).
      *> Each leg fails if the subsidiary quotient is signed whatever
      *> identifier-3 is, as at 2002 and later.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5182A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  DVD     PIC S99 VALUE -11.
       01  QU      PIC 9.
       01  QR      PIC 9.
       01  QE      PIC Z9.
       01  QS      PIC S9.
       01  QM      PIC -9.
       01  QD      PIC 9V9.
       01  RM      PIC S99.
       01  NQ      PIC S9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DIVIDE DVD BY 3 GIVING QU REMAINDER RM.
           IF QU = 3 AND RM = -20
               DISPLAY "U OK"
           ELSE
               DISPLAY "U BAD"
           END-IF.
           DIVIDE DVD BY 3 GIVING QR ROUNDED REMAINDER RM.
           IF QR = 4 AND RM = -20
               DISPLAY "UR OK"
           ELSE
               DISPLAY "UR BAD"
           END-IF.
           DIVIDE DVD BY 3 GIVING QE REMAINDER RM.
           MOVE QE TO NQ
           IF NQ = 3 AND RM = -20
               DISPLAY "E OK"
           ELSE
               DISPLAY "E BAD"
           END-IF.
           DIVIDE DVD BY 3 GIVING QS REMAINDER RM.
           IF QS = -3 AND RM = -2
               DISPLAY "S OK"
           ELSE
               DISPLAY "S BAD"
           END-IF.
           DIVIDE DVD BY 3 GIVING QM REMAINDER RM.
           MOVE QM TO NQ
           IF NQ = -3 AND RM = -2
               DISPLAY "M OK"
           ELSE
               DISPLAY "M BAD"
           END-IF.
           DIVIDE DVD BY 3 GIVING QD REMAINDER RM.
           IF QD = 3.6 AND RM = -21
               DISPLAY "D OK"
           ELSE
               DISPLAY "D BAD"
           END-IF.
           STOP RUN.
