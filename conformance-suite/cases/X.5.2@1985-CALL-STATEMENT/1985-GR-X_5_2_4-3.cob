      *>--- conformance-case
      *> id: 1985-GR-X_5_2_4-3
      *> rules: [1985-GR-X.5.2.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.2.4 GR3 (THE CALL STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CALLER.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "NOSUCHPG"
               ON EXCEPTION
                   DISPLAY "EXCEPTION"
               NOT ON EXCEPTION
                   DISPLAY "CALLED"
           END-CALL.
           DISPLAY "DONE".
           STOP RUN.
