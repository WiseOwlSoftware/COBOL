      *>--- conformance-case
      *> id: 1985-GR-X_5_2_4-15
      *> rules: [1985-GR-X.5.2.4-15]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.2.4 GR15 (THE CALL STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINPG.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "MAIN START".
           CALL "SUBA".
           DISPLAY "MAIN END".
           STOP RUN.
       END PROGRAM MAINPG.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBA.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "SUBA START".
           CALL "SUBB".
           DISPLAY "SUBA END".
       P2.
           EXIT PROGRAM.
       END PROGRAM SUBA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBB.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "SUBB".
       P2.
           EXIT PROGRAM.
       END PROGRAM SUBB.
