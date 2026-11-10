      *>--- conformance-case
      *> id: 1985-GR-VI_4_3_4-5
      *> rules: [1985-GR-VI.4.3.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.4.3.4 GR5 (THE SOURCE-COMPUTER PARAGRAPH). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. DBGTST.
000300 ENVIRONMENT DIVISION.
000400 CONFIGURATION SECTION.
000500 SOURCE-COMPUTER.
000600 PROCEDURE DIVISION.
000700 P1.
000800     DISPLAY "BEFORE".
000900D    DISPLAY "DEBUG".
001000     DISPLAY "AFTER".
001100     STOP RUN.
