      *>--- conformance-case
      *> id: 1985-GR-VI_6_15_4-2
      *> rules: [1985-GR-VI.6.15.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.15.4 GR2 (THE GO TO STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. ALTGO.
000300 ENVIRONMENT DIVISION.
000400 DATA DIVISION.
000500 PROCEDURE DIVISION.
000600 MAIN-PARA.
000700     DISPLAY "START".
000800     ALTER SWITCH-PARA TO PROCEED TO TARGET-PARA.
000900     GO TO SWITCH-PARA.
001000 SWITCH-PARA.
001100     GO TO.
001200 WRONG-PARA.
001300     DISPLAY "WRONG".
001400     STOP RUN.
001500 TARGET-PARA.
001600     DISPLAY "ALTERED".
001700     STOP RUN.
