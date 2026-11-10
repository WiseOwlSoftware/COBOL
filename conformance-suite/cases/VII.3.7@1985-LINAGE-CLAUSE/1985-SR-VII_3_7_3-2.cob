      *>--- conformance-case
      *> id: 1985-SR-VII_3_7_3-2
      *> rules: [1985-SR-VII.3.7.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.7.3 SR2 (THE LINAGE CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LINQUAL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  PRT-FILE
           LINAGE IS PAGE-LEN OF LIMITS-A LINES
           WITH FOOTING AT FOOT-AT OF LIMITS-A
           LINES AT TOP TOP-M OF LIMITS-A
           LINES AT BOTTOM BOT-M OF LIMITS-A.
       01  PRT-REC PIC X(20).
       WORKING-STORAGE SECTION.
       01  LIMITS-A.
           05 PAGE-LEN PIC 99 VALUE 20.
           05 FOOT-AT  PIC 99 VALUE 18.
           05 TOP-M    PIC 99 VALUE 2.
           05 BOT-M    PIC 99 VALUE 2.
       01  LIMITS-B.
           05 PAGE-LEN PIC 99 VALUE 30.
           05 FOOT-AT  PIC 99 VALUE 28.
           05 TOP-M    PIC 99 VALUE 3.
           05 BOT-M    PIC 99 VALUE 3.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT-FILE.
           MOVE "LINE ONE" TO PRT-REC.
           WRITE PRT-REC AFTER ADVANCING 1 LINE.
           CLOSE PRT-FILE.
           DISPLAY "LINAGE OK".
           STOP RUN.
