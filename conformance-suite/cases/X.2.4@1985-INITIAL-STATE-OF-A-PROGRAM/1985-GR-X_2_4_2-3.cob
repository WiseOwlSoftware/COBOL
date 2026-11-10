      *>--- conformance-case
      *> id: 1985-GR-X_2_4_2-3
      *> rules: [1985-GR-X.2.4.2-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.2.4.2 GR3 (INITIAL STATE OF A PROGRAM). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINPROG.
       PROCEDURE DIVISION.
       P1.
           CALL "SUBPROG".
           CALL "SUBPROG".
           STOP RUN.
       END PROGRAM MAINPROG.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBPROG IS INITIAL PROGRAM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           ADD 1 TO CNT.
           DISPLAY CNT.
           EXIT PROGRAM.
       END PROGRAM SUBPROG.
