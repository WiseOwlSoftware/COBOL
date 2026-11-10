      *>--- conformance-case
      *> id: 1985-SR-VI_3_4_3-1
      *> rules: [1985-SR-VI.3.4.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.3.4.3 SR1 (THE DATE-COMPILED PARAGRAPH). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. DCTEST.
000300 DATE-COMPILED. ANY CHARACTERS * / $ ; , ( ) + = < > ARE ALLOWED
000400     AND THIS COMMENT-ENTRY IS CONTAINED ON A SECOND LINE.
000500 ENVIRONMENT DIVISION.
000600 DATA DIVISION.
000700 PROCEDURE DIVISION.
000800 P1.
000900     DISPLAY "DATE-COMPILED OK".
001000     STOP RUN.
