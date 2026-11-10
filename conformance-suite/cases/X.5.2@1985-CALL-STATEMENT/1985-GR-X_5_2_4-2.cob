      *>--- conformance-case
      *> id: 1985-GR-X_5_2_4-2
      *> rules: [1985-GR-X.5.2.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.2.4 GR2 (THE CALL STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINPRG.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "BEFORE CALL".
           CALL "SUBPRG".
           DISPLAY "AFTER CALL 1".
           CALL "SUBPRG"
               ON OVERFLOW DISPLAY "OVERFLOW"
           END-CALL.
           DISPLAY "AFTER CALL 2".
           CALL "SUBPRG"
               ON EXCEPTION DISPLAY "EXCEPTION"
           END-CALL.
           DISPLAY "AFTER CALL 3".
           CALL "SUBPRG"
               ON EXCEPTION DISPLAY "EXCEPTION"
               NOT ON EXCEPTION DISPLAY "NOT EXCEPTION 1"
                                DISPLAY "NOT EXCEPTION 2"
           END-CALL.
           DISPLAY "AFTER CALL 4".
           STOP RUN.
       END PROGRAM MAINPRG.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBPRG.
       PROCEDURE DIVISION.
       S1.
           DISPLAY "IN SUBPRG".
           EXIT PROGRAM.
       END PROGRAM SUBPRG.
