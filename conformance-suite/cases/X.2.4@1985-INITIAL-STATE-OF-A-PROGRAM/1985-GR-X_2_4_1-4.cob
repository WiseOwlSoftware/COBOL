      *>--- conformance-case
      *> id: 1985-GR-X_2_4_1-4
      *> rules: [1985-GR-X.2.4.1-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.2.4.1 GR4 (INITIAL STATE OF A PROGRAM). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINPROG.
       PROCEDURE DIVISION.
       M-START.
           CALL "SUBPROG".
           CALL "SUBPROG".
           DISPLAY "DONE".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBPROG IS INITIAL PROGRAM.
       PROCEDURE DIVISION.
       S-SWITCH.
           GO TO S-FIRST.
       S-FIRST.
           DISPLAY "FIRST".
           ALTER S-SWITCH TO PROCEED TO S-SECOND.
           GO TO S-EXIT.
       S-SECOND.
           DISPLAY "SECOND".
       S-EXIT.
           EXIT PROGRAM.
       END PROGRAM SUBPROG.
       END PROGRAM MAINPROG.
