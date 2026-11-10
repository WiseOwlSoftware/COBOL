      *>--- conformance-case
      *> id: 1985-GR-X_3_1_4-1
      *> rules: [1985-GR-X.3.1.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.3.1.4 GR1 (THE PROGRAM-ID PARAGRAPH AND NESTED SOURCE
      *> PROGRAMS). An output test: the program must translate and run
      *> at 85 and DISPLAY exactly the lines its .out holds, the outcome
      *> the rule requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OUTER.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "OUTER START".
           CALL "INNER".
           DISPLAY "OUTER END".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNER.
       PROCEDURE DIVISION.
       INNER-PARA.
           DISPLAY "INNER RUNS".
           EXIT PROGRAM.
       END PROGRAM INNER.
       END PROGRAM OUTER.
