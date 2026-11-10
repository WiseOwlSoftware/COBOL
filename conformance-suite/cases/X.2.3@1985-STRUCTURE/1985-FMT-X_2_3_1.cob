      *>--- conformance-case
      *> id: 1985-FMT-X_2_3_1
      *> rules: [1985-FMT-X.2.3.1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.2.3.1 (STRUCTURE). An output test: the program must
      *> translate and run at 85 and DISPLAY exactly the lines its .out
      *> holds, the outcome the rule requires. Placed from the audit's
      *> draft, confirmed by scripts/spec/place_witnesses.py (kb/Work
      *> PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. OUTER.
000300 PROCEDURE DIVISION.
000400 MAIN-PARA.
000500     DISPLAY "OUTER START".
000600     CALL "INNER".
000700     DISPLAY "OUTER END".
000800     STOP RUN.
000900 IDENTIFICATION DIVISION.
001000 PROGRAM-ID. INNER.
001100 PROCEDURE DIVISION.
001200 INNER-PARA.
001300     DISPLAY "INNER".
001400     EXIT PROGRAM.
001500 END PROGRAM INNER.
001600 END PROGRAM OUTER.
