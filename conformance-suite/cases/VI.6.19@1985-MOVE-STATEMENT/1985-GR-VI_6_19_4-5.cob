      *>--- conformance-case
      *> id: 1985-GR-VI_6_19_4-5
      *> rules: [1985-GR-VI.6.19.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.19.4 GR5 (THE MOVE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. GRPMOVE.
000300 ENVIRONMENT DIVISION.
000400 DATA DIVISION.
000500 WORKING-STORAGE SECTION.
000600 01  G1.
000700     05  A1  PIC X(3) VALUE "ABC".
000800     05  B1  PIC 9(2) VALUE 12.
000900 01  G2.
001000     05  C2  PIC 9(2).
001100     05  D2  PIC X(2).
001200 PROCEDURE DIVISION.
001300 P1.
001400     MOVE G1 TO G2.
001500     DISPLAY G2.
001600     DISPLAY D2.
001700     STOP RUN.
