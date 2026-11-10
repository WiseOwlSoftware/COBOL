      *>--- conformance-case
      *> id: 1985-FMT-VI_4_3_2
      *> rules: [1985-FMT-VI.4.3.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.4.3.2 (THE SOURCE-COMPUTER PARAGRAPH). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. SRCCMP.
000300 ENVIRONMENT DIVISION.
000400 CONFIGURATION SECTION.
000500 SOURCE-COMPUTER. XYZ-1 WITH DEBUGGING MODE.
000600 OBJECT-COMPUTER.
000700 PROCEDURE DIVISION.
000800 MAIN-PARA.
000900D    DISPLAY "DEBUGGING LINE".
001000     DISPLAY "SOURCE-COMPUTER OK".
001100     STOP RUN.
