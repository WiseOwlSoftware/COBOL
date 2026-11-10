      *>--- conformance-case
      *> id: 1985-GR-IV_4_2_2_2_3-1
      *> rules: [1985-GR-IV.4.2.2.2.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.2.3 GR1 (Literals). An output test: the program
      *> must translate and run at 85 and DISPLAY exactly the lines its
      *> .out holds, the outcome the rule requires. Placed from the
      *> audit's draft, confirmed by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. ZEROTEST.
000300 ENVIRONMENT DIVISION.
000400 DATA DIVISION.
000500 WORKING-STORAGE SECTION.
000600 01  WS-NUM   PIC 9(3) VALUE 123.
000700 01  WS-ALPHA PIC X(4) VALUE "ABCD".
000800 01  WS-ALP2  PIC X(3) VALUE "XYZ".
000900 PROCEDURE DIVISION.
001000 MAIN-PARA.
001100     MOVE ZERO TO WS-NUM.
001200     DISPLAY WS-NUM.
001300     IF WS-NUM = ZERO
001400         DISPLAY "NUMERIC ZERO".
001500     MOVE ALL ZEROES TO WS-ALPHA.
001600     DISPLAY WS-ALPHA.
001700     MOVE ZEROS TO WS-ALP2.
001800     DISPLAY WS-ALP2.
001900     STOP RUN.
