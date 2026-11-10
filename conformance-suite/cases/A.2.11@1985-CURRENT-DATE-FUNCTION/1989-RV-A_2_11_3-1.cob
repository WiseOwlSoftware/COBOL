      *>--- conformance-case
      *> id: 1989-RV-A_2_11_3-1
      *> rules: [1989-RV-A.2.11.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.11.3 RV1 (THE CURRENT-DATE FUNCTION). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CDTEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-CD.
           05  WS-YEAR   PIC 9(4).
           05  WS-MONTH  PIC 9(2).
           05  WS-DAY    PIC 9(2).
           05  WS-HOUR   PIC 9(2).
           05  WS-MIN    PIC 9(2).
           05  WS-SEC    PIC 9(2).
           05  WS-HUND   PIC 9(2).
           05  WS-SIGN   PIC X.
           05  WS-OFFH   PIC 9(2).
           05  WS-OFFM   PIC 9(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE FUNCTION CURRENT-DATE TO WS-CD.
           IF WS-YEAR NUMERIC AND WS-MONTH NUMERIC AND
              WS-DAY NUMERIC AND WS-HOUR NUMERIC AND
              WS-MIN NUMERIC AND WS-SEC NUMERIC AND
              WS-HUND NUMERIC AND WS-OFFH NUMERIC AND
              WS-OFFM NUMERIC
               DISPLAY "DIGITS OK"
           ELSE
               DISPLAY "DIGITS BAD".
           IF WS-MONTH >= 1 AND WS-MONTH <= 12 AND
              WS-DAY >= 1 AND WS-DAY <= 31 AND
              WS-HOUR <= 23 AND WS-MIN <= 59 AND
              WS-SEC <= 59
               DISPLAY "RANGES OK"
           ELSE
               DISPLAY "RANGES BAD".
           IF (WS-SIGN = "0" AND WS-OFFH = 0 AND WS-OFFM = 0) OR
              (WS-SIGN = "+" AND WS-OFFH <= 13 AND WS-OFFM <= 59)
              OR
              (WS-SIGN = "-" AND WS-OFFH <= 12 AND WS-OFFM <= 59)
               DISPLAY "OFFSET OK"
           ELSE
               DISPLAY "OFFSET BAD".
           STOP RUN.
