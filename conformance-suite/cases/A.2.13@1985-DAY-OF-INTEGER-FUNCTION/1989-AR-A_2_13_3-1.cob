      *>--- conformance-case
      *> id: 1989-AR-A_2_13_3-1
      *> rules: [1989-AR-A.2.13.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.13.3 AR1 (THE DAY-OF-INTEGER FUNCTION). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. DOITEST.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01  W-DATE PIC 9(7).
000600 PROCEDURE DIVISION.
000700 MAIN-PARA.
000800     COMPUTE W-DATE = FUNCTION DAY-OF-INTEGER (1).
000900     DISPLAY W-DATE.
001000     COMPUTE W-DATE = FUNCTION DAY-OF-INTEGER (366).
001100     DISPLAY W-DATE.
001200     STOP RUN.
