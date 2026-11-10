      *>--- conformance-case
      *> id: 1985-GR-VI_6_5_4-9
      *> rules: [1985-GR-VI.6.5.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.5.4 GR9 (THE ACCEPT STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TIMETEST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T-NUM PIC 9(8).
       01  T-PARTS REDEFINES T-NUM.
           05  T-HH PIC 99.
           05  T-MM PIC 99.
           05  T-SS PIC 99.
           05  T-CC PIC 99.
       01  T-AN PIC X(10) VALUE "ZZZZZZZZZZ".
       01  T-AN-PARTS REDEFINES T-AN.
           05  T-AN-8 PIC X(8).
           05  T-AN-REST PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           ACCEPT T-NUM FROM TIME.
           IF T-NUM IS NUMERIC AND T-NUM NOT > 23595999
              AND T-HH < 24 AND T-MM < 60 AND T-SS < 60
               DISPLAY "TIME OK"
           ELSE
               DISPLAY "TIME BAD".
           ACCEPT T-AN FROM TIME.
           IF T-AN-8 IS NUMERIC AND T-AN-REST = SPACES
               DISPLAY "EIGHT DIGITS"
           ELSE
               DISPLAY "NOT EIGHT DIGITS".
           STOP RUN.
