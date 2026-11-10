      *>--- conformance-case
      *> id: 1985-GR-X_2_5_4-3
      *> rules: [1985-GR-X.2.5.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.2.5.4 GR3 (END PROGRAM HEADER). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OUTER.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "INNER1".
           CALL "INNER2".
           DISPLAY "OUTER DONE".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNER1.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "INNER1".
           EXIT PROGRAM.
       END PROGRAM INNER1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNER2.
       PROCEDURE DIVISION.
       P2.
           DISPLAY "INNER2".
           EXIT PROGRAM.
       END PROGRAM INNER2.
       END PROGRAM OUTER.
