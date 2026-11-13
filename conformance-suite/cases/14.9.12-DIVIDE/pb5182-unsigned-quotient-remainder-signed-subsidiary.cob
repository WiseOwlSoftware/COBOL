      *>--- conformance-case
      *> id: pb5182-unsigned-quotient-remainder-signed-subsidiary
      *> rules: [GR-14.9.12.4-6]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> §2023:14.9.12.4 GR6 c) (§2002:14.8.11.3 GR6c): "a subsidiary quotient is developed that is
      *> signed and derived from the quotient by truncation of digits at the
      *> least significant end and that has the same number of digits and the
      *> same decimal point location as the data item referenced by
      *> identifier-3." From 2002 the remainder no longer depends on whether
      *> identifier-3 has a sign (§2002:F.1: "The new rules yield
      *> the same value for the remainder regardless of the presence or
      *> absence of a sign in the receiving operand for the quotient"), so
      *> -11 / 3 gives remainder -11 - (-3 x 3) = -2 for every receiver, and
      *> into PIC 9V9 the signed subsidiary -3.6 gives -11 + 10.8 = -0.2,
      *> truncated to 0 in PIC S99. The 1985 test of the same statements
      *> (pb5182-unsigned-quotient-remainder-1985) has -20 and -21.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5182B.
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
           IF QU = 3 AND RM = -2
               DISPLAY "U OK"
           ELSE
               DISPLAY "U BAD"
           END-IF.
           DIVIDE DVD BY 3 GIVING QR ROUNDED REMAINDER RM.
           IF QR = 4 AND RM = -2
               DISPLAY "UR OK"
           ELSE
               DISPLAY "UR BAD"
           END-IF.
           DIVIDE DVD BY 3 GIVING QE REMAINDER RM.
           MOVE QE TO NQ
           IF NQ = 3 AND RM = -2
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
           IF QD = 3.6 AND RM = 0
               DISPLAY "D OK"
           ELSE
               DISPLAY "D BAD"
           END-IF.
           STOP RUN.
