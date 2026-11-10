      *>--- conformance-case
      *> id: 1985-GR-VI_6_8_4-3
      *> rules: [1985-GR-VI.6.8.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.8.4 GR3 (THE COMPUTE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. CMPTST.
000300 ENVIRONMENT DIVISION.
000400 DATA DIVISION.
000500 WORKING-STORAGE SECTION.
000600 01  A        PIC 9(10)  VALUE 5.
000700 01  B        PIC V9(10) VALUE .5.
000800 01  R        PIC 9.9.
000900 PROCEDURE DIVISION.
001000 P1.
001100     COMPUTE R = A + B.
001200     DISPLAY R.
001300     STOP RUN.
