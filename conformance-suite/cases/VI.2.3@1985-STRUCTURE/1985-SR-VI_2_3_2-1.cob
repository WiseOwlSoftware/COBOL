      *>--- conformance-case
      *> id: 1985-SR-VI_2_3_2-1
      *> rules: [1985-SR-VI.2.3.2-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.2.3.2 SR1 (STRUCTURE). An output test: the program
      *> must translate and run at 85 and DISPLAY exactly the lines its
      *> .out holds, the outcome the rule requires. Placed from the
      *> audit's draft, confirmed by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. SR1TEST.
000300 ENVIRONMENT DIVISION.
000400 DATA DIVISION.
000500 PROCEDURE DIVISION.
000600 MAIN-PARA.
000700     DISPLAY "STRUCTURE OK".
000800     STOP RUN.
000900 END PROGRAM SR1TEST.
