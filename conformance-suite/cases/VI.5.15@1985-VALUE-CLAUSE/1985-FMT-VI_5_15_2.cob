      *>--- conformance-case
      *> id: 1985-FMT-VI_5_15_2
      *> rules: [1985-FMT-VI.5.15.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.15.2 (THE VALUE CLAUSE). An output test: the program
      *> must translate and run at 85 and DISPLAY exactly the lines its
      *> .out holds, the outcome the rule requires. Placed from the
      *> audit's draft, confirmed by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. VALTST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-CODE PIC 9 VALUE 5.
           88  MID-RANGE VALUES ARE 3 THRU 6 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF MID-RANGE DISPLAY "IN RANGE" ELSE DISPLAY "OUT".
           MOVE 9 TO WS-CODE.
           IF MID-RANGE DISPLAY "NINE OK".
           MOVE 7 TO WS-CODE.
           IF NOT MID-RANGE DISPLAY "SEVEN OUT".
           STOP RUN.
