      *>--- conformance-case
      *> id: 1985-GR-VI_6_11_4-3
      *> rules: [1985-GR-VI.6.11.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.11.4 GR3 (THE DIVIDE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. DIVTST.
000300 ENVIRONMENT DIVISION.
000400 DATA DIVISION.
000500 WORKING-STORAGE SECTION.
000600 01 A PIC 9(3) VALUE 20.
000700 01 B PIC 9(3) VALUE 4.
000800 01 C PIC 9(3) VALUE 0.
000900 PROCEDURE DIVISION.
001000 P1.
001100     DIVIDE A BY B GIVING C.
001200     DISPLAY C.
001300     DIVIDE 60 BY 3 GIVING C.
001400     DISPLAY C.
001500     STOP RUN.
