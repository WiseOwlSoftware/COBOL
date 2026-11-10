      *>--- conformance-case
      *> id: 1985-GR-XVI_2_3_4-2
      *> rules: [1985-GR-XVI.2.3.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.2.3.4 GR2 (THE SEGMENT-LIMIT CLAUSE). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. SEGLIM.
000300 ENVIRONMENT DIVISION.
000400 CONFIGURATION SECTION.
000500 SOURCE-COMPUTER. ANYCOMP.
000600 OBJECT-COMPUTER. ANYCOMP
000700     SEGMENT-LIMIT IS 10.
000800 PROCEDURE DIVISION.
000900 MAIN-CTL SECTION 00.
001000 M-1.
001100     PERFORM S20.
001200     PERFORM S20.
001300     STOP RUN.
001400 S20 SECTION 20.
001500 S20-A.
001600     GO TO S20-FIRST.
001700 S20-FIRST.
001800     DISPLAY "FIRST".
001900     ALTER S20-A TO PROCEED TO S20-LATER.
002000     GO TO S20-EXIT.
002100 S20-LATER.
002200     DISPLAY "LATER".
002300 S20-EXIT.
002400     EXIT.
