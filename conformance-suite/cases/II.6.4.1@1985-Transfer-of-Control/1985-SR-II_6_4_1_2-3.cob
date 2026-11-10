      *>--- conformance-case
      *> id: 1985-SR-II_6_4_1_2-3
      *> rules: [1985-SR-II.6.4.1.2-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:II.6.4.1.2 SR3 (Transfer of Control). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OUTER.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "CALLER".
           DISPLAY "OUTER DONE".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CALLER.
       PROCEDURE DIVISION.
       C-PARA.
           DISPLAY "CALLER START".
           CALL "COMPROG".
           DISPLAY "CALLER END".
           EXIT PROGRAM.
       END PROGRAM CALLER.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. COMPROG IS COMMON PROGRAM.
       PROCEDURE DIVISION.
       P-PARA.
           DISPLAY "COMPROG RAN".
           EXIT PROGRAM.
       END PROGRAM COMPROG.
       END PROGRAM OUTER.
