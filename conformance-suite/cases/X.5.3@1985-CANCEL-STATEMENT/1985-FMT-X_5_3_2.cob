      *>--- conformance-case
      *> id: 1985-FMT-X_5_3_2
      *> rules: [1985-FMT-X.5.3.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.3.2 (THE CANCEL STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-NAME PIC X(8) VALUE "SUBP".
       PROCEDURE DIVISION.
       P1.
           CALL "SUBP".
           CANCEL "SUBP".
           CALL WS-NAME.
           CANCEL WS-NAME "SUBP".
           DISPLAY "DONE".
           STOP RUN.
       END PROGRAM MAINP.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-CNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       S1.
           ADD 1 TO WS-CNT.
           DISPLAY "COUNT " WS-CNT.
           EXIT PROGRAM.
       END PROGRAM SUBP.
