      *>--- conformance-case
      *> id: 1985-GR-IV_6_4_2_1_1_1-7
      *> rules: [1985-GR-IV.6.4.2.1.1.1-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.1.1.1 GR7 (Definition of Conditional Statement).
      *> An output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINPG.
       PROCEDURE DIVISION.
       P1.
           CALL "SUBPG"
               ON EXCEPTION DISPLAY "EXCEPTION"
               NOT ON EXCEPTION DISPLAY "CALLED"
           END-CALL
           DISPLAY "DONE"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBPG.
       PROCEDURE DIVISION.
       S1.
           DISPLAY "IN SUB"
           EXIT PROGRAM.
       END PROGRAM SUBPG.
       END PROGRAM MAINPG.
