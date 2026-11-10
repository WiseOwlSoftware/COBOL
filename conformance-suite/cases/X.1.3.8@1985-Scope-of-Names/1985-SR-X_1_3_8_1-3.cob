      *>--- conformance-case
      *> id: 1985-SR-X_1_3_8_1-3
      *> rules: [1985-SR-X.1.3.8.1-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.1.3.8.1 SR3 (Scope of Names). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINP.
       PROCEDURE DIVISION.
       P1.
           CALL "SUBP".
           DISPLAY "BACK IN MAINP".
           CALL "NESTP".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NESTP.
       PROCEDURE DIVISION.
       P1.
           CALL "SUBP".
           DISPLAY "BACK IN NESTP".
           EXIT PROGRAM.
       END PROGRAM NESTP.
       END PROGRAM MAINP.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  CALL-COUNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           ADD 1 TO CALL-COUNT.
           DISPLAY "IN SUBP " CALL-COUNT.
           IF CALL-COUNT = 1
               CALL "SUBIN".
       P2.
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBIN.
       PROCEDURE DIVISION.
       P1.
           CALL "SUBP"
               ON EXCEPTION DISPLAY "SUBP NOT IN SUBIN SCOPE"
               NOT ON EXCEPTION DISPLAY "SUBP REACHED FROM SUBIN"
           END-CALL.
           EXIT PROGRAM.
       END PROGRAM SUBIN.
       END PROGRAM SUBP.
