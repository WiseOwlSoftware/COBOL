      *>--- conformance-case
      *> id: 1985-GR-XVI_1_4_3-L2_1
      *> rules: [1985-GR-XVI.1.4.3-L2.1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.1.4.3 GRL2.1 (ORGANIZATION). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. SEGLAST.
000300 ENVIRONMENT DIVISION.
000400 DATA DIVISION.
000500 PROCEDURE DIVISION.
000600 MAIN-SEC SECTION 0.
000700 M1.
000800     GO TO I0.
000900 F1.
001000     DISPLAY "FIXED".
001100 F2.
001200     STOP RUN.
001300 IND-SEC SECTION 50.
001400 I0.
001500     GO TO I1.
001600 I1.
001700     DISPLAY "INITIAL".
001800     ALTER I0 TO PROCEED TO I2.
001900     PERFORM F1.
002000     GO TO I0.
002100 I2.
002200     DISPLAY "ALTERED".
002300     STOP RUN.
