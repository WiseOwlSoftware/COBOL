      *>--- conformance-case
      *> id: g85_f40_01
      *> rules: [1989-RV-A.2.46.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.46.3 RV1 - the 21 character positions of
      *> WHEN-COMPILED: lines 1-3 pin the length, the digit fields
      *> and their ranges, and the sign/offset positions 17-21 (a
      *> '0' sign carries 0000; '-' at most 12 hours; '+' at most
      *> 13); line 6 pins that 1-16 are the LOCAL time and 17-21 its
      *> difference from Greenwich Mean Time, by converting the stamp
      *> and the run's CURRENT-DATE to one Greenwich instant.
      *> §1989:A.2.46.3 RV2 - the value is the date and time of
      *> compilation of the source program: line 5 pins that a
      *> second reference, made after the run-time clock has visibly
      *> advanced (line 4), returns the identical 21 characters (a
      *> value read from the run-time clock fails it), and line 6
      *> that the instant denoted is not later than the run.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F4001.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WC.
           02  WC-DATE  PIC 9(8).
           02  WC-HOUR  PIC 99.
           02  WC-MIN   PIC 99.
           02  WC-SEC   PIC 99.
           02  WC-HUN   PIC 99.
           02  WC-SIGN  PIC X.
           02  WC-OFFH  PIC 99.
           02  WC-OFFM  PIC 99.
       01  WC-D REDEFINES WC.
           02  WC-YEAR  PIC 9(4).
           02  WC-MON   PIC 99.
           02  WC-DAY   PIC 99.
           02  FILLER   PIC X(13).
       01  RT.
           02  RT-DATE  PIC 9(8).
           02  RT-HOUR  PIC 99.
           02  RT-MIN   PIC 99.
           02  RT-SEC   PIC 99.
           02  RT-HUN   PIC 99.
           02  RT-SIGN  PIC X.
           02  RT-OFFH  PIC 99.
           02  RT-OFFM  PIC 99.
       01  RT0          PIC X(21).
       01  WC2          PIC X(21).
       01  WC-T         PIC S9(15).
       01  RT-T         PIC S9(15).
       01  N            PIC 9(9).
       01  LEN          PIC 99.
       01  FIELDS-OK    PIC X VALUE "Y".
       01  OFFSET-OK    PIC X VALUE "Y".
       01  ADVANCED     PIC X VALUE "N".
       01  SAME-STAMP   PIC X VALUE "N".
       01  NOT-AFTER    PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE FUNCTION WHEN-COMPILED TO WC.
           COMPUTE LEN = FUNCTION LENGTH (FUNCTION WHEN-COMPILED).
           DISPLAY "LENGTH " LEN.
           IF WC-DATE NOT NUMERIC OR WC-HOUR NOT NUMERIC
              OR WC-MIN NOT NUMERIC OR WC-SEC NOT NUMERIC
              OR WC-HUN NOT NUMERIC OR WC-OFFH NOT NUMERIC
              OR WC-OFFM NOT NUMERIC
               MOVE "N" TO FIELDS-OK
           ELSE
               IF WC-MON < 1 OR WC-MON > 12
                  OR WC-DAY < 1 OR WC-DAY > 31
                  OR WC-HOUR > 23 OR WC-MIN > 59
                  OR WC-SEC > 59 OR WC-OFFM > 59
                   MOVE "N" TO FIELDS-OK
               END-IF
           END-IF.
           DISPLAY "FIELDS " FIELDS-OK.
           IF WC-SIGN = "-"
               IF WC-OFFH > 12
                   MOVE "N" TO OFFSET-OK
               END-IF
           ELSE
               IF WC-SIGN = "+"
                   IF WC-OFFH > 13
                       MOVE "N" TO OFFSET-OK
                   END-IF
               ELSE
                   IF WC-SIGN = "0"
                       IF WC-OFFH NOT = 0 OR WC-OFFM NOT = 0
                           MOVE "N" TO OFFSET-OK
                       END-IF
                   ELSE
                       MOVE "N" TO OFFSET-OK
                   END-IF
               END-IF
           END-IF.
           DISPLAY "OFFSET " OFFSET-OK.
      * Wait, boundedly, until the run-time clock moves on.
           MOVE FUNCTION CURRENT-DATE TO RT0.
           MOVE RT0 TO RT.
           PERFORM VARYING N FROM 1 BY 1
                   UNTIL N > 100000000 OR RT (1:16) NOT = RT0 (1:16)
               MOVE FUNCTION CURRENT-DATE TO RT
           END-PERFORM.
           IF RT (1:16) NOT = RT0 (1:16)
               MOVE "Y" TO ADVANCED
           END-IF.
           DISPLAY "CLOCK ADVANCED " ADVANCED.
           MOVE FUNCTION WHEN-COMPILED TO WC2.
           IF WC2 = WC
               MOVE "Y" TO SAME-STAMP
           END-IF.
           DISPLAY "SAME STAMP " SAME-STAMP.
      * Both stamps as hundredths of a second, Greenwich time.
           IF FIELDS-OK = "Y" AND OFFSET-OK = "Y"
               COMPUTE WC-T = FUNCTION INTEGER-OF-DATE (WC-DATE)
                   * 8640000 + WC-HOUR * 360000 + WC-MIN * 6000
                   + WC-SEC * 100 + WC-HUN
               COMPUTE RT-T = FUNCTION INTEGER-OF-DATE (RT-DATE)
                   * 8640000 + RT-HOUR * 360000 + RT-MIN * 6000
                   + RT-SEC * 100 + RT-HUN
               IF WC-SIGN = "+"
                   COMPUTE WC-T = WC-T - WC-OFFH * 360000
                       - WC-OFFM * 6000
               END-IF
               IF WC-SIGN = "-"
                   COMPUTE WC-T = WC-T + WC-OFFH * 360000
                       + WC-OFFM * 6000
               END-IF
               IF RT-SIGN = "+"
                   COMPUTE RT-T = RT-T - RT-OFFH * 360000
                       - RT-OFFM * 6000
               END-IF
               IF RT-SIGN = "-"
                   COMPUTE RT-T = RT-T + RT-OFFH * 360000
                       + RT-OFFM * 6000
               END-IF
               IF WC-T NOT > RT-T
                   MOVE "Y" TO NOT-AFTER
               END-IF
           END-IF.
           DISPLAY "COMPILED NOT AFTER RUN " NOT-AFTER.
           STOP RUN.
