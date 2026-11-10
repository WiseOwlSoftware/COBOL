      *>--- conformance-case
      *> id: 1985-GR-VI_6_9_4-1
      *> rules: [1985-GR-VI.6.9.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.9.4 GR1 (THE CONTINUE STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. CONTTEST.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01  X PIC 9 VALUE 1.
000600 PROCEDURE DIVISION.
000700 MAIN-PARA.
000800     DISPLAY "BEFORE".
000900     CONTINUE.
001000     DISPLAY "X=" X.
001100     STOP RUN.
