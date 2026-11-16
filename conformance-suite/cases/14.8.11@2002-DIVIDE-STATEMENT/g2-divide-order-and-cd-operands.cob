      *>--- conformance-case
      *> id: g2-divide-order-and-cd-operands
      *> rules: [2002-GR-14.8.11.3-4, 2002-GR-14.8.11.3-5, 2002-GR-14.8.11.3-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Native arithmetic is in effect (no ARITHMETIC clause:
      *> §2002:11.8.4.2 GR3); no ROUNDED phrase, so stores truncate.
      *> §2002:14.8.11.3 GR4 (format 1) - "This divisor is used with each
      *> dividend, which is each identifier-2 proceeding from left to
      *> right. Item identification for identifier-2 is done as each
      *> dividend is determined."
      *>  F1: DIVIDE D(2) INTO I(6) T(I): I = 3 first, then T(I) is T(3):
      *>      90 / 2 = 45, and T(6) keeps 90   -> F1 I=3 T3=45 T6=90
      *>  F1 size error (§2002:14.6.6 4b, "only that data item remains
      *>      unchanged"): 20 / 0.5 = 40 into B, 5 / 0.5 = 10 overflows
      *>      A PIC 9                          -> SE, F1SE A=5 B=40
      *>  D4: CD dividends N1 N2 by the CD divisor DV: 100 / 8 = 12.5
      *>      and 30 / 8 = 3.75, truncated     -> D4=012 003
      *> §2002:14.8.11.3 GR5 (formats 2, 3) - "The quotient is then
      *> formed as specified in general rule 1 and stored in each
      *> identifier-3 as specified in 14.6.6".
      *>  F2 (format 3, BY): 100 / 8 = 12.5 into Q1 PIC 99 and Q2 PIC 9:
      *>      Q2 overflows and alone stays 0   -> SE, F2 12 0
      *>  F2I (format 2, INTO; GR2a: identifier-1 F is the divisor and
      *>      identifier-2 N the dividend): 100 / 8 = 12.5, Q1 = 12, Q2
      *>      overflows and alone stays 0      -> SE, F2I 12 0
      *>  D5: 100 / 8 into the CD item Q        -> D5=012
      *> §2002:14.8.11.3 GR6 (formats 4, 5) - the subsidiary quotient has
      *> identifier-3's digits; the remainder per GR7 is dividend minus
      *> subsidiary quotient times divisor; "Item identification of the
      *> data item referenced by identifier-4 is done after the
      *> quotient is stored in the data item referenced by identifier-3."
      *>  F4 (format 5, BY): 17 / 5 GIVING I REMAINDER T(I), I was 9:
      *>      I = 3, then T(3) gets 17 - 3 * 5 = 2, T(9) keeps 90
      *>                                       -> F4 I=3 T3=02 T9=90
      *>  F4I (format 4, INTO): DIVIDE Y INTO X, divisor 5, dividend 17,
      *>      the same stores                  -> F4I I=3 T3=02 T9=90
      *>  D6: CD quotient Q and remainder R: 100 / 8 -> Q = 12,
      *>      R = 100 - 12 * 8 = 4             -> D6=012 004
      *> Why each leg can fail: subscripts resolved at statement start
      *> (T(6), T(9) would change), a size error that leaves every
      *> receiver unchanged, or CD record items not stored as receivers.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DIV01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D  PIC 9 VALUE 2.
       01 I  PIC 9 VALUE 6.
       01 TT.
          05 T PIC 99 OCCURS 9 VALUE 90.
       01 A  PIC 9 VALUE 5.
       01 B  PIC 99 VALUE 20.
       01 Q1 PIC 99 VALUE 0.
       01 Q2 PIC 9 VALUE 0.
       01 N  PIC 999 VALUE 100.
       01 F  PIC 9 VALUE 8.
       01 X  PIC 99 VALUE 17.
       01 Y  PIC 9 VALUE 5.
       COMMUNICATION SECTION.
       CD CDIN FOR INPUT
           SYMBOLIC QUEUE IS SQ.
       01 INREC.
          05 N1     PIC 9(3).
          05 N2     PIC 9(3).
          05 DV     PIC 9(3).
          05 Q      PIC 9(3).
          05 R      PIC 9(3).
          05 FILLER PIC X(72).
       PROCEDURE DIVISION.
       MAIN.
           DIVIDE D INTO I T(I).
           DISPLAY "F1 I=" I " T3=" T(3) " T6=" T(6).
           DIVIDE 0.5 INTO B A ON SIZE ERROR DISPLAY "SE" END-DIVIDE.
           DISPLAY "F1SE A=" A " B=" B.
           MOVE 100 TO N1. MOVE 30 TO N2. MOVE 8 TO DV.
           DIVIDE DV INTO N1 N2.
           DISPLAY "D4=" N1 " " N2.
           DIVIDE N BY F GIVING Q1 Q2 ON SIZE ERROR DISPLAY "SE"
           END-DIVIDE.
           DISPLAY "F2 " Q1 " " Q2.
           MOVE 0 TO Q1 Q2.
           DIVIDE F INTO N GIVING Q1 Q2 ON SIZE ERROR DISPLAY "SE"
           END-DIVIDE.
           DISPLAY "F2I " Q1 " " Q2.
           MOVE 100 TO N1. MOVE 0 TO Q.
           DIVIDE N1 BY DV GIVING Q.
           DISPLAY "D5=" Q.
           MOVE 9 TO I. MOVE 0 TO T(3).
           DIVIDE X BY Y GIVING I REMAINDER T(I).
           DISPLAY "F4 I=" I " T3=" T(3) " T9=" T(9).
           MOVE 9 TO I. MOVE 0 TO T(3).
           DIVIDE Y INTO X GIVING I REMAINDER T(I).
           DISPLAY "F4I I=" I " T3=" T(3) " T9=" T(9).
           MOVE 0 TO Q R.
           DIVIDE N1 BY DV GIVING Q REMAINDER R.
           DISPLAY "D6=" Q " " R.
           STOP RUN.
       END PROGRAM G2DIV01.
