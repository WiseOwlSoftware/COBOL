      *>--- conformance-case
      *> id: 1985-FMT-X_5_4_2
      *> rules: [1985-FMT-X.5.4.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.4.2 (THE EXIT PROGRAM STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. EXITPG.
000300 ENVIRONMENT DIVISION.
000400 DATA DIVISION.
000500 PROCEDURE DIVISION.
000600 P1.
000700     DISPLAY "BEFORE EXIT".
000800     EXIT PROGRAM.
000900     DISPLAY "AFTER EXIT".
001000     STOP RUN.
