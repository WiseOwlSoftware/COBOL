      *>--- conformance-case
      *> id: 1985-GR-X_2_5_4-4
      *> rules: [1985-GR-X.2.5.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.2.5.4 GR4 (END PROGRAM HEADER). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINPGM.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "MAIN START".
           CALL "SUBPGM".
           DISPLAY "MAIN END".
           STOP RUN.
       END PROGRAM MAINPGM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBPGM.
       PROCEDURE DIVISION.
       SUB-PARA.
           DISPLAY "IN SUBPGM".
           EXIT PROGRAM.
       END PROGRAM SUBPGM.
