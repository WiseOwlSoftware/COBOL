      *>--- conformance-case
      *> id: 1985-SR-II_6_4_1_2-4
      *> rules: [1985-SR-II.6.4.1.2-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:II.6.4.1.2 SR4 (Transfer of Control). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OUTER.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "OUTER START".
           CALL "INNERA".
           DISPLAY "OUTER END".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNERA.
       PROCEDURE DIVISION.
       A-PARA.
           DISPLAY "IN INNERA".
           CALL "INNERB"
               ON EXCEPTION DISPLAY "INNERB NOT CALLABLE"
               NOT ON EXCEPTION DISPLAY "INNERB CALLED".
           EXIT PROGRAM.
       END PROGRAM INNERA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNERB.
       PROCEDURE DIVISION.
       B-PARA.
           DISPLAY "IN INNERB".
           EXIT PROGRAM.
       END PROGRAM INNERB.
       END PROGRAM OUTER.
