      *>--- conformance-case
      *> id: g2-day-to-yyyyddd-default-window
      *> rules: [2002-AR-15.20.2-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.20.2 AR3 - "If argument-2 is omitted, the function
      *> shall be evaluated as though 50 were specified for argument-2."
      *> The general format nests [argument-2 [argument-3]], so an
      *> omitted argument-2 also omits argument-3, which becomes
      *> NUMVAL(CURRENT-DATE(1:4)), the year of execution Y
      *> (§2002:15.20.2 AR5). The legs are built on Y so that the
      *> expected lines hold in every year:
      *> §2002:15.20.3 RV1: YEAR-TO-YYYY(YY, a2, a3) * 1000 + nnn with
      *> YY = INTEGER(a1 / 1000), nnn = MOD(a1, 1000);
      *> §2002:15.81.3 RV1 maximum-year = a2 + a3; RV2: if
      *> MOD(maximum-year, 100) >= YY the year is
      *> YY + 100 * INTEGER(maximum-year / 100), else
      *> YY + 100 * (INTEGER(maximum-year / 100) - 1).
      *> Leg A: YY0 = MOD(Y + 50, 100), A0 = YY0 * 1000 + 1.
      *>   With a2 = 50, maximum-year = Y + 50 and MOD = YY0 >= YY0,
      *>   so the year is Y + 50: OFFA = +50, DDDA = 001.
      *>   With a2 = 49, maximum-year = Y + 49: MOD is YY0 - 1 < YY0
      *>   (or 99 when YY0 = 0, with INTEGER one century lower), so the
      *>   year is Y - 50 and (A0) - (A0, 49, Y) = 100000: D49.
      *>   (A0) = (A0, 50) and (A0) = (A0, 50, Y): EQA1, EQA2.
      *> Leg B: YY1 = MOD(Y + 51, 100), A1 = YY1 * 1000 + 123.
      *>   With a2 = 50 the year is Y - 49 (MOD(Y + 50, 100) falls
      *>   below YY1): OFFB = -49, DDDB = 123; with a2 = 51 it is
      *>   Y + 51, so (A1, 51, Y) - (A1) = 100000: D51; EQB.
      *> A default of 49 fails EQA/D49/OFFA, a default of 51 fails
      *> EQB/D51/OFFB, and a default of 0 or a fixed window fails both.
      *> (A run that straddles midnight of December 31 could see two
      *> different years of execution.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DTD01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  Y       PIC 9(4).
       01  YY0     PIC 99.
       01  YY1     PIC 99.
       01  A0      PIC 9(5).
       01  A1      PIC 9(5).
       01  R       PIC 9(7).
       01  R2      PIC 9(7).
       01  DIFF    PIC +9(7).
       01  OFS     PIC +99.
       01  DDD     PIC 999.
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE FUNCTION CURRENT-DATE(1:4) TO Y
           COMPUTE YY0 = FUNCTION MOD(Y + 50, 100)
           COMPUTE YY1 = FUNCTION MOD(Y + 51, 100)
           COMPUTE A0 = YY0 * 1000 + 1
           COMPUTE A1 = YY1 * 1000 + 123
           MOVE FUNCTION DAY-TO-YYYYDDD(A0) TO R
           MOVE FUNCTION DAY-TO-YYYYDDD(A0, 50) TO R2
           IF R = R2 DISPLAY "EQA1=SAME" ELSE DISPLAY "EQA1=DIFF"
           END-IF
           MOVE FUNCTION DAY-TO-YYYYDDD(A0, 50, Y) TO R2
           IF R = R2 DISPLAY "EQA2=SAME" ELSE DISPLAY "EQA2=DIFF"
           END-IF
           COMPUTE OFS = FUNCTION INTEGER(R / 1000) - Y
           COMPUTE DDD = FUNCTION MOD(R, 1000)
           DISPLAY "OFFA=" OFS " DDDA=" DDD
           COMPUTE DIFF = R - FUNCTION DAY-TO-YYYYDDD(A0, 49, Y)
           DISPLAY "D49=" DIFF
           MOVE FUNCTION DAY-TO-YYYYDDD(A1) TO R
           MOVE FUNCTION DAY-TO-YYYYDDD(A1, 50, Y) TO R2
           IF R = R2 DISPLAY "EQB=SAME" ELSE DISPLAY "EQB=DIFF"
           END-IF
           COMPUTE OFS = FUNCTION INTEGER(R / 1000) - Y
           COMPUTE DDD = FUNCTION MOD(R, 1000)
           DISPLAY "OFFB=" OFS " DDDB=" DDD
           COMPUTE DIFF = FUNCTION DAY-TO-YYYYDDD(A1, 51, Y) - R
           DISPLAY "D51=" DIFF
           STOP RUN.
