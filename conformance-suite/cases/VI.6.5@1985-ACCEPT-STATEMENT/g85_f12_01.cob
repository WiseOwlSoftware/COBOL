      *>--- conformance-case
      *> id: g85_f12_01
      *> rules: [1985-GR-VI.6.5.4-7, 1985-GR-VI.6.5.4-8, 1985-GR-VI.6.5.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.5.4 GR7 - DATE is YYMMDD, an unsigned integer six
      *> digits long: into a 9(8) receiver it zero-fills two high-order
      *> digits and the low six equal a 9(6) DATE (an eight-digit DATE
      *> fails); its month is 01-12 and its day 01 to the month's length.
      *> §1985:VI.6.5.4 GR8 - DAY is YYDDD, five digits long: a 9(7)
      *> receiver zero-fills two digits; its year of century equals
      *> DATE's and its day of year equals the one computed from DATE's
      *> month and day (a DDD that is not the ordinal day fails).
      *> §1985:VI.6.5.4 GR10 - DAY-OF-WEEK is one digit, 1 = Monday ...
      *> 7 = Sunday: it equals the weekday computed from DATE (counting
      *> days from Saturday 1 January 2000; a 0 = Sunday or 1 = Sunday
      *> numbering fails). The clock is read, never pinned: every value
      *> checked is a relation, so the output is the same on any day of
      *> 2000-2099 (year of century alone gives no century). The reads
      *> are repeated until two DATE samples bracketing them agree, so a
      *> run across midnight cannot mix two days.
      *> Lines: 1 sampling; 2-3 GR7; 4-6 GR8; 7-8 GR10.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1201.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-D1          PIC 9(6) VALUE 0.
       01  WS-D1-R REDEFINES WS-D1.
           05  WS-YY      PIC 99.
           05  WS-MM      PIC 99.
           05  WS-DD      PIC 99.
       01  WS-D2          PIC 9(6) VALUE 1.
       01  WS-DY          PIC 9(5).
       01  WS-DY-R REDEFINES WS-DY.
           05  WS-DY-YY   PIC 99.
           05  WS-DY-DDD  PIC 999.
       01  WS-DW          PIC 9.
       01  WS-D8          PIC 9(8).
       01  WS-D8-R REDEFINES WS-D8.
           05  WS-D8-HI   PIC 99.
           05  WS-D8-LO   PIC 9(6).
       01  WS-DY7         PIC 9(7).
       01  WS-DY7-R REDEFINES WS-DY7.
           05  WS-DY7-HI  PIC 99.
           05  WS-DY7-LO  PIC 9(5).
       01  WS-DW3         PIC 9(3).
       01  WS-TRIES       PIC 9 VALUE 0.
       01  CUM-VALUES.
           05  FILLER     PIC X(36)
               VALUE "000031059090120151181212243273304334".
       01  CUM-TABLE REDEFINES CUM-VALUES.
           05  CUM-DAYS   PIC 999 OCCURS 12 TIMES.
       01  LEN-VALUES.
           05  FILLER     PIC X(24)
               VALUE "312831303130313130313031".
       01  LEN-TABLE REDEFINES LEN-VALUES.
           05  MON-LEN    PIC 99 OCCURS 12 TIMES.
       01  WS-Q           PIC 9(4).
       01  WS-R           PIC 9.
       01  WS-LEAP        PIC 9.
       01  WS-MAXDD       PIC 99.
       01  WS-DOY         PIC 999 VALUE 0.
       01  WS-TOTAL       PIC 9(6).
       01  WS-EXPDW       PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM SAMPLE-PARA
               UNTIL WS-D1 = WS-D2 OR WS-TRIES > 3.
           IF WS-D1 = WS-D2
               DISPLAY "SAMPLES STABLE"
           ELSE
               DISPLAY "SAMPLES UNSTABLE".
      *    GR7: six digits, YYMMDD.
           IF WS-D8-HI = 0 AND WS-D8-LO = WS-D1
               DISPLAY "DATE IS SIX DIGITS"
           ELSE
               DISPLAY "DATE NOT SIX DIGITS " WS-D8.
           DIVIDE WS-YY BY 4 GIVING WS-Q REMAINDER WS-R.
           IF WS-R = 0
               MOVE 1 TO WS-LEAP
           ELSE
               MOVE 0 TO WS-LEAP.
           IF WS-MM > 0 AND WS-MM < 13
               MOVE MON-LEN (WS-MM) TO WS-MAXDD
               IF WS-MM = 2
                   ADD WS-LEAP TO WS-MAXDD.
           IF WS-MM > 0 AND WS-MM < 13
              AND WS-DD > 0 AND WS-DD NOT > WS-MAXDD
               DISPLAY "DATE MONTH AND DAY OK"
           ELSE
               DISPLAY "DATE MONTH AND DAY BAD " WS-D1.
      *    GR8: five digits, YYDDD agreeing with DATE.
           IF WS-DY7-HI = 0 AND WS-DY7-LO = WS-DY
               DISPLAY "DAY IS FIVE DIGITS"
           ELSE
               DISPLAY "DAY NOT FIVE DIGITS " WS-DY7.
           IF WS-DY-YY = WS-YY
               DISPLAY "DAY YEAR OF CENTURY OK"
           ELSE
               DISPLAY "DAY YEAR OF CENTURY BAD " WS-DY.
           IF WS-MM > 0 AND WS-MM < 13
               COMPUTE WS-DOY = CUM-DAYS (WS-MM) + WS-DD
               IF WS-MM > 2
                   ADD WS-LEAP TO WS-DOY.
           IF WS-DY-DDD = WS-DOY
               DISPLAY "DAY OF YEAR OK"
           ELSE
               DISPLAY "DAY OF YEAR BAD " WS-DY " " WS-DOY.
      *    GR10: one digit, 1 = Monday ... 7 = Sunday.
           IF WS-DW3 = WS-DW AND WS-DW3 < 10
               DISPLAY "DAY-OF-WEEK IS ONE DIGIT"
           ELSE
               DISPLAY "DAY-OF-WEEK NOT ONE DIGIT " WS-DW3.
           COMPUTE WS-Q = (WS-YY + 3) / 4.
           COMPUTE WS-TOTAL = WS-YY * 365 + WS-Q + WS-DOY - 1 + 5.
           DIVIDE WS-TOTAL BY 7 GIVING WS-Q REMAINDER WS-R.
           COMPUTE WS-EXPDW = WS-R + 1.
           IF WS-DW = WS-EXPDW
               DISPLAY "DAY-OF-WEEK MONDAY IS 1 OK"
           ELSE
               DISPLAY "DAY-OF-WEEK BAD " WS-DW " " WS-EXPDW.
           STOP RUN.
       SAMPLE-PARA.
           ADD 1 TO WS-TRIES.
           ACCEPT WS-D1 FROM DATE.
           ACCEPT WS-DY FROM DAY.
           ACCEPT WS-DW FROM DAY-OF-WEEK.
           ACCEPT WS-D8 FROM DATE.
           ACCEPT WS-DY7 FROM DAY.
           ACCEPT WS-DW3 FROM DAY-OF-WEEK.
           ACCEPT WS-D2 FROM DATE.
