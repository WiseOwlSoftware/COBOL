      *>--- conformance-case
      *> id: g2-accept-temporal-move-rules
      *> rules: [2002-GR-14.8.1.3-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.1.3 GR6 - "The ACCEPT statement causes the
      *> information requested to be transferred to the data item
      *> specified by identifier-2 according to the rules for the MOVE
      *> statement." GR7-GR12 describe the conceptual items: DATE an
      *> unsigned integer 9(6) YYMMDD, DATE YYYYMMDD 9(8), DAY 9(5)
      *> YYDDD, DAY YYYYDDD 9(7), TIME 9(8), DAY-OF-WEEK 9(1), 1 =
      *> Monday ... 7 = Sunday.
      *> The current date is not known in advance, so every line
      *> prints a derived FACT, never a date value:
      *>  - SAME: ACCEPT R FROM DAY-OF-WEEK equals MOVE DOW1 TO R'
      *>    (R' described as R, DOW1 PIC 9 accepted from DAY-OF-WEEK,
      *>    i.e. the conceptual item itself), and SHAPE: the result
      *>    has the form the MOVE rules give - alphanumeric receiver
      *>    left-justified, space-filled; numeric receiver aligned on
      *>    the decimal point, zero-filled; edited and sign-separate
      *>    receivers edited.
      *>  - DATE YYYYMMDD into 9(6) keeps the low-order six digits
      *>    (MOVE truncation), which are exactly DATE (YYMMDD); DAY
      *>    YYYYDDD into 9(5) likewise equals DAY.
      *>  - DATE into X(8): the six digits, then two spaces.
      *>  - TIME into X(10): eight digits, then two spaces.
      *>  - DAY-OF-WEEK agrees with FUNCTION INTEGER-OF-DATE (day 1 is
      *>    Monday 1601-01-01), so a wrong day number fails.
      *>  - Receivers in a record under an input CD (the 2002-only
      *>    place for identifier-2) take the same transfer.
      *> The block repeats if the date changed while it ran.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ACC06.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 YMD-A  PIC 9(8).
       01 YMD-B  PIC 9(8).
       01 DOW1   PIC 9.
       01 D6     PIC 9(6).
       01 DY5    PIC 9(5).
       01 CALC   PIC 9.
       01 R-X    PIC X(3).
       01 T-X    PIC X(3).
       01 R-99   PIC 99.
       01 T-99   PIC 99.
       01 R-9V9  PIC 9V9.
       01 T-9V9  PIC 9V9.
       01 R-ED   PIC ZZ9.99.
       01 T-ED   PIC ZZ9.99.
       01 ED-X   PIC X(6).
       01 R-S    PIC S9(3) SIGN TRAILING SEPARATE.
       01 R-SG REDEFINES R-S PIC X(4).
       01 T-S    PIC S9(3) SIGN TRAILING SEPARATE.
       01 R-N6   PIC 9(6).
       01 R-N5   PIC 9(5).
       01 R-D8   PIC X(8).
       01 R-T10  PIC X(10).
       01 T-CD   PIC X(2).
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           SYMBOLIC QUEUE IS IN-QUEUE
           SYMBOLIC SOURCE IS IN-SOURCE
           TEXT LENGTH IS IN-LEN
           END KEY IS IN-END
           STATUS KEY IS IN-STATUS
           MESSAGE COUNT IS IN-COUNT.
       01  CDIN-REC.
           05  CD-D   PIC X(2).
           05  CD-YD  PIC 9(7).
           05  CD-N6  PIC 9(6).
           05  FILLER PIC X(72).
       PROCEDURE DIVISION.
       MAIN.
           PERFORM TEST AFTER UNTIL YMD-A = YMD-B
               ACCEPT YMD-A FROM DATE YYYYMMDD
               ACCEPT DOW1 FROM DAY-OF-WEEK
               ACCEPT D6 FROM DATE
               ACCEPT DY5 FROM DAY
               ACCEPT R-X FROM DAY-OF-WEEK
               ACCEPT R-99 FROM DAY-OF-WEEK
               ACCEPT R-9V9 FROM DAY-OF-WEEK
               ACCEPT R-ED FROM DAY-OF-WEEK
               ACCEPT R-S FROM DAY-OF-WEEK
               ACCEPT CD-D FROM DAY-OF-WEEK
               ACCEPT R-N6 FROM DATE YYYYMMDD
               ACCEPT CD-N6 FROM DATE YYYYMMDD
               ACCEPT R-N5 FROM DAY YYYYDDD
               ACCEPT CD-YD FROM DAY YYYYDDD
               ACCEPT R-D8 FROM DATE
               ACCEPT YMD-B FROM DATE YYYYMMDD
           END-PERFORM
           ACCEPT R-T10 FROM TIME
           MOVE DOW1 TO T-X T-99 T-9V9 T-ED T-S T-CD
           COMPUTE CALC = FUNCTION MOD
               (FUNCTION INTEGER-OF-DATE (YMD-A) - 1, 7) + 1
           IF DOW1 = CALC AND DOW1 >= 1 AND DOW1 <= 7
               DISPLAY "DOW AGREES WITH INTEGER-OF-DATE"
           ELSE DISPLAY "DOW WRONG " DOW1 " " CALC END-IF
           IF R-X = T-X AND R-X(2:2) = SPACES AND R-X(1:1) = DOW1
               DISPLAY "X SAME LEFT-JUSTIFIED SPACE-FILLED"
           ELSE DISPLAY "X WRONG [" R-X "]" END-IF
           IF R-99 = T-99 AND R-99 = DOW1
               DISPLAY "99 SAME ZERO-FILLED"
           ELSE DISPLAY "99 WRONG [" R-99 "]" END-IF
           IF R-9V9 = T-9V9 AND R-9V9 = DOW1
               DISPLAY "9V9 SAME DECIMAL-ALIGNED"
           ELSE DISPLAY "9V9 WRONG" END-IF
           MOVE R-ED TO ED-X
           IF R-ED = T-ED AND ED-X(1:2) = SPACES AND ED-X(4:3) = ".00"
              AND ED-X(3:1) = DOW1
               DISPLAY "ED SAME EDITED"
           ELSE DISPLAY "ED WRONG [" R-ED "]" END-IF
           IF R-S = T-S AND R-SG(1:2) = "00" AND R-SG(4:1) = "+"
               DISPLAY "SIGN SAME TRAILING SEPARATE"
           ELSE DISPLAY "SIGN WRONG [" R-SG "]" END-IF
           IF CD-D = T-CD AND CD-D(2:1) = SPACE AND CD-D(1:1) = DOW1
               DISPLAY "CD SAME LEFT-JUSTIFIED"
           ELSE DISPLAY "CD WRONG [" CD-D "]" END-IF
           IF R-N6 = D6 AND CD-N6 = D6
               DISPLAY "DATE YYYYMMDD TRUNCATED TO DATE"
           ELSE DISPLAY "DATE WRONG " R-N6 " " CD-N6 " " D6 END-IF
           IF R-N5 = DY5 AND CD-YD(1:4) = YMD-A(1:4)
              AND CD-YD(3:5) = DY5
               DISPLAY "DAY YYYYDDD TRUNCATED TO DAY"
           ELSE DISPLAY "DAY WRONG " R-N5 " " CD-YD " " DY5 END-IF
           IF R-D8(1:6) = D6 AND R-D8(7:2) = SPACES
               DISPLAY "DATE INTO X(8) SPACE-FILLED"
           ELSE DISPLAY "DATE X WRONG [" R-D8 "]" END-IF
           IF R-T10(1:8) IS NUMERIC AND R-T10(9:2) = SPACES
               DISPLAY "TIME INTO X(10) SPACE-FILLED"
           ELSE DISPLAY "TIME X WRONG [" R-T10 "]" END-IF
           STOP RUN.
