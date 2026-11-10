      *>--- conformance-case
      *> id: 1985-GR-XII_3_4-8
      *> rules: [1985-GR-XII.3.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.3.4 GR8 (THE REPLACE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. REPDBG.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01  X PIC 9 VALUE 0.
000600 PROCEDURE DIVISION.
000700     REPLACE ==MOVE 1
000800D        TO X== BY ==MOVE 2 TO X==.
000900 P1.
001000     MOVE 1 TO X.
001100     DISPLAY X.
001200     STOP RUN.
