      *>--- conformance-case
      *> id: g85_f12_02
      *> rules: [1985-GR-VI.6.5.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.5.4 GR6 - format 2 ACCEPT stores DATE, DAY,
      *> DAY-OF-WEEK and TIME in identifier-2 by the MOVE rules, and the
      *> four are conceptual items the program does not describe (no
      *> item of this program is named DATE, DAY, DAY-OF-WEEK or TIME).
      *> Each receiver is checked against the MOVE rule it falls under
      *> (§1985:VI.6.19.4 GR4a, GR4b, GR5; §1985:IV.4.3.6 GR1-GR3;
      *> §1985:VI.5.6.4 GR1), relative to a 9(6) DATE / 9(5) DAY /
      *> 9 DAY-OF-WEEK read in the same stable sample, so the output is
      *> the same on any day:
      *>  1 sampling (two DATE reads bracketing the rest agree);
      *>  2 X(8) <- DATE: YYMMDD left, two spaces right (a numeric store
      *>    or no space fill fails);
      *>  3 X(3) <- DATE: YYM, truncated on the right;
      *>  4 9(4) <- DATE: MMDD, high-order digits truncated (a left
      *>    truncation or a refusal fails);
      *>  5 99/99/99 <- DATE: YY/MM/DD, edited;
      *>  6 X(8) JUSTIFIED RIGHT <- DATE: two spaces, then YYMMDD;
      *>  7 group <- DAY: YYDDD then three spaces, a group move;
      *>  8 S9(3) <- DAY-OF-WEEK: same value, positive sign generated;
      *>  9 9(3)V99 <- DAY-OF-WEEK: aligned by decimal point, .00;
      *> 10 X(3) <- DAY-OF-WEEK: the digit, then two spaces;
      *> 11 9(10) <- TIME: two zeros filled, the eight digits at most
      *>    23595999.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1202.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-D1          PIC 9(6) VALUE 0.
       01  WS-D1-X6 REDEFINES WS-D1 PIC X(6).
       01  WS-D1-A REDEFINES WS-D1.
           05  WS-D1-YYM  PIC X(3).
           05  FILLER     PIC X(3).
       01  WS-D1-B REDEFINES WS-D1.
           05  WS-D1-YY   PIC XX.
           05  WS-D1-MM   PIC XX.
           05  WS-D1-DD   PIC XX.
       01  WS-D1-C REDEFINES WS-D1.
           05  FILLER     PIC XX.
           05  WS-D1-MMDD PIC 9(4).
       01  WS-D2          PIC 9(6) VALUE 1.
       01  WS-DY          PIC 9(5).
       01  WS-DY-X REDEFINES WS-DY PIC X(5).
       01  WS-DW          PIC 9.
       01  WS-DW-X REDEFINES WS-DW PIC X.
       01  WS-TRIES       PIC 9 VALUE 0.
       01  R-AX8          PIC X(8).
       01  R-AX8-R REDEFINES R-AX8.
           05  R-AX8-6    PIC X(6).
           05  R-AX8-2    PIC XX.
       01  R-AX3          PIC X(3).
       01  R-N4           PIC 9(4).
       01  R-NE           PIC 99/99/99.
       01  R-NE-R REDEFINES R-NE.
           05  R-NE-YY    PIC XX.
           05  R-NE-S1    PIC X.
           05  R-NE-MM    PIC XX.
           05  R-NE-S2    PIC X.
           05  R-NE-DD    PIC XX.
       01  R-JR           PIC X(8) JUSTIFIED RIGHT.
       01  R-JR-R REDEFINES R-JR.
           05  R-JR-2     PIC XX.
           05  R-JR-6     PIC X(6).
       01  R-GRP.
           05  R-G-A      PIC X(5).
           05  R-G-B      PIC X(3).
       01  R-SN           PIC S9(3).
       01  R-ND           PIC 9(3)V99.
       01  R-ND-R REDEFINES R-ND.
           05  R-ND-INT   PIC 9(3).
           05  R-ND-FRC   PIC 99.
       01  R-DWX          PIC X(3).
       01  R-DWX-R REDEFINES R-DWX.
           05  R-DWX-1    PIC X.
           05  R-DWX-2    PIC XX.
       01  R-T10          PIC 9(10).
       01  R-T10-R REDEFINES R-T10.
           05  R-T10-HI   PIC 99.
           05  R-T10-LO   PIC 9(8).
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM SAMPLE-PARA
               UNTIL WS-D1 = WS-D2 OR WS-TRIES > 3.
           IF WS-D1 = WS-D2
               DISPLAY "SAMPLES STABLE"
           ELSE
               DISPLAY "SAMPLES UNSTABLE".
           IF R-AX8-6 = WS-D1-X6 AND R-AX8-2 = SPACES
               DISPLAY "ALPHANUMERIC LEFT SPACE FILL OK"
           ELSE
               DISPLAY "ALPHANUMERIC BAD [" R-AX8 "]".
           IF R-AX3 = WS-D1-YYM
               DISPLAY "ALPHANUMERIC RIGHT TRUNCATION OK"
           ELSE
               DISPLAY "ALPHANUMERIC TRUNCATION BAD [" R-AX3 "]".
           IF R-N4 = WS-D1-MMDD
               DISPLAY "NUMERIC HIGH-ORDER TRUNCATION OK"
           ELSE
               DISPLAY "NUMERIC TRUNCATION BAD " R-N4.
           IF R-NE-YY = WS-D1-YY AND R-NE-S1 = "/"
              AND R-NE-MM = WS-D1-MM AND R-NE-S2 = "/"
              AND R-NE-DD = WS-D1-DD
               DISPLAY "NUMERIC EDITED OK"
           ELSE
               DISPLAY "NUMERIC EDITED BAD [" R-NE "]".
           IF R-JR-2 = SPACES AND R-JR-6 = WS-D1-X6
               DISPLAY "JUSTIFIED RIGHT OK"
           ELSE
               DISPLAY "JUSTIFIED RIGHT BAD [" R-JR "]".
           IF R-G-A = WS-DY-X AND R-G-B = SPACES
               DISPLAY "GROUP RECEIVER OK"
           ELSE
               DISPLAY "GROUP RECEIVER BAD [" R-GRP "]".
           IF R-SN = WS-DW AND R-SN IS POSITIVE
               DISPLAY "SIGNED RECEIVER OK"
           ELSE
               DISPLAY "SIGNED RECEIVER BAD".
           IF R-ND = WS-DW AND R-ND-INT = WS-DW AND R-ND-FRC = 0
               DISPLAY "DECIMAL ALIGNMENT OK"
           ELSE
               DISPLAY "DECIMAL ALIGNMENT BAD " R-ND.
           IF R-DWX-1 = WS-DW-X AND R-DWX-2 = SPACES
               DISPLAY "DAY-OF-WEEK ALPHANUMERIC OK"
           ELSE
               DISPLAY "DAY-OF-WEEK ALPHANUMERIC BAD [" R-DWX "]".
           IF R-T10-HI = 0 AND R-T10-LO NOT > 23595999
               DISPLAY "TIME ZERO FILL OK"
           ELSE
               DISPLAY "TIME ZERO FILL BAD " R-T10.
           STOP RUN.
       SAMPLE-PARA.
           ADD 1 TO WS-TRIES.
           MOVE "ZZZZZZZZ" TO R-AX8.
           MOVE "ZZZZZZZZ" TO R-JR.
           MOVE "ZZZZZZZZ" TO R-GRP.
           MOVE "ZZZ" TO R-DWX.
           MOVE 999 TO R-SN R-ND.
           MOVE 9999999999 TO R-T10.
           ACCEPT WS-D1 FROM DATE.
           ACCEPT WS-DY FROM DAY.
           ACCEPT WS-DW FROM DAY-OF-WEEK.
           ACCEPT R-AX8 FROM DATE.
           ACCEPT R-AX3 FROM DATE.
           ACCEPT R-N4 FROM DATE.
           ACCEPT R-NE FROM DATE.
           ACCEPT R-JR FROM DATE.
           ACCEPT R-GRP FROM DAY.
           ACCEPT R-SN FROM DAY-OF-WEEK.
           ACCEPT R-ND FROM DAY-OF-WEEK.
           ACCEPT R-DWX FROM DAY-OF-WEEK.
           ACCEPT R-T10 FROM TIME.
           ACCEPT WS-D2 FROM DATE.
