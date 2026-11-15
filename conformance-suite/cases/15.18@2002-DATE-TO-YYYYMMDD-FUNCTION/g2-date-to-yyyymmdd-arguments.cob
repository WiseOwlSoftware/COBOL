      *>--- conformance-case
      *> id: g2-date-to-yyyymmdd-arguments
      *> rules: [2002-AR-15.18.2-1, 2002-AR-15.18.2-2, 2002-AR-15.18.2-3,
      *>    2002-AR-15.18.2-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.18.3 RV1 - the result is
      *>   YEAR-TO-YYYY(YY, argument-2, argument-3) * 10000 + mmdd,
      *>   YY = INTEGER(argument-1 / 10000), mmdd = MOD(argument-1, 10000)
      *> §2002:15.81.3 RV1 maximum-year = argument-2 + argument-3, and
      *> §2002:15.81.3 RV2: if MOD(maximum-year, 100) >= YY the year is
      *> YY + 100 * INTEGER(maximum-year / 100), otherwise
      *> YY + 100 * (INTEGER(maximum-year / 100) - 1).
      *> Every leg fixes argument-3 except OMIT2, so nothing depends on
      *> the date of the run, and every leg keeps argument-2 plus
      *> argument-3 inside 1699 < sum < 10000 (§2002:15.18.2 AR6).
      *>
      *> §2002:15.18.2 AR1 "Argument-1 shall be a positive integer less
      *> than 1000000" - literal, both ends of the range, an item, an
      *> expression and the MESSAGE DATE item of a 2002 input CD (an
      *> unsigned 6-digit integer, §2002:A.1.2.3 GR2e):
      *>   A1LIT  (851003, 10, 2002) max 2012, 12 < 85 -> 1985 19851003
      *>          (the 2002 NOTE 1 example)
      *>   A1MAX  (999999, 0, 9999) max 9999, 99 >= 99 -> 9999 99999999
      *>   A1MIN  (1, 99, 1601) YY 0, mmdd 0001, max 1700 -> 17000001
      *>   A1ITEM (761231, 0, 2050) max 2050, 50 < 76 -> 1976 19761231
      *>   A1EXPR (761231 + 1, 0, 2050) -> 19761232 (AR1 NOTE: the
      *>          date is not checked)
      *>   A1CD   (250615, 30, 2000) max 2030, 30 >= 25 -> 20250615
      *> §2002:15.18.2 AR2 "Argument-2 shall be an integer" - negative,
      *> zero, an item and an expression:
      *>   A2NEG  (981002, -10, 1994) max 1984, 84 < 98 -> 18981002
      *>          (the 2002 NOTE 1 example)
      *>   A2ZERO (500101, 0, 2050) max 2050, 50 >= 50 -> 20500101
      *>   A2ITEM (250615, W-A2 = 25, 2000) max 2025, 25 >= 25 -> 20250615
      *>   A2ITM2 (260615, W-A2 = 25, 2000) 25 < 26 -> 1926 19260615
      *>   A2EXPR (260615, W-A2 + 1, 2000) max 2026, 26 >= 26 -> 20260615
      *> §2002:15.18.2 AR3 "If argument-2 is omitted, the function shall
      *> be evaluated as though 50 were specified for argument-2" -
      *> argument-3 cannot be written without argument-2, so both
      *> forms omit argument-3 and take the same year of execution
      *> (§2002:15.18.2 AR5); each pair must be equal:
      *>   OMIT2A (600101) = (600101, 50)
      *>   OMIT2B (760101) = (760101, 50)
      *>   OMIT2C (770101) = (770101, 50)
      *> Equality holds in any year of execution. YY 60 separates 50
      *> from a default of 0 in the years 2010-2049 (1960 v 2060), and
      *> in 2026 YY 76/77 sit on the window's edge (max-year 2076), so
      *> a default of 49 changes OMIT2B and one of 51 changes OMIT2C.
      *> §2002:15.18.2 AR4 "Argument-3 shall be an integer greater than
      *> 1600 and less than 10000" - both ends, an item, an expression
      *> and the TEXT LENGTH item of a 2002 input CD (§2002:A.1.2.3 GR2h):
      *>   A3LOW  (991231, 99, 1601) max 1700, 0 < 99 -> 1699 16991231
      *>   A3HIGH (101, 0, 9999) YY 0, mmdd 0101, 99 >= 0 -> 99000101
      *>   A3ITEM (851003, 10, W-A3 = 2002) -> 19851003
      *>   A3EXPR (500101, 0, W-A3 * 2 - 1954 = 2050) -> 20500101
      *>   A3CD   (981002, -10, IN-LEN = 1994) -> 18981002
      *> A leg fails if a legal argument is refused, if the window
      *> branch (>= versus <) is taken wrongly at its boundary
      *> (A2ITEM/A2ITM2/A2EXPR/A2ZERO), or if an omitted argument-2 is
      *> not 50.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DTY01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  R       PIC 9(8).
       01  R2      PIC 9(8).
       01  W-D1    PIC 9(6) VALUE 761231.
       01  W-A2    PIC S9(4) VALUE 25.
       01  W-A3    PIC 9(4) COMP VALUE 2002.
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           MESSAGE DATE IS IN-MDATE
           TEXT LENGTH IS IN-LEN.
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE FUNCTION DATE-TO-YYYYMMDD(851003, 10, 2002) TO R
           DISPLAY "A1LIT=" R
           MOVE FUNCTION DATE-TO-YYYYMMDD(999999, 0, 9999) TO R
           DISPLAY "A1MAX=" R
           MOVE FUNCTION DATE-TO-YYYYMMDD(1, 99, 1601) TO R
           DISPLAY "A1MIN=" R
           MOVE FUNCTION DATE-TO-YYYYMMDD(W-D1, 0, 2050) TO R
           DISPLAY "A1ITEM=" R
           MOVE FUNCTION DATE-TO-YYYYMMDD(W-D1 + 1, 0, 2050) TO R
           DISPLAY "A1EXPR=" R
           MOVE 250615 TO IN-MDATE
           MOVE FUNCTION DATE-TO-YYYYMMDD(IN-MDATE, 30, 2000) TO R
           DISPLAY "A1CD=" R
           MOVE FUNCTION DATE-TO-YYYYMMDD(981002, -10, 1994) TO R
           DISPLAY "A2NEG=" R
           MOVE FUNCTION DATE-TO-YYYYMMDD(500101, 0, 2050) TO R
           DISPLAY "A2ZERO=" R
           MOVE FUNCTION DATE-TO-YYYYMMDD(250615, W-A2, 2000) TO R
           DISPLAY "A2ITEM=" R
           MOVE FUNCTION DATE-TO-YYYYMMDD(260615, W-A2, 2000) TO R
           DISPLAY "A2ITM2=" R
           MOVE FUNCTION DATE-TO-YYYYMMDD(260615, W-A2 + 1, 2000) TO R
           DISPLAY "A2EXPR=" R
           MOVE FUNCTION DATE-TO-YYYYMMDD(600101) TO R
           MOVE FUNCTION DATE-TO-YYYYMMDD(600101, 50) TO R2
           IF R = R2 DISPLAY "OMIT2A=SAME" ELSE DISPLAY "OMIT2A=DIFF"
           END-IF
           MOVE FUNCTION DATE-TO-YYYYMMDD(760101) TO R
           MOVE FUNCTION DATE-TO-YYYYMMDD(760101, 50) TO R2
           IF R = R2 DISPLAY "OMIT2B=SAME" ELSE DISPLAY "OMIT2B=DIFF"
           END-IF
           MOVE FUNCTION DATE-TO-YYYYMMDD(770101) TO R
           MOVE FUNCTION DATE-TO-YYYYMMDD(770101, 50) TO R2
           IF R = R2 DISPLAY "OMIT2C=SAME" ELSE DISPLAY "OMIT2C=DIFF"
           END-IF
           MOVE FUNCTION DATE-TO-YYYYMMDD(991231, 99, 1601) TO R
           DISPLAY "A3LOW=" R
           MOVE FUNCTION DATE-TO-YYYYMMDD(101, 0, 9999) TO R
           DISPLAY "A3HIGH=" R
           MOVE FUNCTION DATE-TO-YYYYMMDD(851003, 10, W-A3) TO R
           DISPLAY "A3ITEM=" R
           MOVE FUNCTION DATE-TO-YYYYMMDD(500101, 0, W-A3 * 2 - 1954)
             TO R
           DISPLAY "A3EXPR=" R
           MOVE 1994 TO IN-LEN
           MOVE FUNCTION DATE-TO-YYYYMMDD(981002, -10, IN-LEN) TO R
           DISPLAY "A3CD=" R
           STOP RUN.
