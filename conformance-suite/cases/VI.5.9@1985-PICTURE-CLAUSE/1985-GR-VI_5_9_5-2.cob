      *>--- conformance-case
      *> id: 1985-GR-VI_5_9_5-2
      *> rules: [1985-GR-VI.5.9.5-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.9.5 GR2 (THE PICTURE CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. EDTEST.
000300 ENVIRONMENT DIVISION.
000400 DATA DIVISION.
000500 WORKING-STORAGE SECTION.
000600 01  W-SRC  PIC X(6) VALUE "ABCDEF".
000700 01  W-ED   PIC XX/XXBXX0.
000800 PROCEDURE DIVISION.
000900 P1.
001000     MOVE W-SRC TO W-ED.
001100     DISPLAY W-ED.
001200     STOP RUN.
