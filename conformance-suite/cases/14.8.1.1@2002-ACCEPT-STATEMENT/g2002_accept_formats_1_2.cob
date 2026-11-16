      *>--- conformance-case
      *> id: g2002_accept_formats_1_2
      *> rules: [2002-FMT-14.8.1.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.1.1 - the general formats of the ACCEPT statement.
      *> Format 1 (hardware): "ACCEPT identifier-1 [ FROM
      *> mnemonic-name-1 ] [ END-ACCEPT ]"; Format 2 (chronological):
      *> "ACCEPT identifier-2 FROM" DATE [YYYYMMDD] | DAY [YYYYDDD] |
      *> DAY-OF-WEEK | TIME "[ END-ACCEPT ]".
      *> Every arm of both formats is written below, with and without
      *> END-ACCEPT, so that the program translates is the first
      *> assertion: a grammar missing any arm refuses it.
      *> Format 1's device input is not deterministic in a run whose
      *> input is closed (§2002:14.8.1.3 GR1: "Any conversion of data
      *> required between the hardware device and the data item
      *> referenced by identifier-1 is defined by the implementor"),
      *> so its statements sit in a branch that never runs (NEVER = 0):
      *> translation is their check. Mnemonic-name-1 is "associated with
      *> a hardware device" in SPECIAL-NAMES (§2002:14.8.1.2 SR2).
      *> Format 2 runs. The current date and time are not literal
      *> expectations, so each value's SHAPE is pinned from its 2002
      *> general rule:
      *>   GR7  DATE: six digits, month 01-12, day 01-31     -> DATE OK
      *>   GR8  DATE YYYYMMDD: eight digits, same ranges     -> DATE8 OK
      *>   GR9  DAY: five digits, day of year 001-366        -> DAY OK
      *>   GR10 DAY YYYYDDD: seven digits, 001-366           -> DAY7 OK
      *>   GR12 DAY-OF-WEEK: one digit, 1 (Monday)-7 (Sunday) -> DOW OK
      *>   GR11 TIME: hours 00-23, minutes 00-59, seconds 00-59 (no
      *>        LEAP-SECOND directive is in effect)         -> TIME OK
      *> Each receiver is first set to a value outside its ranges, so an
      *> ACCEPT that moves nothing fails its line; one that delivers the
      *> two-digit year where four belong, or the fields in another
      *> order, puts a month or day of year out of range.
      *> Format 3 (screen) is not here: the screen formats "are
      *> dependent on the functionality of the processor" (§2002:B.3)
      *> and WiseOwl declines them at 2002 under §2002:3.1.5.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ACC04.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYSIN IS IN-DEV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NEVER PIC 9 VALUE 0.
       01 X5 PIC X(5).
       01 D6 PIC 9(6) VALUE 999999.
       01 D6R REDEFINES D6.
          05 D6-YY PIC 99.
          05 D6-MM PIC 99.
          05 D6-DD PIC 99.
       01 D8 PIC 9(8) VALUE 99999999.
       01 D8R REDEFINES D8.
          05 D8-YYYY PIC 9(4).
          05 D8-MM PIC 99.
          05 D8-DD PIC 99.
       01 J5 PIC 9(5) VALUE 99999.
       01 J5R REDEFINES J5.
          05 J5-YY PIC 99.
          05 J5-DDD PIC 999.
       01 J7 PIC 9(7) VALUE 9999999.
       01 J7R REDEFINES J7.
          05 J7-YYYY PIC 9(4).
          05 J7-DDD PIC 999.
       01 W1 PIC 9 VALUE 9.
       01 T8 PIC 9(8) VALUE 99999999.
       01 T8R REDEFINES T8.
          05 T8-HH PIC 99.
          05 T8-MI PIC 99.
          05 T8-SS PIC 99.
          05 T8-CC PIC 99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF NEVER = 1
               ACCEPT X5
               ACCEPT X5 END-ACCEPT
               ACCEPT X5 FROM IN-DEV
               ACCEPT X5 FROM IN-DEV END-ACCEPT
           END-IF.
           ACCEPT D6 FROM DATE.
           ACCEPT D8 FROM DATE YYYYMMDD END-ACCEPT.
           ACCEPT J5 FROM DAY END-ACCEPT.
           ACCEPT J7 FROM DAY YYYYDDD.
           ACCEPT W1 FROM DAY-OF-WEEK END-ACCEPT.
           ACCEPT T8 FROM TIME END-ACCEPT.
           IF D6-MM >= 1 AND D6-MM <= 12 AND D6-DD >= 1
              AND D6-DD <= 31
               DISPLAY "DATE OK"
           ELSE
               DISPLAY "DATE BAD " D6.
           IF D8-MM >= 1 AND D8-MM <= 12 AND D8-DD >= 1
              AND D8-DD <= 31 AND D8-YYYY >= 1000
               DISPLAY "DATE8 OK"
           ELSE
               DISPLAY "DATE8 BAD " D8.
           IF J5-DDD >= 1 AND J5-DDD <= 366
               DISPLAY "DAY OK"
           ELSE
               DISPLAY "DAY BAD " J5.
           IF J7-DDD >= 1 AND J7-DDD <= 366 AND J7-YYYY >= 1000
               DISPLAY "DAY7 OK"
           ELSE
               DISPLAY "DAY7 BAD " J7.
           IF W1 >= 1 AND W1 <= 7
               DISPLAY "DOW OK"
           ELSE
               DISPLAY "DOW BAD " W1.
           IF T8-HH <= 23 AND T8-MI <= 59 AND T8-SS <= 59
               DISPLAY "TIME OK"
           ELSE
               DISPLAY "TIME BAD " T8.
           STOP RUN.
