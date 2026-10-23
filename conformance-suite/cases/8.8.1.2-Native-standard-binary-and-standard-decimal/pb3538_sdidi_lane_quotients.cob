      *>--- conformance-case
      *> id: pb3538_sdidi_lane_quotients
      *> rules: [GR-8.8.1.2-3, GR-14.7.4.3-2, GR-14.7.5-2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB3538 branch review finding R2-1: a product the
      *> compiler cannot bound within the exact wide form (two
      *> function values, 39 + 39 digits; a function value times a
      *> 20-digit item, whose quotient then needs 79) is formed on the
      *> standard-decimal intermediate, and so is any operation beside
      *> a floating-point literal. The LANE must not change the VALUE.
      *> 8.8.1.2 3) evaluates left to right, a nested quotient is
      *> truncated at the documented native precision (CONFORMANCE.md
      *> DOC-A.1-123: the receiver's or the operands' fraction digits,
      *> whichever is more, plus 14), and 14.7.4.3 2) truncates into
      *> the receiver. R has 6 fraction digits, so w = 20:
      *>   2 / 7 = 0.28571428571428571428, * 7 = 1.99999999999999999996,
      *>   stored 1.999999 in every spelling (ITEM, INT, WIDE, MOD,
      *>   FLT) and -1.999999 for -2 (NEG);
      *>   THIRD  1 / 3 / 3 * 9 = 0.99999999999999999999, stored 0.999999;
      *>   DEEP   2 * 512 / 2 nine times = 2, stored 2.000000;
      *>   REL    no receiver, w = 14: 1.99999999999996 < 2, LESS;
      *>   PREC   F8 has 8 fraction digits, so w = 22 on both sides of
      *>          the relation, and both are 1.9999999999999999999998;
      *>   ZERO   a zero divisor is the size error condition (14.7.5
      *>          case 2): ON SIZE ERROR runs and R keeps 9.000000.
      *> Why each leg can fail: the standard-decimal quotient of the
      *> exact value (34 digits, rounded or round-to-odd) made every
      *> product above it round back up to 2.000000, so INT, WIDE, MOD
      *> and FLT stored 2.000000, REL was NOT LESS, and a lane that
      *> dropped the operands' fraction digits made PREC NOT EQUAL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3538SDQ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X    PIC 9 VALUE 2.
       01 Y    PIC 9 VALUE 1.
       01 Z    PIC 9 VALUE 0.
       01 T    PIC 9 VALUE 7.
       01 NX   PIC S9 VALUE -2.
       01 F8   PIC 9V9(8) VALUE 2.
       01 Y20  PIC 9(20) VALUE 1.
       01 R    PIC S9(9)V9(6) VALUE 0.
       01 E    PIC -(9)9.9(6).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = X * Y / T * T.
           MOVE R TO E.
           DISPLAY "ITEM " E.
           COMPUTE R = FUNCTION INTEGER (X) * FUNCTION INTEGER (Y)
               / T * T.
           MOVE R TO E.
           DISPLAY "INT " E.
           COMPUTE R = FUNCTION INTEGER (X) * Y20 / T * T.
           MOVE R TO E.
           DISPLAY "WIDE " E.
           COMPUTE R = FUNCTION INTEGER (X) * FUNCTION MOD (3 2)
               / T * T.
           MOVE R TO E.
           DISPLAY "MOD " E.
           COMPUTE R = 2.0E0 / T * T.
           MOVE R TO E.
           DISPLAY "FLT " E.
           COMPUTE R = FUNCTION INTEGER (NX) * FUNCTION INTEGER (Y)
               / T * T.
           MOVE R TO E.
           DISPLAY "NEG " E.
           COMPUTE R = FUNCTION INTEGER (Y) * FUNCTION INTEGER (Y)
               / 3 / 3 * 9.
           MOVE R TO E.
           DISPLAY "THIRD " E.
           COMPUTE R = FUNCTION INTEGER (X) * FUNCTION INTEGER (Y)
               * 512 / 2 / 2 / 2 / 2 / 2 / 2 / 2 / 2 / 2.
           MOVE R TO E.
           DISPLAY "DEEP " E.
           IF FUNCTION INTEGER (X) * FUNCTION INTEGER (Y) / T * T < 2
              DISPLAY "REL LESS"
           ELSE
              DISPLAY "REL NOT LESS"
           END-IF.
           IF F8 * FUNCTION INTEGER (Y) * FUNCTION INTEGER (Y) / T * T
              = F8 * Y * Y / T * T
              DISPLAY "PREC EQUAL"
           ELSE
              DISPLAY "PREC NOT EQUAL"
           END-IF.
           MOVE 9 TO R.
           COMPUTE R = FUNCTION INTEGER (X) * FUNCTION INTEGER (Y)
               / Z * T
              ON SIZE ERROR DISPLAY "ZERO SIZE ERROR"
              NOT ON SIZE ERROR DISPLAY "ZERO NO SIZE ERROR"
           END-COMPUTE.
           MOVE R TO E.
           DISPLAY "ZERO " E.
           STOP RUN.
