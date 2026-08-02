      *> PB11 - the ISO 15.3.1-15.3.4 date/time FORMAT grammar is now RECOGNISED, not merely spot-checked.
      *> 15.39.3 r2 requires a DATE format, 15.41.3 r2 a TIME format and 15.40.3 r2 a COMBINED one. Nothing
      *> asked that question before: CobolDate.Tokenize validated character CLASSES and field WIDTHS, so any
      *> string assembled from legal subfields was accepted and the function FABRICATED a value -
      *> FORMATTED-DATE("hhmmss" ...) returned "000000".
      *>
      *> This pins the ACCEPT side: every one of the six date formats 15.3.1.1 enumerates, the common-time
      *> shapes of 15.3.3.1/15.3.3.2, the UTC and offset forms of 15.3.3.5/15.3.3.6, and both combined forms
      *> of 15.3.4. The reject side is the negative corpus (pb11-*), because a recognizer that accepts
      *> everything would pass this file and still be wrong.
      *>
      *> Integer date 100000 is 1874-10-16 (15.5.2 puts day 1 at 1601-01-01), which is day 289 of 1874 and
      *> falls in week 42 on a Friday - so the ordinal and week forms are cross-checks on each other, not three
      *> independent assertions of the same thing.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB11FMTGRAM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T PIC X(22).
       PROCEDURE DIVISION.
      *> 15.3.1.2 calendar, basic and extended
           MOVE FUNCTION FORMATTED-DATE("YYYYMMDD" 100000) TO T
           DISPLAY "CAL-B=" T
           MOVE FUNCTION FORMATTED-DATE("YYYY-MM-DD" 100000) TO T
           DISPLAY "CAL-E=" T
      *> 15.3.1.4 ordinal, basic and extended
           MOVE FUNCTION FORMATTED-DATE("YYYYDDD" 100000) TO T
           DISPLAY "ORD-B=" T
           MOVE FUNCTION FORMATTED-DATE("YYYY-DDD" 100000) TO T
           DISPLAY "ORD-E=" T
      *> 15.3.1.6 week, basic and extended
           MOVE FUNCTION FORMATTED-DATE("YYYYWwwD" 100000) TO T
           DISPLAY "WK-B=" T
           MOVE FUNCTION FORMATTED-DATE("YYYY-Www-D" 100000) TO T
           DISPLAY "WK-E=" T
      *> 15.3.3.1/15.3.3.2 common time: integer and fractional seconds, basic and extended
           MOVE FUNCTION FORMATTED-TIME("hhmmss" 3600) TO T
           DISPLAY "T-B=" T
           MOVE FUNCTION FORMATTED-TIME("hh:mm:ss.ss" 3600) TO T
           DISPLAY "T-EF=" T
      *> 15.3.3.5 UTC, 15.3.3.6 offset (extended offset subformat pairs with the extended common part)
           MOVE FUNCTION FORMATTED-TIME("hhmmssZ" 3600) TO T
           DISPLAY "T-UTC=" T
           MOVE FUNCTION FORMATTED-TIME("hh:mm:ss+hh:mm" 3600 60) TO T
           DISPLAY "T-OFF=" T
      *> 15.3.4 combined: basic with basic, extended with extended
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss" 100000 3600) TO T
           DISPLAY "DT-B=" T
           MOVE FUNCTION FORMATTED-DATETIME("YYYY-MM-DDThh:mm:ss" 100000 3600) TO T
           DISPLAY "DT-E=" T
           STOP RUN.
