      *>--- conformance-case
      *> id: 1985-GR-XII_3_4-6
      *> rules: [1985-GR-XII.3.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.3.4 GR6 (THE REPLACE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. RPLCMP.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01  W-A PIC 9 VALUE 0.
000600     REPLACE ==ADD 1 TO W-A== BY ==ADD 5 TO W-A==.
000700 PROCEDURE DIVISION.
000800 P1.
000900     ADD 1;   TO    W-A.
001000     DISPLAY W-A.
001100     STOP RUN.
