      *>--- conformance-case
      *> id: 1985-GR-VI_5_9_4-5
      *> rules: [1985-GR-VI.5.9.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.9.4 GR5 (THE PICTURE CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. AEDIT.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01  W-ED1 PIC XXBXX.
000600 01  W-ED2 PIC X/X0X.
000700 PROCEDURE DIVISION.
000800 P1.
000900     MOVE "ABCD" TO W-ED1.
001000     MOVE "XYZ" TO W-ED2.
001100     DISPLAY W-ED1.
001200     DISPLAY W-ED2.
001300     STOP RUN.
