      *>--- conformance-case
      *> id: 1985-GR-X_2_3_3-3
      *> rules: [1985-GR-X.2.3.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.2.3.3 GR3 (STRUCTURE). An output test: the program must
      *> translate and run at 85 and DISPLAY exactly the lines its .out
      *> holds, the outcome the rule requires. Placed from the audit's
      *> draft, confirmed by scripts/spec/place_witnesses.py (kb/Work
      *> PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OUTERP.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "OUTER START".
           CALL "INNERP".
           DISPLAY "OUTER END".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNERP.
       PROCEDURE DIVISION.
       INNER-PARA.
           DISPLAY "INNER".
           EXIT PROGRAM.
       END PROGRAM INNERP.
       END PROGRAM OUTERP.
