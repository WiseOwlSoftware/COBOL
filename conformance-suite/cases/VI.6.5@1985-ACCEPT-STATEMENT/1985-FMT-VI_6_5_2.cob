      *>--- conformance-case
      *> id: 1985-FMT-VI_6_5_2
      *> rules: [1985-FMT-VI.6.5.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.5.2 (THE ACCEPT STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ACCDT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CONSOLE IS CON-DEV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-DATE     PIC 9(6) VALUE ZERO.
       01  WS-DATE-R REDEFINES WS-DATE.
           05  WS-YY   PIC 99.
           05  WS-MM   PIC 99.
           05  WS-DD   PIC 99.
       01  WS-DAY      PIC 9(5) VALUE ZERO.
       01  WS-DAY-R REDEFINES WS-DAY.
           05  WS-DYY  PIC 99.
           05  WS-DDD  PIC 999.
       01  WS-DOW      PIC 9 VALUE ZERO.
       01  WS-TIME     PIC 9(8) VALUE 99999999.
       01  WS-TIME-R REDEFINES WS-TIME.
           05  WS-HH   PIC 99.
           05  WS-MI   PIC 99.
           05  WS-SS   PIC 99.
           05  WS-HS   PIC 99.
       01  WS-IN       PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           ACCEPT WS-DATE FROM DATE.
           IF WS-MM > 0 AND WS-MM < 13 AND WS-DD > 0 AND WS-DD < 32
               DISPLAY "DATE OK"
           ELSE
               DISPLAY "DATE BAD".
           ACCEPT WS-DAY FROM DAY.
           IF WS-DDD > 0 AND WS-DDD < 367
               DISPLAY "DAY OK"
           ELSE
               DISPLAY "DAY BAD".
           ACCEPT WS-DOW FROM DAY-OF-WEEK.
           IF WS-DOW > 0 AND WS-DOW < 8
               DISPLAY "DAY-OF-WEEK OK"
           ELSE
               DISPLAY "DAY-OF-WEEK BAD".
           ACCEPT WS-TIME FROM TIME.
           IF WS-HH < 24 AND WS-MI < 60 AND WS-SS < 60
               DISPLAY "TIME OK"
           ELSE
               DISPLAY "TIME BAD".
           IF WS-DOW = 9
               ACCEPT WS-IN
               ACCEPT WS-IN FROM CON-DEV
               DISPLAY WS-IN.
           STOP RUN.
