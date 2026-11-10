      *>--- conformance-case
      *> id: 1985-FMT-IV_5_3_1
      *> rules: [1985-FMT-IV.5.3.1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.5.3.1 (STRUCTURE). An output test: the program must
      *> translate and run at 85 and DISPLAY exactly the lines its .out
      *> holds, the outcome the rule requires. Placed from the audit's
      *> draft, confirmed by scripts/spec/place_witnesses.py (kb/Work
      *> PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. MINPROG.
000300 PROCEDURE DIVISION.
000400 MAIN-PARA.
000500     DISPLAY "OK".
000600     STOP RUN.
