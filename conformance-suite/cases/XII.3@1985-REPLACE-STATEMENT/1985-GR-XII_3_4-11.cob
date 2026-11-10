      *>--- conformance-case
      *> id: 1985-GR-XII_3_4-11
      *> rules: [1985-GR-XII.3.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.3.4 GR11 (THE REPLACE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. REPL11.
000300 PROCEDURE DIVISION.
000400     REPLACE ==XLIT== BY
000500     =="AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
000600-    "BBBBBBBBBBBBBBBBBBBBBB"==.
000700 MAIN-PARA.
000800     DISPLAY XLIT.
000900     STOP RUN.
