      *>--- conformance-case
      *> id: 1985-SR-X_1_3_8_1-1
      *> rules: [1985-SR-X.1.3.8.1-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.1.3.8.1 SR1 (Scope of Names). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OUTER.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "OUTER START".
           CALL "INNER".
           CALL "P2".
           DISPLAY "OUTER END".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNER.
       PROCEDURE DIVISION.
       INNER-PARA.
           DISPLAY "INNER CALLED".
           EXIT PROGRAM.
       END PROGRAM INNER.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2.
       PROCEDURE DIVISION.
       P2-PARA.
           CALL "INNER"
               ON EXCEPTION DISPLAY "INNER NOT IN P2 SCOPE"
               NOT ON EXCEPTION DISPLAY "INNER REACHED FROM P2"
           END-CALL.
           EXIT PROGRAM.
       END PROGRAM P2.
       END PROGRAM OUTER.
